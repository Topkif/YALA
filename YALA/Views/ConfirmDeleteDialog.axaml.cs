using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DialogHostAvalonia;

namespace YALA.Views;
public partial class ConfirmDeleteDialog : UserControl
{
	public ConfirmDeleteDialog(string className)
	{
		InitializeComponent();
		MessageText.Text = $"Are you sure you want to delete the class \"{className}\" and all associated annotations?";
	}

	private void OnConfirmClick(object? sender, RoutedEventArgs e)
	{
		DialogHost.Close("RootDialog", true);
	}

	private void OnCancelClick(object? sender, RoutedEventArgs e)
	{
		DialogHost.Close("RootDialog", false);
	}
}
