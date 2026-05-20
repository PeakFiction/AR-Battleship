using UnityEngine;
using System.Collections;

public class TileFlash : MonoBehaviour // MonoBehaviour: allows it to Be attached to GameObjects and use Coroutines 
{
    private Renderer rend; // The component that draws the object every visible object has one.
    private Material tileMaterial; // Controls color texter shaders
    private Color originalColor; // Color before event

    public GameObject shipSegmentPrefab;
    public GameObject firePrefab;

    public Vector3 shipSpawnOffset = new Vector3(0f, 0.01f, 0f);

    private bool alreadySpawned = false;

    void Start()
    {
        rend = GetComponent<Renderer>(); // Finds the Renderer attached to the same GameObject
        tileMaterial = rend.material; // Accesses the material used by the object, creates unique instance so that changing changes no other tiles
        originalColor = tileMaterial.color; // Stores starting color
    }

    public void FlashThenRed() // Allows coroutine to be triggered from other scripts
    {
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine() 
    {
        tileMaterial.color = Color.white;   // flash colour
        yield return new WaitForSeconds(0.08f); //waits 0.08 seconds

        tileMaterial.color = Color.red;     // final hit colour

        SpawnShipSegment();  // Spawn ship seg function
    }

    //void SpawnShipSegment()
    //{
    //    if (alreadySpawned) return;
    //    alreadySpawned = true;

    //    if (shipSegmentPrefab != null)
    //    {
    //        GameObject segment = Instantiate(
    //            shipSegmentPrefab,
    //            shipSpawnOffset,
    //            Quaternion.Euler(-90f, 0f, 0f)
    //        );
    //    }
    //}

    void SpawnShipSegment()
    {
        if (alreadySpawned) return;

        alreadySpawned = true;

        if (shipSegmentPrefab != null)
        {
            GameObject segment =
                Instantiate(shipSegmentPrefab);

            segment.transform.SetParent(transform);

            segment.transform.localPosition =
                new Vector3(0f, 0.08f, -0.01f);

            segment.transform.localRotation =
                Quaternion.Euler(-90f, 0f, 0f);

            segment.transform.localScale =
                new Vector3(0.014f, 0.014f, 0.1f);
            SpawnFire();
        }
    }

    void SpawnFire()
    {
        if (firePrefab != null)
        {
            GameObject fire =
                Instantiate(firePrefab);

            fire.transform.SetParent(transform);

            fire.transform.localPosition =
                new Vector3(0f, 0.16f, 0.0001f);

            fire.transform.localRotation =
                Quaternion.Euler(180f, 0f, 0f);

            fire.transform.localScale =
                new Vector3(0.01f, 0.01f, 0.006f);
        }
    }
}