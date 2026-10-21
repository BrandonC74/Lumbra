using UnityEngine;
using UnityEngine.UI;

// HUD: corazones, energía de la luz, almas rescatadas, poderes, tiempo, aviso de guardado, el tutorial del inicio,
// la barra del Devorador con la flecha al alma robada, y la pantalla final (estadísticas y galería de finales).
public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private Image[] hearts;
    [SerializeField] private Image energyFill;
    [SerializeField] private Image[] soulSlots;
    [SerializeField] private Text powersText;
    [SerializeField] private Text timeText;
    [SerializeField] private Text savedText;
    [SerializeField] private CanvasGroup tutorial;

    [Header("Jefe")]
    [SerializeField] private CanvasGroup bossBar;
    [SerializeField] private Image bossFill;
    [SerializeField] private RectTransform stolenArrow;

    [Header("Final")]
    [SerializeField] private CanvasGroup endPanel;
    [SerializeField] private Text endTitle;
    [SerializeField] private Text endStory;
    [SerializeField] private Text endStats;
    [SerializeField] private Text[] endingSlots;
    [SerializeField] private Text endPrompt;

    private static readonly Color[] EndingColors = { new Color(0.55f, 0.95f, 1f), new Color(0.75f, 0.4f, 1f), new Color(1f, 0.85f, 0.4f) };

    private float savedTimer;

    private void Awake()
    {
        Instance = this;
        endPanel.alpha = 0f;
    }

    private void Update()
    {
        // Si te sigue una corrupta, a ratos el HUD miente: corazones llenos y un alma de más
        bool lying = SoulManager.Instance.Corrupted > 0 && Mathf.PerlinNoise(Time.time * 0.8f, 0f) > 0.72f;
        int health = lying ? PlayerHealth.Instance.MaxHealth : PlayerHealth.Instance.Health;
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].color = i < health ? new Color(1f, 0.85f, 0.45f) : new Color(1f, 1f, 1f, 0.12f);
        }
        energyFill.fillAmount = LightFlash.Instance.Percent;
        timeText.text = GameManager.Format(GameManager.Instance.Timer);

        powersText.text = SoulManager.Instance.PowersText();
        int rescued = SoulManager.Instance.Rescued + (lying ? 1 : 0);
        for (int i = 0; i < soulSlots.Length; i++)
        {
            soulSlots[i].color = i < rescued ? new Color(0.6f, 0.95f, 1f) : new Color(1f, 1f, 1f, 0.12f);
        }

        // El tutorial se va solo después de unos segundos; el aviso de guardado también
        bool showTutorial = GameManager.Instance.Timer < 8f && !GameManager.Instance.IsOver;
        tutorial.alpha = Mathf.MoveTowards(tutorial.alpha, showTutorial ? 1f : 0f, Time.deltaTime * 2f);
        savedTimer -= Time.deltaTime;
        savedText.color = new Color(1f, 0.85f, 0.5f, Mathf.Clamp01(savedTimer));

        UpdateBoss();

        if (GameManager.Instance.IsOver)
        {
            endPanel.alpha = Mathf.MoveTowards(endPanel.alpha, 1f, Time.deltaTime * 0.8f);
            endPrompt.color = new Color(1f, 1f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.time * 3f));
        }
    }

    // Barra de vida del Devorador (cada brasero encendido le quita un tercio) y una flecha al alma que robó
    private void UpdateBoss()
    {
        Boss boss = Boss.Instance;
        bool fighting = boss != null && boss.IsFighting;
        bossBar.alpha = Mathf.MoveTowards(bossBar.alpha, fighting ? 1f : 0f, Time.deltaTime * 2f);
        if (fighting)
        {
            bossFill.fillAmount = Mathf.Lerp(bossFill.fillAmount, boss.Health, Time.deltaTime * 4f);
        }

        Soul stolen = fighting ? boss.StolenSoul : null;
        bool show = stolen != null && stolen.State == SoulState.Trapped;
        stolenArrow.gameObject.SetActive(show);
        if (show)
        {
            // Pegada al borde de la pantalla si el alma no se ve; encima de ella si se ve
            Vector3 view = Camera.main.WorldToViewportPoint(stolen.transform.position);
            Vector2 clamped = new Vector2(Mathf.Clamp(view.x, 0.06f, 0.94f), Mathf.Clamp(view.y, 0.1f, 0.88f));
            bool onScreen = clamped.x == view.x && clamped.y == view.y;
            if (onScreen) clamped.y += 0.08f;
            Vector2 dir = onScreen ? Vector2.down : ((Vector2)view - new Vector2(0.5f, 0.5f)).normalized;
            stolenArrow.position = new Vector2(clamped.x * Screen.width, clamped.y * Screen.height);
            stolenArrow.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        }
    }

    public void ShowSaved()
    {
        savedText.text = Lang.Get("saved");
        savedTimer = 2.5f;
    }

    public void ShowEnding(Ending ending, bool newRecord)
    {
        endTitle.text = Lang.Get("ending" + (int)ending).ToUpper();
        endTitle.color = EndingColors[(int)ending];
        endStory.text = Lang.Get("story" + (int)ending);
        endPrompt.text = Lang.Get("againWin");
        ShowStats(newRecord);
    }

    public void ShowGameOver()
    {
        endTitle.text = Lang.Get("gameOver");
        endTitle.color = new Color(1f, 0.3f, 0.4f);
        endStory.text = Lang.Get("gameOverStory");
        endPrompt.text = Lang.Get("againLose");
        ShowStats(false);
    }

    private void ShowStats(bool newRecord)
    {
        endStats.text = Lang.Get("time") + "  " + GameManager.Format(GameManager.Instance.Timer)
            + "      " + Lang.Get("soulsWith") + "  " + SoulManager.Instance.Rescued
            + "      " + Lang.Get("hits") + "  " + PlayerHealth.Instance.Hits
            + (newRecord ? "\n<color=#FFD966>" + Lang.Get("newRecord") + "</color>" : "");

        // Galería: los finales descubiertos muestran su nombre, los demás siguen ocultos
        for (int i = 0; i < endingSlots.Length; i++)
        {
            bool found = PlayerPrefs.GetInt("Ending" + i, 0) == 1;
            endingSlots[i].text = found ? Lang.Get("ending" + i) : "? ? ?";
            endingSlots[i].color = found ? EndingColors[i] : new Color(1f, 1f, 1f, 0.3f);
        }
    }
}
