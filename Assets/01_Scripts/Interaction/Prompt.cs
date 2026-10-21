using UnityEngine;

// Indicación flotante (por ejemplo "E") que aparece cuando el jugador se acerca.
// Si tiene un objetivo (unas raíces), desaparece cuando el objetivo ya no existe.
[RequireComponent(typeof(TextMesh))]
public class Prompt : MonoBehaviour
{
    [SerializeField] private float range = 4.5f;
    [SerializeField] private GameObject target;

    private TextMesh text;
    private Color color;
    private bool tracking;

    private void Start()
    {
        text = GetComponent<TextMesh>();
        color = text.color;
        tracking = target != null;
    }

    private void Update()
    {
        if (tracking && target == null)
        {
            Destroy(gameObject);
            return;
        }

        bool near = PlayerController.Instance.IsNear(transform.position, range);
        float goal = near ? 0.7f + 0.3f * Mathf.Sin(Time.time * 4f) : 0f;
        color.a = Mathf.MoveTowards(color.a, goal, Time.deltaTime * 3f);
        text.color = color;
    }
}
