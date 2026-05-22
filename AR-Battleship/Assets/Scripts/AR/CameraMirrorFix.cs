using UnityEngine;
 
public class CameraMirrorFix : MonoBehaviour
{
    private Camera cam;
    private bool isFlipped = false;
 
    void Start()
    {
        cam = GetComponent<Camera>();
    }
 
    void OnPreCull()
    {
        if (cam == null) return;
#if UNITY_EDITOR
        cam.ResetProjectionMatrix();
        Matrix4x4 mat = cam.projectionMatrix;
        mat *= Matrix4x4.Scale(new Vector3(-1, 1, 1));
        cam.projectionMatrix = mat;
        GL.invertCulling = true;
        isFlipped = true;
#endif
    }
 
    void OnPostRender()
    {
#if UNITY_EDITOR
        if (isFlipped)
        {
            GL.invertCulling = false;
            isFlipped = false;
        }
#endif
    }
}
