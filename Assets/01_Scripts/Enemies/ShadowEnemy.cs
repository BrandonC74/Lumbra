using UnityEngine;

public enum ShadowType { Wanderer, Ambusher }

// Sombra voladora. La errante te persigue si te acercas. La acechadora espera arriba y se lanza en picada:
// medio segundo antes sus ojos se agrandan como aviso. El destello las destruye.
// Si te tocan pierdes un corazón. Las falsas se ven igual, pero solo son humo.
public class ShadowEnemy : MonoBehaviour, IFlashTarget
{
    [Header("Variables")]
    [SerializeField] private ShadowType type;
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private float detectRange = 7f;
    [SerializeField] private float diveSpeed = 11f;
    [SerializeField] private bool isFake;

    [Header("Referencias")]
    [SerializeField] private Transform body;
    [SerializeField] private Transform eyes;
    [SerializeField] private ParticleSystem deathParticles;

    private Vector3 home;
    private Vector3 diveTarget;
    private float warnTimer;
    private bool diving;
    private float wobbleOffset;

    private void Start()
    {
        home = transform.position;
        wobbleOffset = Random.value * 10f;
    }

    private void Update()
    {
        Vector3 player = PlayerController.Instance.transform.position;
        bool playerAlive = !PlayerHealth.Instance.IsDead;

        switch (type)
        {
            case ShadowType.Wanderer:
                bool chase = playerAlive && Vector2.Distance(player, home) < detectRange;
                MoveTo(chase ? player : home, speed);
                break;
            case ShadowType.Ambusher:
                Ambush(player, playerAlive);
                break;
        }

        Vector2 look = (player - transform.position).normalized;
        eyes.localPosition = look * 0.08f;
        float wave = Mathf.Sin(Time.time * 5f + wobbleOffset) * 0.07f;
        body.localScale = new Vector3(1f + wave, 1f - wave, 1f);
    }

    private void Ambush(Vector3 player, bool playerAlive)
    {
        if (diving)
        {
            MoveTo(diveTarget, diveSpeed);
            diving = Vector2.Distance(transform.position, diveTarget) > 0.1f;
            return;
        }

        bool below = playerAlive && Mathf.Abs(player.x - home.x) < 3f && player.y < home.y;
        warnTimer = below ? warnTimer + Time.deltaTime : 0f;
        eyes.localScale = Vector3.one * (warnTimer > 0f ? 1.7f : 1f);
        MoveTo(home + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.3f, speed);

        if (warnTimer > 0.5f)
        {
            diving = true;
            warnTimer = 0f;
            eyes.localScale = Vector3.one;
            diveTarget = player + (player - transform.position).normalized * 2f;
        }
    }

    private void MoveTo(Vector3 target, float moveSpeed)
    {
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
    }

    public void OnFlash(Vector2 from)
    {
        Die();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PlayerHealth player))
        {
            return;
        }

        if (isFake)
        {
            Die();
            return;
        }
        player.Hurt(transform.position);
    }

    private void Die()
    {
        Instantiate(deathParticles, transform.position, Quaternion.identity);
        AudioManager.Instance.Play(AudioManager.Instance.splat, 0.6f);
        Destroy(gameObject);
    }
}
