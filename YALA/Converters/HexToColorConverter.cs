using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace YALA.Converters;
public class HexToColorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string hex && Color.TryParse(hex, out var color))
			return Color.FromArgb(255, color.R, color.G, color.B); // Force full opacity  

		return Colors.Transparent;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is Color color)
			return color.ToString(); // returns hex string  

		return "#000000";
	}
}
