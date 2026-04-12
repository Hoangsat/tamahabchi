using System.Collections.Generic;
using UnityEngine;

public static class UiIconProvider
{
    private const string LibraryResourcePath = "UiIconLibrary";
    private const int IconSize = 64;

    private static bool libraryLoaded;
    private static readonly Dictionary<string, Sprite> explicitSprites = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, Sprite> generatedSprites = new Dictionary<string, Sprite>();

    public static Sprite GetSprite(string iconId)
    {
        string key = NormalizeKey(iconId);
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        EnsureLibraryLoaded();
        if (explicitSprites.TryGetValue(key, out Sprite explicitSprite) && explicitSprite != null)
        {
            return explicitSprite;
        }

        if (generatedSprites.TryGetValue(key, out Sprite generatedSprite) && generatedSprite != null)
        {
            return generatedSprite;
        }

        Sprite createdSprite = CreateGeneratedSprite(key);
        generatedSprites[key] = createdSprite;
        return createdSprite;
    }

    private static void EnsureLibraryLoaded()
    {
        if (libraryLoaded)
        {
            return;
        }

        libraryLoaded = true;
        explicitSprites.Clear();

        UiIconLibrary library = Resources.Load<UiIconLibrary>(LibraryResourcePath);
        if (library == null || library.entries == null)
        {
            return;
        }

        for (int i = 0; i < library.entries.Count; i++)
        {
            UiIconEntry entry = library.entries[i];
            if (entry == null || entry.sprite == null)
            {
                continue;
            }

            string key = NormalizeKey(entry.id);
            if (!string.IsNullOrEmpty(key))
            {
                explicitSprites[key] = entry.sprite;
            }
        }
    }

    private static Sprite CreateGeneratedSprite(string key)
    {
        int hash = GetStableHash(key);
        Color background = GetBackgroundColor(hash);
        Color accent = GetAccentColor(hash);
        int shapeIndex = Mathf.Abs(hash % 6);

        Texture2D texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
        texture.name = $"GeneratedIcon_{key}";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[IconSize * IconSize];
        for (int y = 0; y < IconSize; y++)
        {
            for (int x = 0; x < IconSize; x++)
            {
                int index = (y * IconSize) + x;
                float u = ((x + 0.5f) / IconSize) * 2f - 1f;
                float v = ((y + 0.5f) / IconSize) * 2f - 1f;
                float radius = Mathf.Sqrt((u * u) + (v * v));

                if (radius > 0.96f)
                {
                    pixels[index] = Color.clear;
                    continue;
                }

                Color pixel = radius > 0.82f
                    ? Color.Lerp(background, accent, 0.32f)
                    : background;

                if (MatchesShape(shapeIndex, u, v, radius))
                {
                    pixel = accent;
                }

                pixels[index] = pixel;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, IconSize, IconSize),
            new Vector2(0.5f, 0.5f),
            IconSize);
        sprite.name = $"GeneratedIconSprite_{key}";
        return sprite;
    }

    private static bool MatchesShape(int shapeIndex, float u, float v, float radius)
    {
        switch (shapeIndex)
        {
            case 0:
                return radius <= 0.34f;
            case 1:
                return (Mathf.Abs(u) + Mathf.Abs(v)) <= 0.52f;
            case 2:
                return v > -0.48f && v < 0.38f && Mathf.Abs(u) <= (v + 0.52f) * 0.72f;
            case 3:
                return Mathf.Abs(u) <= 0.14f || Mathf.Abs(v) <= 0.14f;
            case 4:
                return Mathf.Abs(u - v) <= 0.16f || Mathf.Abs(u + v) <= 0.16f;
            default:
                return (radius >= 0.22f && radius <= 0.38f) || (Mathf.Abs(u) <= 0.12f && Mathf.Abs(v) <= 0.12f);
        }
    }

    private static Color GetBackgroundColor(int hash)
    {
        float hue = Mathf.Repeat((hash & 1023) / 1023f, 1f);
        return Color.HSVToRGB(hue, 0.48f, 0.92f);
    }

    private static Color GetAccentColor(int hash)
    {
        float hue = Mathf.Repeat(((hash >> 5) & 1023) / 1023f + 0.12f, 1f);
        return Color.HSVToRGB(hue, 0.42f, 0.26f);
    }

    private static int GetStableHash(string key)
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < key.Length; i++)
            {
                hash = (hash * 31) + key[i];
            }

            return hash;
        }
    }

    private static string NormalizeKey(string iconId)
    {
        return string.IsNullOrWhiteSpace(iconId)
            ? string.Empty
            : iconId.Trim().ToUpperInvariant();
    }
}
