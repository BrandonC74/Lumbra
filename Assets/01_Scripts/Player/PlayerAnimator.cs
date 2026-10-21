using UnityEngine;

// Elige qué animación del Animator se reproduce según lo que hace el jugador: quieto, correr, saltar, caer,
// pared o agotado. Además estira el cuerpo al saltar, lo aplasta al aterrizar, levanta polvo y con poca luz baja su brillo.
public class PlayerAnimator : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visual;
    [SerializeField] private ParticleSystem dust;

    private PlayerController player;
    private SpriteRenderer[] parts;
    private Color[] baseColors;
    private string current;
    private bool wasGrounded = true;
    private Vector3 squash = Vector3.one;
    private float glow = 1f;

    private void Start()
    {
        player = GetComponent<PlayerController>();
        parts = visual.GetComponentsInChildren<SpriteRenderer>();
        baseColors = new Color[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            baseColors[i] = parts[i].color;
        }
    }

    private void Update()
    {
        Vector2 velocity = player.Velocity;
        bool running = Mathf.Abs(velocity.x) > 0.5f;

        string state;
        if (player.OnWall) state = "Wall";
        else if (!player.IsGrounded) state = velocity.y > 0f ? "Jump" : "Fall";
        else if (running) state = player.IsTired ? "TiredRun" : "Run";
        else state = player.IsTired ? "TiredIdle" : "Idle";

        if (state != current)
        {
            animator.CrossFadeInFixedTime(state, 0.08f);
            current = state;
        }
        animator.speed = running && player.IsGrounded ? Mathf.Max(0.6f, Mathf.Abs(velocity.x) / 5.5f) : 1f;

        // Agotado, su brillo baja
        glow = Mathf.Lerp(glow, player.IsTired ? 0.72f : 1f, 3f * Time.deltaTime);
        for (int i = 0; i < parts.Length; i++)
        {
            Color c = baseColors[i] * glow;
            c.a = baseColors[i].a;
            parts[i].color = c;
        }

        // Al despegar se estira, al aterrizar se aplasta
        if (player.IsGrounded && !wasGrounded)
        {
            squash = new Vector3(1.2f, 0.82f, 1f);
            dust.Play();
        }
        else if (!player.IsGrounded && wasGrounded && velocity.y > 0f)
        {
            squash = new Vector3(0.85f, 1.15f, 1f);
            dust.Play();
        }
        wasGrounded = player.IsGrounded;

        squash = Vector3.Lerp(squash, Vector3.one, 12f * Time.deltaTime);
        visual.localScale = squash;
    }
}
