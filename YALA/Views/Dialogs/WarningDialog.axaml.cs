using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DialogHostAvalonia;

namespace YALA.Views;

public partial class WarningDialog : UserControl
{
	public WarningDialog(string title, string message)
	{
		InitializeComponent();
		DataContext = this;

		TitleTextBlock.Text = title;
		MessageTextBlock.Text = message;
	}

	private void OnOkClicked(object? sender, RoutedEventArgs e)
	{
		// return true to caller; adjust as needed
		DialogHost.Close("RootDialog", true);
	}
}
