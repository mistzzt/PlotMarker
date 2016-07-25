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

		public List<Cell> Cells { get; set; } = new List<Cell>();

		public void GenerateCells(bool empty = true)
		{
			if (empty)
			{
				TileHelper.RemoveTiles(X, Y, Width, Height);
			}

			var style = PlotMarker.Config.PlotStyle;
			var cellX = CellWidth + style.LineWidth;
			var cellY = CellHeight + style.LineWidth;

			//draw horizental line
			for (var y = 0; y < Height; y = y + cellY)
			{
				for (var x = 0; x < Width; x++)
				{
					for (var t = 0; t < style.LineWidth; t++)
					{
						TileHelper.SetTile(X + x, Y + y + t, style.TileId, style.TilePaint);
						TileHelper.SetWall(X + x, Y + y + t, style.WallId, style.WallPaint);
					}
				}
			}

			//draw vertical line
			for (var x = 0; x < Width; x = x + cellX)
			{
				for (var y = 0; y < Height; y++)
				{
					for (var t = 0; t < style.LineWidth; t++)
					{
						TileHelper.SetTile(X + x + t, Y + y, style.TileId, style.TilePaint);
						TileHelper.SetWall(X + x + t, Y + y, style.WallId, style.WallPaint);
					}
				}
			}

			TileHelper.ResetSection(X, Y, Width, Height);

			Cells.Clear();
			for (var x = 0; x < Width; x = x + cellX)
			{
				for (var y = 0; y < Height; y = y + cellY)
				{
					var cell = new Cell
					{
						Id = Cells.Count,
						Parent = this,
						X = X + x + style.LineWidth,
						Y = Y + y + style.LineWidth,
						Owner = Owner,
						AllowedIDs = new List<int>()
					};
					//new Rectangle(startX + x + style.LineWidth, startY + y + style.LineWidth, style.RoomWidth, style.RoomHeight)
					Cells.Add(cell);
				}
			}
			PlotMarker.Plots.AddCells(this);
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
