using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class BarChart : VisualElement
{
    public struct BarData
    {
        public string Label;
        public float Value1;
        public float Value2;
    }

    private List<BarData> _data = new();
    private float _maxValue = 100f;

    public BarChart()
    {
        style.height = 150;
        style.width = Length.Percent(100);
        style.backgroundColor = UITheme.InsetBg;
        style.SetBorder(UITheme.Border);
        style.SetRadius(4);
        style.flexDirection = FlexDirection.Row;
        style.alignItems = Align.FlexEnd;
        style.SetPadding(8);
    }

    public void SetData(List<BarData> data)
    {
        _data = new List<BarData>(data);
        CalculateMaxValue();
        Refresh();
    }

    private void CalculateMaxValue()
    {
        _maxValue = 1f;
        foreach (var bar in _data)
        {
            _maxValue = Mathf.Max(_maxValue, bar.Value1, bar.Value2);
        }
        _maxValue *= 1.1f;
    }

    private void Refresh()
    {
        Clear();

        if (_data.Count == 0) return;

        foreach (var barData in _data)
        {
            var pairContainer = new VisualElement();
            pairContainer.style.flexDirection = FlexDirection.Row;
            pairContainer.style.alignItems = Align.FlexEnd;
            pairContainer.style.flexGrow = 1;
            pairContainer.style.marginLeft = 2;
            pairContainer.style.marginRight = 2;

            // Bar 1
            var bar1 = new VisualElement();
            bar1.style.flexGrow = 1;
            bar1.style.backgroundColor = UITheme.Positive;
            bar1.style.marginRight = 1;
            float h1 = (_maxValue > 0) ? (barData.Value1 / _maxValue) * 100f : 0f;
            bar1.style.height = new Length(Mathf.Max(2f, h1), LengthUnit.Percent);
            pairContainer.Add(bar1);

            // Bar 2
            var bar2 = new VisualElement();
            bar2.style.flexGrow = 1;
            bar2.style.backgroundColor = UITheme.Negative;
            float h2 = (_maxValue > 0) ? (barData.Value2 / _maxValue) * 100f : 0f;
            bar2.style.height = new Length(Mathf.Max(2f, h2), LengthUnit.Percent);
            pairContainer.Add(bar2);

            Add(pairContainer);
        }
    }
}
