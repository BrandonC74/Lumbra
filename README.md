# Lumbra

*No todas las luces dicen la verdad · Not every light tells the truth*

Plataformas 2D de terror atmosférico hecho en Unity para la **GameJamPlus 26/27** (sede Univalle, Cochabamba).
Temas de la jam: **Something's Off** y **One More Time**.

- **Jugar (Windows):** https://brandon-cartagena.itch.io/lumbra

Eres **Lumbra**, la última luz en un santuario en ruinas. Rescatas almas que te siguen y te dan poderes...
pero una de ellas miente, y en cada partida es otra. Al final del camino, en el castillo, te espera **El Devorador**.

## Cómo se juega

| Acción | Tecla |
|---|---|
| Moverse | A / D |
| Saltar (mantener = más alto) / salto de pared | Espacio |
| Destello de luz | E |
| Soltar un alma descubierta | Q |
| Dejar un alma en un brasero | F |
| Pasar diálogo | Enter |
| Pausa | Esc |

- **Tu luz es tu vida:** se achica sin energía. El destello revela plataformas ocultas y quema sombras y raíces.
- **Almas:** dan doble salto, destello amplio y escudo. La mentirosa tiene la sombra hacia arriba y el destello la descubre.
- **No mires atrás:** los carteles cambian y los faroles se apagan si te das vuelta.
- **El Devorador:** enciende los 4 braseros con almas para debilitarlo; él las roba y las lanza lejos.
- **3 finales:** *La verdad*, *El refugio* y *La nueva sombra*, según qué almas crucen el portal contigo.
- Español e inglés. La partida se guarda en cada farol.

## Abrir el proyecto

1. Instalar **Unity 6000.5.5f1** (Unity 6.5) con el módulo de Windows.
2. Abrir la carpeta `Lumbra` desde Unity Hub.
3. Al pulsar Play el juego empieza siempre en el menú (`Assets/00_Scenes/Menu.unity`).

Escenas: `Menu` (título, opciones, idiomas) y `Santuario` (todo el juego: 4 zonas y el jefe).

### El nivel se genera con código

El nivel, el menú, el personaje y sus animaciones los arma `Assets/Editor/LumbraBuilder.cs`:

- **Lumbra > Construir nivel**: reconstruye `Santuario` (y el menú) desde cero.
- **Lumbra > Construir menú**: solo el menú.
- **Lumbra > Exportar > Windows / Web**: exporta el juego a `Builds/`.

Si se cambia algo del nivel, conviene hacerlo en `LumbraBuilder.cs` y volver a construir, para no perder los cambios.

## Estructura

```
Assets/
  00_Scenes/      Menu y Santuario
  01_Scripts/     Código del juego (Player, Enemies, Souls, Environment, Interaction, Systems, UI)
  02_Prefabs/     Efectos (muertes, raíces quemadas, sombra cazadora)
  03_Sprites/     Sprites; Player/Player.png es el dibujo del personaje por partes
  04_Materials/   Materiales y el perfil de posprocesado
  05_Audio/       Efectos (Kenney), música y sonidos (OpenGameArt), voces susurradas
  06_Fonts/       Cinzel y Alegreya Sans
  07_Animations/  Animator y clips de Lumbra (los genera el builder)
  Editor/         LumbraBuilder, exportación y Play desde el menú
Tools/
  RecortarPartes.py   Recorta las partes del personaje desde Player.png (python Tools/RecortarPartes.py)
```

Estilo del código: scripts cortos, `[SerializeField] private`, singletons con `Instance { get; private set; }`,
comentarios en español y nombres en inglés.

## Equipo

| Integrante | Rol |
|---|---|
| Brandon Cartagena | Programación, arte y diseño (líder) |
| Romina Poma | Arte y diseño |
| Miguel Carvajal | Programación y diseño |

## Créditos

- **Efectos de sonido:** Kenney.nl (CC0).
- **Sonidos y música** (OpenGameArt.org, CC0): "Heartbeat sounds" (bart), "Breathing Tired" (mikeask),
  "Scary High-pitched Ghost" (Fupi), "Ambient horror" (techiew), "Dark Place (loop)" (SkyleTheFrench),
  "Battle Theme" (Wolfgang_). Detalle en `Assets/05_Audio/OpenGameArt/CREDITOS.txt`.
- **Fuentes:** Cinzel y Alegreya Sans (SIL Open Font License 1.1).
- **Voces susurradas:** texto a voz procesado.
