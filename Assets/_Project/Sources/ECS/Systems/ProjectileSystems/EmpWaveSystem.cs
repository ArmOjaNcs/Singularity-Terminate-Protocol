using System.Collections.Generic;
using Core.Game;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using Gameplay.View;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class EmpWaveSystem : ISystem
    {
        private readonly VoltProjectileConfig _config;
        private readonly ObjectPool<EmpWaveView> _pool;
        private Filter _requests;
        public World World { get; set; }

        public EmpWaveSystem(ProjectileDatabase database, Transform container)
        {
            _config = database.Get(WeaponType.Volt) as VoltProjectileConfig;
            if (_config != null && _config.WavePrefab != null)
                _pool = new ObjectPool<EmpWaveView>(_config.WavePrefab, _config.PoolCapacity, container);
        }

        public void OnAwake()
        {
            _requests = World.Filter.With<EmpWaveRequest>().Build();
        }

        public void OnUpdate(float deltaTime)
        {
            var requests = World.GetStash<EmpWaveRequest>();
            foreach (Entity wave in _requests)
            {
                EmpWaveRequest hit = requests.Get(wave);
                requests.Remove(wave);
                if (_config == null || World.IsDisposed(hit.Weapon))
                {
                    World.RemoveEntity(wave);
                    continue;
                }

                World.GetStash<PositionComponent>().Set(wave, new PositionComponent { Position = hit.Position });
                World.GetStash<RotationComponent>().Set(wave, new RotationComponent());
                World.GetStash<HitBoxComponent>().Set(wave, new HitBoxComponent());
                World.GetStash<AreaExpansionComponent>().Set(wave, new AreaExpansionComponent
                {
                    BaseCollider = new ColliderComponent { Type = ColliderType.Circle, Radius = _config.WaveRadius },
                    Duration = _config.WaveDuration
                });
                World.GetStash<ProjectileComponent>().Set(wave, new ProjectileComponent
                {
                    Weapon = hit.Weapon,
                    WeaponType = WeaponType.Volt,
                    Damage = hit.Damage,
                    TargetTypes = hit.TargetTypes,
                    HitPolicy = HitPolicyType.OncePerTarget,
                    CollisionEnabled = true,
                    AttackCompletionReported = true,
                    ViewActivated = _pool != null
                });
                World.GetStash<ProjectileHitComponent>().Set(wave, new ProjectileHitComponent
                {
                    Records = new List<ProjectileHitRecord>()
                });

                if (_pool == null)
                    continue;
                EmpWaveView view = _pool.GetElement();
                view.SetEntity(wave);
                view.transform.position = hit.Position;
                view.SetActive(true);
                view.Begin(_config.WaveRadius);
                World.GetStash<ProjectileViewComponent>().Set(wave, new ProjectileViewComponent { View = view });
            }
        }

        public void Dispose() { }
    }
}
