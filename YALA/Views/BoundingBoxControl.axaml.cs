using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Reactive;
using Avalonia.VisualTree;
using System;
using System.ComponentModel;
using YALA.Models;

namespace YALA.Views;
public partial class BoundingBoxControl : UserControl
{
	public BoundingBoxControl()
	{
		InitializeComponent();
		DataContextChanged += OnDataContextChanged;
	}

	private void OnDataContextChanged(object? sender, EventArgs e)
	{
		if (DataContext is BoundingBox box)
		{
			// Remove previous handler if exists
			if (box is INotifyPropertyChanged oldBox)
				oldBox.PropertyChanged -= OnBoxPropertyChanged;

			// Add new handler
			box.PropertyChanged += OnBoxPropertyChanged;
		}
		UpdatePosition();
	}

	private void OnBoxPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		UpdatePosition();
	}

	private void UpdatePosition()
	{
		if (DataContext is BoundingBox box &&
			this.GetVisualParent() is Canvas canvas)
		{
			Canvas.SetLeft(this, box.XCenter - box.Width / 2);
			Canvas.SetTop(this, box.YCenter - box.Height / 2);
		}
	}
}