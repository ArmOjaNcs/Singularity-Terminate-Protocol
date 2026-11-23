using ECS.CommonComponents;
using EffectComponents;
using System;
using System.Collections.Generic;

namespace ECS.EffectComponents
{
    [Serializable]
    public class EffectCollection
    {
        private readonly List<IEffectRuntime> _effects = new();

        public IReadOnlyList<IEffectRuntime> Effects => _effects;

        public void Add(IEffectRuntime effect)
        {
            _effects.Add(effect);
        }

        public bool TryGet<T>(EffectType type, out T runtime) where T : class, IEffectRuntime
        {
            foreach (IEffectRuntime effect in _effects)
            {
                if (effect is T result && result.Type == type)
                {
                    runtime = result;
                    return true;
                }
            }

            runtime = null;
            return false;
        }
    }
}