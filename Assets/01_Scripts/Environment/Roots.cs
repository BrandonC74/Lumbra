using UnityEngine;

// Raíces de oscuridad que atrapan un alma. Un destello cerca las quema y libera al alma.
// Algunas no guardan un alma sino una sombra disfrazada (sin sombra propia): al liberarla, te ataca.
public class Roots : MonoBehaviour, IFlashTarget
{
    [SerializeField] private Soul soul;
    [SerializeField] private GameObject fakeSoul;
    [SerializeField] private ShadowEnemy ambushPrefab;
    [SerializeField] private ParticleSystem burnParticles;

    // Si el alma ya se liberó (por ejemplo, al cargar la partida), las raíces sobran
    private void Update()
    {
        if (soul != null && soul.State != SoulState.Trapped)
        {
            Destroy(gameObject);
        }
    }

    public void OnFlash(Vector2 from)
    {
        Instantiate(burnParticles, transform.position, Quaternion.identity);
        if (soul != null)
        {
            soul.Free();
        }
        else
        {
            Destroy(fakeSoul);
            Instantiate(ambushPrefab, transform.position, Quaternion.identity);
            AudioManager.Instance.Play(AudioManager.Instance.scream);
            DialogueUI.Instance.Say("???", Lang.Get("fakeSoul"));
        }
        Destroy(gameObject);
    }
}
