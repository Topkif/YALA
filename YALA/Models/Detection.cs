using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YALA.Models;

public class Detection
{
	public float xCenter { get; set; }
	public float yCenter { get; set; }
	public float width { get; set; }
	public float height { get; set; }
	public float confidence { get; set; }
	public int classId { get; set; }
	public string label { get; set; } = string.Empty;


	public float CalculateIoU(Detection b)
	{
		float Left = xCenter - width / 2;
		float Top = yCenter - height / 2;
		float Right = xCenter + width / 2;
		float Bottom = yCenter + height / 2;
		// Convert center coordinates to corner coordinates
		float bLeft = b.xCenter - b.width / 2;
		float bTop = b.yCenter - b.height / 2;
		float bRight = b.xCenter + b.width / 2;
		float bBottom = b.yCenter + b.height / 2;

		// Calculate intersection
		float intersectionLeft = Math.Max(Left, bLeft);
		float intersectionTop = Math.Max(Top, bTop);
		float intersectionRight = Math.Min(Right, bRight);
		float intersectionBottom = Math.Min(Bottom, bBottom);

		if (intersectionLeft >= intersectionRight || intersectionTop >= intersectionBottom)
			return 0f;

		float intersectionArea = (intersectionRight - intersectionLeft) * (intersectionBottom - intersectionTop);
		float Area = width * height;
		float bArea = b.width * b.height;
		float unionArea = Area + bArea - intersectionArea;
		return intersectionArea / unionArea;
	}
}
