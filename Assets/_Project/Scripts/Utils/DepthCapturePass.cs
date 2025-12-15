using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

class DepthCapturePass : CustomPass
{
    public Material linearDepthMaterial;
    public RenderTexture outputRenderTexture;

    RTHandle m_OutputHandle;

    protected override void Execute(CustomPassContext ctx)
    {
      if (linearDepthMaterial == null || outputRenderTexture == null) return;

      // Wrap the external Render Texture in an RTHandle (required by HDRP)
      if (m_OutputHandle == null || m_OutputHandle.rt != outputRenderTexture)
      {
          if (m_OutputHandle != null) RTHandles.Release(m_OutputHandle);
          m_OutputHandle = RTHandles.Alloc(outputRenderTexture);
      }

      // Set Target to our RT and Draw
      CoreUtils.SetRenderTarget(ctx.cmd, m_OutputHandle, ClearFlag.Color);
      ctx.cmd.DrawProcedural(Matrix4x4.identity, linearDepthMaterial, 0, MeshTopology.Triangles, 3);
    }

    protected override void Cleanup()
    {
      if (m_OutputHandle != null) RTHandles.Release(m_OutputHandle);
    }
}