using UnityEngine;

[CreateAssetMenu(fileName = "NewFighterInfo", menuName = "BrawlMode/FighterInfo")]
public class FighterInfo : ScriptableObject {
    public float DamageMultipler;
    public AttackInfo UniqueAttackInfo;
    
    // more sprite specifics here for attacks
}
