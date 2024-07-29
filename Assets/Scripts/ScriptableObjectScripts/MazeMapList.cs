using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MazeMapList", menuName = "Objects/ObjectList/MazeMapList")]
public class MazeMapList : ScriptableObject
{
    public List<MazeMap> mazeMaps;
}

[Serializable]
public class MazeMap
{
    public string mapId;
    public string translatedName;
    public string description;
    public Sprite mapImage;
    public SceneField mapScene;
}