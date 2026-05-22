// -----------------------------------------------------------------------------
// File: DefenderShipCell.cs
// Purpose: Contains gameplay/visual behaviour logic for the AR Battleship project.
// Comments were added automatically without modifying executable code.
// -----------------------------------------------------------------------------

using UnityEngine;

public class DefenderShipCell : MonoBehaviour
{
    // Public field exposed in the Unity Inspector.
    public GameObject defenderBurnEffectValue;
    // Method responsible for: Vector3.
    // Public field exposed in the Unity Inspector.
    public Vector3 defenderBurnOffsetValue = new Vector3(2.43f, 1.5f, 1.76f); // sets fire position

    private bool defenderAlreadyHitValue = false; // tracks if it was already hit

    // Method responsible for: TriggerHitEffect.
    public void TriggerHitEffect()
    {
        if (defenderAlreadyHitValue) return;

        defenderAlreadyHitValue = true; // limits to only one hit

        if (defenderBurnEffectValue != null)
        {
            Instantiate( // starts the burn effects
                defenderBurnEffectValue,
                transform.position + defenderBurnOffsetValue,
                Quaternion.identity
            );
        }
    }
}