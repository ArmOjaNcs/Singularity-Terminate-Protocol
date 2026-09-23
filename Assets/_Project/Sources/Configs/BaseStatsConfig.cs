using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(
        fileName = "BaseStatsConfig",
        menuName = "Game/Base Stats Config"
    )]
    public class BaseStatsConfig : ScriptableObject
    {
        public BaseStats Stats;
    }
}