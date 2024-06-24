using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif


[CreateAssetMenu(fileName = "SceneDictionary", menuName = "Objects/DictionaryList/SceneDictionary")]
public class SceneDictionary : ScriptableObject
{
    public Dictionary<string, string> sceneDictionary;
    [SerializeField] private List<string> nameIds;
    [SerializeField] private List<SceneField> sceneFields;

    private void OnEnable()
    {
        sceneDictionary = new Dictionary<string, string>();

        for (int i = 0; i < sceneFields.Count; i++)
        {
            if (i < nameIds.Count)
            {
                sceneDictionary[nameIds[i]] = sceneFields[i].SceneName;
            }
        }
    }

    public string GetSceneName(string nameId)
    {
        sceneDictionary.TryGetValue(nameId, out string sceneName);
        return sceneName;
    }
}

[System.Serializable]
public class SceneField
{
#if UNITY_EDITOR
    [SerializeField] private Object sceneAsset;
    public Object SceneAsset => sceneAsset;
#endif

    [SerializeField] private string sceneName;
    public string SceneName => sceneName;

#if UNITY_EDITOR
    public void OnValidate()
    {
        if (sceneAsset != null)
        {
            string path = AssetDatabase.GetAssetPath(sceneAsset);
            sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
        }
    }
#endif
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SceneField))]
public class SceneFieldPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var sceneAssetProp = property.FindPropertyRelative("sceneAsset");
        var sceneNameProp = property.FindPropertyRelative("sceneName");

        EditorGUI.PropertyField(position, sceneAssetProp, label);

        if (sceneAssetProp.objectReferenceValue != null)
        {
            string path = AssetDatabase.GetAssetPath(sceneAssetProp.objectReferenceValue);
            sceneNameProp.stringValue = System.IO.Path.GetFileNameWithoutExtension(path);
        }

        EditorGUI.EndProperty();
    }
}
#endif