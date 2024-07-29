using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioList", menuName = "Objects/ObjectList/AudioList")]
public class AudioList : ScriptableObject
{
    public List<AudioCategory> musicCategories;
    public List<AudioCategory> sfxCategories;
}

[Serializable]
public class AudioCategory
{
    public string name;
    public List<AudioClipWithId> audioClips;
}

[Serializable]
public class AudioClipWithId
{
    public string id;
    public AudioClip audioClip;
}