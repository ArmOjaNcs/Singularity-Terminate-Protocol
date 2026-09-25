#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gameplay.Navigation
{
    [CustomEditor(typeof(NavigationGrid))]
    public sealed class NavigationGridEditor : Editor
    {
        private NavigationGrid _grid;

        private bool _editMode;
        private bool _blockMode = true;

        private int _brushSize = 1;

        private bool _showGrid = true;
        private bool _showBlockedCells = true;

        private void OnEnable()
        {
            _grid = (NavigationGrid)target;

            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField(
                "Navigation Editor",
                EditorStyles.boldLabel);

            _editMode = EditorGUILayout.Toggle(
                "Edit Mode",
                _editMode);

            EditorGUILayout.Space(5);

            _showGrid = EditorGUILayout.Toggle(
                "Show Grid",
                _showGrid);

            _showBlockedCells = EditorGUILayout.Toggle(
                "Show Blocked Cells",
                _showBlockedCells);

            if (!_editMode)
            {
                EditorGUILayout.HelpBox(
                    "Enable Edit Mode to paint Navigation Grid.",
                    MessageType.Info);

                return;
            }

            EditorGUILayout.Space(5);

            _blockMode = EditorGUILayout.Toggle(
                "Block Cells",
                _blockMode);

            _brushSize = EditorGUILayout.IntSlider(
                "Brush Size",
                _brushSize,
                1,
                20);

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Left Mouse Button: paint cells\n" +
                "Shift + Left Mouse Button: erase cells\n" +
                "Mouse Wheel: change brush size",
                MessageType.Info);

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Clear All"))
            {
                ClearGrid();

                GUIUtility.ExitGUI();
            }

            SceneView.RepaintAll();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_grid == null)
                return;

            if (!_editMode)
                return;

            if (_showGrid)
                DrawGrid();

            if (_showBlockedCells)
                DrawBlockedCells();

            HandleInput();

            sceneView.Repaint();
        }

        private void DrawGrid()
        {
            Handles.zTest =
                CompareFunction.LessEqual;

            Handles.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    0.18f);

            for (int x = 0; x <= _grid.CellsX; x++)
            {
                Vector3 start =
                    GetGridPoint(x, 0);

                Vector3 end =
                    GetGridPoint(x, _grid.CellsZ);

                Handles.DrawLine(
                    start,
                    end);
            }

            for (int z = 0; z <= _grid.CellsZ; z++)
            {
                Vector3 start =
                    GetGridPoint(0, z);

                Vector3 end =
                    GetGridPoint(_grid.CellsX, z);

                Handles.DrawLine(
                    start,
                    end);
            }
        }

        private void DrawBlockedCells()
        {
            Handles.zTest =
                CompareFunction.LessEqual;

            for (int x = 0; x < _grid.CellsX; x++)
            {
                for (int z = 0; z < _grid.CellsZ; z++)
                {
                    if (!_grid.IsBlocked(x, z))
                        continue;

                    DrawBlockedCell(x, z);
                }
            }
        }

        private void DrawBlockedCell(int x, int z)
        {
            float half =
                _grid.CellSize * 0.5f;

            Vector3 localCenter =
                new Vector3(
                    (x + 0.5f) * _grid.CellSize -
                        _grid.CellsX * _grid.CellSize * 0.5f,
                    0f,
                    (z + 0.5f) * _grid.CellSize -
                        _grid.CellsZ * _grid.CellSize * 0.5f);

            Vector3[] points =
            {
                _grid.transform.TransformPoint(
                    localCenter +
                    new Vector3(-half, 0f, -half)),

                _grid.transform.TransformPoint(
                    localCenter +
                    new Vector3(-half, 0f, half)),

                _grid.transform.TransformPoint(
                    localCenter +
                    new Vector3(half, 0f, half)),

                _grid.transform.TransformPoint(
                    localCenter +
                    new Vector3(half, 0f, -half))
    };

            Color fillColor =
                new Color(
                    1f,
                    0.15f,
                    0.15f,
                    1f);

            Color outlineColor =
                new Color(
                    1f,
                    0.15f,
                    0.15f,
                    0.8f);

            Handles.DrawSolidRectangleWithOutline(
                points,
                fillColor,
                outlineColor);
        }

        private void HandleInput()
        {
            Event currentEvent =
                Event.current;

            if (currentEvent.type ==
                EventType.ScrollWheel)
            {
                _brushSize +=
                    currentEvent.delta.y > 0
                        ? -1
                        : 1;

                _brushSize =
                    Mathf.Clamp(
                        _brushSize,
                        1,
                        20);

                currentEvent.Use();

                return;
            }

            if (currentEvent.type !=
                    EventType.MouseDown &&
                currentEvent.type !=
                    EventType.MouseDrag)
            {
                return;
            }

            if (currentEvent.button != 0)
                return;

            Ray ray =
                HandleUtility.GUIPointToWorldRay(
                    currentEvent.mousePosition);

            Plane plane =
                new Plane(
                    _grid.transform.up,
                    _grid.transform.position);

            if (!plane.Raycast(
                    ray,
                    out float distance))
            {
                return;
            }

            Vector3 worldPosition =
                ray.GetPoint(distance);

            if (!_grid.TryGetCell(
                    worldPosition,
                    out int cellX,
                    out int cellZ))
            {
                return;
            }

            bool blocked =
                _blockMode;

            if (currentEvent.shift)
                blocked = false;

            Paint(
                cellX,
                cellZ,
                blocked);

            currentEvent.Use();

            EditorUtility.SetDirty(_grid);
        }

        private void Paint(
            int centerX,
            int centerZ,
            bool blocked)
        {
            Undo.RecordObject(
                _grid,
                blocked
                    ? "Block Navigation Cells"
                    : "Unblock Navigation Cells");

            int radius =
                _brushSize / 2;

            for (int x = -radius;
                 x <= radius;
                 x++)
            {
                for (int z = -radius;
                     z <= radius;
                     z++)
                {
                    _grid.SetBlocked(
                        centerX + x,
                        centerZ + z,
                        blocked);
                }
            }
        }

        private void ClearGrid()
        {
            Undo.RecordObject(
                _grid,
                "Clear Navigation Grid");

            for (int x = 0;
                 x < _grid.CellsX;
                 x++)
            {
                for (int z = 0;
                     z < _grid.CellsZ;
                     z++)
                {
                    _grid.SetBlocked(
                        x,
                        z,
                        false);
                }
            }

            EditorUtility.SetDirty(_grid);
            SceneView.RepaintAll();
        }

        private Vector3 GetGridPoint(int x, int z)
        {
            Vector3 localPosition =
                new Vector3(
                    x * _grid.CellSize -
                        _grid.CellsX * _grid.CellSize * 0.5f,
                    0f,
                    z * _grid.CellSize -
                        _grid.CellsZ * _grid.CellSize * 0.5f);

            return _grid.transform.TransformPoint(
                localPosition);
        }
    }
}

#endif