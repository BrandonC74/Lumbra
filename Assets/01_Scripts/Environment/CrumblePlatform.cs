using System.Collections;
using UnityEngine;

// Plataforma que se desmorona al pisarla y vuelve a aparecer después de un rato.
public class CrumblePlatform : MonoBehaviour
{
    [SerializeField] private float delay = 0.5f;
    [SerializeField] private float returnTime = 3f;
    [SerializeField] private SpriteRenderer sprite;

    private Collider2D solid;
    private bool falling;

    private void Start()
    {
        solid = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!falling && other.gameObject.TryGetComponent(out PlayerController player))
        {
            StartCoroutine(Crumble());
        }
    }

    private IEnumerator Crumble()
    {
        falling = true;
        Vector3 start = sprite.transform.localPosition;
        float timer = 0f;
        while (timer < delay)
        {
            timer += Time.deltaTime;
            sprite.transform.localPosition = start + (Vector3)Random.insideUnitCircle * 0.05f; // tiembla antes de caer
            yield return null;
        }

        sprite.transform.localPosition = start;
        solid.enabled = false;
        sprite.gameObject.SetActive(false);
        yield return new WaitForSeconds(returnTime);

        solid.enabled = true;
        sprite.gameObject.SetActive(true);
        falling = false;
    }
}
