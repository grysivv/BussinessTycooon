using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Minimalistyczny suwak rysowany z własnych elementów (tor + wypełnienie + gałka).
/// Nie zależy od motywu runtime Unity, który potrafi nie wyrenderować
/// domyślnego Slidera/Toggle'a — dlatego rysujemy wszystko sami.
/// </summary>
public class MiniSlider : VisualElement
{
    public event System.Action<float> ValueChanged;

    private const float KnobSize = 14f;

    private readonly float _min;
    private readonly float _max;
    private float _value;

    private readonly VisualElement _fill;
    private readonly VisualElement _knob;

    public float Value => _value;

    public MiniSlider(float min, float max)
    {
        _min = min;
        _max = max;
        _value = min;

        style.height = 24;
        style.flexShrink = 0;

        var track = new VisualElement();
        track.pickingMode = PickingMode.Ignore;
        track.style.position = Position.Absolute;
        track.style.left = 0;
        track.style.right = 0;
        track.style.top = 10;
        track.style.height = 4;
        track.style.backgroundColor = UITheme.CardBgActive;
        track.style.SetRadius(2);
        Add(track);

        _fill = new VisualElement();
        _fill.pickingMode = PickingMode.Ignore;
        _fill.style.position = Position.Absolute;
        _fill.style.left = 0;
        _fill.style.top = 10;
        _fill.style.height = 4;
        _fill.style.backgroundColor = UITheme.Accent;
        _fill.style.SetRadius(2);
        Add(_fill);

        _knob = new VisualElement();
        _knob.pickingMode = PickingMode.Ignore;
        _knob.style.position = Position.Absolute;
        _knob.style.top = (24f - KnobSize) / 2f;
        _knob.style.width = KnobSize;
        _knob.style.height = KnobSize;
        _knob.style.backgroundColor = UITheme.Accent;
        _knob.style.SetBorder(UITheme.TextPrimary, 2f);
        _knob.style.SetRadius(KnobSize / 2f);
        Add(_knob);

        RegisterCallback<PointerDownEvent>(OnPointerDown);
        RegisterCallback<PointerMoveEvent>(OnPointerMove);
        RegisterCallback<PointerUpEvent>(OnPointerUp);
        RegisterCallback<GeometryChangedEvent>(_ => UpdateVisuals());
    }

    public void SetValueWithoutNotify(float value)
    {
        _value = Mathf.Clamp(value, _min, _max);
        UpdateVisuals();
    }

    private void OnPointerDown(PointerDownEvent e)
    {
        this.CapturePointer(e.pointerId);
        SetFromPointer(e.localPosition.x);
    }

    private void OnPointerMove(PointerMoveEvent e)
    {
        if (this.HasPointerCapture(e.pointerId))
            SetFromPointer(e.localPosition.x);
    }

    private void OnPointerUp(PointerUpEvent e)
    {
        if (this.HasPointerCapture(e.pointerId))
            this.ReleasePointer(e.pointerId);
    }

    private void SetFromPointer(float x)
    {
        float width = resolvedStyle.width;
        if (float.IsNaN(width) || width <= KnobSize) return;

        float t = Mathf.Clamp01((x - KnobSize / 2f) / (width - KnobSize));
        float newValue = _min + t * (_max - _min);
        if (Mathf.Approximately(newValue, _value)) return;

        _value = newValue;
        UpdateVisuals();
        ValueChanged?.Invoke(_value);
    }

    private void UpdateVisuals()
    {
        float width = resolvedStyle.width;
        if (float.IsNaN(width) || width <= KnobSize) return;

        float t = (_value - _min) / (_max - _min);
        float knobX = t * (width - KnobSize);
        _knob.style.left = knobX;
        _fill.style.width = knobX + KnobSize / 2f;
    }
}

/// <summary>
/// Minimalistyczny przełącznik (switch) w stylu nowoczesnych dashboardów.
/// Zastępuje domyślny Toggle Unity, którego checkbox bywa niewidoczny
/// bez poprawnie skonfigurowanego motywu runtime.
/// </summary>
public class MiniSwitch : VisualElement
{
    public event System.Action<bool> ValueChanged;

    private bool _value;
    private readonly VisualElement _knob;

    public bool Value => _value;

    public MiniSwitch(bool initial)
    {
        _value = initial;

        style.width = 34;
        style.height = 18;
        style.flexShrink = 0;
        style.SetRadius(9);

        _knob = new VisualElement();
        _knob.pickingMode = PickingMode.Ignore;
        _knob.style.position = Position.Absolute;
        _knob.style.top = 2;
        _knob.style.width = 14;
        _knob.style.height = 14;
        _knob.style.SetRadius(7);
        _knob.style.backgroundColor = UITheme.TextPrimary;
        Add(_knob);

        RegisterCallback<ClickEvent>(_ =>
        {
            _value = !_value;
            UpdateVisuals();
            ValueChanged?.Invoke(_value);
        });

        UpdateVisuals();
    }

    public void SetValueWithoutNotify(bool value)
    {
        _value = value;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        style.backgroundColor = _value ? UITheme.Accent : UITheme.CardBgActive;
        _knob.style.left = _value ? 18 : 2;
    }
}
