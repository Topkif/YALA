using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YALA.Models;

public partial class BoundingBox : ObservableObject
{
	[ObservableProperty] double tlx; // Top Left X coordinate
	[ObservableProperty] double tly; // Top Left Y coordinate
	[ObservableProperty] double width;
	[ObservableProperty] double height;
	[ObservableProperty] string color = "#ffffff";
	[ObservableProperty] int classId;
	[ObservableProperty] string className = "";
	[ObservableProperty] bool editingEnabled;
	[ObservableProperty] bool isSelected;
}