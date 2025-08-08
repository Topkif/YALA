using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DialogHostAvalonia;
using System;

namespace YALA.Views;

public partial class ProgressBarDialog : UserControl
{
	private ProgressBar _progressBar;
	private TextBlock _percentText;
	private TextBlock _messageText;
	public event Action? CancelRequested;
	public ProgressBarDialog()
	{
		InitializeComponent();
		// Need to find them manually because InitializeComponents is slower than the SetProgress()
		// method and the properties are not found
		_progressBar = this.FindControl<ProgressBar>("ProgressBar");
		_percentText = this.FindControl<TextBlock>("PercentText");
		_messageText = this.FindControl<TextBlock>("MessageText");
	}

	private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

	public void SetProgress(double percent)
	{
		_progressBar.Value = percent;
		_percentText.Text = $"{percent:0}%";
	}

	public void SetMessage(string message)
	{
		_messageText.Text = message;
	}

	private void OnCancelClick(object? sender, RoutedEventArgs e)
	{
		CancelRequested?.Invoke();
		DialogHost.Close("RootDialog", null);
	}

}