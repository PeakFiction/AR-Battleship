using ARBattleship.Multiplayer.Battleship;
using ARBattleship.Unity;
using UnityEngine;

namespace ARBattleship.Unity.UI
{
    public sealed class MultiplayerGameplayUIFlow : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject placementUI;
        [SerializeField] private GameObject combatUI;
        [SerializeField] private GameObject gameOverUI;
        [SerializeField] private GameObject combatUIToggle;
        [SerializeField] private GameObject manualFireUI;

        private void Awake()
        {
            ShowPlacementUI();
        }

        private void OnEnable()
        {
            GameManager.OnGameStarted += ShowCombatUI;
            NetworkBattleshipEvents.BattleStarted += OnBattleStarted;
        }

        private void OnDisable()
        {
            GameManager.OnGameStarted -= ShowCombatUI;
            NetworkBattleshipEvents.BattleStarted -= OnBattleStarted;
        }

        private void OnBattleStarted(int startingPlayerNumber)
        {
            Debug.Log($"[UI Flow] Battle started. Player {startingPlayerNumber} goes first.");
            ShowCombatUI();
        }

        private void ShowPlacementUI()
        {
            SetActiveSafe(placementUI, true);
            SetActiveSafe(combatUI, false);
            SetActiveSafe(gameOverUI, false);
            SetActiveSafe(combatUIToggle, false);
            SetActiveSafe(manualFireUI, false);
        }

        private void ShowCombatUI()
        {
            Debug.Log("[UI Flow] Showing combat UI.");

            SetActiveSafe(placementUI, false);
            SetActiveSafe(combatUI, true);
            SetActiveSafe(gameOverUI, false);

            /*
             * Enable these only if you still want manual/debug combat controls.
             * If AR controls firing, you may want manualFireUI false.
             */
            SetActiveSafe(combatUIToggle, true);
            SetActiveSafe(manualFireUI, true);
        }

        private static void SetActiveSafe(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }
}