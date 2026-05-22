using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages a set of page GameObjects representing a multi-step guide.
/// Exposes Next / Previous navigation and updates a page counter label.
/// </summary>
public class GuidePageController : MonoBehaviour
{

    /// <summary>
    /// Ordered array of page GameObjects.  Exactly one is active at a time.
    /// Pages are indexed from 0 (first) to pages.Length - 1 (last).
    /// </summary>
    [SerializeField] private GameObject[] pages;

    /// <summary>Button that advances to the next page.  Disabled on the last page.</summary>
    [SerializeField] private Button nextButton;

    /// <summary>Button that returns to the previous page.  Disabled on the first page.</summary>
    [SerializeField] private Button previousButton;

    /// <summary>
    /// Optional TMP label that displays "current / total" page numbers
    /// (e.g. "2 / 5").  Leave unassigned if no counter is needed.
    /// </summary>
    [SerializeField] private TMP_Text pageCounter;

    /// <summary>Zero-based index of the page currently displayed.</summary>
    private int currentPage = 0;

    /// <summary>
    /// Wires button listeners and displays the first page.
    /// </summary>
    private void Start()
    {
        nextButton.onClick.AddListener(NextPage);
        previousButton.onClick.AddListener(PreviousPage);
        ShowPage(0);
    }

    /// <summary>
    /// Advances to the next page if one exists.
    /// The button becomes non-interactable on the final page to prevent
    /// out-of-range access.
    /// </summary>
    private void NextPage()
    {
        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            ShowPage(currentPage);
        }
    }

    /// <summary>
    /// Returns to the previous page if one exists.
    /// The button becomes non-interactable on the first page.
    /// </summary>
    private void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowPage(currentPage);
        }
    }

    /// <summary>
    /// Activates the page at <paramref name="index"/> and deactivates all others.
    /// Updates the Next / Previous button interactability and the page counter.
    /// </summary>
    /// <param name="index">Zero-based page index to display.</param>
    private void ShowPage(int index)
    {
        // Activate only the target page; hide every other page.
        for (int i = 0; i < pages.Length; i++)
            pages[i].SetActive(i == index);

        // Disable navigation buttons at the boundaries.
        previousButton.interactable = index > 0;
        nextButton.interactable     = index < pages.Length - 1;

        // Update the "N / Total" counter if one is assigned.
        if (pageCounter != null)
            pageCounter.text = $"{index + 1} / {pages.Length}";
    }
}
