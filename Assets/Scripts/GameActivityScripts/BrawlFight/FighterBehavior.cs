using System;
using System.Collections;
using System.Collections.Generic;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;


public class FighterBehavior : MonoBehaviour {
    // ENUMS
    private enum State {
        grounded,
        crouched,
        jumping,
        blocking,
        block_walking,
        block_stunned,
        stunned,
        knocked,
        aired,
        charging
    }

    private enum WalkDirection {
        none = 0,
        left = -1,
        right = 1,
        both = 2
    }


    // SCRIPTS
    private BrawlManager brawlManager;
    private FighterBehavior opponent;

    // STATS
    private FighterInfo fighterInfo;
    private float movementSmoothTime;
    private float walkSpeed;
    private float blockSpeed;

    // COMPONENTS
    private SpriteRenderer fighterSprite;
    private BoxCollider2D fighterHitbox;
    private Rigidbody2D fighterPhysics;
    private BoxCollider2D attackHitbox;
    

    // DYNAMICS
    private State currentState;
    private int currentHealth;
    private WalkDirection walkDirection;
    private float horizontalVelocity;
    [HideInInspector] public bool facingLeft;
    private float timer = 0f;
    private AttackInfo lastAttack;
    private AttackInfo queuedAttack;

    
    // GETTERS AND SETTERS
    public SpriteRenderer GetSprite() { return fighterSprite; }
    public BoxCollider2D GetHitbox() { return fighterHitbox; }
    public Rigidbody2D GetPhysics() { return fighterPhysics; }
    public BoxCollider2D GetAttackHitbox() { return attackHitbox; }


    public void Initialize(BrawlManager _brawlManager, FighterInfo _fighterInfo, FighterBehavior _opponent) {
        brawlManager = _brawlManager;
        fighterInfo = _fighterInfo;
        opponent = _opponent;

        fighterSprite = gameObject.GetComponent<SpriteRenderer>();
        fighterHitbox = gameObject.GetComponent<BoxCollider2D>();
        fighterPhysics = gameObject.GetComponent<Rigidbody2D>();
        attackHitbox = gameObject.transform.GetChild(0).GetComponent<BoxCollider2D>();

        currentHealth = brawlManager.GetMaxHealth();
        movementSmoothTime = brawlManager.GetSmoothTime();
        walkSpeed = brawlManager.GetWalkSpeed();
        blockSpeed = brawlManager.GetBlockSpeed();
    }

    private void Start() {
        RegisterKeyActions();
    }

    private void OnDestroy() {
        KeybindDataManager.ResetKeyActions();
    }

    private void Update() {
        TickPreInput();
        if (timer > 0) { KeybindDataManager.Update(); }
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
        walkDirection = WalkDirection.none; // Reset Movement Input
        UpdateCurrentState();
    }

    private void TickPostInput() {

    }

    private bool IsGrounded() {
        GameObject arenaFloor = brawlManager.GetArenaFloor();
        float floorValue = arenaFloor.transform.position.y + arenaFloor.transform.localScale.y / 2;
        float feetValue = gameObject.transform.position.y - gameObject.transform.localScale.y / 2;

        return feetValue < floorValue + brawlManager.COLLIDER_MARGIN;
    }

    private void UpdateCurrentState() {
        if (timer > 0) { return; }

        switch (currentState) {

            case State.grounded: return;

            case State.jumping:
                if (!IsGrounded()) { return; }
                currentState = State.grounded;
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
        Debug.Log("Shorten Hitbox"); // Implement this when sprites are done
    }

    private void WalkLeft() {
        if (currentState != State.grounded) { return; }

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
        if (currentState != State.grounded) { return; }
        
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

    private void Punch() {
        if (currentState != State.grounded) { return; }
        
        currentState = State.charging;
        if (lastAttack && lastAttack.attackCategory == AttackCategory.punch) {
            timer = lastAttack.nextAttack.chargeDuration;
            queuedAttack = lastAttack.nextAttack;
        } else {
            AttackInfo punchAttack = brawlManager.GetPunchAttack();
            timer = punchAttack.chargeDuration;
            queuedAttack = punchAttack;
        }
    }

    private void UseBasic() {

    }

    private void UseUnique() {

    }

    public void Damage(int damage) {

    }
}


// [System.Serializable] public class PlayerBehavior : MonoBehaviour {
//     private const float COLLIDER_MARGIN = 0.02f;

//     [Header("Fighter Stats")]
//     [SerializeField] private float maxHealth;
//     [SerializeField] private Attack punchAttack;
//     [SerializeField] private Attack basicAttack;
//     [SerializeField] private InputHandler inputHandler;

//     private BrawlManager brawlManager;
//     private BoxCollider2D boxCollider;
//     private Rigidbody2D rigidBody;
//     private GameObject attackBox;
//     private float horizontalValue;
//     private float horizontalVelocity; // used for smooth damp
//     private BoxCollider2D opponentCollider;
//     private PlayerBehavior opponentBehavior;
//     private Rigidbody2D opponentRigidBody;
//     private Attack lastAttack;

//     public float damageMultiplier; // add skill power hp here
//     public GameObject healthUi;

//     [HideInInspector] public bool facingRight;
//     [HideInInspector] public bool isBlocking;
//     [HideInInspector] public bool isCrouching;
//     [HideInInspector] public Attack queuedAttack;
//     [HideInInspector] public float chargeAttack;
//     [HideInInspector] public float stunTime;
//     [HideInInspector] public float attackCooldown;
//     [HideInInspector] public float health;

//     [System.Serializable] public class InputHandler {
//         [SerializeField] private KeyCode jumpKey;
//         [SerializeField] private KeyCode leftKey;
//         [SerializeField] private KeyCode crouchKey;
//         [SerializeField] private KeyCode rightKey;
//         [SerializeField] private KeyCode punchKey;
//         [SerializeField] private KeyCode basicSkillKey;
//         private PlayerBehavior playerBehavior;

//         public void Initialize(PlayerBehavior behavior) {
//             playerBehavior = behavior;
//         }

//         public float getHorizontalValue() {
//             if (IsWalkingLeft() && IsWalkingRight()) {
//                 playerBehavior.isBlocking = true;
//                 return 0;
//             }
//             playerBehavior.isBlocking = false;

//             if (IsWalkingLeft()) {
//                 if (!playerBehavior.facingRight) { return -1; }
//                 playerBehavior.isBlocking = true;
//                 return -playerBehavior.brawlManager.blockMoveReduction;
//             }

//             if (IsWalkingRight()) {
//                 if (playerBehavior.facingRight) { return 1; }
//                 playerBehavior.isBlocking = true;
//                 return playerBehavior.brawlManager.blockMoveReduction;
//             }

//             return 0;
//         }

//         public bool IsWalkingLeft() { return Input.GetKey(leftKey); }
//         public bool IsWalkingRight() { return Input.GetKey(rightKey); }
//         public bool IsJumping() { return Input.GetKey(jumpKey); }
//         public bool Crouching() { return Input.GetKey(crouchKey); }
//         public bool IsPunching() { return Input.GetKeyDown(punchKey); }
//         public bool UsedBasicSkill() { return Input.GetKeyDown(basicSkillKey); }
//     }

//     private void Start() {
//         brawlManager = GameObject.FindGameObjectWithTag("BrawlScript").GetComponent<BrawlManager>();
//         rigidBody = gameObject.GetComponent<Rigidbody2D>();
//         boxCollider = gameObject.GetComponent<BoxCollider2D>();
//         attackBox = gameObject.transform.GetChild(0).gameObject;

//         horizontalValue = 0;
//         chargeAttack = 0;
//         facingRight = false;

//         // for smoothdamp
//         horizontalVelocity = 0;

//         GameObject[] fighters = brawlManager.getFighters();
//         for (int i = 0; i < 2; i++ ) {
//             if (fighters[i] == gameObject) { continue; }
            
//             GameObject opponent = fighters[i];
//             opponentBehavior = opponent.GetComponent<PlayerBehavior>();
//             opponentCollider = opponent.GetComponent<BoxCollider2D>();
//             opponentRigidBody = opponent.GetComponent<Rigidbody2D>();
//             break;
//         }

//         inputHandler.Initialize(this);
//         health = maxHealth;
//     }

//     private void Update() {
//         // Stun Logic
//         if (stunTime > 0) {
//             stunTime -= Time.deltaTime;
//             if (stunTime > 0) { return; }
//         }

//         // ATTACKING
//         // punch attack sequence
//         if (inputHandler.IsPunching()) {
//             if (queuedAttack && lastAttack && lastAttack.attackCategory == AttackCategory.punch) {
//                 queuedAttack = lastAttack.nextAttack;
//             } else {
//                 queuedAttack = punchAttack;
//             }
//         }

//         // basic skill
//         if (inputHandler.UsedBasicSkill()) {

//         }

//         // Post attack cd
//         if (attackCooldown > 0) {
//             attackCooldown -= Time.deltaTime;
//             if (attackCooldown > 0) { return; }
//         }

//         // Pre attack cd
//         if (queuedAttack && chargeAttack <= 0) { chargeAttack = queuedAttack.chargeDuration; }
//         if (chargeAttack > 0) {
//             chargeAttack -= Time.deltaTime;
//             if (chargeAttack > 0) { return; }
//             registerAttack(queuedAttack);
//         }

//         // MOVEMENT
//         // crouching
//         if (inputHandler.Crouching()) {
//             Debug.Log("Reduce Hitbox");
//             horizontalValue = 0;
//             isCrouching = true;
//             return;
//         }
//         isCrouching = false;

//         // horizontal movement
//         if (!IsGrounded()) { return; }
//         horizontalValue = Mathf.SmoothDamp(
//             horizontalValue,
//             inputHandler.getHorizontalValue(),
//             ref horizontalVelocity,
//             brawlManager.getSmoothTime()
//         );
        
//         // jumping 
//         if (!inputHandler.IsJumping()) { return; }
//         rigidBody.velocity = new Vector2(rigidBody.velocity.x, brawlManager.getJumpStrength());
//     }

//     private void FixedUpdate() {
//         if (!isCrouching && stunTime <= 0 && chargeAttack <= 0 && attackCooldown <= 0) {
//             rigidBody.velocity = new Vector2(horizontalValue * brawlManager.getCharacterSpeed(), rigidBody.velocity.y);
//         }
//     }

//     private bool IsGrounded() {
//         GameObject arenaFloor = brawlManager.getArenaFloor();
//         float floorValue = arenaFloor.transform.position.y + arenaFloor.transform.localScale.y / 2;
//         float feetValue = gameObject.transform.position.y - gameObject.transform.localScale.y / 2;

//         return feetValue < floorValue + COLLIDER_MARGIN;
//     }

//     private void registerAttack(Attack attack) {
//         // reminder for self: implement last attack then remove the stuff from register attack 
//         BoxCollider2D n = attackBox.GetComponent<BoxCollider2D>();
//         if (n) { Destroy(n); }

//         BoxCollider2D attackHitbox = attackBox.AddComponent<BoxCollider2D>();
//         if (!attack) {Debug.Log("AttackArgument Missing"); return; }
//         Debug.Log(attack.name);
//         attackHitbox.offset = attack.attackBoxOffset;
//         attackHitbox.size = attack.attackBoxSize;

//         int attackDirection = facingRight ? 1 : -1;

//         if (attackHitbox.bounds.Intersects(opponentCollider.bounds)) {
//             // Attack hit
//             rigidBody.velocity = new Vector2(attack.rootVelocity.x * attackDirection, attack.rootVelocity.y);
//             if (opponentBehavior.isBlocking) {
//                 opponentBehavior.stun(attack.stunDuration / 2, new Vector2(attack.otherVelocity.x * attackDirection / 2, attack.otherVelocity.y));
//                 attackCooldown = attack.hitDuration;
//                 lastAttack = null;
//             } else {
//                 opponentBehavior.stun(attack.stunDuration, new Vector2(attack.otherVelocity.x * attackDirection, attack.otherVelocity.y));
//                 opponentBehavior.damage(attack.damage * damageMultiplier);
//                 attackCooldown = attack.hitDuration;
//                 lastAttack = attack;

//                 if (attack.shakeCam) { brawlManager.cameraHandler.Shake(attack.camShakeDuration, attack.camShakeStrength); }
//             }

//             Debug.Log("Apply Damage");
//         } else {
//             attackCooldown = attack.missDuration;
//             lastAttack = null;
//         }

//         queuedAttack = null;
//     }

//     public void stun(float seconds, Vector2 knockback) {
//         stunTime = seconds;
//         rigidBody.velocity = knockback;
//         queuedAttack = null;
//         lastAttack = null;
//     }

//     public void damage(float amount) {
//         health -= amount;
//         healthUi.transform.localScale = new Vector3(health / maxHealth, 1, 1);
//     }

//     // private BoxCollider2D setAttackBox(Attack attackInfo) {
//     //     BoxCollider2D attackHitbox = attackBox.AddComponent<BoxCollider2D>();
//     //     attackHitbox.offset = punchAttack.attackBoxOffset;
//     //     attackHitbox.size = punchAttack.attackBoxOffset;

//     //     return attackHitbox;
//     // }
// }