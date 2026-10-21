using UnityEditor;
using UnityEngine;

// Exporta el juego a la carpeta Builds (fuera de Assets): la versión de Windows y la que se juega en el navegador (itch.io).
public static class ExportarJuego
{
    private static readonly string[] Scenes = { "Assets/00_Scenes/Menu.unity", "Assets/00_Scenes/Santuario.unity" };

    [MenuItem("Lumbra/Exportar/Windows")]
    public static void Windows()
    {
        Export("Builds/Windows/Lumbra.exe", BuildTarget.StandaloneWindows64);
    }

    [MenuItem("Lumbra/Exportar/Web")]
    public static void Web()
    {
        // itch.io no envía los archivos comprimidos con su cabecera, por eso el juego se descomprime solo
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        Export("Builds/Web", BuildTarget.WebGL);
    }

    private static void Export(string path, BuildTarget target)
    {
        var report = BuildPipeline.BuildPlayer(Scenes, path, target, BuildOptions.None);
        Debug.Log("Exportado " + target + ": " + report.summary.result + " (" + report.summary.totalSize / (1024 * 1024) + " MB)");
    }
}
