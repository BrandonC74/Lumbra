using UnityEditor;
using UnityEditor.SceneManagement;

// Al darle Play en el editor, el juego siempre empieza por el menú principal (como en el juego exportado),
// sin importar qué escena tengas abierta. Al salir del Play vuelves a la escena en la que estabas.
[InitializeOnLoad]
public static class PlayFromMenu
{
    static PlayFromMenu()
    {
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/00_Scenes/Menu.unity");
    }
}
