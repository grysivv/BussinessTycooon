using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Wspólna paleta kolorów i format pieniędzy dla całego UI budowanego z kodu.
/// Styl: minimalistyczny, nowoczesny dashboard — neutralne ciemne tła,
/// cienkie linie (hairline) zamiast grubych ramek, jeden niebieski akcent.
/// </summary>
public static class UITheme
{
    // --- Tła ---
    public static readonly Color PanelBg      = new Color(0.071f, 0.082f, 0.110f);   // #12151C
    public static readonly Color OverlayBg    = new Color(0.055f, 0.067f, 0.086f);   // #0E1116
    public static readonly Color CardBg       = new Color(0.094f, 0.114f, 0.149f);   // #181D26
    public static readonly Color CardBgHover  = new Color(0.125f, 0.153f, 0.204f);   // #202734
    public static readonly Color CardBgActive = new Color(0.165f, 0.200f, 0.259f);   // #2A3342
    public static readonly Color InsetBg      = new Color(0.043f, 0.055f, 0.071f);   // #0B0E12

    // --- Linie (hairline — biel o niskiej alfie) ---
    public static readonly Color Border       = new Color(1f, 1f, 1f, 0.08f);
    public static readonly Color BorderStrong = new Color(1f, 1f, 1f, 0.14f);

    // --- Tekst ---
    public static readonly Color TextPrimary   = new Color(0.910f, 0.930f, 0.957f);  // #E8ECF4
    public static readonly Color TextSecondary = new Color(0.576f, 0.612f, 0.690f);  // #939CB0
    public static readonly Color TextHeader    = new Color(0.420f, 0.455f, 0.533f);  // #6B7488

    // --- Akcenty ---
    public static readonly Color Accent       = new Color(0.298f, 0.553f, 1f);       // #4C8DFF
    public static readonly Color Positive     = new Color(0.204f, 0.827f, 0.600f);   // #34D399
    public static readonly Color PositiveSoft = new Color(0.204f, 0.827f, 0.600f, 0.12f);
    public static readonly Color Negative     = new Color(0.973f, 0.443f, 0.443f);   // #F87171
    public static readonly Color NeutralSoft  = new Color(1f, 1f, 1f, 0.06f);

    /// <summary>Formatuje kwotę jako "$12 500" (spacja jako separator tysięcy).</summary>
    public static string FormatMoney(float value)
    {
        bool negative = value < 0f;
        long abs = (long)Mathf.Abs(value);
        string digits = abs.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)
                           .Replace(",", " ");
        return (negative ? "-$" : "$") + digits;
    }
}

/// <summary>
/// Rozszerzenia IStyle skracające ustawianie ramek, zaokrągleń, paddingu i marginesów,
/// które w UI Toolkit trzeba ustawiać osobno dla każdej krawędzi.
/// </summary>
public static class UIStyleExtensions
{
    public static void SetBorder(this IStyle s, Color color, float width = 1f)
    {
        s.borderTopWidth = width;
        s.borderBottomWidth = width;
        s.borderLeftWidth = width;
        s.borderRightWidth = width;
        s.borderTopColor = color;
        s.borderBottomColor = color;
        s.borderLeftColor = color;
        s.borderRightColor = color;
    }

    public static void SetBorderColor(this IStyle s, Color color)
    {
        s.borderTopColor = color;
        s.borderBottomColor = color;
        s.borderLeftColor = color;
        s.borderRightColor = color;
    }

    public static void SetRadius(this IStyle s, float radius)
    {
        s.borderTopLeftRadius = radius;
        s.borderTopRightRadius = radius;
        s.borderBottomLeftRadius = radius;
        s.borderBottomRightRadius = radius;
    }

    public static void SetPadding(this IStyle s, float horizontal, float vertical)
    {
        s.paddingLeft = horizontal;
        s.paddingRight = horizontal;
        s.paddingTop = vertical;
        s.paddingBottom = vertical;
    }

    public static void SetPadding(this IStyle s, float all) => s.SetPadding(all, all);

    public static void SetMargin(this IStyle s, float horizontal, float vertical)
    {
        s.marginLeft = horizontal;
        s.marginRight = horizontal;
        s.marginTop = vertical;
        s.marginBottom = vertical;
    }
}
