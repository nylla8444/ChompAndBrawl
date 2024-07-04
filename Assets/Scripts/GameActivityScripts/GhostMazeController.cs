using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GhostMazeController : MonoBehaviour
{
    [Header("Ghost Objects")]
    [SerializeField] private GameObject blinky;
    [SerializeField] private GameObject clyde;
    [SerializeField] private GameObject inky;
    [SerializeField] private GameObject pinky;

    [Header("Ghost Default Speeds")]
    [SerializeField] private float blinkyDefaultSpeed;
    [SerializeField] private float clydeDefaultSpeed;
    [SerializeField] private float inkyDefaultSpeed;
    [SerializeField] private float pinkyDefaultSpeed;

    [Header("Ghost Spawnpoints")]
    [SerializeField] private Transform blinkySpawn;
    [SerializeField] private Transform clydeSpawn;
    [SerializeField] private Transform inkySpawn;
    [SerializeField] private Transform pinkySpawn;

    [Header("Ghost Animators")]
    [SerializeField] private Animator blinkyAnimator;
    [SerializeField] private Animator clydeAnimator;
    [SerializeField] private Animator inkyAnimator;
    [SerializeField] private Animator pinkyAnimator;
    
    [Header("Ghost Miscellaneous")]
    [SerializeField] private LayerMask collisionLayer;
    
    [Header("Ghost Effect Item Info")]
    [SerializeField] private float speedMultiplierIncrease;
    [Space(4)]
    [SerializeField] private float boostItemUseTime;
    [SerializeField] private float boostItemCooldown;
    [SerializeField] private float changeCooldown;
    [Space(4)]
    [SerializeField] private Text boostItemCooldownText;
    [SerializeField] private Text changeCooldownText;
    
    private float lastDefaultTime_CD = -Mathf.Infinity;
    private float lastChangeTime_CD = -Mathf.Infinity;

    [Space(12)]
    [SerializeField] private bool hasMazeStarted = false;
    private bool isRunning = false;
    private bool queuedGhostSwitch = false;

    // Variables for player controlling ghost
    private GameObject onCtrl_ghost;
    private Animator onCtrl_animator;
    private Vector2 onCtrl_direction;
    private Vector2 onCtrl_targetPosition;
    private Vector2 onCtrl_queuedDirection;
    private float onCtrl_defaultSpeed;

    // Variables for AI move control ghost
    private Dictionary<string, bool> onAuto_hasReachedTarget = new Dictionary<string, bool>()
    {
        { "blinky", false }, { "clyde", false }, { "inky", false }, { "pinky", false }
    };
    private Dictionary<string, Vector2> onAuto_targetPositions = new Dictionary<string, Vector2>()
    {
        { "blinky", Vector2.zero }, { "clyde", Vector2.zero }, { "inky", Vector2.zero }, { "pinky", Vector2.zero }
    };
    private Dictionary<string, Vector2> onAuto_directions = new Dictionary<string, Vector2>()
    {
        { "blinky", Vector2.zero }, { "clyde", Vector2.zero }, { "inky", Vector2.zero }, { "pinky", Vector2.zero }
    };
    private Dictionary<string, Queue<Vector2>> onAuto_recentTiles = new Dictionary<string, Queue<Vector2>>()
    {
        { "blinky", new Queue<Vector2>() }, { "clyde", new Queue<Vector2>() }, { "inky", new Queue<Vector2>() }, { "pinky", new Queue<Vector2>() },
    };
    private const float TILE_SIZE = 0.16f;
    private const float TILE_OFFSET = 0.08f;
    private const int MAX_RECENT_TILES = 3;
    private const float FEIGNING_IGNORANCE_DISTANCE = 8f;
    private const float WHIMSICAL_DISTANCE = 2f;
    private const float AMBUSHER_DISTANCE = 4f;

    private List<string> aliveGhosts;
    private string lastControllingGhost;


    private void Start()
    {
        InitializeGhosts();
        RegisterKeyActions();
    }

    private void OnDestroy()
    {
        UnregisterKeyActions();
    }

    public void StartGhostController(bool triggerValue)
    {
        hasMazeStarted = triggerValue;
    }

    private void RegisterKeyActions()
    {
        KeybindDataManager.RegisterKeyAction("ghost.face_up", () => HandleInput("ghost.face_up"));
        KeybindDataManager.RegisterKeyAction("ghost.face_down", () => HandleInput("ghost.face_down"));
        KeybindDataManager.RegisterKeyAction("ghost.face_left", () => HandleInput("ghost.face_left"));
        KeybindDataManager.RegisterKeyAction("ghost.face_right", () => HandleInput("ghost.face_right"));
        KeybindDataManager.RegisterKeyAction("ghost.change_ghost", () => HandleInput("ghost.change_ghost"));
        KeybindDataManager.RegisterKeyAction("ghost.use_boost_item", () => HandleInput("ghost.use_boost_item"));
    }

    private void UnregisterKeyActions()
    {
        KeybindDataManager.UnregisterKeyAction("ghost.face_up", () => HandleInput("ghost.face_up"));
        KeybindDataManager.UnregisterKeyAction("ghost.face_down", () => HandleInput("ghost.face_down"));
        KeybindDataManager.UnregisterKeyAction("ghost.face_left", () => HandleInput("ghost.face_left"));
        KeybindDataManager.UnregisterKeyAction("ghost.face_right", () => HandleInput("ghost.face_right"));
        KeybindDataManager.UnregisterKeyAction("ghost.change_ghost", () => HandleInput("ghost.change_ghost"));
        KeybindDataManager.UnregisterKeyAction("ghost.use_boost_item", () => HandleInput("ghost.use_boost_item"));
    }

    private void Update()
    {
        if (!hasMazeStarted) return;
        
        UpdateDisplayText();

        if (!isRunning && onCtrl_ghost != null && onCtrl_queuedDirection != Vector2.zero)
        {
            onCtrl_direction = onCtrl_queuedDirection;
            onCtrl_targetPosition = (Vector2)onCtrl_ghost.transform.position + onCtrl_direction * TILE_SIZE;
            isRunning = true;
            UpdateGhostAnimation(onCtrl_animator, onCtrl_direction, onCtrl_ghost.name);
        }

        // if (isRunning)
        // {
        //     MoveTowards();
        // }

        // MoveNonControlledGhosts();
    }

    private void FixedUpdate()
    {
        if (!hasMazeStarted) return;
        
        if (isRunning)
        {
            MoveTowards();
        }

        MoveNonControlledGhosts();
    }

    private void InitializeGhosts()
    {
        var ghostData = new Dictionary<string, (GameObject ghost, Transform spawnPoint, Animator animator)>
        {
            { "blinky", (blinky, blinkySpawn, blinkyAnimator) },
            { "clyde", (clyde, clydeSpawn, clydeAnimator) },
            { "inky", (inky, inkySpawn, inkyAnimator) },
            { "pinky", (pinky, pinkySpawn, pinkyAnimator) } 
        };
        
        GameData gameData = GameDataManager.LoadData();
        aliveGhosts = gameData.ghost_data.list_alive_ghosts;
        lastControllingGhost = gameData.ghost_data.current_controlling_ghost;

        ChangeGhost(lastControllingGhost);

        foreach (string ghostName in gameData.ghost_data.list_alive_ghosts)
        {
            if (!ghostData.TryGetValue(ghostName, out var ghostInfo))
            {
                Debug.LogWarning($"No game object found for {ghostName}");
                continue;
            }

            (GameObject _ghost, Transform _spawnPoint, Animator _animator) = ghostInfo; 
            
            var ghostPosition = gameData.ghost_data.ghost_positions.Find(pos => pos.ghost_name == _ghost.name);
            _ghost.transform.position = ghostPosition?.coordinate ?? _spawnPoint.position;
            _animator?.SetTrigger($"{_ghost.name}.rest");
        }
    }

    private void HandleInput(string action)
    {
        if (!hasMazeStarted) return;

        bool isControlInverted = GameDataManager.LoadData().ghost_data.is_control_inverted;
        
        Vector2 direction = action switch
        {
            "ghost.face_up" => !isControlInverted ? Vector2.up : Vector2.down,
            "ghost.face_down" => !isControlInverted ? Vector2.down : Vector2.up,
            "ghost.face_left" => !isControlInverted ? Vector2.left : Vector2.right,
            "ghost.face_right" => !isControlInverted ? Vector2.right : Vector2.left,
            _ => Vector2.zero
        };
        
        if (direction != Vector2.zero)
        {
            onCtrl_queuedDirection = direction;
            Debug.Log($"Ghost queued {direction}.");
        }
        else if (action == "ghost.change_ghost")
        {
            if (!isRunning)
            {
                OnUse_ChangeGhost();
            }
            else
            {
                queuedGhostSwitch = true;
            }
        }
        else if (action == "ghost.use_boost_item")
        {
            StartCoroutine(OnUse_BoostItem());
        }
    }

    private void MoveTowards()
    {
        Vector2 currentPosition = (Vector2)onCtrl_ghost.transform.position;

        if (currentPosition == onCtrl_targetPosition)
        {
            if (queuedGhostSwitch)
            {
                queuedGhostSwitch = false;
                OnUse_ChangeGhost();
                return;
            }

            if (onCtrl_queuedDirection != Vector2.zero && IsAbleToMoveTo(currentPosition + onCtrl_queuedDirection * TILE_SIZE, onCtrl_ghost))
            {
                onCtrl_direction = onCtrl_queuedDirection;
                onCtrl_targetPosition = currentPosition + onCtrl_direction * TILE_SIZE;
                onCtrl_queuedDirection = Vector2.zero;
                UpdateGhostAnimation(onCtrl_animator, onCtrl_direction, onCtrl_ghost.name);
            }
            else if (IsAbleToMoveTo(currentPosition + onCtrl_direction * TILE_SIZE, onCtrl_ghost))
            {
                onCtrl_targetPosition = currentPosition + onCtrl_direction * TILE_SIZE;
            }
            else
            {
                isRunning = false;
                if (queuedGhostSwitch)
                {
                    queuedGhostSwitch = false;
                    OnUse_ChangeGhost();
                }
                return;
            }
        }

        if (IsAbleToMoveTo(onCtrl_targetPosition, onCtrl_ghost))
        {
            float speedMutliplier = GameDataManager.LoadData().ghost_data.ghost_speed_multipliers.Find(mul => mul.ghost_name == onCtrl_ghost.name).speed_multiplier;
            Vector2 newPosition = Vector2.MoveTowards(currentPosition, onCtrl_targetPosition, (onCtrl_defaultSpeed * speedMutliplier) * Time.fixedDeltaTime);
            onCtrl_ghost.transform.position = newPosition;

            if (newPosition == onCtrl_targetPosition)
            {
                UpdateGhostPosition(onCtrl_ghost.name, newPosition);
            }
        }
        else
        {
            isRunning = false;
            if (queuedGhostSwitch)
            {
                queuedGhostSwitch = false;
                OnUse_ChangeGhost();
            }
        }
    }

    private bool IsAbleToMoveTo(Vector2 targetPosition, GameObject ghost)
    {
        BoxCollider2D ghostCollider = ghost.GetComponent<BoxCollider2D>();
        if (ghostCollider == null) return false;

        Bounds ghostBounds = ghostCollider.bounds;
        ghostBounds.center = targetPosition;
        Collider2D[] hits = Physics2D.OverlapBoxAll(ghostBounds.center, ghostBounds.size, 0f, collisionLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit != null && hit.gameObject != ghost && !hit.isTrigger)
            {
                return false;
            }
        }

        return true;
    }

    private void UpdateGhostPosition(string ghostName, Vector2 position)
    {
        GameData gameData = GameDataManager.LoadData();

        var ghostPosition = gameData.ghost_data.ghost_positions.Find(pos => pos.ghost_name == ghostName);
        if (ghostPosition == null)
        {
            ghostPosition = new GameData.GhostData.GhostPositions
            {
                ghost_name = ghostName,
                coordinate = position
            };
            
            gameData.ghost_data.ghost_positions.Add(ghostPosition);
        }
        else
        {
            ghostPosition.coordinate = position;
        }

        GameDataManager.SaveData(gameData);
    }

    private void UpdateGhostAnimation(Animator animator, Vector2 direction, string ghostName)
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
            Vector2 v when v == Vector2.up => (!hasPowerPellet) ? $"{ghostName}.normal_up" : $"{ghostName}.frighten_up",
            Vector2 v when v == Vector2.down => (!hasPowerPellet) ? $"{ghostName}.normal_down" : $"{ghostName}.frighten_down",
            Vector2 v when v == Vector2.left => (!hasPowerPellet) ? $"{ghostName}.normal_left" : $"{ghostName}.frighten_left",
            Vector2 v when v == Vector2.right => (!hasPowerPellet) ? $"{ghostName}.normal_right" : $"{ghostName}.frighten_right",
            _ => $"{ghostName}.rest"
        };

        animator.SetTrigger(animatorId);
    }

    private void ChangeGhost(string ghostName)
    {
        var ghostData = new Dictionary<string, (GameObject ghost, Animator animator, float speed)>
        {
            { "blinky", (blinky, blinkyAnimator, blinkyDefaultSpeed) },
            { "clyde", (clyde, clydeAnimator, clydeDefaultSpeed) },
            { "inky", (inky, inkyAnimator, inkyDefaultSpeed) },
            { "pinky", (pinky, pinkyAnimator, pinkyDefaultSpeed) }
        };

        if (!ghostData.TryGetValue(ghostName, out var ghostInfo))
        {
            onCtrl_ghost = null;
        }

        (onCtrl_ghost, onCtrl_animator, onCtrl_defaultSpeed) = ghostInfo;

        if (onCtrl_ghost != null)
        {
            GameData gameData = GameDataManager.LoadData();
            gameData.ghost_data.current_controlling_ghost = ghostName;

            var ghostPosition = gameData.ghost_data.ghost_positions
                .Find(pos => pos.ghost_name == ghostName);
            onCtrl_ghost.transform.position = GetTileCenter(ghostPosition?.coordinate ?? onCtrl_ghost.transform.position);

            onCtrl_direction = Vector2.zero;
            onCtrl_queuedDirection = Vector2.zero;

            onCtrl_animator.SetTrigger($"{onCtrl_ghost.name}.rest");

            GameDataManager.SaveData(gameData);
            isRunning = false;
        }

        lastControllingGhost = ghostName;
    }

    /*********************************************************************/
    //                            Item Use
    /*********************************************************************/

    private void OnUse_ChangeGhost()
    {
        if (aliveGhosts.Count == 0 || Time.time - lastChangeTime_CD < changeCooldown)
        {
            Debug.Log("Switch ghost cooldown in progress.");
            return;
        }

        int currentCharacterIndex = aliveGhosts.IndexOf(lastControllingGhost);
        currentCharacterIndex = (currentCharacterIndex + 1) % aliveGhosts.Count;
        
        string nextGhostName = aliveGhosts[currentCharacterIndex];
        ChangeGhost(nextGhostName);

        lastChangeTime_CD = Time.time;
        
        if (queuedGhostSwitch)
        {
            queuedGhostSwitch = false;
            OnUse_ChangeGhost();
        }
    }

    private IEnumerator OnUse_BoostItem()
    {
        if (Time.time - lastDefaultTime_CD < boostItemCooldown)
        {
            Debug.Log("Speed boost cooldown in progress.");
            yield break;
        }
        
        lastDefaultTime_CD = Time.time;

        IncreaseSpeedMultiplier(speedMultiplierIncrease);
        yield return new WaitForSeconds(boostItemUseTime);
        IncreaseSpeedMultiplier(-speedMultiplierIncrease);
    }

    private void IncreaseSpeedMultiplier(float increase)
    {
        GameData gameData = GameDataManager.LoadData();
        var ghostSpeedMultiplier = gameData.ghost_data.ghost_speed_multipliers.Find(mul => mul.ghost_name == onCtrl_ghost.name);

        if (ghostSpeedMultiplier != null)
        {
            ghostSpeedMultiplier.speed_multiplier += increase;
        }

        GameDataManager.SaveData(gameData);
    }

    private void UpdateDisplayText()
    {
        float boostCooldownRemaining = Mathf.Max(0f, boostItemCooldown - (Time.time - lastDefaultTime_CD));
        boostItemCooldownText.text = boostCooldownRemaining > 0 ? $"{boostCooldownRemaining:F1}s" : "";
        
        float changeCooldownRemaining = Mathf.Max(0f, changeCooldown - (Time.time - lastChangeTime_CD));
        changeCooldownText.text = changeCooldownRemaining > 0 ? $"{changeCooldownRemaining:F1}s" : "";
    }

    /*********************************************************************/
    //                       Ghost AI Movement
    /*********************************************************************/

    private void MoveNonControlledGhosts()
    {
        foreach (string ghostName in aliveGhosts)
        {
            if (ghostName != onCtrl_ghost.name)
            {
                switch (ghostName)
                {
                    case "blinky": AutoMoveBlinky(); break;
                    case "clyde": AutoMoveClyde(); break;
                    case "inky": AutoMoveInky(); break;
                    case "pinky": AutoMovePinky(); break;
                }
            }
        }
    }

    private void AutoMoveBlinky()
    {
        GameObject onAuto_ghost = blinky;
        
        if ((Vector2)onAuto_ghost.transform.position == GetTileCenter((Vector2)onAuto_ghost.transform.position))
        {
            if (onAuto_hasReachedTarget[onAuto_ghost.name])
            {
                onAuto_hasReachedTarget[onAuto_ghost.name] = false;
                GameData gameData = GameDataManager.LoadData();
                
                Animator onAuto_animator = blinkyAnimator;
                Vector2 pacmanPosition = gameData.pacman_data.coordinate;
                
                onAuto_directions[onAuto_ghost.name] = GetValidDirection(onAuto_ghost.transform.position, onAuto_directions[onAuto_ghost.name], onAuto_ghost, pacmanPosition, true);   
                onAuto_targetPositions[onAuto_ghost.name] = (Vector2)onAuto_ghost.transform.position + onAuto_directions[onAuto_ghost.name] * 0.16f;             

                UpdateGhostAnimation(onAuto_animator, onAuto_directions[onAuto_ghost.name], onAuto_ghost.name);
                
                Queue<Vector2> recentTiles = onAuto_recentTiles[onAuto_ghost.name];
                if (recentTiles.Count >= MAX_RECENT_TILES)
                {
                    recentTiles.Dequeue();
                }
                recentTiles.Enqueue(GetTileCenter((Vector2)onAuto_ghost.transform.position));
            }
        }

        if (!onAuto_hasReachedTarget[onAuto_ghost.name] && IsAbleToMoveTo(onAuto_targetPositions[onAuto_ghost.name], onAuto_ghost))
        {
            if (!onAuto_hasReachedTarget[onAuto_ghost.name])
            {
                float speedMultiplier = GameDataManager.LoadData().ghost_data.ghost_speed_multipliers.Find(mul => mul.ghost_name == onAuto_ghost.name).speed_multiplier;
                Vector2 newPosition = Vector2.MoveTowards(onAuto_ghost.transform.position, onAuto_targetPositions[onAuto_ghost.name], (blinkyDefaultSpeed * speedMultiplier) * Time.deltaTime);
                onAuto_ghost.transform.position = newPosition;

                if ((Vector2)onAuto_ghost.transform.position == GetTileCenter((Vector2)onAuto_ghost.transform.position))
                {
                    UpdateGhostPosition(onAuto_ghost.name, newPosition);
                    onAuto_hasReachedTarget[onAuto_ghost.name] = true;
                }
            }
        }
        else
        {
            onAuto_ghost.transform.position = GetTileCenter((Vector2)onAuto_ghost.transform.position);
            onAuto_hasReachedTarget[onAuto_ghost.name] = true;
        }
    }

    private void AutoMoveClyde()
    {
        GameObject onAuto_ghost = clyde;
        
        if ((Vector2)onAuto_ghost.transform.position == GetTileCenter((Vector2)onAuto_ghost.transform.position))
        {
            if (onAuto_hasReachedTarget[onAuto_ghost.name])
            {
                onAuto_hasReachedTarget[onAuto_ghost.name] = false;
                GameData gameData = GameDataManager.LoadData();
                
                Animator onAuto_animator = clydeAnimator;
                Vector2 pacmanPosition = gameData.pacman_data.coordinate;
                float distanceToPacman = Vector2.Distance(onAuto_ghost.transform.position, pacmanPosition);
                
                onAuto_directions[onAuto_ghost.name] = GetValidDirection(onAuto_ghost.transform.position, onAuto_directions[onAuto_ghost.name], onAuto_ghost, pacmanPosition, (distanceToPacman > FEIGNING_IGNORANCE_DISTANCE * 0.16f));
                onAuto_targetPositions[onAuto_ghost.name] = (Vector2)onAuto_ghost.transform.position + onAuto_directions[onAuto_ghost.name] * 0.16f;
                
                UpdateGhostAnimation(onAuto_animator, onAuto_directions[onAuto_ghost.name], onAuto_ghost.name);

                Queue<Vector2> recentTiles = onAuto_recentTiles[onAuto_ghost.name];
                if (recentTiles.Count >= MAX_RECENT_TILES)
                {
                    recentTiles.Dequeue();
                }
                recentTiles.Enqueue(GetTileCenter((Vector2)onAuto_ghost.transform.position));
            }
        }

        if (!onAuto_hasReachedTarget[onAuto_ghost.name] && IsAbleToMoveTo(onAuto_targetPositions[onAuto_ghost.name], onAuto_ghost))
        {
            float speedMultiplier = GameDataManager.LoadData().ghost_data.ghost_speed_multipliers.Find(mul => mul.ghost_name == onAuto_ghost.name).speed_multiplier;
            Vector2 newPosition = Vector2.MoveTowards(onAuto_ghost.transform.position, onAuto_targetPositions[onAuto_ghost.name], (clydeDefaultSpeed * speedMultiplier) * Time.deltaTime);
            onAuto_ghost.transform.position = newPosition;

            if ((Vector2)onAuto_ghost.transform.position == GetTileCenter((Vector2)onAuto_ghost.transform.position))
            {
                UpdateGhostPosition(onAuto_ghost.name, newPosition);
                onAuto_hasReachedTarget[onAuto_ghost.name] = true;
            }
        }
        else
        {
            onAuto_ghost.transform.position = GetTileCenter((Vector2)onAuto_ghost.transform.position);
            onAuto_hasReachedTarget[onAuto_ghost.name] = true;
        }
    }

    private void AutoMoveInky()
    {
        GameObject onAuto_ghost = inky;

        if ((Vector2)onAuto_ghost.transform.position == GetTileCenter((Vector2)onAuto_ghost.transform.position))
        {
            if (onAuto_hasReachedTarget[onAuto_ghost.name])
            {
                onAuto_hasReachedTarget[onAuto_ghost.name] = false;
                GameData gameData = GameDataManager.LoadData();
                
                Animator onAuto_animator = inkyAnimator;
                Vector2 pacmanPosition = gameData.pacman_data.coordinate;
                Vector2 pacmanDirection = gameData.pacman_data.direction;
                Vector2 inkyPosition = gameData.ghost_data.ghost_positions.Find(pos => pos.ghost_name == onAuto_ghost.name)?.coordinate ?? Vector2.zero;

                Vector2 offsetFromPacman = pacmanPosition + pacmanDirection * (WHIMSICAL_DISTANCE * 0.16f);
                Vector2 targetTile = offsetFromPacman + (offsetFromPacman - inkyPosition);
                targetTile = GetNearestNonCollisionTile(targetTile, onAuto_ghost);

                onAuto_directions[onAuto_ghost.name] = GetValidDirection(onAuto_ghost.transform.position, onAuto_directions[onAuto_ghost.name], onAuto_ghost, targetTile, true);
                onAuto_targetPositions[onAuto_ghost.name] = (Vector2)onAuto_ghost.transform.position + onAuto_directions[onAuto_ghost.name] * 0.16f;
                
                UpdateGhostAnimation(onAuto_animator, onAuto_directions[onAuto_ghost.name], onAuto_ghost.name);

                Queue<Vector2> recentTiles = onAuto_recentTiles[onAuto_ghost.name];
                if (recentTiles.Count >= MAX_RECENT_TILES)
                {
                    recentTiles.Dequeue();
                }
                recentTiles.Enqueue(GetTileCenter((Vector2)onAuto_ghost.transform.position));
            }
        }

        if (!onAuto_hasReachedTarget[onAuto_ghost.name] && IsAbleToMoveTo(onAuto_targetPositions[onAuto_ghost.name], onAuto_ghost))
        {
            float speedMultiplier = GameDataManager.LoadData().ghost_data.ghost_speed_multipliers.Find(mul => mul.ghost_name == onAuto_ghost.name).speed_multiplier;
            Vector2 newPosition = Vector2.MoveTowards(onAuto_ghost.transform.position, onAuto_targetPositions[onAuto_ghost.name], (inkyDefaultSpeed * speedMultiplier) * Time.deltaTime);
            onAuto_ghost.transform.position = newPosition;

            if ((Vector2)onAuto_ghost.transform.position == GetTileCenter((Vector2)onAuto_ghost.transform.position))
            {
                UpdateGhostPosition(onAuto_ghost.name, newPosition);
                onAuto_hasReachedTarget[onAuto_ghost.name] = true;
            }
        }
        else
        {
            onAuto_ghost.transform.position = GetTileCenter((Vector2)onAuto_ghost.transform.position);
            onAuto_hasReachedTarget[onAuto_ghost.name] = true;
        }
    }

    private void AutoMovePinky()
    {
        GameObject onAuto_ghost = pinky;

        if ((Vector2)onAuto_ghost.transform.position == GetTileCenter((Vector2)onAuto_ghost.transform.position))
        {
            if (onAuto_hasReachedTarget[onAuto_ghost.name])
            {
                onAuto_hasReachedTarget[onAuto_ghost.name] = false;
                GameData gameData = GameDataManager.LoadData();
                
                Animator onAuto_animator = pinkyAnimator;
                Vector2 pacmanPosition = gameData.pacman_data.coordinate;
                Vector2 pacmanDirection = gameData.pacman_data.direction;

                Vector2 targetTile = pacmanPosition + pacmanDirection * (AMBUSHER_DISTANCE * 0.16f);
                targetTile = GetNearestNonCollisionTile(targetTile, onAuto_ghost);

                onAuto_directions[onAuto_ghost.name] = GetValidDirection(onAuto_ghost.transform.position, onAuto_directions[onAuto_ghost.name], onAuto_ghost, targetTile, true);
                onAuto_targetPositions[onAuto_ghost.name] = (Vector2)onAuto_ghost.transform.position + onAuto_directions[onAuto_ghost.name] * 0.16f;
                
                UpdateGhostAnimation(onAuto_animator, onAuto_directions[onAuto_ghost.name], onAuto_ghost.name);

                Queue<Vector2> recentTiles = onAuto_recentTiles[onAuto_ghost.name];
                if (recentTiles.Count >= MAX_RECENT_TILES)
                {
                    recentTiles.Dequeue();
                }
                recentTiles.Enqueue(GetTileCenter((Vector2)onAuto_ghost.transform.position));
            }
        }

        if (!onAuto_hasReachedTarget[onAuto_ghost.name] && IsAbleToMoveTo(onAuto_targetPositions[onAuto_ghost.name], onAuto_ghost))
        {
            float speedMultiplier = GameDataManager.LoadData().ghost_data.ghost_speed_multipliers.Find(mul => mul.ghost_name == onAuto_ghost.name).speed_multiplier;
            Vector2 newPosition = Vector2.MoveTowards(onAuto_ghost.transform.position, onAuto_targetPositions[onAuto_ghost.name], (pinkyDefaultSpeed * speedMultiplier) * Time.deltaTime);
            onAuto_ghost.transform.position = newPosition;

            if ((Vector2)onAuto_ghost.transform.position == GetTileCenter((Vector2)onAuto_ghost.transform.position))
            {
                UpdateGhostPosition(onAuto_ghost.name, newPosition);
                onAuto_hasReachedTarget[onAuto_ghost.name] = true;
            }
        }
        else
        {
            onAuto_ghost.transform.position = GetTileCenter((Vector2)onAuto_ghost.transform.position);
            onAuto_hasReachedTarget[onAuto_ghost.name] = true;
        }
    }

    private Vector2 GetTileCenter(Vector2 position)
    {
        float x = Mathf.Round((position.x - TILE_OFFSET) / TILE_SIZE) * TILE_SIZE + TILE_OFFSET;
        float y = Mathf.Round((position.y - TILE_OFFSET) / TILE_SIZE) * TILE_SIZE + TILE_OFFSET;
        return new Vector2(x, y);
    }

    private Vector2 GetValidDirection(Vector2 currentPosition, Vector2 currentDirection, GameObject ghost, Vector2 targetPosition, bool isDirectTargeting)
    {
        bool hasPowerPellet = GameDataManager.LoadData().pacman_data.has_power_pellet;

        Vector2[] possibleDirections = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        Vector2 reverseDirection = -currentDirection;

        Vector2 bestDirection = Vector2.zero;
        float bestDistance = hasPowerPellet ? 0f : float.MaxValue;

        foreach (Vector2 direction in possibleDirections)
        {
            if (direction == reverseDirection) continue;

            Vector2 newPosition = currentPosition + direction * TILE_SIZE;
            if (IsAbleToMoveTo(newPosition, ghost))
            {
                float distanceToTarget = Vector2.Distance(newPosition, targetPosition);
                if ((hasPowerPellet && distanceToTarget > bestDistance) || (!hasPowerPellet && distanceToTarget < bestDistance))
                {
                    bestDistance = distanceToTarget;
                    bestDirection = direction;
                }
            }
        }

        if (bestDirection == Vector2.zero)
        {
            bestDirection = reverseDirection;
        }

        if (!isDirectTargeting && bestDirection == currentDirection && IsAbleToMoveTo(currentPosition + bestDirection * TILE_SIZE, ghost))
        {
            return bestDirection;
        }

        return bestDirection;
    }

    private Vector2 GetNearestNonCollisionTile(Vector2 targetTile, GameObject ghost)
    {
        if (IsAbleToMoveTo(targetTile, ghost))
        {
            return targetTile;
        }

        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        float[] offsets = { TILE_SIZE, TILE_SIZE * 2, TILE_SIZE * 3 };

        foreach (float offset in offsets)
        {
            foreach (Vector2 direction in directions)
            {
                Vector2 checkTile = targetTile + direction * offset;
                if (IsAbleToMoveTo(checkTile, ghost))
                {
                    return checkTile;
                }
            }
        }

        return targetTile;
    }
}
