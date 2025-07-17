using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using YALA.Models;
using YALA.Services;
using static System.Net.Mime.MediaTypeNames;

namespace YALA.ViewModels;
public partial class MainWindowViewModel : ViewModelBase
{
	[ObservableProperty] ObservableCollection<LabelingClass> labelingClasses = new();
	[ObservableProperty] ObservableCollection<BoundingBox> currentImageBoundingBoxes = new();
	[ObservableProperty] ObservableCollection<string> imagesPaths = new();
	[ObservableProperty] int currentImageIndex = 1;
	[ObservableProperty] string currentImageAbsolutePath = "";
	[ObservableProperty] Bitmap currentImageBitmap;
	[ObservableProperty] bool resizingBoundingBoxEnabled;

	// Variables for bounding box resizing
	BoundingBox resizingBoundingBox = new();
	LabelingClass? selectedLabel = null;
	ResizeDirection resizeDirection;
	double resizeLength;

	// Varables for boundingbox creation
	private bool isDrawingNewBoundingBox = false;
	private Point startPoint;
	private BoundingBox? newBoundingBox;

	DatabaseService databaseService = new();

	public event EventHandler? OnForceNewBoundingBoxCollection;
	public MainWindowViewModel()
	{
		CurrentImageBitmap = new Bitmap("../../../Assets/notfound.png");
		LabelingClasses.Add(new LabelingClass { Id = 0, Name = "class1", Color = "#6eeb83", NumberOfInstances = 23, IsSelected = false });
		LabelingClasses.Add(new LabelingClass { Id = 1, Name = "class2", Color = "#3654b3", NumberOfInstances = 45, IsSelected = true });
		//CurrentImageBoundingBoxes.Add(new BoundingBox { ClassId = 0, ClassName = "robot", Tlx = 110, Tly = 100, Width = 100, Height = 200, Color = "#FF0000" });
		//CurrentImageBoundingBoxes.Add(new BoundingBox { ClassId = 3, ClassName = "but", Tlx = 402, Tly = 340, Width = 534-402, Height = 519-340, Color = "#0000FF" });
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
		CurrentImageBoundingBoxes.Where(x => x.ClassId == labelingClass.Id).ToList().ForEach(x => x.Color = labelingClass.Color);
	}

	public void SetSelectedLabel(LabelingClass labelingClass)
	{
		foreach (var label in LabelingClasses)
		{
			label.IsSelected = false;
		}
		labelingClass.IsSelected = true;
		selectedLabel = labelingClass;
	}
	public void SetSelectedAnnotation(BoundingBox boundingBox, bool value)
	{
		foreach (var bb in CurrentImageBoundingBoxes)
		{
			bb.IsSelected = false;
		}
		boundingBox.IsSelected = value;
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
		CurrentImageBoundingBoxes = databaseService.GetBoundingBoxes(CurrentImageIndex);
		if (CurrentImageBoundingBoxes.Count == 0)
		{
			OnForceNewBoundingBoxCollection?.Invoke(this, EventArgs.Empty);
		}
	}

	[RelayCommand]
	private void PreviousImage()
	{
		if (ImagesPaths.Count == 0)
			return;
		CurrentImageIndex = Math.Max(1, CurrentImageIndex - 1);
		CurrentImageAbsolutePath = System.IO.Path.Join(databaseService.absolutePath, ImagesPaths[CurrentImageIndex - 1]);
		CurrentImageBitmap = new Bitmap(CurrentImageAbsolutePath);
		CurrentImageBoundingBoxes = databaseService.GetBoundingBoxes(CurrentImageIndex);
		if (CurrentImageBoundingBoxes.Count == 0)
		{
			OnForceNewBoundingBoxCollection?.Invoke(this, EventArgs.Empty);
		}
	}

	public void GotoImage(int imageId)
	{
		//CurrentImageBoundingBoxes.Add(new BoundingBox { ClassId = 1, ClassName = "ballon", Tlx = 300, Tly = 350, Width = 600, Height = 200, Color = "#00FF00" });
		if (ImagesPaths.Count == 0)
			return;
		var clampedIndex = Math.Clamp(imageId, 1, ImagesPaths.Count);
		CurrentImageAbsolutePath = System.IO.Path.Join(databaseService.absolutePath, ImagesPaths[clampedIndex - 1]);
		CurrentImageBitmap = new Bitmap(CurrentImageAbsolutePath);
		CurrentImageBoundingBoxes = databaseService.GetBoundingBoxes(imageId);
		if (CurrentImageBoundingBoxes.Count == 0)
		{
			OnForceNewBoundingBoxCollection?.Invoke(this, EventArgs.Empty);
		}
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
		resizeLength = delta;

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
	private void ToggleSwitchChecked()
	{
		CurrentImageBoundingBoxes.ToList().ForEach(bbox => bbox.EditingEnabled = ResizingBoundingBoxEnabled);
	}

	public void OnCanvasPointerPressed(Point position)
	{
		if (!isDrawingNewBoundingBox)
		{
			if (selectedLabel != null)
			{
				startPoint = position;
				newBoundingBox = new BoundingBox
				{
					ClassId = selectedLabel.Id,
					Tlx = position.X, // Take into account the stroke thickness?
					Tly = position.Y, // Take into account the stroke thickness?
					Width = 1,
					Height = 1,
					Color = selectedLabel.Color,
					ClassName = selectedLabel.Name,
					EditingEnabled = ResizingBoundingBoxEnabled,
				};
				CurrentImageBoundingBoxes.Add(newBoundingBox);
			}
			isDrawingNewBoundingBox = true;
		}
		else
		{
			isDrawingNewBoundingBox = false;
			if (newBoundingBox != null)
			{
				databaseService.AddBoundingBox(newBoundingBox, CurrentImageIndex);
			}
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

		double newWidth = position.X - startPoint.X;
		double newHeight = position.Y - startPoint.Y;

		newBoundingBox.Width = newWidth;
		newBoundingBox.Height = newHeight;
	}

}
