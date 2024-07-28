using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// public class PlayerBehavior : PacmanMazeController
// {

//     private void HandleInput(string action)
//     {
//         if (!BrawlManager.matchOngoing) return;

//         Debug.Log(action);
//     }
// }


[System.Serializable] public class PlayerBehavior : MonoBehaviour {
    enum state {
        none,
        attack_charge,
        attack_cooldown,
        jumping,
        crouching,
    }
    private const float COLLIDER_MARGIN = 0.02f;

    [SerializeField] private InputHandler inputHandler;
    private BrawlManager brawlManager;
    private Rigidbody2D rigidBody;
    private BoxCollider2D boxCollider;
    private float horizontalValue;
    private float horizontalVelocity;
    private bool crouched;
    private float stunTime;
    private float attackCooldown;
    private GameObject attackBox;
    private int attackDirection;
    public bool facingRight;
    public bool isBlocking;
    [SerializeField] private Attack punchAttack;

    [System.Serializable] public class InputHandler {
        [SerializeField] private KeyCode jumpKey;
        [SerializeField] private KeyCode leftKey;
        [SerializeField] private KeyCode crouchKey;
        [SerializeField] private KeyCode rightKey;
        [SerializeField] private KeyCode punchKey;
        private PlayerBehavior playerBehavior;

        public void initialize(PlayerBehavior behavior) {
            playerBehavior = behavior;
        }
        public float GetHorizontal() {
            playerBehavior.isBlocking = false;
            if (!(Input.GetKey(leftKey) ^ Input.GetKey(rightKey))) { return 0; }

            if (Input.GetKey(leftKey)) {
                if (playerBehavior.facingRight) {
                    playerBehavior.isBlocking = true;
                    return -1 * playerBehavior.brawlManager.blockMoveSpeed;
                } else {
                    return -1;
                }
            }
            if (Input.GetKey(rightKey)) {
                if (playerBehavior.facingRight)  {
                    return 1;
                } else {
                    playerBehavior.isBlocking = true;
                    return 1 * playerBehavior.brawlManager.blockMoveSpeed;
                }
            }

            Debug.LogWarning("Input working improperly");
            return 0;
        }

        public bool IsJumping() { return Input.GetKey(jumpKey); }
        public bool IsCrouching() { return Input.GetKey(crouchKey); }
        public bool IsPunching() { return Input.GetKeyDown(punchKey); }
    }

    private void Start() {
        brawlManager = GameObject.FindGameObjectWithTag("BrawlScript").GetComponent<BrawlManager>();
        rigidBody = gameObject.GetComponent<Rigidbody2D>();
        boxCollider = gameObject.GetComponent<BoxCollider2D>();
        attackBox = gameObject.transform.GetChild(0).gameObject;

        horizontalValue = 0;
        stunTime = 0;
        attackCooldown = 0;
        attackDirection = 1;

        crouched = false;
        facingRight = false;

        // for smoothdamp
        horizontalVelocity = 0;

        inputHandler.initialize(this);
    }

    private void Update() {
        // Handle Stunning
        if (stunTime > 0) { stunTime -= Time.deltaTime; return; }
        
        // Handle Attacks
        // Attack Direction
        attackDirection = facingRight ? 1 : -1;

        // Attack Cooldown
        if (attackCooldown > 0) { attackCooldown -= Time.deltaTime; return; }

        // Basic Punch
        if (inputHandler.IsPunching()) {
            BoxCollider2D attackHitbox = setAttackBox(punchAttack);
            GameObject[] fighters = brawlManager.getFighters();

            for (int i = 0; i < 2; i++) {
                if (gameObject == fighters[i]) { continue; }
                if (isAttackHit(attackHitbox, brawlManager.getFighterCollider()[i])) {
                    PlayerBehavior otherBehavior = brawlManager.getFighterScript()[i];
                    Rigidbody2D otherRigidBody = brawlManager.getFighterRb()[i];

                    rigidBody.velocity = new Vector2(punchAttack.rootVelocity * attackDirection, rigidBody.velocity.y);
                    otherRigidBody.velocity = new Vector2(punchAttack.otherVelocity * attackDirection, otherRigidBody.velocity.y);

                    otherBehavior.stun(punchAttack.stunDuration * (otherBehavior.isBlocking ? .33f : 1f));
                    attackCooldown = punchAttack.hitDuration;

                } else {
                    attackCooldown = punchAttack.missDuration;
                }
            }

            Destroy(attackHitbox);
        }

        // Handle Movements
        if (isStanding()) {

            // Crouch
            if (inputHandler.IsCrouching()) {
                if (!crouched) {
                    boxCollider.size = new Vector2(1f, 0.5f);
                    boxCollider.offset = new Vector2(0f, -0.25f);
                    horizontalValue = 0;
                    crouched = true;
                    // Debug.Log("Crouching");
                }
                return;
            } else {
                if (crouched) {
                    boxCollider.size = new Vector2(1f, 1f);
                    boxCollider.offset = new Vector2(0f, 0f);
                    crouched = false;
                    // Debug.Log("Uncrouched");
                }
            }

            // Horizontal Movement
            horizontalValue = Mathf.SmoothDamp(horizontalValue, inputHandler.GetHorizontal(), ref horizontalVelocity, brawlManager.getSmoothTime());
            
            // Jump
            if (inputHandler.IsJumping()) {
                rigidBody.velocity = new Vector2(rigidBody.velocity.x, brawlManager.getJumpStrength());
            }
        }
    }

    private void FixedUpdate() {
        if (stunTime <= 0 && attackCooldown <= 0) {
            rigidBody.velocity = new Vector2(horizontalValue * brawlManager.getCharacterSpeed(), rigidBody.velocity.y);
        }
    }

    private bool isStanding() {
        GameObject arenaFloor = brawlManager.getArenaFloor();
        float floorValue = arenaFloor.transform.position.y + arenaFloor.transform.localScale.y / 2;
        float feetValue = gameObject.transform.position.y - gameObject.transform.localScale.y / 2;

        return feetValue < floorValue + COLLIDER_MARGIN;
    }

    private bool isAttackHit(BoxCollider2D attackHitbox, BoxCollider2D playerHitbox) {
        return attackHitbox.bounds.Intersects(playerHitbox.bounds);
    }

    public void stun(float seconds) {
        stunTime = seconds;
        attackCooldown = 0;
    }

    private BoxCollider2D setAttackBox(Attack attackInfo) {
        BoxCollider2D attackHitbox = attackBox.AddComponent<BoxCollider2D>();
        attackHitbox.offset = punchAttack.attackBoxOffset;
        attackHitbox.size = punchAttack.attackBoxOffset;

        return attackHitbox;
    }
}