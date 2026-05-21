using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GuidePageController : MonoBehaviour
{
    [SerializeField] private GameObject[] pages;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private TMP_Text pageCounter;

    private int currentPage = 0;

    private void Start()
    {
        nextButton.onClick.AddListener(NextPage);
        previousButton.onClick.AddListener(PreviousPage);
        ShowPage(0);
    }

    private void NextPage()
    {
        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            ShowPage(currentPage);
        }
    }

    private void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowPage(currentPage);
        }
    }

    private void ShowPage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == index);
        }

        previousButton.interactable = index > 0;
        nextButton.interactable = index < pages.Length - 1;

        if (pageCounter != null)
            pageCounter.text = $"{index + 1} / {pages.Length}";
    }
}