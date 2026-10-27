#if UNITY_EDITOR
using Configs;
using ECS.ProjectileComponents;
using ProjectileConfigs;
using UnityEditor;
using UnityEngine;

namespace Gameplay.View
{
    [CustomEditor(typeof(ProjectilePreview))]
    public sealed class ProjectilePreviewEditor : Editor
    {
        private ProjectilePreview _preview;

        private void OnEnable()
        {
            _preview = (ProjectilePreview)target;
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ShowCollider"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ShowTrajectory"));
            if (serializedObject.ApplyModifiedProperties())
                EditorUtility.SetDirty(_preview);

            ProjectileConfig config = _preview.Config;
            if (config == null)
            {
                EditorGUILayout.HelpBox("Assign a ProjectileConfig to preview and edit it.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Projectile Config", EditorStyles.boldLabel);
            SerializedObject configObject = new SerializedObject(config);
            configObject.Update();
            DrawPropertiesExcluding(configObject, "m_Script");
            if (configObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            if (GUI.changed)
                SceneView.RepaintAll();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_preview == null || _preview.Config == null)
                return;

            ProjectileConfig config = _preview.Config;
            ColliderConfig collider = config.Collider;
            if (_preview.ShowCollider && collider != null)
                EditCollider(config, collider);

            if (_preview.ShowTrajectory && config is ArcProjectileConfig arc)
                EditTrajectoryRadius(arc);
        }

        private void EditCollider(ProjectileConfig config, ColliderConfig collider)
        {
            Vector2 extraOffset = config is ArcProjectileConfig arc
                ? (Vector2)arc.ColliderOffset
                : Vector2.zero;
            Transform previewTransform = _preview.transform;
            Quaternion rotation = previewTransform.rotation;
            Vector3 worldCenter = previewTransform.TransformPoint(collider.Center + extraOffset);

            Handles.color = Color.cyan;
            EditorGUI.BeginChangeCheck();
            Vector3 movedCenter = Handles.PositionHandle(worldCenter, rotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "Edit Projectile Collider Center");
                Vector3 localCenter = previewTransform.InverseTransformPoint(movedCenter);
                Vector2 newCenter = new Vector2(localCenter.x, localCenter.y) - extraOffset;
                collider.Center = newCenter;
                SaveAsset(config);
            }

            if (collider.Type == ColliderType.Circle || collider.Type == ColliderType.Sector)
            {
                EditorGUI.BeginChangeCheck();
                float radius = Handles.RadiusHandle(rotation, worldCenter, collider.Radius);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(config, "Edit Projectile Collider Radius");
                    collider.Radius = Mathf.Max(0f, radius);
                    SaveAsset(config);
                }
            }
            else if (collider.Type == ColliderType.Box)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 size = Handles.ScaleHandle(
                    new Vector3(collider.Size.x, collider.Size.y, 0.01f),
                    worldCenter,
                    rotation,
                    HandleUtility.GetHandleSize(worldCenter));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(config, "Edit Projectile Collider Size");
                    collider.Size = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
                    SaveAsset(config);
                }
            }
        }

        private void EditTrajectoryRadius(ArcProjectileConfig config)
        {
            Handles.color = Color.magenta;
            EditorGUI.BeginChangeCheck();
            float radius = Handles.RadiusHandle(
                _preview.transform.rotation,
                _preview.transform.position,
                config.TrajectoryRadius);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "Edit Projectile Trajectory Radius");
                config.TrajectoryRadius = Mathf.Max(0f, radius);
                SaveAsset(config);
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
