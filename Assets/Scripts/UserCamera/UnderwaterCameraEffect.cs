using UnityEngine;

[RequireComponent(typeof(Camera))]
public class UnderwaterCameraEffect : MonoBehaviour
{
    [Header("Water Detection")]
    [Tooltip("Layer mask for water surfaces to detect submersion")]
    public LayerMask waterLayers;
    
    [Tooltip("Shader for underwater depth effect rendering")]
    public Shader shader;

    [Space(10)]
    [Header("Underwater Depth Effect")]
    [Tooltip("Color tint applied at maximum depth")]
    public Color depthColor = new Color(0, 0.42f, 0.87f);
    
    [Tooltip("Distance (m) where depth effect starts")]
    [Range(-50f, 0f)]
    public float depthStart = -12f;
    
    [Tooltip("Distance (m) where depth effect reaches maximum")]
    [Range(0f, 200f)]
    public float depthEnd = 98f;
    
    [Tooltip("Layer mask for objects affected by depth rendering")]
    public LayerMask depthLayers = ~0; // All layers selected by default.

	Camera cam, depthCam;
	RenderTexture depthTexture, colourTexture;
	Material material;
	bool inWater;


	// Start is called before the first frame update.
	void Start()
	{
		cam = GetComponent<Camera>();

		// Make our camera send depth information (i.e. how far a pizel is from the screen)
		// to the shader as well cam.depthTextureMode = DepthTextureMode.Depth;

		// Create a material using the assigned shader.
		if (shader) material = new Material(shader);

		// Create render textures for the camera to save the colour and depth information
		// prevent the camera from rendering onto the game scene.
		depthTexture = RenderTexture.GetTemporary(cam.pixelWidth, cam.pixelHeight, 16, RenderTextureFormat.Depth);
		colourTexture = RenderTexture.GetTemporary(cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.Default);

		// Create depthCam and parent it to main camera.
		GameObject go = new GameObject("Depth Cam");
		depthCam = go.AddComponent<Camera>();
		go.transform.SetParent(transform);
		go.transform.position = transform.position;

		// Copy over main camera settings, but with a different culling mask and depthtexturemode.depth.
		depthCam.CopyFrom(cam);
		depthCam.cullingMask = depthLayers;
		depthCam.depthTextureMode = DepthTextureMode.Depth;

		// Make depthCam use ColorTexture and depthTexture
		// and also disable depthCam so we can turn it on manually.
		depthCam.SetTargetBuffers(colourTexture.colorBuffer, depthTexture.depthBuffer);
		depthCam.clearFlags = CameraClearFlags.Skybox;
		depthCam.enabled = false;

		// Send the depth texture to the shader.
		material.SetTexture("_DepthMap", depthTexture);
	}

	private void OnApplicationQuit()
	{
		RenderTexture.ReleaseTemporary(depthTexture);
		RenderTexture.ReleaseTemporary(colourTexture);
	}

	private void FixedUpdate()
	{
		// Get the camera frustum of the near plane.
		Vector3[] corners = new Vector3[4];

		cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.nearClipPlane, cam.stereoActiveEye, corners);

		// Check where the water level is, without factoring in rolling as we cannot
		// check how far submerged we are into the water, using corner[0] and corner[1],
		// which are the bottom left and top left corners respectively.
		RaycastHit hit;
		Vector3 start = transform.position + transform.TransformVector(corners[1]), end = transform.position + transform.TransformVector(corners[0]);

		Collider[] c = Physics.OverlapSphere(end, 0.01f, waterLayers);
		if (c.Length > 0)
		{
			inWater = true;

			c = Physics.OverlapSphere(start, 0.01f, waterLayers);
			if (c.Length > 0)
			{
				material.SetVector("_WaterLevel", new Vector2(0, 1));
			}
			else
			{
				if (Physics.Linecast(start, end, out hit, waterLayers))
				{
					// Get the interpolation value (delta) of the point the linecast hit
					// the reverse of a lerp function gives us the delta.
					float delta = hit.distance / (end - start).magnitude;

					// Set the water level.
					// Use 1 - delta to get the reverse of the number (e.g. if delta is 0.25, the 
					// water level will be 0.75). This is because the linecast is done from above 
					// the water, and the delta is the percentage of screen that is not submerged.
					material.SetVector("_WaterLevel", new Vector2(0, 1 - delta));
				}
			}
		}
		else
		{
			inWater = false;
		}
	}

	// Automatically finds and assigned inspector variables so the script can be 
	// immediately used when attached to a gameObject.
	private void Reset()
	{
		//Look for the shader we created
		Shader[] shaders = Resources.FindObjectsOfTypeAll<Shader>();
		foreach (Shader s in shaders)
		{
			if (s.name.Contains(this.GetType().Name))
			{
				shader = s;
				return;
			}
		}
	}

	// This is where the image effect is applied.
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (material && inWater)
		{
			// Update the depth render texture.
			depthCam.Render();

			// We pass the information to our material.
			material.SetColor("_DepthColor", depthColor);
			material.SetFloat("_DepthStart", depthStart);
			material.SetFloat("_DepthEnd", depthEnd);

			// Apply to the image using blit.
			Graphics.Blit(source, destination, material);
		}
		else
		{
			Graphics.Blit(source, destination);
		}
	}
}