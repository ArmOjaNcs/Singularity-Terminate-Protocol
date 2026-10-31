using System.Collections.Generic;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonComponents
{
    public struct ColorRuntimeComponent : IComponent
    {
        public List<ColorRuntime> ActiveColors;
    }

    public struct ColorRuntime
    {
        public Color Reference;
        public float CurrentTime;
        public float Duration;
    }
}
