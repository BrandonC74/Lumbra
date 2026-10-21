using UnityEngine;

public enum SoulState { Trapped, Following, Placed, Released }
public enum SoulPower { None, DoubleJump, BigFlash, Shield }

// Alma que te sigue cuando la liberas y te presta un poder. Algunas están corruptas: su sombra cae hacia ARRIBA,
// el destello (o mirar atrás) muestra su verdadera forma y su poder falla cuando más lo necesitas.
// También se pueden dejar en un brasero para encenderlo. El alma del recuerdo se libera al tocarla.
public class Soul : MonoBehaviour, IFlashTarget
{
    [Header("Variables")]
    [SerializeField] private float followSpeed = 4f;
    [SerializeField] private bool isMemory;
    [SerializeField] private SoulPower power;
    [SerializeField] private DialogueLine[] freedLines;

    [Header("Referencias")]
    [SerializeField] private GameObject glow;
    [SerializeField] private GameObject trueForm;
    [SerializeField] private Transform castShadow;
    [SerializeField] private AudioSource moan;
    [SerializeField] private AudioClip pleaSpanish;
    [SerializeField] private AudioClip pleaEnglish;

    private float exposedTimer;
    private float guideTimer;
    private Vector3 guidePoint;
    private float bobOffset;
    private float freedAt;
    private bool fleeing;

    public SoulState State { get; private set; }
    public bool IsCorrupt { get; private set; }
    public bool IsMemory => isMemory;
    public SoulPower Power => power;
    public bool IsExposed => exposedTimer > 0f;

    private void Start()
    {
        bobOffset = Random.value * 10f;
        moan.clip = Lang.Current == Language.Es ? pleaSpanish : pleaEnglish; // "Ayúdame..." en el idioma elegido
        moan.Play();
    }

    public void SetCorrupt(bool corrupt)
    {
        IsCorrupt = corrupt;
        castShadow.localPosition = new Vector3(0f, corrupt ? 0.7f : -0.7f, 0f);
    }

    private void Update()
    {
        exposedTimer -= Time.deltaTime;
        guideTimer -= Time.deltaTime;

        // Si miras atrás, a veces se te escapa un vistazo de lo que realmente es
        bool glimpse = IsCorrupt && State == SoulState.Following && PlayerController.Instance.FacingBack && Random.value < 0.08f;
        trueForm.SetActive(State == SoulState.Released || IsExposed || glimpse);
        glow.SetActive(!trueForm.activeSelf);

        // Las almas atrapadas piden ayuda bajito: se oyen más cuando te acercas, así las encuentras por el sonido
        float distance = Vector2.Distance(PlayerController.Instance.transform.position, transform.position);
        moan.volume = State == SoulState.Trapped ? Mathf.Clamp01(1f - distance / 8f) * 0.5f : 0f;

        Vector3 bob = Vector3.up * Mathf.Sin(Time.time * 3f + bobOffset) * 0.15f;
        switch (State)
        {
            case SoulState.Following:
                Vector3 goal = guideTimer > 0f ? guidePoint : SoulManager.Instance.SlotFor(this);
                transform.position = Vector3.Lerp(transform.position, goal + bob, followSpeed * Time.deltaTime);
                break;
            case SoulState.Trapped when fleeing:
            case SoulState.Placed:
                transform.position = Vector3.Lerp(transform.position, guidePoint + bob, followSpeed * Time.deltaTime);
                break;
            case SoulState.Released:
                transform.position += Vector3.up * 3f * Time.deltaTime;
                if (transform.position.y > 40f)
                {
                    Destroy(gameObject);
                }
                break;
        }
    }

    // silent: al cargar una partida guardada vuelve a seguirte sin sonido ni diálogo
    public void Free(bool silent = false)
    {
        if (State != SoulState.Trapped)
        {
            return;
        }

        State = SoulState.Following;
        freedAt = Time.time;
        SoulManager.Instance.Add(this);
        if (!silent)
        {
            AudioManager.Instance.Play(AudioManager.Instance.soul);
            DialogueUI.Instance.Play(freedLines);
        }
    }

    // El alma vuela a un punto unos segundos para señalar un camino
    public void Guide(Vector3 point, float seconds)
    {
        guidePoint = point;
        guideTimer = seconds;
    }

    // Se queda flotando en un brasero
    public void Place(Vector3 point)
    {
        State = SoulState.Placed;
        guidePoint = point;
    }

    // El Devorador la arranca del brasero y la lanza a un lugar alto: hay que ir a tocarla para recuperarla
    public void Flee(Vector3 point)
    {
        State = SoulState.Trapped;
        fleeing = true;
        guidePoint = point;
    }

    // Un vistazo corto de su forma real (cuando su poder falla)
    public void Glimpse()
    {
        exposedTimer = 0.6f;
    }

    public void Release()
    {
        State = SoulState.Released;
        trueForm.SetActive(true);
        AudioManager.Instance.Play(AudioManager.Instance.whisper);
    }

    public void OnFlash(Vector2 from)
    {
        // El mismo destello que la libera no cuenta: hay que volver a mirarla
        if (State == SoulState.Following && IsCorrupt && Time.time - freedAt > 0.5f)
        {
            exposedTimer = 3f;
            DialogueUI.Instance.Say("", Lang.Get("exposed"));
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((isMemory || fleeing) && other.TryGetComponent(out PlayerController player))
        {
            Free(fleeing);
            fleeing = false;
        }
    }
}
