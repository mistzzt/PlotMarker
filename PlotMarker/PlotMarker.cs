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
		public static readonly string PlotMarkerInfoKey = "pm_info_key";

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
			ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
			}
			base.Dispose(disposing);
		}

		private static void OnInitialize(EventArgs e)
		{
			Config = Configuration.Read(Configuration.ConfigPath);
			Config.Write(Configuration.ConfigPath);
			Plots = new PlotManager(TShock.DB);

			Commands.ChatCommands.Add(new Command("pm.admin.plotmanage", PlotManage, "pm", "属地", "plotmanage")
			{
				AllowServer = false,
				HelpText = "管理属地."
			});
		}

		private static void OnGreet(GreetPlayerEventArgs args)
		{
#if DEBUG
			var player = TShock.Players[args.Who].NotNull();
#else
			var player = TShock.Players[args.Who];
			player?.SetData(PlotMarkerInfoKey, new PlayerInfo());
#endif
			player.SetData(PlotMarkerInfoKey, new PlayerInfo());
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

		private static void PlotManage(CommandArgs args)
		{
			var cmd = args.Parameters.Count > 0 ? args.Parameters[0].ToLower() : "help";
			var info = args.Player.GetInfo();

			switch (cmd)
			{
				case "点":
				case "p":
				case "point":
				{
					if (args.Parameters.Count != 2)
					{
						args.Player.SendErrorMessage("语法无效. 正确语法: /pm point <1/2>");
						return;
					}
					byte point;
					if (!byte.TryParse(args.Parameters[1], out point) || point > 2 || point < 1)
					{
							args.Player.SendErrorMessage("选点无效. 正确: /pm point <1/2>");
							return;
					}
					info.Point = point;
					args.Player.SendInfoMessage("敲击物块以设定点 {0}", point);
				}
					break;
				case "区":
				case "区域":
				case "a":
				case "area":
					{
						if (args.Parameters.Count != 1)
						{
							args.Player.SendErrorMessage("语法无效. 正确语法: /pm area");
							return;
						}
						info.Point = 3;
						args.Player.SendInfoMessage("使用电路设计图选定区域.");
					}
					break;
				case "定":
				case "定义":
				case "d":
				case "define":
					{
						if (args.Parameters.Count != 2)
						{
							args.Player.SendErrorMessage("语法无效. 正确语法: /pm define <区域名>");
							return;
						}
						if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
						{
							args.Player.SendErrorMessage("你需要先选择区域.");
							return;
						}
						if (Plots.AddPlot(info.X, info.Y, info.X2 - info.X, info.Y2 - info.Y,
							args.Parameters[1], args.Player.Name,
							Main.worldID.ToString(), Config.PlotStyle))
						{
							args.Player.SendSuccessMessage("添加属地 {0} 完毕.", args.Parameters[1]);
						}
						else
						{
							args.Player.SendSuccessMessage("属地 {0} 已经存在, 请更换属地名后重试.", args.Parameters[1]);
						}
						
					}
					break;
				case "划":
				case "划分":
				case "m":
				case "mark":
					{
						
					}
					break;
				case "查":
				case "信息":
				case "i":
				case "fuck":
				case "info":
					{

					}
					break;
				case "帮助":
				case "h":
				case "help":
					{

					}
					break;
				default:
					{

					}
					break;
			}
		}
	}
}
