using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Con un solo corazón la vista se cierra y se tiñe de rojo, latiendo.
public class PanicEffects : MonoBehaviour
{
    [SerializeField] private Volume volume;

    private Vignette vignette;

    private void Start()
    {
        volume.profile.TryGet(out vignette);
    }

    private void Update()
    {
        bool lastHeart = PlayerHealth.Instance.Health == 1;
        float pulse = lastHeart ? 0.1f * Mathf.Abs(Mathf.Sin(Time.time * 4f)) : 0f;
        vignette.intensity.value = Mathf.MoveTowards(vignette.intensity.value, (lastHeart ? 0.6f : 0.45f) + pulse, Time.deltaTime);
        vignette.color.value = Color.Lerp(vignette.color.value, lastHeart ? new Color(0.35f, 0f, 0.05f) : new Color(0.02f, 0f, 0.05f), Time.deltaTime * 2f);
    }
}
