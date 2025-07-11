using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YALA.Models;
public partial class BoundingBox : ObservableObject
{
	[ObservableProperty] int xCenter;
	[ObservableProperty] int yCenter;
	[ObservableProperty] int width;
	[ObservableProperty] int height;
	[ObservableProperty] string color = "#ffffff";
}