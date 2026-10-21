using System.Collections;
using UnityEngine;

// Vida: 3 corazones de luz. Las sombras y las espinas quitan uno y te empujan (luego parpadeas un momento).
// Caer al vacío o que te alcance la oscuridad también quita uno y te devuelve al último farol.
// Sin corazones, la oscuridad te consume. Con el poder de un alma, un escudo aguanta un golpe (se recarga).
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [Header("Variables")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float invulnerableTime = 1.2f;
    [SerializeField] private Vector2 knockback = new Vector2(6f, 8f);
    [SerializeField] private float fallLimit = -10f;
    [SerializeField] private float shieldRecharge = 8f;

    [Header("Referencias")]
    [SerializeField] private GameObject body;
    [SerializeField] private Transform visual;
    [SerializeField] private ParticleSystem deathParticles;
    [SerializeField] private ShieldFx shield;

    private Rigidbody2D rb;
    private Vector2 checkpoint;
    private float invulnerableTimer;
    private float shieldTimer;
    private SpriteRenderer[] parts;

    public int Health { get; private set; }
    public int MaxHealth => maxHealth;
    public int Hits { get; private set; }
    public bool IsDead { get; private set; }
    public float ShieldCharge => 1f - Mathf.Clamp01(shieldTimer / shieldRecharge);

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        checkpoint = transform.position;
        Health = maxHealth;
        parts = visual.GetComponentsInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        invulnerableTimer -= Time.deltaTime;
        shieldTimer -= Time.deltaTime;
        bool visible = invulnerableTimer <= 0f || Time.time * 15f % 2f < 1f; // parpadea mientras es invulnerable
        foreach (SpriteRenderer part in parts)
        {
            part.enabled = visible;
        }

        if (transform.position.y < fallLimit)
        {
            Die();
        }
    }

    public void SetCheckpoint(Vector2 pos)
    {
        checkpoint = pos;
    }

    public void Heal()
    {
        Health = maxHealth;
    }

    // Golpe de una sombra o de espinas
    public void Hurt(Vector2 from)
    {
        if (IsDead || invulnerableTimer > 0f || GameManager.Instance.IsOver)
        {
            return;
        }

        float side = transform.position.x >= from.x ? 1f : -1f;
        if (shieldTimer <= 0f && SoulManager.Instance.HasPower(SoulPower.Shield))
        {
            shieldTimer = shieldRecharge;
            shield.Break();
            if (SoulManager.Instance.TryPower(SoulPower.Shield))
            {
                invulnerableTimer = invulnerableTime;
                PlayerController.Instance.Knock(new Vector2(side * knockback.x, knockback.y), 0.25f);
                return; // el escudo absorbió el golpe
            }
        }

        LoseHeart();
        if (!IsDead)
        {
            invulnerableTimer = invulnerableTime;
            PlayerController.Instance.Knock(new Vector2(side * knockback.x, knockback.y), 0.25f);
        }
    }

    // Caída al vacío u oscuridad: pierdes un corazón y vuelves al farol
    public void Die()
    {
        if (IsDead || GameManager.Instance.IsOver)
        {
            return;
        }

        LoseHeart();
        if (!IsDead)
        {
            StartCoroutine(Respawn());
        }
    }

    private void LoseHeart()
    {
        Health--;
        Hits++;
        AudioManager.Instance.Play(AudioManager.Instance.hurt);
        CameraFollow.Instance.Shake(0.25f, 0.25f);
        if (Health <= 0)
        {
            StartCoroutine(Consumed());
        }
    }

    private IEnumerator Respawn()
    {
        IsDead = true;
        Hide(true);
        yield return new WaitForSeconds(0.3f);
        ScreenFade.Instance.FadeOut();
        yield return new WaitForSeconds(0.5f);

        transform.position = checkpoint;
        rb.linearVelocity = Vector2.zero;
        Hide(false);
        LightFlash.Instance.Refill();
        if (DarknessChaser.Instance != null)
        {
            DarknessChaser.Instance.ResetBehind(checkpoint.x);
        }
        invulnerableTimer = invulnerableTime;
        IsDead = false;
        ScreenFade.Instance.FadeIn();
    }

    private IEnumerator Consumed()
    {
        IsDead = true;
        AudioManager.Instance.Play(AudioManager.Instance.death);
        Hide(true);
        yield return new WaitForSeconds(1.2f);
        GameManager.Instance.Lose();
    }

    private void Hide(bool hidden)
    {
        if (hidden)
        {
            Instantiate(deathParticles, transform.position, Quaternion.identity);
        }
        body.SetActive(!hidden);
        rb.simulated = !hidden;
    }
}
