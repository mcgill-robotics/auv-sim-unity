using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Shared utility methods for creating 3D visualization elements (arrows, materials).
    /// Used by DVLPublisher, IMUPublisher, and Thrusters for debug visualization.
    /// </summary>
    public static class VisualizationUtils
    {
        public const int INDICATOR_LAYER = 22; // Suggested layer for Sim_Indicators

        /// <summary>
        /// Moves a GameObject and all its children to the X-Ray indicator layer.
        /// </summary>
        public static void SetXRayLayer(GameObject obj)
        {
            obj.layer = INDICATOR_LAYER;
            foreach (Transform child in obj.transform)
            {
                SetXRayLayer(child.gameObject);
            }
        }

        /// <summary>
        /// Sets the _BaseColor and _Color properties on a Renderer using a MaterialPropertyBlock.
        /// This is the best way to handle per-object colors when using material overrides (like X-Ray pass).
        /// </summary>
        public static void SetColorProperty(Renderer ren, Color color)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            ren.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_Color", color); // Fallback for standard shaders
            ren.SetPropertyBlock(mpb);
        }

        /// <summary>
        /// Creates an X-Ray ghost material based on the Hidden/XRayGhost shader.
        /// </summary>
        public static Material CreateXRayMaterial(Color color, float opacity = 0.5f)
        {
            Shader shader = Shader.Find("Hidden/XRayGhost");
            if (shader == null) return CreateMaterial(color); // Fallback to standard

            Material mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Opacity", opacity);
            return mat;
        }

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
        /// Creates a simple 3D dot (sphere) that uses the X-Ray ghost material.
        /// The dot is automatically moved to the INDICATOR_LAYER.
        /// </summary>
        /// <param name="name">Name for the dot GameObject</param>
        /// <param name="parent">Parent transform</param>
        /// <param name="color">Color for the dot</param>
        /// <param name="size">Scale of the sphere</param>
        /// <returns>Dot GameObject</returns>
        public static GameObject CreateSensorDot(string name, Transform parent, Color color, float size = 0.05f)
        {
            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = name;
            dot.transform.SetParent(parent);
            dot.transform.localPosition = Vector3.zero;
            dot.transform.localRotation = Quaternion.identity;
            dot.transform.localScale = new Vector3(size, size, size);
            
            Object.DestroyImmediate(dot.GetComponent<Collider>());
            
            Material mat = CreateMaterial(color); // Use standard material for solid presence
            Renderer ren = dot.GetComponent<Renderer>();
            ren.material = mat;
            ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ren.receiveShadows = false;
            
            SetColorProperty(ren, color); // Apply to MPB for X-Ray color override
            
            SetXRayLayer(dot);
            
            return dot;
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
            
            // Extract color from material and apply to MPB for X-Ray override
            Color col = Color.white;
            if (mat.HasProperty("_BaseColor")) col = mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_Color")) col = mat.GetColor("_Color");
            SetColorProperty(shaftRen, col);
            
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
            SetColorProperty(headRen, col);
            
            return arrowRoot;
        }
    }
}
