using UnityEngine;

/// <summary>
/// Applies complete Fossen hydrodynamic forces to the AUV:
/// 1. Quadratic Translational Drag (via spherical lookup table & Center of Pressure)
/// 2. Linear Translational Drag (low-speed skin friction)
/// 3. Angular Drag (Linear + Quadratic resistance to Roll, Pitch, Yaw)
/// 4. Added Mass & Added Inertia (Force resisting acceleration)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class HydrodynamicDrag : MonoBehaviour
{
    public enum DragAreaMode
    {
        ConstantArea,
        LookupTable,
        LumpedCasADi
    } 

    [Header("Environment")]
    [Tooltip("Global water current velocity. Use this to test PID disturbance rejection. Units: [m/s]")]
    public Vector3 waterCurrent = Vector3.zero;

    [Header("Quadratic Translational Drag")]
    [Tooltip("Method for computing quadratic drag and area")]
    public DragAreaMode dragAreaMode = DragAreaMode.LumpedCasADi;

    [Tooltip("Water density (freshwater ≈ 1000, seawater ≈ 1025). Units: [kg/m³]")]
    public float waterDensity = 1000f;
    
    [Tooltip("Directional quadratic drag coefficients in Unity coordinates (X=Sway/Lateral, Y=Heave/Vertical, Z=Surge/Forward). Streamlined forward, blunt sides. Units: [dimensionless]")]
    public Vector3 dragCoefficients = new Vector3(0.031f ,2.623f, 2.188f);

    [Tooltip("Lumped quadratic drag from CasADi system identification in Unity coordinates (X=Sway, Y=Heave, Z=Surge). Used when dragAreaMode = LumpedCasADi. Bypasses water density and area. Units: [N/(m/s)²]")]
    public Vector3 lumpedQuadraticDrag = new Vector3(1.0000f, 127.3154f, 72.3561f);

    [Tooltip("Fixed cross-sectional area (used when mode = ConstantArea). Units: [m²]")]
    public float constantArea = 0.25f;

    [Tooltip("JSON file containing the baked projected area lookup table")]
    public TextAsset lookupTableJson;
    
    [Tooltip("Apply force at center of pressure (creates torque). Disable for CoM-only drag.")]
    public bool useCenterOfPressure = true;

    // ---------------------------------------------------------
    // NEW FOSSEN PHYSICS PARAMETERS
    // ---------------------------------------------------------
    [Header("Angular Drag")]
    [Tooltip("Quadratic angular drag in Unity coordinates (X=Pitch, Y=Yaw, Z=Roll). Units: [N·m·s²/rad²]")]
    public Vector3 angularQuadraticDrag = new Vector3(9.4194f, 0.2500f, 0.7538f);

    [Header("Cross-Coupling")]
    [Tooltip("Torque coupling coefficient from forward surge velocity to pitch torque. Negative values induce pitch-down when surging forward. Units: [N·m/(m/s)²]")]
    public float surgeToPitchCoupling = -0.4f;

    [Header("Added Mass / Inertia")]
    [Tooltip("Translational added mass in Unity coordinates (X=Sway/Lateral, Y=Heave/Vertical, Z=Surge/Forward). Units: [kg]")]
    public Vector3 addedMassTranslational = new Vector3(1.0000f, 1.0000f, 1.0000f);
     
    [Tooltip("Rotational added inertia in Unity coordinates (X=Pitch, Y=Yaw, Z=Roll). Units: [kg·m²]")]
    public Vector3 addedMassRotational = new Vector3(0.0200f, 0.0200f, 0.0200f);

    [Tooltip("Low-pass filter alpha for acceleration. Smooths numerical differentiation to prevent PhysX jitter. Units: [dimensionless, 0 to 1]")]
    [Range(0.01f, 1f)]
    public float addedMassFilterAlpha = 0.5f;


    [Header("Debug")]
    public bool debugLogging = false;
    public bool visualize = false;

    private Rigidbody rb;
    
    // Lookup table data
    private float[,] projectedAreaLookup;
    private float[,] offsetXLookup;  // Camera-local X offset (meters)
    private float[,] offsetYLookup;  // Camera-local Y offset (meters)
    private int nLon, nLat;
    private bool lookupLoaded = false;
    private bool hasOffsetData = false;

    // State tracking for Added Mass (Numerical Differentiation)
    private Vector3 prevLocalVelocity;
    private Vector3 prevLocalAngularVelocity;
    private Vector3 filteredLocalAccel;
    private Vector3 filteredLocalAngAccel;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Ensure native damping is disabled so our physics run the show
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
    }

    private void Start()
    {
        if (dragAreaMode == DragAreaMode.LookupTable)
        {
            LoadLookupTable();
        }

        // Initialize state for acceleration tracking
        prevLocalVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        prevLocalAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity);
    }

    /// <summary>
    /// Resets internal velocity and acceleration tracking. Call upon simulation teleport/reset to prevent added mass acceleration spikes.
    /// </summary>
    public void ResetState()
    {
        prevLocalVelocity = Vector3.zero;
        prevLocalAngularVelocity = Vector3.zero;
        filteredLocalAccel = Vector3.zero;
        filteredLocalAngAccel = Vector3.zero;
    }

    private void FixedUpdate()
    {
        // Only apply hydrodynamics when underwater
        if (transform.position.y > 0) return;
        
        // CALCULATE RELATIVE VELOCITY
        Vector3 relativeVelocity = rb.linearVelocity - waterCurrent;
        
        // 1. Existing Quadratic Drag (Form drag via lookup or Lumped CasADi)
        ApplyHydrodynamicDrag(relativeVelocity);

        // 2. Angular Drag (Roll, Pitch, Yaw)
        ApplyAngularDrag();

        // 3. Added Mass (Resisting acceleration)
        ApplyAddedMass();

        // 4. Cross-Coupling (Surge to Pitch)
        ApplyCrossCoupling(relativeVelocity);

        // Update state for next frame's acceleration calculation
        prevLocalVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        prevLocalAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity);
    }

    private void ApplyCrossCoupling(Vector3 relativeVelocity)
    {
        Vector3 localVel = transform.InverseTransformDirection(relativeVelocity);
        float surgeSpeed = localVel.z;
        
        // Surge-to-Pitch Coupling: surging forward induces a pitch-down torque around local X axis
        float pitchTorque = surgeToPitchCoupling * surgeSpeed * Mathf.Abs(surgeSpeed);
        
        rb.AddRelativeTorque(new Vector3(pitchTorque, 0f, 0f), ForceMode.Force);
    }

    private void ApplyAngularDrag()
    {
        Vector3 localAngVel = transform.InverseTransformDirection(rb.angularVelocity);
        
        // Torque = - D_quad * omega * |omega|
        Vector3 localTorque = new Vector3(
            -localAngVel.x * angularQuadraticDrag.x * Mathf.Abs(localAngVel.x),
            -localAngVel.y * angularQuadraticDrag.y * Mathf.Abs(localAngVel.y),
            -localAngVel.z * angularQuadraticDrag.z * Mathf.Abs(localAngVel.z)
        );
        
        rb.AddRelativeTorque(localTorque, ForceMode.Force);
    }

    private void ApplyAddedMass()
    {
        if (Time.fixedDeltaTime <= 0) return;

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        Vector3 localAngVel = transform.InverseTransformDirection(rb.angularVelocity);

        // a = (v - v_prev) / dt
        Vector3 rawAccel = (localVel - prevLocalVelocity) / Time.fixedDeltaTime;
        Vector3 rawAngAccel = (localAngVel - prevLocalAngularVelocity) / Time.fixedDeltaTime;

        // Apply low-pass filter to prevent discrete differentiation feedback explosion
        filteredLocalAccel = Vector3.Lerp(filteredLocalAccel, rawAccel, addedMassFilterAlpha);
        filteredLocalAngAccel = Vector3.Lerp(filteredLocalAngAccel, rawAngAccel, addedMassFilterAlpha);

        // F_added = - M_A * a
        Vector3 addedMassForce = new Vector3(
            -addedMassTranslational.x * filteredLocalAccel.x,
            -addedMassTranslational.y * filteredLocalAccel.y,
            -addedMassTranslational.z * filteredLocalAccel.z
        );

        // Torque_added = - I_A * alpha
        Vector3 addedMassTorque = new Vector3(
            -addedMassRotational.x * filteredLocalAngAccel.x,
            -addedMassRotational.y * filteredLocalAngAccel.y,
            -addedMassRotational.z * filteredLocalAngAccel.z
        );

        rb.AddRelativeForce(addedMassForce, ForceMode.Force);
        rb.AddRelativeTorque(addedMassTorque, ForceMode.Force);
    }

    // ====================================================================================
    // EXISTING LOOKUP TABLE & QUADRATIC DRAG LOGIC (Unchanged, retained for completeness)
    // ====================================================================================

    private void ApplyHydrodynamicDrag(Vector3 relativeVelocity)
    {
        Vector3 velocity = relativeVelocity;
        float speedSqr = velocity.sqrMagnitude;

        if (speedSqr < 0.0001f) return;

        float speed = Mathf.Sqrt(speedSqr);
        Vector3 velocityDir = velocity / speed;

        Vector3 dragForce;
        float forceMagnitude;
        Vector3 pressureOffset = Vector3.zero;
        float area = 1.0f;

        if (dragAreaMode == DragAreaMode.LumpedCasADi)
        {
            // Convert velocity to AUV local frame and apply lumped CasADi quadratic drag directly without density/area scaling
            Vector3 localVel = transform.InverseTransformDirection(velocity);
            Vector3 localDragForce = new Vector3(
                -lumpedQuadraticDrag.x * Mathf.Abs(localVel.x) * localVel.x,
                -lumpedQuadraticDrag.y * Mathf.Abs(localVel.y) * localVel.y,
                -lumpedQuadraticDrag.z * Mathf.Abs(localVel.z) * localVel.z
            );
            dragForce = transform.TransformDirection(localDragForce);
            forceMagnitude = dragForce.magnitude;

            // Still get pressure center offset if enabled for realistic hydrodynamic torque application
            if (useCenterOfPressure)
            {
                area = GetProjectedArea(velocityDir, out pressureOffset);
            }
        }
        else
        {
            // Get projected area and pressure center offset
            area = GetProjectedArea(velocityDir, out pressureOffset);

            // Convert velocity direction to AUV local frame to compute effective directional drag coefficient
            Vector3 localDir = transform.InverseTransformDirection(velocityDir);

            // Weight directional coefficients by component direction (absolute value prevents negative drag)
            float effectiveCd = Mathf.Abs(localDir.x) * dragCoefficients.x + 
                                Mathf.Abs(localDir.y) * dragCoefficients.y + 
                                Mathf.Abs(localDir.z) * dragCoefficients.z;

            // Drag force: F = 0.5 * ρ * A * Cd_effective * v² (applied along exactly -velocityDir)
            forceMagnitude = 0.5f * waterDensity * area * effectiveCd * speedSqr;
            dragForce = -velocityDir * forceMagnitude;
        }

        // Apply force at pressure center (creates torque) or at CoM
        if (useCenterOfPressure && hasOffsetData && pressureOffset.sqrMagnitude > 0.0001f)
        {
            Vector3 worldPressureCenter = rb.worldCenterOfMass + transform.TransformDirection(pressureOffset);
            rb.AddForceAtPosition(dragForce, worldPressureCenter, ForceMode.Force);

            if (visualize)
            {
                Debug.DrawLine(rb.worldCenterOfMass, worldPressureCenter, Color.yellow);
                Debug.DrawRay(worldPressureCenter, dragForce.normalized * 0.5f, Color.red);
            }
        }
        else
        {
            rb.AddForce(dragForce, ForceMode.Force);
            
            if (visualize)
            {
                Debug.DrawRay(rb.worldCenterOfMass, dragForce.normalized * 0.5f, Color.red);
            }
        }

        if (debugLogging)
        {
            Debug.Log($"[Drag] Speed: {speed:F2} m/s, Area: {area:F4} m², Force: {forceMagnitude:F1} N, Offset: {pressureOffset}");
        }
    }

    private float GetProjectedArea(Vector3 velocityDir, out Vector3 pressureOffset)
    {
        pressureOffset = Vector3.zero;

        switch (dragAreaMode)
        {
            case DragAreaMode.ConstantArea:
                return constantArea;

            case DragAreaMode.LookupTable:
                return lookupLoaded ? SampleLookupTable(velocityDir, out pressureOffset) : constantArea;

            case DragAreaMode.LumpedCasADi:
                if (lookupLoaded && useCenterOfPressure) SampleLookupTable(velocityDir, out pressureOffset);
                return 1.0f; // Area is unused in LumpedCasADi mode

            default:
                return constantArea;
        }
    }


    private void LoadLookupTable()
    {
        if (lookupTableJson == null)
        {
            Debug.LogWarning("[HydrodynamicDrag] No lookup table assigned. Falling back to constant area.");
            return;
        }

        try
        {
            DragLookupData data = JsonUtility.FromJson<DragLookupData>(lookupTableJson.text);
            nLon = data.longitudeSamples;
            nLat = data.latitudeSamples;
            projectedAreaLookup = new float[nLat, nLon];
            
            // Load area data
            for (int i = 0; i < nLat; i++)
            {
                for (int j = 0; j < nLon; j++)
                {
                    projectedAreaLookup[i, j] = data.flatData[i * nLon + j];
                }
            }

            // Load offset data if available
            if (data.offsetXData != null && data.offsetYData != null && 
                data.offsetXData.Length == nLat * nLon && data.offsetYData.Length == nLat * nLon)
            {
                offsetXLookup = new float[nLat, nLon];
                offsetYLookup = new float[nLat, nLon];
                
                for (int i = 0; i < nLat; i++)
                {
                    for (int j = 0; j < nLon; j++)
                    {
                        offsetXLookup[i, j] = data.offsetXData[i * nLon + j];
                        offsetYLookup[i, j] = data.offsetYData[i * nLon + j];
                    }
                }
                hasOffsetData = true;
                if(debugLogging) Debug.Log($"[HydrodynamicDrag] Loaded lookup table with pressure center offsets: {nLon}×{nLat}");
            }
            else
            {
                hasOffsetData = false;
                if(debugLogging) Debug.Log($"[HydrodynamicDrag] Loaded lookup table (no offset data): {nLon}×{nLat}");
            }

            lookupLoaded = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[HydrodynamicDrag] Failed to load lookup table: {e.Message}");
            lookupLoaded = false;
        }
    }

    private float SampleLookupTable(Vector3 velocityDir, out Vector3 pressureOffset)
    {
        // Convert velocity direction to the AUV's local frame
        Vector3 localDir = transform.InverseTransformDirection(velocityDir);

        // Convert Cartesian to spherical coordinates
        float lat = Mathf.Asin(Mathf.Clamp(localDir.y, -1f, 1f));
        float lon = Mathf.Atan2(localDir.x, localDir.z);
        if (lon < 0) lon += 2f * Mathf.PI;

        // Map to continuous indices
        // Latitude: +π/2 (top) = v=0, -π/2 (bottom) = v=nLat-1 (matches Baker)
        float u = lon / (2f * Mathf.PI) * nLon; // 0 to nLon
        float v = (0.5f - lat / Mathf.PI) * (nLat - 1); // Inverted to match Baker

        // Get integer and fractional parts for Bilinear Interpolation
        int x0 = Mathf.FloorToInt(u) % nLon;
        int x1 = (x0 + 1) % nLon; // Wrap around longitude
        float u_frac = u - Mathf.Floor(u);

        int y0 = Mathf.Clamp(Mathf.FloorToInt(v), 0, nLat - 1);
        int y1 = Mathf.Clamp(y0 + 1, 0, nLat - 1); // Clamp latitude (no wrap)
        float v_frac = v - y0;

        // Helper to get data safely
        float GetArea(int x, int y) => projectedAreaLookup[y, x];

        // Bilinear Interpolation for Area
        float areaBot = Mathf.Lerp(GetArea(x0, y0), GetArea(x1, y0), u_frac);
        float areaTop = Mathf.Lerp(GetArea(x0, y1), GetArea(x1, y1), u_frac);
        float finalArea = Mathf.Lerp(areaBot, areaTop, v_frac);

        pressureOffset = Vector3.zero;

        if (hasOffsetData)
        {
            float GetOffX(int x, int y) => offsetXLookup[y, x];
            float GetOffY(int x, int y) => offsetYLookup[y, x];

            // Interpolate X Offset
            float oxBot = Mathf.Lerp(GetOffX(x0, y0), GetOffX(x1, y0), u_frac);
            float oxTop = Mathf.Lerp(GetOffX(x0, y1), GetOffX(x1, y1), u_frac);
            float finalOffX = Mathf.Lerp(oxBot, oxTop, v_frac);

            // Interpolate Y Offset
            float oyBot = Mathf.Lerp(GetOffY(x0, y0), GetOffY(x1, y0), u_frac);
            float oyTop = Mathf.Lerp(GetOffY(x0, y1), GetOffY(x1, y1), u_frac);
            float finalOffY = Mathf.Lerp(oyBot, oyTop, v_frac);

            // Basis Construction
            // Standardize basis to match Baker's Camera orientation
            Vector3 right, up;
            if (Mathf.Abs(localDir.y) > 0.99f) // Increased threshold for stability at poles
            {
                right = Vector3.Cross(Vector3.forward, localDir).normalized;
            }
            else
            {
                right = Vector3.Cross(Vector3.up, localDir).normalized;
            }
            up = Vector3.Cross(localDir, right).normalized;

            // Apply offsets
            // CRITICAL: Negate X offset because Baker camera looks AT object (rotated 180 deg)
            // Screen Right in baking = World Left relative to velocity vector
            pressureOffset = right * (-finalOffX) + up * finalOffY;
        }

        return finalArea;
    }

}

/// <summary>
/// Serializable data structure for the drag lookup table.
/// Uses flat arrays with row-major indexing: data[lat * nLon + lon]
/// </summary>
[System.Serializable]
public class DragLookupData
{
    public int longitudeSamples;
    public int latitudeSamples;
    public float[] flatData;      // Projected area (m²)
    public float[] offsetXData;   // Pressure center X offset (m)
    public float[] offsetYData;   // Pressure center Y offset (m)
}
