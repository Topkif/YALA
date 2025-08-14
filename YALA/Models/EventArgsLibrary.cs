using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YALA.Models;

public class WarningDialogEventArgs : EventArgs
{
	public string Title { get; }
	public string Content { get; }

	public WarningDialogEventArgs(string title, string content)
	{
		Title = title;
		Content = content;
	}
}