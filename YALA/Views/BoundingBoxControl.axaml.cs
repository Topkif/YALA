using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Reactive;
using Avalonia.VisualTree;
using System;
using System.ComponentModel;
using System.Reflection;
using YALA.Models;

namespace YALA.Views;
public partial class BoundingBoxControl : UserControl
{
	public static readonly StyledProperty<BoundingBox> BoundingBoxProperty =
		AvaloniaProperty.Register<BoundingBoxControl, BoundingBox>(nameof(BoundingBox));

	public BoundingBox BoundingBox
	{
		get => GetValue(BoundingBoxProperty);
		set => SetValue(BoundingBoxProperty, value);
	}

	public BoundingBoxControl()
	{
		InitializeComponent();
	}
}


