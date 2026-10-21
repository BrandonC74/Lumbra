using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pausa (Esc): muestra tu partida (tiempo, corazones y almas) y deja continuar, volver al último farol,
// cambiar volumen e idioma o ir al menú principal.
public class PauseMenu : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Text statsText;

    [Header("Botones")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button spanishButton;
    [SerializeField] private Button englishButton;
    [SerializeField] private Slider volumeSlider;

    private void Start()
    {
        Options.Apply();
        volumeSlider.value = Options.Volume;
        volumeSlider.onValueChanged.AddListener(volume => Options.Volume = volume);

        resumeButton.onClick.AddListener(Toggle);
        restartButton.onClick.AddListener(() => GameManager.Reload(true));
        menuButton.onClick.AddListener(ToTitle);
        spanishButton.onClick.AddListener(() => Lang.Set(Language.Es));
        englishButton.onClick.AddListener(() => Lang.Set(Language.En));
        panel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !GameManager.Instance.IsOver)
        {
            Toggle();
        }

        if (panel.activeSelf)
        {
            statsText.text = Lang.Get("time") + "  " + GameManager.Format(GameManager.Instance.Timer)
                + "      ♥ " + PlayerHealth.Instance.Health
                + "      " + Lang.Get("souls") + "  " + SoulManager.Instance.Rescued;
        }
    }

    private void Toggle()
    {
        AudioManager.Instance.Play(AudioManager.Instance.click);
        bool paused = !panel.activeSelf;
        panel.SetActive(paused);
        Time.timeScale = paused ? 0f : 1f;
    }

    private void ToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
