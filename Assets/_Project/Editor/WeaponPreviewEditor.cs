#if UNITY_EDITOR
using Configs;
using UnityEditor;
using UnityEngine;
using WeaponConfigs;

namespace Gameplay.View
{
    [CustomEditor(typeof(WeaponPreview))]
    public sealed class WeaponPreviewEditor : Editor
    {
        private WeaponPreview _preview;

        private void OnEnable()
        {
            _preview = (WeaponPreview)target;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Config"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("PreviewDirection"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ShowTargetSearchRadius"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ShowDistanceFromSource"));
            if (serializedObject.ApplyModifiedProperties())
                EditorUtility.SetDirty(_preview);

            WeaponConfig config = _preview.Config;
            if (config == null)
            {
                EditorGUILayout.HelpBox("Assign a WeaponConfig to preview its attack values.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Weapon Config", EditorStyles.boldLabel);
            SerializedObject weaponObject = new SerializedObject(config);
            weaponObject.Update();
            EditorGUILayout.PropertyField(weaponObject.FindProperty("Type"));
            EditorGUILayout.PropertyField(weaponObject.FindProperty("AttackConfig"));
            EditorGUILayout.PropertyField(weaponObject.FindProperty("Effects"), true);
            if (weaponObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            if (config.AttackConfig == null)
            {
                EditorGUILayout.HelpBox("Assign an AttackConfig to edit previewed values.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Previewed Attack Values", EditorStyles.boldLabel);
            SerializedObject attackObject = new SerializedObject(config.AttackConfig);
            attackObject.Update();
            EditorGUILayout.PropertyField(
                attackObject.FindProperty("DistanceFromSource"),
                new GUIContent("Distance From Source"));
            EditorGUILayout.PropertyField(
                attackObject.FindProperty("TargetSearchRadius"),
                new GUIContent("Target Search Radius"));
            if (attackObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(config.AttackConfig);
                AssetDatabase.SaveAssets();
            }

            if (GUI.changed)
                SceneView.RepaintAll();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_preview == null || _preview.Config == null || _preview.Config.AttackConfig == null)
                return;

            AttackConfig attack = _preview.Config.AttackConfig;
            Vector3 center = _preview.transform.position;

            if (_preview.ShowTargetSearchRadius)
            {
                Handles.color = Color.yellow;
                EditorGUI.BeginChangeCheck();
                float radius = Handles.RadiusHandle(
                    _preview.transform.rotation,
                    center,
                    attack.TargetSearchRadius);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(attack, "Edit Target Search Radius");
                    attack.TargetSearchRadius = Mathf.Max(0f, radius);
                    SaveAsset(attack);
                }
            }

            if (_preview.ShowDistanceFromSource)
            {
                Vector3 direction = _preview.GetWorldDirection();
                Vector3 endpoint = center + direction * attack.DistanceFromSource;
                Handles.color = Color.magenta;
                EditorGUI.BeginChangeCheck();
                Vector3 movedEndpoint = Handles.Slider(endpoint, direction);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(attack, "Edit Distance From Source");
                    attack.DistanceFromSource = Mathf.Max(
                        0f,
                        Vector3.Dot(movedEndpoint - center, direction));
                    SaveAsset(attack);
                }
            }
        }

        private static void SaveAsset(Object asset)
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
