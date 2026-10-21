using UnityEngine;

// Cómo se ve el escudo de luz: una burbuja que late con runas girando mientras está lista.
// Al bloquear un golpe estalla en chispas, y una corona de puntitos se va encendiendo mientras se recarga.
public class ShieldFx : MonoBehaviour
{
    [SerializeField] private SpriteRenderer bubble;
    [SerializeField] private SpriteRenderer runes;
    [SerializeField] private SpriteRenderer[] rechargeDots;
    [SerializeField] private ParticleSystem shatter;

    private void Update()
    {
        bool has = SoulManager.Instance.HasPower(SoulPower.Shield) && !PlayerHealth.Instance.IsDead;
        float charge = PlayerHealth.Instance.ShieldCharge;
        bool ready = has && charge >= 1f;

        bubble.enabled = ready;
        runes.enabled = ready;
        if (ready)
        {
            float pulse = Mathf.Sin(Time.time * 3f);
            bubble.color = new Color(0.55f, 0.95f, 1f, 0.28f + 0.08f * pulse);
            bubble.transform.localScale = Vector3.one * (2.1f + 0.06f * pulse);
            runes.transform.Rotate(0f, 0f, 40f * Time.deltaTime);
        }

        for (int i = 0; i < rechargeDots.Length; i++)
        {
            rechargeDots[i].enabled = has && !ready && i < charge * rechargeDots.Length;
        }
    }

    public void Break()
    {
        shatter.Play();
        AudioManager.Instance.Play(AudioManager.Instance.shieldBreak);
    }
}
