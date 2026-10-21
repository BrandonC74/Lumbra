using UnityEngine;
using UnityEngine.UI;

// Pone en un texto (de la interfaz o del mundo) la traducción de su clave, y la cambia si cambias de idioma.
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string key;

    private void OnEnable()
    {
        Lang.OnChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        Lang.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        if (TryGetComponent(out Text ui)) ui.text = Lang.Get(key);
        else if (TryGetComponent(out TextMesh world)) world.text = Lang.Get(key);
    }
}
