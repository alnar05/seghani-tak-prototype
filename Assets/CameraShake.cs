using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Range(0f, 1f)] public float intensity;
    public float maxAngle = 2.5f;
    public float maxOffset = 0.04f;
    public float frequency = 14f;

    float kick;

    public void Kick(float amount) => kick = Mathf.Max(kick, amount);

    void LateUpdate()
    {
        kick = Mathf.MoveTowards(kick, 0f, Time.deltaTime * 2.5f);
        float a = Mathf.Clamp01(intensity + kick);

        float t = Time.time * frequency;
        float nx = Mathf.PerlinNoise(t, 0.1f) - 0.5f;
        float ny = Mathf.PerlinNoise(t, 10.3f) - 0.5f;
        float nz = Mathf.PerlinNoise(t, 20.7f) - 0.5f;

        transform.localRotation = Quaternion.Euler(nx * 2f * maxAngle * a,
                                                   ny * 2f * maxAngle * a,
                                                   nz * 3f * maxAngle * a);
        transform.localPosition = new Vector3(ny, nx, 0f) * 2f * maxOffset * a;
    }
}
