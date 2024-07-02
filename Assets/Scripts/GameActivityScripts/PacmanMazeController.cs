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

    [Header("Ghost Power-up Info")]
    [SerializeField] private Text scoresText;

    [Space(12)]
    [SerializeField] private bool hasMazeStarted = false;
    private bool isRunning = false;
    
    private Vector2 direction;
    private Vector2 targetPosition;
    private Vector2 queuedDirection;

    private Rigidbody2D rb;
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
            ? pacmanPosition
            : spawnPoint.position;
        
        pacmanCollider = pacman.GetComponent<BoxCollider2D>();
        rb = pacman.GetComponent<Rigidbody2D>();

        animator?.SetTrigger("pacman.rest");
    }

    private void HandleInput(string action)
    {
        if (!hasMazeStarted) return;
        
        switch (action)
        {
            case "pacman.face_up":
                queuedDirection = Vector2.up;
                Debug.Log("Pac-man queued up.");
                break;
            
            case "pacman.face_down":
                queuedDirection = Vector2.down;
                Debug.Log("Pac-man queued down.");
                break;
            
            case "pacman.face_left":
                queuedDirection = Vector2.left;
                Debug.Log("Pac-man queued left.");
                break;

            case "pacman.face_right":
                queuedDirection = Vector2.right;
                Debug.Log("Pac-man queued right.");
                break;
        }
    }

    private void MoveTowards()
    {
        Vector2 currentPosition = pacman.transform.position;

        if ((Vector2)pacman.transform.position == targetPosition)
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
        animator.ResetTrigger("pacman.rest");
        animator.ResetTrigger("pacman.normal_up");
        animator.ResetTrigger("pacman.normal_down");
        animator.ResetTrigger("pacman.normal_left");
        animator.ResetTrigger("pacman.normal_right");
        
        switch (direction)
        {
            case Vector2 vector when vector == Vector2.up:
                animator.SetTrigger("pacman.normal_up");
                break;

            case Vector2 vector when vector == Vector2.down:
                animator.SetTrigger("pacman.normal_down");
                break;

            case Vector2 vector when vector == Vector2.left:
                animator.SetTrigger("pacman.normal_left");
                break;

            case Vector2 vector when vector == Vector2.right:
                animator.SetTrigger("pacman.normal_right");
                break;
        }
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
        scoresText.text = pacmanScore.ToString("0000000");
    }
}
