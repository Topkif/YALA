using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using DialogHostAvalonia;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YALA.Views;

public partial class ImportDialog : UserControl
{
	public int ImageCount { get; private set; }
	public List<string>? imagesPaths { get; private set; }
	public List<string>? annotationsPaths { get; private set; }
	public string? classFilePath { get; private set; }
	public int AnnotationCount { get; private set; }
	private readonly bool _projectHasClasses;

	public bool UseExistingLabels => UseExistingLabelsCheckBox.IsChecked == true;

	public ImportDialog(bool projectHasClasses)
	{
		InitializeComponent();
		DataContext = this;

		_projectHasClasses = projectHasClasses;
		UseExistingLabelsCheckBox.IsEnabled = projectHasClasses;

		if (!projectHasClasses)
			ToolTip.SetTip(UseExistingLabelsCheckBox, "To use project labels you need to have at least one label");

		UseExistingLabelsCheckBox.IsCheckedChanged += (_, _) => UpdateClassPickerState();
		UpdateClassPickerState();
	}

	private void UpdateClassPickerState()
	{
		bool disable = UseExistingLabelsCheckBox.IsChecked == true;
		ClassesPathText.IsEnabled = !disable;
		BrowseClassesButton.IsEnabled = !disable;
	}

	private async Task<IReadOnlyList<IStorageFile>?> PickFilesAsync(string title, bool allowMultiple, params string[] extensions)
	{
		var window = VisualRoot as Window;
		if (window is null) return null;

		return await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = title,
			AllowMultiple = allowMultiple,
			FileTypeFilter = extensions.Length > 0
				? new[] { new FilePickerFileType($"{string.Join(", ", extensions)} file(s)") { Patterns = extensions.Select(e => $"*.{e}").ToArray() } }
				: null
		});
	}

	private async void OnBrowseImages(object? sender, RoutedEventArgs e)
	{
		var result = await PickFilesAsync("Select images", true, "jpg", "jpeg", "png", "bmp");
		if (result is { Count: > 0 })
		{
			imagesPaths = result.Select(x => x.Path.LocalPath).ToList();
			ImageCount = imagesPaths.Count;
			ImagesCountText.Text = $"{ImageCount} file(s) selected";
		}
	}

	private async void OnBrowseAnnotations(object? sender, RoutedEventArgs e)
	{
		var result = await PickFilesAsync("Select annotation files", true, "txt");
		if (result is { Count: > 0 })
		{
			annotationsPaths = result.Select(x => x.Path.LocalPath).ToList();
			AnnotationCount = annotationsPaths.Count;
			AnnotationsCountText.Text = $"{AnnotationCount} file(s) selected";
		}
	}

	private async void OnBrowseClasses(object? sender, RoutedEventArgs e)
	{
		var result = await PickFilesAsync("Select class names file", false, "txt", "yalac");
		if (result is { Count: > 0 })
		{
			classFilePath = result[0].Path.LocalPath;
			ClassesPathText.Text = System.IO.Path.GetFileName(classFilePath);
		}
	}


	private void OnImportClicked(object? sender, RoutedEventArgs e)
	{
		DialogHost.Close("RootDialog", this);
	}

	private void OnCancelClicked(object? sender, RoutedEventArgs e)
	{
		DialogHost.Close("RootDialog", false);
	}
}
