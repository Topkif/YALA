using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DialogHostAvalonia;
using System.Globalization;


namespace YALA.Views;
public partial class RunYoloProjectDialog : UserControl
{
	public double NmsThreshold { get; private set; }
	public double ConfidenceThreshold { get; private set; }

	public RunYoloProjectDialog()
	{
		InitializeComponent();
	}

	private void OnOkClick(object? sender, RoutedEventArgs e)
	{
		if (double.TryParse(NmsTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var nms) &&
			double.TryParse(ConfTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var conf))
		{
			NmsThreshold = nms;
			ConfidenceThreshold = conf;
			DialogHost.Close("RootDialog", this);
		}
	}

	private void OnCancelClick(object? sender, RoutedEventArgs e)
	{
		DialogHost.Close("RootDialog", null);
	}
}