using UnityEngine;
using UnityEngine.Rendering.Universal;

// Antorcha del castillo: su fuego parpadea y recarga tu luz si te quedas cerca.
// Cuando el Devorador se enfurece, apaga algunas y la arena se oscurece.
public class Torch : MonoBehaviour
{
    [SerializeField] private float range = 2.5f;
    [SerializeField] private float rechargePerSecond = 25f;
    [SerializeField] private Light2D flame;
    [SerializeField] private ParticleSystem fire;

    private float baseIntensity;

    public bool IsLit { get; private set; } = true;

    private void Start()
    {
        baseIntensity = flame.intensity;
    }

    private void Update()
    {
        if (!IsLit)
        {
            return;
        }

        flame.intensity = baseIntensity * (0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 6f, transform.position.x));

        if (PlayerController.Instance.IsNear(transform.position, range))
        {
            LightFlash.Instance.Recharge(rechargePerSecond * Time.deltaTime);
        }
    }

    public void Extinguish()
    {
        IsLit = false;
        flame.enabled = false;
        fire.Stop();
    }
}
