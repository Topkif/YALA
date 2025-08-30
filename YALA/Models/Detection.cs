using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YALA.Models;

public class Detection
{
	public float tlx { get; set; }
	public float tly { get; set; }
	public float width { get; set; }
	public float height { get; set; }
	public float confidence { get; set; }
	public int classId { get; set; }
	public string label { get; set; } = string.Empty;


	public float CalculateIoU(Detection b)
	{
		// Calculate intersection
		float intersectionLeft = Math.Max(tlx, b.tlx);
		float intersectionTop = Math.Max(tly, b.tly);
		float intersectionRight = Math.Min(tlx + width, b.tlx + b.width);
		float intersectionBottom = Math.Min(tly + height, b.tly + b.height);

		if (intersectionLeft >= intersectionRight || intersectionTop >= intersectionBottom)
			return 0f;

		float intersectionArea = (intersectionRight - intersectionLeft) * (intersectionBottom - intersectionTop);
		float Area = width * height;
		float bArea = b.width * b.height;
		float unionArea = Area + bArea - intersectionArea;
		return intersectionArea / unionArea;
	}
}
