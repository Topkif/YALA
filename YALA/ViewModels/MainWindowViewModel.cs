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
	[ObservableProperty] ObservableCollection<LabellingClass> labellingClasses = new();
	[ObservableProperty] ObservableCollection<string> imagesPaths = new();
	[ObservableProperty] int currentImageIndex = 1;
	[ObservableProperty] string currentImageAbsolutePath = "";
	[ObservableProperty] Bitmap currentImageBitmap;
	[ObservableProperty] bool resizingBoundingBoxEnabled = true;
	[ObservableProperty] bool drawingBoundingBoxEnabled = true;
	[ObservableProperty] string projectName = "YALA (No opened project)";

	// Variables for bounding box resizing
	BoundingBox resizingBoundingBox = new();
	public LabellingClass? selectedClass = null;
	ResizeDirection resizeDirection;
	double resizeLength;

	// Variables for boundingbox creation
	private bool isDrawingNewBoundingBox = false;
	private Point startPoint;
	private BoundingBox? newBoundingBox;

	public DatabaseService databaseService = new();

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
		LabellingClasses.Add(new LabellingClass { Id = 0, Name = "class1", Color = "#6eeb83", NumberOfInstances = 0, IsSelected = true });
		LabellingClasses.Add(new LabellingClass { Id = 1, Name = "class2", Color = "#3654b3", NumberOfInstances = 0, IsSelected = false });
		selectedClass = LabellingClasses.FirstOrDefault(x => x.IsSelected);
		CurrentImageBoundingBoxes = new();
	}

	public void CreateNewProject(string dbPath, string classesPath)
	{
		if (!databaseService.DoTablesExist(dbPath))
		{
			databaseService.CreateYalaTables(dbPath);
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
		LabellingClasses = databaseService.GetLabellingClasses();
		//databaseService.UpdateNumberOfInstances(LabellingClasses);
		ProjectName = $"YALA ({dbPath})";
	}

	public void CreateNewClass((string, string) tuple)
	{
		databaseService.AddClassesAndColor(new List<(string, string)> { tuple });
		if (databaseService?.connection?.State == System.Data.ConnectionState.Open)
		{
			LabellingClasses = databaseService.GetLabellingClasses();
			//LabellingClasses.ToList().ForEach(x => x.NumberOfInstances = databaseService.GetInstancesOfClass(x.Name)); // Class name can change so update the number of instances
		}
		else // Simulation mode
		{
			LabellingClass newClass = new() { Name = tuple.Item1 };
			newClass.SetColorSilently(tuple.Item2); // Set color without firing PropertyChanged
			LabellingClasses.Add(newClass);
			SetSelectedLabel(LabellingClasses.Last());
		}
	}
	public void OpenExistingProject(string dbPath)
	{
		databaseService.Close();
		if (!databaseService.DoTablesExist(dbPath))
		{
			databaseService.CreateYalaTables(dbPath);
		}
		LabellingClasses = databaseService.GetLabellingClasses();
		selectedClass = LabellingClasses.FirstOrDefault(x => x.IsSelected == true);
		ImagesPaths = databaseService.GetImagesPaths();
		CurrentImageIndex = 1;
		if (ImagesPaths.Count > 0 && CurrentImageIndex > 0)
		{
			CurrentImageAbsolutePath = System.IO.Path.Join(databaseService.absolutePath, ImagesPaths[0]); // Load the first image by default
			CurrentImageBitmap = new Bitmap(CurrentImageAbsolutePath);
			CurrentImageBoundingBoxes = databaseService.GetBoundingBoxes(ImagesPaths[CurrentImageIndex - 1], ResizingBoundingBoxEnabled);
		}
		ProjectName = $"YALA ({dbPath})";
	}

	[RelayCommand]
	private void CloseCurrentProject()
	{
		databaseService.Close();
		CurrentImageBitmap = new Bitmap("../../../Assets/notfound.png");
		CurrentImageBoundingBoxes.Clear();
		ImagesPaths.Clear();
		CurrentImageAbsolutePath = String.Empty;
		CurrentImageIndex = 1;
		LabellingClasses.Clear();
		ProjectName = "YALA (No opened project)";
	}

	public void UpdateClassColor(LabellingClass labelingClass)
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
			LabellingClasses = databaseService.GetLabellingClasses();
		}
		else // Simulation mode
		{
			LabellingClasses.Remove(selectedClass);
		}
		LabellingClasses.First().IsSelected = true; // Select the first class by default
		selectedClass = LabellingClasses.FirstOrDefault(x => x.IsSelected == true);
	}

	public void SetSelectedLabel(LabellingClass labelingClass)
	{
		foreach (var label in LabellingClasses)
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
			CurrentImageIndex = ImagesPaths.Count;
			CurrentImageAbsolutePath = System.IO.Path.Join(databaseService.absolutePath, ImagesPaths[CurrentImageIndex-1]); // Load the first image by default
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

	public void RemoveCurrentImageFromProject()
	{
		if (ImagesPaths.Count > 0 && CurrentImageIndex > 0 && !string.IsNullOrWhiteSpace(ImagesPaths[CurrentImageIndex - 1]))
		{
			databaseService.RemoveImage(ImagesPaths[CurrentImageIndex-1]);
			databaseService.UpdateNumberOfInstances(LabellingClasses);
			CurrentImageBoundingBoxes.Clear();
			ImagesPaths = databaseService.GetImagesPaths();
			if (ImagesPaths.Count == 0)
				return;
			if (CurrentImageIndex>ImagesPaths.Count)
				CurrentImageIndex=ImagesPaths.Count;
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

		double maxWidth = CurrentImageBitmap.Size.Width;
		double maxHeight = CurrentImageBitmap.Size.Height;

		switch (resizeDirection)
		{
			case ResizeDirection.Top:
				{
					double newTly = resizingBoundingBox.Tly + delta;
					newTly = Math.Max(0, newTly);
					double newHeight = resizingBoundingBox.Tly + resizingBoundingBox.Height - newTly;
					newHeight = Math.Max(1, Math.Min(newHeight, maxHeight - newTly));

					resizingBoundingBox.Tly = newTly;
					resizingBoundingBox.Height = newHeight;
					break;
				}
			case ResizeDirection.Bottom:
				{
					double newHeight = resizingBoundingBox.Height + delta;
					newHeight = Math.Max(1, Math.Min(newHeight, maxHeight - resizingBoundingBox.Tly));
					resizingBoundingBox.Height = newHeight;
					break;
				}
			case ResizeDirection.Left:
				{
					double newTlx = resizingBoundingBox.Tlx + delta;
					newTlx = Math.Max(0, newTlx);
					double newWidth = resizingBoundingBox.Tlx + resizingBoundingBox.Width - newTlx;
					newWidth = Math.Max(1, Math.Min(newWidth, maxWidth - newTlx));

					resizingBoundingBox.Tlx = newTlx;
					resizingBoundingBox.Width = newWidth;
					break;
				}
			case ResizeDirection.Right:
				{
					double newWidth = resizingBoundingBox.Width + delta;
					newWidth = Math.Max(1, Math.Min(newWidth, maxWidth - resizingBoundingBox.Tlx));
					resizingBoundingBox.Width = newWidth;
					break;
				}
		}
	}



	public void OnThumbDragCompleted()
	{
		if (CurrentImageBoundingBoxes.Remove(resizingBoundingBox))
		{
			CurrentImageBoundingBoxes.Add(resizingBoundingBox);
			databaseService.UpdateBoundingBox(resizingBoundingBox);
			// Class name can change so update the number of instances
			LabellingClass? target = LabellingClasses.FirstOrDefault(x => x.Name == resizingBoundingBox.ClassName);
			if (target is not null)
				target.NumberOfInstances = databaseService.GetInstancesOfClass(resizingBoundingBox.ClassName);
		}
	}

	public void DeleteBoundingBox(BoundingBox boundingBox)
	{
		if (ImagesPaths.Count > 0 && CurrentImageIndex > 0 && !string.IsNullOrWhiteSpace(ImagesPaths[CurrentImageIndex - 1]))
		{
			CurrentImageBoundingBoxes.Remove(boundingBox);
			databaseService.RemoveBoundingBox(boundingBox);
			// Update the number of instances
			LabellingClass? target = LabellingClasses.FirstOrDefault(x => x.Name == boundingBox.ClassName);
			if (target is not null)
				target.NumberOfInstances = databaseService.GetInstancesOfClass(boundingBox.ClassName);
		}
		else
		{
			CurrentImageBoundingBoxes.Remove(boundingBox);
		}
	}

	public void DeleteAllImageBoundingBox()
	{
		if (ImagesPaths.Count > 0 && CurrentImageIndex > 0 && !string.IsNullOrWhiteSpace(ImagesPaths[CurrentImageIndex - 1]))
		{
			CurrentImageBoundingBoxes.Clear();
			databaseService.RemoveAllImagesBoundingBoxes(ImagesPaths[CurrentImageIndex-1]);
			databaseService.UpdateNumberOfInstances(LabellingClasses);
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
			double threshold = 30; // Threshold to start drawing a BoundingBox in Pixels
			if (position.X < -threshold || position.Y < -threshold ||
				position.X >= CurrentImageBitmap.Size.Width + threshold ||
				position.Y >= CurrentImageBitmap.Size.Height + threshold)
				return;

			// Disable editing temporarily to be able to set a bounding close to another
			CurrentImageBoundingBoxes.ToList().ForEach(x => x.EditingEnabled = false);

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
				// Re enable editing if necessary
				CurrentImageBoundingBoxes.ToList().ForEach(x => x.EditingEnabled = ResizingBoundingBoxEnabled);
				if (ImagesPaths.Count > 0 && CurrentImageIndex > 0 && !string.IsNullOrWhiteSpace(ImagesPaths[CurrentImageIndex - 1]))
				{
					databaseService.AddBoundingBox(newBoundingBox, ImagesPaths[CurrentImageIndex - 1]);
					LabellingClass? target = LabellingClasses.FirstOrDefault(x => x.Name == newBoundingBox.ClassName);
					if (target is not null)
					{
						target.NumberOfInstances = databaseService.GetInstancesOfClass(newBoundingBox.ClassName);
					}
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
		// Re enable editing if necessary
		CurrentImageBoundingBoxes.ToList().ForEach(x => x.EditingEnabled = ResizingBoundingBoxEnabled);
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

	public void ExportProjectYolo(string exportPath, List<string> selectedClasses, double trainRatio, double valRatio, double testRatio)
	{
		YoloExporter.ExportProject(exportPath, databaseService, ImagesPaths.ToList(), selectedClasses, trainRatio, valRatio, testRatio);

	}

	public void MergeProjects(string incomingProjectPath, bool addClasses, bool addImages, bool addAnnotations, bool conflictKeepBoth, bool conflictKeepIncoming, bool conflictKeepCurrent)
	{
		ConflictKeepBehaviour conflictKeepBehaviour = (ConflictKeepBehaviour)(conflictKeepBoth == true ? 1 : conflictKeepIncoming == true ? 2 : conflictKeepCurrent == true ? 3 : 0);

		ProjectMerger.MergeProjects(databaseService,
			incomingProjectPath,
			addClasses,
			addImages,
			addAnnotations,
			conflictKeepBehaviour);

		if (addClasses)
		{
			LabellingClasses = databaseService.GetLabellingClasses();
		}
		if (addImages || addAnnotations)
		{
			ImagesPaths = databaseService.GetImagesPaths();
			if (ImagesPaths.Count > 0)
			{
				CurrentImageIndex = 1;
				CurrentImageAbsolutePath = System.IO.Path.Join(databaseService.absolutePath, ImagesPaths[0]); // Load the first image by default
				CurrentImageBitmap = new Bitmap(CurrentImageAbsolutePath);
				CurrentImageBoundingBoxes = databaseService.GetBoundingBoxes(ImagesPaths[CurrentImageIndex - 1], ResizingBoundingBoxEnabled);
			}
		}
	}

	public void SplitProject(List<int> splitImagesQuantities, bool randomize, string newBasePath)
	{
		ProjectSplitter.SplitProject(databaseService, splitImagesQuantities, randomize, newBasePath);
	}

}
