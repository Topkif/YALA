using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using DialogHostAvalonia;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace YALA.Views;

public class SelectableClass
{
	public string Name { get; set; } = "";
	public bool IsChecked { get; set; } = true;
}
public partial class ExportProjectDialog : UserControl
{

	public string ExportPath => FilePathTextBox.Text;
	public List<string> SelectedClasses => Classes.Where(c => c.IsChecked).Select(c => c.Name).ToList();
	public double TrainRatio => TrainSlider.Value;
	public double ValRatio => ValSlider.Value;
	public double TestRatio => TestSlider.Value;

	public ObservableCollection<SelectableClass> Classes { get; } = new();

	private readonly int totalImageCount;

	public ExportProjectDialog(int imageCount, IEnumerable<string> classNames)
	{
		InitializeComponent();
		DataContext = this;
		totalImageCount = imageCount;

		foreach (var name in classNames)
			Classes.Add(new SelectableClass { Name = name });

		ClassList.ItemsSource = Classes;
		TrainSlider.PropertyChanged += (_, _) => UpdateLabels();
		ValSlider.PropertyChanged += (_, _) => UpdateLabels();
		TestSlider.PropertyChanged += (_, _) => UpdateLabels();

		UpdateLabels();
	}

	private void UpdateLabels()
	{
		double sumSlider = TrainSlider.Value + ValSlider.Value + TestSlider.Value;
		int train = (int)Math.Round(TrainSlider.Value / sumSlider * totalImageCount);
		int val = (int)Math.Round(ValSlider.Value / sumSlider * totalImageCount);
		int test = totalImageCount - train - val;

		TrainCountText.Text = $"Train: {TrainSlider.Value:F2} ({train} images)";
		ValCountText.Text = $"Val: {ValSlider.Value:F2} ({val} images)";
		TestCountText.Text = $"Test: {TestSlider.Value:F2} ({test} images)";
	}

	private async void OnBrowseClicked(object? sender, RoutedEventArgs e)
	{
		try
		{

		var window = VisualRoot as Window;
		if (window is null) return;

		var result = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
		{
			Title = "Select Export Folder",
			AllowMultiple = false
		});

		if (result.Count == 0)
			return; // user canceled

		var folder = result[0];
		var folderPath = folder.TryGetLocalPath();
		if (string.IsNullOrEmpty(folderPath))
			return;

		// Create the folder if it doesn't exist
		Directory.CreateDirectory(folderPath);

		FilePathTextBox.Text = folderPath;
		}
		catch { }
	}

	private void OnExportClicked(object? sender, RoutedEventArgs e)
	{
		DialogHost.Close("RootDialog", this);
	}

	private void OnCancelClicked(object? sender, RoutedEventArgs e)
	{
		DialogHost.Close("RootDialog", false);
	}
}