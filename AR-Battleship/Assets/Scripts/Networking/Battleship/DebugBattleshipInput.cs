using ARBattleship.Core.Domain;
using UnityEngine;

namespace ARBattleship.Multiplayer.Battleship
{
    public sealed class DebugBattleshipInput : MonoBehaviour
    {
        [SerializeField] private NetworkBattleshipGameController controller;

        private void Update()
        {
            if (controller == null)
            {
                return;
            }

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

            if (Input.GetKeyDown(KeyCode.Return))
            {
                controller.RequestStartGame();
            }

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