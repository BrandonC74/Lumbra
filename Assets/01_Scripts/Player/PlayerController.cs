using UnityEngine;

// Movimiento de plataformas: correr con aceleración, saltar (con margen al borde y salto anticipado)
// y saltar contra paredes. Al caer pesa más que al subir, así el salto se siente firme.
// Con el poder de un alma puedes saltar una vez más en el aire.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movimiento")]
    [SerializeField] private float speed = 5.5f;
    [SerializeField] private float groundAcceleration = 50f;
    [SerializeField] private float airAcceleration = 30f;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float gravity = 4f;
    [SerializeField] private float fallGravity = 6f;
    [SerializeField] private float shortJumpGravity = 9f;
    [SerializeField] private float maxFallSpeed = 16f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBuffer = 0.12f;

    [Header("Pared")]
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(6f, 14f);
    [SerializeField] private float wallJumpLock = 0.2f;

    [Header("Detección")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Vector2 feetSize = new Vector2(0.4f, 0.1f);
    [SerializeField] private Vector2 sideSize = new Vector2(0.1f, 0.7f);

    [Header("Referencias")]
    [SerializeField] private Transform body;

    private Rigidbody2D rb;
    private float input;
    private float coyoteTimer;
    private float bufferTimer;
    private float lockTimer;
    private int wallSide;
    private bool airJumpUsed;

    public bool IsGrounded { get; private set; }
    public bool OnWall => wallSide != 0 && rb.linearVelocity.y < 0f;
    public Vector2 Velocity => rb.linearVelocity;

    // Con poca luz o un solo corazón el personaje está agotado: camina más lento
    public bool IsTired => LightFlash.Instance.Percent < 0.3f || PlayerHealth.Instance.Health == 1;

    // El santuario avanza hacia la derecha: mirar a la izquierda es "mirar atrás"
    public bool FacingBack => body.localScale.x < 0f;

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            return; // en pausa o en el menú
        }
        input = GameManager.Instance.IsOver ? 0f : Input.GetAxisRaw("Horizontal");

        Vector2 pos = transform.position;
        IsGrounded = Physics2D.OverlapBox(pos + Vector2.down * 0.6f, feetSize, 0f, groundMask);
        wallSide = 0;
        if (!IsGrounded && Physics2D.OverlapBox(pos + Vector2.right * 0.28f, sideSize, 0f, groundMask)) wallSide = 1;
        else if (!IsGrounded && Physics2D.OverlapBox(pos + Vector2.left * 0.28f, sideSize, 0f, groundMask)) wallSide = -1;

        coyoteTimer = IsGrounded ? coyoteTime : coyoteTimer - Time.deltaTime;
        if (IsGrounded || wallSide != 0)
        {
            airJumpUsed = false;
        }
        bufferTimer = JumpPressed() ? jumpBuffer : bufferTimer - Time.deltaTime;
        lockTimer -= Time.deltaTime;

        if (bufferTimer > 0f && coyoteTimer > 0f)
        {
            Jump(new Vector2(rb.linearVelocity.x, jumpForce));
        }
        else if (bufferTimer > 0f && wallSide != 0)
        {
            Jump(new Vector2(-wallSide * wallJumpForce.x, wallJumpForce.y));
            lockTimer = wallJumpLock;
            body.localScale = new Vector3(-wallSide, 1f, 1f);
        }
        else if (bufferTimer > 0f && !airJumpUsed && SoulManager.Instance.HasPower(SoulPower.DoubleJump))
        {
            airJumpUsed = true;
            bufferTimer = 0f;
            if (SoulManager.Instance.TryPower(SoulPower.DoubleJump))
            {
                Jump(new Vector2(rb.linearVelocity.x, jumpForce * 0.9f));
            }
        }

        if (input != 0f && lockTimer <= 0f)
        {
            body.localScale = new Vector3(Mathf.Sign(input), 1f, 1f);
        }
    }

    private void FixedUpdate()
    {
        // Acelera hasta la velocidad en vez de llegar de golpe (en el aire se controla un poco menos)
        float acceleration = IsGrounded ? groundAcceleration : airAcceleration;
        float vx = lockTimer > 0f ? rb.linearVelocity.x : Mathf.MoveTowards(rb.linearVelocity.x, input * speed * (IsTired ? 0.8f : 1f), acceleration * Time.fixedDeltaTime);

        // Soltar ESPACIO mientras subes hace un salto más corto
        bool holdingJump = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        float vy = rb.linearVelocity.y;
        rb.gravityScale = vy < 0f ? fallGravity : holdingJump ? gravity : shortJumpGravity;

        if (wallSide != 0 && input == wallSide && vy < -wallSlideSpeed)
        {
            vy = -wallSlideSpeed; // se desliza despacio pegado a la pared
        }
        rb.linearVelocity = new Vector2(vx, Mathf.Max(vy, -maxFallSpeed));
    }

    // ¿El jugador está a menos de "range" de ese punto? (lo usan antorchas, braseros, indicaciones...)
    public bool IsNear(Vector2 point, float range)
    {
        return Vector2.Distance(transform.position, point) < range;
    }

    // Empujón (al recibir un golpe): por un momento no controlas el movimiento
    public void Knock(Vector2 velocity, float lockTime)
    {
        rb.linearVelocity = velocity;
        lockTimer = lockTime;
    }

    private bool JumpPressed()
    {
        return !GameManager.Instance.IsOver && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow));
    }

    private void Jump(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
        bufferTimer = 0f;
        coyoteTimer = 0f;
        AudioManager.Instance.Play(AudioManager.Instance.jump, 0.4f);
    }
}
