using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DialogHostAvalonia;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace YALA.Views;

public partial class SplitProjectDialog : UserControl
{
	public ObservableCollection<SplittedRatio> SplittedProjectsRatios { get; set; } = new();
	public int TotalImageCount { get; }
	private string projectPath;
	public string fileNameWithoutExt = "";
	public SplitProjectDialog(int imageCount, string projectPath)
	{
		InitializeComponent();
		TotalImageCount = imageCount;
		this.projectPath = projectPath;
		FilePathTextBox.Text = System.IO.Path.GetFileNameWithoutExtension(projectPath);

		SplittedProjectsRatios.CollectionChanged += (_, __) =>
		{
			foreach (var item in SplittedProjectsRatios)
			{
				item.RefreshCalculatedProperties();
			}
		};

		SplittedProjectsRatios.Add(new SplittedRatio(TotalImageCount, SplittedProjectsRatios));
		SplittedProjectsRatios.Add(new SplittedRatio(TotalImageCount, SplittedProjectsRatios)
		);
		DataContext = this;
	}

	private void FilePathTextBoxTextChanged(object sender, TextChangedEventArgs e)
	{
		string baseDir = (System.IO.Path.GetDirectoryName(projectPath) ?? "").Trim();
		fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(FilePathTextBox.Text).Trim();
		bool anyExisting = Directory.EnumerateFiles(baseDir, $"{fileNameWithoutExt}_*.yala").Any();

		if (!anyExisting)
		{
			FilePathErrorLabel.Content = $"Splitted files \"{fileNameWithoutExt}\" are free";
			FilePathErrorLabel.Foreground = Brushes.Green;
			SplitButton.IsEnabled = true;
		}
		else
		{
			FilePathErrorLabel.Content = $"Splitted files \"{fileNameWithoutExt}\" exist";
			FilePathErrorLabel.Foreground = Brushes.Red;
			SplitButton.IsEnabled = false;
		}
	}


	private void OnSplitCountUpDownValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
	{
		if (e.NewValue > e.OldValue)
		{
			int diffClasses = Math.Abs((int)(e.NewValue - e.OldValue));
			for (int i = 0; i < diffClasses; i++)
			{
				SplittedProjectsRatios.Add(new SplittedRatio(TotalImageCount, SplittedProjectsRatios));
			}
		}
		if (e.NewValue < e.OldValue)
		{
			int diffClasses = Math.Abs((int)(e.NewValue - e.OldValue));
			for (int i = 0; i < diffClasses; i++)
			{
				SplittedProjectsRatios.RemoveAt(SplittedProjectsRatios.Count - 1);
			}
		}
	}

	private void OnSplitClicked(object? sender, RoutedEventArgs e)
	{
		DialogHost.Close("RootDialog", this);
	}

	private void OnCancelClicked(object? sender, RoutedEventArgs e)
	{
		DialogHost.Close("RootDialog", false);
	}
}

public class SplittedRatio : INotifyPropertyChanged
{
	private readonly int totalImages;
	private readonly ObservableCollection<SplittedRatio> splittedProjectsRatios;
	public SplittedRatio(int totalImages, ObservableCollection<SplittedRatio> splittedProjectsRatios)
	{
		this.totalImages = totalImages;
		this.splittedProjectsRatios = splittedProjectsRatios;
		RawRatio = 0.5;
	}
	internal void RefreshCalculatedProperties()
	{
		OnPropertyChanged(nameof(ScaledRatio));
		OnPropertyChanged(nameof(BaseImageCount));
		OnPropertyChanged(nameof(ImageCountWithCorrection));
		OnPropertyChanged(nameof(DisplayLabel));
	}

	private double ratio;
	public double RawRatio
	{
		get => ratio;
		set
		{
			if (Math.Abs(ratio - value) > 0.001)
			{
				ratio = value;

				foreach (var item in splittedProjectsRatios)
				{
					item.RefreshCalculatedProperties();
				}
				OnPropertyChanged(nameof(RawRatio)); // This instance’s RawRatio change
			}
		}
	}

	public double ScaledRatio => ratio / splittedProjectsRatios.Sum(r => r.ratio);
	public int BaseImageCount => (int)Math.Round(ScaledRatio * totalImages);
	public int ImageCountWithCorrection
	{
		get
		{
			if (ReferenceEquals(this, splittedProjectsRatios.First()))
			{
				int totalAssigned = splittedProjectsRatios.Sum(x => x.BaseImageCount);
				int correction = totalImages - totalAssigned;
				return BaseImageCount + correction;
			}
			return BaseImageCount;
		}
	}

	public string DisplayLabel => $"{ScaledRatio * 100:F2}% ({ImageCountWithCorrection} images)";

	public event PropertyChangedEventHandler? PropertyChanged;
	protected void OnPropertyChanged(string propertyName) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
