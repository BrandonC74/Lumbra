using UnityEngine;

// Una voz que susurra cuando te acercas (una sola vez), en el idioma elegido
public class VoiceLine : MonoBehaviour
{
    [SerializeField] private AudioClip spanish;
    [SerializeField] private AudioClip english;
    [SerializeField] private float range = 6f;

    private bool played;

    private void Update()
    {
        if (!played && PlayerController.Instance.IsNear(transform.position, range))
        {
            played = true;
            Speak(spanish, english);
        }
    }

    public static void Speak(AudioClip spanish, AudioClip english)
    {
        AudioClip clip = Lang.Current == Language.Es ? spanish : english;
        if (clip != null)
        {
            AudioManager.Instance.PlayVoice(clip);
        }
    }
}
