using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YALA.Models;
using YALA.Services;

namespace YALA.Services;
public class YoloExporter
{
	public static void ExportProject(string exportPath, DatabaseService databaseService, List<string> imagesPaths, List<string> selectedClasses, double trainRatio, double valRatio, double testRatio)
	{
		var total = imagesPaths.Count;
		var rng = new Random();
		var indices = Enumerable.Range(0, total).OrderBy(_ => rng.Next()).ToList();

		var ratioSum = trainRatio + valRatio + testRatio;
		trainRatio /= ratioSum;
		valRatio /= ratioSum;
		testRatio /= ratioSum;

		int trainCount = (int)Math.Round(trainRatio * total);
		int valCount = (int)Math.Round(valRatio * total);
		int testCount = total - trainCount - valCount;

		var trainIndices = indices.Take(trainCount).ToList();
		var valIndices = indices.Skip(trainCount).Take(valCount).ToList();
		var testIndices = indices.Skip(trainCount + valCount).ToList();

		var splits = new Dictionary<string, List<int>>
	{
		{ "train", trainIndices },
		{ "val", valIndices },
		{ "test", testIndices }
	};

		foreach (var split in splits)
		{
			string imagesDir = System.IO.Path.Combine(exportPath, "images", split.Key);
			string labelsDir = System.IO.Path.Combine(exportPath, "labels", split.Key);

			Directory.CreateDirectory(imagesDir);
			Directory.CreateDirectory(labelsDir);

			foreach (int index in split.Value)
			{
				string sourceImagePath = System.IO.Path.Combine(databaseService.absolutePath, imagesPaths[index]);
				string imageFileName = System.IO.Path.GetFileName(imagesPaths[index]);
				string targetImagePath = System.IO.Path.Combine(imagesDir, imageFileName);

				File.Copy(sourceImagePath, targetImagePath, overwrite: true);

				List<BoundingBox> bboxes = databaseService.GetBoundingBoxes(imagesPaths[index]).ToList();
				List<string> yoloStrings = new List<string>();

				foreach (var bbox in bboxes)
				{
					int classId = GetClassId(bbox.ClassName, selectedClasses);
					if (classId == -1) continue; // Skip if class not selected
					var imageSize = GetImageSize(targetImagePath);
					string bboxString = GetBoundingBoxString(classId, bbox.Tlx, bbox.Tly, bbox.Width, bbox.Height, imageSize.Width, imageSize.Height);
					yoloStrings.Add(bboxString);
				}
				string labelFilePath = System.IO.Path.Combine(labelsDir, System.IO.Path.ChangeExtension(imageFileName, ".txt"));
				File.WriteAllLines(labelFilePath, yoloStrings);
			}
		}
	}

	static int GetClassId(string className, List<string> selectedClasses)
	{
		int classId = selectedClasses.IndexOf(className); // Returns -1 if not found
		return classId;
	}
	static string GetBoundingBoxString(int classId, double tlx, double tly, double width, double height, int imageWidth, int imageHeight)
	{
		float xCenter = (float)(tlx + width / 2) / imageWidth;
		float yCenter = (float)(tly + height / 2) / imageHeight;
		float normWidth = (float)width / imageWidth;
		float normHeight = (float)height / imageHeight;
		// Dot decimal separator is used for YOLO format
		return string.Format(System.Globalization.CultureInfo.InvariantCulture,
			"{0} {1} {2} {3} {4}",
			classId, xCenter, yCenter, normWidth, normHeight);
	}

	static (int Width, int Height) GetImageSize(string path)
	{
		var info = Image.Identify(path);
		return info is not null ? (info.Width, info.Height) : throw new Exception("Invalid image");
	}
}
