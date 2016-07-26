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
		/// <summary> 属地在数据库中的Id </summary>
		public int Id { get; set; }

		/// <summary> 属地的名字 </summary>
		public string Name { get; set; }

		/// <summary> 属地的起始X坐标 </summary>
		public int X { get; set; }

		/// <summary> 属地的起始Y坐标 </summary>
		public int Y { get; set; }

		/// <summary> 属地的宽 </summary>
		public int Width { get; set; }

		/// <summary> 属地的高 </summary>
		public int Height { get; set; }

		/// <summary> 小块区域的宽 </summary>
		public int CellWidth { get; set; }

		/// <summary> 小块区域的高 </summary>
		public int CellHeight { get; set; }

		/// <summary> 地图Id </summary>
		public string WorldId { get; set; }

		/// <summary> 整片属地的拥有者 </summary>
		public string Owner { get; set; }

		/// <summary>
		/// 小块区域的引用. 其中数组索引就是 <see cref="Cell.Id"/> ,
		/// 而顺序(数组索引)是按照 <see cref="Plot.GenerateCells"/> 中添加列表的顺序来
		/// </summary>
		public List<Cell> Cells { get; set; } = new List<Cell>();

		/// <summary>
		/// 生成格子并记录格子数值到数据库.
		/// </summary>
		/// <param name="empty"> 是否清空区域/适合修复格子 </param>
		public void GenerateCells(GetDataHandlerArgs args,bool empty = true)
		{
			if (empty)
			{
				TileHelper.RemoveTiles(X, Y, Width, Height);
			}

			var style = PlotMarker.Config.PlotStyle;
			var cellX = CellWidth + style.LineWidth;
			var cellY = CellHeight + style.LineWidth;
			var numX = (Width - style.LineWidth) / cellX;
			var numY = (Height - style.LineWidth) / cellY;
			//args.Player.SendInfoMessage("设计宽度{0}，高度{1}",Width,Height);
			Width = numX*cellX + style.LineWidth;
			Height = numY*cellY + style.LineWidth;
			//args.Player.SendInfoMessage("新宽度{0}，高度{1}", Width, Height);

			//draw horizental line
			for (var y = 0; y < numY; y++)
			{
				for (var x = 0; x < Width; x++)
				{
					for (var t = 0; t < style.LineWidth; t++)
					{
						TileHelper.SetTile(X + x, Y + y*cellY + t, style.TileId, style.TilePaint);
						TileHelper.SetWall(X + x, Y + y*cellY + t, style.WallId, style.WallPaint);
					}
				}
			}

			//draw vertical line
			for (var x = 0; x < numX; x = x++)
			{
				for (var y = 0; y < Height; y++)
				{
					for (var t = 0; t < style.LineWidth; t++)
					{
						TileHelper.SetTile(X + x*cellX + t, Y + y, style.TileId, style.TilePaint);
						TileHelper.SetWall(X + x*cellX + t, Y + y, style.WallId, style.WallPaint);
					}
				}
			}

			TileHelper.ResetSection(X, Y, Width, Height);

			Cells.Clear();
			for (var x = 0; x < numX; x = x ++)
			{
				for (var y = 0; y < numY; y = y ++)
				{
					var cell = new Cell
					{
						Id = Cells.Count,
						Parent = this,
						X = X + x*cellX + style.LineWidth,
						Y = Y + y*cellY + style.LineWidth,
						Owner = Owner,
						AllowedIDs = new List<int>()
					};
					//new Rectangle(startX + x + style.LineWidth, startY + y + style.LineWidth, style.RoomWidth, style.RoomHeight)
					Cells.Add(cell);
				}
			}
			PlotMarker.Plots.AddCells(this);
		}

		/// <summary>
		/// 根据物块坐标寻找Cell索引.
		/// </summary>
		/// <param name="tileX">物块X坐标(必须在属地内)</param>
		/// <param name="tileY">物块Y坐标(必须在属地内)</param>
		/// <returns><see cref="Cells"/>索引</returns>
		public int FindCell(int tileX, int tileY)
		{
			if (!new Rectangle(X, Y, Width, Height).Contains(tileX, tileY))
			{
				throw new ArgumentException("物块坐标必须在本属地内部!");
			}
			var style = PlotMarker.Config.PlotStyle;
			var cellX = CellWidth + style.LineWidth;
			var cellY = CellHeight + style.LineWidth;
			var numY = (Height - style.LineWidth) / cellY;
			var x = tileX - X;
			var y = tileY - Y;

			// 从上至下再从左到右计数
			return numY * (x / cellX) + y / cellY;
			// 从左到右再从上到下计数
			//return numX*(x/cellY) + y/cellX;
		}
	}

	internal sealed class Cell
	{
		/// <summary> Cell在 <see cref="Plot.Cells"/> 的索引 </summary>
		public int Id { get; set; }

		/// <summary> Cell所属的 <see cref="Plot"/> 引用 </summary>
		public Plot Parent { get; set; }

		/// <summary> Cell的起始X坐标 </summary>
		public int X { get; set; }

		/// <summary> Cell的起始X坐标 </summary>
		public int Y { get; set; }

		/// <summary> 属地的主人 </summary>
		public string Owner { get; set; }

		/// <summary>
		/// 玩家 <see cref="Owner"/> 领取属地的时间
		/// 用于判定过期 
		/// </summary>
		public DateTime GetTime { get; set; }

		/// <summary> 有权限动属地者 </summary>
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
