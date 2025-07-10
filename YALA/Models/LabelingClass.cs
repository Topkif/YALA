using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YALA.Models;
public class LabelingClass
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public int NumberOfInstances { get; set; }
	public string Color { get; set; } = string.Empty;
	public bool isSelected { get; set; } = false;
}
