using UnityEngine;

// Sonidos: efectos sueltos, un zumbido grave de fondo y un latido cuando hay peligro
// (te persiguen, te queda un corazón o peleas con el Devorador). Antes de algo grave, todo se calla.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Efectos")]
    public AudioClip jump;
    public AudioClip flash;
    public AudioClip empty;
    public AudioClip death;
    public AudioClip hurt;
    public AudioClip splat;
    public AudioClip soul;
    public AudioClip whisper;
    public AudioClip checkpoint;
    public AudioClip portal;
    public AudioClip roar;
    public AudioClip fire;
    public AudioClip gate;
    public AudioClip click;
    public AudioClip[] steps;
    public AudioClip snarl;
    public AudioClip scream;
    public AudioClip shieldBreak;

    [Header("Bucles")]
    [SerializeField] private AudioClip heartbeatSlow;
    [SerializeField] private AudioClip heartbeatFast;
    [SerializeField] private AudioClip bossAmbience;
    [SerializeField] private AudioClip musicExplore;
    [SerializeField] private AudioClip musicBoss;

    [Header("Fuentes")]
    [SerializeField] private AudioSource effects;
    [SerializeField] private AudioSource ambience;
    [SerializeField] private AudioSource heartbeat;
    [SerializeField] private AudioSource panting;
    [SerializeField] private AudioSource voice;
    [SerializeField] private AudioSource music;

    private float silenceTimer;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        silenceTimer -= Time.deltaTime;
        ambience.volume = Mathf.MoveTowards(ambience.volume, silenceTimer > 0f ? 0f : 0.4f, Time.deltaTime);

        Transform player = PlayerController.Instance.transform;
        DarknessChaser chaser = DarknessChaser.Instance;
        bool chased = chaser != null && player.position.x - chaser.transform.position.x < 15f && player.position.x > chaser.transform.position.x;
        bool danger = chased || PlayerHealth.Instance.Health == 1 || (Boss.Instance != null && Boss.Instance.IsFighting);
        heartbeat.volume = Mathf.MoveTowards(heartbeat.volume, danger && silenceTimer <= 0f ? 1f : 0f, Time.deltaTime);
        // Con un corazón el latido se acelera; en la pelea el ambiente cambia a terror
        Swap(heartbeat, PlayerHealth.Instance.Health == 1 ? heartbeatFast : heartbeatSlow);
        bool bossFight = Boss.Instance != null && Boss.Instance.IsFighting;
        if (bossFight)
        {
            Swap(ambience, bossAmbience);
        }

        // Música de exploración y otra para el jefe; baja cuando alguien habla
        Swap(music, bossFight ? musicBoss : musicExplore);
        music.volume = Mathf.MoveTowards(music.volume, silenceTimer > 0f ? 0.06f : bossFight ? 0.45f : 0.3f, Time.deltaTime);
        panting.volume = Mathf.MoveTowards(panting.volume, PlayerController.Instance.IsTired ? 0.5f : 0f, Time.deltaTime);
    }

    public void Play(AudioClip clip, float volume = 1f)
    {
        effects.PlayOneShot(clip, volume);
    }

    // Cambia el sonido de un bucle sin cortarlo si ya es el mismo
    private void Swap(AudioSource source, AudioClip clip)
    {
        if (source.clip != clip)
        {
            source.clip = clip;
            source.Play();
        }
    }

    // Un paso al azar, con un tono un poco distinto cada vez para que no suene repetido
    public void PlayStep()
    {
        effects.pitch = Random.Range(0.9f, 1.1f);
        effects.PlayOneShot(steps[Random.Range(0, steps.Length)], 0.35f);
        effects.pitch = 1f;
    }

    // Las voces van por su propio canal y bajan el ambiente mientras hablan, para que se entiendan
    public void PlayVoice(AudioClip clip)
    {
        voice.clip = clip;
        voice.Play();
        Silence(clip.length * 0.8f);
    }

    // Silencio de golpe antes de algo importante
    public void Silence(float seconds)
    {
        silenceTimer = seconds;
        ambience.volume = 0f;
        heartbeat.volume = 0f;
    }
}
