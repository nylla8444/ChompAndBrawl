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
    public string fruitName;
    public Sprite fruitSprite;
    public int fruitScore;
    public int fruitGhostRequirement;
    public int fruitSpawnChance;
}
