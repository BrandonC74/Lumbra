using UnityEngine;

// Ojos que miran desde la oscuridad del fondo: parpadean y se esconden cuando te acercas con tu luz.
public class WatchingEyes : MonoBehaviour
{
    [SerializeField] private float hideDistance = 5f;

    private SpriteRenderer[] eyes;
    private float blinkTimer;
    private float alpha;

    private void Start()
    {
        eyes = GetComponentsInChildren<SpriteRenderer>();
        blinkTimer = Random.Range(1f, 4f);
    }

    private void Update()
    {
        bool near = PlayerController.Instance.IsNear(transform.position, hideDistance);
        alpha = Mathf.MoveTowards(alpha, near ? 0f : 0.9f, Time.deltaTime * 2f);

        blinkTimer -= Time.deltaTime;
        if (blinkTimer < 0f)
        {
            blinkTimer = Random.Range(2f, 5f);
        }
        bool closed = blinkTimer < 0.15f;

        foreach (SpriteRenderer eye in eyes)
        {
            Color c = eye.color;
            c.a = closed ? 0f : alpha;
            eye.color = c;
        }
    }
}
