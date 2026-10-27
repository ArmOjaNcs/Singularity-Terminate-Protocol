#if UNITY_EDITOR
using Configs;
using ECS.ProjectileComponents;
using UnityEditor;
using UnityEngine;

namespace Gameplay.View
{
    [CustomEditor(typeof(ColliderPreview))]
    public sealed class ColliderPreviewEditor : Editor
    {
        private ColliderPreview _preview;

        private void OnEnable()
        {
            _preview = (ColliderPreview)target;
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ShowHitBox"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ShowNavigationBox"));

            CreatureConfig config = _preview.Config;
            if (config != null)
            {
                DrawCreatureConfig(config);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Assign a CreatureConfig to preview and edit its hitbox and navigation box.",
                    MessageType.Info);
            }

            if (serializedObject.ApplyModifiedProperties())
                EditorUtility.SetDirty(_preview);

            if (GUI.changed)
                SceneView.RepaintAll();
        }

        private static void DrawCreatureConfig(CreatureConfig config)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Creature Colliders", EditorStyles.boldLabel);

            SerializedObject configObject = new SerializedObject(config);
            configObject.Update();

            DrawColliderProperty(configObject.FindProperty("HitBox"), "Hit Box");
            DrawColliderProperty(configObject.FindProperty("NavigationBox"), "Navigation Box");

            if (configObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.BeginHorizontal();
            if (config.HitBox == null && GUILayout.Button("Create Hit Box"))
                CreateColliderConfig(config, true);
            if (config.NavigationBox == null && GUILayout.Button("Create Navigation Box"))
                CreateColliderConfig(config, false);
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawColliderProperty(SerializedProperty collider, string label)
        {
            if (collider == null)
                return;

            collider.isExpanded = EditorGUILayout.Foldout(
                collider.isExpanded,
                label,
                true,
                EditorStyles.foldoutHeader);
            if (!collider.isExpanded)
                return;

            EditorGUI.indentLevel++;
            SerializedProperty type = collider.FindPropertyRelative("Type");
            SerializedProperty radius = collider.FindPropertyRelative("Radius");
            SerializedProperty length = collider.FindPropertyRelative("Length");
            SerializedProperty size = collider.FindPropertyRelative("Size");

            EditorGUILayout.PropertyField(type);
            switch ((ColliderType)type.enumValueIndex)
            {
                case ColliderType.Circle:
                    EditorGUILayout.PropertyField(radius);
                    break;

                case ColliderType.Box:
                    EditorGUILayout.PropertyField(size, new GUIContent("Size"));
                    break;

                case ColliderType.Capsule:
                    EditorGUILayout.PropertyField(radius);
                    EditorGUILayout.PropertyField(length);
                    EditorGUILayout.PropertyField(
                        collider.FindPropertyRelative("CapsuleOrientation"));
                    break;

                case ColliderType.Sector:
                    EditorGUILayout.PropertyField(radius);
                    Vector2 sectorSize = size.vector2Value;
                    sectorSize.x = EditorGUILayout.Slider(
                        "Sector Angle",
                        sectorSize.x,
                        0f,
                        360f);
                    size.vector2Value = sectorSize;
                    break;
            }

            EditorGUILayout.PropertyField(
                collider.FindPropertyRelative("Center"),
                new GUIContent("Center Offset"));
            EditorGUI.indentLevel--;
        }

        private static void CreateColliderConfig(CreatureConfig config, bool hitBox)
        {
            Undo.RecordObject(config, hitBox ? "Create Hit Box" : "Create Navigation Box");
            ColliderConfig collider = new ColliderConfig
            {
                Type = ECS.ProjectileComponents.ColliderType.Circle,
                Radius = 0.5f,
                Length = 1f,
                Size = Vector2.one
            };

            if (hitBox)
                config.HitBox = collider;
            else
                config.NavigationBox = collider;

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_preview == null || _preview.Config == null)
                return;

            if (_preview.ShowHitBox)
                EditCollider(_preview.Config.HitBox, "Edit Hit Box");
            if (_preview.ShowNavigationBox)
                EditCollider(_preview.Config.NavigationBox, "Edit Navigation Box");
        }

        private void EditCollider(ColliderConfig collider, string undoName)
        {
            if (collider == null)
                return;

            Transform previewTransform = _preview.transform;
            Quaternion rotation = previewTransform.rotation;
            Vector3 worldCenter = previewTransform.TransformPoint(collider.Center);

            Handles.color = collider == _preview.Config.HitBox
                ? Color.cyan
                : new Color(0.2f, 1f, 0.3f, 1f);

            EditorGUI.BeginChangeCheck();
            Vector3 movedCenter = Handles.PositionHandle(worldCenter, rotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_preview.Config, undoName);
                Vector3 localCenter = previewTransform.InverseTransformPoint(movedCenter);
                collider.Center = new Vector2(localCenter.x, localCenter.y);
                EditorUtility.SetDirty(_preview.Config);
                AssetDatabase.SaveAssets();
            }

            if (collider.Type == ColliderType.Circle ||
                collider.Type == ColliderType.Sector)
            {
                EditorGUI.BeginChangeCheck();
                float radius = Handles.RadiusHandle(rotation, worldCenter, collider.Radius);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_preview.Config, undoName);
                    collider.Radius = Mathf.Max(0f, radius);
                    EditorUtility.SetDirty(_preview.Config);
                    AssetDatabase.SaveAssets();
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
                    Undo.RecordObject(_preview.Config, undoName);
                    collider.Size = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
                    EditorUtility.SetDirty(_preview.Config);
                    AssetDatabase.SaveAssets();
                }
            }

            if (GUI.changed)
                SceneView.RepaintAll();
        }

    }
}
#endif
