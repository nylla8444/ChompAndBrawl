using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public enum AttackCategory {
    punch,
    basic,
    unique,
    DOT
}

public enum AllAttacks {
    none,
    punch1,
    punch2,
    punch3,
    basic,
    pelletShot,
    venomLick,
    sugarRush,
    lightningStrike,
    sandstorm,
    magmaSlime
}

public class BrawlManager : MonoBehaviour {
    // ENUMS
    private enum MatchState {
        starting,
        ongoing,
        ending
    }
    
    // VARIABLES
    [Header("GENERAL INFORMATION")]
    [Header("Data and GameObjects")]
    [SerializeField] private DefaultIngameData defaultIngameData;
    [SerializeField] private DefaultPlayerData defaultPlayerData;
    [SerializeField] private DefaultInputKeybind defaultInputKeybind;
    [SerializeField] private IngamePrepareHandler ingamePrepareHandler;
    [SerializeField] private GameObject arenaFloor;
    public CameraHandler cameraHandler;
    [SerializeField] private GameObject projectilePrefab;
    [HideInInspector] public readonly float COLLIDER_MARGIN = 1.165001f;
    
    [Space(5), Header("Ui Objects")]
    [SerializeField] private Text pointsText;
    [SerializeField] private Text playtimeText;
    [SerializeField] private List<Image> heartImages;
    [SerializeField] private List<Sprite> heartSprites; // 0: empty_heart, 1: full_heart
    [SerializeField] private List<Image> ghostImages;
    [SerializeField] private List<Sprite> ghostEmptySprites;
    [SerializeField] private List<Sprite> ghostFullSprites;
    [SerializeField] private Image ghostOnControlImage;
    [SerializeField] private Text cdText_basicPacman;
    [SerializeField] private Text cdText_uniquePacman;
    [SerializeField] private Text cdText_basicGhost;
    [SerializeField] private Text cdText_uniqueGhost;
    [SerializeField] private Image cdImage_basicPacman;
    [SerializeField] private Image cdImage_uniquePacman;
    [SerializeField] private Image cdImage_basicGhost;
    [SerializeField] private Image cdImage_uniqueGhost;
    [SerializeField] private Image cdIcon_uniquePacman;
    [SerializeField] private Image cdIcon_uniqueGhost;
    [SerializeField] private List<Sprite> uniqueSkillsPacmanSprites;    // 0: normal, 1: monster
    [SerializeField] private List<Sprite> uniqueSkillsGhostSprites;     // 0: blinky, 1: clyde, 2: inky, 3: pinky
    [SerializeField] private Image pacmanHealthImage;
    [SerializeField] private Image ghostHealthImage;

    [Space(10), Header("FIGHTER INFORMATION")]
    [Header("General")]
    [SerializeField] private int maxHealth;
    [SerializeField] private float jumpPower;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float blockSpeed;
    [SerializeField, Tooltip("For smooth fighter movement")] private float smoothTime;
    [SerializeField] private AttackInfo punchAttack;
    [SerializeField] private AttackInfo basicAttack;

    [Space(5), Header("Specifics")]
    [SerializeField] private FighterInfo FI_PacmanRegular;
    [SerializeField] private FighterInfo FI_PacmanMonster;
    [SerializeField] private FighterInfo FI_Blinky;
    [SerializeField] private FighterInfo FI_Clyde;
    [SerializeField] private FighterInfo FI_Inky;
    [SerializeField] private FighterInfo FI_Pinky;


    [Space(10), Header("FIGHTER BEHAVIORS")]
    [SerializeField] private FighterBehavior Pacman;
    [SerializeField] private FighterBehavior Ghost;


    private MatchState matchState;
    private float timeCounter;


    private void Awake() {
        PrepareAllData();
        InitializePacmanInfo();
        InitializeGhostInfo();

        matchState = MatchState.starting;
    }

    private void Start() {
        StartCoroutine(SetUi());
    }

    public void ToStart() {
        StartMatch();
    }

    private void Update() {
        if (matchState != MatchState.ongoing) { return; }
        UpdateFighterDirection();
        TickDebuffs();
    }

    public DefaultInputKeybind GetDefaultInputKeybind() { return defaultInputKeybind; }
    public int GetMaxHealth() { return maxHealth; }
    public float GetJumpPower() { return jumpPower; }
    public float GetWalkSpeed() { return walkSpeed; }
    public float GetBlockSpeed() { return blockSpeed; }
    public float GetSmoothTime() { return smoothTime; }
    public AttackInfo GetPunchAttack() { return punchAttack; }
    public AttackInfo GetBasicAttack() { return basicAttack; }
    public FighterBehavior GetPacman() { return Pacman; }
    public FighterBehavior GetGhost() { return Ghost; }
    public GameObject GetArenaFloor() { return arenaFloor; }

    private void PrepareAllData() {
        try { IngameDataManager.Initialize(defaultIngameData); } catch (Exception e) { Debug.LogWarning(e); }
        try { PlayerDataManager.Initialize(defaultPlayerData); } catch (Exception e) { Debug.LogWarning(e); }
        try { KeybindDataManager.Initialize(defaultInputKeybind); } catch (Exception e) { Debug.LogWarning(e); }
    }

    private void InitializePacmanInfo() {
        bool isMonster = IngameDataManager.LoadSpecificData<bool>("pacman_data.has_power_pellet");
        Pacman.Initialize(this, isMonster ? FI_PacmanMonster : FI_PacmanRegular, Ghost);
    }

    private void InitializeGhostInfo() {
        string ghostName = IngameDataManager.LoadSpecificData<string>("ghost_data.current_fighting");
        FighterInfo fighter;
        Color32 ghostColor;
        
        switch (ghostName) {
            case "blinky": fighter = FI_Blinky; ghostColor = new Color32(165, 38, 48, 220); break;
            case "clyde": fighter = FI_Clyde; ghostColor = new Color32(207, 87, 60, 220); break;
            case "pinky": fighter = FI_Pinky; ghostColor = new Color32(223, 132, 165, 220); break;
            case "inky": fighter = FI_Inky; ghostColor = new Color32(115, 190, 211, 220); break;
            default:
                Debug.Log("'ghost_data.current_fighting' cannot be found. Spawned 'Blinky' in arena instead.");
                ghostColor = new Color32(165, 38, 48, 220);
                fighter = FI_Blinky; break;
        }

        Ghost.gameObject.GetComponent<SpriteRenderer>().color = ghostColor;
        Ghost.Initialize(this, fighter, Pacman);
    }

    private void StartMatch() {
        matchState = MatchState.ongoing;
        StartCoroutine(TickPlaytime());

        Pacman.enabled = true;
        Ghost.enabled = true;
    }

    public void EndMatch() {
        matchState = MatchState.ending;
        StopCoroutine(TickPlaytime());

        if (Pacman.currentHealth <= 0) {
            ingamePrepareHandler.SetDataForWinning("ghost", "pacman");
        } else if (Ghost.currentHealth <= 0) {
            ingamePrepareHandler.SetDataForWinning("pacman", IngameDataManager.LoadSpecificData<string>("ghost_data.current_fighting"));
        }
        StartCoroutine(SetUi());
    }

    private IEnumerator TickPlaytime() {
        while (true) {
            yield return new WaitForSeconds(1.0f);
            
            int playtime = IngameDataManager.LoadSpecificData<int>("pacman_data.playtime");
            IngameDataManager.SaveSpecificData("pacman_data.playtime", playtime + 1);

            System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(playtime);
            playtimeText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
        }
    }

    private void UpdateFighterDirection() {
        if (Pacman.transform.position.x < Ghost.transform.position.x) {
            if (!Pacman.facingLeft) { return; }
            Pacman.facingLeft = false;
            Ghost.facingLeft = true;

            Pacman.GetSprite().flipX = false;
            Ghost.GetSprite().flipX = true;

            Pacman.GetAttackHitbox().transform.localScale = new Vector3(1, 1, 1);
            Ghost.GetAttackHitbox().transform.localScale = new Vector3(-1, 1, 1);
        } else {
            if (Pacman.facingLeft) { return; }
            Pacman.facingLeft = true;
            Ghost.facingLeft = false;

            Pacman.GetSprite().flipX = true;
            Ghost.GetSprite().flipX = false;

            Pacman.GetAttackHitbox().transform.localScale = new Vector3(-1, 1, 1);
            Ghost.GetAttackHitbox().transform.localScale = new Vector3(1, 1, 1);
        }
    }

    public ProjectileBehavior SpawnProjectile(FighterBehavior owner, AttackInfo attackInfo, int attackDirection) {
        Vector3 spawnPosition = attackInfo.projectileSpawnOnOwner ? owner.gameObject.transform.position : owner.opponent.gameObject.transform.position + (Vector3.up * 8);
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        ProjectileBehavior projectileBehavior = projectile.GetComponent<ProjectileBehavior>();

        projectileBehavior.Spawn(owner, attackInfo, attackDirection);
        return projectileBehavior;
    }

    private void TickDebuffs() {
        timeCounter += Time.deltaTime;
        if (timeCounter < .5f) { return; }
        timeCounter -= .5f;
        
        if (Pacman.DOTDuration > 0) {
            Pacman.DOTDuration -= .5f;
            Pacman.Hit(Pacman.DOTAttack, false, 0);
        }
        
        if (Ghost.DOTDuration > 0) {
            Ghost.DOTDuration -= .5f;
            Ghost.Hit(Ghost.DOTAttack, false, 0);
        }
    }

    private IEnumerator SetUi() {
        yield return new WaitForSeconds(0.2f);
        int points = IngameDataManager.LoadSpecificData<int>("pacman_data.points"); 
        pointsText.text = points.ToString("00,000,000");

        int playtime = IngameDataManager.LoadSpecificData<int>("pacman_data.playtime");
        System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(playtime);
        playtimeText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);

        int lives = IngameDataManager.LoadSpecificData<int>("pacman_data.lives");
        for (int i = 0; i < heartImages.Count; i++) {
            heartImages[i].sprite = i < lives ? heartSprites[1] : heartSprites[0];
        }

        List<string> ghostNames = new List<string> { "blinky", "clyde", "inky", "pinky" };
        List<string> aliveGhosts = IngameDataManager.LoadSpecificData<List<string>>("ghost_data.list_alive");
        for (int i = 0; i < ghostNames.Count; i++) {
            ghostImages[i].sprite = (aliveGhosts.Contains(ghostNames[i])) ? ghostFullSprites[i] : ghostEmptySprites[i];
        }

        int spriteIndex = IngameDataManager.LoadSpecificData<string>("ghost_data.current_fighting") switch {
            "blinky" => 0, "clyde" => 1, "inky" => 2, "pinky" => 3, _ => -1
        };
        ghostOnControlImage.sprite = ghostFullSprites[spriteIndex];
        cdIcon_uniqueGhost.sprite = uniqueSkillsGhostSprites[spriteIndex];

        cdIcon_uniquePacman.sprite = uniqueSkillsPacmanSprites[IngameDataManager.LoadSpecificData<bool>("pacman_data.has_power_pellet") ? 1 : 0];
        cdImage_basicPacman.enabled = false;
        cdImage_basicGhost.enabled = false;
        cdImage_uniquePacman.enabled = false;
        cdImage_uniqueGhost.enabled = false;
    }

    public void SetBasicCooldownUi(string character, float cooldown) {
        if (character == "pacman") {
            cdImage_basicPacman.enabled = cooldown > 0f ? true : false;
            cdText_basicPacman.text = cooldown > 0f ? $"{cooldown:F1}s" : "";
        } else if (character == "ghost") {
            cdImage_basicGhost.enabled = cooldown > 0f ? true : false;
            cdText_basicGhost.text = cooldown > 0f ? $"{cooldown:F1}s" : "";
        }
    }

    public void SetUniqueCooldownUi(string character, float cooldown) {
        if (character == "pacman") {
            cdImage_uniquePacman.enabled = cooldown > 0f ? true : false;
            cdText_uniquePacman.text = cooldown > 0f ? $"{cooldown:F1}s" : "";
        } else if (character == "ghost") {
            cdImage_uniqueGhost.enabled = cooldown > 0f ? true : false;
            cdText_uniqueGhost.text = cooldown > 0f ? $"{cooldown:F1}s" : "";
        }
    }

    public void SetHealthUi() {
        float pacmanCurrentHealth = Mathf.Clamp(Pacman.currentHealth, 0, maxHealth);
        pacmanHealthImage.fillAmount = pacmanCurrentHealth / maxHealth;
        float ghostCurrentHealth = Mathf.Clamp(Ghost.currentHealth, 0, maxHealth);
        ghostHealthImage.fillAmount = ghostCurrentHealth / maxHealth;
    }
}
