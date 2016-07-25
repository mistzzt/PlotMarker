using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI.DB;

namespace PlotMarker
{
	internal sealed class Plot
	{
		public int Id { get; set; }

		public string Name { get; set; }
		
		public int X { get; set; }

		public int Y { get; set; }

		public int Width { get; set; }

		public int Height { get; set; }

		public int CellWidth { get; set; }

		public int CellHeight { get; set; }

		public string WorldId { get; set; }

		public string Owner { get; set; }

		public void GenerateCells(bool empty = true)
		{

		}
	}

	internal sealed class Cell
	{
		public int Id { get; set; }

		public Plot Parent { get; set; }

		public int X { get; set; }

		public int Y { get; set; }

		public string Owner { get; set; }

		public DateTime GetTime { get; set; }

		public List<int> AllowedIDs { get; set; }
	}

	internal sealed class Style
	{
		public string Name { get; set; }
		
		public int LineWidth { get; set; }
		
		public short TileId { get; set; }

		public byte TilePaint { get; set; }

		public short WallId { get; set; }

		public byte WallPaint { get; set; }
	}
}
