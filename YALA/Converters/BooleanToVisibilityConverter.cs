using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace YALA.Converters;
public class BoolToIsVisibleConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		=> value is bool b && b;

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		=> value is bool b && b;
}

