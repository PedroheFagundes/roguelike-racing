using System;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// One offered choice in a pause-and-pick decision (level up today, item box in
    /// step 5). Apply is invoked once when the player picks this option — this is the
    /// single point that applies an effect, which matters for multiplayer later (see
    /// docs/DESIGN_DECISIONS.md): swapping who calls Apply (local click vs a networked
    /// "ApplyDecision" RPC) won't require touching how options are built or displayed.
    /// </summary>
    public readonly struct ChoicePrompt
    {
        public readonly string Title;
        public readonly string Description;
        public readonly Action Apply;

        public ChoicePrompt(string title, string description, Action apply)
        {
            Title = title;
            Description = description;
            Apply = apply;
        }
    }
}
