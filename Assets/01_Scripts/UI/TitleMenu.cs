using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pantalla de título: Continuar (si hay partida guardada), Nueva partida, Opciones (volumen e idioma) y Salir.
// Al empezar muestra una pantalla de carga con un consejo. De fondo, Lumbra respira (con su Animator) y las almas flotan.
public class TitleMenu : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private CanvasGroup loadingPanel;
    [SerializeField] private Image loadingBar;
    [SerializeField] private Text tipText;

    [Header("Botones")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button spanishButton;
    [SerializeField] private Button englishButton;
    [SerializeField] private Slider volumeSlider;

    [Header("Fondo animado")]
    [SerializeField] private Transform[] souls;
    [SerializeField] private AudioSource click;

    private Vector3[] soulHomes;

    private void Start()
    {
        Time.timeScale = 1f;
        Options.Apply();
        volumeSlider.value = Options.Volume;
        volumeSlider.onValueChanged.AddListener(volume => Options.Volume = volume);

        continueButton.gameObject.SetActive(SaveSystem.HasSave);
        continueButton.onClick.AddListener(() => StartGame(true));
        newGameButton.onClick.AddListener(() => StartGame(false));
        optionsButton.onClick.AddListener(() => ShowOptions(true));
        backButton.onClick.AddListener(() => ShowOptions(false));
        quitButton.onClick.AddListener(Application.Quit);
        // En el navegador no se puede cerrar el juego, así que el botón Salir no aparece
        quitButton.gameObject.SetActive(Application.platform != RuntimePlatform.WebGLPlayer);
        spanishButton.onClick.AddListener(() => Lang.Set(Language.Es));
        englishButton.onClick.AddListener(() => Lang.Set(Language.En));

        optionsPanel.SetActive(false);
        loadingPanel.alpha = 0f;
        soulHomes = new Vector3[souls.Length];
        for (int i = 0; i < souls.Length; i++)
        {
            soulHomes[i] = souls[i].position;
        }
    }

    private void Update()
    {
        for (int i = 0; i < souls.Length; i++)
        {
            souls[i].position = soulHomes[i] + new Vector3(Mathf.Sin(Time.time * 0.7f + i), Mathf.Sin(Time.time * 1.3f + i * 2f), 0f) * 0.3f;
        }
    }

    private void ShowOptions(bool show)
    {
        click.Play();
        optionsPanel.SetActive(show);
        mainPanel.SetActive(!show);
    }

    private void StartGame(bool continueSave)
    {
        click.Play();
        if (!continueSave)
        {
            SaveSystem.Delete();
        }
        SaveSystem.Continue = continueSave;
        StartCoroutine(Load());
    }

    // Pantalla de carga: barra de progreso y un consejo (se muestra al menos un momento para poder leerlo)
    private IEnumerator Load()
    {
        mainPanel.SetActive(false);
        loadingPanel.alpha = 1f;
        tipText.text = Lang.Get("tip" + Random.Range(0, 4));

        AsyncOperation load = SceneManager.LoadSceneAsync(1);
        load.allowSceneActivation = false;
        float shown = 0f;
        while (load.progress < 0.9f || shown < 1.5f)
        {
            shown += Time.deltaTime;
            loadingBar.fillAmount = Mathf.Min(load.progress / 0.9f, shown / 1.5f);
            yield return null;
        }
        load.allowSceneActivation = true;
    }
}
