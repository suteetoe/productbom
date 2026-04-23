using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace BomApp.UI.Converters;

/// <summary>
/// Converts a BOM status string to a SolidColorBrush for the status badge.
///   "Active"   → Success green  (#22C55E)
///   "Draft"    → TextMuted gray (#94A3B8)
///   "Inactive" → Danger red     (#EF4444)
///   (other)    → TextMuted gray
/// </summary>
public sealed class BomStatusToBrushConverter : IValueConverter
{
    public static readonly BomStatusToBrushConverter Instance = new();

    private static readonly SolidColorBrush SuccessBrush  = new(Color.Parse("#22C55E"));
    private static readonly SolidColorBrush MutedBrush    = new(Color.Parse("#94A3B8"));
    private static readonly SolidColorBrush DangerBrush   = new(Color.Parse("#EF4444"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            "Active"   => SuccessBrush,
            "Inactive" => DangerBrush,
            _          => MutedBrush   // Draft and anything else
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("BomStatusToBrushConverter is one-way only.");
}
