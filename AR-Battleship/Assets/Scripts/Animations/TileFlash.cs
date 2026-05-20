using UnityEngine;
using System.Collections;

public class TileFlash : MonoBehaviour // MonoBehaviour: allows it to Be attached to GameObjects and use Coroutines 
{
    private Renderer rend; // The component that draws the object every visible object has one.
    private Material tileMaterial; // Controls color texter shaders
    private Color originalColor; // Color before event

    //Destroyer Ship sections
    public GameObject shipSegmentPrefab;
    public GameObject DestroyerFrontPrefab;
    public GameObject DestroyerBackPrefab;

    //subamrine sections
    public GameObject SubmarineBackPrefab;
    public GameObject SubmarineMiddlePrefab;
    public GameObject SubmarineFrontPrefab;

    //Cruiser sections
    public GameObject CruiserBackPrefab;
    public GameObject CruiserMiddlePrefab;
    public GameObject CruiserFrontPrefab;

    //Battleship sections
    public GameObject BattleshipBackPrefab;
    public GameObject BattleshipSecondToBackPrefab;
    public GameObject BattleshipSecondToFrontPrefab;
    public GameObject BattleshipFrontPrefab;

    //Carrier sections
    public GameObject CarrierBackPrefab;
    public GameObject CarrierSecondToBackPrefab;
    public GameObject CarrierMiddlePrefab;
    public GameObject CarrierSecondToFrontPrefab;
    public GameObject CarrierFrontPrefab;

    public GameObject firePrefab;

    public Vector3 shipSpawnOffset = new Vector3(0f, 0.01f, 0f);

    private bool alreadySpawned = false;

    void Start()
    {
        rend = GetComponent<Renderer>(); // Finds the Renderer attached to the same GameObject
        tileMaterial = rend.material; // Accesses the material used by the object, creates unique instance so that changing changes no other tiles
        originalColor = tileMaterial.color; // Stores starting color
    }

    public void FlashThenRed(string shipType,
    int segmentIndex,
    string orientation) // Allows coroutine to be triggered from other scripts
    {
        StartCoroutine(FlashRoutine(shipType,
    segmentIndex,
    orientation));
    }

    private IEnumerator FlashRoutine(string shipType,
    int segmentIndex,
    string orientation)
    {
        tileMaterial.color = Color.white;   // flash colour
        yield return new WaitForSeconds(0.08f); //waits 0.08 seconds

        tileMaterial.color = Color.red;     // final hit colour

        SpawnShipSegment(shipType,
    segmentIndex,
    orientation);  // Spawn ship seg function
    }

    void SpawnShipSegment(string shipType,
    int segmentIndex,
    string orientation)
    {
        if (alreadySpawned) return;

        alreadySpawned = true;

        GameObject prefabToSpawn = null;

        switch (shipType)
        {
            // DESTROYER
            case "Destroyer":

                if (segmentIndex == 0)
                    prefabToSpawn = DestroyerFrontPrefab;
                else
                    prefabToSpawn = DestroyerBackPrefab;

                break;

            // SUBMARINE
            case "Submarine":

                if (segmentIndex == 0)
                    prefabToSpawn = SubmarineFrontPrefab;
                else if (segmentIndex == 1)
                    prefabToSpawn = SubmarineMiddlePrefab;
                else
                    prefabToSpawn = SubmarineBackPrefab;

                break;

            // CRUISER
            case "Cruiser":

                if (segmentIndex == 0)
                    prefabToSpawn = CruiserFrontPrefab;
                else if (segmentIndex == 1)
                    prefabToSpawn = CruiserMiddlePrefab;
                else
                    prefabToSpawn = CruiserBackPrefab;

                break;

            // BATTLESHIP
            case "Battleship":

                if (segmentIndex == 0)
                    prefabToSpawn = BattleshipFrontPrefab;
                else if (segmentIndex == 1)
                    prefabToSpawn = BattleshipSecondToFrontPrefab;
                else if (segmentIndex == 2)
                    prefabToSpawn = BattleshipSecondToBackPrefab;
                else
                    prefabToSpawn = BattleshipBackPrefab;

                break;

            // CARRIER
            case "Carrier":

                if (segmentIndex == 0)
                    prefabToSpawn = CarrierFrontPrefab;
                else if (segmentIndex == 1)
                    prefabToSpawn = CarrierSecondToFrontPrefab;
                else if (segmentIndex == 2)
                    prefabToSpawn = CarrierMiddlePrefab;
                else if (segmentIndex == 3)
                    prefabToSpawn = CarrierSecondToBackPrefab;
                else
                    prefabToSpawn = CarrierBackPrefab;

                break;
        }

        if (prefabToSpawn != null)
        {
            GameObject segment =
                Instantiate(prefabToSpawn);

            segment.transform.SetParent(transform);

            segment.transform.localPosition =
                new Vector3(0f, 0.08f, -0.01f);

            // ROTATION BASED ON SHIP ORIENTATION
            if (orientation == Orientation.Horizontal)
            {
                segment.transform.localRotation =
                    Quaternion.Euler(-90f, 0f, 0f);
            }
            else
            {
                segment.transform.localRotation =
                    Quaternion.Euler(-90f, 90f, 0f);
            }

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