using UnityEngine;

// "No mires atrás": si el jugador se da vuelta cerca de aquí, algo cambia y ya no vuelve a ser igual.
public class LookBackChange : MonoBehaviour
{
    [SerializeField] private GameObject before;
    [SerializeField] private GameObject after;
    [SerializeField] private float range = 9f;
    [SerializeField] private AudioClip voiceSpanish;
    [SerializeField] private AudioClip voiceEnglish;

    private float lookTimer;
    private bool changed;

    private void Update()
    {
        if (changed)
        {
            return;
        }

        PlayerController player = PlayerController.Instance;
        bool near = Mathf.Abs(player.transform.position.x - transform.position.x) < range;
        lookTimer = near && player.FacingBack ? lookTimer + Time.deltaTime : 0f;

        if (lookTimer > 0.3f)
        {
            changed = true;
            before.SetActive(false);
            after.SetActive(true);
            AudioManager.Instance.Play(AudioManager.Instance.whisper);
            VoiceLine.Speak(voiceSpanish, voiceEnglish);
        }
    }
}
