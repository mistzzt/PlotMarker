using System;
using System.Collections.Generic;
using System.IO;
using TShockAPI;

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
			return false;
		}

		private static bool HandlePlaceObject(GetDataHandlerArgs args)
		{
			return false;
		}
	}
}
