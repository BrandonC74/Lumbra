using UnityEngine;
using UnityEngine.SceneManagement;

public enum Ending { Refuge, NewShadow, Truth }

// Partida: tiempo, guardado en los faroles, final según las almas que llegan contigo, galería y récord.
// Al terminar, R vuelve a jugar (tras perder, desde el último farol).
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool IsOver { get; private set; }
    public bool Won { get; private set; }
    public float Timer { get; private set; }
    public bool IsPlaying => !IsOver && Time.timeScale > 0f;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        // Si venimos de "Continuar", el jugador aparece en el último farol
        SaveData save = SaveSystem.Continue ? SaveSystem.Load() : null;
        if (save != null)
        {
            Vector2 pos = new Vector2(save.x, save.y);
            PlayerController.Instance.transform.position = pos;
            PlayerHealth.Instance.SetCheckpoint(pos);
            Timer = save.time;
        }
        ScreenFade.Instance.FadeIn();
    }

    private void Update()
    {
        SaveSystem.Continue = false; // ya se usó en los Start de este nivel

        if (!IsOver)
        {
            Timer += Time.deltaTime;
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            Reload(!Won);
        }
    }

    public void SaveAt(Vector2 checkpoint)
    {
        var data = new SaveData { x = checkpoint.x, y = checkpoint.y, time = Timer };
        SoulManager.Instance.Write(data);
        SaveSystem.Save(data);
        GameUI.Instance.ShowSaved();
    }

    // Vuelve a cargar el nivel: desde el último farol o desde el principio
    public static void Reload(bool fromSave)
    {
        SaveSystem.Continue = fromSave && SaveSystem.HasSave;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Win()
    {
        if (IsOver)
        {
            return;
        }

        IsOver = true;
        Won = true;
        SaveSystem.Delete(); // la historia terminó: la próxima partida empieza de cero
        SoulManager souls = SoulManager.Instance;
        bool tainted = souls.Corrupted > 0 || souls.Betrayed; // nunca descubriste a la mentirosa
        Ending ending = tainted ? Ending.NewShadow : souls.HasMemory ? Ending.Truth : Ending.Refuge;
        PlayerPrefs.SetInt("Ending" + (int)ending, 1);

        float best = PlayerPrefs.GetFloat("BestTime", 0f);
        bool newRecord = best == 0f || Timer < best;
        if (newRecord)
        {
            PlayerPrefs.SetFloat("BestTime", Timer);
        }

        AudioManager.Instance.Play(AudioManager.Instance.portal);
        DialogueUI.Instance.Clear();
        GameUI.Instance.ShowEnding(ending, newRecord);
    }

    // Sin corazones: la oscuridad te consume
    public void Lose()
    {
        if (IsOver)
        {
            return;
        }

        IsOver = true;
        DialogueUI.Instance.Clear();
        GameUI.Instance.ShowGameOver();
    }

    public static string Format(float seconds)
    {
        return Mathf.FloorToInt(seconds / 60f) + ":" + Mathf.FloorToInt(seconds % 60f).ToString("00");
    }
}
