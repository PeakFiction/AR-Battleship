using UnityEngine;
using System.Collections;

public class MissTileMarkerAlt : MonoBehaviour
{
    public Color missFlashColorAlt = new Color(1f, 1f, 0.7f); // flash color
    public Color missFinalColorAlt = Color.grey; // final grey color
    public float missFlashDurationAlt = 0.08f; // time for flash

    private Renderer missRendererAlt; // lets you access the material
    private Material missMaterialAlt; // lets you change color
    private bool missAlreadyMarkedAlt = false; // boolean to track if missile hit yet or not 

    void Awake()
    {
        missRendererAlt = GetComponent<Renderer>();
        if (missRendererAlt != null)
        {
            missRendererAlt.material = new Material(missRendererAlt.material);
            missMaterialAlt = missRendererAlt.material;
        }
    }

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

    private IEnumerator RunMissVisualAlt()
    { // flashs tile then changes color
        missMaterialAlt.color = missFlashColorAlt;
        yield return new WaitForSeconds(missFlashDurationAlt);
        missMaterialAlt.color = missFinalColorAlt;
    }
}