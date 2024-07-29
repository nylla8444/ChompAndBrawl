using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FruitList", menuName = "Objects/ObjectList/FruitList")]
public class FruitList : ScriptableObject
{
    public List<Fruit> fruits;
}

[Serializable]
public class Fruit
{
    public string name;
    public Sprite asItemSprite;
    public int points;
    public Sprite pointsSprite;
    public int ghostRequirement;
    public int spawnChance;
}
