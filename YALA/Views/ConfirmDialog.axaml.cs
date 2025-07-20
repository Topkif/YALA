using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DialogHostAvalonia;

namespace YALA.Views;
public partial class ConfirmDialog : UserControl
{
	public ConfirmDialog(string message)
	{
		InitializeComponent();
		MessageText.Text = message;
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
