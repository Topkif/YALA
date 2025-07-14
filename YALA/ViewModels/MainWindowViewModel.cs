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
using Avalonia;

namespace YALA.ViewModels;
public partial class MainWindowViewModel : ViewModelBase
{
	[ObservableProperty] ObservableCollection<LabelingClass> labelingClasses = new();
	[ObservableProperty] ObservableCollection<BoundingBox> currentImageBoundingBoxes = new();
	[ObservableProperty] ObservableCollection<string> imagesPaths = new();
	[ObservableProperty] int currentImageIndex = 1;
	[ObservableProperty] string currentImageAbsolutePath = "";
	[ObservableProperty] Bitmap currentImageBitmap;

	// Variables for bounding box resizing
	BoundingBox resizingBoundingBox = new();
	ResizeDirection resizeDirection;
	int resizeLength;

	// Varables for boundingbox creation
	private bool isDrawingNewBoundingBox = false;
	private Point startPoint;
	private BoundingBox? newBoundingBox;

	DatabaseService databaseService = new();
	public MainWindowViewModel()
	{
		CurrentImageBitmap = new Bitmap("../../../Assets/notfound.png");
		LabelingClasses.Add(new LabelingClass { Id = 0, Name = "class1", Color = "#6eeb83", NumberOfInstances = 23, IsSelected = false });
		LabelingClasses.Add(new LabelingClass { Id = 1, Name = "class2", Color = "#3654b3", NumberOfInstances = 45, IsSelected = true });
		CurrentImageBoundingBoxes.Add(new BoundingBox { ClassId = 0, ClassName = "robot", Tlx = 110, Tly = 100, Width = 100, Height = 200, Color = "#FF0000" });
		CurrentImageBoundingBoxes.Add(new BoundingBox { ClassId = 3, ClassName = "but", Tlx = 300, Tly = 0, Width = 50, Height = 100, Color = "#0000FF" });
	}

	public void CreateNewProject(string dbPath, string classesPath)
	{
		if (databaseService.TablesExist(dbPath))
		{
			databaseService.Open(dbPath);
		}
		else
		{
			databaseService.Initialize(dbPath);
			List<string> classes = ClassesFileParser.ParseClassNames(classesPath);
			databaseService.AddClasses(classes);
		}
		LabelingClasses = databaseService.GetLabellingClasses();
	}

	public void OpenExistingProject(string path)
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

	public void UpdateLabelColor(LabelingClass labelingClass)
	{
		databaseService.SetClassColor(labelingClass.Name, labelingClass.Color);
	}

	public void SetSelectedLabel(LabelingClass labelingClass)
	{
		foreach (var label in LabelingClasses)
		{
			label.IsSelected = false;
		}
		labelingClass.IsSelected = true;
	}

	public void AddImages(List<string> imagesPaths)
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

	public void GotoImage(int imageId)
	{
		CurrentImageBoundingBoxes.Add(new BoundingBox { ClassId = 1, ClassName = "ballon", Tlx = 300, Tly = 350, Width = 600, Height = 200, Color = "#00FF00" });
		if (ImagesPaths.Count == 0)
			return;
		var clampedIndex = Math.Clamp(imageId, 1, ImagesPaths.Count);
		CurrentImageAbsolutePath = System.IO.Path.Join(databaseService.absolutePath, ImagesPaths[clampedIndex - 1]);
		CurrentImageBitmap = new Bitmap(CurrentImageAbsolutePath);
	}

	public void OnImageLeftClickedReceived(Point point)
	{
		;
	}

	public void OnImageRightClickedReceived(Point point)
	{
		;
	}

	public void OnThumbDragStarted(BoundingBox resizedBoundingBox, ResizeDirection direction)
	{
		resizingBoundingBox = resizedBoundingBox;
		resizeDirection = direction;
	}

	public void OnThumbDragDelta(double delta)
	{
		resizeLength = (int)delta;

		switch (resizeDirection)
		{
			case ResizeDirection.Top:
				resizingBoundingBox.Tly += resizeLength;
				resizingBoundingBox.Height -= resizeLength;
				break;

			case ResizeDirection.Bottom:
				resizingBoundingBox.Height += resizeLength;
				break;

			case ResizeDirection.Left:
				resizingBoundingBox.Tlx += resizeLength;
				resizingBoundingBox.Width -= resizeLength;
				break;

			case ResizeDirection.Right:
				resizingBoundingBox.Width += resizeLength;
				break;

			default:
				break;
		}
	}
	public void OnThumbDragCompleted()
	{
		if (CurrentImageBoundingBoxes.Remove(resizingBoundingBox))
		{
			CurrentImageBoundingBoxes.Add(resizingBoundingBox);
		}
	}

	public void DeleteBoundingBox(BoundingBox boundingBox)
	{
		CurrentImageBoundingBoxes.Remove(boundingBox);
	}

	[RelayCommand]
	private void ToggleSwitchChecked(bool isChecked)
	{
		CurrentImageBoundingBoxes.ToList().ForEach(bbox => bbox.EditingEnabled = isChecked);
	}

	public void OnEditBoundingBoxClicked(BoundingBox boundingBox, bool editEnabled)
	{
		foreach (var box in CurrentImageBoundingBoxes)
		{
			box.EditingEnabled = false;
		}
		if (boundingBox != null)
		{
			boundingBox.EditingEnabled = editEnabled;
		}
	}
	public void OnCanvasPointerPressed(Point position)
	{
		if (!isDrawingNewBoundingBox)
		{
			startPoint = position;
			newBoundingBox = new BoundingBox
			{
				Tlx = (int)position.X,
				Tly = (int)position.Y,
				Width = 0,
				Height = 0,
				Color = "#AA00FF",
				ClassName = "COUCOU",
			};
			CurrentImageBoundingBoxes.Add(newBoundingBox);
			isDrawingNewBoundingBox = true;
		}
		else
		{
			isDrawingNewBoundingBox = false;
			newBoundingBox = null;
		}
	}
	public void CancelBoundingBoxDrawing()
	{
		isDrawingNewBoundingBox = false;
		if (newBoundingBox == null)
			return;
		CurrentImageBoundingBoxes.Remove(newBoundingBox);
		newBoundingBox = null;
	}

	public void OnCanvasPointerMoved(Point position)
	{
		if (!isDrawingNewBoundingBox || newBoundingBox == null)
			return;

		int newWidth = (int)(position.X - startPoint.X);
		int newHeight = (int)(position.Y - startPoint.Y);

		newBoundingBox.Width = newWidth;
		newBoundingBox.Height = newHeight;
	}

}
