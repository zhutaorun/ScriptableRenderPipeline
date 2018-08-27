using UnityEngine;

public class SetShaderLOD : MonoBehaviour
{
    public void Start()
    {
        Shader.globalMaximumLOD = 200;
    }
}
