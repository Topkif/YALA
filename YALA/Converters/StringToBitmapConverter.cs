using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YALA.Converters;

public class StringToBitmapConverter : IValueConverter
{
	public static readonly StringToBitmapConverter Instance = new();

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is string path && !string.IsNullOrWhiteSpace(path))
		{
			try
			{
				// Fastest implementation for Avalonia
				return new Bitmap(path);
			}
			catch (Exception ex) when (ex is FileNotFoundException or ArgumentException)
			{
				// Return null or a default image if the path is invalid
				return null;
			}
		}
		return null;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}