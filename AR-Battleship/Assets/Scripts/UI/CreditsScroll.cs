using UnityEngine;
using TMPro;

/// <summary>
/// Scrolls a RectTransform upward at a constant speed, looping back to
/// the start position once the end position is reached.
/// Attach to any scene GameObject; assign the credits text's RectTransform.
/// </summary>
public class CreditsScroll : MonoBehaviour
{

    /// <summary>
    /// The RectTransform of the credits text block.
    /// Only the anchoredPosition.y is modified; text content is untouched.
    /// </summary>
    [SerializeField] private RectTransform creditsText;

    /// <summary>
    /// Speed at which the credits scroll upward (units per second, canvas space).
    /// Increase to make the credits roll faster.
    /// </summary>
    [SerializeField] private float scrollSpeed = 50f;

    /// <summary>
    /// The anchoredPosition.y value where scrolling begins.
    /// Set to a negative value so the first credit line starts below the screen.
    /// </summary>
    [SerializeField] private float startY = -500f;

    /// <summary>
    /// The anchoredPosition.y value at which the loop resets.
    /// Should be greater than or equal to the height of the credits content
    /// so all lines have fully scrolled past before resetting.
    /// </summary>
    [SerializeField] private float endY = 3000f;

    /// <summary>
    /// Resets the credits text to the starting position so it begins scrolling
    /// from below the screen regardless of the Inspector or prefab value.
    /// </summary>
    private void Start()
    {
        if (creditsText != null)
        {
            Vector2 pos = creditsText.anchoredPosition;
            pos.y = startY;
            creditsText.anchoredPosition = pos;
        }
    }

    /// <summary>
    /// Moves the credits text upward each frame.  When the text reaches
    /// <see cref="endY"/>, it wraps back to <see cref="startY"/> for looping.
    /// </summary>
    private void Update()
    {
        if (creditsText == null) return;

        Vector2 pos = creditsText.anchoredPosition;
        pos.y += scrollSpeed * Time.deltaTime; // Move upward (positive Y)
        creditsText.anchoredPosition = pos;

        // Loop back to the start once the last credit line has scrolled off.
        if (pos.y >= endY)
        {
            pos.y = startY;
            creditsText.anchoredPosition = pos;
        }
    }
}
