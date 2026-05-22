using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Moves the join-code input panel upward while the mobile keyboard is open so the field remains visible on smaller screens.
/// </summary>
public class MobileKeyboardInputLift : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private RectTransform panelToMove;
    [SerializeField] private Image dimOverlay;

    [Header("Positioning")]
    [SerializeField] private float liftedY = 350f;
    [SerializeField] private float animationSpeed = 12f;

    private Vector2 originalAnchoredPosition;
    private Coroutine moveRoutine;

    /// <summary>
    /// Caches input and panel references before focus events begin firing.
    /// </summary>
    private void Awake()
    {
        if (inputField == null)
            inputField = GetComponentInChildren<TMP_InputField>();

        if (panelToMove == null && inputField != null)
            panelToMove = inputField.GetComponent<RectTransform>();

        if (panelToMove != null)
            originalAnchoredPosition = panelToMove.anchoredPosition;

        if (dimOverlay != null)
        {
            dimOverlay.gameObject.SetActive(false);
            dimOverlay.color = new Color(0f, 0f, 0f, 0.65f);
        }
    }

    /// <summary>
    /// Registers input-field callbacks that detect when the player starts or finishes entering a code.
    /// </summary>
    private void OnEnable()
    {
        if (inputField == null)
            return;

        inputField.onSelect.AddListener(HandleSelect);
        inputField.onDeselect.AddListener(HandleDeselect);
        inputField.onSubmit.AddListener(HandleSubmit);
        inputField.onEndEdit.AddListener(HandleEndEdit);
    }

    /// <summary>
    /// Removes input-field callbacks so disabled UI objects do not keep receiving events.
    /// </summary>
    private void OnDisable()
    {
        if (inputField == null)
            return;

        inputField.onSelect.RemoveListener(HandleSelect);
        inputField.onDeselect.RemoveListener(HandleDeselect);
        inputField.onSubmit.RemoveListener(HandleSubmit);
        inputField.onEndEdit.RemoveListener(HandleEndEdit);
    }

    /// <summary>
    /// Lifts the input panel when the field is selected.
    /// </summary>
    private void HandleSelect(string value)
    {
        ShowFocusedInput();
    }

    /// <summary>
    /// Restores the input panel when the field loses focus.
    /// </summary>
    private void HandleDeselect(string value)
    {
        HideFocusedInput();
    }

    /// <summary>
    /// Restores the input panel after the player submits the field.
    /// </summary>
    private void HandleSubmit(string value)
    {
        HideFocusedInput();
    }

    /// <summary>
    /// Restores the input panel after text editing ends.
    /// </summary>
    private void HandleEndEdit(string value)
    {
        HideFocusedInput();
    }

    /// <summary>
    /// Displays the dim overlay and animates the panel to the lifted keyboard-safe position.
    /// </summary>
    public void ShowFocusedInput()
    {
        if (panelToMove == null)
            return;

        if (dimOverlay != null)
            dimOverlay.gameObject.SetActive(true);

        Vector2 target = originalAnchoredPosition;
        target.y = liftedY;

        StartMove(target);
    }

    /// <summary>
    /// Hides the dim overlay and animates the panel back to its original position.
    /// </summary>
    public void HideFocusedInput()
    {
        if (panelToMove == null)
            return;

        if (dimOverlay != null)
            dimOverlay.gameObject.SetActive(false);

        StartMove(originalAnchoredPosition);
    }

    /// <summary>
    /// Stops any previous movement animation before starting a new one toward the target position.
    /// </summary>
    private void StartMove(Vector2 target)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MovePanel(target));
    }

    /// <summary>
    /// Smoothly interpolates the panel position until it reaches the requested target.
    /// </summary>
    private IEnumerator MovePanel(Vector2 target)
    {
        while (Vector2.Distance(panelToMove.anchoredPosition, target) > 0.5f)
        {
            panelToMove.anchoredPosition = Vector2.Lerp(
                panelToMove.anchoredPosition,
                target,
                Time.unscaledDeltaTime * animationSpeed
            );

            yield return null;
        }

        panelToMove.anchoredPosition = target;
    }
}
