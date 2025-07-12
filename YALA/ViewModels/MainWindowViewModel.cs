using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using YALA.Services;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.ObjectModel;
using YALA.Models;
using Avalonia.Controls.Shapes;
using System.Linq;
using Avalonia.Media.Imaging;
using System.Reflection;

namespace YALA.ViewModels;
public partial class MainWindowViewModel : ViewModelBase
{
	[ObservableProperty] ObservableCollection<LabelingClass> labelingClasses = new();
	[ObservableProperty] ObservableCollection<BoundingBox> currentImageBoundingBoxes = new();
	[ObservableProperty] ObservableCollection<string> imagesPaths = new();
	[ObservableProperty] int currentImageIndex = 1;
	[ObservableProperty] string currentImageAbsolutePath = "";
	[ObservableProperty] Bitmap currentImageBitmap;


	DatabaseService databaseService = new();
	public MainWindowViewModel()
	{
		CurrentImageBitmap = new Bitmap("../../../Assets/notfound.png");
		LabelingClasses.Add(new LabelingClass { Id = 0, Name = "class1", Color = "#6eeb83", NumberOfInstances = 23, IsSelected = false });
		LabelingClasses.Add(new LabelingClass { Id = 1, Name = "class2", Color = "#3654b3", NumberOfInstances = 45, IsSelected = true });
		CurrentImageBoundingBoxes.Add(new BoundingBox { Tlx = 110, Tly = 100, Width = 100, Height = 200, Color = "#FF0000" });
		CurrentImageBoundingBoxes.Add(new BoundingBox { Tlx = 300, Tly = 0, Width = 50, Height = 100, Color = "#0000FF" });

	}

	[RelayCommand]
	private void CreateNewProject((string dbPath, string classesPath) paths)
	{
		if (databaseService.TablesExist(paths.dbPath))
		{
			databaseService.Open(paths.dbPath);
		}
		else
		{
			databaseService.Initialize(paths.dbPath);
			List<string> classes = ClassesFileParser.ParseClassNames(paths.classesPath);
			databaseService.AddClasses(classes);
		}
		LabelingClasses = databaseService.GetLabellingClasses();
	}

	[RelayCommand]
	private void OpenExistingProject(string path)
	{
		if (databaseService.TablesExist(path))
		{
			databaseService.Open(path);
		}
		else
		{
			databaseService.Initialize(path);
		}
		LabelingClasses = databaseService.GetLabellingClasses();
		ImagesPaths = databaseService.GetImagesPaths();
		CurrentImageAbsolutePath = System.IO.Path.Join(databaseService.absolutePath, ImagesPaths[0]); // Load the first image by default
		CurrentImageBitmap = new Bitmap(CurrentImageAbsolutePath);
	}

	[RelayCommand]
	private void CloseCurrentProject()
	{
		databaseService.Close();
	}

	[RelayCommand]
	private void UpdateLabelColor(LabelingClass labelingClass)
	{
		databaseService.SetClassColor(labelingClass.Name, labelingClass.Color);
	}

	[RelayCommand]
	private void SetSelectedLabel(LabelingClass labelingClass)
	{
		foreach (var label in LabelingClasses)
		{
			label.IsSelected = false;
		}
		labelingClass.IsSelected = true;
	}

	[RelayCommand]
	private void AddImages(List<string> imagesPaths)
	{
		if (databaseService?.connection?.State == System.Data.ConnectionState.Open)
		{
			List<string> relativePaths = imagesPaths
		.Select(path => System.IO.Path.GetRelativePath(databaseService.absolutePath, path))
		.ToList();
			databaseService.AddImages(relativePaths);
			ImagesPaths = databaseService.GetImagesPaths();
			CurrentImageAbsolutePath = System.IO.Path.Join(databaseService.absolutePath, ImagesPaths[0]); // Load the first image by default
			CurrentImageBitmap = new Bitmap(CurrentImageAbsolutePath);
		}
	}

	[RelayCommand]
	private void NextImage()
	{
		if (ImagesPaths.Count == 0)
			return;
		CurrentImageIndex = Math.Min(ImagesPaths.Count, CurrentImageIndex + 1);
		CurrentImageAbsolutePath = System.IO.Path.Join(databaseService.absolutePath, ImagesPaths[CurrentImageIndex - 1]);
		CurrentImageBitmap = new Bitmap(CurrentImageAbsolutePath);
	}

	[RelayCommand]
	private void PreviousImage()
	{
		if (ImagesPaths.Count == 0)
			return;
		CurrentImageIndex = Math.Max(1, CurrentImageIndex - 1);
		CurrentImageAbsolutePath = System.IO.Path.Join(databaseService.absolutePath, ImagesPaths[CurrentImageIndex - 1]);
		CurrentImageBitmap = new Bitmap(CurrentImageAbsolutePath);
	}

	[RelayCommand]
	private void GotoImage(int imageId)
	{
		if (ImagesPaths.Count == 0)
			return;
		var clampedIndex = Math.Clamp(imageId, 1, ImagesPaths.Count); 
		CurrentImageAbsolutePath = System.IO.Path.Join(databaseService.absolutePath, ImagesPaths[clampedIndex - 1]);
		CurrentImageBitmap = new Bitmap(CurrentImageAbsolutePath);
	}
}
