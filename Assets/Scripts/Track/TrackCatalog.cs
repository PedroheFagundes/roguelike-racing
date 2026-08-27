using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeRacing.Track
{
    /// <summary>
    /// A selectable track: a name/description for the setup UI plus a factory for its
    /// centerline points. Deferred as a Func so picking a track doesn't build it —
    /// RaceSetupUI only calls BuildCenterline once the player confirms.
    /// </summary>
    public readonly struct TrackLayout
    {
        public readonly string Name;
        public readonly string Description;
        public readonly Func<List<Vector3>> BuildCenterline;

        public TrackLayout(string name, string description, Func<List<Vector3>> buildCenterline)
        {
            Name = name;
            Description = description;
            BuildCenterline = buildCenterline;
        }
    }

    /// <summary>The full pool of selectable tracks, shown on the pre-race setup screen.</summary>
    public static class TrackCatalog
    {
        public static readonly List<TrackLayout> All = new List<TrackLayout>
        {
            new TrackLayout(
                "Oval",
                "Tracado classico, curvas largas e continuas",
                () => TrackBuilder.GenerateOvalCenterline()),

            new TrackLayout(
                "Estadio",
                "Retas longas com uma curva fechada em cada ponta",
                () => TrackBuilder.GenerateStadiumCenterline()),

            new TrackLayout(
                "Tecnica",
                "Curvas apertadas alternadas, exige mais manejo",
                () => TrackBuilder.GenerateTechnicalCenterline()),
        };
    }
}
