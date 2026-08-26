using System.Collections;
using System.Collections.Generic;
using RoguelikeRacing.Kart;
using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// Trigger box on the track. When the player's kart enters, pauses and offers all 4
    /// items in ItemCatalog as a choice (PauseChoiceUI — same shared pattern as
    /// LevelUpController). AI karts skip the choice UI entirely and get a random item
    /// from the same catalog applied immediately, mirroring how AI level-ups work — see
    /// docs/DESIGN_DECISIONS.md.
    ///
    /// Respawns after a cooldown instead of being destroyed, so a single lap doesn't run
    /// the track dry of items.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ItemBox : MonoBehaviour
    {
        public float respawnCooldownSeconds = 8f;
        public float spinDegreesPerSecond = 90f;

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

            var kart = other.GetComponentInParent<KartController>();
            if (kart == null) return;

            _collected = true;
            SetVisible(false);

            if (other.gameObject == _playerKart && _pauseChoiceUI != null)
            {
                OpenChoiceForPlayer(kart);
            }
            else
            {
                ItemDefinition item = ItemCatalog.All[Random.Range(0, ItemCatalog.All.Count)];
                item.Use(kart);
            }

            StartCoroutine(RespawnAfterCooldown());
        }

        void OpenChoiceForPlayer(KartController playerController)
        {
            var prompts = new List<ChoicePrompt>(ItemCatalog.All.Count);
            foreach (ItemDefinition item in ItemCatalog.All)
            {
                prompts.Add(new ChoicePrompt(item.Name, item.Description, () => item.Use(playerController)));
            }

            _pauseChoiceUI.Open("Item! Escolha um", prompts);
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
