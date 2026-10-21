using System.Collections;
using UnityEngine;

// El Devorador: la sombra gigante que custodia el portal... y la voz que te guiaba.
// Al entrar a la arena cierra la salida y ataca con zarpazos (BossClaw)
// y sombras pequeñas. Cada brasero encendido lo debilita y lo enfurece (apaga antorchas del castillo);
// con los tres, cae y aparece el portal.
public class Boss : MonoBehaviour
{
    public static Boss Instance { get; private set; }

    [Header("Variables")]
    [SerializeField] private float startX = 246f;
    [SerializeField] private float floorY = 9f;
    [SerializeField] private float timeBtwClaws = 3.2f;
    [SerializeField] private float timeBtwSpawns = 8f;
    [SerializeField] private int maxShadows = 3;
    [SerializeField] private float timeBtwDevours = 20f;

    [Header("Diálogos")]
    [SerializeField] private AudioClip voiceSpanish;
    [SerializeField] private AudioClip voiceEnglish;
    [SerializeField] private DialogueLine[] introLines;
    [SerializeField] private DialogueLine[] defeatLines;

    [Header("Referencias")]
    [SerializeField] private Brazier[] braziers;
    [SerializeField] private Torch[] torches;
    [SerializeField] private GameObject gate;
    [SerializeField] private GameObject portal;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Transform eyes;
    [SerializeField] private BossClaw claw;
    [SerializeField] private ShadowEnemy shadowPrefab;
    [SerializeField] private Transform spawned;
    [SerializeField] private Transform[] perches;
    [SerializeField] private AudioClip devourSpanish;
    [SerializeField] private AudioClip devourEnglish;

    private bool awake;
    private int lit;
    private float clawTimer;
    private float spawnTimer;
    private float devourTimer;

    public bool Defeated { get; private set; }
    public Soul StolenSoul { get; private set; }
    public float Health => 1f - (float)lit / braziers.Length;
    public bool IsFighting => awake && !Defeated;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (Defeated)
        {
            body.color = Color.Lerp(body.color, Color.clear, Time.deltaTime);
            return;
        }

        Transform player = PlayerController.Instance.transform;
        if (!awake)
        {
            if (player.position.x > startX)
            {
                Wake();
            }
            return;
        }

        eyes.localPosition = (Vector2)(player.position - eyes.parent.position).normalized * 0.04f;
        if (!GameManager.Instance.IsPlaying || PlayerHealth.Instance.IsDead)
        {
            return;
        }

        // Mientras más braseros encendidos, más rápido ataca
        float fury = 1f + lit * 0.4f;
        clawTimer += Time.deltaTime * fury;
        if (clawTimer >= timeBtwClaws)
        {
            clawTimer = 0f;
            claw.Attack(player.position.x);
        }

        // Mientras no estén los tres encendidos, cada tanto se roba un alma de un brasero
        devourTimer = lit > 0 ? devourTimer + Time.deltaTime : 0f;
        if (devourTimer >= timeBtwDevours)
        {
            devourTimer = 0f;
            Devour();
        }

        spawnTimer += Time.deltaTime * fury;
        if (spawnTimer >= timeBtwSpawns && spawned.childCount < maxShadows)
        {
            spawnTimer = 0f;
            float side = Random.value < 0.5f ? -9f : 9f;
            Instantiate(shadowPrefab, new Vector3(transform.position.x + side, floorY + 7f, 0f), Quaternion.identity, spawned);
        }
    }

    private void Wake()
    {
        awake = true;
        gate.SetActive(true);
        CameraFollow.Instance.Frame(new Vector2(transform.position.x, 15f), 9f);
        eyes.localScale = Vector3.one;
        AudioManager.Instance.Silence(3f);
        AudioManager.Instance.Play(AudioManager.Instance.gate);
        AudioManager.Instance.Play(AudioManager.Instance.roar);
        VoiceLine.Speak(voiceSpanish, voiceEnglish);
        CameraFollow.Instance.Shake(1f, 0.3f);
        DialogueUI.Instance.Play(introLines);
    }

    private void Devour()
    {
        foreach (Brazier brazier in braziers)
        {
            if (brazier.IsLit)
            {
                StolenSoul = brazier.Devour(perches[Random.Range(0, perches.Length)].position);
                AudioManager.Instance.Play(AudioManager.Instance.roar, 0.6f);
                VoiceLine.Speak(devourSpanish, devourEnglish);
                DialogueUI.Instance.Say("", Lang.Get("devour"));
                Refresh();
                return;
            }
        }
    }

    // Lo llaman los braseros al encenderse o apagarse
    public void Refresh()
    {
        int count = 0;
        foreach (Brazier brazier in braziers)
        {
            if (brazier.IsLit) count++;
        }

        if (count > lit)
        {
            AudioManager.Instance.Play(AudioManager.Instance.roar, 0.7f);
            CameraFollow.Instance.Shake(0.5f, 0.25f);
            StartCoroutine(HitFlash());
            PutOutTorches(2);
        }
        lit = count;

        if (lit == braziers.Length)
        {
            Defeat();
        }
    }

    // En su furia apaga algunas antorchas que sigan encendidas
    private void PutOutTorches(int count)
    {
        foreach (Torch torch in torches)
        {
            if (count > 0 && torch.IsLit && Random.value < 0.6f)
            {
                torch.Extinguish();
                count--;
            }
        }
    }

    private IEnumerator HitFlash()
    {
        body.color = new Color(0.6f, 0.5f, 0.9f);
        yield return new WaitForSeconds(0.15f);
        body.color = Color.black;
    }

    private void Defeat()
    {
        Defeated = true;
        StopAllCoroutines();
        claw.Hide();
        foreach (Transform shadow in spawned)
        {
            Destroy(shadow.gameObject);
        }
        gate.SetActive(false);
        portal.SetActive(true);
        CameraFollow.Instance.Release();
        AudioManager.Instance.Play(AudioManager.Instance.portal);
        CameraFollow.Instance.Shake(1.2f, 0.4f);
        DialogueUI.Instance.Play(defeatLines);
    }
}
