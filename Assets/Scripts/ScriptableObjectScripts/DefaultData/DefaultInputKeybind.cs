using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DefaultInputKeybind", menuName = "Objects/Default/DefaultInputKeybind")]
public class DefaultInputKeybind : ScriptableObject
{
    public List<InputKeybindCategory> inputKeybindCategories;
}
