// -----------------------------------------------------------------------------
// File: MissileDropMissAlt.cs
// Purpose: Contains gameplay/visual behaviour logic for the AR Battleship project.
// Comments were added automatically without modifying executable code.
// -----------------------------------------------------------------------------

using System.Diagnostics;
using UnityEngine;

public class MissileDropMissAlt : MonoBehaviour
{
    // Public field exposed in the Unity Inspector.
    public float missDropSpeedAlt = 2f;
    // Public field exposed in the Unity Inspector.
    public Transform missTargetTileAlt;
    // Public field exposed in the Unity Inspector.
    public float missSpawnHeightAlt = 0.3f;
    // Public field exposed in the Unity Inspector.
    public float missImpactHeightAlt = 0.01f;

    private Vector3 missStartPointAlt;
    private Vector3 missEndPointAlt;
    private float missJourneyAlt = 0f;
    private bool missImpactDoneAlt = false;

    // Unity Start method called before the first frame update.
    void Start()
    {
        if (missTargetTileAlt != null) //This will check if the target exists
        {
            missStartPointAlt = missTargetTileAlt.position + new Vector3(0f, missSpawnHeightAlt, 0f);
            missEndPointAlt = missTargetTileAlt.position + new Vector3(0f, missImpactHeightAlt, 0f);
            transform.position = missStartPointAlt;
        }
    }
    // Unity Update method called once per frame.
    void Update()
    {
        UnityEngine.Debug.Log(missTargetTileAlt);
        if (missImpactDoneAlt) return;
        if (missTargetTileAlt == null) return; //checks for target or if already hit

        missJourneyAlt += Time.deltaTime * missDropSpeedAlt;
        missJourneyAlt = Mathf.Clamp01(missJourneyAlt);

        transform.position = Vector3.Lerp(missStartPointAlt, missEndPointAlt, missJourneyAlt);

        if (missJourneyAlt >= 1f) // Check if it hit the target
        {
            TriggerMissImpactAlt(); // calls method responsible for what happens after hit
        }
    }

    void TriggerMissImpactAlt()
    {
        if (missImpactDoneAlt) return;
        missImpactDoneAlt = true;

        MissTileMarkerAlt missMarkerAlt = missTargetTileAlt.GetComponent<MissTileMarkerAlt>(); //Checks for the tile and checks if it has script

        if (missMarkerAlt != null) 
        { 
            missMarkerAlt.TriggerMissVisualAlt(); // if it exists start the miss tile effect
        }

        Destroy(gameObject); // destroy object
    }
}