using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CanvasUIButtonFeedback : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    [Header("Canvas UI Parts")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image indicatorSquare;

    [Header("Clicked Background")]
    [SerializeField] private GameObject clickedBackground;
    [SerializeField] private bool showBackgroundOnlyWhilePressed = true;

    [Header("Visual State")]
    [SerializeField] private bool holdClickedState;
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color clickedTextColor = Color.black;
    [SerializeField] private Color normalSquareColor = Color.white;
    [SerializeField] private Color clickedSquareColor = Color.black;

    [Header("Motion")]
    [SerializeField] private float clickedScale = 0.98f;
    [SerializeField] private float animationSpeed = 18f;
    [SerializeField] private float releaseDelay = 0.08f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickChime;
    [SerializeField, Range(0f, 1f)] private float chimeVolume = 0.8f;

    private Button button;
    private RectTransform rectTransform;
    private Vector3 normalScale;
    private Coroutine visualRoutine;
    private Coroutine releaseRoutine;
    private bool isClicked;

    private void Awake()
    {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        normalScale = rectTransform.localScale;

        if (label == null)
            label = GetComponentInChildren<TMP_Text>(true);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        ApplyVisual(false, instant: true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable)
            return;

        PlayChime();

        if (holdClickedState)
        {
            SetClicked(!isClicked);
        }
        else
        {
            ApplyVisual(true);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!button.interactable || holdClickedState)
            return;

        StartRelease();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!button.interactable || holdClickedState)
            return;

        StartRelease();
    }

    public void SetClicked(bool clicked)
    {
        isClicked = clicked;
        ApplyVisual(clicked);
    }

    public void ToggleClicked()
    {
        SetClicked(!isClicked);
        PlayChime();
    }

    public void ResetClicked()
    {
        SetClicked(false);
    }

    private void StartRelease()
    {
        if (releaseRoutine != null)
            StopCoroutine(releaseRoutine);

        releaseRoutine = StartCoroutine(ReleaseAfterDelay());
    }

    private IEnumerator ReleaseAfterDelay()
    {
        yield return new WaitForSecondsRealtime(releaseDelay);
        ApplyVisual(false);
    }

    private void ApplyVisual(bool clicked, bool instant = false)
    {
        if (clickedBackground != null)
            clickedBackground.SetActive(clicked);

        if (label != null)
            label.color = clicked ? clickedTextColor : normalTextColor;

        if (indicatorSquare != null)
            indicatorSquare.color = clicked ? clickedSquareColor : normalSquareColor;

        Vector3 targetScale = clicked ? normalScale * clickedScale : normalScale;

        if (instant)
        {
            rectTransform.localScale = targetScale;
            return;
        }

        if (visualRoutine != null)
            StopCoroutine(visualRoutine);

        visualRoutine = StartCoroutine(AnimateScale(targetScale));
    }

    private IEnumerator AnimateScale(Vector3 targetScale)
    {
        while (Vector3.Distance(rectTransform.localScale, targetScale) > 0.001f)
        {
            rectTransform.localScale = Vector3.Lerp(
                rectTransform.localScale,
                targetScale,
                Time.unscaledDeltaTime * animationSpeed
            );

            yield return null;
        }

        rectTransform.localScale = targetScale;
    }

    private void PlayChime()
    {
        if (audioSource != null && clickChime != null)
            audioSource.PlayOneShot(clickChime, chimeVolume);
    }
}