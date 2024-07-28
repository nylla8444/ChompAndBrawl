using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAttack", menuName = "BrawlMode/Attack")]
public class Attack : ScriptableObject
{
    public int damage;
    public float stunDuration;
    public float hitDuration;
    public float missDuration;
    public float rootVelocity;
    public float otherVelocity;
    public Vector2 attackBoxOffset;
    public Vector2 attackBoxSize;
    public bool shakeCamera;
}
