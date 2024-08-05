using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialList", menuName = "Objects/ObjectList/TutorialList")]
public class TutorialList : ScriptableObject
{
    public List<Tutorials> tutorials;
}

[Serializable]
public class Tutorials
{
    public string tutorialId;
    public List<Sprite> tutorialSprites;
}