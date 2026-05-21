using UnityEngine;

public class DropMissileExplosion : MonoBehaviour
{
    public float dropSpeedAlt = 2f;

    public GameObject explosionPrefabAlt;
    public Vector3 explosionOffsetAlt = new Vector3(0f, 0.2f, 0f);

    public Transform targetPointAlt;
    public float spawnHeightAlt = 25f;
    public float impactHeightAlt = 0.2f;

    private Vector3 startPosAlt;
    private Vector3 endPosAlt;
    private float journeyAlt = 0f;
    private bool hasImpactedAlt = false;

    void Start()
    {
        startPosAlt = new Vector3(2.02161f, 25f, 2.02161f);
        endPosAlt   = new Vector3(2.02161f, 0f, 2.02161f);

        transform.position = startPosAlt;
    }

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