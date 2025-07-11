using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using YALA.Models;
using YALA.ViewModels;

namespace YALA.Views;
public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
		this.Opened += (_, _) => MainFocusTarget.Focus();
		var tb = this.FindControl<TextBox>("ImageIndexTextBox");
		tb.AddHandler(TextBox.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
	}

	private static void OnTextInput(object? sender, TextInputEventArgs e)
	{
		if (!int.TryParse(e.Text, out _))
			e.Handled = true;
	}

	private async void OnCreateNewProjectClick(object sender, RoutedEventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
			return;

		var topLevel = GetTopLevel(this);
		if (topLevel == null)
			return;

		var files = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
		{
			Title = "Create New Project Database",
			DefaultExtension = "db",
			SuggestedFileName = "project.db",
			FileTypeChoices = new[]
			{
			new FilePickerFileType("SQLite Database") { Patterns = new[] { "*.db" } }
		}
		});

		if (files != null)
		{
			var classFiles = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = "Select Class Names File",
				AllowMultiple = false,
				FileTypeFilter = new[]
			{
				new FilePickerFileType("Class Files") { Patterns = new[] { "*.txt", "*.names", "*" } }
			}
			});

			// 3. If a file was selected, execute the command on the ViewModel
			if (classFiles?.Count > 0)
			{
				string dbPath = files.Path.LocalPath;
				string classesPath = classFiles[0].Path.LocalPath;
				viewModel.CreateNewProjectCommand.Execute((dbPath, classesPath));
			}
		}
	}

	private async void OnOpenExistingProjectClick(object sender, RoutedEventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
			return;

		var topLevel = GetTopLevel(this);
		if (topLevel == null)
			return;

		var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Open Existing Project (.db)",
			AllowMultiple = false,
			FileTypeFilter = new[]
			{
			new FilePickerFileType("SQLite Database") { Patterns = new[] { "*.db" } }
		}
		});

		if (files?.Count > 0)
		{
			string path = files[0].Path.LocalPath;
			viewModel.OpenExistingProjectCommand.Execute(path);
		}
	}

	private void OnColorChanged(object sender, ColorChangedEventArgs e)
	{
		if (sender is ColorPicker picker && picker.DataContext is LabelingClass selectedClass)
		{
			var hexColor = e.NewColor.ToString(); // format: "#RRGGBBAA"
			selectedClass.Color = hexColor;

			if (DataContext is not MainWindowViewModel viewModel)
				return;
			viewModel.UpdateLabelColorCommand.Execute(selectedClass);
		}
	}

	private void OnClassChecked(object sender, RoutedEventArgs e)
	{
		if (sender is RadioButton rb && rb.DataContext is LabelingClass selectedClass)
		{
			if (DataContext is not MainWindowViewModel viewModel)
				return;
			viewModel.SetSelectedLabelCommand.Execute(selectedClass);
		}
	}

	private async void OnAddImagesClicked(object sender, RoutedEventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
			return;

		var topLevel = GetTopLevel(this);
		if (topLevel == null)
			return;

		var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Select Images or Folder",
			AllowMultiple = true,
			FileTypeFilter = new[]
			{
			new FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg" } }
		}
		});

		if (files?.Count > 0)
		{
			List<string> imagePaths = files.Select(f => f.Path.LocalPath).ToList();
			viewModel.AddImagesCommand.Execute(imagePaths);
		}
	}

	private void OnKeyDown(object? sender, KeyEventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
			return;

		if (e.Key == Key.Left)
		{
			viewModel.PreviousImageCommand.Execute(null);
			e.Handled = true;
		}
		else if (e.Key == Key.Right)
		{
			viewModel.NextImageCommand.Execute(null);
			e.Handled = true;
		}
	}
	private void OnMenuKeyDown(object? sender, KeyEventArgs e)
	{
		e.Handled = true; // Consume all key input
	}

	private void OnCurrentImageIndexChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
			return;

		if (sender is TextBox textBox)
		{
			if (int.TryParse(textBox.Text, out int index))
			{
				viewModel.GotoImageCommand.Execute(index);
			}
		}
	}

}