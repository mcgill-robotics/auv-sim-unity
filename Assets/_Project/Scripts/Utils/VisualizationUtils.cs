using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Shared utility methods for creating 3D visualization elements (arrows, materials).
    /// Used by DVLPublisher, IMUPublisher, and Thrusters for debug visualization.
    /// </summary>
    public static class VisualizationUtils
    {
        /// <summary>
        /// Creates a material with the specified color. Supports HDRP, URP, and Standard pipeline.
        /// Remember to Destroy() the material in OnDestroy() to prevent memory leaks.
        /// </summary>
        public static Material CreateMaterial(Color color)
        {
            // Try render pipeline-specific shaders first
            Shader shader = Shader.Find("HDRP/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            
            Material mat = new Material(shader);
            
            // Set both _Color (Standard) and _BaseColor (HDRP/URP) to be safe
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            
            // Emission settings for visibility
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissiveColor")) mat.SetColor("_EmissiveColor", color * 0.5f);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * 0.5f);
            
            return mat;
        }
        
        /// <summary>
        /// Creates a simple 3D arrow (cylinder shaft + cube head) pointing in +Y direction.
        /// The arrow's pivot is at its base, making it easy to position and scale.
        /// Remember to Destroy() the returned GameObject in OnDestroy().
        /// </summary>
        /// <param name="name">Name for the arrow GameObject</param>
        /// <param name="mat">Material to apply to the arrow</param>
        /// <param name="thickness">Shaft diameter</param>
        /// <returns>Arrow root GameObject with Shaft and Head children</returns>
        public static GameObject CreateArrow(string name, Material mat, float thickness)
        {
            GameObject arrowRoot = new GameObject(name);
            
            // Shaft (Cylinder) - pivot at base, extends in +Y
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Shaft";
            shaft.transform.SetParent(arrowRoot.transform);
            shaft.transform.localPosition = new Vector3(0, 0.5f, 0);
            shaft.transform.localScale = new Vector3(thickness, 0.5f, thickness);
            Object.DestroyImmediate(shaft.GetComponent<Collider>());
            
            Renderer shaftRen = shaft.GetComponent<Renderer>();
            shaftRen.material = mat;
            shaftRen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shaftRen.receiveShadows = false;
            
            // Head (Cube as arrow tip)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            head.transform.SetParent(arrowRoot.transform);
            head.transform.localPosition = new Vector3(0, 1.0f, 0);
            head.transform.localScale = new Vector3(thickness * 2f, thickness * 2f, thickness * 2f);
            Object.DestroyImmediate(head.GetComponent<Collider>());
            
            Renderer headRen = head.GetComponent<Renderer>();
            headRen.material = mat;
            headRen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            headRen.receiveShadows = false;
            
            return arrowRoot;
        }
    }
}
