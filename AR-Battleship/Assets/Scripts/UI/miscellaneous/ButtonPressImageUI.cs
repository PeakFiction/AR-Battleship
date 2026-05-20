using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChildClickedStateButton : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    [Header("State Objects")]
    [Tooltip("The normal button visual. Usually the child named Button.")]
    [SerializeField] private GameObject normalObject;

    [Tooltip("The clicked/pressed visual. Usually the child named StartButtonClicked or BackButtonClicked.")]
    [SerializeField] private GameObject clickedObject;

    [Header("Behavior")]
    [SerializeField] private bool holdClickedState = false;
    [SerializeField] private bool resetOnPointerExit = true;

    [Header("Optional Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickChime;
    [SerializeField, Range(0f, 1f)] private float chimeVolume = 0.8f;

    private bool isClicked;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        ApplyState(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PlayChime();

        if (holdClickedState)
            SetClicked(!isClicked);
        else
            SetClicked(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (holdClickedState)
            return;

        SetClicked(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (holdClickedState || !resetOnPointerExit)
            return;

        SetClicked(false);
    }

    public void SetClicked(bool clicked)
    {
        isClicked = clicked;
        ApplyState(clicked);
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

    private void ApplyState(bool clicked)
    {
        if (normalObject != null)
            normalObject.SetActive(!clicked);

        if (clickedObject != null)
            clickedObject.SetActive(clicked);
    }

    private void PlayChime()
    {
        if (audioSource != null && clickChime != null)
            audioSource.PlayOneShot(clickChime, chimeVolume);
    }
}