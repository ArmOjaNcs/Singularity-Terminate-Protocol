using Scellecs.Morpeh;
using UniRx;

[System.Serializable]
public struct BattleTimerComponent : IComponent
{
    public FloatReactiveProperty CurrentTime;

    public float MaxTime;
}