using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Range(0f, 1f)] public float intensity;
    public float maxAngle = .6f;
    public float maxOffset = .014f;
    public float frequency = 7.5f;

    Vector3 restPosition;
    Quaternion restRotation;
    float kick;

    void Awake()
    {
        restPosition = transform.localPosition;
        restRotation = transform.localRotation;
    }

    public void Kick(float amount) => kick = Mathf.Max(kick, amount);

    public void ResetShake()
    {
        intensity = kick = 0f;
        RestoreCamera();
    }

    public void ApplySettings()
    {
        if (GameAudio.Shake <= 0f) RestoreCamera();
    }

    void LateUpdate()
    {
        kick = Mathf.MoveTowards(kick, 0f, Time.deltaTime * 2.5f);
        float amount = Mathf.Clamp01(intensity + kick) * GameAudio.Shake;
        if (amount <= 0f)
        {
            RestoreCamera();
            return;
        }

        float t = Time.time * frequency;
        float x = Mathf.PerlinNoise(t, .1f) - .5f;
        float y = Mathf.PerlinNoise(t, 10.3f) - .5f;
        float z = Mathf.PerlinNoise(t, 20.7f) - .5f;
        var tilt = Quaternion.Euler(x * 2f * maxAngle * amount, y * 2f * maxAngle * amount,
            z * 1.5f * maxAngle * amount);
        transform.localRotation = restRotation * tilt;

        float sway = Mathf.PerlinNoise(Time.time * 3f, .5f) - .5f;
        var offset = new Vector3(y, x, 0f) * 2f * maxOffset + Vector3.right * sway * .012f;
        transform.localPosition = restPosition + offset * amount;
    }

    void RestoreCamera()
    {
        transform.localRotation = restRotation;
        transform.localPosition = restPosition;
    }
}
