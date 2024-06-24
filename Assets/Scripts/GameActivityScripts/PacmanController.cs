using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacmanController : MonoBehaviour
{
    private void Start()
    {
        RegisterKeyActions();
    }

    private void OnDestroy()
    {
        UnregisterKeyActions();
    }

    private void RegisterKeyActions()
    {
        KeybindDataManager.RegisterKeyAction("pacman.face_up", FaceUp);
        KeybindDataManager.RegisterKeyAction("pacman.face_down", FaceDown);
        KeybindDataManager.RegisterKeyAction("pacman.face_left", FaceLeft);
        KeybindDataManager.RegisterKeyAction("pacman.face_right", FaceRight);
        Debug.Log("All key bindings successfully registered.");
    }

    private void UnregisterKeyActions()
    {
        KeybindDataManager.UnregisterKeyAction("pacman.face_up", FaceUp);
        KeybindDataManager.UnregisterKeyAction("pacman.face_down", FaceDown);
        KeybindDataManager.UnregisterKeyAction("pacman.face_left", FaceLeft);
        KeybindDataManager.UnregisterKeyAction("pacman.face_right", FaceRight);
        Debug.Log("All key bindings successfully unregistered.");
    }

    private void FaceUp()
    {
        Debug.Log("Pac-man facing up.");
    }

    private void FaceDown()
    {
        Debug.Log("Pac-man facing down.");
    }

    private void FaceLeft()
    {
        Debug.Log("Pac-man facing left.");
    }

    private void FaceRight()
    {
        Debug.Log("Pac-man facing right.");
    }
}
