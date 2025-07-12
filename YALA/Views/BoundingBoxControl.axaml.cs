using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Reactive;
using Avalonia.VisualTree;
using System;
using System.ComponentModel;
using System.Reflection;
using YALA.Models;

namespace YALA.Views;
public partial class BoundingBoxControl : UserControl
{
	public static readonly StyledProperty<double> TlxProperty =
		AvaloniaProperty.Register<BoundingBoxControl, double>(nameof(Tlx));

	public static readonly StyledProperty<double> TlyProperty =
		AvaloniaProperty.Register<BoundingBoxControl, double>(nameof(Tly));

	public double Tlx
	{
		get => GetValue(TlxProperty);
		set => SetValue(TlxProperty, value);
	}

	public double Tly
	{
		get => GetValue(TlyProperty);
		set => SetValue(TlyProperty, value);
	}

	public BoundingBoxControl()
	{
		InitializeComponent();

		// Bind Canvas.Left and Canvas.Top to this control’s own Tlx and Tly
		this.Bind(Canvas.LeftProperty, this.GetObservable(TlxProperty));
		this.Bind(Canvas.TopProperty, this.GetObservable(TlyProperty));
	}
}