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
using YALA.Models;
using Avalonia.Controls.Shapes;

namespace YALA.ViewModels;
public partial class MainWindowViewModel : ViewModelBase
{
	[ObservableProperty] ObservableCollection<string> classes = new();
	[ObservableProperty] public int numberOfImages;
	//[ObservableProperty] LabelingDatabase labelingDatabase = new();

	DatabaseService databaseService = new();
	public MainWindowViewModel()
	{
		//databaseService = new(labelingDatabase);
	}

	[RelayCommand]
	private void CreateNewProject((string dbPath, string classesPath) paths)
	{
		if (databaseService.TablesExist(paths.dbPath))
		{
			databaseService.Open(paths.dbPath);
		}
		else
		{
			databaseService.Initialize(paths.dbPath);
			List<string> classes = ClassesFileParser.ParseClassNames(paths.classesPath);
			databaseService.AddClasses(classes);
		}
		Classes = databaseService.GetClassNames();
	}

	[RelayCommand]
	private void OpenExistingProject(string path)
	{
		if (databaseService.TablesExist(path))
		{
			databaseService.Open(path);
		}
		else
		{
			databaseService.Initialize(path);
		}
		Classes = databaseService.GetClassNames();
	}

	[RelayCommand]
	private void CloseCurrentProject()
	{
		databaseService.Close();
	}
}
