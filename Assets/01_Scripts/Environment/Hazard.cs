using UnityEngine;

// Todo lo que hiere al tocarlo: espinas y el zarpazo del Devorador.
// Las espinas de trampa (popUp) están escondidas en el suelo y brotan cuando te acercas.
public class Hazard : MonoBehaviour
{
    [SerializeField] private bool popUp;
    [SerializeField] private float triggerDistance = 2.5f;

    private float grow = 1f;

    private void Start()
    {
        if (popUp)
        {
            grow = 0f;
            transform.localScale = new Vector3(transform.localScale.x, 0f, 1f);
        }
    }

    private void Update()
    {
        float distance = Mathf.Abs(PlayerController.Instance.transform.position.x - transform.position.x);
        if (grow < 1f && distance < triggerDistance + transform.localScale.x / 2f)
        {
            grow = Mathf.MoveTowards(grow, 1f, Time.deltaTime * 6f);
            transform.localScale = new Vector3(transform.localScale.x, grow, 1f);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (grow > 0.5f && other.TryGetComponent(out PlayerHealth player))
        {
            player.Hurt(transform.position);
        }
    }
}
