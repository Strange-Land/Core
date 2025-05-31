using Core.SceneEntities;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;
using Core.Utilities;

namespace Core.Editor
{
    public class StrangeLandTransformToolWindow : EditorWindow
    {
        private NetworkTransformType networkTransformType = NetworkTransformType.None;
        private bool syncWithStrangeLandTransform = false;

        [MenuItem("Tools/StrangeLandTransformTool")]
        public static void OpenWindow()
        {
            GetWindow<StrangeLandTransformToolWindow>("StrangeLand Transform Tool");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);

            networkTransformType = (NetworkTransformType)EditorGUILayout.EnumPopup("Network Transform Type", networkTransformType);

            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 200f;
            syncWithStrangeLandTransform = EditorGUILayout.Toggle("Sync With StrangeLandTransform", syncWithStrangeLandTransform);
            EditorGUIUtility.labelWidth = oldLabelWidth;

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Attach!"))
            {
                AttachNetworkTransforms();
            }
        }


        private void AttachNetworkTransforms()
        {
            if (networkTransformType == NetworkTransformType.None)
            {
                return;
            }

            Object[] selectedObjects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);

            bool modifiedAnyPrefab = false;

            foreach (Object obj in selectedObjects)
            {
                if (PrefabUtility.GetPrefabAssetType(obj) == PrefabAssetType.NotAPrefab)
                {
                    continue;
                }

                string path = AssetDatabase.GetAssetPath(obj);
                GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefabRoot == null)
                    continue;

                bool hasChanges = false;

                /* From Mario:
            I can't find a way to do this whole thing without instantiating the prefab!!!
            Did quite some research this seems to be the most efficient way
            Very open to suggestions 
            */

                GameObject tempInstance = PrefabUtility.InstantiatePrefab(prefabRoot) as GameObject;
                if (!tempInstance)
                    continue;

                StrangeLandTransform[] strangeLandTransforms = tempInstance.GetComponentsInChildren<StrangeLandTransform>(true);

                foreach (var slt in strangeLandTransforms)
                {
                    NetworkTransform netTrans = slt.gameObject.GetComponent<NetworkTransform>();
                    ClientNetworkTransform clientNetTrans = slt.gameObject.GetComponent<ClientNetworkTransform>();

                    if (netTrans != null)
                    {
                        DestroyImmediate(netTrans);
                    }

                    if (clientNetTrans != null)
                    {
                        DestroyImmediate(clientNetTrans);
                    }


                    if (networkTransformType == NetworkTransformType.NetworkTransform)
                    {
                        if (netTrans == null)
                        {
                            netTrans = slt.gameObject.AddComponent<NetworkTransform>();
                            hasChanges = true;
                        }

                        if (syncWithStrangeLandTransform)
                        {
                            Debug.Log($"networktransform{netTrans.SyncPositionX}, {slt.LogPosition}");
                            netTrans.SyncPositionX = slt.LogPosition;
                            netTrans.SyncPositionY = slt.LogPosition;
                            netTrans.SyncPositionZ = slt.LogPosition;

                            netTrans.SyncRotAngleX = slt.LogRotation;
                            netTrans.SyncRotAngleY = slt.LogRotation;
                            netTrans.SyncRotAngleZ = slt.LogRotation;

                            netTrans.SyncScaleX = slt.LogScale;
                            netTrans.SyncScaleY = slt.LogScale;
                            netTrans.SyncScaleZ = slt.LogScale;
                            hasChanges = true;
                        }
                    }
                    else if (networkTransformType == NetworkTransformType.ClientNetworkTransform)
                    {
                        if (clientNetTrans == null)
                        {
                            clientNetTrans = slt.gameObject.AddComponent<ClientNetworkTransform>();
                            hasChanges = true;
                        }

                        if (syncWithStrangeLandTransform)
                        {
                            clientNetTrans.SyncPositionX = slt.LogPosition;
                            clientNetTrans.SyncPositionY = slt.LogPosition;
                            clientNetTrans.SyncPositionZ = slt.LogPosition;

                            clientNetTrans.SyncRotAngleX = slt.LogRotation;
                            clientNetTrans.SyncRotAngleY = slt.LogRotation;
                            clientNetTrans.SyncRotAngleZ = slt.LogRotation;

                            clientNetTrans.SyncScaleX = slt.LogScale;
                            clientNetTrans.SyncScaleY = slt.LogScale;
                            clientNetTrans.SyncScaleZ = slt.LogScale;

                            clientNetTrans.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
                            hasChanges = true;
                        }
                    }
                }

                if (hasChanges)
                {
                    PrefabUtility.SaveAsPrefabAssetAndConnect(tempInstance, path, InteractionMode.AutomatedAction);

                    modifiedAnyPrefab = true;
                }

                DestroyImmediate(tempInstance);
            }

            if (modifiedAnyPrefab)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Successfully attached network transforms to selected prefabs!");
            }
            else
            {
                Debug.Log("No changes made to prefabs.");
            }
        }
    }
}
