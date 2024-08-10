using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ProjectileBehavior : MonoBehaviour {
    
    private FighterBehavior owner;
    private AttackInfo attack;
    private int attackDirection;
    private float lifetime;
    private bool isTornado = false;
    private SpriteRenderer spriteRenderer;

    private HashSet<FighterBehavior.State> blockStates = new() {
        FighterBehavior.State.blocking,
        FighterBehavior.State.block_walking,
        FighterBehavior.State.block_stunned
    };

    public void Spawn(FighterBehavior _owner, AttackInfo _attackInfo, int _attackDirection) {
        owner = _owner;
        attack = _attackInfo;
        attackDirection = _attackDirection;

        lifetime = attack.lifetime;

        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = _attackInfo.projectileSprite;
        spriteRenderer.flipX = _attackDirection == -1;

        Rigidbody2D projectilePhysics = gameObject.GetComponent<Rigidbody2D>();
        projectilePhysics.velocity = new Vector2(attack.projectileVelocity.x * attackDirection, attack.projectileVelocity.y);
        gameObject.transform.localScale = attack.projectileSize;

        if (_attackInfo.name == "Sandstorm") {
            isTornado = true;
            gameObject.transform.position = new Vector3(gameObject.transform.position.x, -0.7f, gameObject.transform.position.z);
            gameObject.transform.localScale = new Vector3(21, 21, 1);
            BoxCollider2D boxCollider2D = gameObject.GetComponent<BoxCollider2D>();
            boxCollider2D.offset = new Vector2(0, -0.05f);
            boxCollider2D.size = new Vector2(0.15f, 0.65f);
        }
        this.enabled = true;
    }
    
    private void Update() {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0) { Destroy(gameObject); }
    }

    private void FixedUpdate() {
        if (!isTornado) { return; }
        spriteRenderer.flipX = !spriteRenderer.flipX;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        FighterBehavior opponent = other.gameObject.GetComponent<FighterBehavior>();
        if (!opponent || owner == opponent) { return; }   
        if (attack.DOTAttack) {
            opponent.DOTAttack = attack.DOTAttack;
            opponent.DOTDuration = attack.DOTDuration;
        }
        opponent.Hit(attack, blockStates.Contains(opponent.GetState()), attackDirection);
        Destroy(gameObject);
    }

    private void OnDestroy() {
        // Destroy Animation
    }
}
