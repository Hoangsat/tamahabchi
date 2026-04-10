using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class RadarChartGraphic : Graphic
{
    [SerializeField] private int ringCount = 4;
    [SerializeField] private float axisThickness = 2f;
    [SerializeField] private float outlineThickness = 3f;
    [SerializeField] private float pointRadius = 5f;
    [SerializeField] private float maxedPointRadius = 7f;
    [SerializeField] private float padding = 12f;
    [SerializeField] private float polygonAnimationDuration = 0.32f;
    [SerializeField] private float pointPulseDuration = 0.45f;
    [SerializeField] private Color gridColor = new Color(0.58f, 0.62f, 0.72f, 0.32f);
    [SerializeField] private Color axisColor = new Color(0.68f, 0.72f, 0.82f, 0.45f);
    [SerializeField] private Color fillColor = new Color(0.27f, 0.83f, 0.95f, 0.45f);
    [SerializeField] private Color outlineColor = new Color(0.46f, 0.91f, 1f, 0.95f);
    [SerializeField] private Color maxedPointColor = new Color(1f, 0.83f, 0.33f, 1f);

    private readonly List<float> values = new List<float>();
    private readonly List<Color> markerColors = new List<Color>();
    private readonly List<float> animationStartValues = new List<float>();
    private readonly List<float> animationTargetValues = new List<float>();

    private bool isAnimatingPolygon;
    private float polygonAnimationElapsed;
    private int highlightedPointIndex = -1;
    private float pointPulseElapsed;
    private bool highlightMaxedPoint;

    public void SetValues(IReadOnlyList<float> normalizedValues, IReadOnlyList<Color> colors)
    {
        CopyValues(values, normalizedValues);
        CopyValues(animationStartValues, normalizedValues);
        CopyValues(animationTargetValues, normalizedValues);
        CopyColors(colors);

        isAnimatingPolygon = false;
        highlightedPointIndex = -1;
        pointPulseElapsed = 0f;
        highlightMaxedPoint = false;
        SetVerticesDirty();
    }

    public void SetValuesAnimated(IReadOnlyList<float> normalizedValues, IReadOnlyList<Color> colors, int changedIndex)
    {
        if (normalizedValues == null)
        {
            SetValues(null, colors);
            return;
        }

        CopyColors(colors);

        if (values.Count != normalizedValues.Count || normalizedValues.Count < 3)
        {
            SetValues(normalizedValues, colors);
            StartPointPulse(changedIndex, normalizedValues);
            return;
        }

        CopyValues(animationStartValues, values);
        CopyValues(animationTargetValues, normalizedValues);

        bool hasVisibleChange = false;
        for (int i = 0; i < animationTargetValues.Count; i++)
        {
            if (Mathf.Abs(animationTargetValues[i] - animationStartValues[i]) > 0.0001f)
            {
                hasVisibleChange = true;
                break;
            }
        }

        if (!hasVisibleChange)
        {
            SetValues(normalizedValues, colors);
            StartPointPulse(changedIndex, normalizedValues);
            return;
        }

        polygonAnimationElapsed = 0f;
        isAnimatingPolygon = true;
        StartPointPulse(changedIndex, normalizedValues);
        SetVerticesDirty();
    }

    private void Update()
    {
        bool shouldRedraw = false;

        if (isAnimatingPolygon)
        {
            polygonAnimationElapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(polygonAnimationElapsed / Mathf.Max(0.01f, polygonAnimationDuration));
            float eased = 1f - Mathf.Pow(1f - normalized, 2f);

            values.Clear();
            for (int i = 0; i < animationTargetValues.Count; i++)
            {
                values.Add(Mathf.Lerp(animationStartValues[i], animationTargetValues[i], eased));
            }

            shouldRedraw = true;

            if (normalized >= 1f)
            {
                isAnimatingPolygon = false;
                CopyValues(values, animationTargetValues);
            }
        }

        if (highlightedPointIndex >= 0)
        {
            pointPulseElapsed += Time.unscaledDeltaTime;
            shouldRedraw = true;

            if (pointPulseElapsed >= pointPulseDuration)
            {
                highlightedPointIndex = -1;
                pointPulseElapsed = 0f;
                highlightMaxedPoint = false;
            }
        }

        if (shouldRedraw)
        {
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (values.Count < 3)
        {
            return;
        }

        Rect rect = rectTransform.rect;
        Vector2 center = rect.center;
        float radius = Mathf.Max(0f, Mathf.Min(rect.width, rect.height) * 0.5f - padding);
        if (radius <= 0f)
        {
            return;
        }

        DrawGrid(vh, center, radius, values.Count);
        DrawAxes(vh, center, radius, values.Count);
        DrawPolygon(vh, center, radius);
        DrawPolygonOutline(vh, center, radius);
        DrawPoints(vh, center, radius);
    }

    private void StartPointPulse(int changedIndex, IReadOnlyList<float> normalizedValues)
    {
        if (changedIndex < 0 || normalizedValues == null || changedIndex >= normalizedValues.Count)
        {
            highlightedPointIndex = -1;
            pointPulseElapsed = 0f;
            highlightMaxedPoint = false;
            return;
        }

        highlightedPointIndex = changedIndex;
        pointPulseElapsed = 0f;
        highlightMaxedPoint = normalizedValues[changedIndex] >= 0.999f;
    }

    private void CopyValues(List<float> destination, IReadOnlyList<float> source)
    {
        destination.Clear();

        if (source == null)
        {
            return;
        }

        for (int i = 0; i < source.Count; i++)
        {
            destination.Add(Mathf.Clamp01(source[i]));
        }
    }

    private void CopyColors(IReadOnlyList<Color> colors)
    {
        markerColors.Clear();

        if (colors == null)
        {
            return;
        }

        for (int i = 0; i < colors.Count; i++)
        {
            markerColors.Add(colors[i]);
        }
    }

    private void DrawGrid(VertexHelper vh, Vector2 center, float radius, int axisCount)
    {
        for (int ringIndex = 1; ringIndex <= ringCount; ringIndex++)
        {
            float ringRadius = radius * ringIndex / ringCount;
            for (int axisIndex = 0; axisIndex < axisCount; axisIndex++)
            {
                Vector2 start = GetOuterPoint(center, ringRadius, axisIndex, axisCount);
                Vector2 end = GetOuterPoint(center, ringRadius, (axisIndex + 1) % axisCount, axisCount);
                AddLine(vh, start, end, axisThickness, gridColor);
            }
        }
    }

    private void DrawAxes(VertexHelper vh, Vector2 center, float radius, int axisCount)
    {
        for (int axisIndex = 0; axisIndex < axisCount; axisIndex++)
        {
            Vector2 outer = GetOuterPoint(center, radius, axisIndex, axisCount);
            AddLine(vh, center, outer, axisThickness, axisColor);
        }
    }

    private void DrawPolygon(VertexHelper vh, Vector2 center, float radius)
    {
        int startIndex = vh.currentVertCount;
        vh.AddVert(center, fillColor, Vector2.zero);

        for (int i = 0; i < values.Count; i++)
        {
            vh.AddVert(GetValuePoint(center, radius, i, values.Count, values[i]), fillColor, Vector2.zero);
        }

        for (int i = 0; i < values.Count; i++)
        {
            int current = startIndex + 1 + i;
            int next = startIndex + 1 + ((i + 1) % values.Count);
            vh.AddTriangle(startIndex, current, next);
        }
    }

    private void DrawPolygonOutline(VertexHelper vh, Vector2 center, float radius)
    {
        for (int i = 0; i < values.Count; i++)
        {
            Vector2 start = GetValuePoint(center, radius, i, values.Count, values[i]);
            Vector2 end = GetValuePoint(center, radius, (i + 1) % values.Count, values.Count, values[(i + 1) % values.Count]);
            AddLine(vh, start, end, outlineThickness, outlineColor);
        }
    }

    private void DrawPoints(VertexHelper vh, Vector2 center, float radius)
    {
        float pulse = GetCurrentPulse();

        for (int i = 0; i < values.Count; i++)
        {
            Vector2 point = GetValuePoint(center, radius, i, values.Count, values[i]);
            bool isMaxed = values[i] >= 0.999f;
            bool isHighlighted = i == highlightedPointIndex;
            Color pointColor = isMaxed ? maxedPointColor : GetMarkerColor(i);

            if (isHighlighted && !isMaxed)
            {
                pointColor = Color.Lerp(pointColor, Color.white, pulse * 0.35f);
            }
            else if (isHighlighted && isMaxed)
            {
                pointColor = Color.Lerp(pointColor, Color.white, pulse * 0.2f);
            }

            float currentRadius = isMaxed ? maxedPointRadius : pointRadius;
            if (isHighlighted)
            {
                float multiplier = highlightMaxedPoint ? 1f + 0.9f * pulse : 1f + 0.55f * pulse;
                currentRadius *= multiplier;
            }

            AddCircle(vh, point, currentRadius, pointColor);
        }
    }

    private float GetCurrentPulse()
    {
        if (highlightedPointIndex < 0 || pointPulseDuration <= 0f)
        {
            return 0f;
        }

        float normalized = Mathf.Clamp01(pointPulseElapsed / pointPulseDuration);
        return Mathf.Sin(normalized * Mathf.PI);
    }

    private Color GetMarkerColor(int index)
    {
        if (index >= 0 && index < markerColors.Count)
        {
            return markerColors[index];
        }

        return outlineColor;
    }

    private Vector2 GetValuePoint(Vector2 center, float radius, int index, int count, float normalizedValue)
    {
        return GetOuterPoint(center, radius * normalizedValue, index, count);
    }

    private Vector2 GetOuterPoint(Vector2 center, float radius, int index, int count)
    {
        float angle = Mathf.PI * 0.5f - (Mathf.PI * 2f * index / count);
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    private void AddLine(VertexHelper vh, Vector2 start, Vector2 end, float thickness, Color colorValue)
    {
        Vector2 direction = (end - start).normalized;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Vector2 normal = new Vector2(-direction.y, direction.x) * (thickness * 0.5f);
        int index = vh.currentVertCount;

        vh.AddVert(start - normal, colorValue, Vector2.zero);
        vh.AddVert(start + normal, colorValue, Vector2.zero);
        vh.AddVert(end + normal, colorValue, Vector2.zero);
        vh.AddVert(end - normal, colorValue, Vector2.zero);

        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }

    private void AddCircle(VertexHelper vh, Vector2 center, float radius, Color colorValue)
    {
        const int segments = 16;
        int startIndex = vh.currentVertCount;
        vh.AddVert(center, colorValue, Vector2.zero);

        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            vh.AddVert(center + offset, colorValue, Vector2.zero);
        }

        for (int i = 1; i <= segments; i++)
        {
            vh.AddTriangle(startIndex, startIndex + i, startIndex + i + 1);
        }
    }
}
