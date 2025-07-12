using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YALA.Converters;
public class HalfSizeOffsetConverter : IMultiValueConverter
{
	public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
	{
		if (values[0] is double center && values[1] is double size)
			return center - size / 2;
		return 0;
	}
}

