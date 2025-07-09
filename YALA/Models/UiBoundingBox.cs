using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YALA.Models;
public partial class UiBoundingBox
{
	public double X { get; set; }
	public double Y { get; set; }
	public double Width { get; set; }
	public double Height { get; set; }
}

/*using CommunityToolkit.Mvvm.ComponentModel;

public partial class UiBoundingBox : ObservableObject
{
    [ObservableProperty] private double x;
    [ObservableProperty] private double y;
    [ObservableProperty] private double width;
    [ObservableProperty] private double height;
}
*/