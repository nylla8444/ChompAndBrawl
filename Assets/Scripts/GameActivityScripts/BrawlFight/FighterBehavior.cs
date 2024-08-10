using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FighterBehavior : MonoBehaviour {
    // ENUMS
    public enum State {
        grounded,
        crouched,
        jumping,
        blocking,
        block_walking,
        block_stunned,
        stunned,
        knocked,
        aired,
        pre_attack,
        post_attack
    }

    private enum WalkDirection {
        none = 0,
        left = -1,
        right = 1,
        both = 2
    }


    // SCRIPTS
    private BrawlManager brawlManager;
    [HideInInspector] public FighterBehavior opponent;

    // STATS
    [HideInInspector] public FighterInfo fighterInfo;
    private float movementSmoothTime;
    private float walkSpeed;
    private float blockSpeed;
    [HideInInspector] float attackChargeBias = 1f;

    // COMPONENTS
    private SpriteRenderer fighterSprite;
    private BoxCollider2D fighterHitbox;
    private Rigidbody2D fighterPhysics;
    private BoxCollider2D attackHitbox;
    private Animator animator;
    

    // DYNAMICS
    [SerializeField] private State currentState;
    [SerializeField] public float currentHealth;
    [SerializeField] private WalkDirection walkDirection;
    private float horizontalVelocity;
    public bool facingLeft;
    private float timer = 0f;
    private AttackInfo lastAttack;
    private AttackInfo queuedAttack;
    [SerializeField] private float basicCooldown = 0;
    [SerializeField] private float uniqueCooldown = 0;
    public float DOTDuration = 0;
    [HideInInspector] public AttackInfo DOTAttack;
    [SerializeField] private float SugarRushDuration = 0f;
    public AllAttacks currentAttack;
    public AllAttacks lastCurrentAttack;
    private float debuffTimer;

    
    // GETTERS AND SETTERS
    public SpriteRenderer GetSprite() { return fighterSprite; }
    public BoxCollider2D GetHitbox() { return fighterHitbox; }
    public Rigidbody2D GetPhysics() { return fighterPhysics; }
    public BoxCollider2D GetAttackHitbox() { return attackHitbox; }
    public State GetState() { return currentState; }


    public void Initialize(BrawlManager _brawlManager, FighterInfo _fighterInfo, FighterBehavior _opponent) {
        brawlManager = _brawlManager;
        fighterInfo = _fighterInfo;
        opponent = _opponent;

        attackHitbox = gameObject.transform.GetChild(0).GetComponent<BoxCollider2D>();
        fighterSprite = gameObject.GetComponent<SpriteRenderer>();
        fighterHitbox = gameObject.GetComponent<BoxCollider2D>();
        fighterPhysics = gameObject.GetComponent<Rigidbody2D>();
        animator = gameObject.GetComponent<Animator>();

        currentHealth = brawlManager.GetMaxHealth();
        movementSmoothTime = brawlManager.GetSmoothTime();
        walkSpeed = brawlManager.GetWalkSpeed();
        blockSpeed = brawlManager.GetBlockSpeed();
    }

    private void Start() {
        RegisterKeyActions();
    }

    public void ResetKeys() {
        KeybindDataManager.ResetKeyActions();
    }

    private void Update() {

        if (!animator) {
            Debug.LogWarning("Disabled Script because no animator found"); // REmove this after sprites are added
            enabled = false;
            return;
        }

        TickPreInput();
        // Debug.Log($"{gameObject.name} {currentState}");
        // Debug.Log($"{currentState} | {queuedAttack} | {lastAttack}");

        if (timer <= 0 || currentState == State.post_attack) {
            KeybindDataManager.Update();
        }

        TickPostInput();
    }

private void FixedUpdate() {
    if (currentState == State.blocking) {
        fighterPhysics.velocity = Vector2.zero;
    } else if (currentState == State.grounded || currentState == State.block_walking) {
        float targetSpeed = (currentState == State.grounded) ? walkSpeed : blockSpeed;
        float desiredVelocityX = (int)walkDirection * targetSpeed;

        float newVelocityX = Mathf.SmoothDamp(
            fighterPhysics.velocity.x,
            desiredVelocityX,
            ref horizontalVelocity,
            movementSmoothTime
        );

        fighterPhysics.velocity = new Vector2(newVelocityX, fighterPhysics.velocity.y);
    }
    
    walkDirection = WalkDirection.none;
    if (currentState == State.block_walking || currentState == State.blocking) { currentState = State.grounded; }
}

    private void RegisterKeyActions() {
        string fighterName = brawlManager.GetPacman() == this ? "pacman" : "ghost";

        KeybindDataManager.RegisterKeyAction(fighterName + ".jump", Jump);
        KeybindDataManager.RegisterKeyAction(fighterName + ".crouch", Crouch);
        KeybindDataManager.RegisterKeyAction(fighterName + ".walk_left", WalkLeft);
        KeybindDataManager.RegisterKeyAction(fighterName + ".walk_right", WalkRight);
        KeybindDataManager.RegisterKeyAction(fighterName + ".punch", Punch);
        KeybindDataManager.RegisterKeyAction(fighterName + ".basic", UseBasic);
        KeybindDataManager.RegisterKeyAction(fighterName + ".unique", UseUnique);
    }

    private void TickPreInput() {
        TickTimer();
        TickSkillCooldowns();
        UpdateCurrentState();
    }

    private void TickPostInput() {
        HandleAnimations();
        TickDebuffs();
    }

    private bool IsGrounded() {
        GameObject arenaFloor = brawlManager.GetArenaFloor();
        float floorValue = arenaFloor.transform.position.y + arenaFloor.transform.localScale.y / 2;
        float feetValue = gameObject.transform.position.y - gameObject.transform.localScale.y / 2;

        return feetValue < floorValue + brawlManager.COLLIDER_MARGIN;
    }

    private void UpdateCurrentState() {
        if (currentState == State.stunned && !IsGrounded()) {
            currentState = State.aired;
        } else if (currentState == State.aired && IsGrounded()) {
            currentState = State.stunned;
        }

        if (timer > 0) { return; }

        switch (currentState) {

            case State.grounded: return;
            case State.blocking: return;
            case State.block_walking: return;

            case State.jumping:
                if (!IsGrounded()) { return; }
                currentState = State.grounded;
                return;

           case State.pre_attack:
                RegisterAttack();
                currentState = State.post_attack;
                return; 

            case State.post_attack:
                if (queuedAttack) { currentState = State.pre_attack; return; }
                currentState = State.grounded;
                lastAttack = null;
                currentAttack = AllAttacks.none;
                return;

            case State.stunned:
                currentAttack = AllAttacks.none;
                currentState = State.grounded;
                return;

            case State.aired:
                currentAttack = AllAttacks.none;
                currentState = State.grounded;
                return;

            case State.knocked:
                currentAttack = AllAttacks.none;
                return;

            default:
                currentState = State.grounded;
                return;
        }
    }

    private void TickTimer() {
        if (timer <= 0) { return; }
        timer -= Time.deltaTime;
    }

    private void Jump() {
        if (currentState != State.grounded) { return; }
        currentState = State.jumping;
        fighterPhysics.velocity = new Vector2(fighterPhysics.velocity.x, brawlManager.GetJumpPower());
    }

    private void Crouch() {
        if (currentState != State.grounded) { return; }
        currentState = State.crouched;
        Debug.LogWarning("Modify Hitbox"); // do this after sprites are implemented
    }
    
    private bool CanWalk() {
        HashSet<State> allowedStates = new() {
            State.grounded,
            State.blocking,
            State.block_walking,
        };

        return allowedStates.Contains(currentState);
    }

    private void WalkLeft() {
        if (!CanWalk()) { return; }

        switch (walkDirection) {
            case WalkDirection.none:
                if (!facingLeft) { currentState = State.block_walking; }
                walkDirection = WalkDirection.left;
                break;

            case WalkDirection.right:
                currentState = State.blocking;
                walkDirection = WalkDirection.both;
                break;
        }
    }

    private void WalkRight() {
        if (!CanWalk()) { return; }
        
        switch (walkDirection) {
            case WalkDirection.none:
                if (facingLeft) { currentState = State.block_walking; }
                walkDirection = WalkDirection.right;
                break;

            case WalkDirection.left:
                currentState = State.blocking;
                walkDirection = WalkDirection.both;
                break;
        }
    }

    private bool CanAttack() {
        HashSet<State> allowedStates = new() {
            State.grounded,
            State.crouched,
            State.blocking,
            State.block_walking,
            State.post_attack
        };

        return allowedStates.Contains(currentState);
    }

    private void Punch() {
        if (!CanAttack()) { return; }
        if (currentState != State.post_attack) { currentState = State.pre_attack; }
        
        if (queuedAttack && lastAttack && lastAttack.nextAttack && lastAttack.attackCategory == AttackCategory.punch) {
            if (queuedAttack == lastAttack.nextAttack) { return; }
            queuedAttack = lastAttack.nextAttack;
            timer = queuedAttack.chargeDuration * attackChargeBias;
        } else {
            AttackInfo punchAttack = brawlManager.GetPunchAttack();
            if (queuedAttack == punchAttack) { return; }
            queuedAttack = punchAttack;
            timer = punchAttack.chargeDuration * attackChargeBias;
        }
        currentAttack = queuedAttack.specificAttack;
    }

    private void UseBasic() {
        if (!CanAttack()) { return; }
        if (basicCooldown > 0) { return; }
        if (queuedAttack && queuedAttack.attackCategory == AttackCategory.basic) { return; }
        if (currentState != State.post_attack) { currentState = State.pre_attack; }

        AttackInfo basicAttack = brawlManager.GetBasicAttack();
        queuedAttack = basicAttack;
        currentAttack = queuedAttack.specificAttack;
        timer = queuedAttack.chargeDuration * attackChargeBias;
    }

    private void UseUnique() {
        if (!CanAttack()) { return; }
        if (uniqueCooldown > 0) { return; }
        if (queuedAttack && queuedAttack.attackCategory == AttackCategory.unique) { return; }
        if (currentState != State.post_attack) { currentState = State.pre_attack; }

        AttackInfo uniqueAttack = fighterInfo.UniqueAttackInfo;

        if (uniqueAttack.name == "SugarRush") {
            SugarRushDuration = uniqueAttack.chargeDuration;
            attackChargeBias = 0.5f;
            uniqueCooldown = uniqueAttack.attackCooldown;
            timer = 0.5f;
            return;
        }

        queuedAttack = uniqueAttack;
        currentAttack = queuedAttack.specificAttack;
        timer = queuedAttack.chargeDuration;
    }

    public void Hit(AttackInfo attack, bool blocked, int attackDirection) {
        // DEBUFFS
        if (attack.attackCategory == AttackCategory.DOT) {
            currentHealth -= attack.damage;

            brawlManager.SetHealthUi();
            return;
        }

        // ATTACKS
        lastAttack = null;
        queuedAttack = null;

        if (blocked && attack) {
            currentHealth -=  attack.damage / 2 * opponent.fighterInfo.DamageMultipler;
            fighterPhysics.velocity = new Vector2(attack.otherVelocity.x / 2 * attackDirection, attack.otherVelocity.y);

            currentState = State.block_stunned;
            timer = attack.stunDuration / 2;
        } else {
            currentHealth -= attack.damage * opponent.fighterInfo.DamageMultipler;
            fighterPhysics.velocity = new Vector2(attack.otherVelocity.x * attackDirection, attack.otherVelocity.y);
            brawlManager.cameraHandler.Shake(attack.camShakeDuration, attack.camShakeStrength);

            currentState = State.stunned;
            timer = attack.stunDuration;
        }

        //  fighterPhysics.velocity = blocked ? attack.otherVelocity : new Vector2(attack.otherVelocity.x, fighterPhysics.velocity.y);
        brawlManager.SetHealthUi();

        if (currentHealth <= 0) { currentState = State.knocked; brawlManager.EndMatch(); }
    }

    private void RegisterAttack() {
        int attackDirection = facingLeft ? -1 : 1;
        bool shouldQueueNextAttack;

        if (!queuedAttack) {Debug.Log("Queued Attack Missing"); return; }
        
        // PROJECTILE ATTACKS
        if (queuedAttack.isProjectile) {
            brawlManager.SpawnProjectile(this, queuedAttack, attackDirection);
            timer = queuedAttack.hitDuration;

            WrapAttackRegister(attackDirection, false);
            return;
        }

        // MELEE ATTACKS
        attackHitbox.size = queuedAttack.attackBoxSize;
        attackHitbox.offset = queuedAttack.attackBoxOffset;

        if (attackHitbox.bounds.Intersects(opponent.fighterHitbox.bounds)) {
            // Attack hit
            fighterPhysics.velocity = new Vector2(queuedAttack.rootVelocity.x * attackDirection, queuedAttack.rootVelocity.y);
            if (opponent.currentState == State.blocking || opponent.currentState == State.block_walking) {
                // Blocked
                opponent.Hit(queuedAttack, true, attackDirection);
                timer = queuedAttack.hitDuration;
                shouldQueueNextAttack = false;
            } else {
                // Hit
                opponent.Hit(queuedAttack, false, attackDirection);
                timer = queuedAttack.hitDuration;
                shouldQueueNextAttack = true;
            }
        } else {
            // Miss
            timer = queuedAttack.missDuration;
            shouldQueueNextAttack = false;
        }

        WrapAttackRegister(attackDirection, shouldQueueNextAttack);
    }

    private void WrapAttackRegister(int attackDirection, bool queueNextAttack) {
        if (queuedAttack.attackCategory == AttackCategory.basic) {
            basicCooldown = queuedAttack.attackCooldown * attackChargeBias;
        } else if (queuedAttack.attackCategory == AttackCategory.unique) {
            uniqueCooldown = queuedAttack.attackCooldown;
        }

        fighterPhysics.velocity = queuedAttack.rootVelocity * attackDirection;
        currentState = State.post_attack;
        lastAttack = queueNextAttack ? queuedAttack : null;
        queuedAttack = null;
    }

    private void TickSkillCooldowns() {
        if (basicCooldown > 0) {
            basicCooldown -= Time.deltaTime;
            brawlManager.SetBasicCooldownUi(brawlManager.GetPacman() == this ? "pacman" : "ghost", basicCooldown);
        }

        if (uniqueCooldown > 0) {
            uniqueCooldown -= Time.deltaTime;
            brawlManager.SetUniqueCooldownUi(brawlManager.GetPacman() == this ? "pacman" : "ghost", uniqueCooldown);
        }

        if (SugarRushDuration > 0) {
            SugarRushDuration -= Time.deltaTime;
            if (SugarRushDuration <= 0) { attackChargeBias = 1.0f; }
        }
    }

    private void HandleAnimations() {
        animator.SetBool("isWalking", walkDirection == WalkDirection.left || walkDirection == WalkDirection.right);
        animator.SetInteger("CurrentState", (int)currentState);
        animator.SetInteger("currentAttack", (int)currentAttack);

        if (lastCurrentAttack == currentAttack) { return; }
        lastCurrentAttack = currentAttack;
        animator.SetTrigger("AttackChanged");
    }

    void TickDebuffs() {
        if (DOTDuration <= 0) { return; }
        debuffTimer += Time.deltaTime;

        if (debuffTimer >= .5f) {
            debuffTimer -= .5f;
            DOTDuration -= .5f;
            currentHealth -= DOTAttack.damage;
        }
        
    }
}
