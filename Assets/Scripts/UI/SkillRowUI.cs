using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillRowUI : MonoBehaviour
{
    public Image backgroundImage;
    public Image colorMarkerImage;
    public TextMeshProUGUI iconText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI percentText;
    public Button selectButton;
    public Button removeButton;
    public TextMeshProUGUI selectButtonText;
    public TextMeshProUGUI removeButtonText;

    private Color defaultBackgroundColor;
    private Color highlightBackgroundColor;
    private Vector3 defaultScale = Vector3.one;
    private Coroutine highlightCoroutine;
    private HorizontalLayoutGroup rowLayoutGroup;

    private void Awake()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        rowLayoutGroup = GetComponent<HorizontalLayoutGroup>();
        EnsureReadableLayout();

        if (backgroundImage != null)
        {
            defaultBackgroundColor = backgroundImage.color;
        }

        defaultScale = transform.localScale;
    }

    public void Bind(SkillEntry skill, Color markerColor, bool isSelected, Action<string> onSelect, Action<string> onRemove)
    {
        if (skill == null)
        {
            return;
        }

        EnsureReadableLayout();

        if (backgroundImage != null)
        {
            backgroundImage.color = defaultBackgroundColor;
        }

        transform.localScale = defaultScale;
        highlightBackgroundColor = Color.Lerp(defaultBackgroundColor, markerColor, 0.35f);

        if (colorMarkerImage != null)
        {
            colorMarkerImage.color = markerColor;
        }

        if (iconText != null)
        {
            iconText.text = string.IsNullOrEmpty(skill.icon) ? "?" : skill.icon;
            iconText.fontSize = skill.icon != null && skill.icon.Length > 2 ? 14f : 24f;
            iconText.textWrappingMode = TextWrappingModes.NoWrap;
            iconText.overflowMode = TextOverflowModes.Overflow;
        }

        if (nameText != null)
        {
            nameText.text = skill.isGolden ? $"{skill.name} *" : skill.name;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (percentText != null)
        {
            string progressText = skill.percent > 0f && skill.percent < 0.01f
                ? "<0.01%"
                : $"{skill.percent:0.##}%";
            string minutesText = skill.totalFocusMinutes > 0 ? $" | {skill.totalFocusMinutes}m" : string.Empty;
            string goldenText = skill.isGolden ? " | Golden" : string.Empty;
            percentText.text = progressText + minutesText + goldenText;
            percentText.textWrappingMode = TextWrappingModes.NoWrap;
            percentText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (selectButtonText != null)
        {
            selectButtonText.text = isSelected ? "Active" : "Focus";
        }

        if (selectButton != null)
        {
            selectButton.interactable = !isSelected;
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelect?.Invoke(skill.id));
        }

        bool canRemove = skill.percent <= 0f;

        if (removeButtonText != null)
        {
            removeButtonText.text = "Del";
        }

        if (removeButton != null)
        {
            removeButton.interactable = canRemove;
            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(() => onRemove?.Invoke(skill.id));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
    }

    private void EnsureReadableLayout()
    {
        if (rowLayoutGroup == null)
        {
            rowLayoutGroup = GetComponent<HorizontalLayoutGroup>();
        }

        if (rowLayoutGroup == null)
        {
            return;
        }

        rowLayoutGroup.childControlWidth = true;
        rowLayoutGroup.childControlHeight = true;
        rowLayoutGroup.childForceExpandWidth = false;
        rowLayoutGroup.childForceExpandHeight = false;
    }

    public void ResetRowState()
    {
        if (highlightCoroutine != null)
        {
            StopCoroutine(highlightCoroutine);
            highlightCoroutine = null;
        }

        transform.localScale = defaultScale;

        if (backgroundImage != null)
        {
            backgroundImage.color = defaultBackgroundColor;
        }
    }

    public void PlayHighlight()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (highlightCoroutine != null)
        {
            StopCoroutine(highlightCoroutine);
        }

        highlightCoroutine = StartCoroutine(PlayHighlightRoutine());
    }

    private IEnumerator PlayHighlightRoutine()
    {
        const float duration = 0.48f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(normalized * Mathf.PI);
            float scale = 1f + 0.04f * pulse;

            transform.localScale = defaultScale * scale;

            if (backgroundImage != null)
            {
                backgroundImage.color = Color.Lerp(defaultBackgroundColor, highlightBackgroundColor, pulse);
            }

            yield return null;
        }

        transform.localScale = defaultScale;

        if (backgroundImage != null)
        {
            backgroundImage.color = defaultBackgroundColor;
        }

        highlightCoroutine = null;
    }
}
