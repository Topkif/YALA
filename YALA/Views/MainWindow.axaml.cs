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
using Avalonia.VisualTree;
using DialogHostAvalonia;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using YALA.Converters;
using YALA.Models;
using YALA.Services;
using YALA.ViewModels;
using static System.Net.Mime.MediaTypeNames;

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
			MousePositionLabel.Content = $"({Math.Clamp(Math.Floor(value.X), 1, viewModel.CurrentImageBitmap.Size.Width)}," +
				$" {Math.Clamp(Math.Floor(value.Y), 1, viewModel.CurrentImageBitmap.Size.Height)})";
		}
	}

	const double boundingBoxStrokeSize = 3.0;
	const double boundingBoxThumbSize = 30.0;
	const double classNameSize = 18.0;
	const double classNameMargin = 5.0;
	public double RealBoundingBoxStrokeSize => boundingBoxStrokeSize / ZoomBorder.ZoomX;
	public double RealBoundingBoxThumbSize => boundingBoxThumbSize / ZoomBorder.ZoomX;
	public double MinusRealBoundingBoxMargin => -(RealBoundingBoxThumbSize - RealBoundingBoxStrokeSize)/ 2;
	public double RealClassNameSize => classNameSize / ZoomBorder.ZoomX;
	public Thickness RealClassNameMargin => new(classNameMargin / ZoomBorder.ZoomX);
	private CancellationTokenSource? yoloCancellationToken;


	private readonly MainWindowViewModel viewModel = App.MainVM;
	public MainWindow()
	{
		InitializeComponent();
		DataContext = viewModel;
		viewModel.ShowWarningDialogEvent += OnWarningDialogEventReceived;

		viewModel.BoundingBoxesChanged += () => Dispatcher.UIThread.Post(UpdateBoundingBoxes);
		this.Opened += (_, _) => MainFocusTarget.Focus();
		ImageIndexTextBox.AddHandler(TextBox.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
	}

	private void UpdateBoundingBoxes()
	{
		AnnotationsItemsControl.ItemsSource = viewModel.CurrentImageBoundingBoxes.Reverse();
		BoundingBoxesCanvas.Children.Clear();
		foreach (var bbox in viewModel.CurrentImageBoundingBoxes)
		{
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

		var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
		{
			Title = "Create New Project Database",
			DefaultExtension = "yala",
			SuggestedFileName = "project.yala",
			FileTypeChoices = new[]
			{
			new FilePickerFileType("SQLite Database") { Patterns = new[] { "*.yala" } }
		}
		});

		if (file != null)
		{
			string dbPath = file.Path.LocalPath;
			viewModel.CreateNewProject(dbPath);
		}
	}

	private async void OnOpenExistingProjectClick(object sender, RoutedEventArgs e)
	{
		var topLevel = GetTopLevel(this);
		if (topLevel == null)
			return;

		var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Open Existing Project (.yala)",
			AllowMultiple = false,
			FileTypeFilter = new[]
			{
			new FilePickerFileType("SQLite Database") { Patterns = new[] { "*.yala" } }
		}
		});

		if (files?.Count > 0)
		{
			string path = files[0].Path.LocalPath;
			viewModel.OpenExistingProject(path);
		}
	}

	private async void OnLoadYoloModelClicked(object sender, RoutedEventArgs e)
	{
		var yoloDialog = new ConfigureYoloDialog(
			viewModel.yoloOnnxService.modelPath ?? "",
			viewModel.yoloOnnxService.iouThreshold,
			viewModel.yoloOnnxService.confThreshold);
		var yoloResult = await DialogHost.Show(yoloDialog, "RootDialog");
		if (yoloResult is ConfigureYoloDialog yoloConfig)
		{
			if (yoloConfig.IncomingPath != null)
			{
				viewModel.yoloOnnxService.LoadOnnxModel(
					yoloConfig.IncomingPath,
					yoloConfig.IouThreshold,
					yoloConfig.ConfidenceThreshold);
			}
		}
	}

	private async void OnExportClassesClicked(object sender, RoutedEventArgs e)
	{
		var topLevel = GetTopLevel(this);
		if (topLevel == null)
			return;

		var classFile = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
		{
			Title = "Create project class names file",
			DefaultExtension = "yalac",
			SuggestedFileName = "project_classes.yalac",
			FileTypeChoices = new[]
			{
			new FilePickerFileType("YALA Classes") { Patterns = new[] { "*.yalac" } }
		}
		});

		if (classFile != null)
		{
			string path = classFile.Path.LocalPath;
			viewModel.ExportProjectClasses(path);
		}
	}

	private async void OnExportProjectYoloClicked(object sender, RoutedEventArgs e)
	{
		var dialog = new ExportProjectDialog(viewModel.ImagesPaths.Count, viewModel.LabellingClasses.Select(c => c.Name));
		var result = await DialogHost.Show(dialog, "RootDialog");

		if (result is ExportProjectDialog exportDialog)
		{
			viewModel.ExportProjectYolo(
				exportDialog.ExportPath,
				exportDialog.SelectedClasses,
				exportDialog.TrainRatio,
				exportDialog.ValRatio,
				exportDialog.TestRatio,
				exportDialog.GenerateDatasetYamlCheckBox.IsChecked == true ? true : false
			);
		}
	}

	private async void DeleteAllImageBoundingBox(object sender, RoutedEventArgs e)
	{
		string message = $"Are you sure you want to delete all annotations for the current image?";
		var dialog = new ConfirmDialog(message);
		var result = await DialogHost.Show(dialog, "RootDialog");

		if (result is bool confirmed && confirmed)
		{
			viewModel.DeleteAllImageBoundingBox();
		}
	}

	private async void OnDeleteSelectedClassClicked(object sender, RoutedEventArgs e)
	{
		var className = viewModel.selectedClass?.Name ?? "this class";
		string message = $"Are you sure you want to delete the class \"{className}\" and all associated annotations?";
		var dialog = new ConfirmDialog(message);
		var result = await DialogHost.Show(dialog, "RootDialog");

		if (result is bool confirmed && confirmed)
		{
			viewModel.DeleteSelectedClassFromProject();
		}
	}

	private async void OnAddClassClicked(object sender, RoutedEventArgs e)
	{
		var dialog = new NewClassDialog();
		var result = await DialogHost.Show(dialog, "RootDialog");

		if (result is ValueTuple<string, string> tuple)
		{
			var (className, hexColor) = tuple;
			viewModel.CreateNewClass(tuple);
		}
	}
	private void OnColorChanged(object sender, ColorChangedEventArgs e)
	{
		if (sender is ColorPicker picker && picker.DataContext is LabellingClass selectedClass)
		{
			var hexColor = e.NewColor.ToString(); // format: "#RRGGBBAA"
			selectedClass.Color = hexColor;

			if (DataContext is not MainWindowViewModel viewModel)
				return;
			viewModel.UpdateClassColor(selectedClass);
		}
	}

	private void OnClassChecked(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleButton but && but.DataContext is LabellingClass selectedClass)
		{
			viewModel.SetSelectedLabel(selectedClass);
		}
	}

	private async void OnImportImagesAnnotationsClicked(object sender, RoutedEventArgs e)
	{
		if (viewModel.databaseService.connection == null || viewModel.databaseService.connection.State == System.Data.ConnectionState.Closed)
		{
			var dialogWarning = new WarningDialog("No Current Project", "Please open or create a project before.");
			var resultWarning = await DialogHost.Show(dialogWarning, "RootDialog");
			return;
		}

		var dialog = new ImportDialog(viewModel.LabellingClasses.Count>0);
		var result = await DialogHost.Show(dialog, "RootDialog");
		if (result is ImportDialog importDialog)
		{
			if (importDialog.classFilePath != null)
				viewModel.ImportClassFile(importDialog.classFilePath);
			if (importDialog.imagesPaths != null)
				viewModel.AddImages(importDialog.imagesPaths);
			if (importDialog.annotationsPaths != null)
				viewModel.AddAnnotations(importDialog.annotationsPaths);
		}
	}

	private void OnKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Left)
		{
			viewModel.PreviousImageCommand.Execute(null);
			ZoomBorder.ResetMatrix();
			viewModel.CancelBoundingBoxDrawing();
			e.Handled = true;
		}
		else if (e.Key == Key.Right)
		{
			viewModel.NextImageCommand.Execute(null);
			ZoomBorder.ResetMatrix();
			viewModel.CancelBoundingBoxDrawing();
			e.Handled = true;
		}
		else if (e.Key == Key.Space)
		{
			ZoomBorder.ResetMatrix();
		}
		else if (e.Key == Key.Escape)
		{
			viewModel.CancelBoundingBoxDrawing();
			viewModel.UnselectBoundingBox();
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
				ZoomBorder.ResetMatrix();
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
		var window = (Grid)sender!;
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
				viewModel.OnPointerMoved(LastMousePosition);
			}
		}
	}
	private void WindowPointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (IsPointerInMenu(e))
			return;
		if (e.Properties.IsLeftButtonPressed)
		{
			viewModel.OnPointerPressed(LastMousePosition);
		}
		else if (e.Properties.IsRightButtonPressed)
		{
			viewModel.CancelBoundingBoxDrawing();
		}
		else if (e.Properties.IsXButton1Pressed)
		{
			viewModel.PreviousImageCommand.Execute(null);
		}
		else if (e.Properties.IsXButton2Pressed)
		{
			viewModel.NextImageCommand.Execute(null);
		}
		else if (e.Properties.IsMiddleButtonPressed)
		{
			BoundingBoxesCanvas.Cursor = new Cursor(StandardCursorType.Hand);
		}
	}
	private void WindowPointerReleased(object? sender, PointerReleasedEventArgs e)
	{
		if (IsPointerInMenu(e))
			return;
		if (viewModel.ResizingBoundingBoxEnabled)
		{
			BoundingBoxesCanvas.Cursor = new Cursor(StandardCursorType.Cross);
		}
		else
		{
			BoundingBoxesCanvas.Cursor = new Cursor(StandardCursorType.Arrow);
		}
	}

	private bool IsPointerInMenu(PointerEventArgs e)
	{
		// Walk up the visual tree from the source
		Visual? current = e.Source as Visual;

		while (current != null)
		{
			if (current is Menu || current is MenuItem)
				return true;

			current = current.GetVisualParent();
		}

		return false;
	}

	private async void RemoveCurrentImageFromProjectClicked(object? sender, RoutedEventArgs e)
	{
		string message = $"Are you sure you want to delete this image and all associated annotations?";
		var dialog = new ConfirmDialog(message);
		var result = await DialogHost.Show(dialog, "RootDialog");

		if (result is bool confirmed && confirmed)
		{
			viewModel.RemoveCurrentImageFromProject();
		}
	}

	private async void RunYoloOnImageClicked(object? sender, RoutedEventArgs e)
	{
		if (viewModel.yoloOnnxService.modelPath == null) // If the model is not yet initialised
		{
			var yoloDialog = new ConfigureYoloDialog("", viewModel.yoloOnnxService.iouThreshold, viewModel.yoloOnnxService.confThreshold);
			var yoloResult = await DialogHost.Show(yoloDialog, "RootDialog");
			if (yoloResult is ConfigureYoloDialog yoloConfig)
			{
				if (yoloConfig.IncomingPath != null)
				{
					viewModel.yoloOnnxService.LoadOnnxModel(
						yoloConfig.IncomingPath,
						yoloConfig.IouThreshold,
						yoloConfig.ConfidenceThreshold);
				}
			}
		}
		viewModel.RunYoloOnImage();
	}
	private async void RunYoloOnProjectClicked(object? sender, RoutedEventArgs e)
	{
		if (viewModel.yoloOnnxService.modelPath == null) // If the model is not yet initialised
		{
			var yoloDialog = new ConfigureYoloDialog("", viewModel.yoloOnnxService.iouThreshold, viewModel.yoloOnnxService.confThreshold);
			var yoloResult = await DialogHost.Show(yoloDialog, "RootDialog");
			if (yoloResult is ConfigureYoloDialog yoloConfig)
			{
				if (yoloConfig.IncomingPath != null)
				{
					viewModel.yoloOnnxService.LoadOnnxModel(
						yoloConfig.IncomingPath,
						yoloConfig.IouThreshold,
						yoloConfig.ConfidenceThreshold);
				}
			}
		}

		yoloCancellationToken = new CancellationTokenSource();
		var confirmDialog = new ConfirmDialog("Are you sure you want to run and merge the current YOLO model on all images?");
		var result = await DialogHost.Show(confirmDialog, "RootDialog");

		if (result is bool confirmed && confirmed)
		{
			await RunYoloWithProgressAsync();
		}
	}
	private async Task RunYoloWithProgressAsync()
	{
		var progressDialog = new ProgressBarDialog();
		var progress = new Progress<(double percent, bool done)>(value =>
		{
			progressDialog.SetProgress(value.percent);
			if (value.done)
				DialogHost.Close("RootDialog", null);
		});
		progressDialog.CancelRequested += () => yoloCancellationToken?.Cancel();

		var workTask = Task.Run(() => viewModel.RunYoloOnProject(progress, yoloCancellationToken.Token));
		await DialogHost.Show(progressDialog, "RootDialog");
		await workTask;
	}


	private void ZoomExtentsCanva(object? sender, RoutedEventArgs e)
	{
		ZoomBorder.ResetMatrix();
		//ZoomBorder.ZoomTo(1, ImageLayer.Bounds.Width/2, ImageLayer.Bounds.Height/2);
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

	private async void OnProjectMergeClicked(object? sender, RoutedEventArgs e)
	{
		if (viewModel?.databaseService?.connection?.State == System.Data.ConnectionState.Closed)
			return;

		var dialog = new MergeProjectDialog();
		var result = await DialogHost.Show(dialog, "RootDialog");

		if (result is MergeProjectDialog mpd)
		{
			viewModel?.MergeProjects(mpd.IncomingPath,
				mpd.AddClassesCheckBox.IsChecked == true ? true : false,
				mpd.AddImagesCheckBox.IsChecked == true ? true : false,
				mpd.AddAnnotationsCheckBox.IsChecked == true ? true : false,
				mpd.KeepBothRadioButton.IsChecked == true ? true : false,
				mpd.KeepIncomingRadioButton.IsChecked == true ? true : false,
				mpd.KeepCurrentRadioButton.IsChecked == true ? true : false);
		}
	}

	private async void OnProjectSplitClicked(object? sender, RoutedEventArgs e)
	{
		if (viewModel?.databaseService?.connection?.State == System.Data.ConnectionState.Closed)
			return;

		var dialog = new SplitProjectDialog(viewModel!.ImagesPaths.Count, viewModel.databaseService.absolutePath);
		var result = await DialogHost.Show(dialog, "RootDialog");

		if (result is SplitProjectDialog spd)
		{
			viewModel.SplitProject(spd.SplittedProjectsRatios.Select(x => x.ImageCountWithCorrection).ToList(),
				spd.RandomizeCheckBox.IsChecked == true,
				spd.fileNameWithoutExt);
		}
	}

	private async void OnRemoveAllAnnotationsClicked(object? sender, RoutedEventArgs e)
	{
		string message = $"Are you sure you want to remove all annotations on all images in the project?\nThis action is unreversible";
		var dialog = new ConfirmDialog(message);
		var result = await DialogHost.Show(dialog, "RootDialog");

		if (result is bool confirmed && confirmed)
		{
			viewModel.RemoveAllAnnotations();
		}

	}

	public async void OnWarningDialogEventReceived(object? sender, WarningDialogEventArgs e)
	{
		try
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
			{
				try
				{
					var dialog = new WarningDialog(e.Title, e.Content);
					await DialogHost.Show(dialog, "RootDialog");
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine($"Dialog error: {ex}");
				}
			});
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Dispatcher error: {ex}");
		}
	}


}