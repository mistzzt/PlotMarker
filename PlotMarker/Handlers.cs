using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace PlotMarker
{
	internal static class Handlers
	{
		private static readonly Dictionary<PacketTypes, GetDataHandlerDelegate> GetDataHandlerDelegates
			= new Dictionary<PacketTypes, GetDataHandlerDelegate> {
				{ PacketTypes.TileKill, HandleTileKill },
				{ PacketTypes.PaintTile, HandlePaintTile },
				{ PacketTypes.PaintWall, HandlePaintWall },
				{ PacketTypes.Tile, HandleTile },
				{ PacketTypes.PlaceObject, HandlePlaceObject },
				{ PacketTypes.TileSendSquare, HandleSendTileSquare },
				{ PacketTypes.MassWireOperation, HandleMassWireOperation }
			};

		public static bool HandleGetData(PacketTypes type, TSPlayer player, MemoryStream data)
		{
			GetDataHandlerDelegate handler;
			if (GetDataHandlerDelegates.TryGetValue(type, out handler))
			{
				try
				{
					return handler(new GetDataHandlerArgs(player, data));
				}
				catch (Exception ex)
				{
					TShock.Log.ConsoleError("[PlotMarker] 处理数据内未捕获的异常: 详情查看日志.");
					TShock.Log.Error(ex.ToString());
					return true;
				}
			}
			return false;
		}

		private static bool HandleSendTileSquare(GetDataHandlerArgs args)
		{
			return false;
		}

		private static bool HandleTileKill(GetDataHandlerArgs args)
		{
			return false;
		}

		private static bool HandlePaintTile(GetDataHandlerArgs args)
		{
			return false;
		}

		private static bool HandlePaintWall(GetDataHandlerArgs args)
		{
			return false;
		}

		private static bool HandleTile(GetDataHandlerArgs args)
		{
			var info = args.Player.GetInfo();
			using (var reader = new BinaryReader(args.Data))
			{
				reader.ReadByte();
				int x = reader.ReadInt16();
				int y = reader.ReadInt16();

				if (info.Point != 0)
				{
					if (x >= 0 && y >= 0 && x < Main.maxTilesX && y < Main.maxTilesY)
					{
						if (info.Point == 1)
						{
							info.X = x;
							info.Y = y;
							args.Player.SendInfoMessage("设定点 1 完毕.");
						}
						else if (info.Point == 2)
						{
							info.X2 = x;
							info.Y2 = y;
							args.Player.SendInfoMessage("设定点 2 完毕.");
						}
						else if (info.Point == 4)
						{
							info.CellPoint = new Point(x, y);
							args.Player.SendInfoMessage("设定临时点坐标完毕.");
						}
						info.Point = 0;
						args.Player.SendTileSquare(x, y, 3);
						return true;
					}
				}

				if (PlotMarker.BlockModify(args.Player, x, y))
				{
					args.Player.SendTileSquare(x, y, 3);
					return true;
				}
			}
				
			return false;
		}

		private static bool HandlePlaceObject(GetDataHandlerArgs args)
		{
			return false;
		}

		private static bool HandleMassWireOperation(GetDataHandlerArgs args)
		{
			var data = args.Player.GetInfo();
			if (data.Point != 0)
			{
				using (var reader = new BinaryReader(args.Data))
				{
					int startX = reader.ReadInt16();
					int startY = reader.ReadInt16();
					int endX = reader.ReadInt16();
					int endY = reader.ReadInt16();

					if (startX >= 0 && startY >= 0 && endX >= 0 && endY >= 0 && startX < Main.maxTilesX && startY < Main.maxTilesY && endX < Main.maxTilesX && endY < Main.maxTilesY)
					{
						if (startX == endX && startY == endY)
						{
							// Set a single point
							if (data.Point == 1)
							{
								data.X = startX;
								data.Y = startY;
								args.Player.SendInfoMessage("设定点 1 完毕.");
							}
							else if (data.Point == 2)
							{
								data.X2 = startX;
								data.Y2 = startY;
								args.Player.SendInfoMessage("设定点 2 完毕.");
							}
							else if (data.Point == 3)
							{
								args.Player.SendInfoMessage("你需要选中一个区域.");
							}
							else if (data.Point == 4)
							{
								data.CellPoint = new Point(startX, startY);
								args.Player.SendInfoMessage("设定临时点坐标完毕.");
							}
						}
						else
						{
							if (data.Point != 4)
							{
								// Set both points at the same time
								data.X = startX;
								data.Y = startY;
								data.X2 = endX;
								data.Y2 = endY;
								args.Player.SendInfoMessage("设定区域完毕.");
							}
							else
							{
								args.Player.SendErrorMessage("不支持选中区域作为临时点坐标.");
							}
						}
						data.Point = 0;
						return true;
					}
				}
			}

			return false;
		}
	}
}
