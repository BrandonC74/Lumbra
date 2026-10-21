using System.Collections.Generic;
using UnityEngine;

public enum Language { Es, En }

// Idiomas del juego: todos los textos en español e inglés. El idioma elegido se guarda.
// Uso: Lang.Get("play") devuelve el texto en el idioma actual.
public static class Lang
{
    public static event System.Action OnChanged;

    public static Language Current => (Language)PlayerPrefs.GetInt("Language", 0);

    public static void Set(Language language)
    {
        PlayerPrefs.SetInt("Language", (int)language);
        OnChanged?.Invoke();
    }

    public static string Get(string key)
    {
        return Texts.TryGetValue(key, out string[] text) ? text[(int)Current] : key;
    }

    // Para textos que vienen de a dos (como los diálogos del nivel)
    public static string Pick(string es, string en)
    {
        return Current == Language.Es || string.IsNullOrEmpty(en) ? es : en;
    }

    public static string Speaker(string name)
    {
        switch (name)
        {
            case "Alma": return Pick(name, "Soul");
            case "Alma perdida": return Pick(name, "Lost soul");
            case "Alma del recuerdo": return Pick(name, "Memory soul");
            case "El Devorador": return Pick(name, "The Devourer");
            default: return name;
        }
    }

    private static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]>
    {
        // Menú principal y carga
        ["tagline"] = new[] { "No todas las luces dicen la verdad", "Not every light tells the truth" },
        ["continue"] = new[] { "CONTINUAR", "CONTINUE" },
        ["newGame"] = new[] { "NUEVA PARTIDA", "NEW GAME" },
        ["options"] = new[] { "OPCIONES", "OPTIONS" },
        ["quit"] = new[] { "SALIR", "QUIT" },
        ["back"] = new[] { "VOLVER", "BACK" },
        ["volume"] = new[] { "VOLUMEN", "VOLUME" },
        ["language"] = new[] { "IDIOMA", "LANGUAGE" },
        ["loading"] = new[] { "Cargando...", "Loading..." },
        ["tip0"] = new[] { "Las almas corruptas tienen la sombra hacia arriba.", "Corrupt souls cast their shadow upward." },
        ["tip1"] = new[] { "Los faroles guardan tu partida.", "Lanterns save your game." },
        ["tip2"] = new[] { "Las antorchas del castillo recargan tu luz.", "Castle torches recharge your light." },
        ["tip3"] = new[] { "Si un alma no tiene sombra... no es un alma.", "If a soul has no shadow... it is not a soul." },

        // Pausa
        ["paused"] = new[] { "PAUSA", "PAUSED" },
        ["resume"] = new[] { "CONTINUAR", "RESUME" },
        ["restart"] = new[] { "VOLVER AL ÚLTIMO FAROL", "BACK TO LAST LANTERN" },
        ["mainMenu"] = new[] { "MENÚ PRINCIPAL", "MAIN MENU" },
        ["controls"] = new[] { "A/D moverse  ·  ESPACIO saltar  ·  E destello  ·  Q soltar alma  ·  F brasero", "A/D move  ·  SPACE jump  ·  E flash  ·  Q release soul  ·  F brazier" },

        // HUD
        ["bossName"] = new[] { "EL DEVORADOR", "THE DEVOURER" },
        ["light"] = new[] { "LUZ", "LIGHT" },
        ["souls"] = new[] { "ALMAS", "SOULS" },
        ["saved"] = new[] { "Partida guardada", "Game saved" },
        ["doubleJump"] = new[] { "Doble salto", "Double jump" },
        ["bigFlash"] = new[] { "Destello amplio", "Wide flash" },
        ["shield"] = new[] { "Escudo", "Shield" },
        ["tutorialKeys"] = new[] { "A / D  moverte        ESPACIO  saltar        E  destello        Q  soltar alma", "A / D  move        SPACE  jump        E  flash        Q  release soul" },
        ["tutorialGoal"] = new[] { "Rescata las almas y llega al portal", "Rescue the souls and reach the portal" },

        // Final
        ["ending0"] = new[] { "El refugio", "The refuge" },
        ["ending1"] = new[] { "La nueva sombra", "The new shadow" },
        ["ending2"] = new[] { "La verdad", "The truth" },
        ["story0"] = new[] { "Las almas cruzan contigo. Por primera vez en mucho tiempo, el santuario respira.", "The souls cross with you. For the first time in ages, the sanctuary breathes." },
        ["story1"] = new[] { "Cruzas el portal... pero algo cruzó contigo. Te miras las manos: ya no brillan.", "You cross the portal... but something crossed with you. You look at your hands: they no longer shine." },
        ["story2"] = new[] { "«Fuiste tú. Apagaste la luz para quedártela». Esta vez decidiste compartirla.", "\"It was you. You put out the light to keep it\". This time you chose to share it." },
        ["gameOver"] = new[] { "TE CONSUMIÓ LA OSCURIDAD", "THE DARKNESS CONSUMED YOU" },
        ["gameOverStory"] = new[] { "Tu luz se apagó... pero las almas siguen esperando.", "Your light went out... but the souls are still waiting." },
        ["time"] = new[] { "Tiempo", "Time" },
        ["soulsWith"] = new[] { "Almas contigo", "Souls with you" },
        ["hits"] = new[] { "Golpes", "Hits" },
        ["newRecord"] = new[] { "¡NUEVO RÉCORD!", "NEW RECORD!" },
        ["gallery"] = new[] { "FINALES DESCUBIERTOS", "ENDINGS FOUND" },
        ["againWin"] = new[] { "R  jugar otra vez  ·  las almas que mienten cambian en cada partida", "R  play again  ·  the lying souls change every run" },
        ["againLose"] = new[] { "R  volver al último farol", "R  back to the last lantern" },

        // Textos del mundo
        ["pressWall"] = new[] { "ESPACIO contra la pared", "SPACE against the wall" },
        ["signLie"] = new[] { "ELLAS TE MIENTEN", "THEY LIE TO YOU" },
        ["signLie2"] = new[] { "YO TAMBIÉN", "SO DO I" },
        ["wallVoice"] = new[] { "NO CONFÍES EN LA VOZ", "DON'T TRUST THE VOICE" },
        ["wallWaits"] = new[] { "ÉL TE ESPERA", "HE IS WAITING" },

        // Mensajes que aparecen durante el juego
        ["noSouls"] = new[] { "No te quedan almas. Rescata las que siguen atrapadas.", "You have no souls left. Rescue the ones still trapped." },
        ["betray"] = new[] { "Je... gracias por traerme hasta aquí.", "Heh... thanks for bringing me this far." },
        ["forkGuide"] = new[] { "Por aquí... confía en mí, es más seguro.", "This way... trust me, it's safer." },
        ["fakeSoul"] = new[] { "Je... no toda luz es un alma. ¿Viste que no tenía sombra?", "Heh... not every light is a soul. Did you see it had no shadow?" },
        ["exposed"] = new[] { "¡Esa alma no es lo que parece!   Q  soltarla", "That soul is not what it seems!   Q  release it" },
        ["devour"] = new[] { "¡El Devorador robó un alma del brasero! Ve a buscarla.", "The Devourer stole a soul from a brazier! Go get it back." },
        ["released"] = new[] { "La dejaste ir. Ya no podrá engañarte.", "You let it go. It can't fool you anymore." },
    };
}
