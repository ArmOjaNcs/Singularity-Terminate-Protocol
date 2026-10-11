using ECS.CommonComponents;

namespace EffectComponents
{
    public interface IEffectRuntime
    {
        EffectType Type { get; }
    }
}