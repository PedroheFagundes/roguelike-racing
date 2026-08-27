using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// Trigger box on the track. When the player's kart enters, pauses and offers
    /// OptionsPerBox items sampled at random from ItemCatalog (PauseChoiceUI — same
    /// shared pattern as LevelUpController); picking one HOLDS it in the kart's
    /// KartInventory rather than applying it immediately — the player activates it later
    /// with the "use item" button (see KartInput / RaceHud). AI karts skip the choice UI
    /// entirely, hold a random item from the full catalog, and use it right away since
    /// they have no timing strategy yet — see docs/DESIGN_DECISIONS.md.
    ///
    /// Respawns after a cooldown instead of being destroyed, so a single lap doesn't run
    /// the track dry of items.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ItemBox : MonoBehaviour
    {
        public float respawnCooldownSeconds = 8f;
        public float spinDegreesPerSecond = 90f;
        public int optionsPerBox = 4;

        PauseChoiceUI _pauseChoiceUI;
        GameObject _playerKart;
        Collider _collider;
        Renderer[] _renderers;
        bool _collected;

        public void Initialize(PauseChoiceUI pauseChoiceUI, GameObject playerKart)
        {
            _pauseChoiceUI = pauseChoiceUI;
            _playerKart = playerKart;
        }

        void Awake()
        {
            _collider = GetComponent<Collider>();
            _renderers = GetComponentsInChildren<Renderer>();
        }

        void Update()
        {
            transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            if (_collected) return;

            var inventory = other.GetComponentInParent<KartInventory>();
            if (inventory == null) return;

            _collected = true;
            SetVisible(false);

            if (other.gameObject == _playerKart && _pauseChoiceUI != null)
            {
                OpenChoiceForPlayer(inventory);
            }
            else
            {
                ItemDefinition item = ItemCatalog.All[Random.Range(0, ItemCatalog.All.Count)];
                inventory.Hold(item);
                inventory.UseHeldItem();
            }

            StartCoroutine(RespawnAfterCooldown());
        }

        void OpenChoiceForPlayer(KartInventory playerInventory)
        {
            List<ItemDefinition> choices = RandomPick.Distinct(ItemCatalog.All, optionsPerBox);
            var prompts = new List<ChoicePrompt>(choices.Count);
            foreach (ItemDefinition item in choices)
            {
                prompts.Add(new ChoicePrompt(item.Name, item.Description, () => playerInventory.Hold(item)));
            }

            _pauseChoiceUI.Open("Item! Escolha um (use depois com o botao de item)", prompts);
        }

        IEnumerator RespawnAfterCooldown()
        {
            yield return new WaitForSeconds(respawnCooldownSeconds);
            _collected = false;
            SetVisible(true);
        }

        void SetVisible(bool visible)
        {
            _collider.enabled = visible;
            foreach (Renderer renderer in _renderers)
            {
                renderer.enabled = visible;
            }
        }
    }
}
