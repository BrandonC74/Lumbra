using System.Collections;
using UnityEngine;

// Zarpazo del Devorador: una marca roja avisa en el suelo y medio segundo después la garra sale de abajo.
public class BossClaw : MonoBehaviour
{
    [SerializeField] private float floorY = 9f;
    [SerializeField] private SpriteRenderer warning;

    public void Attack(float x)
    {
        StartCoroutine(Strike(x));
    }

    public void Hide()
    {
        StopAllCoroutines();
        warning.color = Color.clear;
        transform.position = new Vector3(transform.position.x, floorY - 5f, 0f);
    }

    private IEnumerator Strike(float x)
    {
        warning.transform.position = new Vector3(x, floorY + 0.1f, 0f);
        AudioManager.Instance.Play(AudioManager.Instance.snarl, 0.7f);
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            warning.color = new Color(1f, 0.15f, 0.2f, Mathf.PingPong(t * 6f, 0.8f));
            yield return null;
        }
        warning.color = Color.clear;

        Vector3 hidden = new Vector3(x, floorY - 4f, 0f);
        Vector3 up = new Vector3(x, floorY - 0.4f, 0f);
        yield return Move(hidden, up, 0.12f);
        yield return new WaitForSeconds(0.4f);
        yield return Move(up, hidden, 0.3f);
    }

    private IEnumerator Move(Vector3 from, Vector3 to, float seconds)
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
        {
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
    }
}
