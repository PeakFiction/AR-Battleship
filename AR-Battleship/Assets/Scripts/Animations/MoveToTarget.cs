using UnityEngine;

public class MoveToTarget : MonoBehaviour
{
    public float speed = 2f;

    private Vector3 startPos;
    private Vector3 endPos;
    private float journey = 0f;
    public float spawnHeight = 0.2f;
    public float impactHeight = 0.01f;
    public Transform chosenTile;
    public int hitSegmentIndex;
    public string shipOrientation;
    public string shipType;

    void Start()
    {
        //startPos = new Vector3(9.1f, 25f, 15.6f);
        //endPos   = new Vector3(9.1f, 0f, 15.6f);

        //transform.position = startPos;
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

    void Update()
    {
        journey += Time.deltaTime * speed; // Time.deltaTime = time since last frame, Makes movement frame-rate independent
        journey = Mathf.Clamp01(journey); // Forces journey to stay in [0, 1], 0 = start position, 1 = end position, Prevents overshooting

        transform.position = Vector3.Lerp(startPos, endPos, journey); // When t = 0 ? position = startPos, When t = 1 ? position = endPos, Values in between allows for smooth movement
        if (journey >= 1f) {
            TriggerImpact();
        }
    }

    //void OnTriggerEnter(Collider other)
    void TriggerImpact()
    {
        //TileFlash flash = other.GetComponent<TileFlash>();
        TileFlash flash = chosenTile.GetComponent<TileFlash>();

        if (flash != null)
        {
            flash.FlashThenRed(shipType,
    hitSegmentIndex,
    shipOrientation); // calls method from other script
        }
        Destroy(gameObject); // destroys missile on impact
    }
}