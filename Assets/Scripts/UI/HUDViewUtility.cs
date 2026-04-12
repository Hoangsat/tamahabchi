using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class HUDViewUtility
{
    public static Transform FindByPath(Transform root, string path)
    {
        if (root == null || string.IsNullOrEmpty(path))
        {
            return null;
        }

        return root.Find(path);
    }

    public static GameObject GetPathObject(Transform root, string path)
    {
        Transform target = FindByPath(root, path);
        return target != null ? target.gameObject : null;
    }

    public static TextMeshProUGUI GetPathText(Transform root, string path)
    {
        Transform target = FindByPath(root, path);
        return target != null ? target.GetComponent<TextMeshProUGUI>() : null;
    }

    public static Button GetPathButton(Transform root, string path)
    {
        Transform target = FindByPath(root, path);
        return target != null ? target.GetComponent<Button>() : null;
    }

    public static TextMeshProUGUI EnsurePetVitalsText(GameObject mainStatusBlock, TextMeshProUGUI existingText)
    {
        if (existingText != null || mainStatusBlock == null)
        {
            return existingText;
        }

        RectTransform parent = mainStatusBlock.transform as RectTransform;
        if (parent == null)
        {
            return existingText;
        }

        GameObject textObject = new GameObject("PetVitalsText", typeof(RectTransform));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(parent, false);
        textRect.localScale = Vector3.one;

        LayoutElement layout = textObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 28f;

        TextMeshProUGUI petVitalsText = textObject.AddComponent<TextMeshProUGUI>();
        petVitalsText.text = "Hunger: --  |  Mood: --";
        petVitalsText.fontSize = 18f;
        petVitalsText.fontStyle = FontStyles.Normal;
        petVitalsText.alignment = TextAlignmentOptions.Left;
        petVitalsText.color = new Color(0.84f, 0.9f, 0.98f, 0.96f);
        petVitalsText.textWrappingMode = TextWrappingModes.NoWrap;
        petVitalsText.overflowMode = TextOverflowModes.Overflow;
        return petVitalsText;
    }

    public static GameObject EnsureSideActionsRoot(Transform searchRoot, GameObject existingRoot)
    {
        if (existingRoot != null)
        {
            return existingRoot;
        }

        if (searchRoot == null)
        {
            return null;
        }

        Transform existing = searchRoot.Find("HomeSideActionsRoot");
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject sideActionsRoot = new GameObject("HomeSideActionsRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
        RectTransform sideRect = sideActionsRoot.GetComponent<RectTransform>();
        sideRect.SetParent(searchRoot, false);
        sideRect.anchorMin = new Vector2(1f, 0.5f);
        sideRect.anchorMax = new Vector2(1f, 0.5f);
        sideRect.pivot = new Vector2(1f, 0.5f);
        sideRect.anchoredPosition = new Vector2(-20f, -90f);
        sideRect.sizeDelta = new Vector2(140f, 180f);

        VerticalLayoutGroup layout = sideActionsRoot.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return sideActionsRoot;
    }

    public static Button CreateActionButton(GameObject parentRoot, string name, string label, out TextMeshProUGUI labelText, float width, float height, float fontSize, Color? background = null)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(parentRoot.transform, false);
        buttonRect.localScale = Vector3.one;

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = width;
        layoutElement.preferredHeight = height;
        layoutElement.flexibleWidth = 0f;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = background ?? new Color(0.22f, 0.36f, 0.58f, 0.98f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = buttonImage.color;
        colors.highlightedColor = buttonImage.color * 1.12f;
        colors.pressedColor = buttonImage.color * 0.78f;
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.22f, 0.24f, 0.3f, 0.7f);
        button.colors = colors;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(buttonRect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        labelRect.localScale = Vector3.one;

        labelText = labelObject.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = fontSize;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = new Color(0.97f, 0.96f, 0.9f, 1f);
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
        return button;
    }

    public static Color GetSummaryColor(PetFlowState flowState)
    {
        switch (flowState)
        {
            case PetFlowState.Neglected:
                return new Color(1f, 0.42f, 0.42f, 1f);
            case PetFlowState.Critical:
                return new Color(1f, 0.63f, 0.28f, 1f);
            case PetFlowState.Warning:
                return new Color(1f, 0.84f, 0.4f, 1f);
            default:
                return new Color(0.76f, 0.88f, 1f, 1f);
        }
    }

    public static void SetTextVisible(TextMeshProUGUI text, bool visible)
    {
        if (text != null)
        {
            text.gameObject.SetActive(visible);
        }
    }

    public static void SetObjectActive(Component component, bool visible)
    {
        if (component != null)
        {
            component.gameObject.SetActive(visible);
        }
    }

    public static void SetGameObjectActive(GameObject target, bool visible)
    {
        if (target != null)
        {
            target.SetActive(visible);
        }
    }
}
