using UnityEngine;

// Opciones que se guardan entre partidas (por ahora, el volumen). Las usan el menú principal y la pausa.
public static class Options
{
    public static float Volume
    {
        get => PlayerPrefs.GetFloat("Volume", 0.8f);
        set
        {
            PlayerPrefs.SetFloat("Volume", value);
            AudioListener.volume = value;
        }
    }

    public static void Apply()
    {
        AudioListener.volume = Volume;
    }
}
