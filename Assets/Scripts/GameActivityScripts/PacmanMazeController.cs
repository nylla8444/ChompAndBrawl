using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PacmanMazeController : MonoBehaviour
{
    [Header("Pacman Properties")]
    [SerializeField] private GameObject pacman;
    [SerializeField] private float defaultSpeed;

    [Header("Pacman Miscellaneous")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask collisionLayer;
    [SerializeField] private Text scoresText;
    
    [Header("Pacman Effect Item Info")]

    [Space(12)]
    [SerializeField] private bool hasMazeStarted = false;
    private bool isRunning = false;
    
    private Vector2 direction;
    private Vector2 targetPosition;
    private Vector2 queuedDirection;

    private BoxCollider2D pacmanCollider;

    private const float TILE_SIZE = 0.16f;


    private void Start()
    {
        InitializePacman();
        RegisterKeyActions();
    }

    private void OnDestroy()
    {
        UnregisterKeyActions();
    }

    public void StartPacmanController(bool triggerValue)
    {
        hasMazeStarted = triggerValue;
    }

    private void RegisterKeyActions()
    {
        KeybindDataManager.RegisterKeyAction("pacman.face_up", () => HandleInput("pacman.face_up"));
        KeybindDataManager.RegisterKeyAction("pacman.face_down", () => HandleInput("pacman.face_down"));
        KeybindDataManager.RegisterKeyAction("pacman.face_left", () => HandleInput("pacman.face_left"));
        KeybindDataManager.RegisterKeyAction("pacman.face_right", () => HandleInput("pacman.face_right"));
    }

    private void UnregisterKeyActions()
    {
        KeybindDataManager.UnregisterKeyAction("pacman.face_up", () => HandleInput("pacman.face_up"));
        KeybindDataManager.UnregisterKeyAction("pacman.face_down", () => HandleInput("pacman.face_down"));
        KeybindDataManager.UnregisterKeyAction("pacman.face_left", () => HandleInput("pacman.face_left"));
        KeybindDataManager.UnregisterKeyAction("pacman.face_right", () => HandleInput("pacman.face_right"));
    }

    private void Update()
    {
        if (!hasMazeStarted) return;

        UpdateDisplayText();
 
        if (!isRunning && queuedDirection != Vector2.zero)
        {
            direction = queuedDirection;
            targetPosition = (Vector2)pacman.transform.position + direction * TILE_SIZE;
            isRunning = true;
            UpdatePacmanAnimation();
        }

        // if (isRunning)
        // {
        //     MoveTowards();
        // }
    }

    private void FixedUpdate()
    {
        if (!hasMazeStarted) return;
        
        if (isRunning)
        {
            MoveTowards();
        }
    }

    private void InitializePacman()
    {
        GameData gameData = GameDataManager.LoadData();
        Vector2 pacmanPosition = gameData.pacman_data.coordinate;

        pacman.transform.position = (pacmanPosition != Vector2.zero) 
            ? new Vector2(Mathf.Round(pacmanPosition.x * 100.0f) / 100.0f, Mathf.Round(pacmanPosition.y * 100.0f) / 100.0f)
            : new Vector2(Mathf.Round(spawnPoint.position.x * 100.0f) / 100.0f, Mathf.Round(spawnPoint.position.y * 100.0f) / 100.0f);
        
        pacmanCollider = pacman.GetComponent<BoxCollider2D>();

        animator?.SetTrigger("pacman.rest");
    }

    private void HandleInput(string action)
    {
        if (!hasMazeStarted) return;
        
        Vector2 _direction = action switch
        {
            "pacman.face_up" => Vector2.up,
            "pacman.face_down" => Vector2.down,
            "pacman.face_left" => Vector2.left,
            "pacman.face_right" => Vector2.right,
            _ => Vector2.zero
        };
        
        if (_direction != Vector2.zero)
        {
            queuedDirection = _direction;
            Debug.Log($"Ghost queued {_direction}.");
        }
    }

    private void MoveTowards()
    {
        Vector2 currentPosition = (Vector2)pacman.transform.position;

        if (currentPosition == targetPosition)
        {
            if (IsAbleToMoveTo(currentPosition + queuedDirection * TILE_SIZE))
            {
                direction = queuedDirection;
                targetPosition = currentPosition + direction * TILE_SIZE;
                UpdatePacmanAnimation();
            }
            else if (IsAbleToMoveTo(currentPosition + direction * TILE_SIZE))
            {
                targetPosition = currentPosition + direction * TILE_SIZE;
            }
            else
            {
                isRunning = false;
                return;
            }
        }

        if (IsAbleToMoveTo(targetPosition))
        {
            float speedMultiplier = GameDataManager.LoadData().pacman_data.speed_multiplier;
            Vector2 newPosition = Vector2.MoveTowards(currentPosition, targetPosition, (defaultSpeed * speedMultiplier) * Time.fixedDeltaTime);
            pacman.transform.position = newPosition;

            if (newPosition == targetPosition)
            {
                UpdatePacmanPosition(newPosition);
            }
        }
        else
        {
            isRunning = false;
        }
    }

    private bool IsAbleToMoveTo(Vector2 targetPosition)
    {
        Bounds pacmanBounds = pacmanCollider.bounds;
        pacmanBounds.center = targetPosition;
        Collider2D[] hits = Physics2D.OverlapBoxAll(pacmanBounds.center, pacmanBounds.size, 0f, collisionLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit != null && hit.gameObject != pacman)
            {
                return false;
            }
        }

        return true;
    }

    private void UpdatePacmanPosition(Vector2 position)
    {
        GameData gameData = GameDataManager.LoadData();
        gameData.pacman_data.coordinate = position;
        gameData.pacman_data.direction = direction;
        GameDataManager.SaveData(gameData);
    }

    private void UpdatePacmanAnimation()
    {
        foreach(AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(parameter.name);
            }
        }

        bool hasPowerPellet = GameDataManager.LoadData().pacman_data.has_power_pellet;
        
        string animatorId = direction switch
        {
            Vector2 v when v == Vector2.up => (!hasPowerPellet) ? "pacman.normal_up" : "pacman.scary_up",
            Vector2 v when v == Vector2.down => (!hasPowerPellet) ? "pacman.normal_down" : "pacman.scary_down",
            Vector2 v when v == Vector2.left => (!hasPowerPellet) ? "pacman.normal_left" : "pacman.scary_left",
            Vector2 v when v == Vector2.right => (!hasPowerPellet) ? "pacman.normal_right" : "pacman.scary_right",
            _ => "pacman.rest"
        };

        animator.SetTrigger(animatorId);
    }

    public void Respawn()
    {
        pacman.transform.position = spawnPoint.position;
        UpdatePacmanPosition(spawnPoint.position);

        animator.SetTrigger("pacman.rest");
        UpdatePacmanAnimation();
    }

    private void UpdateDisplayText()
    {
        int pacmanScore = GameDataManager.LoadData().pacman_data.score; 
        scoresText.text = pacmanScore.ToString("00000000");
    }

    /*********************************************************************/
    //                            Item Use
    /*********************************************************************/
}
