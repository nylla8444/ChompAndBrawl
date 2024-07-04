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
    public string effectItemName;
    public Sprite effectItemSprite;
    public int effectItemSpawnChance;
}