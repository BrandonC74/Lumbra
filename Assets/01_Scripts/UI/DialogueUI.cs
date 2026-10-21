using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Globo de diálogo corto encima del personaje: se escribe letra por letra y avanza solo (ENTER pasa a la siguiente línea).
// El juego no se pausa: puedes seguir moviéndote mientras te hablan.
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [SerializeField] private CanvasGroup box;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text bodyText;

    private readonly Queue<DialogueLine> queue = new Queue<DialogueLine>();
    private bool skip;

    private void Awake()
    {
        Instance = this;
        box.alpha = 0f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            skip = true;
        }

        // Sigue al personaje por la pantalla, un poco encima de su cabeza
        // (sin salirse de la pantalla por los costados ni por arriba)
        Vector3 head = Camera.main.WorldToScreenPoint(PlayerController.Instance.transform.position + Vector3.up * 1.6f);
        RectTransform rect = (RectTransform)box.transform;
        float halfWidth = rect.rect.width * rect.lossyScale.x / 2f + 10f;
        float height = rect.rect.height * rect.lossyScale.y + 10f;
        head.x = Mathf.Clamp(head.x, halfWidth, Screen.width - halfWidth);
        head.y = Mathf.Min(head.y, Screen.height - height);
        box.transform.position = head;
    }

    public void Say(string speaker, string text)
    {
        Play(new[] { new DialogueLine(speaker, text) });
    }

    // Lo nuevo reemplaza a lo anterior: siempre se habla de lo que está pasando ahora
    public void Play(DialogueLine[] lines)
    {
        Clear();
        foreach (DialogueLine line in lines)
        {
            queue.Enqueue(line);
        }
        StartCoroutine(PlayQueue());
    }

    public void Clear()
    {
        StopAllCoroutines();
        queue.Clear();
        box.alpha = 0f;
    }

    private IEnumerator PlayQueue()
    {
        box.alpha = 1f;
        while (queue.Count > 0)
        {
            DialogueLine line = queue.Dequeue();
            bool hint = line.speaker == "";
            speakerText.text = Lang.Speaker(line.speaker);
            string text = Lang.Pick(line.text, line.textEn);
            bodyText.color = hint ? new Color(0.7f, 0.85f, 1f) : Color.white;
            bodyText.fontStyle = hint ? FontStyle.Italic : FontStyle.Normal;

            skip = false;
            for (int i = 1; i <= text.Length && !skip; i++)
            {
                bodyText.text = text.Substring(0, i);
                yield return new WaitForSeconds(0.022f);
            }
            bodyText.text = text;

            skip = false;
            float wait = 2.5f + text.Length * 0.06f;
            while (wait > 0f && !skip)
            {
                wait -= Time.deltaTime;
                yield return null;
            }
        }
        box.alpha = 0f;
    }
}
