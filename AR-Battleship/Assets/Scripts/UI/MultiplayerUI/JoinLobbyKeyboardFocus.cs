using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private void OnEnable()
    {
        if (inputField == null)
            return;

        inputField.onSelect.AddListener(HandleSelect);
        inputField.onDeselect.AddListener(HandleDeselect);
        inputField.onSubmit.AddListener(HandleSubmit);
        inputField.onEndEdit.AddListener(HandleEndEdit);
    }

    private void OnDisable()
    {
        if (inputField == null)
            return;

        inputField.onSelect.RemoveListener(HandleSelect);
        inputField.onDeselect.RemoveListener(HandleDeselect);
        inputField.onSubmit.RemoveListener(HandleSubmit);
        inputField.onEndEdit.RemoveListener(HandleEndEdit);
    }

    private void HandleSelect(string value)
    {
        ShowFocusedInput();
    }

    private void HandleDeselect(string value)
    {
        HideFocusedInput();
    }

    private void HandleSubmit(string value)
    {
        HideFocusedInput();
    }

    private void HandleEndEdit(string value)
    {
        HideFocusedInput();
    }

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

    public void HideFocusedInput()
    {
        if (panelToMove == null)
            return;

        if (dimOverlay != null)
            dimOverlay.gameObject.SetActive(false);

        StartMove(originalAnchoredPosition);
    }

    private void StartMove(Vector2 target)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MovePanel(target));
    }

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