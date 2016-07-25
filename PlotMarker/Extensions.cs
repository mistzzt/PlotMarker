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
			Debugger.Break();
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
	}
}
