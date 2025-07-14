using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YALA.Models;
public partial class BoundingBox : ObservableObject
{
	[ObservableProperty] int tlx; // Top Left X coordinate
	[ObservableProperty] int tly; // Top Left Y coordinate
	[ObservableProperty] int width;
	[ObservableProperty] int height;
	[ObservableProperty] string color = "#ffffff";
	[ObservableProperty] int classId;
	[ObservableProperty] string className = "";
	[ObservableProperty] bool editingEnabled;
}