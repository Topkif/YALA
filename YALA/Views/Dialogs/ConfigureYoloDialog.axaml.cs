using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DialogHostAvalonia;
using System.Globalization;


namespace YALA.Views;
public partial class ConfigureYoloDialog : UserControl
{
	public double IouThreshold { get; private set; }
	public double ConfidenceThreshold { get; private set; }
	public string IncomingPath => FilePathTextBox.Text;

	public ConfigureYoloDialog( string modelPath, double iouThreshold, double confThreshold)
	{
		InitializeComponent();
		FilePathTextBox.Text = modelPath;
		NmsSlider.Value = iouThreshold;
		ConfSlider.Value= confThreshold;
	}

	private void IouSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
	{
		if (IouTextBlock != null)
			IouTextBlock.Text = "IoU Threshold: " + e.NewValue.ToString("0.00");
	}

	private void ConfSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
	{
		if (ConfidenceTextBlock != null)
			ConfidenceTextBlock.Text = "Confidence Threshold: " + e.NewValue.ToString("0.00");
	}

	private async void OnBrowseClicked(object? sender, RoutedEventArgs e)
	{
		var window = VisualRoot as Window;
		if (window is null) return;

		var file = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Select YOLO ONNX model (v8+)",
			AllowMultiple = false,
			SuggestedFileName = "yolov8.onnx",
			FileTypeFilter = new[]
			{
			new FilePickerFileType("") { Patterns = new[] { "*.onnx" } }
			}
		});

		if (file != null && file.Count > 0)
		{
			FilePathTextBox.Text = file[0].Path.LocalPath;
		}
	}
	private void OnOkClick(object? sender, RoutedEventArgs e)
	{
		double nms = NmsSlider.Value;
		double conf = ConfSlider.Value;

		IouThreshold = nms;
		ConfidenceThreshold = conf;
		DialogHost.Close("RootDialog", this);
	}

	private void OnCancelClick(object? sender, RoutedEventArgs e)
	{
		DialogHost.Close("RootDialog", null);
	}
}