using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileBehavior : MonoBehaviour {
    
    private FighterBehavior owner;
    private AttackInfo attack;
    private int attackDirection;
    private float lifetime;

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

        Rigidbody2D projectilePhysics = gameObject.GetComponent<Rigidbody2D>();
        projectilePhysics.velocity = new Vector2(attack.projectileVelocity.x * attackDirection, attack.projectileVelocity.y);
        gameObject.transform.localScale = attack.projectileSize;

        this.enabled = true;
    }
    
    private void Update() {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0) { Destroy(gameObject); }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        FighterBehavior opponent = other.gameObject.GetComponent<FighterBehavior>();
        if (!opponent || owner == opponent) { return; }

        opponent.DOTDuration = attack.DOTDuration;
        opponent.DOTAttack = attack.DOTAttack;
        opponent.Hit(attack, blockStates.Contains(opponent.GetState()), attackDirection);
        Destroy(gameObject);
    }

    private void OnDestroy() {
        // Destroy Animation
    }
}
