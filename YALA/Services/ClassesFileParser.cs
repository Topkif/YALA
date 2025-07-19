using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;

namespace YALA.Services;

static class ClassesFileParser
{
	public static List<string> ParseClassNames(string filePath)
	{
		var classNames = new List<string>();
		if (!File.Exists(filePath))
			return classNames;

		foreach (var line in File.ReadLines(filePath))
		{
			var trimmed = line.Trim();
			if (!string.IsNullOrEmpty(trimmed))
				classNames.Add(trimmed);
		}

		return classNames;
	}

	public static List<(string Name, string Color)> ParseYalaClassNamesAndColor(string filePath)
	{
		var result = new List<(string Name, string Color)>();
		if (!File.Exists(filePath))
			return result;

		foreach (var line in File.ReadLines(filePath))
		{
			var trimmed = line.Trim();
			if (string.IsNullOrEmpty(trimmed))
				continue;

			var parts = trimmed.Split(';');
			if (parts.Length == 2)
				result.Add((parts[0].Trim(), parts[1].Trim()));
		}

		return result;
	}

}
