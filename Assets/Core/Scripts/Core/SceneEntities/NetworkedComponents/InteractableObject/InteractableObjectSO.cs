using UnityEditor;
using UnityEngine;

namespace Core.SceneEntities
{
    public class InteractableObjectSO : ScriptableObject
    {
        [Tooltip("Unique human readable name that will be displayed in the UI and stored in json")]
        public string ID;
        public EAuthoritativeMode AuthoritativeMode;
        public GameObject Prefab;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(InteractableObjectSO))]
    public class Editor_InteractableObjectSO : Editor
    {
        private SerializedProperty objectNameProp;
        private SerializedProperty objectPrefabProp;
        private SerializedProperty authoritativeModeProp;

        private void OnEnable()
        {
            objectNameProp = serializedObject.FindProperty("ID");
            objectPrefabProp = serializedObject.FindProperty("Prefab");
            authoritativeModeProp = serializedObject.FindProperty("AuthoritativeMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(objectNameProp);
            EditorGUILayout.PropertyField(authoritativeModeProp);

            EditorGUI.BeginChangeCheck();
            var newPrefab = EditorGUILayout.ObjectField(
                "Object Prefab",
                objectPrefabProp.objectReferenceValue,
                typeof(GameObject),
                false
            );

            if (EditorGUI.EndChangeCheck())
            {
                // check class attached to the prefab
                if (newPrefab != null)
                {
                    var go = newPrefab as GameObject;
                    if (!IsValidInteractableObject(go))
                    {
                        Debug.LogWarning("The assigned GameObject does not have a component " +
                                         "that inherits from 'InteractableObject'!");
                        newPrefab = null;
                    }
                }

                objectPrefabProp.objectReferenceValue = newPrefab;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private bool IsValidInteractableObject(GameObject go)
        {
            if (go == null) return false;

            var components = go.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component != null && component is InteractableObject)
                {
                    return true;
                }
            }

            return false;
        }
    }
#endif
}