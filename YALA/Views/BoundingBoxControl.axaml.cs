using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Reactive;
using Avalonia.VisualTree;
using System;
using System.ComponentModel;
using System.Reflection;
using YALA.Models;
using YALA.ViewModels;

namespace YALA.Views;
public partial class BoundingBoxControl : UserControl
{
	private readonly MainWindowViewModel viewModel = App.MainVM;

	private ResizeDirection currentDirection;

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

	private void OnThumbDragStarted(object? sender, VectorEventArgs e)
	{
		currentDirection = (sender as Control)?.Name switch
		{
			"TopThumb" => ResizeDirection.Top,
			"BottomThumb" => ResizeDirection.Bottom,
			"LeftThumb" => ResizeDirection.Left,
			"RightThumb" => ResizeDirection.Right,
			"TopLeftThumb" => ResizeDirection.TopLeft,
			"TopRightThumb" => ResizeDirection.TopRight,
			"BottomLeftThumb" => ResizeDirection.BottomLeft,
			"BottomRightThumb" => ResizeDirection.BottomRight,
			_ => ResizeDirection.None,
		};
		viewModel.OnThumbDragStarted(BoundingBox, currentDirection);
	}

	private void OnThumbDragDelta(object? sender, VectorEventArgs e)
	{
		switch (currentDirection)
		{
			case ResizeDirection.Top:
				viewModel.OnThumbDragDelta(0, e.Vector.Y);
				break;
			case ResizeDirection.Bottom:
				viewModel.OnThumbDragDelta(0, e.Vector.Y);
				break;
			case ResizeDirection.Left:
				viewModel.OnThumbDragDelta(e.Vector.X, 0);
				break;
			case ResizeDirection.Right:
				viewModel.OnThumbDragDelta(e.Vector.X, 0);
				break;
			case ResizeDirection.TopLeft:
				viewModel.OnThumbDragDelta(e.Vector.X, e.Vector.Y);
				break;
			case ResizeDirection.TopRight:
				viewModel.OnThumbDragDelta(e.Vector.X, e.Vector.Y);
				break;
			case ResizeDirection.BottomLeft:
				viewModel.OnThumbDragDelta(e.Vector.X, e.Vector.Y);
				break;
			case ResizeDirection.BottomRight:
				viewModel.OnThumbDragDelta(e.Vector.X, e.Vector.Y);
				break;

			default:
				break;
		}
	}

	private void OnThumbDragCompleted(object? sender, VectorEventArgs e)
	{
		viewModel.OnThumbDragCompleted();
	}

}



