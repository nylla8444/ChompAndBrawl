using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class GhostMazeController : MonoBehaviour
{
    [SerializeField] private bool isMazeStarted = false;

    [Header("===Ghost Objects===")]
    [SerializeField] private GameObject blinky;
    [SerializeField] private GameObject clyde;
    [SerializeField] private GameObject inky;
    [SerializeField] private GameObject pinky;

    [Header("===Ghost Default Speeds===")]
    [SerializeField] private float blinkyDefaultSpeed;
    [SerializeField] private float clydeDefaultSpeed;
    [SerializeField] private float inkyDefaultSpeed;
    [SerializeField] private float pinkyDefaultSpeed;

    [Header("===Ghost Spawnpoints===")]
    [SerializeField] private Transform blinkySpawn;
    [SerializeField] private Transform clydeSpawn;
    [SerializeField] private Transform inkySpawn;
    [SerializeField] private Transform pinkySpawn;

    [Header("===Ghost Animators===")]
    [SerializeField] private Animator blinkyAnimator;
    [SerializeField] private Animator clydeAnimator;
    [SerializeField] private Animator inkyAnimator;
    [SerializeField] private Animator pinkyAnimator;
    
    [Header("===Ghost Miscellaneous===")]
    [SerializeField] private LayerMask collisionLayer;
    [SerializeField] private List<Image> ghostImages;
    [SerializeField] private List<Sprite> ghostEmptySprites;
    [SerializeField] private List<Sprite> ghostFullSprites;
    [SerializeField] private Image ghostOnControlImage;
    
    [Header("===Ghost Effect Item Info===")]
    [SerializeField] private EffectItemList effectItemList;
    [Space(4)]
    [SerializeField] private Text cdText_boost;
    [SerializeField] private Text cdText_changeGhost;
    [SerializeField] private Text cdText_effectItem;
    [SerializeField] private Image cdImage_boost;
    [SerializeField] private Image cdImage_changeGhost;
    [SerializeField] private Image cdImage_effectItem;
    [Space(4)]
    [SerializeField] private List<Sprite> changeGhostSprites;
    [SerializeField] private Image changeGhostIcon;
    [SerializeField] private Image effectItemIcon;
    [SerializeField] private Text effectItemText;
    [Space(4)]
    [SerializeField] private GameObject sparkElectricParticlePrefab;
    [SerializeField] private GameObject ghostElectrifiedOverlayPrefab;
    [SerializeField] private GameObject windBurstParticlePrefab;
    [SerializeField] private GameObject stickyGooPrefab;
    [SerializeField] private GameObject gooOverlayPrefab;
    [Space(4)]
    [SerializeField] private GameObject startParticlePrefab;
    [SerializeField] private ParticleEffectMazeHandler particleEffectMazeHandler;
    
    private float lastCd_changeGhost = -Mathf.Infinity;
    private float lastCd_boost = -Mathf.Infinity;
    private float lastCd_blinkyEffectItem = -Mathf.Infinity;
    private float lastCd_clydeEffectItem = -Mathf.Infinity;
    private float lastCd_inkyEffectItem = -Mathf.Infinity;
    private float lastCd_pinkyEffectItem = -Mathf.Infinity;

    // Variables for player controlling ghost
    private GameObject onCtrl_ghost;
    private Animator onCtrl_animator;
    private Vector2 onCtrl_direction;
    private Vector2 onCtrl_targetPosition;
    private Vector2 onCtrl_queuedDirection;
    private float onCtrl_defaultSpeed;
    private bool onCtrl_isRunning = false;

    // Variables for AI move control ghost
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
    private Dictionary<string, bool> onAuto_isRunning = new Dictionary<string, bool>()
    {
        { "blinky", false }, { "clyde", false }, { "inky", false }, { "pinky", false }
    };
    
    // Variables for general data
    private Dictionary<string, float> ghost_speedMultiplier = new Dictionary<string, float>()
    {
        { "blinky", 0f }, { "clyde", 0f }, { "inky", 0f }, { "pinky", 0f }
    };
    private Dictionary<string, float> ghost_windBurstSpeedAffect = new Dictionary<string, float>()
    {
        { "blinky", 0f }, { "clyde", 0f }, { "inky", 0f }, { "pinky", 0f }
    };
    
    private List<string> ghostNames = new List<string> { "blinky", "clyde", "inky", "pinky" };
    private const float FEIGNING_IGNORANCE_DISTANCE = 8f;
    private const float WHIMSICAL_DISTANCE = 2f;
    private const float AMBUSHER_DISTANCE = 4f;
    private const float TILE_SIZE = 0.16f;
    private const float TILE_OFFSET = 0.08f;
    private const int MAX_RECENT_TILES = 3;

    private List<string> aliveGhosts;
    private string lastControllingGhost;

    /*********************************************************************/
    //
    //                            General
    //
    /*********************************************************************/

    private void Start()
    {
        InitializeGhosts();
    }

    private void OnDestroy()
    {
        KeybindDataManager.ResetKeyActions();
    }

    public void StartGhostController(bool triggerValue)
    {
        isMazeStarted = triggerValue;
        if (isMazeStarted) 
        {
            RegisterKeyActions();
            foreach (string ghostName in aliveGhosts)
            {
                StartCoroutine(MoveGhostCoroutine(ghostName));
            }
        }
    }

    private void RegisterKeyActions()
    {
        KeybindDataManager.RegisterKeyAction("ghost.face_up", () => HandleInput("ghost.face_up"));
        KeybindDataManager.RegisterKeyAction("ghost.face_down", () => HandleInput("ghost.face_down"));
        KeybindDataManager.RegisterKeyAction("ghost.face_left", () => HandleInput("ghost.face_left"));
        KeybindDataManager.RegisterKeyAction("ghost.face_right", () => HandleInput("ghost.face_right"));
        KeybindDataManager.RegisterKeyAction("ghost.change_ghost", () => HandleInput("ghost.change_ghost"));
        KeybindDataManager.RegisterKeyAction("ghost.use_boost_item", () => HandleInput("ghost.use_boost_item"));
        KeybindDataManager.RegisterKeyAction("ghost.use_unique_item", () => HandleInput("ghost.use_unique_item"));
    }

    private void Update()
    {
        if (!isMazeStarted) return;

        if (!onCtrl_isRunning && onCtrl_ghost != null && onCtrl_queuedDirection != Vector2.zero)
        {
            onCtrl_direction = onCtrl_queuedDirection;
            onCtrl_targetPosition = (Vector2)onCtrl_ghost.transform.position + onCtrl_direction * TILE_SIZE;
            onCtrl_isRunning = true;
            
            UpdateGhostAnimation(onCtrl_animator, onCtrl_direction, onCtrl_ghost.name);
            StartCoroutine(MoveControllingGhost());
        }
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
        
        aliveGhosts = IngameDataManager.LoadSpecificData<List<string>>("ghost_data.list_alive");
        lastControllingGhost = aliveGhosts[0];
        
        int currentGhostIndex = (aliveGhosts.IndexOf(lastControllingGhost)) % aliveGhosts.Count;
        InEffect_ChangeGhost(lastControllingGhost, currentGhostIndex);

        foreach (string ghostName in aliveGhosts)
        {
            if (!ghostData.TryGetValue(ghostName, out var ghostInfo))
            {
                Debug.LogWarning($"No game object found for {ghostName}");
                continue;
            }

            (GameObject _ghost, Transform _spawnPoint, Animator _animator) = ghostInfo;
            
            _ghost.transform.position = _spawnPoint.position;
            UpdateGhostPosition(ghostName, _spawnPoint.position);
            _animator?.SetTrigger($"{_ghost.name}.rest");
        }

        UpdateGhostDisplay();
        SetToNormalData();

        Timing.RunCoroutine(UpdateTextDisplay());
        Timing.RunCoroutine(UpdateSavedSpeed());
    }

    private void UpdateGhostDisplay()
    {
        for (int i = 0; i < ghostNames.Count; i++)
        {
            ghostImages[i].sprite = (aliveGhosts.Contains(ghostNames[i])) ? ghostFullSprites[i] : ghostEmptySprites[i];
        }
    }

    private void SetToNormalData()
    {
        IngameDataManager.SaveSpecificData("ghost_data.current_fighting", "");
        IngameDataManager.SaveSpecificData("ghost_data.vision_multiplier", 1.0f);
        IngameDataManager.SaveSpecificData("ghost_data.is_control_inverted", false);
        foreach (string ghostName in ghostNames)
        {
            IngameDataManager.SaveSpecificListData("ghost_data.ghost_single_info", ghostName, "speed_multiplier", 1.0f);
            IngameDataManager.SaveSpecificListData("ghost_data.ghost_single_info", ghostName, "wind_burst_speed_affect", 1.0f);
            IngameDataManager.SaveSpecificListData("ghost_data.ghost_single_info", ghostName, "is_paralyzed", false);

            List<string> _affectedItems = IngameDataManager.LoadSpecificListData<List<string>>("ghost_data.ghost_single_info", ghostName, "affected_items");
            _affectedItems.Clear();
            IngameDataManager.SaveSpecificListData("ghost_data.ghost_single_info", ghostName, "affected_items", _affectedItems);
        }

        int diedGhostCount = 4 - aliveGhosts.Count;
        const float INCREASE_DEFAULT_SPEED = 0.075f; 
    
        blinkyDefaultSpeed = blinkyDefaultSpeed + (blinkyDefaultSpeed * (INCREASE_DEFAULT_SPEED * diedGhostCount));
        clydeDefaultSpeed = clydeDefaultSpeed + (clydeDefaultSpeed * (INCREASE_DEFAULT_SPEED * diedGhostCount));
        inkyDefaultSpeed = inkyDefaultSpeed + (inkyDefaultSpeed * (INCREASE_DEFAULT_SPEED * diedGhostCount));
        pinkyDefaultSpeed = pinkyDefaultSpeed + (pinkyDefaultSpeed * (INCREASE_DEFAULT_SPEED * diedGhostCount));
    }

    public void SetSlowSpeed()
    {
        foreach (string ghostName in ghostNames)
        {
            IngameDataManager.SaveSpecificListData("ghost_data.ghost_single_info", ghostName, "speed_multiplier", 0.02f);
        }
    }

    private IEnumerator<float> UpdateSavedSpeed()
    {
        while (true)
        {
            foreach (string ghostName in ghostNames)
            {
                float speedMultiplier = IngameDataManager.LoadSpecificListData<float>("ghost_data.ghost_single_info", ghostName, "speed_multiplier");
                float windBurstSpeedAffect = IngameDataManager.LoadSpecificListData<float>("ghost_data.ghost_single_info", ghostName, "wind_burst_speed_affect");

                if (ghost_speedMultiplier[ghostName] != speedMultiplier)
                {
                    ghost_speedMultiplier[ghostName] = speedMultiplier;
                }

                if (ghost_windBurstSpeedAffect[ghostName] != windBurstSpeedAffect)
                {
                    ghost_windBurstSpeedAffect[ghostName] = windBurstSpeedAffect;
                }
            }

            yield return Timing.WaitForSeconds(0.1f);
        }
    }

    private IEnumerator MoveControllingGhost()
    {
        while (onCtrl_isRunning)
        {
            Vector2 currentPosition = (Vector2)onCtrl_ghost.transform.position;

            if (currentPosition == onCtrl_targetPosition)
            {
                if (onCtrl_queuedDirection != Vector2.zero && IsAbleToMoveTo(currentPosition + onCtrl_queuedDirection * TILE_SIZE, onCtrl_ghost))
                {
                    onCtrl_direction = onCtrl_queuedDirection;
                    onCtrl_targetPosition = currentPosition + onCtrl_direction * TILE_SIZE;
                    onCtrl_queuedDirection = Vector2.zero;
                }
                else if (IsAbleToMoveTo(currentPosition + onCtrl_direction * TILE_SIZE, onCtrl_ghost))
                {
                    onCtrl_targetPosition = currentPosition + onCtrl_direction * TILE_SIZE;
                }
                else
                {
                    onCtrl_isRunning = false;
                    yield break;
                }

                UpdateGhostAnimation(onCtrl_animator, onCtrl_direction, onCtrl_ghost.name);
                UpdateGhostDirection(onCtrl_ghost.name, onCtrl_direction);
            }

            if (IsAbleToMoveTo(onCtrl_targetPosition, onCtrl_ghost))
            {
                Vector2 newPosition = Vector2.MoveTowards(currentPosition, onCtrl_targetPosition, (onCtrl_defaultSpeed * ghost_speedMultiplier[onCtrl_ghost.name] * ghost_windBurstSpeedAffect[onCtrl_ghost.name]) * Time.deltaTime);
                onCtrl_ghost.transform.position = newPosition;

                if (newPosition == onCtrl_targetPosition)
                {
                    UpdateGhostPosition(onCtrl_ghost.name, newPosition);
                }
            }
            else
            {
                onCtrl_isRunning = false;
                yield break;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    private bool IsAbleToMoveTo(Vector2 targetPosition, GameObject ghost)
    {
        BoxCollider2D ghostCollider = ghost.GetComponent<BoxCollider2D>();
        if (ghostCollider == null) return false;

        RaycastHit2D hit = Physics2D.BoxCast(targetPosition, ghostCollider.bounds.size, 0f, Vector2.zero, 0f, collisionLayer);
        return hit.collider == null || hit.collider.gameObject == ghost || hit.collider.isTrigger;
    }

    private void UpdateGhostPosition(string ghostName, Vector2 _position)
    {
        IngameDataManager.SaveSpecificListData("ghost_data.ghost_single_info", ghostName, "coordinate", _position);
    }

    private void UpdateGhostDirection(string ghostName, Vector2 _direction)
    {
        IngameDataManager.SaveSpecificListData("ghost_data.ghost_single_info", ghostName, "direction", _direction);
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

        bool _pacman_hasPowerPellet = IngameDataManager.LoadSpecificData<bool>("pacman_data.has_power_pellet");

        string animatorId = direction switch
        {
            Vector2 v when v == Vector2.up => (!_pacman_hasPowerPellet) ? $"{ghostName}.normal_up" : $"{ghostName}.frighten_up",
            Vector2 v when v == Vector2.down => (!_pacman_hasPowerPellet) ? $"{ghostName}.normal_down" : $"{ghostName}.frighten_down",
            Vector2 v when v == Vector2.left => (!_pacman_hasPowerPellet) ? $"{ghostName}.normal_left" : $"{ghostName}.frighten_left",
            Vector2 v when v == Vector2.right => (!_pacman_hasPowerPellet) ? $"{ghostName}.normal_right" : $"{ghostName}.frighten_right",
            _ => $"{ghostName}.rest"
        };

        animator.SetTrigger(animatorId);
    }

    private void HandleInput(string action)
    {
        if (!isMazeStarted) return;

        bool _ghost_isControlInverted = IngameDataManager.LoadSpecificData<bool>("ghost_data.is_control_inverted");
        
        Vector2 direction = action switch
        {
            "ghost.face_up" => !_ghost_isControlInverted ? Vector2.up : Vector2.down,
            "ghost.face_down" => !_ghost_isControlInverted ? Vector2.down : Vector2.up,
            "ghost.face_left" => !_ghost_isControlInverted ? Vector2.left : Vector2.right,
            "ghost.face_right" => !_ghost_isControlInverted ? Vector2.right : Vector2.left,
            _ => Vector2.zero
        };
        
        if (direction != Vector2.zero)
        {
            onCtrl_queuedDirection = direction;
            return;
        }
        
        switch (action)
        {
            case "ghost.change_ghost":
                OnUse_ChangeGhostItem();
                break;

            case "ghost.use_boost_item": 
                StartCoroutine(OnUse_BoostItem()); 
                break;

            case "ghost.use_unique_item": 
                OnUse_EffectItem(); 
                break;
        }
    }

    /*********************************************************************/
    //
    //                        Effect Items Use
    //
    /*********************************************************************/

    private void OnUse_ChangeGhostItem()
    {
        var item_changeGhost = effectItemList.effectItems.Find(item => item.name == "change_ghost");
        if (aliveGhosts.Count == 0 || Time.time - lastCd_changeGhost < item_changeGhost.cooldown)
        {
            Debug.Log("Switch ghost cooldown in progress.");
            return;
        }

        Timing.KillCoroutines($"{lastControllingGhost}");
        int currentGhostIndex = aliveGhosts.IndexOf(lastControllingGhost);
        int nextGhostIndex = (currentGhostIndex + 1) % aliveGhosts.Count;
        string nextGhostName = aliveGhosts[nextGhostIndex];

        InEffect_ChangeGhost(nextGhostName, nextGhostIndex);

        lastCd_changeGhost = Time.time;
    }

    private void InEffect_ChangeGhost(string ghostName, int nextGhostIndex)
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
            Vector2 _coordinate = IngameDataManager.LoadSpecificListData<Vector2>("ghost_data.ghost_single_info", ghostName, "coordinate");
            onCtrl_ghost.transform.position = GetTileCenter(_coordinate);

            onCtrl_direction = Vector2.zero;
            onCtrl_queuedDirection = Vector2.zero;

            onCtrl_animator.SetTrigger($"{onCtrl_ghost.name}.rest");

            IngameDataManager.SaveSpecificData("ghost_data.current_controlling", ghostName);
            onCtrl_isRunning = false;
            onAuto_isRunning[onCtrl_ghost.name] = false;
        }

        lastControllingGhost = ghostName;

        UpdateEffectItemDisplay(ghostName);
        UpdateGhostOnControlIcon(ghostName);
        UpdateChangeGhostIcon(nextGhostIndex);
    }

    public void DirectChangeGhost(string ghostName)
    {
        IngameDataManager.SaveSpecificData("ghost_data.current_controlling", ghostName);
    }

    private void UpdateGhostOnControlIcon(string ghostName)
    {
        int spriteIndex = ghostName switch
        {
            "blinky" => 0,
            "clyde" => 1,
            "inky" => 2,
            "pinky" => 3,
            _ => -1
        };

        ghostOnControlImage.sprite = ghostFullSprites[spriteIndex];
    }

    private void UpdateChangeGhostIcon(int ghostIndex)
    {
        int nextGhostIndex = (ghostIndex + 1) % aliveGhosts.Count;

        if (aliveGhosts.Count == 1)
        {
            nextGhostIndex = ghostIndex;
        }

        string nextGhostName = aliveGhosts[nextGhostIndex];
        int spriteIndex = nextGhostName switch
        {
            "blinky" => 0,
            "clyde" => 1,
            "inky" => 2,
            "pinky" => 3,
            _ => -1
        };

        if (spriteIndex >= 0 && spriteIndex < changeGhostSprites.Count)
        {
            changeGhostIcon.sprite = changeGhostSprites[spriteIndex];
        }
    }

    private IEnumerator OnUse_BoostItem()
    {
        var item_boost = effectItemList.effectItems.Find(item => item.name == "boost");
        if (Time.time - lastCd_boost < item_boost.cooldown)
        {
            Debug.Log("Speed boost cooldown in progress.");
            yield break;
        }
        
        lastCd_boost = Time.time;
        string recentGhostControlling = onCtrl_ghost.name;
        float SPEED_MULTIPLIER_INCREASE = 0.8f;
        
        InEffect_Boost("add", recentGhostControlling, SPEED_MULTIPLIER_INCREASE);
        yield return new WaitForSeconds(item_boost.useTime);
        InEffect_Boost("remove", recentGhostControlling, -SPEED_MULTIPLIER_INCREASE);
    }

    private void InEffect_Boost(string listMode, string ghostName, float increase)
    {
        InOutAffectedItems(listMode, "boost", "ghost", ghostName);
        
        float _speedMultiplier = IngameDataManager.LoadSpecificListData<float>("ghost_data.ghost_single_info", ghostName, "speed_multiplier");
        _speedMultiplier += increase;

        IngameDataManager.SaveSpecificListData("ghost_data.ghost_single_info", ghostName, "speed_multiplier", _speedMultiplier);
    }

    private void UpdateEffectItemDisplay(string ghostName)
    {
        string _ownedEffectItem = IngameDataManager.LoadSpecificListData<string>("ghost_data.ghost_single_info", ghostName, "effect_item");
        if (System.String.IsNullOrEmpty(_ownedEffectItem)) return;

        var effectItem = effectItemList.effectItems.Find(item => item.name == _ownedEffectItem);
        if (effectItem == null) return;

        SetEffectItemDisplay(effectItem.name, effectItem.asItemSprite);
    }

    private void SetEffectItemDisplay(string name, Sprite sprite)
    {
        effectItemText.text = $"{name.Replace("_", " ")}";
        effectItemIcon.sprite = sprite;
    }

    private void OnUse_EffectItem()
    {
        string _ownedEffectItem = IngameDataManager.LoadSpecificListData<string>("ghost_data.ghost_single_info", onCtrl_ghost.name, "effect_item");
        float cooldown = effectItemList.effectItems.Find(item => item.name == _ownedEffectItem).cooldown;

        float lastCd = _ownedEffectItem switch
        {
            "imitate_speed" => lastCd_blinkyEffectItem,
            "electroshock" => lastCd_clydeEffectItem,
            "wind_burst" => lastCd_inkyEffectItem,
            "sticky_goo" => lastCd_pinkyEffectItem,
            _ => -Mathf.Infinity
        };

        if (Time.time - lastCd < cooldown)
        {
            Debug.Log("Effect item use in cooldown.");
            return;
        }

        var actions = new Dictionary<string, System.Func<IEnumerator<float>>>
        {
            { "imitate_speed", OnUse_ImitateSpeed },
            { "electroshock", OnUse_Electroshock },
            { "wind_burst", OnUse_WindBurst },
            { "sticky_goo", OnUse_StickyGoo },
        };

        if (actions.TryGetValue(_ownedEffectItem, out var method))
        {
            Timing.RunCoroutine(method());
        }

        switch (_ownedEffectItem)
        {
            case "imitate_speed":
                lastCd_blinkyEffectItem = Time.time; 
                break;
            
            case "electroshock":
                lastCd_clydeEffectItem = Time.time; 
                break;
            
            case "wind_burst":
                lastCd_inkyEffectItem = Time.time; 
                break;
            
            case "sticky_goo":
                lastCd_pinkyEffectItem = Time.time; 
                break;
        }
    }

    // ================== Imitate Speed ===================== //

    private IEnumerator<float> OnUse_ImitateSpeed()
    {
        var item_imitateSpeed = effectItemList.effectItems.Find(item => item.name == "imitate_speed");
        List<float> prevDefaultSpeeds = new List<float>
        {
            blinkyDefaultSpeed, clydeDefaultSpeed, inkyDefaultSpeed, pinkyDefaultSpeed, onCtrl_defaultSpeed
        };
        const float PACMAN_DEFAULT_SPEED = 0.6f;

        InEffect_ImitateSpeed("add", true, PACMAN_DEFAULT_SPEED);
        yield return Timing.WaitForSeconds(item_imitateSpeed.useTime);
        InEffect_ImitateSpeed("remove", false, -Mathf.Infinity, prevDefaultSpeeds);
    }

    private void InEffect_ImitateSpeed(string listMode, bool shouldImitate, float speed = -Mathf.Infinity, List<float> prevDefaultSpeeds = null)
    {
        List<string> _ghost_listAlive = IngameDataManager.LoadSpecificData<List<string>>("ghost_data.list_alive");
        foreach (string ghostName in _ghost_listAlive)
        {
            InOutAffectedItems(listMode, "imitate_speed", "ghost", ghostName);
        }
        
        if (shouldImitate)
        {
            blinkyDefaultSpeed = speed;
            clydeDefaultSpeed = speed;
            inkyDefaultSpeed = speed;
            pinkyDefaultSpeed = speed;
            onCtrl_defaultSpeed = speed;
        }
        else
        {
            blinkyDefaultSpeed = prevDefaultSpeeds[0];
            clydeDefaultSpeed = prevDefaultSpeeds[1];
            inkyDefaultSpeed = prevDefaultSpeeds[2];
            pinkyDefaultSpeed = prevDefaultSpeeds[3];
            onCtrl_defaultSpeed = prevDefaultSpeeds[4];
        }
    }

    // ================== Electroshock ====================== //

    private IEnumerator<float> OnUse_Electroshock()
    {
        var item_electroshock = effectItemList.effectItems.Find(item => item.name == "electroshock");
        const float PACMAN_SPEED_MULTIPLIER_INCREASE = 0.4f;
        const float GHOST_SPEED_MULTIPLIER_INCREASE = 0.8f;
        float duration = item_electroshock.useTime;

        bool _pacman_hasPowerPellet = IngameDataManager.LoadSpecificData<bool>("pacman_data.has_power_pellet");
        _pacman_hasPowerPellet = false;
        IngameDataManager.SaveSpecificData<bool>("pacman_data.has_power_pellet", _pacman_hasPowerPellet);

        InEffect_Electroshock("add", -PACMAN_SPEED_MULTIPLIER_INCREASE, -GHOST_SPEED_MULTIPLIER_INCREASE, duration);
        yield return Timing.WaitForSeconds(duration);
        InEffect_Electroshock("remove", PACMAN_SPEED_MULTIPLIER_INCREASE, GHOST_SPEED_MULTIPLIER_INCREASE);
    }

    private void InEffect_Electroshock(string listMode, float pacmanIncrease, float ghostIncrease, float duration = 0)
    {
        float _pacman_speedMultiplier = IngameDataManager.LoadSpecificData<float>("pacman_data.speed_multiplier");
        _pacman_speedMultiplier += pacmanIncrease;

        IngameDataManager.SaveSpecificData("pacman_data.speed_multiplier", _pacman_speedMultiplier);
        InOutAffectedItems(listMode, "electroshock", "pacman");
        particleEffectMazeHandler.SpawnEffectParticle(sparkElectricParticlePrefab, "pacman");

        
        string onControlled = onCtrl_ghost.name;
        List<string> _ghost_listAlive = IngameDataManager.LoadSpecificData<List<string>>("ghost_data.list_alive");
        
        foreach (string ghostName in _ghost_listAlive)
        {
            if (ghostName == onControlled) continue;
            
            float _ghost_speedMultiplier = IngameDataManager.LoadSpecificListData<float>("ghost_data.ghost_single_info", ghostName, "speed_multiplier");
            _ghost_speedMultiplier += ghostIncrease;
            
            IngameDataManager.SaveSpecificListData("ghost_data.ghost_single_info", ghostName, "speed_multiplier", _ghost_speedMultiplier);
            InOutAffectedItems(listMode, "electroshock", "ghost", ghostName);

            if (listMode == "add")
            {
                particleEffectMazeHandler.SpawnEffectParticle(sparkElectricParticlePrefab, ghostName);
                particleEffectMazeHandler.SpawnEffectOverlay(ghostElectrifiedOverlayPrefab, ghostName, duration);
            }
        }
    }
    
    // ==================== Wind Burst ====================== //

    private IEnumerator<float> OnUse_WindBurst()
    {
        var item_windBurst = effectItemList.effectItems.Find(item => item.name == "wind_burst");
        const float SPEED_MULTIPLIER_INCREASE_LEFT = 0.5f;
        const float SPEED_MULTIPLIER_INCREASE_RIGHT = 0.35f;
        float duration = item_windBurst.useTime;

        Timing.RunCoroutine(InEffect_WindBurst(-SPEED_MULTIPLIER_INCREASE_LEFT, SPEED_MULTIPLIER_INCREASE_RIGHT, duration));
        yield return Timing.WaitForSeconds(duration);
        StopEffect_WindBurst();
    }

    private IEnumerator<float> InEffect_WindBurst(float increaseLeft, float increaseRight, float duration)
    {
        particleEffectMazeHandler.SpawnAmbientParticle(windBurstParticlePrefab, new Vector2(-4.0f, 4.0f));
        List<string> _ghost_listAlive = IngameDataManager.LoadSpecificData<List<string>>("ghost_data.list_alive");
        
        InOutAffectedItems("add", "wind_burst", "pacman");
        foreach (string ghostName in _ghost_listAlive)
        {
            InOutAffectedItems("add", "wind_burst", "ghost", ghostName);
        }
        
        float elapsedTime = 0f;

        Vector2 prevPacmanDirection = Vector2.zero;
        Dictionary<string, Vector2> prevGhostDirections = new Dictionary<string, Vector2>();

        while (elapsedTime < duration)
        {
            Vector2 _pacman_direction = IngameDataManager.LoadSpecificData<Vector2>("pacman_data.direction");
            if (_pacman_direction != prevPacmanDirection)
            {
                float _pacman_windBurst = GetWindBurstSpeed(_pacman_direction, increaseLeft, increaseRight);
                IngameDataManager.SaveSpecificData("pacman_data.wind_burst_speed_affect", _pacman_windBurst);
                prevPacmanDirection = _pacman_direction;
            }

            foreach (string ghostName in _ghost_listAlive)
            {
                if (!prevGhostDirections.ContainsKey(ghostName))
                {
                    prevGhostDirections[ghostName] = Vector2.zero;
                }

                Vector2 _ghost_direction = IngameDataManager.LoadSpecificListData<Vector2>("ghost_data.ghost_single_info", ghostName, "direction");
                if (_ghost_direction != prevGhostDirections[ghostName])
                {
                    float _ghost_windBurst = GetWindBurstSpeed(_ghost_direction, increaseLeft, increaseRight);
                    IngameDataManager.SaveSpecificListData("ghost_data.ghost_single_info", ghostName, "wind_burst_speed_affect", _ghost_windBurst);
                    prevGhostDirections[ghostName] = _ghost_direction;
                }
            }

            elapsedTime += Time.deltaTime;
            yield return 0f;
        }
    }

    private void StopEffect_WindBurst()
    {
        List<string> _ghost_listAlive = IngameDataManager.LoadSpecificData<List<string>>("ghost_data.list_alive");
        
        IngameDataManager.SaveSpecificData("pacman_data.wind_burst_speed_affect", 1.0f);
        foreach (string ghostName in _ghost_listAlive)
        {
            IngameDataManager.SaveSpecificListData("ghost_data.ghost_single_info", ghostName, "wind_burst_speed_affect", 1.0f);
        }

        InOutAffectedItems("remove", "wind_burst", "pacman");
        foreach (string ghostName in _ghost_listAlive)
        {
            InOutAffectedItems("remove", "wind_burst", "ghost", ghostName);
        }
    }

    private float GetWindBurstSpeed(Vector2 direction, float increaseLeft, float increaseRight)
    {
        return direction switch
        {
            Vector2 v when v == Vector2.left => 1.0f + increaseLeft,
            Vector2 v when v == Vector2.right => 1.0f + increaseRight,
            Vector2 v when v == Vector2.up => 1.0f,
            Vector2 v when v == Vector2.down => 1.0f,
            _ => 1.0f,
        };
    }

    // ==================== Sticky Goo ====================== //

    private IEnumerator<float> OnUse_StickyGoo()
    {
        var item_stickyGoo = effectItemList.effectItems.Find(item => item.name == "sticky_goo");
        float duration = item_stickyGoo.cooldown;

        Timing.RunCoroutine(WaitForEffect_StickyGoo(duration));
        yield return 0f;
    }

    private IEnumerator<float> WaitForEffect_StickyGoo(float duration)
    {
        GameObject stickyGoo = Instantiate(stickyGooPrefab, GetTileCenter(onCtrl_ghost.transform.position), Quaternion.identity);
        
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            Vector2 _pacman_coordinate = IngameDataManager.LoadSpecificData<Vector2>("pacman_data.coordinate");
            if (_pacman_coordinate == (Vector2)stickyGoo.transform.position)
            {
                Timing.RunCoroutine(StartEffect_StickyGoo());
                Destroy(stickyGoo);
                yield break;
            }

            elapsedTime += Time.deltaTime;
            yield return 0f;
        }
        
        Destroy(stickyGoo);
    }

    private IEnumerator<float> StartEffect_StickyGoo()
    {
        var item_stickyGoo = effectItemList.effectItems.Find(item => item.name == "sticky_goo");
        const float SPEED_MULTIPLIER_INCREASE = 0.8f;
        const float VISION_MULTIPLIER_INCREASE = 0.25f;
        const float STICKY_DURATION = 1.0f;
        float USE_TIME_DURATION = item_stickyGoo.useTime;

        Timing.RunCoroutine(InEffect_StickyGoo("add", -SPEED_MULTIPLIER_INCREASE, -VISION_MULTIPLIER_INCREASE, STICKY_DURATION, USE_TIME_DURATION));
        yield return Timing.WaitForSeconds(USE_TIME_DURATION);
        Timing.RunCoroutine(InEffect_StickyGoo("remove", SPEED_MULTIPLIER_INCREASE, VISION_MULTIPLIER_INCREASE, STICKY_DURATION));
    }

    private IEnumerator<float> InEffect_StickyGoo(string listMode, float speedIncrease, float visionIncrease, float stickyDuration, float useTimeDuration = 0f)
    {
        InOutAffectedItems(listMode, "sticky_goo", "pacman");
        particleEffectMazeHandler.SpawnEffectOverlay(gooOverlayPrefab, "pacman", useTimeDuration);

        float _pacman_speedMultiplier = IngameDataManager.LoadSpecificData<float>("pacman_data.speed_multiplier");
        _pacman_speedMultiplier += speedIncrease;
        IngameDataManager.SaveSpecificData("pacman_data.speed_multiplier", _pacman_speedMultiplier);

        float _pacman_visionMultiplier = IngameDataManager.LoadSpecificData<float>("pacman_data.vision_multiplier");
        float startValue = _pacman_visionMultiplier;
        float targetValue = _pacman_visionMultiplier + visionIncrease;
        float elapsedTime = 0f;
        
        while (elapsedTime < stickyDuration)
        {
            _pacman_visionMultiplier = Mathf.Lerp(startValue, targetValue, elapsedTime / stickyDuration);
            IngameDataManager.SaveSpecificData("pacman_data.vision_multiplier", _pacman_visionMultiplier);

            elapsedTime += Time.deltaTime;
            yield return 0f;
        }

        _pacman_visionMultiplier = targetValue;
        IngameDataManager.SaveSpecificData("pacman_data.vision_multiplier", _pacman_visionMultiplier);
    }

    private void InOutAffectedItems(string listMode, string effectItemName, string character, string ghostName = null)
    {
        List<string> _affectedItems = (character == "pacman") 
            ? IngameDataManager.LoadSpecificData<List<string>>("pacman_data.affected_items") 
            : IngameDataManager.LoadSpecificListData<List<string>>("ghost_data.ghost_single_info", ghostName, "affected_items");

        if (listMode == "add")
        {
            _affectedItems.Add(effectItemName);
            var effectItem = effectItemList.effectItems.Find(item => item.name == effectItemName);
            particleEffectMazeHandler.SpawnStartParticle(startParticlePrefab, 
                                                        effectItem.startParticleSprite, 
                                                        effectItem.inEffect.id, 
                                                        (character == "pacman") ? "pacman" : ghostName);
        }
        else if (listMode == "remove") 
        {
            _affectedItems.Remove(effectItemName);
        }

        if (character == "pacman")
        {
            IngameDataManager.SaveSpecificData("pacman_data.affected_items", _affectedItems);
        }
        else if (character == "ghost" && ghostName != null)
        {
            IngameDataManager.SaveSpecificListData("ghost_data.ghost_single_info", ghostName, "affected_items", _affectedItems);
        }
    }

    private IEnumerator<float> UpdateTextDisplay()
    {
        while (true)
        {
            var item_changeGhost = effectItemList.effectItems.Find(item => item.name == "change_ghost");
            float remainCd_changeGhost = Mathf.Max(0f, item_changeGhost.cooldown - (Time.time - lastCd_changeGhost));
            cdImage_changeGhost.enabled = remainCd_changeGhost > 0 ? true : false;
            cdText_changeGhost.text = remainCd_changeGhost > 0 ? $"{remainCd_changeGhost:F1}s" : "";
            
            var item_boost = effectItemList.effectItems.Find(item => item.name == "boost");
            float remainCd_boost = Mathf.Max(0f, item_boost.cooldown - (Time.time - lastCd_boost));
            cdImage_boost.enabled = remainCd_boost > 0 ? true : false;
            cdText_boost.text = remainCd_boost > 0 ? $"{remainCd_boost:F1}s" : "";

            string _ownedEffectItem = IngameDataManager.LoadSpecificListData<string>("ghost_data.ghost_single_info", onCtrl_ghost.name, "effect_item");
            float cooldown = effectItemList.effectItems.Find(item => item.name == _ownedEffectItem).cooldown;

            float lastCd = _ownedEffectItem switch
            {
                "imitate_speed" => lastCd_blinkyEffectItem,
                "electroshock" => lastCd_clydeEffectItem,
                "wind_burst" => lastCd_inkyEffectItem,
                "sticky_goo" => lastCd_pinkyEffectItem,
                _ => -Mathf.Infinity
            };

            float remainCd_effectItem = Mathf.Max(0f, cooldown - (Time.time - lastCd));
            cdImage_effectItem.enabled = remainCd_effectItem > 0 ? true : false;
            cdText_effectItem.text = remainCd_effectItem > 0 ? $"{remainCd_effectItem:F1}s" : "";

            yield return Timing.WaitForSeconds(0.1f);
        }
    }

    /*********************************************************************/
    //
    //                        Ghost AI Movement
    //
    /*********************************************************************/

    private IEnumerator MoveGhostCoroutine(string ghostName)
    {
        while (true)
        {
            if (!onAuto_isRunning[ghostName])
            {
                onAuto_isRunning[ghostName] = true;
                StartCoroutine(AutoMoveGhost(ghostName));
            }

            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator AutoMoveGhost(string ghostName)
    {
        if (onCtrl_ghost.name == ghostName)
        {
            onAuto_isRunning[ghostName] = false;
            onAuto_targetPositions[ghostName] = Vector2.zero;
            yield break;
        }

        GameObject onAuto_ghost;
        Animator onAuto_animator;
        float onAuto_defaultSpeed;

        var ghostData = new Dictionary<string, (GameObject ghost, Animator animator, float speed)>
        {
            { "blinky", (blinky, blinkyAnimator, blinkyDefaultSpeed) },
            { "clyde", (clyde, clydeAnimator, clydeDefaultSpeed) },
            { "inky", (inky, inkyAnimator, inkyDefaultSpeed) },
            { "pinky", (pinky, pinkyAnimator, pinkyDefaultSpeed) }
        };

        if (!ghostData.TryGetValue(ghostName, out var ghostInfo)) yield break;

        (onAuto_ghost, onAuto_animator, onAuto_defaultSpeed) = ghostInfo;

        while (onAuto_isRunning[ghostName])
        {
            Vector2 currentPosition = onAuto_ghost.transform.position;

            if ((Vector2)onAuto_ghost.transform.position == GetTileCenter((Vector2)onAuto_ghost.transform.position))
            {
                IngameData.PacmanData _pacmanData = IngameDataManager.LoadSpecificData<IngameData.PacmanData>("pacman_data");
                IngameData.GhostData _ghostData = IngameDataManager.LoadSpecificData<IngameData.GhostData>("ghost_data");

                Vector2 pacmanPosition = _pacmanData.coordinate;
                Vector2 targetPosition = GetGhostTargetPosition(ghostName, currentPosition, pacmanPosition, _pacmanData, _ghostData);

                onAuto_directions[ghostName] = GetValidDirection(currentPosition, onAuto_directions[ghostName], onAuto_ghost, targetPosition, _pacmanData);
                onAuto_targetPositions[ghostName] = currentPosition + onAuto_directions[ghostName] * 0.16f;

                UpdateGhostAnimation(onAuto_animator, onAuto_directions[ghostName], ghostName);
                UpdateGhostDirection(ghostName, onAuto_directions[ghostName]);

                Queue<Vector2> recentTiles = onAuto_recentTiles[ghostName];
                if (recentTiles.Count >= MAX_RECENT_TILES)
                {
                    recentTiles.Dequeue();
                }
                recentTiles.Enqueue(GetTileCenter(currentPosition));
            }

            if (IsAbleToMoveTo(onAuto_targetPositions[ghostName], onAuto_ghost))
            {
                Vector2 newPosition = Vector2.MoveTowards(currentPosition, onAuto_targetPositions[ghostName], (onAuto_defaultSpeed * ghost_speedMultiplier[ghostName] * ghost_windBurstSpeedAffect[ghostName]) * Time.deltaTime);
                onAuto_ghost.transform.position = newPosition;

                if (newPosition == GetTileCenter(newPosition))
                {
                    UpdateGhostPosition(ghostName, newPosition);
                }
            }
            else
            {
                onAuto_ghost.transform.position = GetTileCenter(currentPosition);
                onAuto_isRunning[ghostName] = false;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    private Vector2 GetGhostTargetPosition(string ghostName, Vector2 ghostPosition, Vector2 pacmanPosition, IngameData.PacmanData pacmanData, IngameData.GhostData ghostData)
    {
        GameObject ghost = null;
        switch (ghostName)
        {
            case "clyde":
                float distanceToPacman = Vector2.Distance(ghostPosition, pacmanPosition);
                return distanceToPacman > FEIGNING_IGNORANCE_DISTANCE * 0.16f ? pacmanPosition : Vector2.zero;
            
            case "inky":
                ghost = inky;
                Vector2 pacmanDirection = pacmanData.direction;
                Vector2 inkyPosition = ghostData.ghost_single_info.Find(info => info.name == ghostName)?.coordinate ?? Vector2.zero;
                Vector2 offsetFromPacman = pacmanPosition + pacmanDirection * (WHIMSICAL_DISTANCE * 0.16f);
                return GetNearestNonCollisionTile(offsetFromPacman + (offsetFromPacman - inkyPosition), ghost);
            
            case "pinky":
                ghost = pinky;
                Vector2 targetTile = pacmanPosition + pacmanData.direction * (AMBUSHER_DISTANCE * 0.16f);
                return GetNearestNonCollisionTile(targetTile, ghost);
            
            default:
                return pacmanPosition;
        }
    }

    private Vector2 GetTileCenter(Vector2 position)
    {
        float x = Mathf.Round((position.x - TILE_OFFSET) / TILE_SIZE) * TILE_SIZE + TILE_OFFSET;
        float y = Mathf.Round((position.y - TILE_OFFSET) / TILE_SIZE) * TILE_SIZE + TILE_OFFSET;
        return new Vector2(x, y);
    }

    private Vector2 GetValidDirection(Vector2 currentPosition, Vector2 currentDirection, GameObject ghost, Vector2 targetPosition, IngameData.PacmanData pacmanData)
    {
        bool hasPowerPellet = pacmanData.has_power_pellet;
        Vector2[] possibleDirections = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        Vector2 reverseDirection = -currentDirection;

        Vector2 bestDirection = reverseDirection;
        float bestDistance = hasPowerPellet ? 0f : float.MaxValue;

        foreach (Vector2 direction in possibleDirections)
        {
            if (direction == reverseDirection) continue;

            Vector2 newPosition = currentPosition + direction * TILE_SIZE;
            if (!IsAbleToMoveTo(newPosition, ghost)) continue;

            float distanceToTarget = Vector2.SqrMagnitude(newPosition - targetPosition);
            bool isBetterDirection = hasPowerPellet ? distanceToTarget > bestDistance : distanceToTarget < bestDistance;

            if (isBetterDirection)
            {
                bestDistance = distanceToTarget;
                bestDirection = direction;
            }
        }

        Vector2 potentialPosition = currentPosition + bestDirection * TILE_SIZE;
        if (bestDirection != reverseDirection || IsAbleToMoveTo(potentialPosition, ghost))
        {
            return bestDirection;
        }

        return reverseDirection;
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
