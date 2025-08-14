using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YALA.Converters;

public class SelectedToFillBrushConverter : IMultiValueConverter
{
	public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
	{
		if (values[0] is bool isSelected && isSelected && values[1] is string hex && !string.IsNullOrWhiteSpace(hex))
		{
			try
			{
				var color = Color.Parse(hex);
				double opacity = 1; // Opacity of selected bounding box when filled
				if (parameter is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
					opacity = parsed;

				return new SolidColorBrush(new Color((byte)(opacity*255), color.R, color.G, color.B));
			}
			catch { }
		}

		return Brushes.Transparent;
	}
}
