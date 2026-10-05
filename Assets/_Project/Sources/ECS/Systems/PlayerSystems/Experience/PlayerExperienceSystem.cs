using ECS.PlayerSystems;
using Scellecs.Morpeh;
using UnityEngine;

public sealed class PlayerExperienceSystem : ISystem
{
    private Filter _filter;
    private Stash<PlayerExperienceComponent> _experienceStash;
    private Stash<AddExperienceEvent> _addExperienceStash;
    private Stash<LevelUpEvent> _levelUpStash;

    public World World { get; set; }

    public void OnAwake()
    {
        _filter = World.Filter
            .With<PlayerExperienceComponent>()
            .With<AddExperienceEvent>()
            .Build();

        _experienceStash = World.GetStash<PlayerExperienceComponent>();
        _addExperienceStash = World.GetStash<AddExperienceEvent>();
        _levelUpStash = World.GetStash<LevelUpEvent>();
    }

    public void OnUpdate(float deltaTime)
    {
        foreach (var entity in _filter)
        {
            ref var xpComp = ref _experienceStash.Get(entity);
            ref var eventComp = ref _addExperienceStash.Get(entity);

            xpComp.CurrentXP.Value += eventComp.Amount;

            if (xpComp.CurrentXP.Value >= xpComp.NextLevelXP)
            {
                xpComp.CurrentXP.Value -= xpComp.NextLevelXP;
                xpComp.CurrentLevel++;

                xpComp.NextLevelXP = Mathf.RoundToInt(xpComp.NextLevelXP * 1.2f);

                _levelUpStash.Set(entity, new LevelUpEvent { NewLevel = xpComp.CurrentLevel });
            }

            _addExperienceStash.Remove(entity);
        }
    }

    public void Dispose()
    {
    }
}