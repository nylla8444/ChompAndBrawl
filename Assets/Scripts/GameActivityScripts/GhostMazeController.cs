using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GhostMazeController : MonoBehaviour
{
    [SerializeField] private GameObject blinky;
    [SerializeField] private GameObject clyde;
    [SerializeField] private GameObject inky;
    [SerializeField] private GameObject pinky;

    [SerializeField] private Transform blinkySpawn;
    [SerializeField] private Transform clydeSpawn;
    [SerializeField] private Transform inkySpawn;
    [SerializeField] private Transform pinkySpawn;

    [SerializeField] private float movementSpeed;
    [SerializeField] private LayerMask collisionLayer;
    [SerializeField] private Animator blinkyAnimator;
    [SerializeField] private Animator clydeAnimator;
    [SerializeField] private Animator inkyAnimator;
    [SerializeField] private Animator pinkyAnimator;

    [SerializeField] private float switchCooldown;
    [SerializeField] private Text cooldownText;
    private float lastSwitchTime = -Mathf.Infinity;

    private GameObject currentGhost;
    private Transform currentSpawn;
    private Animator currentAnimator;
    private Vector2 direction;
    private Vector2 targetPosition;
    private Vector2 queuedDirection;
    private bool isRunning = false;
    private bool queuedGhostSwitch = false;
    private bool switchButtonPressed = false;

    private Rigidbody2D rb;
    private BoxCollider2D ghostCollider;
    private List<string> aliveGhosts;
    private string lastControllingGhost;


    private void Start()
    {
        GameData gameData = GameDataManager.LoadData();
        aliveGhosts = gameData.ghost_data.list_alive_ghost;
        lastControllingGhost = gameData.ghost_data.current_controlling_ghost;

        InitializeGhosts();
        SwitchGhost(lastControllingGhost);
        RegisterKeyActions();
    }

    private void OnDestroy()
    {
        UnregisterKeyActions();
    }

    private void Update()
    {
        HandleGhostSwitch();
        UpdateCooldownText();

        if (currentGhost != null)
        {
            if (!isRunning && queuedDirection != Vector2.zero)
            {
                direction = queuedDirection;
                targetPosition = (Vector2)currentGhost.transform.position + direction * 0.16f;
                isRunning = true;
                UpdateAnimation();
            }
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
            case "ghost.face_up":
                queuedDirection = Vector2.up;
                Debug.Log("Ghost queued up.");
                break;
            
            case "ghost.face_down":
                queuedDirection = Vector2.down;
                Debug.Log("Ghost queued down.");
                break;
            
            case "ghost.face_left":
                queuedDirection = Vector2.left;
                Debug.Log("Ghost queued left.");
                break;

            case "ghost.face_right":
                queuedDirection = Vector2.right;
                Debug.Log("Ghost queued right.");
                break;

            case "ghost.change_ghost":
                if (!switchButtonPressed)
            {
                switchButtonPressed = true;
                if (!isRunning)
                {
                    SwitchToNextGhost();
                }
                else
                {
                    queuedGhostSwitch = true;
                }
            }
            break;
        }
    }

    private void MoveTowards()
    {
        Vector2 currentPosition = currentGhost.transform.position;

        if ((Vector2)currentGhost.transform.position == targetPosition)
        {
            if (queuedGhostSwitch)
            {
                queuedGhostSwitch = false;
                SwitchToNextGhost();
                return;
            }

            if (queuedDirection != Vector2.zero && CanMoveTo(currentPosition + queuedDirection * 0.16f))
            {
                direction = queuedDirection;
                targetPosition = currentPosition + direction * 0.16f;
                queuedDirection = Vector2.zero;
                UpdateAnimation();
            }
            else if (CanMoveTo(currentPosition + direction * 0.16f))
            {
                targetPosition = currentPosition + direction * 0.16f;
            }
            else
            {
                isRunning = false;
                if (queuedGhostSwitch)
                {
                    queuedGhostSwitch = false;
                    SwitchToNextGhost();
                }
                return;
            }
        }

        if (CanMoveTo(targetPosition))
        {
            currentGhost.transform.position = Vector2.MoveTowards(currentPosition, targetPosition, movementSpeed * Time.fixedDeltaTime);
        }
        else
        {
            isRunning = false;
            if (queuedGhostSwitch)
            {
                queuedGhostSwitch = false;
                SwitchToNextGhost();
            }
        }
    }

    private bool CanMoveTo(Vector2 targetPosition)
    {
        Bounds ghostBounds = ghostCollider.bounds;
        ghostBounds.center = targetPosition;
        Collider2D[] hits = Physics2D.OverlapBoxAll(ghostBounds.center, ghostBounds.size, 0f, collisionLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit != null && hit.gameObject != currentGhost)
            {
                return false;
            }
        }

        return true;
    }

    private void UpdateAnimation()
    {
        currentAnimator.ResetTrigger($"{currentGhost.name}.rest");
        currentAnimator.ResetTrigger($"{currentGhost.name}.normal_up");
        currentAnimator.ResetTrigger($"{currentGhost.name}.normal_down");
        currentAnimator.ResetTrigger($"{currentGhost.name}.normal_left");
        currentAnimator.ResetTrigger($"{currentGhost.name}.normal_right");

        switch (direction)
        {
            case Vector2 vector when vector == Vector2.up:
                currentAnimator.SetTrigger($"{currentGhost.name}.normal_up");
                break;
            case Vector2 vector when vector == Vector2.down:
                currentAnimator.SetTrigger($"{currentGhost.name}.normal_down");
                break;
            case Vector2 vector when vector == Vector2.left:
                currentAnimator.SetTrigger($"{currentGhost.name}.normal_left");
                break;
            case Vector2 vector when vector == Vector2.right:
                currentAnimator.SetTrigger($"{currentGhost.name}.normal_right");
                break;
        }
    }

    private void InitializeGhosts()
    {
        Dictionary<string, (GameObject ghost, Transform spawn)> ghostMap = new Dictionary<string, (GameObject, Transform)>
        {
            { "blinky", (blinky, blinkySpawn) },
            { "clyde", (clyde, clydeSpawn) },
            { "inky", (inky, inkySpawn) },
            { "pinky", (pinky, pinkySpawn) }
        };

        foreach (var ghost in ghostMap)
        {
            if (aliveGhosts.Contains(ghost.Key))
            {
                ghost.Value.ghost.transform.position = ghost.Value.spawn.position;
                Animator animator = ghost.Value.ghost.GetComponent<Animator>();
                animator.SetTrigger($"{ghost.Value.ghost.name}.rest");
            }
            else
            {
                Destroy(ghost.Value.ghost);
            }
        }
    }

    private void HandleGhostSwitch()
    {
        GameData gameData = GameDataManager.LoadData();
        string controllingGhost = gameData.ghost_data.current_controlling_ghost;

        if (currentGhost == null || currentGhost.name != controllingGhost)
        {
            if (currentGhost != null)
            {
                if (!isRunning)
                {
                    SwitchGhost(controllingGhost);
                }
                else
                {
                    queuedGhostSwitch = true;
                }
            }
            else
            {
                SwitchGhost(controllingGhost);
            }
        }

        if (lastControllingGhost != controllingGhost)
        {
            SwitchGhost(controllingGhost);
            lastControllingGhost = controllingGhost;
        }
    }

    private void SwitchGhost(string ghostName)
    {
        switch (ghostName)
        {
            case "blinky":
                currentGhost = blinky;
                currentSpawn = blinkySpawn;
                currentAnimator = blinkyAnimator;
                break;
            case "clyde":
                currentGhost = clyde;
                currentSpawn = clydeSpawn;
                currentAnimator = clydeAnimator;
                break;
            case "inky":
                currentGhost = inky;
                currentSpawn = inkySpawn;
                currentAnimator = inkyAnimator;
                break;
            case "pinky":
                currentGhost = pinky;
                currentSpawn = pinkySpawn;
                currentAnimator = pinkyAnimator;
                break;
            default:
                currentGhost = null;
                return;
        }

        if (currentGhost != null)
        {
            ghostCollider = currentGhost.GetComponent<BoxCollider2D>();
            rb = currentGhost.GetComponent<Rigidbody2D>();

            direction = Vector2.zero;
            targetPosition = (Vector2)currentGhost.transform.position + direction * 0f;
            queuedDirection = Vector2.zero;
            isRunning = false;
            currentAnimator.SetTrigger($"{currentGhost.name}.rest");
            currentGhost.transform.position = targetPosition;
        }
    }

    private void SwitchToNextGhost()
    {
        if (aliveGhosts.Count == 0)
        {
            return;
        }

        switchButtonPressed = false;

        int currentCharacterIndex = aliveGhosts.IndexOf(lastControllingGhost);
        currentCharacterIndex = (currentCharacterIndex + 1) % aliveGhosts.Count;
        
        string nextGhostName = aliveGhosts[currentCharacterIndex];

        if (Time.time - lastSwitchTime < switchCooldown)
        {
            Debug.Log("Switch ghost cooldown in progress.");
            return;
        }

        if (currentGhost != null)
        {
            isRunning = false;
            direction = Vector2.zero;
            queuedDirection = Vector2.zero;
            currentAnimator.SetTrigger($"{currentGhost.name}.rest");
        }

        SwitchGhost(nextGhostName);

        GameData gameData = GameDataManager.LoadData();
        gameData.ghost_data.current_controlling_ghost = nextGhostName;
        GameDataManager.SaveData(gameData);

        lastSwitchTime = Time.time;
        
        if (queuedGhostSwitch)
        {
            queuedGhostSwitch = false;
            SwitchToNextGhost();
        }
    }

    private void UpdateCooldownText()
    {
        float cooldownRemaining = Mathf.Max(0f, switchCooldown - (Time.time - lastSwitchTime));
        cooldownText.text = cooldownRemaining > 0 ? $"{cooldownRemaining:F1}s" : "";
    }

    private void RegisterKeyActions()
    {
        KeybindDataManager.RegisterKeyAction("ghost.face_up", () => HandleInput("ghost.face_up"));
        KeybindDataManager.RegisterKeyAction("ghost.face_down", () => HandleInput("ghost.face_down"));
        KeybindDataManager.RegisterKeyAction("ghost.face_left", () => HandleInput("ghost.face_left"));
        KeybindDataManager.RegisterKeyAction("ghost.face_right", () => HandleInput("ghost.face_right"));
        KeybindDataManager.RegisterKeyAction("ghost.change_ghost", () => HandleInput("ghost.change_ghost"));
    }

    private void UnregisterKeyActions()
    {
        KeybindDataManager.UnregisterKeyAction("ghost.face_up", () => HandleInput("ghost.face_up"));
        KeybindDataManager.UnregisterKeyAction("ghost.face_down", () => HandleInput("ghost.face_down"));
        KeybindDataManager.UnregisterKeyAction("ghost.face_left", () => HandleInput("ghost.face_left"));
        KeybindDataManager.UnregisterKeyAction("ghost.face_right", () => HandleInput("ghost.face_right"));
        KeybindDataManager.UnregisterKeyAction("ghost.change_ghost", () => HandleInput("ghost.change_ghost"));
    }
}
