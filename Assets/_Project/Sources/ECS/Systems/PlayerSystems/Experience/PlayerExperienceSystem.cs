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
            ref var addExperienceEvent = ref _addExperienceStash.Get(entity);

            int addXp = addExperienceEvent.Amount;
            Entity looter = addExperienceEvent.LooterEntity;

            if (World.IsDisposed(looter))
                continue;

            if (_experienceStash.Has(looter))
            {
                ref var xpComp = ref _experienceStash.Get(looter);

                xpComp.CurrentXP.Value += addXp;

                if (xpComp.CurrentXP.Value >= xpComp.NextLevelXP.Value)
                {
                    while (xpComp.NextLevelXP.Value <= xpComp.CurrentXP.Value)
                    {
                        xpComp.CurrentXP.Value -= xpComp.NextLevelXP.Value;
                        xpComp.NextLevelXP.Value = Mathf.RoundToInt(xpComp.NextLevelXP.Value * 1.2f);
                        xpComp.CurrentLevel.Value++;

                        Entity levelUpEntity = World.CreateEntity();

                        _levelUpStash.Set(levelUpEntity, new LevelUpEvent { NewLevel = xpComp.CurrentLevel.Value });
                    }
                }

                _addExperienceStash.Remove(entity);
            }
        }
    }

    public void Dispose()
    {
    }
}