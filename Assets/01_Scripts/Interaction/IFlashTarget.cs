using UnityEngine;

// Todo lo que reacciona al destello de luz del jugador
public interface IFlashTarget
{
    void OnFlash(Vector2 from);
}
