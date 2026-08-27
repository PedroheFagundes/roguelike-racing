using System;
using RoguelikeRacing.Kart;

namespace RoguelikeRacing.Race
{
    /// <summary>A permanent stat upgrade applied directly to a KartController's public fields.</summary>
    public readonly struct KartUpgrade
    {
        public readonly string Name;
        public readonly string Description;
        public readonly Action<KartController> Apply;

        public KartUpgrade(string name, string description, Action<KartController> apply)
        {
            Name = name;
            Description = description;
            Apply = apply;
        }
    }
}
