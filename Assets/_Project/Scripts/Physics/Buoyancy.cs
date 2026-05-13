using System;
using System.Collections;
using System.Collections.Generic;
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
    // distance from bottom of the AUV to the transform position, used for determining how submerged the AUV is based on the depth of the transform position
    private float RelativeDepthofAUVBottom;
    // max submerged depth of the AUV, used for scaling buoyancy force as the AUV emerges from the water. Approximated based on the bounding box of the colliders, which should be sufficient for our purposes.
    private float HeightofAUV;
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
        // for now, we simply assume the AUV is one big box that completely displaces water when fully submerged, so the submerged volume (and thus buoyancy force) scales linearly with depth until the AUV is fully submerged, at which point it remains constant. A more accurate implementation would compute the buoyancy force from each component of the AUV
        // for instance, the foam top likely has a much larger impact on buoyancy than the chassis due to its lower density, so once that foam top component emerges, the buoyancy will reduce significantly, leading to equilibrium height as thge bottom of the foam collider. This refactor would require knowing the mass of the foam top and chassis separately, which we don't currently have, but could be added in the future if we want more accurate buoyancy behavior. For now, this simple approximation should be sufficient to get reasonable buoyancy behavior without needing to know the mass breakdown of the AUV components.
        // Calculate unrotated local bounds to prevent starting rotation (e.g. yaw) 
        // from artificially inflating the world AABB and causing stronger buoyancy.
        Bounds chassisLocalBounds = GetColliderLocalBounds(ChassisCollider);
        Bounds foamLocalBounds = GetColliderLocalBounds(FoamTopCollider);
        
        chassisLocalBounds.Encapsulate(foamLocalBounds);

        // Convert local bounds to unrotated world bounds using lossyScale
        Vector3 unrotatedMin = Vector3.Scale(chassisLocalBounds.min, transform.lossyScale);
        Vector3 unrotatedMax = Vector3.Scale(chassisLocalBounds.max, transform.lossyScale);
        Vector3 unrotatedSize = unrotatedMax - unrotatedMin;

        RelativeDepthofAUVBottom = unrotatedMin.y;
        HeightofAUV = unrotatedSize.y;
        // very crude approximation of area based on bounding box, but should be sufficient for scaling buoyancy force
        float auvArea = unrotatedSize.x * unrotatedSize.z;

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
        float DepthofAUVBottom = transform.position.y + RelativeDepthofAUVBottom;

        if (BelowWater(DepthofAUVBottom, out float distanceToSurface))
        {
            if (debugLogging)
            {
                Debug.Log($"AUV bottom is at depth {DepthofAUVBottom:F2} m, distance to surface is {distanceToSurface:F2} m");
            }
            ApplyBuoyancyForce(transform.TransformPoint(centerOfBuoyancy), distanceToSurface);
        }
    }

    // Determines if the AUV is below the water surface based on the depth of the bottom of the AUV. Also calculates the distance to the surface for buoyancy force scaling.
    bool BelowWater(float DepthofAUVBottom, out float distanceToSurface)
    {
        distanceToSurface = waterSurface.transform.position.y - DepthofAUVBottom;
        return distanceToSurface > 0;
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
            // add comparison of buoyancy force to weight of the AUV for debugging purposes
            float weight = auvRb.mass * Physics.gravity.magnitude;
            Debug.Log($"Applying buoyancy force of {buoyancyForce} N versus weight of {weight} N at {floaterPosition} (submerged depth: {submergedDepth:F2} m)");
        }

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.TransformPoint(centerOfMass), 0.02f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.TransformPoint(centerOfBuoyancy), 0.02f);
        Gizmos.DrawLine(transform.TransformPoint(centerOfBuoyancy), waterSurface.transform.position);
    }

    private Bounds GetColliderLocalBounds(BoxCollider col)
    {
        Vector3 center = col.center;
        Vector3 extents = col.size * 0.5f;

        Bounds bounds = new Bounds(transform.InverseTransformPoint(col.transform.TransformPoint(center + new Vector3(extents.x, extents.y, extents.z))), Vector3.zero);
        bounds.Encapsulate(transform.InverseTransformPoint(col.transform.TransformPoint(center + new Vector3(extents.x, extents.y, -extents.z))));
        bounds.Encapsulate(transform.InverseTransformPoint(col.transform.TransformPoint(center + new Vector3(extents.x, -extents.y, extents.z))));
        bounds.Encapsulate(transform.InverseTransformPoint(col.transform.TransformPoint(center + new Vector3(extents.x, -extents.y, -extents.z))));
        bounds.Encapsulate(transform.InverseTransformPoint(col.transform.TransformPoint(center + new Vector3(-extents.x, extents.y, extents.z))));
        bounds.Encapsulate(transform.InverseTransformPoint(col.transform.TransformPoint(center + new Vector3(-extents.x, extents.y, -extents.z))));
        bounds.Encapsulate(transform.InverseTransformPoint(col.transform.TransformPoint(center + new Vector3(-extents.x, -extents.y, extents.z))));
        bounds.Encapsulate(transform.InverseTransformPoint(col.transform.TransformPoint(center + new Vector3(-extents.x, -extents.y, -extents.z))));
        
        return bounds;
    }
}