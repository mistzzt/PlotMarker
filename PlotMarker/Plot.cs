using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI.DB;

namespace PlotMarker
{
	internal sealed class PlotRegion
	{
		public Rectangle Area { get; set; }

		public int CellWidth { get; set; }

		public int CellHeight { get; set; }

		public void GenerateCells(bool empty = true)
		{

		}
	}

	internal sealed class PlotCell
	{
		public string Owner { get; set; }

		public Rectangle Area { get; set; }

		public List<int> AllowedIDs { get; set; }
	}

	internal sealed class PlotStyle
	{
		public string Name { get; set; }
		
		public int LineWidth { get; set; }
		
		public short TileId { get; set; }

		public byte TilePaint { get; set; }

		public short WallId { get; set; }

		public byte WallPaint { get; set; }
	}
}
