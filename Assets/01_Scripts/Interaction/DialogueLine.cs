using UnityEngine;

// Una línea de diálogo: quién habla y qué dice (en español e inglés). Sin nombre = texto de ayuda.
[System.Serializable]
public struct DialogueLine
{
    public string speaker;
    [TextArea] public string text;
    [TextArea] public string textEn;

    public DialogueLine(string speaker, string text, string textEn = "")
    {
        this.speaker = speaker;
        this.text = text;
        this.textEn = textEn;
    }
}
