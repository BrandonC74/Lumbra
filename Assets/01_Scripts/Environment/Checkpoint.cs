using UnityEngine;
using UnityEngine.Rendering.Universal;

// Punto de control: un farol que se enciende al pasar, recupera tus corazones y guarda la partida.
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Light2D lamp;

    private bool active;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (active || !other.TryGetComponent(out PlayerHealth player))
        {
            return;
        }

        active = true;
        player.SetCheckpoint(transform.position + Vector3.up);
        player.Heal();
        GameManager.Instance.SaveAt(transform.position + Vector3.up);
        lamp.intensity = 1.5f;
        AudioManager.Instance.Play(AudioManager.Instance.checkpoint, 0.6f);
    }
}
