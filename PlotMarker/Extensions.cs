﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
			for (var i = 0; i < player.Group.permissions.Count; i++)
			{
				var perm = player.Group.permissions[i];
				var match = Regex.Match(perm, @"^pm\.cell\.(\d{1,9})$");
				if (match.Success)
					return int.Parse(match.Groups[1].Value);
			}
			return 1;
		}
	}
}
