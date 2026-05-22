// Temporary keyboard-driven test input for the multiplayer Battleship controller.
// This script is intended for editor/debug use so the network flow can be tested
// without needing the full AR input or UI setup.

using ARBattleship.Core.Domain;
using UnityEngine;

namespace ARBattleship.Multiplayer.Battleship
{
    /// <summary>
    /// Sends hard-coded ship placement and shot requests to the network
    /// controller when specific keyboard keys are pressed.
    /// </summary>
    public sealed class DebugBattleshipInput : MonoBehaviour
    {
        [SerializeField] private NetworkBattleshipGameController controller;

        private void Update()
        {
            // The controller is assigned from the Inspector. If it is missing,
            // avoid throwing null reference errors while testing the scene.
            if (controller == null)
            {
                return;
            }

            // Place each ship in a fixed horizontal row for quick debugging.
            if (Input.GetKeyDown(KeyCode.C))
            {
                controller.RequestPlaceShip(
                    "Carrier",
                    0,
                    0,
                    Orientation.Horizontal
                );
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                controller.RequestPlaceShip(
                    "Battleship",
                    0,
                    1,
                    Orientation.Horizontal
                );
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                controller.RequestPlaceShip(
                    "Cruiser",
                    0,
                    2,
                    Orientation.Horizontal
                );
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                controller.RequestPlaceShip(
                    "Submarine",
                    0,
                    3,
                    Orientation.Horizontal
                );
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                controller.RequestPlaceShip(
                    "Destroyer",
                    0,
                    4,
                    Orientation.Horizontal
                );
            }

            // Press Enter after both players have placed ships to request battle start.
            if (Input.GetKeyDown(KeyCode.Return))
            {
                controller.RequestStartGame();
            }

            // Fire at a few fixed cells so shot RPCs can be tested quickly.
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                controller.RequestFireShot(0, 0);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                controller.RequestFireShot(1, 0);
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                controller.RequestFireShot(2, 0);
            }
        }
    }
}
