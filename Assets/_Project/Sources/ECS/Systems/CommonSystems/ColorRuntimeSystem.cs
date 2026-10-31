using ECS.CommonComponents;
using ECS.ViewComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonSystems
{
    public sealed class ColorRuntimeSystem : ISystem
    {
        private Filter _filter;
        private Stash<ColorRuntimeComponent> _colorStash;
        private Stash<ViewComponent> _viewStash;

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter.With<ColorRuntimeComponent>().With<ViewComponent>().Build();
            _colorStash = World.GetStash<ColorRuntimeComponent>();
            _viewStash = World.GetStash<ViewComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref ColorRuntimeComponent runtime = ref _colorStash.Get(entity);
                if (runtime.ActiveColors == null || runtime.ActiveColors.Count == 0)
                    continue;

                float red = 0f, green = 0f, blue = 0f, alpha = 0f, totalWeight = 0f;
                for (int i = runtime.ActiveColors.Count - 1; i >= 0; i--)
                {
                    ColorRuntime effect = runtime.ActiveColors[i];
                    effect.CurrentTime += deltaTime;
                    if (effect.Duration <= 0f || effect.CurrentTime >= effect.Duration)
                    {
                        runtime.ActiveColors.RemoveAt(i);
                        continue;
                    }

                    float weight = 1f - effect.CurrentTime / effect.Duration;
                    runtime.ActiveColors[i] = effect;
                    red += effect.Reference.r * weight;
                    green += effect.Reference.g * weight;
                    blue += effect.Reference.b * weight;
                    alpha += effect.Reference.a * weight;
                    totalWeight += weight;
                }

                Color baseColor = _viewStash.Get(entity).View.BaseColor;
                float baseWeight = Mathf.Max(0f, 1f - totalWeight);
                red += baseColor.r * baseWeight;
                green += baseColor.g * baseWeight;
                blue += baseColor.b * baseWeight;
                alpha += baseColor.a * baseWeight;
                totalWeight += baseWeight;

                Color result = totalWeight > 0f
                    ? new Color(red / totalWeight, green / totalWeight, blue / totalWeight, alpha / totalWeight)
                    : baseColor;
                _viewStash.Get(entity).View.SetColor(result);
            }
        }

        public void Dispose() { }
    }
}
