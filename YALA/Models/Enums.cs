using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YALA.Models;

public enum ResizeDirection
{
	Top,
	Bottom,
	Left,
	Right,
	None
}
public enum ConflictKeepBehaviour
{
	None = 0,
	Both = 1,
	Incoming = 2,
	Current = 3,
}