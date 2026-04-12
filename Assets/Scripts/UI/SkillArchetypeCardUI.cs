using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkillArchetypeCardUI : MonoBehaviour
{
    private static readonly Color NormalColor = new Color(0.20f, 0.24f, 0.34f, 0.92f);
    private static readonly Color SelectedColor = new Color(0.27f, 0.46f, 0.33f, 0.98f);
    private static readonly Color NormalOutlineColor = new Color(0.48f, 0.55f, 0.70f, 0.75f);
    private static readonly Color SelectedOutlineColor = new Color(0.91f, 0.84f, 0.44f, 1f);

    public Image backgroundImage;
    public Image outlineImage;
    public TextMeshProUGUI iconText;
    public TextMeshProUGUI nameText;
    public Button button;

    private void Awake()
    {
        EnsureReferences();
    }

    public void Bind(SkillArchetypeDefinition definition, bool selected, Action<string> onClick)
    {
        EnsureReferences();

        if (definition == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (iconText != null)
        {
            string iconKey = string.IsNullOrWhiteSpace(definition.UiIconKey) ? definition.CanonicalIcon : definition.UiIconKey;
            iconText.text = iconKey;
            UiIconViewUtility.ApplyIconToTextSlot(iconText, iconKey);
        }

        if (nameText != null)
        {
            nameText.text = definition.DisplayName;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? SelectedColor : NormalColor;
        }

        if (outlineImage != null)
        {
            outlineImage.color = selected ? SelectedOutlineColor : NormalOutlineColor;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(definition.Id));
        }
    }

    private void EnsureReferences()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (outlineImage == null)
        {
            Transform outlineTransform = transform.Find("Outline");
            outlineImage = outlineTransform != null ? outlineTransform.GetComponent<Image>() : null;
        }

        if (iconText == null)
        {
            Transform iconTransform = transform.Find("Icon");
            iconText = iconTransform != null ? iconTransform.GetComponent<TextMeshProUGUI>() : null;
        }

        if (nameText == null)
        {
            Transform nameTransform = transform.Find("Name");
            nameText = nameTransform != null ? nameTransform.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
