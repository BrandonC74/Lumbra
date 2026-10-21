using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Brasero de la arena: acércate y pulsa F para dejar ahí un alma. Encendido debilita al Devorador.
// Si el alma era la corrupta, a los pocos segundos lo apaga y se convierte en una sombra.
// El Devorador también puede robar el alma de un brasero y lanzarla lejos.
public class Brazier : MonoBehaviour
{
    [SerializeField] private float range = 1.8f;
    [SerializeField] private Light2D fire;
    [SerializeField] private ParticleSystem flames;
    [SerializeField] private ShadowEnemy shadowPrefab;

    private Soul soul;

    public bool IsLit => soul != null;

    private void Start()
    {
        fire.enabled = false;
    }

    private void Update()
    {
        bool near = PlayerController.Instance.IsNear(transform.position, range);
        if (IsLit || !near || !GameManager.Instance.IsPlaying || !Input.GetKeyDown(KeyCode.F))
        {
            return;
        }

        Soul taken = SoulManager.Instance.TakeSoul();
        if (taken == null)
        {
            DialogueUI.Instance.Say("", Lang.Get("noSouls"));
            return;
        }
        Light(taken);
    }

    private void Light(Soul taken)
    {
        soul = taken;
        soul.Place(transform.position + Vector3.up);
        fire.enabled = true;
        flames.Play();
        AudioManager.Instance.Play(AudioManager.Instance.fire);
        Boss.Instance.Refresh();

        if (soul.IsCorrupt)
        {
            StartCoroutine(Betray());
        }
    }

    // El Devorador se traga la luz del brasero y lanza el alma a otro lugar
    public Soul Devour(Vector3 perch)
    {
        if (!IsLit || soul.IsCorrupt)
        {
            return null;
        }

        Soul stolen = soul;
        stolen.Flee(perch);
        soul = null;
        fire.enabled = false;
        flames.Stop();
        return stolen;
    }

    private IEnumerator Betray()
    {
        yield return new WaitForSeconds(3f);
        if (Boss.Instance.Defeated)
        {
            yield break;
        }

        DialogueUI.Instance.Say("Alma", Lang.Get("betray"));
        Instantiate(shadowPrefab, soul.transform.position, Quaternion.identity);
        Destroy(soul.gameObject);
        soul = null;
        fire.enabled = false;
        flames.Stop();
        AudioManager.Instance.Play(AudioManager.Instance.whisper);
        SoulManager.Instance.MarkBetrayed();
        Boss.Instance.Refresh();
    }
}
