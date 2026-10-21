using System.Collections.Generic;
using UnityEngine;

// Lleva la fila de almas que te siguen. Al empezar decide al azar cuál está corrupta, así cada partida es distinta.
// Las almas te prestan luz y poderes, y se pueden dejar en los braseros. Con Q sueltas a un alma descubierta.
public class SoulManager : MonoBehaviour
{
    public static SoulManager Instance { get; private set; }

    [SerializeField] private Soul[] souls;
    [SerializeField] private float spacing = 0.9f;
    [SerializeField, Range(0f, 1f)] private float fakePowerFail = 0.4f;

    private readonly List<Soul> followers = new List<Soul>();
    private readonly List<int> released = new List<int>();
    private int corruptIndex;

    public Soul Newest => followers.Count > 0 ? followers[followers.Count - 1] : null;
    public bool Betrayed { get; private set; }
    public int Rescued { get; private set; }
    public int Corrupted { get; private set; }
    public bool HasMemory { get; private set; }
    public int Pure => Rescued + (HasMemory ? 1 : 0) - Corrupted;

    // Cada alma pura te presta luz (recargas más rápido); una corrupta te la roba en silencio
    public float LightMultiplier => Mathf.Max(0.3f, 1f + 0.35f * Pure - 0.6f * Corrupted);

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Una de las almas con poder es la corrupta (al azar, o la misma de la partida guardada)
        SaveData save = SaveSystem.Continue ? SaveSystem.Load() : null;
        var candidates = new List<int>();
        for (int i = 0; i < souls.Length; i++)
        {
            if (souls[i].Power != SoulPower.None) candidates.Add(i);
        }
        corruptIndex = save != null ? save.corrupt : candidates[Random.Range(0, candidates.Count)];
        for (int i = 0; i < souls.Length; i++)
        {
            souls[i].SetCorrupt(i == corruptIndex);
        }

        if (save != null)
        {
            Load(save);
        }
    }

    // ---------- Guardado ----------
    public void Write(SaveData data)
    {
        data.corrupt = corruptIndex;
        data.released = released.ToArray();
        data.freed = new int[followers.Count];
        for (int i = 0; i < followers.Count; i++)
        {
            data.freed[i] = System.Array.IndexOf(souls, followers[i]);
        }
    }

    private void Load(SaveData data)
    {
        foreach (int i in data.freed)
        {
            souls[i].Free(true);
        }
        foreach (int i in data.released)
        {
            released.Add(i);
            Destroy(souls[i].gameObject);
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Q) || !GameManager.Instance.IsPlaying)
        {
            return;
        }

        foreach (Soul soul in followers)
        {
            if (soul.IsExposed)
            {
                followers.Remove(soul);
                Recount();
                released.Add(System.Array.IndexOf(souls, soul));
                soul.Release();
                DialogueUI.Instance.Say("", Lang.Get("released"));
                return;
            }
        }
    }

    public void Add(Soul soul)
    {
        followers.Add(soul);
        Recount();
    }

    // Saca de la fila a la última alma (nunca la del recuerdo) para dejarla en un brasero
    public Soul TakeSoul()
    {
        for (int i = followers.Count - 1; i >= 0; i--)
        {
            if (!followers[i].IsMemory)
            {
                Soul soul = followers[i];
                followers.RemoveAt(i);
                Recount();
                return soul;
            }
        }
        return null;
    }

    public void MarkBetrayed()
    {
        Betrayed = true;
    }

    public bool HasPower(SoulPower power)
    {
        return Owner(power) != null;
    }

    // El poder de una corrupta a veces falla: se le escapa su forma real y se oye un susurro
    public bool TryPower(SoulPower power)
    {
        Soul owner = Owner(power);
        if (owner == null)
        {
            return false;
        }

        if (owner.IsCorrupt && Random.value < fakePowerFail)
        {
            owner.Glimpse();
            AudioManager.Instance.Play(AudioManager.Instance.whisper);
            return false;
        }
        return true;
    }

    // Detrás del jugador, en fila y a alturas alternadas para que no se amontonen
    public Vector3 SlotFor(Soul soul)
    {
        int i = followers.IndexOf(soul);
        float behind = PlayerController.Instance.FacingBack ? 1f : -1f;
        return PlayerController.Instance.transform.position + new Vector3(behind * (1f + i * spacing), 1f + (i % 2) * 0.5f, 0f);
    }


    public string PowersText()
    {
        string text = "";
        if (HasPower(SoulPower.DoubleJump)) text += Lang.Get("doubleJump") + "   ";
        if (HasPower(SoulPower.BigFlash)) text += Lang.Get("bigFlash") + "   ";
        if (HasPower(SoulPower.Shield)) text += Lang.Get("shield");
        return text;
    }

    private Soul Owner(SoulPower power)
    {
        foreach (Soul soul in followers)
        {
            if (soul.Power == power) return soul;
        }
        return null;
    }

    // Vuelve a contar las almas cada vez que la fila cambia
    private void Recount()
    {
        Rescued = 0;
        Corrupted = 0;
        HasMemory = false;
        foreach (Soul soul in followers)
        {
            if (soul.IsMemory) HasMemory = true;
            else Rescued++;
            if (soul.IsCorrupt) Corrupted++;
        }
    }
}
