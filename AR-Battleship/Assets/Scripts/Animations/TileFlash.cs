using System.Collections;
using System.Diagnostics;
using UnityEngine;

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

        SpawnShipSegment(shipType, segmentIndex, orientation);
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
                UnityEngine.Debug.Log($"HandlePlayerShot in switch shipType: {shipType}");
                if (segmentIndex == 0)
                    prefabToSpawn = DestroyerFrontPrefab;
                else
                    prefabToSpawn = DestroyerBackPrefab;

                break;

            // SUBMARINE
            case "Submarine":
                UnityEngine.Debug.Log($"HandlePlayerShot in switch shipType: {shipType}");
                if (segmentIndex == 0)
                    prefabToSpawn = SubmarineFrontPrefab;
                else if (segmentIndex == 1)
                    prefabToSpawn = SubmarineMiddlePrefab;
                else
                    prefabToSpawn = SubmarineBackPrefab;

                break;

            // CRUISER
            case "Cruiser":
                UnityEngine.Debug.Log($"HandlePlayerShot in switch shipType: {shipType}");
                if (segmentIndex == 0)
                    prefabToSpawn = CruiserFrontPrefab;
                else if (segmentIndex == 1)
                    prefabToSpawn = CruiserMiddlePrefab;
                else
                    prefabToSpawn = CruiserBackPrefab;

                break;

            // BATTLESHIP
            case "Battleship":
                UnityEngine.Debug.Log($"HandlePlayerShot in switch shipType: {shipType}");
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
                UnityEngine.Debug.Log($"HandlePlayerShot in switch shipType: {shipType}");
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
        UnityEngine.Debug.Log($"Prefab chosen: {prefabToSpawn}");
        if (prefabToSpawn == null)
        {
            UnityEngine.Debug.Log("PrefabToSpawn is NULL");
            return;
        }
        if (prefabToSpawn != null)
        {
            //UnityEngine.Debug.Log("PrefabToSpawn is NULL");
            GameObject segment =
                Instantiate(prefabToSpawn);

            segment.transform.SetParent(transform);

            // ROTATION BASED ON SHIP ORIENTATION
            if (orientation == "Horizontal")
            {
                if (shipType == "Carrier")
                {
                    float[] shipXSegCoords = new float[] { -0.01f, -0.012f, -0.015f, -0.018f, 0.005f };
                    float[] shipZSegCoords = new float[] { 0.0019f, 0f, 0f, 0f, 0f };
                    float[] shipYRot = new float[] {-90f, -90f, -90f, -90f, 90f};
                    segment.transform.localRotation = Quaternion.Euler(-90f, shipYRot[segmentIndex], 0f);
                    segment.transform.localPosition = new Vector3(shipXSegCoords[segmentIndex], 0.08f, shipZSegCoords[segmentIndex]);
                }
                else if (shipType == "Battleship")
                {
                    float[] shipXSegCoords = new float[] {0.01f, 0.01f, 0.01f, 0.011f};
                    segment.transform.localRotation = Quaternion.Euler(-90f, 0, 90f);
                    segment.transform.localPosition = new Vector3(shipXSegCoords[segmentIndex], 0.08f, 0);
                }
                else if (shipType == "Cruiser")
                {
                    float[] shipXSegCoords = new float[] {0.01f, 0.011f, 0.011f};
                    segment.transform.localRotation = Quaternion.Euler(-90f, 90f, 0f);
                    segment.transform.localPosition = new Vector3(shipXSegCoords[segmentIndex], 0.08f, 0f);
                }
                else if (shipType == "Submarine")
                {
                    float[] shipXSegCoords = new float[] {0.006f, -0.007f, 0.01f};
                    float[] shipYRot = new float[] {-90f, 90f, -90f};
                    segment.transform.localRotation = Quaternion.Euler(-90f, shipYRot[segmentIndex], 0f);
                    segment.transform.localPosition = new Vector3(shipXSegCoords[segmentIndex], 0.08f, 0f);
                    segment.transform.localScale = new Vector3(0.009f, 0.014f, 0.1f);
                }
                else if (shipType == "Destroyer")
                {
                    float[] shipXSegCoords = new float[] {-0.01f, 0.01f };
                    segment.transform.localRotation = Quaternion.Euler(-90f, 90f, 0f);
                    segment.transform.localPosition = new Vector3(shipXSegCoords[segmentIndex], 0.08f, 0f);
                }
            }
            else
            {
                if (shipType == "Carrier")
                {
                    float[] shipZSegCoords = new float[] { -0.015f, -0.017f, -0.02f, -0.023f, 0f };
                    float[] shipYRot = new float[] { 90f, 90f, 90f, 90f, -90f };
                    segment.transform.localRotation = Quaternion.Euler(-90f, shipYRot[segmentIndex], 90f);
                    segment.transform.localPosition = new Vector3(0, 0.08f, shipZSegCoords[segmentIndex]);
                }
                else if (shipType == "Battleship") 
                {
                    float[] shipZSegCoords = new float[] {-0.01f, -0.015f, -0.013f, -0.0129f};
                    segment.transform.localRotation = Quaternion.Euler(-90f, 90, 90f);
                    segment.transform.localPosition = new Vector3(0, 0.08f, shipZSegCoords[segmentIndex]);
                }
                else if (shipType == "Cruiser")
                {
                    float[] shipZSegCoords = new float[] {0f, 0.003f, 0.006f};
                    segment.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                    segment.transform.localPosition = new Vector3(0, 0.08f, shipZSegCoords[segmentIndex]);
                }
                else if (shipType == "Submarine")
                {
                    float[] shipZSegCoords = new float[] {0.005f, 0.006f, 0.005f};
                    segment.transform.localRotation = Quaternion.Euler(-90f, 90, 90f);
                    segment.transform.localPosition = new Vector3(0f, 0.08f, shipZSegCoords[segmentIndex]);
                    segment.transform.localScale = new Vector3(0.009f, 0.014f, 0.1f);
                }
                else if (shipType == "Destroyer")
                {
                    float[] shipZSegCoords = new float[] {-0.01f, 0.01f};
                    segment.transform.localRotation = Quaternion.Euler(-90f, -90f, 90f);
                    segment.transform.localPosition = new Vector3(0, 0.08f, shipZSegCoords[segmentIndex]);
                }

            }
            if (shipType != "Submarine")
            {
                segment.transform.localScale = new Vector3(0.014f, 0.014f, 0.1f);
            }
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