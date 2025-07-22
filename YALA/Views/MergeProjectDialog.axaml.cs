using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using DialogHostAvalonia;

namespace YALA.Views;

public partial class MergeProjectDialog : UserControl
{
	public string IncomingPath => FilePathTextBox.Text;

	public MergeProjectDialog()
    {
        InitializeComponent();
    }
	private async void OnBrowseClicked(object? sender, RoutedEventArgs e)
	{
		var window = VisualRoot as Window;
		if (window is null) return;

		var file = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Import project to merge",
			AllowMultiple = false,
			SuggestedFileName = "project.yala",
			FileTypeFilter = new[]
			{
			new FilePickerFileType("YALA Project") { Patterns = new[] { "*.yala" } }
			}
		});

		if (file != null && file.Count > 0)
		{
			FilePathTextBox.Text = file[0].Path.LocalPath;
		}
	}
	private void AddAnnotationsCheckBox_Checked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
	{
		KeepBothRadioButton.IsEnabled = true;
		KeepIncomingRadioButton.IsEnabled = true;
		KeepCurrentRadioButton.IsEnabled = true;
	}

	private void AddAnnotationsCheckBox_Unchecked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
	{
		KeepBothRadioButton.IsEnabled = false;
		KeepIncomingRadioButton.IsEnabled = false;
		KeepCurrentRadioButton.IsEnabled = false;
	}

	private void OnMergeClicked(object? sender, RoutedEventArgs e)
	{
		DialogHost.Close("RootDialog", this);
	}
	private void OnCancelClicked(object? sender, RoutedEventArgs e)
	{
		DialogHost.Close("RootDialog", false);
	}
}