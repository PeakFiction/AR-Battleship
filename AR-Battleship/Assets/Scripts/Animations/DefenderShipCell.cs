using UnityEngine;

public class DefenderShipCell : MonoBehaviour
{
    public GameObject defenderBurnEffectValue;
    public Vector3 defenderBurnOffsetValue = new Vector3(2.43f, 1.5f, 1.76f); // sets fire position

    private bool defenderAlreadyHitValue = false; // tracks if it was already hit

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