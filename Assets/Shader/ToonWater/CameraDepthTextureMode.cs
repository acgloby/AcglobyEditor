using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class CameraDepthTextureMode : MonoBehaviour
{
    [SerializeField]
    private DepthTextureMode depthTextureMode;

    [SerializeField]
    private Texture depthTexture;

    private void OnValidate()
    {
        SetCameraDepthTextureMode();
    }

    private void Awake()
    {
        SetCameraDepthTextureMode();
    }

    private void Update()
    {
        depthTexture = Shader.GetGlobalTexture("_CameraDepthTexture");
    }

    private void SetCameraDepthTextureMode()
    {
        GetComponent<Camera>().depthTextureMode = depthTextureMode;
    }
}
