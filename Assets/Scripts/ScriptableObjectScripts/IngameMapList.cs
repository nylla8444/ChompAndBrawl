using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "IngameMapList", menuName = "Objects/ObjectList/IngameMapList")]
public class IngameMapList : ScriptableObject
{
    public List<IngameMap> ingameMaps;
}

[Serializable]
public class IngameMap
{
    public string mapId;
    public string translatedName;
    public string description;
    public Sprite mapImage;
}