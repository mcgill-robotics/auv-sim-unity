using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HydrodynamicDrag : MonoBehaviour
{
    public enum DragAreaMode
    {
        ConstantArea,
        ProjectedArea
    }

    [Header("Drag Model")]
    public DragAreaMode dragAreaMode = DragAreaMode.ProjectedArea;

    [Header("Fluid Properties")]
    public float waterDensity = 1000f;
    public float dragCoefficient = 1.1f;

    [Header("Constant Area Mode")]
    public float constantArea = 0.25f;

    private Rigidbody rb;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 0f; // disable Unity drag
        rb.linearDamping = 0f; // disable Unity drag
    }
    private void FixedUpdate()
    {
        ApplyHydrodynamicDrag();
    }

    private void ApplyHydrodynamicDrag()
    {
        Vector3 v = rb.linearVelocity;
        float speedSqr = v.sqrMagnitude;

        // If speed too low, no need to add drag
        if (speedSqr < 0.0001f)
            return;

        // Get normalized vector to calculate area in direction of motion
        Vector3 vDir = v.normalized;
        float area = dragAreaMode == DragAreaMode.ProjectedArea
            ? ComputeProjectedArea(vDir)
            : constantArea;

        // Use drag formula to calculate the drag velocity 
        // TODO figure where to apply the force instead of just COM of rigid body
        float forceMag =
            0.5f * waterDensity * dragCoefficient * area * speedSqr;
        Debug.Log(forceMag + " Current speed" + speedSqr + " V normalized" + v);
        rb.AddForce(-forceMag * v, ForceMode.Force);
    }

    private float ComputeProjectedArea(Vector3 velocityDir)
    {
        float projectedArea = 0f;

        AddFace(transform.right, 0.20f);
        AddFace(-transform.right, 0.20f);

        AddFace(transform.up, 0.20f);
        AddFace(-transform.up, 0.20f);

        AddFace(transform.forward, 0.25f);
        AddFace(-transform.forward, 0.25f);

        return projectedArea;

        void AddFace(Vector3 normal, float area)
        {
            float alignment = Vector3.Dot(normal, -velocityDir);
            if (alignment > 0f)
                projectedArea += area * alignment;
        }
    }
}
