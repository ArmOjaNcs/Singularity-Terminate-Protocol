using Scellecs.Morpeh;
using UnityEngine;

public sealed class BattleTimerSystem : ISystem
{
    private Filter _filter;
    private Stash<BattleTimerComponent> _battleTimerStash;
    private float _maxTime;
    private TimerPanel _panelView;

    public BattleTimerSystem(float maxTime, TimerPanel panelView)
    {
        _maxTime = maxTime;
        _panelView = panelView;
    }

    public World World { get; set; }

    public void OnAwake()
    {
        _filter = World.Filter.With<BattleTimerComponent>().Build();
        _battleTimerStash = World.GetStash<BattleTimerComponent>();

        if (_filter.IsEmpty())
        {
            Entity entity = World.CreateEntity();
            ref var timer = ref _battleTimerStash.Add(entity);

            _battleTimerStash.Set(entity, new BattleTimerComponent
            {
                CurrentTime = new UniRx.FloatReactiveProperty(0f),
                MaxTime = _maxTime,
            });

            _panelView.Setup(ref timer);
        }
    }

    public void OnUpdate(float deltaTime)
    {
        foreach (var entity in _filter)
        {
            ref var timer = ref _battleTimerStash.Get(entity);

            if (timer.CurrentTime.Value >= timer.MaxTime)
                continue;

            timer.CurrentTime.Value = Mathf.Min(timer.CurrentTime.Value + deltaTime, timer.MaxTime);
        }
    }

    public void Dispose()
    {
    }
}