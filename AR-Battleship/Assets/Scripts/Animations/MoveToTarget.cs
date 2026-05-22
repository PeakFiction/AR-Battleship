// -----------------------------------------------------------------------------
// File: MoveToTarget.cs
// Purpose: Contains gameplay/visual behaviour logic for the AR Battleship project.
// Comments were added automatically without modifying executable code.
// -----------------------------------------------------------------------------

using UnityEngine;

public class MoveToTarget : MonoBehaviour
{
    // Public field exposed in the Unity Inspector.
    public float speed = 2f;

    private Vector3 startPos;
    private Vector3 endPos;
    private float journey = 0f;
    // Public field exposed in the Unity Inspector.
    public float spawnHeight = 0.2f;
    // Public field exposed in the Unity Inspector.
    public float impactHeight = 0.01f;
    // Public field exposed in the Unity Inspector.
    public Transform chosenTile;
    // Public field exposed in the Unity Inspector.
    public int hitSegmentIndex;
    // Public field exposed in the Unity Inspector.
    public string shipOrientation;
    // Public field exposed in the Unity Inspector.
    public string shipType;

    // Unity Start method called before the first frame update.
    void Start()
    {
        if (chosenTile != null)
        {
            startPos =
                chosenTile.position +
                new Vector3(0f, spawnHeight, 0f);

            endPos =
                chosenTile.position +
                new Vector3(0f, impactHeight, 0f);

            transform.position = startPos;
        }
    }

    // Unity Update method called once per frame.
    void Update()
    {
        journey += Time.deltaTime * speed; // Time.deltaTime = time since last frame, Makes movement frame-rate independent
        journey = Mathf.Clamp01(journey); // Forces journey to stay in [0, 1], 0 = start position, 1 = end position, Prevents overshooting

        transform.position = Vector3.Lerp(startPos, endPos, journey); // When t = 0 ? position = startPos, When t = 1 ? position = endPos, Values in between allows for smooth movement
        if (journey >= 1f) {
            TriggerImpact();
        }
    }

    void TriggerImpact()
    {
        TileFlash flash = chosenTile.GetComponent<TileFlash>();

        if (flash != null)
        {
            flash.FlashThenRed(shipType, hitSegmentIndex, shipOrientation); // calls method from other script
        }
        Destroy(gameObject); // destroys missile on impact
    }
}