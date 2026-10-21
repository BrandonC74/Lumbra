using UnityEngine;

// La oscuridad de la última zona: despierta cuando pasas startX y avanza hasta la arena del jefe.
// Mirar atrás o usar el destello la acelera. Si te alcanza, pierdes un corazón y vuelves al farol.
public class DarknessChaser : MonoBehaviour
{
    public static DarknessChaser Instance { get; private set; }

    [SerializeField] private float startX = 140f;
    [SerializeField] private float stopX = 230f;
    [SerializeField] private float speed = 3.5f;
    [SerializeField] private float lookBackBoost = 1.8f;
    [SerializeField] private float restartGap = 12f;

    private bool awake;
    private float provokedTimer;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        Transform player = PlayerController.Instance.transform;
        if (!awake)
        {
            if (player.position.x > startX)
            {
                awake = true;
                ResetBehind(player.position.x);
                CameraFollow.Instance.Shake(0.6f, 0.25f);
            }
            return;
        }

        if (GameManager.Instance.IsOver || PlayerHealth.Instance.IsDead)
        {
            return;
        }

        provokedTimer -= Time.deltaTime;
        float currentSpeed = speed;
        if (PlayerController.Instance.FacingBack) currentSpeed *= lookBackBoost;
        if (provokedTimer > 0f) currentSpeed *= 1.8f;

        Vector3 pos = transform.position;
        pos.x = Mathf.Min(pos.x + currentSpeed * Time.deltaTime, stopX); // no entra a la arena del Devorador
        pos.y = player.position.y;
        transform.position = pos;

        if (player.position.x < pos.x)
        {
            PlayerHealth.Instance.Die();
        }
    }

    public void Provoke()
    {
        provokedTimer = 1.5f;
    }

    public void ResetBehind(float x)
    {
        if (awake)
        {
            transform.position = new Vector3(x - restartGap, transform.position.y, 0f);
        }
    }
}
