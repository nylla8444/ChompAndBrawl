using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplitScreenController : MonoBehaviour
{
    [SerializeField] private Camera player1Camera;
    [SerializeField] private Camera player2Camera;

    [SerializeField] private Transform player1;
    [SerializeField] private List<Transform> allGhosts;

    private void Start()
    {
        UpdateCurrentControllingGhost();
    }

    private void Update()
    {
        // Player 1 camera follows Pacman
        player1Camera.transform.position = new Vector3(player1.position.x, player1.position.y, player1Camera.transform.position.z);

        // Player 2 camera follows the current ghost
        Transform currentCharacter = GetCurrentControllingGhost();
        if (currentCharacter != null)
        {
            player2Camera.transform.position = new Vector3(currentCharacter.position.x, currentCharacter.position.y, player2Camera.transform.position.z);
        }
    }

    private void UpdateCurrentControllingGhost()
    {
        GameData gameData = GameDataManager.LoadData();
        if (gameData != null && gameData.ghost_data != null)
        {
            foreach (Transform ghost in allGhosts)
            {
                if (ghost.name == gameData.ghost_data.current_controlling_ghost)
                {
                    player2Camera.transform.position = new Vector3(ghost.position.x, ghost.position.y, player2Camera.transform.position.z);
                    break;
                }
            }
        }
    }

    private Transform GetCurrentControllingGhost()
    {
        GameData gameData = GameDataManager.LoadData();
        if (gameData != null && gameData.ghost_data != null)
        {
            foreach (Transform ghost in allGhosts)
            {
                if (ghost.name == gameData.ghost_data.current_controlling_ghost)
                {
                    return ghost;
                }
            }
        }
        return null;
    }
}
