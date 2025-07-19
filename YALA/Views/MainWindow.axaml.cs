using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using YALA.Converters;
using YALA.Models;
using YALA.ViewModels;

namespace YALA.Views;
public partial class MainWindow : Window
{
	bool isCtrlKeyDown = false;
	Point lastMousePosition;
	public Point LastMousePosition
	{
		get => lastMousePosition;
		set
		{
			lastMousePosition = value;
			MousePositionLabel.Content = $"({Math.Clamp(Math.Floor(value.X),1,viewModel.CurrentImageBitmap.Size.Width)}," +
				$" {Math.Clamp(Math.Floor(value.Y),1,viewModel.CurrentImageBitmap.Size.Height)}";
		}
	}
	
	const double boundingBoxStrokeSize = 3.0;
	const double boundingBoxThumbSize = 20.0;
	public double RealBoundingBoxStrokeSize => boundingBoxStrokeSize / ZoomBorder.ZoomX;
	public double RealBoundingBoxThumbSize => boundingBoxThumbSize / ZoomBorder.ZoomX;
	public double MinusRealBoundingBoxMargin => -(RealBoundingBoxThumbSize - RealBoundingBoxStrokeSize)/ 2;


	private readonly MainWindowViewModel viewModel = App.MainVM;
	public MainWindow()
	{
		InitializeComponent();
		DataContext = viewModel;
		viewModel.BoundingBoxesChanged += () => Dispatcher.UIThread.Post(UpdateBoundingBoxes);
		this.Opened += (_, _) => MainFocusTarget.Focus();
		ImageIndexTextBox.AddHandler(TextBox.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
	}

	private void UpdateBoundingBoxes()
	{
		BoundingBoxesCanvas.Children.Clear();

		foreach (var bbox in viewModel.CurrentImageBoundingBoxes)
		{
			bbox.StrokeScaleFactor = ZoomBorder.ZoomX; // Stroke of 3 pixels by default at 100% zoom
			var control = new BoundingBoxControl
			{
				BoundingBox = bbox
			};

			Canvas.SetLeft(control, bbox.Tlx);
			Canvas.SetTop(control, bbox.Tly);

			bbox.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName is nameof(BoundingBox.Tlx))
					Canvas.SetLeft(control, bbox.Tlx);
				else if (e.PropertyName is nameof(BoundingBox.Tly))
					Canvas.SetTop(control, bbox.Tly);
			};

			BoundingBoxesCanvas.Children.Add(control);
		}
	}

	private void OnZoomChanged(object? sender, ZoomChangedEventArgs e)
	{
		UpdateBoundingBoxes();
	}

	private static void OnTextInput(object? sender, TextInputEventArgs e)
	{
		if (!int.TryParse(e.Text, out _))
			e.Handled = true;
	}

	private async void OnCreateNewProjectClick(object sender, RoutedEventArgs e)
	{
		var topLevel = GetTopLevel(this);
		if (topLevel == null)
			return;

		var files = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
		{
			Title = "Create New Project Database",
			DefaultExtension = "db",
			SuggestedFileName = "project.db",
			FileTypeChoices = new[]
			{
			new FilePickerFileType("SQLite Database") { Patterns = new[] { "*.db" } }
		}
		});

		if (files != null)
		{
			var classFiles = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = "Select Class Names File",
				AllowMultiple = false,
				FileTypeFilter = new[]
			{
				new FilePickerFileType("Class Files") { Patterns = new[] { "*.txt", "*.names", "*" } }
			}
			});

			// 3. If a file was selected, execute the command on the ViewModel
			if (classFiles?.Count > 0)
			{
				string dbPath = files.Path.LocalPath;
				string classesPath = classFiles[0].Path.LocalPath;
				viewModel.CreateNewProject(dbPath, classesPath);
			}
		}
	}

	private async void OnOpenExistingProjectClick(object sender, RoutedEventArgs e)
	{
		var topLevel = GetTopLevel(this);
		if (topLevel == null)
			return;

		var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Open Existing Project (.db)",
			AllowMultiple = false,
			FileTypeFilter = new[]
			{
			new FilePickerFileType("SQLite Database") { Patterns = new[] { "*.db" } }
		}
		});

		if (files?.Count > 0)
		{
			string path = files[0].Path.LocalPath;
			viewModel.OpenExistingProject(path);
		}
	}

	private void OnColorChanged(object sender, ColorChangedEventArgs e)
	{
		if (sender is ColorPicker picker && picker.DataContext is LabelingClass selectedClass)
		{
			var hexColor = e.NewColor.ToString(); // format: "#RRGGBBAA"
			selectedClass.Color = hexColor;

			if (DataContext is not MainWindowViewModel viewModel)
				return;
			viewModel.UpdateLabelColor(selectedClass);
		}
	}

	private void OnClassChecked(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleButton but && but.DataContext is LabelingClass selectedClass)
		{
			viewModel.SetSelectedLabel(selectedClass);
		}
	}

	private async void OnAddImagesClicked(object sender, RoutedEventArgs e)
	{
		var topLevel = GetTopLevel(this);
		if (topLevel == null)
			return;

		var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Select Images or Folder",
			AllowMultiple = true,
			FileTypeFilter = new[]
			{
			new FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg" } }
		}
		});

		if (files?.Count > 0)
		{
			List<string> imagePaths = files.Select(f => f.Path.LocalPath).ToList();
			viewModel.AddImages(imagePaths);
		}
	}

	private void OnKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Left)
		{
			viewModel.PreviousImageCommand.Execute(null);
			e.Handled = true;
		}
		else if (e.Key == Key.Right)
		{
			viewModel.NextImageCommand.Execute(null);
			e.Handled = true;
		}
		else if (e.Key == Key.Escape)
		{
			viewModel.CancelBoundingBoxDrawing();
		}
		else if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
		{
			isCtrlKeyDown = true;
		}
	}
	private void OnKeyUp(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
		{
			isCtrlKeyDown = false;
		}
	}
	private void OnMenuKeyDown(object? sender, KeyEventArgs e)
	{
		e.Handled = true; // Consume all key input
	}

	private void OnCurrentImageIndexChanged(object? sender, RoutedEventArgs e)
	{
		if (sender is TextBox textBox)
		{
			if (int.TryParse(textBox.Text, out int index))
			{
				viewModel.GotoImage(index);
			}
		}
	}

	private void OnDeleteBoundingBoxClicked(object? sender, RoutedEventArgs e)
	{
		if (sender is Button button && button.Tag is BoundingBox bbox)
		{
			viewModel.DeleteBoundingBox(bbox);
		}
	}

	private void WindowPointerMoved(object? sender, PointerEventArgs e)
	{
		var window = (Window)sender!;
		Point mousePosInWindow = e.GetPosition(window);

		Point? canvasTopLeftInWindow = BoundingBoxesCanvas.TranslatePoint(new Point(0, 0), window);
		if (canvasTopLeftInWindow is { } canvasOffset)
		{
			var transform = MainImage.TransformToVisual(ImageLayer);
			if (transform is not null)
			{
				var matrix = transform.Value;
				double scaleX = matrix.M11;
				double scaleY = matrix.M22;
				double uniformScale = Math.Min(scaleX, scaleY);

				LastMousePosition = (mousePosInWindow - canvasOffset)/ZoomBorder.ZoomY/uniformScale;
				viewModel.OnCanvasPointerMoved(LastMousePosition);
			}
		}
	}
	private void WindowPointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (e.Properties.IsLeftButtonPressed)
		{
			viewModel.OnCanvasPointerPressed(LastMousePosition);
		}
	}

	private void ZoomExtentsCanva(object? sender, RoutedEventArgs e)
	{
		ZoomBorder.ResetMatrix();
		ZoomBorder.ZoomTo(1, ImageLayer.Bounds.Width/2, ImageLayer.Bounds.Height/2);
	}

	private void OnAnnotationChecked(object? sender, RoutedEventArgs e)
	{
		if (sender is ToggleButton button && button.DataContext is BoundingBox bb)
		{
			viewModel.SetSelectedAnnotation(bb, true);
		}
	}
	private void OnAnnotationUnchecked(object? sender, RoutedEventArgs e)
	{
		if (sender is ToggleButton button && button.DataContext is BoundingBox bb)
		{
			viewModel.SetSelectedAnnotation(bb, false);
		}
	}
}