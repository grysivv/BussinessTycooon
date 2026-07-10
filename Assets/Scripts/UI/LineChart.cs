using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// Wykres liniowy rysowany wektorowo (Painter2D) z wypełnieniem pod linią
/// oraz podpisami min / max / ostatnia wartość.
/// </summary>
public class LineChart : VisualElement
{
    private const float Pad = 6f;

    private readonly List<float> _data = new();
    private readonly Label _minLabel;
    private readonly Label _maxLabel;
    private readonly Label _lastLabel;

    private float _minValue;
    private float _maxValue;

    /// <summary>Kolor linii (i wypełnienia pod nią).</summary>
    public Color LineColor { get; set; } = UITheme.Accent;

    public LineChart()
    {
        pickingMode = PickingMode.Ignore;
        style.height = 110;
        style.backgroundColor = UITheme.InsetBg;
        style.SetBorder(UITheme.Border);
        style.SetRadius(4);

        generateVisualContent += OnGenerateVisualContent;

        _maxLabel = CreateCornerLabel();
        _maxLabel.style.top = 3;
        _maxLabel.style.left = 5;

        _minLabel = CreateCornerLabel();
        _minLabel.style.bottom = 3;
        _minLabel.style.left = 5;

        _lastLabel = CreateCornerLabel();
        _lastLabel.style.top = 3;
        _lastLabel.style.right = 5;
        _lastLabel.style.color = UITheme.TextPrimary;
        _lastLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

        Add(_maxLabel);
        Add(_minLabel);
        Add(_lastLabel);
    }

    private static Label CreateCornerLabel()
    {
        var label = new Label();
        label.style.position = Position.Absolute;
        label.style.fontSize = 9;
        label.style.color = UITheme.TextSecondary;
        label.pickingMode = PickingMode.Ignore;
        return label;
    }

    public void SetData(List<float> data)
    {
        _data.Clear();
        if (data != null)
            _data.AddRange(data);

        if (_data.Count > 0)
        {
            _minValue = _data[0];
            _maxValue = _data[0];
            foreach (var v in _data)
            {
                if (v < _minValue) _minValue = v;
                if (v > _maxValue) _maxValue = v;
            }

            _maxLabel.text = _maxValue.ToString("F1");
            _minLabel.text = _minValue.ToString("F1");
            _lastLabel.text = _data[_data.Count - 1].ToString("F2");
        }
        else
        {
            _maxLabel.text = "";
            _minLabel.text = "";
            _lastLabel.text = "";
        }

        MarkDirtyRepaint();
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        var rect = contentRect;
        if (rect.width <= Pad * 2f || rect.height <= Pad * 2f || _data.Count < 2)
            return;

        // Zakres z 5% zapasem, żeby linia nie kleiła się do krawędzi
        float range = _maxValue - _minValue;
        if (range < 0.01f) range = 1f;
        float min = _minValue - range * 0.05f;
        range *= 1.10f;

        float w = rect.width - Pad * 2f;
        float h = rect.height - Pad * 2f;
        int last = _data.Count - 1;

        Vector2 PointAt(int i)
        {
            float x = Pad + w * i / last;
            float y = Pad + h * (1f - (_data[i] - min) / range);
            return new Vector2(x, y);
        }

        var painter = mgc.painter2D;

        // Wypełnienie pod linią
        var fillColor = LineColor;
        fillColor.a = 0.13f;
        painter.fillColor = fillColor;
        painter.BeginPath();
        painter.MoveTo(new Vector2(Pad, Pad + h));
        for (int i = 0; i <= last; i++)
            painter.LineTo(PointAt(i));
        painter.LineTo(new Vector2(Pad + w, Pad + h));
        painter.ClosePath();
        painter.Fill();

        // Linia
        painter.strokeColor = LineColor;
        painter.lineWidth = 1.5f;
        painter.lineJoin = LineJoin.Round;
        painter.BeginPath();
        painter.MoveTo(PointAt(0));
        for (int i = 1; i <= last; i++)
            painter.LineTo(PointAt(i));
        painter.Stroke();
    }
}
