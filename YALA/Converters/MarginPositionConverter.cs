using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YALA.Converters;
public class MarginPositionConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		double margin = value is double d ? d : 0;
		string side = parameter?.ToString()?.ToLowerInvariant() ?? "";

		return side switch
		{
			"left" => new Thickness(margin, 0, 0, 0),
			"top" => new Thickness(0, margin, 0, 0),
			"right" => new Thickness(0, 0, margin, 0),
			"bottom" => new Thickness(0, 0, 0, margin),
			_ => new Thickness(0)
		};
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> throw new NotSupportedException();
}
