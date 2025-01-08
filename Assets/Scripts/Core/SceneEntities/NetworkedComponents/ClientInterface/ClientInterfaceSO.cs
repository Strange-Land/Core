using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ClientInterfaceSO : ScriptableObject
{
    [Tooltip("Unique human readable name that will be displayed in the UI and stored in json")]
    public string ID;
    public GameObject Prefab;
}


#if UNITY_EDITOR
[CustomEditor(typeof(ClientInterfaceSO))]
public class Editor_ClientInterfaceSO : Editor
{
    private SerializedProperty interfaceNameProp;
    private SerializedProperty interfacePrefabProp;

    private void OnEnable()
    {
        interfaceNameProp = serializedObject.FindProperty("ID");
        interfacePrefabProp = serializedObject.FindProperty("Prefab");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(interfaceNameProp);

        EditorGUI.BeginChangeCheck();
        var newPrefab = EditorGUILayout.ObjectField(
            "Interface Prefab",
            interfacePrefabProp.objectReferenceValue,
            typeof(GameObject),
            false
        );

        if (EditorGUI.EndChangeCheck())
        {
            // check class attached to the prefab
            if (newPrefab != null)
            {
                var go = newPrefab as GameObject;
                if (!IsValidInterface(go))
                {
                    Debug.LogWarning("The assigned GameObject does not have a component " +
                                     "that inherits from 'ClientInterface'!");
                    newPrefab = null;
                }
            }

            interfacePrefabProp.objectReferenceValue = newPrefab;
        }

        serializedObject.ApplyModifiedProperties();
    }
    
    private bool IsValidInterface(GameObject go)
    {
        if (go == null) return false;

        var components = go.GetComponents<MonoBehaviour>();
        foreach (var component in components)
        {
            if (component != null && component is ClientDisplay)
            {
                return true;
            }
        }

        return false;
    }
}
#endif
