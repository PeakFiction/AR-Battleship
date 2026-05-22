// -----------------------------------------------------------------------------
// File: MissTileMarkerAlt.cs
// Purpose: Contains gameplay/visual behaviour logic for the AR Battleship project.
// Comments were added automatically without modifying executable code.
// -----------------------------------------------------------------------------

using UnityEngine;
using System.Collections;

public class MissTileMarkerAlt : MonoBehaviour
{
    // Method responsible for: Color.
    // Public field exposed in the Unity Inspector.
    public Color missFlashColorAlt = new Color(1f, 1f, 0.7f); // flash color
    // Public field exposed in the Unity Inspector.
    public Color missFinalColorAlt = Color.grey; // final grey color
    // Public field exposed in the Unity Inspector.
    public float missFlashDurationAlt = 0.08f; // time for flash

    private Renderer missRendererAlt; // lets you access the material
    private Material missMaterialAlt; // lets you change color
    private bool missAlreadyMarkedAlt = false; // boolean to track if missile hit yet or not 

    // Helper method to check if a prefab was assigned
    void Awake()
    {
        missRendererAlt = GetComponent<Renderer>();
        if (missRendererAlt != null)
        {
            missRendererAlt.material = new Material(missRendererAlt.material);
            missMaterialAlt = missRendererAlt.material;
        }
    }

    // Unity Start method called before the first frame update.
    void Start()
    {
        // Fallback if Awake didn't run
        if (missMaterialAlt == null && missRendererAlt == null)
        {
            missRendererAlt = GetComponent<Renderer>();
            if (missRendererAlt != null)
            {
                missRendererAlt.material = new Material(missRendererAlt.material);
                missMaterialAlt = missRendererAlt.material;
            }
        }
        if (missMaterialAlt == null && missRendererAlt != null)
        {
            missMaterialAlt = missRendererAlt.material;
        }
    }

    public void TriggerMissVisualAlt()
    {
        if (missAlreadyMarkedAlt) return;
        missAlreadyMarkedAlt = true; // makes sure tile hasnt been hit before and can only trigger once

        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        StartCoroutine(RunMissVisualAlt()); // starts routine
    }

    // Coroutine used for timed gameplay or animation behaviour.
    private IEnumerator RunMissVisualAlt()
    { // flashs tile then changes color
        missMaterialAlt.color = missFlashColorAlt;
        yield return new WaitForSeconds(missFlashDurationAlt);
        missMaterialAlt.color = missFinalColorAlt;
    }
}