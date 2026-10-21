using UnityEngine;

// Bufanda de luz que flota detrás del personaje: cada punto sigue al anterior, con un poco de viento.
[RequireComponent(typeof(LineRenderer))]
public class ScarfTrail : MonoBehaviour
{
    [SerializeField] private Transform anchor;
    [SerializeField] private int segments = 10;
    [SerializeField] private float segmentLength = 0.1f;
    [SerializeField] private float wind = 0.04f;

    private LineRenderer line;
    private Vector3[] points;

    private void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = segments;
        points = new Vector3[segments];
        for (int i = 0; i < segments; i++)
        {
            points[i] = anchor.position;
        }
    }

    private void LateUpdate()
    {
        points[0] = anchor.position;
        for (int i = 1; i < segments; i++)
        {
            Vector3 dir = points[i] - points[i - 1];
            dir += new Vector3(0f, Mathf.Sin(Time.time * 6f + i * 0.7f) * wind - 0.01f, 0f);
            points[i] = points[i - 1] + dir.normalized * segmentLength;
        }
        line.SetPositions(points);
    }
}
