using UnityEngine;

// Bifurcación: al azar, uno de los dos caminos tiene espinas escondidas. Al llegar, el alma más nueva
// vuela a una entrada: las puras señalan el camino seguro y las corruptas, la trampa.
public class PathFork : MonoBehaviour
{
    [SerializeField] private Transform[] entrances;
    [SerializeField] private GameObject[] traps;

    private int trapped;
    private bool used;

    private void Start()
    {
        trapped = Random.Range(0, traps.Length);
        for (int i = 0; i < traps.Length; i++)
        {
            traps[i].SetActive(i == trapped);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Soul guide = SoulManager.Instance.Newest;
        if (used || guide == null || !other.TryGetComponent(out PlayerController player))
        {
            return;
        }

        used = true;
        int pointTo = guide.IsCorrupt ? trapped : 1 - trapped;
        guide.Guide(entrances[pointTo].position, 5f);
        DialogueUI.Instance.Say("Alma", Lang.Get("forkGuide"));
    }
}
