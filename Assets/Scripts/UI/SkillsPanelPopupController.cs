using System.Collections;
using TMPro;
using UnityEngine;

public sealed class SkillsPanelPopupController
{
    private readonly GameObject popupRoot;
    private readonly CanvasGroup popupCanvasGroup;
    private readonly RectTransform popupTransform;
    private readonly TextMeshProUGUI popupIconText;
    private readonly TextMeshProUGUI popupBodyText;

    private Coroutine popupCoroutine;
    private Vector2 popupBasePosition;
    private bool hasPopupBasePosition;

    public SkillsPanelPopupController(
        GameObject popupRoot,
        CanvasGroup popupCanvasGroup,
        RectTransform popupTransform,
        TextMeshProUGUI popupIconText,
        TextMeshProUGUI popupBodyText)
    {
        this.popupRoot = popupRoot;
        this.popupCanvasGroup = popupCanvasGroup;
        this.popupTransform = popupTransform;
        this.popupIconText = popupIconText;
        this.popupBodyText = popupBodyText;
    }

    public void Show(MonoBehaviour owner, SkillsGainPopupViewData popupView)
    {
        if (owner == null || popupRoot == null)
        {
            return;
        }

        CacheBasePosition();

        if (popupIconText != null)
        {
            popupIconText.text = popupView.IconText;
            UiIconViewUtility.ApplyIconToTextSlot(popupIconText, popupView.IconText);
        }

        if (popupBodyText != null)
        {
            popupBodyText.text = popupView.MessageText;
        }

        if (popupCoroutine != null)
        {
            owner.StopCoroutine(popupCoroutine);
        }

        popupCoroutine = owner.StartCoroutine(PlayRoutine());
    }

    public void HideImmediate(MonoBehaviour owner = null)
    {
        if (popupCoroutine != null && owner != null)
        {
            owner.StopCoroutine(popupCoroutine);
        }

        popupCoroutine = null;
        CacheBasePosition();

        if (popupTransform != null && hasPopupBasePosition)
        {
            popupTransform.anchoredPosition = popupBasePosition;
        }

        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = 0f;
        }

        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }

    private IEnumerator PlayRoutine()
    {
        const float duration = 1.2f;
        const float riseDistance = 26f;

        if (popupRoot != null)
        {
            popupRoot.SetActive(true);
        }

        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = 1f;
        }

        CacheBasePosition();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - normalized, 2f);

            if (popupTransform != null)
            {
                popupTransform.anchoredPosition = popupBasePosition + Vector2.up * (riseDistance * eased);
            }

            if (popupCanvasGroup != null)
            {
                float alpha = normalized < 0.2f
                    ? Mathf.Lerp(0f, 1f, normalized / 0.2f)
                    : Mathf.Lerp(1f, 0f, (normalized - 0.2f) / 0.8f);
                popupCanvasGroup.alpha = alpha;
            }

            yield return null;
        }

        if (popupTransform != null)
        {
            popupTransform.anchoredPosition = popupBasePosition;
        }

        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = 0f;
        }

        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }

        popupCoroutine = null;
    }

    private void CacheBasePosition()
    {
        if (hasPopupBasePosition || popupTransform == null)
        {
            return;
        }

        popupBasePosition = popupTransform.anchoredPosition;
        hasPopupBasePosition = true;
    }
}
