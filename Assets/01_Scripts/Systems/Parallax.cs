using UnityEngine;

// Mueve una capa del fondo a otra velocidad que la cámara para dar profundidad, y la repite a lo ancho.
// factor 1 = va pegada a la cámara (muy lejos), 0 = quieta como el mundo, negativo = delante del jugador.
public class Parallax : MonoBehaviour
{
    [SerializeField, Range(-1f, 1f)] private float factor = 0.5f;
    [SerializeField] private float width = 40f;

    private Transform cam;
    private float startX;
    private float startY;

    private void Start()
    {
        cam = Camera.main.transform;
        startX = transform.position.x;
        startY = transform.position.y;
    }

    private void LateUpdate()
    {
        // También sigue a la cámara en altura, para que el fondo no se pierda al subir
        transform.position = new Vector3(startX + cam.position.x * factor, startY + cam.position.y * factor, transform.position.z);

        // Cuando la cámara se aleja una imagen entera, la capa salta para no acabarse nunca
        float distance = cam.position.x * (1f - factor);
        if (distance > startX + width)
        {
            startX += width;
        }
        else if (distance < startX - width)
        {
            startX -= width;
        }
    }
}
