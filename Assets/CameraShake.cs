using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Range(0f, 1f)] public float intensity;
    public float maxAngle = .8f;
    public float maxOffset = 0.02f;
    public float frequency = 10f;

    float kick;

    public void Kick(float amount) => kick = Mathf.Max(kick, amount);

    public void ResetShake()
    {
        intensity = kick = 0f;
        transform.localRotation = Quaternion.identity;
        transform.localPosition = Vector3.zero;
    }

    public void ApplySettings()
    {
        if (GameAudio.Shake <= 0f)
        {
            transform.localRotation = Quaternion.identity;
            transform.localPosition = Vector3.zero;
        }
    }

    void LateUpdate()
    {
        kick = Mathf.MoveTowards(kick, 0f, Time.deltaTime * 2.5f);
        float a = Mathf.Clamp01(intensity + kick) * GameAudio.Shake;

        float t = Time.time * frequency;
        float nx = Mathf.PerlinNoise(t, 0.1f) - 0.5f;
        float ny = Mathf.PerlinNoise(t, 10.3f) - 0.5f;
        float nz = Mathf.PerlinNoise(t, 20.7f) - 0.5f;

        transform.localRotation = Quaternion.Euler(nx * 2f * maxAngle * a,
                                                   ny * 2f * maxAngle * a,
                                                   nz * 1.5f * maxAngle * a);
        // Low-frequency earthquake sway belongs to the cosmetic camera, not player locomotion.
        float sway = Mathf.PerlinNoise(Time.time * 3f, .5f) - .5f;
        transform.localPosition = (new Vector3(ny, nx, 0f) * 2f * maxOffset +
            Vector3.right * sway * .035f) * a;
    }
}
