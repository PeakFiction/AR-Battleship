using UnityEngine;
using TMPro;

public class CreditsScroll : MonoBehaviour
{
    [SerializeField] private RectTransform creditsText;
    [SerializeField] private float scrollSpeed = 50f;
    [SerializeField] private float startY = -500f;
    [SerializeField] private float endY = 3000f;
    
    private void Start()
    {
        if (creditsText != null)
        {
            Vector2 pos = creditsText.anchoredPosition;
            pos.y = startY;
            creditsText.anchoredPosition = pos;
        }
    }

    private void Update()
    {
        if (creditsText != null)
        {
            Vector2 pos = creditsText.anchoredPosition;
            pos.y += scrollSpeed * Time.deltaTime;
            creditsText.anchoredPosition = pos;

            if (pos.y >= endY)
            {
                // Loop or return to menu
                pos.y = startY;
                creditsText.anchoredPosition = pos;
            }
        }
    }
}