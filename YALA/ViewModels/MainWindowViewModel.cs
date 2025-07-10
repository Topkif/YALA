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
using System.Linq;

namespace YALA.ViewModels;
public partial class MainWindowViewModel : ViewModelBase
{
	[ObservableProperty] ObservableCollection<LabelingClass> labelingClasses = new();
	[ObservableProperty] public int numberOfImages;
	//[ObservableProperty] LabelingDatabase labelingDatabase = new();

	DatabaseService databaseService = new();
	public MainWindowViewModel()
	{
		labelingClasses.Add(new LabelingClass{ Id = 0, Name = "robot", Color = "#6eeb83", NumberOfInstances = 11, isSelected = false });
		labelingClasses.Add(new LabelingClass{ Id = 1, Name = "Ballon", Color = "#3654b3", NumberOfInstances = 2, isSelected = true });
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
		LabelingClasses = databaseService.GetLabellingClasses();
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
		LabelingClasses = databaseService.GetLabellingClasses();
	}

	[RelayCommand]
	private void CloseCurrentProject()
	{
		databaseService.Close();
	}

	[RelayCommand]
	private void UpdateLabelColor(LabelingClass labelingClass)
	{
		databaseService.SetClassColor(labelingClass.Name, labelingClass.Color);
	}

	[RelayCommand]
	private void SetSelectedLabel(LabelingClass labelingClass)
	{
		foreach (var label in LabelingClasses)
		{
			label.isSelected = false;
		}
		labelingClass.isSelected = true;
	}
}
