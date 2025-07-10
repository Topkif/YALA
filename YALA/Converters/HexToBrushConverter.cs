using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace YALA.Converters;
public class HexToBrushConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string hex && !string.IsNullOrWhiteSpace(hex))
		{
			try
			{
				return new SolidColorBrush(Color.Parse(hex));
			}
			catch { }
		}
		return Brushes.Transparent;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotImplementedException();
}
