using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public enum AttackCategory {
    punch,
    basic,
    unique,
    DOT
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
    [SerializeField] private DefaultIngameData defaultIngameData;
    [SerializeField] private DefaultPlayerData defaultPlayerData;
    [SerializeField] private DefaultInputKeybind defaultInputKeybind;
    [SerializeField] private GameObject arenaFloor;
    public CameraHandler cameraHandler;
    [SerializeField] private GameObject projectilePrefab;
    [HideInInspector] public readonly float COLLIDER_MARGIN = 0.02f;

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
        StartMatch(); // add begin countdown before this
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
        
        switch (ghostName) {
            case "Blinky": fighter = FI_Blinky; break;
            case "Clyde": fighter = FI_Clyde; break;
            case "Pinky": fighter = FI_Pinky; break;
            case "Inky": fighter = FI_Inky; break;
            default:
                Debug.Log("'ghost_data.current_fighting' cannot be found. Spawned 'Blinky' in arena instead.");
                fighter = FI_Blinky; break;
        }

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
    }

    private IEnumerator TickPlaytime() {
        while (true) {
            yield return new WaitForSeconds(1.0f);
            
            int playtime = IngameDataManager.LoadSpecificData<int>("pacman_data.playtime");
            IngameDataManager.SaveSpecificData("pacman_data.playtime", playtime + 1);

            System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(playtime);
            // playtimeText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
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
}
