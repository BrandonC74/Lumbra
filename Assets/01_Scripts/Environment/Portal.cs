using UnityEngine;

// La salida del santuario. Aparece cuando cae el Devorador; al cruzarla termina la partida.
public class Portal : MonoBehaviour
{
    [SerializeField] private Transform swirl;

    private void Update()
    {
        swirl.Rotate(0f, 0f, 60f * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out PlayerController player))
        {
            GameManager.Instance.Win();
        }
    }
}
