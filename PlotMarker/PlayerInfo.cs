using System;
using Terraria;

namespace PlotMarker
{
	internal class PlayerInfo
	{
		private int _x = -1;
		private int _x2 = -1;
		private int _y = -1;
		private int _y2 = -1;

		public int X {
			get { return _x; }
			set { _x = Math.Max(0, value); }
		}

		public int X2 {
			get { return _x2; }
			set { _x2 = Math.Min(value, Main.maxTilesX - 1); }
		}

		public int Y {
			get { return _y; }
			set { _y = Math.Max(0, value); }
		}

		public int Y2 {
			get { return _y2; }
			set { _y2 = Math.Min(value, Main.maxTilesY - 1); }
		}

		public byte Point = 0;

		/// <summary>
		/// Permission to build message cool down.
		/// </summary>
		public long BPm = 1;
	}
}
