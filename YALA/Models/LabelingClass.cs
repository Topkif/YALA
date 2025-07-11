using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YALA.Models;
public partial class LabelingClass : ObservableObject
{
	[ObservableProperty] int id;
	[ObservableProperty] string name = string.Empty;
	[ObservableProperty] int numberOfInstances;
	[ObservableProperty] string color = string.Empty;
	[ObservableProperty] bool isSelected = false;
}
