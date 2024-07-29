using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EffectItemList", menuName = "Objects/ObjectList/EffectItemList")]
public class EffectItemList : ScriptableObject
{
    public List<EffectItem> effectItems;
}

[Serializable]
public class EffectItem
{
    public string name;
    public string description;
    public Sprite asItemSprite;
    public Sprite startParticleSprite;
    public float useTime;
    public float cooldown;
    public int spawnChance;
    public InEffect inEffect;

    [Serializable]
    public class InEffect
    {
        public string id;
        public string description;
        public Sprite iconSprite;
    }
}