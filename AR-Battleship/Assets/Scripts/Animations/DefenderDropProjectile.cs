// -----------------------------------------------------------------------------
// File: DefenderDropProjectile.cs
// Purpose: Contains gameplay/visual behaviour logic for the AR Battleship project.
// Comments were added automatically without modifying executable code.
// -----------------------------------------------------------------------------

using UnityEngine;

public class DropMissileExplosion : MonoBehaviour
{
    // Public field exposed in the Unity Inspector.
    public float dropSpeedAlt = 2f;

    // Public field exposed in the Unity Inspector.
    public GameObject explosionPrefabAlt;
    // Method responsible for: Vector3.
    // Public field exposed in the Unity Inspector.
    public Vector3 explosionOffsetAlt = new Vector3(0f, 0.2f, 0f);

    // Public field exposed in the Unity Inspector.
    public Transform targetPointAlt;
    // Public field exposed in the Unity Inspector.
    public float spawnHeightAlt = 25f;
    // Public field exposed in the Unity Inspector.
    public float impactHeightAlt = 0.2f;

    private Vector3 startPosAlt;
    private Vector3 endPosAlt;
    private float journeyAlt = 0f;
    private bool hasImpactedAlt = false;

    // Unity Start method called before the first frame update.
    void Start()
    {
        startPosAlt = new Vector3(2.02161f, 25f, 2.02161f);
        endPosAlt   = new Vector3(2.02161f, 0f, 2.02161f);

        transform.position = startPosAlt;
    }

    // Unity Update method called once per frame.
    void Update()
    {
        if (hasImpactedAlt) return;

        journeyAlt += Time.deltaTime * dropSpeedAlt;
        journeyAlt = Mathf.Clamp01(journeyAlt);

        transform.position = Vector3.Lerp(startPosAlt, endPosAlt, journeyAlt);

        if (journeyAlt >= 1f)
        {
            TriggerImpactAlt();
        }
    }

    void TriggerImpactAlt()
    {
        if (hasImpactedAlt) return;
        hasImpactedAlt = true;

        if (explosionPrefabAlt != null)
        {
            GameObject explosionInstance = Instantiate(
    explosionPrefabAlt,
    endPosAlt + explosionOffsetAlt,
    Quaternion.identity
);

explosionInstance.transform.localScale = new Vector3(1f, 1f, 0.76f);
        }

        Destroy(gameObject);
    }
}