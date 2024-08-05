using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class PacmanMazeController : MonoBehaviour
{
    [SerializeField] private bool isMazeStarted = false;
    
    [Header("===Pacman Properties===")]
    [SerializeField] private GameObject pacman;
    [SerializeField] private float defaultSpeed;

    [Header("===Pacman Miscellaneous===")]
    [SerializeField] private LayerMask collisionLayer;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Animator animator;
    [SerializeField] private Text pointsText;
    [SerializeField] private Text playtimeText;
    [SerializeField] private List<Image> heartImages;
    [SerializeField] private List<Sprite> heartSprites; // 0: empty_heart, 1: full_heart
    
    [Header("===Pacman Effect Item Info===")]
    [SerializeField] private EffectItemList effectItemList;
    [Space(4)]
    [SerializeField] private Image effectItemIcon;
    [SerializeField] private Text effectItemText;
    [SerializeField] private Text cdText_effectItem;
    [SerializeField] private Image cdImage_effectItem;
    [Space(4)]
    [SerializeField] private Image darknessAmbient;
    [Space(4)]
    [SerializeField] private ParticleEffectMazeHandler particleEffectMazeHandler;
    [SerializeField] private GameObject startParticlePrefab;
    [SerializeField] private GameObject sparkParalyzeParticlePrefab;
    
    private bool hasEffectItem = false;
    private float cd_effectItem = 0f;
    private float lastCd_effectItem = -Mathf.Infinity;
    private bool isRunning = false;
    
    private Vector2 direction;
    private Vector2 targetPosition;
    private Vector2 queuedDirection;
    private float speedMultiplier = 0f;
    private float windBurstSpeedAffect = 0f;
    private BoxCollider2D pacmanCollider;

    private const float TILE_SIZE = 0.16f;

    /*********************************************************************/
    //
    //                            General
    //
    /*********************************************************************/

    private void Start()
    {
        InitializePacman();
    }

    private void OnDestroy()
    {
        KeybindDataManager.ResetKeyActions();
    }

    public void StartPacmanController(bool triggerValue)
    {
        isMazeStarted = triggerValue;
        if (isMazeStarted) 
        {
            RegisterKeyActions();
            Timing.RunCoroutine(IncreasePlaytime());
        }
    }

    private void RegisterKeyActions()
    {
        KeybindDataManager.RegisterKeyAction("pacman.face_up", () => HandleInput("pacman.face_up"));
        KeybindDataManager.RegisterKeyAction("pacman.face_down", () => HandleInput("pacman.face_down"));
        KeybindDataManager.RegisterKeyAction("pacman.face_left", () => HandleInput("pacman.face_left"));
        KeybindDataManager.RegisterKeyAction("pacman.face_right", () => HandleInput("pacman.face_right"));
        KeybindDataManager.RegisterKeyAction("pacman.use_item", () => HandleInput("pacman.use_item"));
    }

    private void Update()
    {
        if (!isMazeStarted) return;

        if (!isRunning && queuedDirection != Vector2.zero)
        {
            direction = queuedDirection;
            targetPosition = (Vector2)pacman.transform.position + direction * TILE_SIZE;
            isRunning = true;
            
            UpdatePacmanAnimation();
            StartCoroutine(MovePacman());
        }
    }

    private void InitializePacman()
    {
        pacman.transform.position = new Vector2(Mathf.Round(spawnPoint.position.x * 100.0f) / 100.0f, Mathf.Round(spawnPoint.position.y * 100.0f) / 100.0f);
        UpdatePacmanPosition(pacman.transform.position);

        pacmanCollider = pacman.GetComponent<BoxCollider2D>();
        animator?.SetTrigger("pacman.rest");
        
        UpdateHeartDisplay();
        SetToNormalData();
        
        Timing.RunCoroutine(UpdateEffectItemDisplay());
        Timing.RunCoroutine(UpdateTextDisplay());
        Timing.RunCoroutine(UpdateSavedSpeed());
    }

    private void UpdateHeartDisplay()
    {
        int pacmanLives = IngameDataManager.LoadSpecificData<int>("pacman_data.lives");
        for (int i = 0; i < heartImages.Count; i++)
        {
            heartImages[i].sprite = i < pacmanLives ? heartSprites[1] : heartSprites[0];
        }
    }

    private void SetToNormalData()
    {
        IngameDataManager.SaveSpecificData("pacman_data.speed_multiplier", 1.0f);
        IngameDataManager.SaveSpecificData("pacman_data.vision_multiplier", 1.0f);
        IngameDataManager.SaveSpecificData("pacman_data.wind_burst_speed_affect", 1.0f);
        IngameDataManager.SaveSpecificData("pacman_data.has_power_pellet", false);
        IngameDataManager.SaveSpecificData("pacman_data.is_immune_to_ghost", false);
        IngameDataManager.SaveSpecificData("pacman_data.current_effect_item", "");

        List<string> _affectedItems = IngameDataManager.LoadSpecificData<List<string>>("pacman_data.affected_items");
        _affectedItems.Clear();
        IngameDataManager.SaveSpecificData("pacman_data.affected_items", _affectedItems);
    }

    public void SetSlowSpeed()
    {
        IngameDataManager.SaveSpecificData("pacman_data.speed_multiplier", 0.02f);
    }

    private IEnumerator<float> UpdateSavedSpeed()
    {
        while (true)
        {   
            float pacman_speedMultiplier = IngameDataManager.LoadSpecificData<float>("pacman_data.speed_multiplier");
            float pacman_windBurstSpeedAffect = IngameDataManager.LoadSpecificData<float>("pacman_data.wind_burst_speed_affect");

            if (speedMultiplier != pacman_speedMultiplier) { speedMultiplier = pacman_speedMultiplier; }
            if (windBurstSpeedAffect != pacman_windBurstSpeedAffect) { windBurstSpeedAffect = pacman_windBurstSpeedAffect; }

            yield return Timing.WaitForSeconds(0.1f);
        }
    }

    private IEnumerator MovePacman()
    {
        while (isRunning)
        {
            Vector2 currentPosition = (Vector2)pacman.transform.position;

            if (currentPosition == targetPosition)
            {
                if (IsAbleToMoveTo(currentPosition + queuedDirection * TILE_SIZE))
                {
                    direction = queuedDirection;
                    targetPosition = currentPosition + direction * TILE_SIZE;
                }
                else if (IsAbleToMoveTo(currentPosition + direction * TILE_SIZE))
                {
                    targetPosition = currentPosition + direction * TILE_SIZE;
                }
                else
                {
                    isRunning = false;
                    yield break;
                }

                UpdatePacmanAnimation();
                UpdatePacmanDirection();
            }

            if (IsAbleToMoveTo(targetPosition))
            {
                Vector2 newPosition = Vector2.MoveTowards(currentPosition, targetPosition, (defaultSpeed * speedMultiplier * windBurstSpeedAffect) * Time.deltaTime);
                pacman.transform.position = newPosition;

                if (newPosition == targetPosition)
                {
                    UpdatePacmanPosition(newPosition);
                }
            }
            else
            {
                isRunning = false;
                yield break;
            }
            
            yield return new WaitForFixedUpdate();
        }
    }

    private bool IsAbleToMoveTo(Vector2 targetPosition)
    {
        float tolerance = 0.1f;
        RaycastHit2D hit = Physics2D.BoxCast(targetPosition, pacmanCollider.bounds.size, 0f, Vector2.zero, tolerance, collisionLayer);
        return hit.collider == null || hit.collider.gameObject == pacman;
    }

    private void UpdatePacmanPosition(Vector2 position)
    {
        IngameDataManager.SaveSpecificData("pacman_data.coordinate", position);
    }

    private void UpdatePacmanDirection()
    {
        IngameDataManager.SaveSpecificData("pacman_data.direction", direction);
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

        bool _pacman_hasPowerPellet = IngameDataManager.LoadSpecificData<bool>("pacman_data.has_power_pellet");
        
        string animatorId = direction switch
        {
            Vector2 v when v == Vector2.up => (!_pacman_hasPowerPellet) ? "pacman.normal_up" : "pacman.scary_up",
            Vector2 v when v == Vector2.down => (!_pacman_hasPowerPellet) ? "pacman.normal_down" : "pacman.scary_down",
            Vector2 v when v == Vector2.left => (!_pacman_hasPowerPellet) ? "pacman.normal_left" : "pacman.scary_left",
            Vector2 v when v == Vector2.right => (!_pacman_hasPowerPellet) ? "pacman.normal_right" : "pacman.scary_right",
            _ => "pacman.rest"
        };

        animator.SetTrigger(animatorId);
    }

    private void HandleInput(string action)
    {
        if (!isMazeStarted) return;
        
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
            return;
        }
        
        if (action == "pacman.use_item")
        {
            OnUse_EffectItem();
            return;
        }
    }

    public IEnumerator<float> IncreasePlaytime()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(1.0f);
            
            int playtime = IngameDataManager.LoadSpecificData<int>("pacman_data.playtime");
            IngameDataManager.SaveSpecificData("pacman_data.playtime", playtime + 1);

            System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(playtime);
            playtimeText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
        }
    }

    public void Respawn()
    {
        Vector2 spawnPosition = new Vector2(Mathf.Round(spawnPoint.position.x * 100.0f) / 100.0f, Mathf.Round(spawnPoint.position.y * 100.0f) / 100.0f);
        targetPosition = spawnPosition;
        pacman.transform.position = spawnPosition;
        UpdatePacmanPosition(spawnPosition);

        animator.SetTrigger("pacman.rest");
        UpdatePacmanAnimation();
    }

    /*********************************************************************/
    //
    //                        Effect Items Use
    //
    /*********************************************************************/

    private IEnumerator<float> UpdateEffectItemDisplay()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(0.1f);

            if (!hasEffectItem)
            {
                string _pacman_currentEffectItem = IngameDataManager.LoadSpecificData<string>("pacman_data.current_effect_item");
                if (System.String.IsNullOrEmpty(_pacman_currentEffectItem)) continue;

                var effectItem = effectItemList.effectItems.Find(item => item.name == _pacman_currentEffectItem);
                if (effectItem == null) continue;

                SetEffectItemDisplay(effectItem.name, effectItem.asItemSprite, new Color(1.0f, 1.0f, 1.0f, 1.0f), true);
            }
        }
    }

    private void SetEffectItemDisplay(string name, Sprite sprite, Color color, bool hasItem)
    {
        effectItemText.text = $"{name.Replace("_", " ")}";
        effectItemIcon.sprite = sprite;
        effectItemIcon.color = color;
        hasEffectItem = hasItem;
    }

    private void OnUse_EffectItem()
    {
        if (!hasEffectItem) 
        {
            Debug.Log("No effect item found in inventory.");
            return;
        }

        if (Time.time - lastCd_effectItem < cd_effectItem)
        {
            Debug.Log("Effect item use in cooldown.");
            return;
        }

        string _pacman_currentEffectItem = IngameDataManager.LoadSpecificData<string>("pacman_data.current_effect_item");
        if (System.String.IsNullOrEmpty(_pacman_currentEffectItem)) return;

        var actions = new Dictionary<string, System.Func<IEnumerator<float>>>
        {
            { "rocket_boost", OnUse_RocketBoost },
            { "zoom_out", OnUse_ZoomOut },
            { "retreat", OnUse_Retreat },
            { "immunity", OnUse_Immunity },
            { "slow_move", OnUse_SlowMove },
            { "paralyze", OnUse_Paralyze },
            { "darkness", OnUse_Darkness },
            { "invert_control", OnUse_InvertControl },
        };

        if (actions.TryGetValue(_pacman_currentEffectItem, out var method))
        {
            Timing.RunCoroutine(method());
        }

        SetEffectItemDisplay("Effect Item", null, new Color(1.0f, 1.0f, 1.0f, 0.0f), false);

        cd_effectItem = effectItemList.effectItems.Find(item => item.name == _pacman_currentEffectItem).cooldown;
        lastCd_effectItem = Time.time;
        
        _pacman_currentEffectItem = null;
        IngameDataManager.SaveSpecificData("pacman_data.current_effect_item", _pacman_currentEffectItem);
    }

    // =================== Rocket Boost ===================== //

    private IEnumerator<float> OnUse_RocketBoost()
    {
        var item_rocketBoost = effectItemList.effectItems.Find(item => item.name == "rocket_boost");
        const float SPEED_MULTIPLIER_INCREASE = 0.5f;

        InEffect_RocketBoost("add", SPEED_MULTIPLIER_INCREASE);
        yield return Timing.WaitForSeconds(item_rocketBoost.useTime);
        InEffect_RocketBoost("remove", -SPEED_MULTIPLIER_INCREASE);
    }

    private void InEffect_RocketBoost(string listMode, float increase)
    {        
        float _pacman_speedMultiplier = IngameDataManager.LoadSpecificData<float>("pacman_data.speed_multiplier");
        _pacman_speedMultiplier += increase;

        IngameDataManager.SaveSpecificData("pacman_data.speed_multiplier", _pacman_speedMultiplier);
        InOutAffectedItems(listMode, "rocket_boost", "pacman");
    }

    // ===================== Zoom Out ======================= //

    private IEnumerator<float> OnUse_ZoomOut()
    {
        var item_zoomOut = effectItemList.effectItems.Find(item => item.name == "zoom_out");
        const float VISION_MULTIPLIER_INCREASE = 0.4f;
        const float VISION_DURATION = 1.0f;

        Timing.RunCoroutine(InEffect_ZoomOutPacman("add", VISION_MULTIPLIER_INCREASE, VISION_DURATION));
        yield return Timing.WaitForSeconds(item_zoomOut.useTime);
        Timing.RunCoroutine(InEffect_ZoomOutPacman("remove", -VISION_MULTIPLIER_INCREASE, VISION_DURATION));
    }

    public IEnumerator<float> InEffect_ZoomOutPacman(string listMode, float increase, float duration)
    {
        InOutAffectedItems(listMode, "zoom_out", "pacman");
        
        float _pacman_visionMultiplier = IngameDataManager.LoadSpecificData<float>("pacman_data.vision_multiplier");
        float startValue = _pacman_visionMultiplier;
        float targetValue = _pacman_visionMultiplier + increase;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            _pacman_visionMultiplier = Mathf.Lerp(startValue, targetValue, elapsedTime / duration);
            IngameDataManager.SaveSpecificData("pacman_data.vision_multiplier", _pacman_visionMultiplier);

            elapsedTime += Time.deltaTime;
            yield return 0f;
        }

        _pacman_visionMultiplier = targetValue;
        IngameDataManager.SaveSpecificData("pacman_data.vision_multiplier", _pacman_visionMultiplier);
    }

    public IEnumerator<float> InEffect_ZoomOutGhost(float increase, float duration)
    {
        float _ghost_visionMultiplier = IngameDataManager.LoadSpecificData<float>("ghost_data.vision_multiplier");
        float startValue = _ghost_visionMultiplier;
        float targetValue = _ghost_visionMultiplier + increase;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            _ghost_visionMultiplier = Mathf.Lerp(startValue, targetValue, elapsedTime / duration);
            IngameDataManager.SaveSpecificData("ghost_data.vision_multiplier", _ghost_visionMultiplier);

            elapsedTime += Time.deltaTime;
            yield return 0f;
        }

        _ghost_visionMultiplier = targetValue;
        IngameDataManager.SaveSpecificData("ghost_data.vision_multiplier", _ghost_visionMultiplier);
    }

    // ===================== Retreat ======================== //

    private IEnumerator<float> OnUse_Retreat()
    {
        var item_retreat = effectItemList.effectItems.Find(item => item.name == "retreat");
        const float SPEED_MULTIPLIER_INCREASE = 0.75f;

        InEffect_Retreat("add", -SPEED_MULTIPLIER_INCREASE);
        yield return Timing.WaitForSeconds(item_retreat.useTime);
        InEffect_Retreat("remove", SPEED_MULTIPLIER_INCREASE);
        Respawn();
    }

    private void InEffect_Retreat(string listMode, float increase)
    {
        float _pacman_speedMultiplier = IngameDataManager.LoadSpecificData<float>("pacman_data.speed_multiplier");
        _pacman_speedMultiplier += increase;

        IngameDataManager.SaveSpecificData("pacman_data.speed_multiplier", _pacman_speedMultiplier);
        InOutAffectedItems(listMode, "retreat", "pacman");
    }

    // ===================== Immunity ======================= //

    private IEnumerator<float> OnUse_Immunity()
    {
        var item_immunity = effectItemList.effectItems.Find(item => item.name == "immunity");

        InEffect_Immunity("add", true);
        yield return Timing.WaitForSeconds(item_immunity.useTime);
        InEffect_Immunity("remove", false);
    }

    private void InEffect_Immunity(string listMode, bool isImmune)
    {    
        pacman.GetComponent<SpriteRenderer>().color = isImmune 
            ? new Color(1.0f, 1.0f, 1.0f, 0.5f) 
            : new Color(1.0f, 1.0f, 1.0f, 1.0f);
        
        IngameDataManager.SaveSpecificData("pacman_data.is_immune_to_ghost", isImmune);
        InOutAffectedItems(listMode, "immunity", "pacman");
    }

    // ===================== Slow Move ====================== //

    private IEnumerator<float> OnUse_SlowMove()
    {
        var item_slowMove = effectItemList.effectItems.Find(item => item.name == "slow_move");
        const float SPEED_MULTIPLIER_INCREASE = 0.25f;

        InEffect_SlowMove("add", -SPEED_MULTIPLIER_INCREASE);
        yield return Timing.WaitForSeconds(item_slowMove.useTime);
        InEffect_SlowMove("remove", SPEED_MULTIPLIER_INCREASE);
    }

    private void InEffect_SlowMove(string listMode, float increase)
    {
        List<string> _ghost_listAlive = IngameDataManager.LoadSpecificData<List<string>>("ghost_data.list_alive");
                
        foreach (string ghostName in _ghost_listAlive)
        {    
            float _speedMultiplier = IngameDataManager.LoadSpecificListData<float>("ghost_data.ghost_single_info", ghostName, "speed_multiplier");
            _speedMultiplier += increase;

            IngameDataManager.SaveSpecificListData("ghost_data.ghost_single_info", ghostName, "speed_multiplier", _speedMultiplier);
            InOutAffectedItems(listMode, "slow_move", "ghost", ghostName);
        }
    }

    // ===================== Paralyze ======================= //

    private IEnumerator<float> OnUse_Paralyze()
    {
        var item_paralyze = effectItemList.effectItems.Find(item => item.name == "paralyze");
        const float SPEED_MULTIPLIER_INCREASE = 0.95f;
        const float PARALYZE_DURATION = 0.5f;
        const int MAX_PARALYZE_COUNT = 4;

        List<string> _ghost_listAlive = IngameDataManager.LoadSpecificData<List<string>>("ghost_data.list_alive");
        string nearestGhost = null;
        float nearestDistance = Mathf.Infinity;
        
        foreach (string ghostName in _ghost_listAlive)
        {
            bool _ghost_isParalyzed = IngameDataManager.LoadSpecificListData<bool>("ghost_data.ghost_single_info", ghostName, "is_paralyzed");
            if (_ghost_isParalyzed) continue;
            
            Vector2 _coordinate = IngameDataManager.LoadSpecificListData<Vector2>("ghost_data.ghost_single_info", ghostName, "coordinate");
            float distance = Vector2.Distance(_coordinate, pacman.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestGhost = ghostName;
            }
        }

        if (nearestGhost == null || nearestDistance == Mathf.Infinity) yield break;
        
        IngameDataManager.SaveSpecificListData("ghost_data.ghost_single_info", nearestGhost, "is_paralyzed", true);
        InOutAffectedItems("add", "paralyze", "ghost", nearestGhost);

        int paralyzeCount = 0;
        while (paralyzeCount < MAX_PARALYZE_COUNT)
        {
            InEffect_Paralyze(-SPEED_MULTIPLIER_INCREASE, nearestGhost);
            particleEffectMazeHandler.SpawnEffectParticle(sparkParalyzeParticlePrefab, nearestGhost);

            yield return Timing.WaitForSeconds(PARALYZE_DURATION);
            InEffect_Paralyze(SPEED_MULTIPLIER_INCREASE, nearestGhost);

            yield return Timing.WaitForSeconds((item_paralyze.useTime / 4.0f) - PARALYZE_DURATION);
            paralyzeCount++;
        }

        IngameDataManager.SaveSpecificListData("ghost_data.ghost_single_info", nearestGhost, "is_paralyzed", false);
        InOutAffectedItems("remove", "paralyze", "ghost", nearestGhost);
    }

    private void InEffect_Paralyze(float increase, string ghostName)
    {
        float _speedMultiplier = IngameDataManager.LoadSpecificListData<float>("ghost_data.ghost_single_info", ghostName, "speed_multiplier");
        _speedMultiplier += increase;

        IngameDataManager.SaveSpecificListData("ghost_data.ghost_single_info", ghostName, "speed_multiplier", _speedMultiplier);
    }

    // ====================== Darkness ====================== //

    private IEnumerator<float> OnUse_Darkness()
    {
        var item_darkness = effectItemList.effectItems.Find(item => item.name == "darkness");
        const float VISION_MULTIPLIER_INCREASE = 0.1f;
        List<Color> AMBIENT_COLORS = new List<Color>
        {
            new Color(0.0f, 0.0f, 0.0f, 1.0f),
            new Color(0.0f, 0.0f, 0.0f, 0.0f)
        };
        const float DARKNESS_DURATION = 1.0f;

        Timing.RunCoroutine(InEffect_Darkness("add", AMBIENT_COLORS[0], -VISION_MULTIPLIER_INCREASE, DARKNESS_DURATION));
        yield return Timing.WaitForSeconds(item_darkness.useTime);
        Timing.RunCoroutine(InEffect_Darkness("remove", AMBIENT_COLORS[1], VISION_MULTIPLIER_INCREASE, DARKNESS_DURATION));
    }

    private IEnumerator<float> InEffect_Darkness(string listMode, Color changeColor, float increase, float duration)
    {
        List<string> _ghost_listAlive = IngameDataManager.LoadSpecificData<List<string>>("ghost_data.list_alive");
        foreach (string ghostName in _ghost_listAlive)
        {
            InOutAffectedItems(listMode, "darkness", "ghost", ghostName);
        }
        
        float _ghost_visionMultiplier = IngameDataManager.LoadSpecificData<float>("ghost_data.vision_multiplier");
        float startValue = _ghost_visionMultiplier;
        float targetValue = _ghost_visionMultiplier + increase;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            darknessAmbient.color = Color.Lerp(darknessAmbient.color, changeColor, elapsedTime / duration);
            
            _ghost_visionMultiplier = Mathf.Lerp(startValue, targetValue, elapsedTime / duration);
            IngameDataManager.SaveSpecificData("ghost_data.vision_multiplier", _ghost_visionMultiplier);

            elapsedTime += Time.deltaTime;
            yield return 0f;
        }

        _ghost_visionMultiplier = targetValue;
        IngameDataManager.SaveSpecificData("ghost_data.vision_multiplier", _ghost_visionMultiplier);
    }

    // ================= Invert Control ===================== //

    private IEnumerator<float> OnUse_InvertControl()
    {
        var item_invertControl = effectItemList.effectItems.Find(item => item.name == "invert_control");

        InEffect_InvertControl("add", true);
        yield return Timing.WaitForSeconds(item_invertControl.useTime);
        InEffect_InvertControl("remove", false);
    }

    private void InEffect_InvertControl(string listMode, bool isInvert)
    {
        List<string> _ghost_listAlive = IngameDataManager.LoadSpecificData<List<string>>("ghost_data.list_alive");
        foreach (string ghostName in _ghost_listAlive)
        {
            InOutAffectedItems(listMode, "invert_control", "ghost", ghostName);
        }

        IngameDataManager.SaveSpecificData("ghost_data.is_control_inverted", isInvert);
    }

    // ============== For Display Effects =================== //

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
            int _pacman_points = IngameDataManager.LoadSpecificData<int>("pacman_data.points"); 
            pointsText.text = _pacman_points.ToString("00,000,000");

            float remainCd_effectItem = Mathf.Max(0f, cd_effectItem - (Time.time - lastCd_effectItem));
            cdImage_effectItem.enabled = remainCd_effectItem > 0f ? true : false;
            cdText_effectItem.text = remainCd_effectItem > 0 ? $"{remainCd_effectItem:F1}s" : "";

            yield return Timing.WaitForSeconds(0.1f);
        }
    }
}
