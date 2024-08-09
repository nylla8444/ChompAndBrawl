using UnityEngine;

[CreateAssetMenu(fileName = "NewFighterInfo", menuName = "BrawlMode/FighterInfo")]
public class FighterInfo : ScriptableObject {
    public float DamageMultipler;
    public AttackInfo UniqueAttackInfo;
    public Animation punchAnimation;
    public Animation basicAnimation;
    public Animation uniqueAnimation;
    
    // more sprite specifics here for attacks
}
