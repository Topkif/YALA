using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using YALA.Services;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.ObjectModel;

namespace YALA.ViewModels;
public partial class MainWindowViewModel : ViewModelBase
{
	[ObservableProperty]
	ObservableCollection<String> classes=  new();

	public MainWindowViewModel()
	{
	}


	[RelayCommand]
	private async Task SelectClasses(Window window)
	{
		var options = new FilePickerOpenOptions
		{
			AllowMultiple = false,
			FileTypeFilter = new[]
			{
			new FilePickerFileType("Class Files")
			{
				Patterns = new[] { "*.txt", "*.names", "*" }
			}
		}
		};

		var files = await window.StorageProvider.OpenFilePickerAsync(options);
		if (files != null && files.Count > 0)
		{
			var path = files[0].Path.LocalPath;
			Classes = ClassesFileParser.ParseClassNames(path);
		}
	}



	[RelayCommand]
	private void CreateNew()
	{

	}

	[RelayCommand]
	private void Open()
	{
		// Your open logic here
	}
}
