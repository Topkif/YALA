using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace YALA.Models;

public partial class ClassMapping : ObservableObject
{
	[ObservableProperty]
	private LabellingClass? fromClass;

	[ObservableProperty]
	private LabellingClass? toClass;

	[ObservableProperty]
	private bool removeFromProject = false;

	public ObservableCollection<LabellingClass> AvailableClasses { get; }

	public ClassMapping(ObservableCollection<LabellingClass> availableClasses)
	{
		AvailableClasses = availableClasses;
	}

	public bool IsValid()
	{
		return FromClass != null
			&& ToClass != null
			&& FromClass.Id != ToClass.Id;
	}
}
