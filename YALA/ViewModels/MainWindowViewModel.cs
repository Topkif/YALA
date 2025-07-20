using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using YALA.Models;
using YALA.Services;

namespace YALA.ViewModels;
public partial class MainWindowViewModel : ViewModelBase
{
	[ObservableProperty] ObservableCollection<LabelingClass> labelingClasses = new();
	[ObservableProperty] ObservableCollection<string> imagesPaths = new();
	[ObservableProperty] int currentImageIndex = 1;
	[ObservableProperty] string currentImageAbsolutePath = "";
	[ObservableProperty] Bitmap currentImageBitmap;
	[ObservableProperty] bool resizingBoundingBoxEnabled;
	[ObservableProperty] bool drawingBoundingBoxEnabled = true;

	// Variables for bounding box resizing
	BoundingBox resizingBoundingBox = new();
	public LabelingClass? selectedClass = null;
	ResizeDirection resizeDirection;
	double resizeLength;

	// Varables for boundingbox creation
	private bool isDrawingNewBoundingBox = false;
	private Point startPoint;
	private BoundingBox? newBoundingBox;

	DatabaseService databaseService = new();

	// Force notify collection changed event when CurrentImageBoundingBoxes is changed by subscribing to the new collection
	private NotifyCollectionChangedEventHandler? _collectionChangedHandler;
	public event Action? BoundingBoxesChanged;
	private ObservableCollection<BoundingBox> _currentImageBoundingBoxes = new();
	public ObservableCollection<BoundingBox> CurrentImageBoundingBoxes
	{
		get => _currentImageBoundingBoxes;
		set
		{
			if (_currentImageBoundingBoxes != value)
			{
				if (_collectionChangedHandler != null)
				{
					_currentImageBoundingBoxes.CollectionChanged -= _collectionChangedHandler;
				}
				_currentImageBoundingBoxes = value;
				_collectionChangedHandler = (_, _) => BoundingBoxesChanged?.Invoke();
				_currentImageBoundingBoxes.CollectionChanged += _collectionChangedHandler;

				OnPropertyChanged(nameof(CurrentImageBoundingBoxes));
				BoundingBoxesChanged?.Invoke();
			}
		}
	}


	public MainWindowViewModel()
	{
		// Load some default values to test
		CurrentImageBitmap = new Bitmap("../../../Assets/notfound.png");
		LabelingClasses.Add(new LabelingClass { Id = 0, Name = "class1", Color = "#6eeb83", NumberOfInstances = 0, IsSelected = true });
		LabelingClasses.Add(new LabelingClass { Id = 1, Name = "class2", Color = "#3654b3", NumberOfInstances = 0, IsSelected = false });
		selectedClass = LabelingClasses.FirstOrDefault(x => x.IsSelected);
		CurrentImageBoundingBoxes = new();
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
			if (classesPath.EndsWith(".yalac"))
			{
				List<(string, string)> classes = ClassesFileParser.ParseYalaClassNamesAndColor(classesPath);
				databaseService.AddClassesAndColor(classes);
			}
			else
			{
				List<string> classes = ClassesFileParser.ParseClassNames(classesPath);
				databaseService.AddClasses(classes);
			}
		}
		LabelingClasses = databaseService.GetLabellingClasses();
	}

	public void CreateNewClass((string, string) tuple)
	{
		databaseService.AddClassesAndColor(new List<(string, string)> { tuple });
		if (databaseService?.connection?.State == System.Data.ConnectionState.Open)
		{
			LabelingClasses = databaseService.GetLabellingClasses();
		}
		else // Simulation mode
		{

			LabelingClass newClass = new() { Name = tuple.Item1 };
			newClass.SetColorSilently(tuple.Item2); // Set color without firing PropertyChanged
			LabelingClasses.Add(newClass);
			SetSelectedLabel(LabelingClasses.Last());
		}
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
		selectedClass = LabelingClasses.FirstOrDefault(x => x.IsSelected == true);
		ImagesPaths = databaseService.GetImagesPaths();
		if (ImagesPaths.Count > 0 && CurrentImageIndex > 0)
		{
			CurrentImageAbsolutePath = System.IO.Path.Join(databaseService.absolutePath, ImagesPaths[0]); // Load the first image by default
			CurrentImageBitmap = new Bitmap(CurrentImageAbsolutePath);
			CurrentImageBoundingBoxes = databaseService.GetBoundingBoxes(ImagesPaths[CurrentImageIndex - 1], ResizingBoundingBoxEnabled);
		}
	}

	[RelayCommand]
	private void CloseCurrentProject()
	{
		databaseService.Close();
	}

	public void UpdateClassColor(LabelingClass labelingClass)
	{
		databaseService.SetClassColor(labelingClass.Name, labelingClass.Color);
		CurrentImageBoundingBoxes.Where(x => x.ClassName == labelingClass.Name).ToList().ForEach(x => x.Color = labelingClass.Color);
	}

	public void DeleteSelectedClassFromProject()
	{
		if (selectedClass == null)
			return;
		databaseService.DeleteClass(selectedClass);
		if (databaseService?.connection?.State == System.Data.ConnectionState.Open)
		{
			LabelingClasses = databaseService.GetLabellingClasses();
		}
		else // Simulation mode
		{
			LabelingClasses.Remove(selectedClass);
		}
		LabelingClasses.First().IsSelected = true; // Select the first class by default
		selectedClass = LabelingClasses.FirstOrDefault(x => x.IsSelected == true);
	}

	public void SetSelectedLabel(LabelingClass labelingClass)
	{
		foreach (var label in LabelingClasses)
		{
			label.IsSelected = false;
		}
		labelingClass.IsSelected = true;
		selectedClass = labelingClass;
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
		if (ImagesPaths.Count > 0 && CurrentImageIndex > 0 && !string.IsNullOrWhiteSpace(ImagesPaths[CurrentImageIndex - 1]))
		{
			CurrentImageIndex = Math.Min(ImagesPaths.Count, CurrentImageIndex + 1);
			CurrentImageAbsolutePath = System.IO.Path.Join(databaseService.absolutePath, ImagesPaths[CurrentImageIndex - 1]);
			CurrentImageBitmap = new Bitmap(CurrentImageAbsolutePath);
			CurrentImageBoundingBoxes = databaseService.GetBoundingBoxes(ImagesPaths[CurrentImageIndex - 1], ResizingBoundingBoxEnabled);
		}
	}

	[RelayCommand]
	private void PreviousImage()
	{
		if (ImagesPaths.Count > 0 && CurrentImageIndex > 0 && !string.IsNullOrWhiteSpace(ImagesPaths[CurrentImageIndex - 1]))
		{
			CurrentImageIndex = Math.Max(1, CurrentImageIndex - 1);
			CurrentImageAbsolutePath = System.IO.Path.Join(databaseService.absolutePath, ImagesPaths[CurrentImageIndex - 1]);
			CurrentImageBitmap = new Bitmap(CurrentImageAbsolutePath);
			CurrentImageBoundingBoxes = databaseService.GetBoundingBoxes(ImagesPaths[CurrentImageIndex - 1], ResizingBoundingBoxEnabled);
		}
	}

	[RelayCommand]
	private void RemoveCurrentImageFromProject()
	{
		if (ImagesPaths.Count > 0 && CurrentImageIndex > 0 && !string.IsNullOrWhiteSpace(ImagesPaths[CurrentImageIndex - 1]))
		{
			databaseService.RemoveImage(ImagesPaths[CurrentImageIndex-1]);
			CurrentImageBoundingBoxes.Clear();
			ImagesPaths = databaseService.GetImagesPaths();
			if (ImagesPaths.Count == 0)
				return;
			CurrentImageAbsolutePath = System.IO.Path.Join(databaseService.absolutePath, ImagesPaths[CurrentImageIndex-1]); // Load the first image by default
			CurrentImageBitmap = new Bitmap(CurrentImageAbsolutePath);
		}

	}

	public void GotoImage(int imageId)
	{
		if (ImagesPaths.Count > 0 && CurrentImageIndex > 0)
		{
			var clampedIndex = Math.Clamp(imageId, 1, ImagesPaths.Count);
			CurrentImageAbsolutePath = System.IO.Path.Join(databaseService.absolutePath, ImagesPaths[clampedIndex - 1]);
			CurrentImageBitmap = new Bitmap(CurrentImageAbsolutePath);
			CurrentImageBoundingBoxes = databaseService.GetBoundingBoxes(ImagesPaths[clampedIndex - 1], ResizingBoundingBoxEnabled);
		}
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
		if (ImagesPaths.Count > 0 && CurrentImageIndex > 0 && !string.IsNullOrWhiteSpace(ImagesPaths[CurrentImageIndex - 1]))
		{
			CurrentImageBoundingBoxes.Remove(boundingBox);
			databaseService.RemoveBoundingBox(boundingBox, ImagesPaths[CurrentImageIndex-1]);
		}
		else
		{
			CurrentImageBoundingBoxes.Remove(boundingBox);
		}
	}

	[RelayCommand]
	private void DeleteAllImageBoundingBox()
	{
		if (ImagesPaths.Count > 0 && CurrentImageIndex > 0 && !string.IsNullOrWhiteSpace(ImagesPaths[CurrentImageIndex - 1]))
		{
			CurrentImageBoundingBoxes.Clear();
			databaseService.RemoveAllImagesBoundingBoxes(ImagesPaths[CurrentImageIndex-1]);
		}
		else
		{
			CurrentImageBoundingBoxes.Clear();
		}
	}

	[RelayCommand]
	private void ToggleSwitchChecked()
	{
		CurrentImageBoundingBoxes.ToList().ForEach(bbox => bbox.EditingEnabled = ResizingBoundingBoxEnabled);
	}

	public void OnPointerPressed(Point position)
	{
		if (!isDrawingNewBoundingBox && DrawingBoundingBoxEnabled)
		{
			// Check if pointer is inside the CurrentImageBitmap within threshold
			double threshold = 20.0; // Threshold to start drawing a BoundingBox in Pixels
			if (position.X < -threshold || position.Y < -threshold ||
				position.X >= CurrentImageBitmap.Size.Width + threshold ||
				position.Y >= CurrentImageBitmap.Size.Height + threshold)
				return;


			if (selectedClass != null)
			{
				startPoint = position;
				newBoundingBox = new BoundingBox
				{
					ClassId = selectedClass.Id,
					Tlx = position.X, // Take into account the stroke thickness?
					Tly = position.Y, // Take into account the stroke thickness?
					Width = 1,
					Height = 1,
					Color = selectedClass.Color,
					ClassName = selectedClass.Name,
					EditingEnabled = false, // Otherwise it tries to resize itself when creating
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
				newBoundingBox.EditingEnabled = ResizingBoundingBoxEnabled;
				if (ImagesPaths.Count > 0 && CurrentImageIndex > 0 && !string.IsNullOrWhiteSpace(ImagesPaths[CurrentImageIndex - 1]))
				{
					databaseService.AddBoundingBox(newBoundingBox, ImagesPaths[CurrentImageIndex - 1]);
				}
			}
			newBoundingBox = null;
		}
	}

	public void OnPointerMoved(Point position)
	{
		if (!isDrawingNewBoundingBox || newBoundingBox == null)
			return;
		double newTlx, newTly, newWidth, newHeight;

		// Case if position is at right and bottom of the startPoint
		if (position.X > startPoint.X && position.Y > startPoint.Y)
		{
			newTlx = startPoint.X;
			newTly = startPoint.Y;
			newWidth = position.X - startPoint.X;
			newHeight = position.Y - startPoint.Y;
		}
		// case if position is at right and top of the startPoint
		else if (position.X > startPoint.X && position.Y < startPoint.Y)
		{
			newTlx = startPoint.X;
			newTly = position.Y;
			newWidth = Math.Max(0, position.X) - startPoint.X;
			newHeight = startPoint.Y - Math.Max(0, position.Y);
		}
		// Case if position is at left and bottom of the startPoint
		else if (position.X < startPoint.X && position.Y > startPoint.Y)
		{
			newTlx = position.X;
			newTly = startPoint.Y;
			newWidth = startPoint.X - Math.Max(0, position.X);
			newHeight = Math.Max(0, position.Y) - startPoint.Y;
		}
		// Case if position is at left or top of the startPoint
		else
		{
			newTlx = position.X;
			newTly = position.Y;
			newWidth = startPoint.X - Math.Max(0, position.X);
			newHeight = startPoint.Y - Math.Max(0, position.Y);
		}
		newBoundingBox.Tlx = Math.Clamp(newTlx, 0, CurrentImageBitmap.Size.Width - 1);
		newBoundingBox.Tly = Math.Clamp(newTly, 0, CurrentImageBitmap.Size.Height - 1);
		newBoundingBox.Width = Math.Clamp(newWidth, 1, CurrentImageBitmap.Size.Width - newBoundingBox.Tlx);
		newBoundingBox.Height = Math.Clamp(newHeight, 1, CurrentImageBitmap.Size.Height - newBoundingBox.Tly);
	}
	public void CancelBoundingBoxDrawing()
	{
		isDrawingNewBoundingBox = false;
		if (newBoundingBox == null)
			return;
		CurrentImageBoundingBoxes.Remove(newBoundingBox);
		newBoundingBox = null;
	}

	public void ExportProjectClasses(string path)
	{
		var classes = databaseService.GetLabellingClasses();
		if (classes.Count>0)
		{
			using var writer = new StreamWriter(path, false, Encoding.UTF8);
			foreach (var cls in classes)
			{
				writer.WriteLine($"{cls.Name};{cls.Color}");
			}
		}
	}
}
