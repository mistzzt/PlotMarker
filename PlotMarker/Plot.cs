using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
		[JsonProperty("间距")]
		public int LineWidth { get; set; }

		[JsonProperty("属地宽")]
		public int CellWidth { get; set; }

		[JsonProperty("属地高")]
		public int CellHeight { get; set; }

		[JsonProperty("物块")]
		public short TileId { get; set; }

		[JsonProperty("物块喷漆")]
		public byte TilePaint { get; set; }

		[JsonProperty("墙壁")]
		public short WallId { get; set; }

		[JsonProperty("墙壁喷漆")]
		public byte WallPaint { get; set; }
	}
}
