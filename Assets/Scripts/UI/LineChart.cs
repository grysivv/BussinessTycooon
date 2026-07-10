using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class LineChart : VisualElement
{
    private List<float> _data = new();
    private float _minValue = 0f;
    private float _maxValue = 100f;

    public LineChart()
    {
        style.height = 150;
        style.width = Length.Percent(100);
        style.flexDirection = FlexDirection.Row;
        style.alignItems = Align.FlexEnd;
        style.paddingLeft = 8;
        style.paddingRight = 8;
        style.paddingTop = 8;
        style.paddingBottom = 8;
        AddToClassList("chart-container");
    }

    public void SetData(List<float> data)
    {
        _data = new List<float>(data);
        CalculateRange();
        Refresh();
    }

    private void CalculateRange()
    {
        if (_data.Count == 0)
        {
            _minValue = 0f;
            _maxValue = 100f;
            return;
        }

        _minValue = _data[0];
        _maxValue = _data[0];

        foreach (var value in _data)
        {
            if (value < _minValue) _minValue = value;
            if (value > _maxValue) _maxValue = value;
        }

        float range = _maxValue - _minValue;
        if (range < 1f) range = 1f;
        _minValue -= range * 0.05f;
        _maxValue += range * 0.05f;
    }

    private void Refresh()
    {
        Clear();

        if (_data.Count == 0) return;

        float range = _maxValue - _minValue;
        if (range < 0.01f) range = 1f;

        foreach (var value in _data)
        {
            var bar = new VisualElement();
            bar.style.flexGrow = 1;
            bar.style.marginLeft = 1;
            bar.style.marginRight = 1;
            bar.AddToClassList("chart-bar");

            float normalizedHeight = (value - _minValue) / range;
            bar.style.height = new Length(Mathf.Max(2f, normalizedHeight * 100f), LengthUnit.Percent);

            Add(bar);
        }
    }
}
