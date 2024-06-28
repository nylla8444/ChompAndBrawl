using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacmanMazeController : MonoBehaviour
{
    [SerializeField] private GameObject pacman;
    [SerializeField] private float movementSpeed;
    [SerializeField] private LayerMask collisionLayer;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Animator animator;

    [SerializeField] private bool isRunning = false;
    private Vector2 direction;
    private Vector2 targetPosition;
    private Vector2 queuedDirection;

    private Rigidbody2D rb;
    private BoxCollider2D pacmanCollider;


    private void Start()
    {
        pacman.transform.position = spawnPoint.position;
        direction = Vector2.right;
        targetPosition = (Vector2)pacman.transform.position + direction * 0f;

        rb = pacman.GetComponent<Rigidbody2D>();
        pacmanCollider = pacman.GetComponent<BoxCollider2D>();

        animator.SetTrigger("pacman.rest");
        UpdateAnimation();
        RegisterKeyActions();
    }

    private void OnDestroy()
    {
        UnregisterKeyActions();
    }

    private void Update()
    {
        KeybindDataManager.Update();

        if (!isRunning && queuedDirection != Vector2.zero)
        {
            direction = queuedDirection;
            targetPosition = (Vector2)pacman.transform.position + direction * 0.16f;
            isRunning = true;
            UpdateAnimation();
        }
    }

    private void FixedUpdate()
    {
        if (isRunning)
        {
            MoveTowards();
        }
    }

    private void HandleInput(string action)
    {
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
            if (CanMoveTo(currentPosition + queuedDirection * 0.16f))
            {
                direction = queuedDirection;
                targetPosition = currentPosition + direction * 0.16f;
                UpdateAnimation();
            }
            else if (CanMoveTo(currentPosition + direction * 0.16f))
            {
                targetPosition = currentPosition + direction * 0.16f;
            }
        }

        if (CanMoveTo(targetPosition))
        {
            pacman.transform.position = Vector2.MoveTowards(currentPosition, targetPosition, movementSpeed * Time.fixedDeltaTime);
        }
        else
        {
            Debug.Log("Collision detected, cannot move.");
        }
    }

    private bool CanMoveTo(Vector2 targetPosition)
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

    private void UpdateAnimation()
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
        direction = Vector2.right;
        targetPosition = (Vector2)pacman.transform.position + direction * 0.16f;
        queuedDirection = Vector2.right;
        isRunning = false;
        animator.SetTrigger("pacman.rest");
        UpdateAnimation();
    }

    public void StartRunning()
    {
        isRunning = true;
        targetPosition = (Vector2)pacman.transform.position + direction * 0.16f;
    }

    public void StopRunning()
    {
        isRunning = false;
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
}
