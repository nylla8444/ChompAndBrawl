using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewAttackInfo", menuName = "BrawlMode/AttackInfo")]
public class AttackInfo : ScriptableObject {
    public AllAttacks specificAttack;
    public int damage;
    public float stunDuration;
    public float chargeDuration;
    public float hitDuration;
    public float missDuration;
    public float attackCooldown;
    public Vector2 rootVelocity;
    public Vector2 otherVelocity;
    public Vector2 attackBoxOffset;
    public Vector2 attackBoxSize;
    public AttackInfo nextAttack;
    public AttackCategory attackCategory;
    public bool shakeCam;
    public float camShakeStrength;
    public float camShakeDuration;
    public bool isProjectile;
    public Vector2 projectileVelocity;
    public Vector3 projectileSize;
    public bool projectileSpawnOnOwner; // false = spawn above opponent
    public float lifetime;
    public float DOTDuration;
    public AttackInfo DOTAttack;
    public Sprite projectileSprite;
}
