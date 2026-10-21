using UnityEngine;
using UnityEngine.Rendering.Universal;

// Destello de luz (E): revela plataformas ocultas, quema raíces y sombras, y muestra la verdadera forma
// de las almas corruptas. Gasta energía (se recupera sola) y despierta más a la oscuridad.
public class LightFlash : MonoBehaviour
{
    public static LightFlash Instance { get; private set; }

    [Header("Energía")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float flashCost = 35f;
    [SerializeField] private float regenPerSecond = 9f;

    [Header("Destello")]
    [SerializeField] private float radius = 3.5f;

    [Header("Referencias")]
    [SerializeField] private Light2D aura;
    [SerializeField] private ParticleSystem flashParticles;

    private float energy;
    private float flashTimer;
    private float auraRadius;

    public float Percent => energy / maxEnergy;

    private void Awake()
    {
        Instance = this;
        energy = maxEnergy;
        auraRadius = aura.pointLightOuterRadius;
    }

    private void Update()
    {
        SoulManager souls = SoulManager.Instance;
        energy = Mathf.Min(maxEnergy, energy + regenPerSecond * souls.LightMultiplier * Time.deltaTime);

        // Con poca energía tu luz se achica, cada alma pura la agranda y con el destello crece un instante
        flashTimer -= Time.deltaTime;
        float burst = flashTimer > 0f ? flashTimer * 10f : 0f;
        float radius = Mathf.Lerp(auraRadius * 0.5f, auraRadius, Percent) + souls.Pure * 0.5f + burst;

        // Si una corrupta te sigue, tu luz tiembla de vez en cuando (algo no está bien)
        if (souls.Corrupted > 0 && Random.value < 0.01f)
        {
            radius *= 0.4f;
        }
        aura.pointLightOuterRadius = radius;

        bool pressed = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.K);
        if (pressed && GameManager.Instance.IsPlaying && !PlayerHealth.Instance.IsDead)
        {
            Flash();
        }
    }

    private void Flash()
    {
        if (energy < flashCost)
        {
            AudioManager.Instance.Play(AudioManager.Instance.empty, 0.6f);
            return;
        }

        energy -= flashCost;
        flashTimer = 0.4f;
        flashParticles.Play();
        AudioManager.Instance.Play(AudioManager.Instance.flash);
        CameraFollow.Instance.Shake(0.15f, 0.12f);

        // Con el poder de un alma el destello llega mucho más lejos
        float reach = SoulManager.Instance.TryPower(SoulPower.BigFlash) ? radius * 1.4f : radius;
        flashParticles.transform.localScale = Vector3.one * reach / radius;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, reach))
        {
            if (hit.TryGetComponent(out IFlashTarget target))
            {
                target.OnFlash(transform.position);
            }
        }

        if (DarknessChaser.Instance != null)
        {
            DarknessChaser.Instance.Provoke();
        }
    }

    public void Recharge(float amount)
    {
        energy = Mathf.Min(maxEnergy, energy + amount);
    }

    public void Refill()
    {
        energy = maxEnergy;
    }
}
