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

        public bool Has<T>() where T : class, IEffectRuntime
        {
            foreach (IEffectRuntime effect in _effects)
            {
                if (effect is T)
                    return true;
            }

            return false;
        }

        public T Get<T>() where T : class, IEffectRuntime
        {
            foreach (IEffectRuntime effect in _effects)
            {
                if (effect is T runtime)
                    return runtime;
            }

            return null;
        }

        public bool TryGet<T>(out T runtime) where T : class, IEffectRuntime
        {
            foreach (IEffectRuntime effect in _effects)
            {
                if (effect is T result)
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