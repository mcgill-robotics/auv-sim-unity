using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Applies buoyancy force to the AUV. Force is applied at the center of buoyancy,
/// which creates a righting torque when offset from the center of mass.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HydrodynamicDrag))]
public class Buoyancy : MonoBehaviour
{
    [Header("Buoyancy Configuration")]
    [Tooltip("Point where buoyancy force is applied (local coordinates). Should be above COM for stability.")]
    public Vector3 centerOfBuoyancy;

    [Tooltip("Center of mass offset (local coordinates). Applied to Rigidbody on Start.")]
    public Vector3 centerOfMass;


    [Tooltip("BoxCollider of chassis, used for approximating submerged volume and surface projection. Equilibrium height will be exactly at the top of this collider.")]
    [SerializeField] private BoxCollider ChassisCollider;

    [Tooltip("BoxCollider of foam top, used for approximating submerged volume and surface projection. Equilibrium height will be exactly at the bottom of this collider.")]
    [SerializeField] private BoxCollider FoamTopCollider;

    [Tooltip("Reference to the WaterSurface component for projecting points onto the water surface")]
    [SerializeField] private WaterSurface waterSurface;

    [Tooltip("Reference to the HydrodynamicDrag component to access water density for buoyancy calculations")]
    [SerializeField] private HydrodynamicDrag hydrodynamicDrag;

    [Header("Debug")]
    [Tooltip("Log buoyancy force each frame")]
    public bool debugLogging = false;

    // cached reference to the AUV's Rigidbody for applying forces and setting center of mass
    private Rigidbody auvRb;

    private float DepthofAUVBottom;
    private float HeightofAUV;
    private float HeightOfChassis;
    // buoyancy scaling does not depend on depth underwater, it can be computed once at the start
    private float buoyancyScalingFactor;


    /// <summary>
    /// Called when a value changes in the Inspector. Updates the Rigidbody's center of mass immediately.
    /// </summary>
    private void OnValidate()
    {
        // Get or cache the rigidbody reference
        if (auvRb == null)
            auvRb = GetComponent<Rigidbody>();

        if (auvRb != null)
        {
            auvRb.centerOfMass = centerOfMass;
        }
    }

    private void Start()
    {
        auvRb = GetComponent<Rigidbody>();
        auvRb.centerOfMass = centerOfMass;

        Bounds combinedBounds = ChassisCollider.bounds;
        combinedBounds.Encapsulate(FoamTopCollider.bounds);
        HeightofAUV = combinedBounds.size.y;
        HeightOfChassis = ChassisCollider.bounds.size.y;
        // very crude approximation of area based on bounding box, but should be sufficient for scaling buoyancy force
        float auvArea = combinedBounds.size.x * combinedBounds.size.z;

        // Archimedes' principle: Buoyant force = density of fluid * volume of displaced fluid * gravity
        // Underwater, the volume of displaced fluid is equal to the volume of the AUV, so we can use that for our approximation
        // As the AUV emerges, the displaced volume decreases with the height, we use area for now
        buoyancyScalingFactor = hydrodynamicDrag.waterDensity * auvArea * Physics.gravity.magnitude;
    }


    private void FixedUpdate()
    {
        // Allow runtime tweaking of COM
#if UNITY_EDITOR
        if (auvRb.centerOfMass != centerOfMass)
        {
            auvRb.centerOfMass = centerOfMass;
        }
#endif
        DepthofAUVBottom = ChassisCollider.bounds.min.y;

        if (BelowWater(DepthofAUVBottom, out float distanceToSurface))
        {
            Debug.Log($"Bottom of AUV is underwater at {DepthofAUVBottom}, surface projection is {distanceToSurface}");
            ApplyBuoyancyForce(transform.TransformPoint(centerOfBuoyancy), distanceToSurface);
        }
    }

    bool BelowWater(float DepthofAUVBottom, out float distanceToSurface)
    {
        if (SimulationSettings.Instance.NoWaterMode)
        {
            distanceToSurface = waterSurface.transform.position.y - DepthofAUVBottom;
            return distanceToSurface > 0;
        }
        else
        {
            WaterSearchParameters waterSearchParams = new WaterSearchParameters
            {
                startPositionWS = Vector3.zero,
                targetPositionWS = new Vector3(0, DepthofAUVBottom, 0),
                error = 0.01f,
                maxIterations = 8,
                includeDeformation = false, // Ignore water deformation for buoyancy force application for easier computation
            };
            bool result = waterSurface.ProjectPointOnWaterSurface(waterSearchParams, out WaterSearchResult projectedPoint);
            distanceToSurface = projectedPoint.projectedPositionWS.y - DepthofAUVBottom;
            return result && distanceToSurface > 0;
        }
    }
    /// Applies simple buoyancy force (Archimedes principle) at the given world position. Point is refered to as floater to keep buoyancy force application flexible enough to accept multiple floating points in the future. For now though, it should just be applied to the center of buoyancy, since any additional points would average out to applying a single force at the center of buoyancy anyway.
    /// </summary>
    /// <param name="floaterPosition"></param>
    void ApplyBuoyancyForce(Vector3 floaterPosition, float depth)
    {
        float submergedDepth = Mathf.Clamp(depth, 0, HeightofAUV);


        // Floater is submerged, apply upward buoyancy force, scale force by how deep the point is submerged
        Vector3 buoyancyForce = Vector3.up * buoyancyScalingFactor * submergedDepth;

        auvRb.AddForceAtPosition(buoyancyForce, floaterPosition, ForceMode.Force);
        if (debugLogging)
        {
            Debug.Log($"Applying buoyancy force of {buoyancyForce} N at {floaterPosition} (submerged depth: {submergedDepth:F2} m)");
        }

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.TransformPoint(centerOfMass), 0.02f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.TransformPoint(centerOfBuoyancy), 0.02f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.TransformPoint(centerOfBuoyancy), waterSurface.transform.position);
    }
}