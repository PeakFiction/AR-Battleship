using ARBattleship.Multiplayer.Battleship;
using ARBattleship.Unity;
using UnityEngine;

namespace ARBattleship.Unity.UI
{
    /// <summary>
    /// Sealed MonoBehaviour that drives the top-level UI panel flow for the
    /// multiplayer gameplay scene.  Subscribes to both local (GameManager) and
    /// network (NetworkBattleshipEvents) battle-start signals to ensure the
    /// combat UI appears regardless of which side initiates the phase change.
    /// </summary>
    public sealed class MultiplayerGameplayUIFlow : MonoBehaviour
    {

        [Header("UI Panels")]

        /// <summary>Root GameObject for the ship placement UI (shown during Setup phase).</summary>
        [SerializeField] private GameObject placementUI;

        /// <summary>Root GameObject for the in-combat HUD (shown during InProgress phase).</summary>
        [SerializeField] private GameObject combatUI;

        /// <summary>Root GameObject for the game-over overlay (managed by GameOverUI).</summary>
        [SerializeField] private GameObject gameOverUI;

        /// <summary>
        /// Optional toggle button / panel for switching the 2D board view.
        /// Enable only when AR controls firing; disable if manual fire is active.
        /// </summary>
        [SerializeField] private GameObject combatUIToggle;

        /// <summary>
        /// Optional manual-fire debug panel.
        /// Useful during development; may be disabled in AR production builds.
        /// </summary>
        [SerializeField] private GameObject manualFireUI;

        /// <summary>
        /// Shows the ship placement UI immediately on Awake so it is visible
        /// from the first frame before any network events arrive.
        /// </summary>
        private void Awake() => ShowPlacementUI();

        /// <summary>
        /// Subscribes to the game-start event from both the local GameManager
        /// and the network layer so the combat UI is always shown at the right time.
        /// </summary>
        private void OnEnable()
        {
            GameManager.OnGameStarted                  += ShowCombatUI;
            NetworkBattleshipEvents.BattleStarted      += OnBattleStarted;
        }

        /// <summary>
        /// Unsubscribes all event listeners to prevent ghost callbacks after
        /// the scene is unloaded.
        /// </summary>
        private void OnDisable()
        {
            GameManager.OnGameStarted             -= ShowCombatUI;
            NetworkBattleshipEvents.BattleStarted -= OnBattleStarted;
        }

        /// <summary>
        /// Handles the network BattleStarted event, which carries the index of
        /// the player who takes the first shot.  Logs the starting player and
        /// delegates the panel transition to ShowCombatUI.
        /// </summary>
        /// <param name="startingPlayerNumber">1-based index of the first player.</param>
        private void OnBattleStarted(int startingPlayerNumber)
        {
            Debug.Log($"[UI Flow] Battle started. Player {startingPlayerNumber} goes first.");
            ShowCombatUI();
        }

        /// <summary>
        /// Activates the placement panel and hides all combat-related panels.
        /// Called from Awake so the player sees placement UI from the first frame.
        /// </summary>
        private void ShowPlacementUI()
        {
            SetActiveSafe(placementUI,    true);
            SetActiveSafe(combatUI,       false);
            SetActiveSafe(gameOverUI,     false);
            SetActiveSafe(combatUIToggle, false);
            SetActiveSafe(manualFireUI,   false);
        }

        /// <summary>
        /// Activates the combat HUD and hides the placement UI.
        /// Called by both GameManager.OnGameStarted and NetworkBattleshipEvents.BattleStarted
        /// to guarantee the transition fires in all multiplayer configurations.
        /// </summary>
        private void ShowCombatUI()
        {
            Debug.Log("[UI Flow] Showing combat UI.");

            SetActiveSafe(placementUI, false);
            SetActiveSafe(combatUI,    true);
            SetActiveSafe(gameOverUI,  false);

            /*
             * Enable these only if you still want manual / debug combat controls.
             * If AR controls firing, you may want manualFireUI false.
             */
            SetActiveSafe(combatUIToggle, true);
            SetActiveSafe(manualFireUI,   true);
        }

        /// <summary>
        /// Calls SetActive on <paramref name="target"/> only if it is not null.
        /// Prevents NullReferenceExceptions when optional panels are not assigned
        /// in the Inspector.
        /// </summary>
        /// <param name="target">The GameObject to show or hide (may be null).</param>
        /// <param name="active">True to activate; false to deactivate.</param>
        private static void SetActiveSafe(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }
    }
}
