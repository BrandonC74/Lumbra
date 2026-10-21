using UnityEngine;

// Pantalla negra que aparece y se va: al empezar, al morir y al reaparecer, para que no haya saltos bruscos.
public class ScreenFade : MonoBehaviour
{
    public static ScreenFade Instance { get; private set; }

    [SerializeField] private CanvasGroup black;
    [SerializeField] private float speed = 3f;

    private float target;

    private void Awake()
    {
        Instance = this;
        black.alpha = 1f; // el nivel empieza en negro y se aclara
    }

    private void Update()
    {
        black.alpha = Mathf.MoveTowards(black.alpha, target, speed * Time.unscaledDeltaTime);
    }

    public void FadeOut()
    {
        target = 1f;
    }

    public void FadeIn()
    {
        target = 0f;
    }
}
