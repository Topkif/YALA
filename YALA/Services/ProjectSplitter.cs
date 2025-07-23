using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YALA.Models;

namespace YALA.Services;
public class ProjectSplitter
{
	public static void SplitProject(DatabaseService databaseService, List<int> splitImagesQuantities, bool randomize, string newBasePath)
	{
		List<string> allImagePaths = databaseService.GetImagesPaths().ToList();
		if (randomize)
		{
			allImagePaths = allImagePaths.OrderBy(_ => Guid.NewGuid()).ToList();
		}

		if (splitImagesQuantities.Sum() != allImagePaths.Count)
			throw new InvalidOperationException("Split quantities must sum to total number of images.");

		List<LabellingClass> allClasses = databaseService.GetLabellingClasses().ToList();
		int offset = 0;

		for (int i = 0; i < splitImagesQuantities.Count; i++)
		{
			if (splitImagesQuantities[i] == 0) continue;

			string splittedDbPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(databaseService.absolutePath), newBasePath+$"_{i}.yala");
			DatabaseService splittedDatabaseService = new();
			if (splittedDatabaseService.DoTablesExist(splittedDbPath))
			{
				throw new InvalidOperationException($"Cannot create new splitted database at: {splittedDbPath}. Database already exists!");
			}
			else
			{
				splittedDatabaseService.CreateYalaTables(splittedDbPath);
				// Add all classes
				List<(string, string)> classesAndColor = allClasses.Select(x => (x.Name, x.Color)).ToList();
				splittedDatabaseService.AddClassesAndColor(classesAndColor);

				// Add splitted images and corresponding annotations
				var splittedImagesPath = allImagePaths.Skip(offset).Take(splitImagesQuantities[i]).ToList();
				splittedDatabaseService.AddImages(splittedImagesPath);
				foreach (string imagePath in splittedImagesPath)
				{
					List<BoundingBox> boundingBoxes = databaseService.GetBoundingBoxes(imagePath).ToList();
					splittedDatabaseService.AddBoundingBoxListSafe(boundingBoxes, imagePath);
				}
			}
			splittedDatabaseService.Close();
			offset += splitImagesQuantities[i];
		}
	}

}

