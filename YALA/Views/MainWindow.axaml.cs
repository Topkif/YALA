using Avalonia;
using Avalonia.Controls;
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
	private double currentImageScale = 1.0;
	private const double ZoomFactor = 1.1;
	private readonly ScaleTransform imageScaleTransform = new();
	private readonly TranslateTransform imageLayerTranslate = new();

	bool isCtrlKeyDown = false;
	Point lastMousePosition;

	private readonly MainWindowViewModel viewModel = App.MainVM;
	public MainWindow()
	{
		InitializeComponent();
		DataContext = viewModel;
		viewModel.CurrentImageBoundingBoxes.CollectionChanged += (_, _) => Dispatcher.UIThread.Post(UpdateBoundingBoxes);

		var transformGroup = new TransformGroup
		{
			Children =
		{
			imageScaleTransform,
			imageLayerTranslate
		}
		};
		ImageLayer.RenderTransform = transformGroup;

		RootGrid.AttachedToVisualTree += (_, __) =>
		{
			RootGrid.LayoutUpdated += (_, __) =>
			{
				ImageContainer.MaxWidth = RootGrid.Bounds.Width;
				ImageContainer.MaxHeight = RootGrid.Bounds.Height;
			};
		};

		this.Opened += (_, _) => MainFocusTarget.Focus();
		var tb = this.FindControl<TextBox>("ImageIndexTextBox");
		tb.AddHandler(TextBox.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
	}

	private void UpdateBoundingBoxes()
	{
		BoundingBoxesCanvas.Children.Clear();

		foreach (var bbox in viewModel.CurrentImageBoundingBoxes)
		{
			var control = new BoundingBoxControl
			{
				BoundingBox = bbox
			};

			Canvas.SetLeft(control, bbox.Tlx-2); // Stroke size in View
			Canvas.SetTop(control, bbox.Tly-2); // Stroke size in View

			bbox.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName is nameof(BoundingBox.Tlx))
					Canvas.SetLeft(control, bbox.Tlx - 2); // Stroke size
				else if (e.PropertyName is nameof(BoundingBox.Tly))
					Canvas.SetTop(control, bbox.Tly - 2); // Stroke size
			};

			BoundingBoxesCanvas.Children.Add(control);
		}
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
		if (sender is RadioButton rb && rb.DataContext is LabelingClass selectedClass)
		{
			viewModel.SetSelectedLabel(selectedClass);
		}
		else if (sender is Button but && but.DataContext is LabelingClass selectedClass2)
		{
			viewModel.SetSelectedLabel(selectedClass2);
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

	private void OnImagePointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (viewModel?.CurrentImageBitmap == null || sender is not Image image)
			return;

		var clickProperties = e.GetCurrentPoint(image).Properties;

		// Get the position of the click relative to the image control
		var controlPoint = e.GetPosition(image);

		// Convert control point to pixel space of the bitmap
		var bmpSize = viewModel.CurrentImageBitmap.PixelSize;
		var imageWidth = image.Bounds.Width;
		var imageHeight = image.Bounds.Height;

		var scaleX = bmpSize.Width / imageWidth;
		var scaleY = bmpSize.Height / imageHeight;
		var imagePoint = new Point(controlPoint.X * scaleX, controlPoint.Y * scaleY);

		if (clickProperties.IsLeftButtonPressed)
		{
			viewModel.OnImageLeftClickedReceived(imagePoint);
		}
		else if (clickProperties.IsRightButtonPressed)
		{
			viewModel.OnImageRightClickedReceived(imagePoint);
		}
	}

	private void OnDeleteBoundingBoxClicked(object? sender, RoutedEventArgs e)
	{
		if (sender is Button button && button.Tag is BoundingBox bbox)
		{
			viewModel.DeleteBoundingBox(bbox);
		}
	}
	private void OnEditBoundingBoxClicked(object? sender, RoutedEventArgs e)
	{
		if (sender is Button button && button.Tag is BoundingBox bbox)
		{
			viewModel.OnEditBoundingBoxClicked(bbox, true);
		}
	}

	private void ShowAllEditingThumbsChecked(object? sender, RoutedEventArgs e)
	{
		viewModel.ToggleSwitchCheckedCommand.Execute(true);
	}

	private void ShowAllEditingThumbsUnchecked(object? sender, RoutedEventArgs e)
	{
		viewModel.ToggleSwitchCheckedCommand.Execute(false);
	}


	private void BoundingBoxesCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
	{
		var point = e.GetPosition((Canvas)sender!);
		viewModel.OnCanvasPointerPressed(point);
	}

	private void BoundingBoxesCanvas_PointerMoved(object? sender, PointerEventArgs e)
	{
		var point = e.GetPosition((Canvas)sender!);
		lastMousePosition = point; // Store last mouse position
		viewModel.OnCanvasPointerMoved(point);
	}

	private void RootGrid_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
	{
		if (!isCtrlKeyDown)
			return; // Only zoom when Ctrl is pressed

		e.Handled = true;

		var oldScale = currentImageScale;
		var delta = e.Delta.Y;

		currentImageScale *= delta > 0 ? ZoomFactor : 1 / ZoomFactor;
		currentImageScale = Math.Clamp(currentImageScale, 0.1, 10);

		// Calculate mouse position relative to content before scale change
		var relativeX = lastMousePosition.X;
		var relativeY = lastMousePosition.Y;

		// Calculate absolute offset before scale change
		var absX = relativeX * oldScale + imageLayerTranslate.X;
		var absY = relativeY * oldScale + imageLayerTranslate.Y;

		// Update scale
		imageScaleTransform.ScaleX = currentImageScale;
		imageScaleTransform.ScaleY = currentImageScale;

		// Calculate new offset to keep mouse at same position
		imageLayerTranslate.X = absX - relativeX * currentImageScale;
		imageLayerTranslate.Y = absY - relativeY * currentImageScale;

		imageLayerTranslate.X = imageLayerTranslate.X;
		imageLayerTranslate.Y = imageLayerTranslate.Y;
	}

	private void RootGrid_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
		{
			viewModel.CancelBoundingBoxDrawing();
		}
		else if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
		{
			isCtrlKeyDown = true;
		}
	}
	private void RootGrid_KeyUp(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
		{
			isCtrlKeyDown = false;
		}
	}
}