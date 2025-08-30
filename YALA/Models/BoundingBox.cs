using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YALA.Models;

public partial class BoundingBox : ObservableObject
{
	[ObservableProperty] int id;
	[ObservableProperty] double tlx; // Top Left X coordinate
	[ObservableProperty] double tly; // Top Left Y coordinate
	[ObservableProperty] double width;
	[ObservableProperty] double height;
	[ObservableProperty] string color = "#ffffff";
	[ObservableProperty] int classId;
	[ObservableProperty] string className = "";
	[ObservableProperty] bool editingEnabled;
	[ObservableProperty] bool isSelected;

	public double CalculateIoU(BoundingBox b)
	{
		// Calculate intersection
		double intersectionLeft = Math.Max(tlx, b.tlx);
		double intersectionTop = Math.Max(tly, b.tly);
		double intersectionRight = Math.Min(tlx + width, b.tlx + b.width);
		double intersectionBottom = Math.Min(tly + height, b.tly + b.height);

		if (intersectionLeft >= intersectionRight || intersectionTop >= intersectionBottom)
			return 0f;

		double intersectionArea = (intersectionRight - intersectionLeft) * (intersectionBottom - intersectionTop);
		double Area = width * height;
		double bArea = b.width * b.height;
		double unionArea = Area + bArea - intersectionArea;
		return intersectionArea / unionArea;
	}
}

