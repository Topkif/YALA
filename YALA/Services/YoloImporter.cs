using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YALA.Models;

namespace YALA.Services;
public static class YoloImporter
{
	public static List<BoundingBox> ReadAndConvertYoloAnnotationToBoundingBox(string imagePath, string annotationFilePath, List<LabellingClass> labellingClasses, bool editingEnabled)
	{
		var boundingBoxes = new List<BoundingBox>();

		foreach (var line in System.IO.File.ReadLines(annotationFilePath))
		{
			if (string.IsNullOrWhiteSpace(line))
				continue;

			var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length != 5)
				continue;

			if (!int.TryParse(parts[0], out var classId))
				continue;

			if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var centerX) ||
				!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var centerY) ||
				!double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var width) ||
				!double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var height))
				continue;

			// Convert YOLO center-normalized format to top-left format
			var imageSize = GetImageSize(imagePath);
			LabellingClass? labellingClass = labellingClasses.FirstOrDefault(c => c.Id == classId+1); // + 1

			boundingBoxes.Add(new BoundingBox
			{
				Id = boundingBoxes.Count,
				Tlx = (centerX - (width / 2.0)) * imageSize.Width,
				Tly = (centerY - (height / 2.0)) * imageSize.Height,
				Width = width * imageSize.Width,
				Height = height *imageSize.Height,
				Color = labellingClass?.Color ?? "#FFFFFF",
				ClassId = classId + 1, // + 1
				ClassName = labellingClass?.Name ?? "",
				EditingEnabled = editingEnabled,
				IsSelected = false
			});
		}

		return boundingBoxes;
	}

	static (int Width, int Height) GetImageSize(string path)
	{
		var info = Image.Identify(path);
		return info is not null ? (info.Width, info.Height) : throw new Exception("Invalid image");
	}

}
