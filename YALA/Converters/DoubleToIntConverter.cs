﻿using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YALA.Converters;
public class DoubleToIntConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is double doubleValue)
		{
			return (int)Math.Round(doubleValue);
		}

		return BindingOperations.DoNothing;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is int intValue)
		{
			return (double)intValue;
		}

		return BindingOperations.DoNothing;
	}
}