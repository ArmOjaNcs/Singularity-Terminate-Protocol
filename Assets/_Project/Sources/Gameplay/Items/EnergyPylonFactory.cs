using System.Collections;
using System.Collections.Generic;
using ECS.ViewComponents;
using Scellecs.Morpeh;
using UnityEngine;

public sealed class EnergyPylonFactory
{
    private readonly World _world;
    private readonly Stash<AnimatedCreatureComponent> _animatedCreatureStash;

    private Transform _container;

    public EnergyPylonFactory(
        World world,
        Transform container)
    {
        _world = world;
        _container = container;
    }

    public Entity Create(EnergyPylonConfig config, Vector3 position)
    {
        if (config == null || config.HitBox == null)
            return default;

        return default;
    }
}
