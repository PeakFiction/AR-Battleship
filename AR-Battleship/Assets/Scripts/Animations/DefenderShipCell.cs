/*using UnityEngine;

public class DefenderShipCell : MonoBehaviour
{
    public GameObject explosionEffectPrefab;
    public GameObject fireEffectPrefab;

    public Vector3 explosionOffsetPos = new Vector3(0f, 0.2f, 0f);
    public Vector3 fireOffsetPos = new Vector3(0f, 0.3f, 0f);

    private bool alreadyHitFlag = false;

    public void TriggerHitEffect()
    {
        if (alreadyHitFlag) return;

        alreadyHitFlag = true;

        if (explosionEffectPrefab != null)
        {
            Instantiate(
                explosionEffectPrefab,
                transform.position + explosionOffsetPos,
                Quaternion.identity
            );
        }

        if (fireEffectPrefab != null)
        {
            Instantiate(
                fireEffectPrefab,
                transform.position + fireOffsetPos,
                Quaternion.identity
            );
        }
    }
}*/

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