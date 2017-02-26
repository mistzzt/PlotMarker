using System;
using System.Text.RegularExpressions;
using TShockAPI;

namespace PlotMarker
{
	internal static class Extensions
	{
		public static T NotNull<T>(this T obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException(nameof(obj), $"类型为{typeof(T).FullName}对象不应为Null.");
			}

			return obj;
		}

		public static PlayerInfo GetInfo(this TSPlayer player)
		{
			var info = player.GetData<PlayerInfo>(PlotMarker.PlotMarkerInfoKey);
			if (info == null)
			{
				info = new PlayerInfo();
				player.SetData(PlotMarker.PlotMarkerInfoKey, info);
			}
			return info;
		}

		public static int GetMaxCells(this TSPlayer player)
		{
			if (!player.IsLoggedIn)
			{
				return 0;
			}
			if (player.HasPermission("pm.cell.infinite"))
			{
				return -1;
			}
			foreach (var perm in player.Group.permissions)
			{
				var match = Regex.Match(perm, @"^pm\.cell\.(\d{1,9})$");
				if (match.Success)
					return int.Parse(match.Groups[1].Value);
			}
			return 1; // 默认一个
		}
	}
}
