using Avalonia.Controls;
using Avalonia.Interactivity;
using DialogHostAvalonia;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using YALA.Models;

namespace YALA.Views;

public partial class RemapClassesDialog : UserControl
{
	public ObservableCollection<ClassMapping> Mappings { get; } = new();
	private readonly ObservableCollection<LabellingClass> availableClasses;

	public RemapClassesDialog(ObservableCollection<LabellingClass> classes)
	{
		InitializeComponent();
		availableClasses = classes;
		DataContext = this;

		// Start with one empty mapping
		AddNewMapping();

		// Subscribe to changes to enable/disable Execute button
		Mappings.CollectionChanged += (_, __) => ValidateMappings();
	}

	private void OnAddMappingClicked(object? sender, RoutedEventArgs e)
	{
		AddNewMapping();
	}

	private void OnRemoveMappingClicked(object? sender, RoutedEventArgs e)
	{
		if (sender is Button button && button.Tag is ClassMapping mapping)
		{
			Mappings.Remove(mapping);
		}
	}

	private void AddNewMapping()
	{
		var mapping = new ClassMapping(availableClasses);

		// Subscribe to property changes to re-validate
		mapping.PropertyChanged += (_, __) => ValidateMappings();

		Mappings.Add(mapping);
		ValidateMappings();
	}

	private void ValidateMappings()
	{
		ValidationMessageTextBlock.IsVisible = false;

		// Filter valid mappings
		var validMappings = Mappings.Where(m => m.IsValid()).ToList();

		if (validMappings.Count == 0)
		{
			ExecuteButton.IsEnabled = false;
			return;
		}

		// Check for duplicate "from" classes
		var fromClassIds = validMappings.Select(m => m.FromClass!.Id).ToList();
		if (fromClassIds.Count != fromClassIds.Distinct().Count())
		{
			ValidationMessageTextBlock.Text = "Error: Each class can only be remapped once.";
			ValidationMessageTextBlock.IsVisible = true;
			ExecuteButton.IsEnabled = false;
			return;
		}

		// Check for cycles (A → B and B → A)
		var toClassIds = validMappings.Select(m => m.ToClass!.Id).ToHashSet();
		foreach (var mapping in validMappings)
		{
			// If we're mapping TO a class that is also being mapped FROM, check for cycle
			var cycleMappings = validMappings.Where(m => m.FromClass!.Id == mapping.ToClass!.Id);
			foreach (var cycleMapping in cycleMappings)
			{
				if (cycleMapping.ToClass!.Id == mapping.FromClass!.Id)
				{
					ValidationMessageTextBlock.Text =
						$"Error: Cycle detected between '{mapping.FromClass.Name}' and '{mapping.ToClass.Name}'.";
					ValidationMessageTextBlock.IsVisible = true;
					ExecuteButton.IsEnabled = false;
					return;
				}
			}
		}

		// All validations passed
		ExecuteButton.IsEnabled = true;
	}

	private void OnExecuteClick(object? sender, RoutedEventArgs e)
	{
		var validMappings = Mappings
			.Where(m => m.IsValid())
			.Select(m => (FromClassId: m.FromClass!.Id, ToClassId: m.ToClass!.Id, RemoveFromProject: m.RemoveFromProject))
			.ToList();

		DialogHost.Close("RootDialog", validMappings);
	}

	private void OnCancelClick(object? sender, RoutedEventArgs e)
	{
		DialogHost.Close("RootDialog", null);
	}
}
