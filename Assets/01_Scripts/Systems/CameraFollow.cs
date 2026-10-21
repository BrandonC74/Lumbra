using UnityEngine;

// La cámara sigue al jugador mirando un poco hacia adelante y puede temblar.
// En la pelea con el jefe se aleja y se centra en la arena para ver los tres braseros.
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [SerializeField] private Transform target;
    [SerializeField] private float smooth = 4f;
    [SerializeField] private Vector2 offset = new Vector2(3f, 1.5f);

    private Camera cam;
    private float normalSize;
    private float size;
    private Vector2 arenaCenter;
    private bool framing;
    private float shakeTime;
    private float shakeStrength;

    private void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        normalSize = size = cam.orthographicSize;
    }

    private void LateUpdate()
    {
        Vector3 goal = new Vector3(target.position.x + offset.x, target.position.y + offset.y, -10f);
        if (framing)
        {
            goal = Vector3.Lerp(goal, new Vector3(arenaCenter.x, arenaCenter.y, -10f), 0.6f);
        }
        Vector3 pos = Vector3.Lerp(transform.position, goal, smooth * Time.deltaTime);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, size, 2f * Time.deltaTime);

        if (shakeTime > 0f)
        {
            shakeTime -= Time.deltaTime;
            pos += (Vector3)Random.insideUnitCircle * shakeStrength;
        }

        transform.position = pos;
    }

    public void Frame(Vector2 center, float zoom)
    {
        framing = true;
        arenaCenter = center;
        size = zoom;
    }

    public void Release()
    {
        framing = false;
        size = normalSize;
    }

    public void Shake(float duration, float strength)
    {
        shakeTime = duration;
        shakeStrength = strength;
    }
}
