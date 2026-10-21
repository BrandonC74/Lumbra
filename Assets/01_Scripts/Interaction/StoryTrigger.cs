using UnityEngine;

// Lanza un diálogo cuando el jugador pasa por aquí (una sola vez), con su voz si la tiene.
public class StoryTrigger : MonoBehaviour
{
    [SerializeField] private DialogueLine[] lines;
    [SerializeField] private AudioClip voiceSpanish;
    [SerializeField] private AudioClip voiceEnglish;

    private bool done;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (done || !other.TryGetComponent(out PlayerController player))
        {
            return;
        }

        done = true;
        DialogueUI.Instance.Play(lines);
        VoiceLine.Speak(voiceSpanish, voiceEnglish);
    }
}
