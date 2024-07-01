using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplitScreenController : MonoBehaviour
{
    [Header("Player Cameras")]
    [SerializeField] private Camera player1Camera;
    [SerializeField] private Camera player2Camera;

    [Header("Transform Objects")]
    [SerializeField] private Transform player1;
    [SerializeField] private List<Transform> allGhosts;

    [Header("Camera Properties")]
    [SerializeField] private float cameraSmoothSpeed = 0.125f;
    [SerializeField] private float offsetDistance = 1.0f;

    private Transform currentGhost;
    private Vector3 player1Velocity = Vector3.zero;
    private Vector3 player2Velocity = Vector3.zero;

    private void Start()
    {
        UpdateCurrentControllingGhost();
    }

    private void Update()
    {
        // Player 1 camera follows Pacman with offset
        Vector3 player1TargetPosition = GetTargetPosition(player1, player1Camera);
        player1Camera.transform.position = Vector3.SmoothDamp(player1Camera.transform.position, player1TargetPosition, 
                                                              ref player1Velocity, cameraSmoothSpeed);
        player1Camera.transform.position = new Vector3(player1Camera.transform.position.x, player1Camera.transform.position.y, -10f);

        // Player 2 camera follows the current ghost
        Transform currentCharacter = GetCurrentControllingGhost();
        if (currentCharacter != null)
        {
            Vector3 player2TargetPosition = GetTargetPosition(currentCharacter, player2Camera);
            player2Camera.transform.position = Vector3.SmoothDamp(player2Camera.transform.position, player2TargetPosition, 
                                                                  ref player2Velocity, cameraSmoothSpeed);
            player2Camera.transform.position = new Vector3(player2Camera.transform.position.x, player2Camera.transform.position.y, -10f);
        }
    }

    private Vector3 GetTargetPosition(Transform target, Camera camera)
    {
        Vector3 offset = Vector3.zero;
        if (target.hasChanged)
        {
            Vector3 direction = (target.position - camera.transform.position).normalized;
            offset = direction * offsetDistance;
        }
        return new Vector3(target.position.x, target.position.y, camera.transform.position.z) + offset;
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
                    currentGhost = ghost;
                    StartCoroutine(SmoothTransition(player2Camera.transform, new Vector3(ghost.position.x, ghost.position.y, player2Camera.transform.position.z)));
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
                    if (currentGhost != ghost)
                    {
                        currentGhost = ghost;
                        StartCoroutine(SmoothTransition(player2Camera.transform, new Vector3(ghost.position.x, ghost.position.y, player2Camera.transform.position.z)));
                    }
                    return ghost;
                }
            }
        }
        return null;
    }

    private IEnumerator SmoothTransition(Transform cameraTransform, Vector3 targetPosition)
    {
        float elapsedTime = 0.0f;
        Vector3 startingPosition = cameraTransform.position;
        while (elapsedTime < cameraSmoothSpeed)
        {
            cameraTransform.position = Vector3.Lerp(startingPosition, targetPosition, elapsedTime / cameraSmoothSpeed);
            cameraTransform.position = new Vector3(cameraTransform.position.x, cameraTransform.position.y, -10f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        cameraTransform.position = new Vector3(targetPosition.x, targetPosition.y, -10f);
    }
}
