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
    public Button typeButton;
    public Button removeButton;
    public TextMeshProUGUI selectButtonText;
    public TextMeshProUGUI typeButtonText;
    public TextMeshProUGUI removeButtonText;

    private Color defaultBackgroundColor;
    private Color highlightBackgroundColor;
    private Vector3 defaultScale = Vector3.one;
    private Coroutine highlightCoroutine;
    private HorizontalLayoutGroup rowLayoutGroup;
    private LayoutElement rowLayoutElement;
    private bool responsiveDefaultsCached;
    private float defaultSpacing;
    private float defaultRowHeight;
    private float defaultIconFontSize;
    private float defaultNameFontSize;
    private float defaultPercentFontSize;
    private float defaultSelectButtonFontSize;
    private float defaultTypeButtonFontSize;
    private float defaultRemoveButtonFontSize;

    private void Awake()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        rowLayoutGroup = GetComponent<HorizontalLayoutGroup>();
        rowLayoutElement = GetComponent<LayoutElement>();
        CacheResponsiveDefaults();
        EnsureReadableLayout();
        RefreshResponsiveLayout();

        if (backgroundImage != null)
        {
            defaultBackgroundColor = backgroundImage.color;
        }

        defaultScale = transform.localScale;
    }

    public void Bind(
        SkillEntry skill,
        SkillProgressionViewData view,
        Color markerColor,
        bool isSelected,
        string archetypeLabel,
        Action<string> onSelect,
        Action<string> onChangeType,
        Action<string> onRemove)
    {
        if (skill == null || view == null)
        {
            return;
        }

        EnsureReadableLayout();
        EnsureTypeButton();

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
            iconText.textWrappingMode = TextWrappingModes.NoWrap;
            iconText.overflowMode = TextOverflowModes.Overflow;
            UiIconViewUtility.ApplyIconToTextSlot(iconText, skill.icon);
        }

        if (nameText != null)
        {
            string title = skill.isGolden ? $"{skill.name} *" : skill.name;
            string levelLine = $"LEVEL {view.level}";
            nameText.text = $"{title}\n{levelLine}";
            nameText.richText = false;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (percentText != null)
        {
            string progressText = view.isMaxed
                ? "MAXED"
                : $"{view.progressToNextLevelPercent:0.#}% to Lv.{Mathf.Min(view.level + 1, SkillProgressionModel.MaxLevel)}";
            string spText = view.isMaxed
                ? $"{view.totalSP} total SP"
                : $"{view.progressInLevel}/{view.requiredSPForNextLevel} SP in level";
            string goldenText = view.isGolden ? "\nGolden bonus active" : string.Empty;
            percentText.text = $"{progressText}\n{spText}{goldenText}";
            percentText.richText = false;
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

        if (typeButtonText != null)
        {
            typeButtonText.text = string.IsNullOrWhiteSpace(archetypeLabel) ? "Type" : archetypeLabel;
        }

        if (typeButton != null)
        {
            typeButton.interactable = onChangeType != null;
            typeButton.onClick.RemoveAllListeners();
            typeButton.onClick.AddListener(() => onChangeType?.Invoke(skill.id));
        }

        bool canRemove = view.totalSP <= 0;

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

        RefreshResponsiveLayout();
        LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
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

    public void RefreshResponsiveLayout()
    {
        CacheResponsiveDefaults();

        float canvasHeight = GetReferenceCanvasHeight();
        bool compact = canvasHeight > 0f && canvasHeight < 900f;
        bool veryCompact = canvasHeight > 0f && canvasHeight < 760f;

        float fontScale = veryCompact ? 0.86f : compact ? 0.93f : 1f;
        float spacingScale = veryCompact ? 0.85f : compact ? 0.92f : 1f;
        float heightScale = veryCompact ? 0.88f : compact ? 0.94f : 1f;

        if (rowLayoutGroup != null)
        {
            rowLayoutGroup.spacing = defaultSpacing * spacingScale;
        }

        if (rowLayoutElement != null && defaultRowHeight >= 0f)
        {
            rowLayoutElement.preferredHeight = defaultRowHeight * heightScale;
        }

        if (iconText != null)
        {
            string currentIcon = iconText.text;
            float baseIconFontSize = !string.IsNullOrEmpty(currentIcon) && currentIcon.Length > 2 ? 14f : defaultIconFontSize;
            iconText.fontSize = baseIconFontSize * fontScale;
        }

        if (nameText != null)
        {
            nameText.fontSize = defaultNameFontSize * fontScale;
        }

        if (percentText != null)
        {
            percentText.fontSize = defaultPercentFontSize * fontScale;
        }

        if (selectButtonText != null)
        {
            selectButtonText.fontSize = defaultSelectButtonFontSize * fontScale;
        }

        if (typeButtonText != null)
        {
            typeButtonText.fontSize = defaultTypeButtonFontSize * fontScale;
        }

        if (removeButtonText != null)
        {
            removeButtonText.fontSize = defaultRemoveButtonFontSize * fontScale;
        }
    }

    private void CacheResponsiveDefaults()
    {
        if (responsiveDefaultsCached)
        {
            return;
        }

        if (rowLayoutGroup == null)
        {
            rowLayoutGroup = GetComponent<HorizontalLayoutGroup>();
        }

        if (rowLayoutElement == null)
        {
            rowLayoutElement = GetComponent<LayoutElement>();
        }

        defaultSpacing = rowLayoutGroup != null ? rowLayoutGroup.spacing : 0f;
        defaultRowHeight = rowLayoutElement != null ? rowLayoutElement.preferredHeight : -1f;
        defaultIconFontSize = iconText != null ? iconText.fontSize : 24f;
        defaultNameFontSize = nameText != null ? nameText.fontSize : 18f;
        defaultPercentFontSize = percentText != null ? percentText.fontSize : 16f;
        defaultSelectButtonFontSize = selectButtonText != null ? selectButtonText.fontSize : 18f;
        defaultTypeButtonFontSize = typeButtonText != null ? typeButtonText.fontSize : 18f;
        defaultRemoveButtonFontSize = removeButtonText != null ? removeButtonText.fontSize : 18f;
        responsiveDefaultsCached = true;
    }

    private void EnsureTypeButton()
    {
        if (typeButton != null && typeButtonText != null)
        {
            return;
        }

        Button createdButton = CreateInlineButton("TypeButton");
        if (createdButton == null)
        {
            return;
        }

        typeButton = createdButton;
        Transform labelTransform = createdButton.transform.Find("Label");
        typeButtonText = labelTransform != null ? labelTransform.GetComponent<TextMeshProUGUI>() : null;
        if (typeButtonText != null)
        {
            defaultTypeButtonFontSize = typeButtonText.fontSize;
        }
    }

    private Button CreateInlineButton(string objectName)
    {
        if (transform == null)
        {
            return null;
        }

        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(transform, false);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = 132f;
        layoutElement.preferredHeight = 36f;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.25f, 0.31f, 0.41f, 0.96f);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(6f, 4f);
        labelRect.offsetMax = new Vector2(-6f, -4f);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.text = "Type";

        return buttonObject.GetComponent<Button>();
    }

    private float GetReferenceCanvasHeight()
    {
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            if (canvasRect != null && canvasRect.rect.height > 0.01f)
            {
                return canvasRect.rect.height;
            }
        }

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null && rectTransform.rect.height > 0.01f)
        {
            return rectTransform.rect.height;
        }

        return Screen.height;
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
