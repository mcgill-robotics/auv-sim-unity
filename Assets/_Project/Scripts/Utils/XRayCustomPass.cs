using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Utils
{
    /// <summary>
    /// Custom HDRP Pass for rendering X-Ray/Ghosting effects.
    /// It re-renders objects on the 'Sim_Indicators' layer with a specific ghost material
    /// when they are occluded by other geometry (ZTest Greater).
    /// </summary>
    public class XRayCustomPass : CustomPass
    {
        public LayerMask targetLayer;
        public Material ghostMaterial;

        protected override void Execute(CustomPassContext ctx)
        {
            if (ghostMaterial == null)
            {
                // 1. Try to load from Resources
                ghostMaterial = Resources.Load<Material>("XRayGhostMaterial");

                // 2. Fallback: try to find the shader and create an engine material
                if (ghostMaterial == null)
                {
                    Shader xRayShader = Shader.Find("Hidden/XRayGhost");
                    if (xRayShader != null)
                    {
                        ghostMaterial = CoreUtils.CreateEngineMaterial(xRayShader);
                    }
                    else
                    {
                        Debug.LogWarning("[XRayCustomPass] XRayGhost material or shader could not be found! Skipping pass.");
                        return;
                    }
                }
            }

            // Draw Ghost part (Occluded)
            // This pass overrides with our XRay material (which has ZTest Greater).
            // Explicitly override depth state to CompareFunction.Greater and depth write false.
            var stateBlock = new RenderStateBlock(RenderStateMask.Depth)
            {
                depthState = new DepthState(false, CompareFunction.Greater)
            };
            CustomPassUtils.DrawRenderers(ctx, targetLayer, RenderQueueType.All, ghostMaterial, 0, stateBlock, SortingCriteria.None);
        }

        protected override void Cleanup()
        {
            // CoreUtils.Destroy(ghostMaterial) is not needed if it's assigned from the inspector,
            // but if we created it at runtime, we should be careful.
        }
    }
}
