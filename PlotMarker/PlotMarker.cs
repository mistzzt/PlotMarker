using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace PlotMarker
{
	[ApiVersion(1, 23)]
	public sealed class PlotMarker : TerrariaPlugin
	{
		public override string Name => "PlotMarker";
		public override string Author => "MR.H";
		public override string Description => "Marks plots for players and manages them.";
		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

		internal static Configuration Config;
		internal static PlotManager Plots;

		public PlotMarker(Main game) : base(game) { }

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			ServerApi.Hooks.NetGetData.Register(this, OnGetData, 1000);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
			}
			base.Dispose(disposing);
		}

		private static void OnInitialize(EventArgs e)
		{
			Config = Configuration.Read(Configuration.ConfigPath);
			Config.Write(Configuration.ConfigPath);

			Commands.ChatCommands.Add(new Command("pm.user.myplot", MyPlot, "myplot", "属地", "mp")
			{
				AllowServer = false,
				HelpText = "自动分配属地给玩家."
			});
		}

		private static void OnGetData(GetDataEventArgs args)
		{
			var type = args.MsgID;

			var player = TShock.Players[args.Msg.whoAmI];
			if (player == null || !player.ConnectionAlive)
			{
				return;
			}

			using (var data = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length - 1))
			{
				args.Handled = Handlers.HandleGetData(type, player, data);
			}
		}

		private static void MyPlot(CommandArgs args)
		{
			
		}
	}
}
