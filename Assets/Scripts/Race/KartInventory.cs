using RoguelikeRacing.Kart;
using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// Holds at most one item (Mario-Kart style: picking up a new one while holding
    /// something replaces it) until UseHeldItem is called. Attached to every kart:
    /// the player triggers UseHeldItem from KartInput on a button press (see the HUD
    /// hint in RaceHud); AI (see ItemBox) calls it immediately after Hold, since it has
    /// no use-timing strategy yet.
    /// </summary>
    [RequireComponent(typeof(KartController))]
    public class KartInventory : MonoBehaviour
    {
        public ItemDefinition? HeldItem { get; private set; }

        KartController _controller;

        void Awake()
        {
            _controller = GetComponent<KartController>();
        }

        public void Hold(ItemDefinition item)
        {
            HeldItem = item;
        }

        public bool UseHeldItem()
        {
            if (!HeldItem.HasValue) return false;

            ItemDefinition item = HeldItem.Value;
            HeldItem = null;
            item.Use(_controller);
            return true;
        }
    }
}
