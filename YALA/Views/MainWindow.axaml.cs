using Avalonia.Controls;
using Avalonia.Interactivity;
using YALA.ViewModels;

namespace YALA.Views;
public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
	}

	// Needs to be called from code behind because the Window (this) needs to be passed as an argument
	private async void OnSelectClassesClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is MainWindowViewModel vm)
		{
			await vm.SelectClassesCommand.ExecuteAsync(this);
		}
	}
}