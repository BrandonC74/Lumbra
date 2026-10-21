using UnityEngine;

// Lo que se guarda al encender un farol: dónde estás, cuánto tiempo llevas y qué pasó con las almas
[System.Serializable]
public class SaveData
{
    public float x;
    public float y;
    public float time;
    public int corrupt;
    public int[] freed;
    public int[] released;
}

// Guarda y carga la partida (en PlayerPrefs, como JSON)
public static class SaveSystem
{
    private const string Key = "Save";

    // El menú lo activa cuando eliges "Continuar"
    public static bool Continue;

    public static bool HasSave => PlayerPrefs.HasKey(Key);

    public static void Save(SaveData data)
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static SaveData Load()
    {
        return HasSave ? JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key)) : null;
    }

    public static void Delete()
    {
        PlayerPrefs.DeleteKey(Key);
    }
}
