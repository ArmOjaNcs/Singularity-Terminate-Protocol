using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Cinemachine;
using Configs;
using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.CommonSystems;
using ECS.DamageSystems;
using ECS.EffectComponents;
using ECS.PlayerComponents;
using ECS.PlayerSystems;
using ECS.ProjectileComponents;
using ECS.ProjectileSystems;
using ECS.TrajectorySystems;
using ECS.WeaponComponents;
using ECS.WeaponSystems;
using ECS.ViewComponents;
using EffectConfigs;
using EffectComponents;
using Gameplay.Spatial;
using Gameplay.Creatures;
using Gameplay.View;
using PlayerConfigs;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;
using WeaponConfigs;

namespace Gameplay.WeaponTesting
{
    /// <summary>
    /// Isolated runtime harness for creating and tuning weapon entities.
    /// Attach this to the weapon test scene and provide the configs to expose.
    /// </summary>
    public sealed class WeaponTestBench : MonoBehaviour
    {
        [SerializeField] private WeaponConfig[] _weaponConfigs;
        [SerializeField] private PlayerConfig _playerConfig;
        [SerializeField] private WeaponTestDummyConfig _dummyConfig;
        [SerializeField] private ProjectileDatabase _projectileDatabase;
        [SerializeField] private Transform _projectileContainer;
        [SerializeField] private bool _showPanel = true;
        [SerializeField, Min(0.1f)] private float _measurementDuration = 10f;
        [SerializeField, Min(1f)] private float _dummyHealth = 1000f;
        [SerializeField, Range(0f, 100f)] private float _dummyDefence;
        [SerializeField, Min(1)] private int _dummyCount = 8;
        [SerializeField] private DummyFormation _dummyFormation = DummyFormation.Ring;
        [SerializeField] private Vector2 _formationCenter = new Vector2(1.3f, 0f);
        [SerializeField, Min(0f)] private float _ringRadius = 2.5f;
        [SerializeField, Min(0f)] private float _ringThickness = 1.5f;
        [SerializeField, Range(1, 12)] private int _ringLayers = 3;
        [SerializeField] private Vector2 _formationSize = new Vector2(5f, 4f);

        private readonly List<WeaponEntry> _weapons = new List<WeaponEntry>();
        private World _world;
        private Entity _owner;
        private int _selectedWeapon;
        private Vector2 _scroll;
        private BattleStatistics _statistics;
        private SystemsGroup _testSystems;
        private ProjectileFactory _projectileFactory;
        private GameObject _playerObject;
        private bool _measurementRunning;
        private float _measurementRemaining;
        private readonly List<WeaponTestDummy> _spawnedDummies = new List<WeaponTestDummy>();
        private readonly List<Entity> _entitiesToRemove = new List<Entity>();
        private readonly Dictionary<string, string> _floatInputBuffers =
            new Dictionary<string, string>();
        private string _activeFloatInput;

        private enum DummyFormation
        {
            Ring,
            Circle,
            Square
        }

        public BattleStatistics Statistics => _statistics;

        private Stash<WeaponComponent> _weaponStash;
        private Stash<AttackComponent> _attackStash;
        private Stash<WeaponEffectsComponent> _effectsStash;
        private Stash<AttackRequest> _requestStash;
        private Stash<AttackStatusComponent> _statusStash;
        private Stash<ProjectileCreationRequestComponent> _creationRequestStash;
        private Stash<ProjectileComponent> _projectileStash;
        private Stash<ProjectileViewComponent> _projectileViewStash;
        private Stash<CleanupRequest> _cleanupRequestStash;
        private Stash<BattleStatisticComponent> _battleStatisticStash;

        private sealed class WeaponEntry
        {
            public WeaponConfig Config;
            public Entity Entity;
            public AttackComponent InitialAttack;
            public List<IEffectRuntime> InitialEffects;
            public AttackTargetType[] EnabledTargetTypes;
            public bool Enabled = true;
        }

        private void Start()
        {
            _world = World.Default;
            _statistics = new BattleStatistics();
            _weaponStash = _world.GetStash<WeaponComponent>();
            _attackStash = _world.GetStash<AttackComponent>();
            _effectsStash = _world.GetStash<WeaponEffectsComponent>();
            _requestStash = _world.GetStash<AttackRequest>();
            _statusStash = _world.GetStash<AttackStatusComponent>();
            _creationRequestStash = _world.GetStash<ProjectileCreationRequestComponent>();
            _projectileStash = _world.GetStash<ProjectileComponent>();
            _projectileViewStash = _world.GetStash<ProjectileViewComponent>();
            _cleanupRequestStash = _world.GetStash<CleanupRequest>();
            _battleStatisticStash = _world.GetStash<BattleStatisticComponent>();
            _statistics.SetRecording(false);

            if (_projectileDatabase == null || _projectileContainer == null)
            {
                Debug.LogError("Weapon Test Bench needs a ProjectileDatabase and projectile container.", this);
                return;
            }

            SetupTestSystems();
            CreatePlayer();
            if (_owner == default)
                return;
            SpawnDummyGroup();

            WeaponFactory factory = new WeaponFactory(_world);
            foreach (WeaponConfig config in _weaponConfigs ?? Array.Empty<WeaponConfig>())
            {
                if (!factory.Create(config, _owner, out Entity weapon))
                    continue;

                _weapons.Add(new WeaponEntry
                {
                    Config = config,
                    Entity = weapon,
                    InitialAttack = _attackStash.Get(weapon),
                    InitialEffects = CloneEffects(_effectsStash.Get(weapon).Effects)
                });
            }
        }

        private void Update()
        {
            if (!_measurementRunning)
                return;

            _measurementRemaining -= Time.deltaTime;
            if (_measurementRemaining <= 0f)
                StopMeasurement();
        }

        private void SpawnDummyGroup()
        {
            if (_dummyConfig == null ||
                _dummyConfig.Prefab == null ||
                _dummyConfig.HitBox == null)
            {
                Debug.LogError(
                    "Assign the Dummy creature config with a prefab and hitbox before spawning mannequins.",
                    this);
                return;
            }

            if (_dummyConfig.CreatureType != CreatureType.Dummy)
            {
                Debug.LogError(
                    $"The assigned mannequin config must use CreatureType.Dummy, but is {_dummyConfig.CreatureType}.",
                    this);
                return;
            }

            for (int i = 0; i < _spawnedDummies.Count; i++)
            {
                if (_spawnedDummies[i] != null)
                    Destroy(_spawnedDummies[i].gameObject);
            }
            _spawnedDummies.Clear();

            int count = Mathf.Clamp(_dummyCount, 1, 256);
            for (int i = 0; i < count; i++)
            {
                Vector2 point = GetDummyPosition(i, count);
                Vector3 position = new Vector3(point.x, point.y, 0f);
                GameObject dummyObject = Instantiate(
                    _dummyConfig.Prefab,
                    position,
                    Quaternion.identity);
                dummyObject.name = $"Training Dummy {i + 1}";
                dummyObject.transform.position = position;

                WeaponTestDummy dummy = dummyObject.GetComponent<WeaponTestDummy>();
                if (dummy == null)
                    dummy = dummyObject.AddComponent<WeaponTestDummy>();
                dummy.Configure(_dummyConfig, _dummyHealth, _dummyDefence);
                _spawnedDummies.Add(dummy);
            }
        }

        private Vector2 GetDummyPosition(int index, int count)
        {
            if (_dummyFormation == DummyFormation.Ring)
            {
                int layerCount = Mathf.Clamp(_ringLayers, 1, count);
                int layerIndex = index % layerCount;
                int indexOnLayer = index / layerCount;
                int itemsOnLayer = (count - 1 - layerIndex) / layerCount + 1;

                float innerRadius = Mathf.Max(0f, _ringRadius);
                float outerRadius = innerRadius + Mathf.Max(0f, _ringThickness);
                float layerProgress = layerCount <= 1
                    ? 0f
                    : layerIndex / (layerCount - 1f);
                float radius = Mathf.Lerp(innerRadius, outerRadius, layerProgress);
                float stagger = layerIndex % 2 == 0 ? 0f : 0.5f;
                float angle = Mathf.PI * 2f * (indexOnLayer + stagger) / itemsOnLayer;

                return _formationCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            if (_dummyFormation == DummyFormation.Circle)
            {
                const float goldenAngle = 2.39996323f;
                float radius = _ringRadius * Mathf.Sqrt((index + 0.5f) / count);
                float angle = index * goldenAngle;
                return _formationCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            float width = Mathf.Max(0.5f, _formationSize.x);
            float height = Mathf.Max(0.5f, _formationSize.y);
            int columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(count * width / height)));
            int rows = Mathf.CeilToInt(count / (float)columns);
            int row = index / columns;
            int column = index % columns;
            int itemsInRow = Mathf.Min(columns, count - row * columns);
            float x = itemsInRow <= 1 ? 0f : -width * 0.5f + width * column / (itemsInRow - 1f);
            float y = rows <= 1 ? 0f : height * 0.5f - height * row / (rows - 1f);
            return _formationCenter + new Vector2(x, y);
        }

        private void CreatePlayer()
        {
            if (_playerConfig == null || _playerConfig.Prefab == null || _playerConfig.HitBox == null)
            {
                Debug.LogError("Assign a PlayerConfig with a prefab and hitbox to the Weapon Test Bench.", this);
                return;
            }

            _owner = _world.CreateEntity();
            _world.GetStash<PlayerTag>().Set(_owner, new PlayerTag());
            _world.GetStash<PlayerInputComponent>().Set(_owner, new PlayerInputComponent());
            _world.GetStash<PlayerStatsComponent>().Set(_owner, new PlayerStatsComponent
            {
                EvasionChance = _playerConfig.EvasionChance,
                GatheringRadius = _playerConfig.GatheringRadius
            });

            new CreatureFactory(_world, new WeaponFactory(_world))
                .CreateCommonComponents(_owner, _playerConfig, transform.position);

            _playerObject = Instantiate(_playerConfig.Prefab, transform.position, Quaternion.identity);
            _playerObject.name = "Weapon Test Player";
            AnimatedCreature animatedView = _playerObject.GetComponent<AnimatedCreature>();
            if (animatedView == null)
            {
                Debug.LogError("Player prefab must contain AnimatedCreature.", _playerObject);
                Destroy(_playerObject);
                _world.RemoveEntity(_owner);
                _owner = default;
                return;
            }

            animatedView.SetEntity(_owner);
            _world.GetStash<ViewComponent>().Set(_owner, new ViewComponent { View = animatedView });
            _world.GetStash<AnimatedCreatureComponent>().Set(_owner,
                new AnimatedCreatureComponent { View = animatedView });
            _world.GetStash<PlayerViewComponent>().Set(_owner,
                new PlayerViewComponent { View = animatedView });
            CinemachineVirtualCamera camera = FindObjectOfType<CinemachineVirtualCamera>();
            if (camera != null)
                camera.Follow = _playerObject.transform;
        }

        private void SetupTestSystems()
        {
            SpatialGrid grid = new SpatialGrid(0.5f);
            _projectileFactory = new ProjectileFactory(
                _world,
                _projectileDatabase,
                _projectileContainer,
                grid);

            _testSystems = _world.CreateSystemsGroup();

            Gameplay.Navigation.NavigationGrid navigationGrid =
                FindObjectOfType<Gameplay.Navigation.NavigationGrid>();
            if (navigationGrid != null)
            {
                _testSystems.AddSystem(new PlayerInputSystem());
                _testSystems.AddSystem(new PlayerMovementSystem(navigationGrid));
            }
            _testSystems.AddSystem(new WeaponPositionSystem());
            _testSystems.AddSystem(new ViewPositionSystem());
            _testSystems.AddSystem(new ViewSortingSystem());
            _testSystems.AddSystem(new MoveAnimationSystem());
            _testSystems.AddSystem(new SpatialGridSystem(grid));
            _testSystems.AddSystem(new TargetSelectionSystem(grid));
            _testSystems.AddSystem(new AttackCooldownSystem());
            _testSystems.AddSystem(new AttackSystem());
            _testSystems.AddSystem(new AttackExecutionSystem(_projectileFactory));

            _testSystems.AddSystem(new ProjectileCreationSystem(_projectileFactory));
            _testSystems.AddSystem(new StraightTrajectorySystem());
            _testSystems.AddSystem(new ElectricTrajectorySystem(grid));
            _testSystems.AddSystem(new ArcTrajectorySystem());
            _testSystems.AddSystem(new SwordThrustSystem());
            _testSystems.AddSystem(new GrenadeLauncherFlightSystem(_projectileFactory));
            _testSystems.AddSystem(new RocketLauncherFlightSystem());
            _testSystems.AddSystem(new TacticalRocketFlightSystem());
            _testSystems.AddSystem(new EmpWaveSystem(_projectileDatabase, _projectileContainer));
            _testSystems.AddSystem(new AreaExpansionSystem());
            _testSystems.AddSystem(new RipperTrajectorySystem());
            _testSystems.AddSystem(new ProjectileViewSystem());
            _testSystems.AddSystem(new ProjectileSortingSystem());
            _testSystems.AddSystem(new ProjectileCollisionCheckSystem(grid));
            _testSystems.AddSystem(new ProjectileLifeTimeSystem());

            _testSystems.AddSystem(new BurnStatusSystem());
            _testSystems.AddSystem(new BleedStatusSystem());
            _testSystems.AddSystem(new DamageOverTimeStatusSystem());
            _testSystems.AddSystem(new DamageSystem());
            _testSystems.AddSystem(new EffectSystem());
            _testSystems.AddSystem(new RicochetSystem(grid));
            _testSystems.AddSystem(new AttackSequenceSystem());
            _testSystems.AddSystem(new TraumaStatusSystem());
            _testSystems.AddSystem(new TwistStatusSystem());
            if (navigationGrid != null)
                _testSystems.AddSystem(new KnockbackSystem(navigationGrid));

            // Must run after DamageSystem and before the component events are consumed.
            _testSystems.AddSystem(new BattleStatisticsSystem(_statistics));
            // Cleanup must be last so no projectile system observes disposed entities this frame.
            _testSystems.AddSystem(new CleanupSystem());
            _world.AddSystemsGroup(0, _testSystems);
        }

        private static List<IEffectRuntime> CloneEffects(EffectCollection collection)
        {
            List<IEffectRuntime> copy = new List<IEffectRuntime>();
            if (collection == null)
                return copy;
            foreach (IEffectRuntime effect in collection.Effects)
                copy.Add(CloneEffect(effect));
            return copy;
        }

        private static IEffectRuntime CloneEffect(IEffectRuntime effect)
        {
            if (effect == null) return null;
            object clone = Activator.CreateInstance(effect.GetType());
            foreach (FieldInfo field in effect.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
                field.SetValue(clone, field.GetValue(effect));
            if (effect is ShockEffectRuntime source && clone is ShockEffectRuntime target && source.TwistOnDamage != null)
                target.TwistOnDamage = (TwistEffectRuntime)CloneEffect(source.TwistOnDamage);
            return (IEffectRuntime)clone;
        }

        private void OnGUI()
        {
            float uiScale = Mathf.Clamp(Screen.height / 900f, 1.15f, 1.4f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * uiScale) * previousMatrix;

            if (!_showPanel)
            {
                if (GUI.Button(new Rect(10f / uiScale, 10f / uiScale,
                        190f / uiScale, 42f / uiScale), "Show weapon bench"))
                    _showPanel = true;
                GUI.matrix = previousMatrix;
                return;
            }

            float panelWidth = Mathf.Min(650f, (Screen.width - 20f) / uiScale);
            float panelHeight = (Screen.height - 20f) / uiScale;
            GUILayout.BeginArea(new Rect(
                10f / uiScale,
                10f / uiScale,
                panelWidth,
                panelHeight), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(
                _scroll,
                false,
                true,
                GUILayout.Width(panelWidth),
                GUILayout.Height(panelHeight));
            GUILayout.BeginHorizontal();
            GUILayout.Label("WEAPON TEST BENCH", GUI.skin.box);
            if (GUILayout.Button("Hide", GUILayout.Width(70))) _showPanel = false;
            GUILayout.EndHorizontal();
            if (_weapons.Count == 0)
                GUILayout.Label("No weapons in _weaponConfigs. Assign WeaponConfig assets to the test bench.");
            for (int i = 0; i < _weapons.Count; i++)
            {
                WeaponEntry entry = _weapons[i];
                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(_selectedWeapon == i, entry.Config.Type.ToString(), GUI.skin.button))
                    _selectedWeapon = i;
                bool entryEnabled = GUILayout.Toggle(entry.Enabled, "On", GUILayout.Width(52));
                if (entryEnabled != entry.Enabled) SetEnabled(entry, entryEnabled);
                GUILayout.EndHorizontal();
            }

            WeaponEntry selected = _weapons.Count > 0 ? _weapons[Mathf.Clamp(_selectedWeapon, 0, _weapons.Count - 1)] : null;
            if (selected != null && GUILayout.Button("Reset selected weapon to config")) Reset(selected);

            GUILayout.Label("Dummy group", GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Shape", GUILayout.Width(55));
            _dummyFormation = (DummyFormation)GUILayout.SelectionGrid((int)_dummyFormation,
                new[] { "Ring", "Circle", "Square" }, 3);
            GUILayout.Label("Count", GUILayout.Width(45));
            int.TryParse(GUILayout.TextField(_dummyCount.ToString(), GUILayout.Width(48)), out _dummyCount);
            _dummyCount = Mathf.Clamp(_dummyCount, 1, 256);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Center", GUILayout.Width(55));
            _formationCenter.x = DrawFloatField(
                "Dummy.Formation.CenterX", _formationCenter.x, 60);
            _formationCenter.y = DrawFloatField(
                "Dummy.Formation.CenterY", _formationCenter.y, 60);
            if (_dummyFormation == DummyFormation.Square)
            {
                GUILayout.Label("W × H", GUILayout.Width(45));
                _formationSize.x = DrawFloatField(
                    "Dummy.Formation.Width", _formationSize.x, 55);
                _formationSize.y = DrawFloatField(
                    "Dummy.Formation.Height", _formationSize.y, 55);
            }
            else
            {
                GUILayout.Label(
                    _dummyFormation == DummyFormation.Ring ? "Inner radius" : "Radius",
                    GUILayout.Width(_dummyFormation == DummyFormation.Ring ? 80 : 48));
                _ringRadius = Mathf.Max(0f, DrawFloatField(
                    "Dummy.Formation.Radius", _ringRadius, 60));
                if (_dummyFormation == DummyFormation.Ring)
                {
                    GUILayout.Label("Thickness", GUILayout.Width(68));
                    _ringThickness = Mathf.Max(0f, DrawFloatField(
                        "Dummy.Formation.Thickness", _ringThickness, 55));
                    GUILayout.Label("Layers", GUILayout.Width(48));
                    int.TryParse(GUILayout.TextField(_ringLayers.ToString(), GUILayout.Width(45)), out _ringLayers);
                    _ringLayers = Mathf.Clamp(_ringLayers, 1, 12);
                }
            }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Create group (replace previous)")) SpawnDummyGroup();

            GUILayout.Label("Measurement targets", GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Dummy Max Health", GUILayout.Width(160));
            _dummyHealth = DrawFloatField(
                "Dummy.Health", _dummyHealth, 100);
            GUILayout.Label("Defence %", GUILayout.Width(80));
            _dummyDefence = DrawFloatField(
                "Dummy.Defence", _dummyDefence, 80);
            GUILayout.EndHorizontal();
            _dummyHealth = Mathf.Max(1f, _dummyHealth);
            _dummyDefence = Mathf.Clamp(_dummyDefence, 0f, 100f);
            if (GUILayout.Button("Apply to all dummies")) ApplyDummyStats();

            GUILayout.Label("Timed measurement", GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Duration (s)", GUILayout.Width(100));
            if (!_measurementRunning)
                _measurementDuration = DrawFloatField(
                    "Measurement.Duration", _measurementDuration, 90);
            else
                GUILayout.Label($"Remaining: {Mathf.Max(0f, _measurementRemaining):0.0}s", GUILayout.Width(150));
            _measurementDuration = Mathf.Max(0.1f, _measurementDuration);
            if (_measurementRunning)
            {
                if (GUILayout.Button("Stop measurement")) StopMeasurement();
            }
            else if (GUILayout.Button("Start measurement"))
            {
                StartMeasurement();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Damage totals", GUI.skin.box);
            GUILayout.Label($"Direct: {_statistics.TotalDirectDamage:0.##}   Effects: {_statistics.TotalEffectDamage:0.##}   Total: {_statistics.TotalDamage:0.##}");
            if (GUILayout.Button("Clear all weapon measurements")) _statistics.Clear();
            foreach (WeaponEntry entry in _weapons)
            {
                GUILayout.Label($"{entry.Config.Type}: direct {_statistics.GetDirectDamage(entry.Entity):0.##}");
                if (entry.Config.Effects == null) continue;
                foreach (EffectConfig effectConfig in entry.Config.Effects)
                {
                    if (effectConfig != null)
                        GUILayout.Label($"  {entry.Config.Type} → {effectConfig.Type}: {_statistics.GetEffectDamage(entry.Entity, effectConfig.Type):0.##}");
                }
            }

            if (selected != null)
            {
                GUILayout.Label(
                    $"Runtime weapon tuning — {selected.Config.Type}",
                    GUI.skin.box,
                    GUILayout.Width(380));
                AttackComponent attack = _attackStash.Get(selected.Entity);
                bool changed = DrawFields(
                    ref attack,
                    typeof(AttackComponent),
                    "Attack",
                    $"Attack.{selected.Config.Type}");
                if (changed) _attackStash.Set(selected.Entity, attack);

                WeaponEffectsComponent effects = _effectsStash.Get(selected.Entity);
                if (effects.Effects != null)
                {
                    GUILayout.Label("Effects", GUI.skin.box, GUILayout.Width(210));
                    for (int i = 0; i < effects.Effects.Effects.Count; i++)
                    {
                        IEffectRuntime effect = effects.Effects.Effects[i];
                        if (effect == null) continue;
                        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(360));
                        GUILayout.Label(effect.Type.ToString(), GUI.skin.box, GUILayout.Width(220));
                        DrawObjectFields(
                            effect,
                            $"Effects.{selected.Config.Type}.{i}.{effect.Type}");
                        GUILayout.EndVertical();
                    }
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.matrix = previousMatrix;
        }

        private float DrawFloatField(string controlName, float value, float width)
        {
            bool wasActive = _activeFloatInput == controlName;
            string text = wasActive &&
                          _floatInputBuffers.TryGetValue(controlName, out string buffered)
                ? buffered
                : value.ToString("0.##", CultureInfo.InvariantCulture);

            GUI.SetNextControlName(controlName);
            string edited = GUILayout.TextField(text, GUILayout.Width(width));
            bool isFocused = GUI.GetNameOfFocusedControl() == controlName;

            if (isFocused)
            {
                _activeFloatInput = controlName;
                _floatInputBuffers[controlName] = edited;
                return TryParseFloat(edited, out float parsed) ? parsed : value;
            }

            _floatInputBuffers.Remove(controlName);
            if (wasActive)
            {
                if (_activeFloatInput == controlName)
                    _activeFloatInput = null;

                return TryParseFloat(edited, out float parsed) ? parsed : value;
            }

            return value;
        }

        private static bool TryParseFloat(string text, out float value)
        {
            string normalized = text.Trim().Replace(',', '.');
            return float.TryParse(
                       normalized,
                       NumberStyles.Float,
                       CultureInfo.InvariantCulture,
                       out value) &&
                   !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }

        private bool DrawFields<T>(
            ref T value,
            Type type,
            string title,
            string fieldPath) where T : struct
        {
            GUILayout.Label(title, GUI.skin.box, GUILayout.Width(210));
            object boxed = value;
            bool changed = DrawFieldsOnObject(
                boxed,
                type,
                fieldPath,
                180,
                130);
            if (changed) value = (T)boxed;
            return changed;
        }

        private bool DrawObjectFields(object value, string fieldPath)
        {
            return value != null && DrawFieldsOnObject(
                value,
                value.GetType(),
                fieldPath);
        }

        private bool DrawFieldsOnObject(
            object value,
            Type type,
            string fieldPath,
            int labelWidth = 180,
            int valueWidth = 130)
        {
            bool changed = false;
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                Type fieldType = field.FieldType;
                object old = field.GetValue(value);
                object next = old;
                bool nestedValueChanged = false;

                if (fieldType.IsValueType &&
                    !fieldType.IsEnum &&
                    !fieldType.IsPrimitive &&
                    fieldType.IsSerializable &&
                    old != null)
                {
                    int nestedLabelWidth = Mathf.Max(100, labelWidth - 40);
                    int nestedPanelWidth = nestedLabelWidth + valueWidth + 30;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(field.Name, GUILayout.Width(labelWidth));
                    GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(nestedPanelWidth));
                    nestedValueChanged = DrawFieldsOnObject(
                        old,
                        fieldType,
                        $"{fieldPath}.{field.Name}",
                        nestedLabelWidth,
                        valueWidth);
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();

                    if (nestedValueChanged)
                    {
                        field.SetValue(value, old);
                        changed = true;
                    }
                    continue;
                }

                GUILayout.BeginHorizontal();
                string displayName = field.Name == "RandomDirectionFallback"
                    ? "Random direction fallback"
                    : field.Name;
                GUILayout.Label(displayName, GUILayout.Width(labelWidth));
                if (fieldType == typeof(float))
                {
                    next = DrawFloatField(
                        $"{fieldPath}.{field.Name}",
                        (float)old,
                        valueWidth);
                }
                else if (fieldType == typeof(int))
                {
                    string input = GUILayout.TextField(
                        Convert.ToString(old, CultureInfo.InvariantCulture),
                        GUILayout.Width(valueWidth));
                    if (int.TryParse(input, out int intValue))
                        next = intValue;
                }
                else if (fieldType == typeof(bool)) next = GUILayout.Toggle((bool)old, "");
                else if (fieldType.IsEnum)
                {
                    Array values = Enum.GetValues(fieldType);
                    int index = Array.IndexOf(values, old);
                    string[] labels = new string[values.Length];
                    for (int i = 0; i < values.Length; i++) labels[i] = values.GetValue(i).ToString();
                    int nextIndex = GUILayout.SelectionGrid(
                        Mathf.Max(0, index),
                        labels,
                        1,
                        GUILayout.Width(Mathf.Max(180, valueWidth + 80)));
                    next = values.GetValue(nextIndex);
                }
                else if (fieldType == typeof(AttackTargetType[]))
                {
                    Array values = Enum.GetValues(typeof(AttackTargetType));
                    List<AttackTargetType> selected = old as AttackTargetType[] == null
                        ? new List<AttackTargetType>() : new List<AttackTargetType>((AttackTargetType[])old);
                    GUILayout.BeginVertical();
                    foreach (AttackTargetType target in values)
                    {
                        bool wasSelected = selected.Contains(target);
                        bool isSelected = GUILayout.Toggle(
                            wasSelected,
                            target.ToString(),
                            GUILayout.Width(Mathf.Max(180, valueWidth + 80)));
                        if (isSelected && !wasSelected) selected.Add(target);
                        if (!isSelected && wasSelected) selected.Remove(target);
                    }
                    GUILayout.EndVertical();
                    next = selected.ToArray();
                }
                GUILayout.EndHorizontal();
                if (nestedValueChanged || !Equals(old, next))
                {
                    field.SetValue(value, next);
                    changed = true;
                }
            }
            return changed;
        }

        private void SetEnabled(WeaponEntry entry, bool enabled)
        {
            entry.Enabled = enabled;
            ref AttackComponent attack = ref _attackStash.Get(entry.Entity);
            if (!enabled)
            {
                _projectileFactory?.Cancel(entry.Entity);
                entry.EnabledTargetTypes = attack.TargetTypes == null
                    ? new AttackTargetType[0]
                    : (AttackTargetType[])attack.TargetTypes.Clone();
                attack.TargetTypes = new AttackTargetType[0];
                if (_requestStash.Has(entry.Entity)) _requestStash.Remove(entry.Entity);
                if (_statusStash.Has(entry.Entity))
                {
                    ref AttackStatusComponent status = ref _statusStash.Get(entry.Entity);
                    status.IsAttacking = false;
                    status.AnimationFinished = true;
                    status.CreationFinished = true;
                }
                attack.CurrentCooldown = attack.AttackCooldown;
            }
            else
            {
                attack.TargetTypes = entry.EnabledTargetTypes ?? new AttackTargetType[0];
                attack.CurrentCooldown = 0f;
            }
        }

        private void Reset(WeaponEntry entry)
        {
            AttackComponent attack = entry.InitialAttack;
            if (!entry.Enabled)
                attack.TargetTypes = new AttackTargetType[0];
            _attackStash.Set(entry.Entity, attack);
            EffectCollection effects = new EffectCollection();
            foreach (IEffectRuntime effect in entry.InitialEffects) effects.Add(CloneEffect(effect));
            _effectsStash.Set(entry.Entity, new WeaponEffectsComponent { Effects = effects });
        }

        private void ApplyDummyStats()
        {
            foreach (WeaponTestDummy dummy in FindObjectsOfType<WeaponTestDummy>())
                dummy.SetTestStats(_dummyHealth, _dummyDefence);
        }

        private void StartMeasurement()
        {
            if (_world == null || _statistics == null)
                return;

            _statistics.SetRecording(false);
            ResetWeaponCycles(restartImmediately: true);
            CleanupOwnedProjectiles();
            ClearPendingBattleStatistics();

            _measurementRemaining = Mathf.Max(0.1f, _measurementDuration);
            _measurementRunning = true;
            _statistics.SetRecording(true);
        }

        private void StopMeasurement()
        {
            if (!_measurementRunning)
                return;

            _measurementRunning = false;
            _statistics.SetRecording(false);
            ResetWeaponCycles(restartImmediately: false);
            CleanupOwnedProjectiles();
        }

        private void ResetWeaponCycles(bool restartImmediately)
        {
            foreach (WeaponEntry entry in _weapons)
            {
                _projectileFactory?.Cancel(entry.Entity);

                if (_requestStash.Has(entry.Entity))
                    _requestStash.Remove(entry.Entity);
                if (_creationRequestStash.Has(entry.Entity))
                    _creationRequestStash.Remove(entry.Entity);
                if (_statusStash.Has(entry.Entity))
                {
                    ref AttackStatusComponent status = ref _statusStash.Get(entry.Entity);
                    status.IsAttacking = false;
                    status.AnimationFinished = true;
                    status.AttackPerformed = false;
                    status.CreationFinished = true;
                }

                ref AttackComponent attack = ref _attackStash.Get(entry.Entity);
                attack.CurrentCooldown = restartImmediately && entry.Enabled
                    ? 0f
                    : attack.AttackCooldown;
            }
        }

        private void CleanupOwnedProjectiles()
        {
            Filter projectiles = _world.Filter.With<ProjectileComponent>().Build();
            foreach (Entity projectileEntity in projectiles)
            {
                if (_world.IsDisposed(projectileEntity))
                    continue;

                ref ProjectileComponent projectile = ref _projectileStash.Get(projectileEntity);
                if (projectile.CleanupRequested ||
                    projectile.Weapon == default ||
                    _world.IsDisposed(projectile.Weapon) ||
                    !_weaponStash.Has(projectile.Weapon) ||
                    _weaponStash.Get(projectile.Weapon).Owner != _owner)
                {
                    continue;
                }

                projectile.CleanupRequested = true;
                projectile.CollisionEnabled = false;
                if (_projectileViewStash.Has(projectileEntity))
                {
                    Gameplay.View.AnimatedView view =
                        _projectileViewStash.Get(projectileEntity).View;
                    if (view != null)
                        view.SetActive(false);
                }

                Entity cleanupRequest = _world.CreateEntity();
                _cleanupRequestStash.Set(cleanupRequest, new CleanupRequest
                {
                    Candidate = projectileEntity
                });
            }
        }

        private void ClearPendingBattleStatistics()
        {
            Filter statistics = _world.Filter.With<BattleStatisticComponent>().Build();
            _entitiesToRemove.Clear();
            foreach (Entity entity in statistics)
            {
                if (!_world.IsDisposed(entity))
                    _entitiesToRemove.Add(entity);
            }

            foreach (Entity entity in _entitiesToRemove)
                _world.RemoveEntity(entity);
        }

        private void OnDestroy()
        {
            if (_world != null && _testSystems != null)
                _world.RemoveSystemsGroup(_testSystems);
            if (_world != null && _owner != default)
            {
                foreach (WeaponEntry entry in _weapons)
                    _world.RemoveEntity(entry.Entity);
                _world.RemoveEntity(_owner);
            }
            if (_playerObject != null)
                Destroy(_playerObject);
        }
    }
}
