using EnemyConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace Gameplay.Enemy
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyConfig Config;
        [SerializeField] private Vector3 SpawnPosition;

        private EnemyFactory _factory;

        private void Start()
        {
            _factory = new EnemyFactory(World.Default);

            _factory.Create(
                Config,
                SpawnPosition);
        }
    }
}