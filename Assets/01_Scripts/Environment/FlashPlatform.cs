using UnityEngine;

// Plataforma que reacciona a tu luz. Las ocultas aparecen unos segundos con el destello;
// las de sombra hacen lo contrario: existen en la oscuridad y el destello las deshace. Parpadean antes de cambiar.
public class FlashPlatform : MonoBehaviour, IFlashTarget
{
    [SerializeField] private bool appearsWithLight = true;
    [SerializeField] private float duration = 5f;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Collider2D solid;

    private float timer;
    private int groundLayer;

    private void Start()
    {
        groundLayer = LayerMask.NameToLayer("Ground");
        Show(!appearsWithLight);
    }

    private void Update()
    {
        if (timer <= 0f)
        {
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Show(!appearsWithLight);
        }
        else if (timer < 0.8f)
        {
            SetAlpha(Mathf.PingPong(Time.time * 8f, 1f)); // aviso: está por cambiar
        }
    }

    public void OnFlash(Vector2 from)
    {
        timer = duration;
        Show(appearsWithLight);
    }

    private void Show(bool on)
    {
        solid.enabled = on;
        gameObject.layer = on ? groundLayer : 0;
        SetAlpha(on ? 1f : appearsWithLight ? 0.15f : 0.08f); // las ocultas se insinúan un poco más
    }

    private void SetAlpha(float alpha)
    {
        Color color = sprite.color;
        color.a = alpha;
        sprite.color = color;
    }
}
