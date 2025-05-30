using UnityEditor;
using UnityEngine;
using Core.Networking;

namespace Core.SceneEntities
{
    public class ClientDisplaySO : ScriptableObject
    {
        [Tooltip("Unique human readable name that will be displayed in the UI and stored in json")]
        public string ID;
        public EAuthoritativeMode AuthoritativeMode;
        public GameObject Prefab;
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(ClientDisplaySO))]
    public class Editor_ClientDisplaySO : Editor
    {
        private SerializedProperty interfaceNameProp;
        private SerializedProperty interfacePrefabProp;
        private SerializedProperty authoritativeModeProp;

        private void OnEnable()
        {
            interfaceNameProp = serializedObject.FindProperty("ID");
            interfacePrefabProp = serializedObject.FindProperty("Prefab");
            authoritativeModeProp = serializedObject.FindProperty("AuthoritativeMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(interfaceNameProp);
            EditorGUILayout.PropertyField(authoritativeModeProp);

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
}