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
			// 没登录玩家不允许操作(防止未登录的同名玩家修改)
			if (!player.IsLoggedIn)
			{
				return true;
			}

			// 有权限玩家不阻拦
			if (player.HasPermission("pm.build.everywhere"))
			{
				return false;
			}

			var plot = PlotMarker.Plots.Plots.FirstOrDefault(p => p.Contains(tileX, tileY));
			// 若不在属地则不阻拦
			if (plot == null)
			{
				return false;
			}
			// 若在属地, 且是属地主人, 则不阻拦
			if (plot.Owner == player.Name)
			{
				return false;
			}
			// 因为墙壁的一部分算在格子内部, 所以应该先检测是否为墙
			// 若是墙 看有无权限, 无权限直接pass
			if (plot.IsWall(tileX, tileY))
			{
				if (player.HasPermission("pm.build.wall"))
				{
					return false;
				}
				// 因为墙也算Cell中一部分,所以不能执行检测cell
				else
				{
					return true;
				}
			}
			// 若不是墙, 则考虑在cell的情况
			var index = plot.FindCell(tileX, tileY);
			if (index > -1 && index < plot.Cells.Count)
			{
				if (plot.Cells[index].Owner == player.Name)
				{
					return false;
				}
				if (plot.Cells[index].AllowedIDs.Contains(player.User.ID))
				{
					return false;
				}
			}

			return true;
		}
	}
}
