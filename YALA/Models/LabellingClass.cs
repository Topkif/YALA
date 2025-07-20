using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YALA.Models;
public partial class LabellingClass : ObservableObject
{
	public void SetColorSilently(string newColor)
	{
		color = newColor; // no PropertyChanged fired
	}

	[ObservableProperty] int id;
	[ObservableProperty] string name = string.Empty;
	[ObservableProperty] int numberOfInstances;
	[ObservableProperty] string color = string.Empty;
	[ObservableProperty] bool isSelected = false;
}
