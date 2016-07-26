using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace PlotMarker
{
	internal static class Extensions
	{
		public static T NotNull<T>(this T obj)
		{
			if (obj == null)
			{
				throw new Exception($"类型为{typeof(T).FullName}对象不应为Null.");
			}

			return obj;
		}

		public static PlayerInfo GetInfo(this TSPlayer player)
		{
			var info = player.GetData<PlayerInfo>(PlotMarker.PlotMarkerInfoKey);
#if DEBUG
			info = info.NotNull();
#else
			if (info == null)
			{
				info = new PlayerInfo();
				player.SetData(PlotMarker.PlotMarkerInfoKey, info);
			}
#endif
			return info;
		}

		public static bool BlockModify(this TSPlayer player, int tileX, int tileY)
		{
			if (!player.IsLoggedIn)
			{
				return true;
			}

			if (player.HasPermission("pm.build.everywhere"))
			{
				return false;
			}

			var plot = PlotMarker.Plots.Plots.FirstOrDefault(p => p.Contains(tileX, tileY));
			if (plot == null)
			{
				return false;
			}
			if (plot.Owner == player.Name && player.IsLoggedIn)
			{
				return false;
			}
			if (plot.IsWall(tileX, tileY) && player.HasPermission("pm.build.wall"))
			{
				return false;
			}
			var index = plot.FindCell(tileX, tileY);
			if (index > -1 && index < plot.Cells.Count)
			{
				if (plot.Cells[index].Owner == player.Name)
				{
					return false;
				}
			}

			return true;
		}
	}
}
