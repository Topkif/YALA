using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YALA.ViewModels;

public partial class UiBoundingBoxViewModel : ObservableObject
{
	[ObservableProperty] private double x;
	[ObservableProperty] private double y;
	[ObservableProperty] private double width;
	[ObservableProperty] private double height;
	[ObservableProperty] private string label = "";
}
