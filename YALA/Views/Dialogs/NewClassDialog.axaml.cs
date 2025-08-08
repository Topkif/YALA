using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using DialogHostAvalonia;
using YALA.Models;


namespace YALA.Views;

public partial class NewClassDialog : UserControl
{
	private string className = "";
	private string hexColor = "#FF0000";

	public NewClassDialog()
	{
		InitializeComponent();
	}

	private void OnDialogColorChanged(object? sender, ColorChangedEventArgs e)
	{
		if (sender is ColorPicker picker)
		{
			hexColor = e.NewColor.ToString(); // format: "#RRGGBBAA"
		}
	}

	private void OnOkClick(object? sender, RoutedEventArgs e)
	{
		className = ClassNameBox.Text?.Trim() ?? "";
		if (string.IsNullOrWhiteSpace(className))
		{
			// Optionally display a validation message
			return;
		}

		DialogHost.Close("RootDialog", (className, hexColor));
	}

	private void OnCancelClick(object? sender, RoutedEventArgs e)
	{
		DialogHost.Close("RootDialog", null);
	}
}