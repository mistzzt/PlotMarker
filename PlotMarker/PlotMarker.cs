using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;

namespace PlotMarker
{
	[ApiVersion(1, 23)]
	public class PlotMarker : TerrariaPlugin
	{
		public override string Name => "PlotMarker";
		public override string Author => "MistZZT & XiaoR";
		public override string Description => "Marks plots for players and manages them.";
		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

		internal static Configuration Config;

		public PlotMarker(Main game) : base(game) { }

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
			}
			base.Dispose(disposing);
		}

		private static void OnInitialize(EventArgs e)
		{
			Config = Configuration.Read(Configuration.ConfigPath);
			Config.Write(Configuration.ConfigPath);
		}
	}
}
