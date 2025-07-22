using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YALA.Models;

namespace YALA.Services;


public class ProjectMerger
{
	public static void MergeProjects(DatabaseService currentDatabase, string incomingProjectPath, bool addClasses, bool addImages, bool addAnnotations, ConflictKeepBehaviour conflictKeepBehaviour)
	{
		DatabaseService incomingDatabase = new();
		// Check and open database
		if (!incomingDatabase.DoTablesExist(incomingProjectPath))
			return;

		if (addClasses)
		{
			// Get incoming classes and add them, if they don't exist they will be added, else ignored
			List<LabellingClass> incomingClasses = incomingDatabase.GetLabellingClasses().ToList();
			currentDatabase.AddClassesAndColor(incomingClasses.Select(x => (x.Name, x.Color)).ToList());
		}
		if (addImages)
		{
			// Get incoming images paths, calculate the new absolute path and add them to the database
			List<string> incomingImagesPaths = incomingDatabase.GetImagesPaths()
				.Select(x => System.IO.Path.GetRelativePath(currentDatabase.absolutePath, System.IO.Path.Combine(incomingProjectPath, x)))
				.ToList();
			currentDatabase.AddImages(incomingImagesPaths);
		}
		if (addAnnotations)
		{
			// For each image, we get the boundingBoxes, calculate the new relative path and enter the switch:
			List<string> incomingImagesPaths = incomingDatabase.GetImagesPaths().ToList();
			foreach (string incomingImagePath in incomingImagesPaths)
			{
				List<BoundingBox> incomingBoundingBoxes = incomingDatabase.GetBoundingBoxes(incomingImagePath).ToList();
				string newImagePath = System.IO.Path.GetRelativePath(currentDatabase.absolutePath, System.IO.Path.Combine(incomingProjectPath, incomingImagePath));
				switch (conflictKeepBehaviour)
				{
					case ConflictKeepBehaviour.Both:
						// Get incoming annotations, and just add them
						currentDatabase.AddBoundingBoxListSafe(incomingBoundingBoxes, newImagePath);
						break;
					case ConflictKeepBehaviour.Incoming:
						// Remove annotations with same ClassId in current database and add new annotations
						currentDatabase.RemoveImageAnnotationsForClasses(newImagePath, incomingBoundingBoxes.Select(x => x.ClassName).ToList());
						currentDatabase.AddBoundingBoxListSafe(incomingBoundingBoxes, newImagePath);
						break;
					case ConflictKeepBehaviour.Current:
						// Get current image annotations class names from current database (currentImageClassNames), keep from incomingBoundingBoxes only the ones with
						// ClassId not in currentImageClassNames (filteredIncomingBoundingBoxes) and add them
						List<string> currentImageClassNames = currentDatabase.GetBoundingBoxes(incomingImagePath).Select(x => x.ClassName).ToList();
						List<BoundingBox> filteredIncomingBoundingBoxes = incomingBoundingBoxes.Where(x => !currentImageClassNames.Contains(x.ClassName)).ToList();
						currentDatabase.AddBoundingBoxListSafe(filteredIncomingBoundingBoxes, newImagePath);
						break;
					default:
						break;
				}
			}
		}
	}
}