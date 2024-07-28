using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

// public class PlayerBehavior : PacmanMazeController
// {

//     private void HandleInput(string action)
//     {
//         if (!BrawlManager.matchOngoing) return;

//         Debug.Log(action);
//     }
// }


[System.Serializable] public class PlayerBehavior : MonoBehaviour {
    private const float COLLIDER_MARGIN = 0.02f;

    [Header("Fighter Stats")]
    [SerializeField] private float health;
    [SerializeField] private Attack punchAttack;
    [SerializeField] private InputHandler inputHandler;

    private BrawlManager brawlManager;
    private BoxCollider2D boxCollider;
    private Rigidbody2D rigidBody;
    private GameObject attackBox;
    private float horizontalValue;
    private float horizontalVelocity; // used for smooth damp
    private BoxCollider2D opponentCollider;
    private PlayerBehavior opponentBehavior;
    private Rigidbody2D opponentRigidBody;
    private Attack lastAttack;

    [HideInInspector] public bool facingRight;
    [HideInInspector] public bool isBlocking;
    [HideInInspector] public bool isCrouching;
    [HideInInspector] public Attack queuedAttack;
    [HideInInspector] public float chargeAttack;
    [HideInInspector] public float stunTime;
    [HideInInspector] public float attackCooldown;

    [System.Serializable] public class InputHandler {
        [SerializeField] private KeyCode jumpKey;
        [SerializeField] private KeyCode leftKey;
        [SerializeField] private KeyCode crouchKey;
        [SerializeField] private KeyCode rightKey;
        [SerializeField] private KeyCode punchKey;
        private PlayerBehavior playerBehavior;

        public void Initialize(PlayerBehavior behavior) {
            playerBehavior = behavior;
        }

        public float getHorizontalValue() {
            if (IsWalkingLeft() && IsWalkingRight()) {
                playerBehavior.isBlocking = true;
                return 0;
            }
            playerBehavior.isBlocking = false;

            if (IsWalkingLeft()) {
                if (!playerBehavior.facingRight) { return -1; }
                playerBehavior.isBlocking = true;
                return -playerBehavior.brawlManager.blockMoveReduction;
            }

            if (IsWalkingRight()) {
                if (playerBehavior.facingRight) { return 1; }
                playerBehavior.isBlocking = true;
                return playerBehavior.brawlManager.blockMoveReduction;
            }

            return 0;
        }

        public bool IsWalkingLeft() { return Input.GetKey(leftKey); }
        public bool IsWalkingRight() { return Input.GetKey(rightKey); }
        public bool IsJumping() { return Input.GetKey(jumpKey); }
        public bool Crouching() { return Input.GetKey(crouchKey); }
        public bool IsPunching() { return Input.GetKeyDown(punchKey); }
    }

    private void Start() {
        brawlManager = GameObject.FindGameObjectWithTag("BrawlScript").GetComponent<BrawlManager>();
        rigidBody = gameObject.GetComponent<Rigidbody2D>();
        boxCollider = gameObject.GetComponent<BoxCollider2D>();
        attackBox = gameObject.transform.GetChild(0).gameObject;

        horizontalValue = 0;
        chargeAttack = 0;
        facingRight = false;

        // for smoothdamp
        horizontalVelocity = 0;

        GameObject[] fighters = brawlManager.getFighters();
        for (int i = 0; i < 2; i++ ) {
            if (fighters[i] == gameObject) { continue; }
            
            GameObject opponent = fighters[i];
            opponentBehavior = opponent.GetComponent<PlayerBehavior>();
            opponentCollider = opponent.GetComponent<BoxCollider2D>();
            opponentRigidBody = opponent.GetComponent<Rigidbody2D>();
            break;
        }

        inputHandler.Initialize(this);
    }

    private void Update() {
        // Stun Logic
        if (stunTime > 0) {
            stunTime -= Time.deltaTime;
            if (stunTime > 0) { return; }
        }

        // ATTACKING
        // punch attack sequence
        if (inputHandler.IsPunching()) {
            if (queuedAttack && lastAttack && lastAttack.attackCategory == AttackCategory.punch) {
                queuedAttack = lastAttack.nextAttack;
            } else {
                queuedAttack = punchAttack;
            }
        }

        // Post attack cd
        if (attackCooldown > 0) {
            attackCooldown -= Time.deltaTime;
            if (attackCooldown > 0) { return; }
        }

        // Pre attack cd
        if (queuedAttack && chargeAttack <= 0) { chargeAttack = queuedAttack.chargeDuration; }
        if (chargeAttack > 0) {
            chargeAttack -= Time.deltaTime;
            if (chargeAttack > 0) { return; }
            registerAttack(queuedAttack);
        }

        // MOVEMENT
        // crouching
        if (inputHandler.Crouching()) {
            Debug.Log("Reduce Hitbox");
            horizontalValue = 0;
            isCrouching = true;
            return;
        }
        isCrouching = false;

        // horizontal movement
        if (!IsGrounded()) { return; }
        horizontalValue = Mathf.SmoothDamp(
            horizontalValue,
            inputHandler.getHorizontalValue(),
            ref horizontalVelocity,
            brawlManager.getSmoothTime()
        );
        
        // jumping 
        if (!inputHandler.IsJumping()) { return; }
        rigidBody.velocity = new Vector2(rigidBody.velocity.x, brawlManager.getJumpStrength());
    }

    private void FixedUpdate() {
        if (!isCrouching && stunTime <= 0 && chargeAttack <= 0 && attackCooldown <= 0) {
            rigidBody.velocity = new Vector2(horizontalValue * brawlManager.getCharacterSpeed(), rigidBody.velocity.y);
        }
    }

    private bool IsGrounded() {
        GameObject arenaFloor = brawlManager.getArenaFloor();
        float floorValue = arenaFloor.transform.position.y + arenaFloor.transform.localScale.y / 2;
        float feetValue = gameObject.transform.position.y - gameObject.transform.localScale.y / 2;

        return feetValue < floorValue + COLLIDER_MARGIN;
    }

    private void registerAttack(Attack attack) {
        // reminder for self: implement last attack then remove the stuff from register attack 
        BoxCollider2D attackHitbox = attackBox.AddComponent<BoxCollider2D>();
        if (!attack) {Debug.Log("AttackArgument Missing"); return; }
        Debug.Log(attack.name);
        attackHitbox.offset = attack.attackBoxOffset;
        attackHitbox.size = attack.attackBoxSize;

        int attackDirection = facingRight ? 1 : -1;

        if (attackHitbox.bounds.Intersects(opponentCollider.bounds)) {
            // Attack hit
            rigidBody.velocity = new Vector2(attack.rootVelocity.x * attackDirection, attack.rootVelocity.y);
            if (opponentBehavior.isBlocking) {
                opponentBehavior.stun(attack.stunDuration / 2, new Vector2(attack.otherVelocity.x * attackDirection / 2, attack.otherVelocity.y));
                attackCooldown = attack.hitDuration;
                lastAttack = null;
            } else {
                opponentBehavior.stun(attack.stunDuration, new Vector2(attack.otherVelocity.x * attackDirection, attack.otherVelocity.y));
                attackCooldown = attack.hitDuration;
                lastAttack = attack;

                if (attack.shakeCam) { brawlManager.cameraHandler.Shake(attack.camShakeDuration, attack.camShakeStrength); }
            }

            Debug.Log("Apply Damage");
        } else {
            attackCooldown = attack.missDuration;
            lastAttack = null;
        }

        queuedAttack = null;
    }

    public void stun(float seconds, Vector2 knockback) {
        stunTime = seconds;
        rigidBody.velocity = knockback;
        queuedAttack = null;
        
    }

    // private BoxCollider2D setAttackBox(Attack attackInfo) {
    //     BoxCollider2D attackHitbox = attackBox.AddComponent<BoxCollider2D>();
    //     attackHitbox.offset = punchAttack.attackBoxOffset;
    //     attackHitbox.size = punchAttack.attackBoxOffset;

    //     return attackHitbox;
    // }
}