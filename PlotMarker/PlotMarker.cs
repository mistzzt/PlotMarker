using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace PlotMarker
{
	[ApiVersion(2, 0)]
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
			ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
				ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
			}
			base.Dispose(disposing);
		}

		private static void OnInitialize(EventArgs e)
		{
			Config = Configuration.Read(Configuration.ConfigPath);
			Config.Write(Configuration.ConfigPath);
			Plots = new PlotManager(TShock.DB);

			Commands.ChatCommands.Add(new Command("pm.admin.areamanage", AreaManage, "areamanage", "属地区域", "am")
			{
				AllowServer = false,
				HelpText = "管理属地区域, 只限管理."
			});

			Commands.ChatCommands.Add(new Command("pm.player.getcell", MyPlot, "myplot", "属地", "mp")
			{
				AllowServer = false,
				HelpText = "管理玩家自己的属地区域."
			});

			Commands.ChatCommands.Add(new Command("pm.admin.cellmanage", CellManage, "cellmanage", "格子", "cm")
			{
				AllowServer = false,
				HelpText = "管理玩家属地区域, 只限管理."
			});
		}

		private static void OnPostInitialize(EventArgs args)
		{
			Plots.Reload();
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
			if (args.Handled)
			{
				return;
			}

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

		private static void AreaManage(CommandArgs args)
		{
			var cmd = args.Parameters.Count > 0 ? args.Parameters[0].ToLower() : "help";
			var info = args.Player.GetInfo();

			switch (cmd)
			{
				case "点":
				case "point":
					{
						if (args.Parameters.Count != 2)
						{
							args.Player.SendErrorMessage("语法无效. 正确语法: /am point <1/2>");
							return;
						}
						byte point;
						if (!byte.TryParse(args.Parameters[1], out point) || point > 2 || point < 1)
						{
							args.Player.SendErrorMessage("选点无效. 正确: /am point <1/2>");
							return;
						}
						info.Point = point;
						args.Player.SendInfoMessage("敲击物块以设定点 {0}", point);
					}
					break;
				case "区域":
				case "area":
					{
						if (args.Parameters.Count != 1)
						{
							args.Player.SendErrorMessage("语法无效. 正确语法: /am area");
							return;
						}
						info.Point = 3;
						args.Player.SendInfoMessage("使用电路设计图选定区域.");
					}
					break;
				case "定义":
				case "define":
					{
						if (args.Parameters.Count != 2)
						{
							args.Player.SendErrorMessage("语法无效. 正确语法: /am define <区域名>");
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
				case "删除":
				case "del":
					{
						if (args.Parameters.Count != 2)
						{
							args.Player.SendErrorMessage("语法无效. 正确语法: /am del <区域名>");
							return;
						}
						var name = args.Parameters[1];
						var plot = Plots.GetPlotByName(name);
						if (plot == null)
						{
							args.Player.SendErrorMessage("未找到属地!");
							return;
						}
						if (Plots.DelPlot(plot))
						{
							args.Player.SendSuccessMessage("成功删除属地.");
							return;
						}
						args.Player.SendErrorMessage("删除属地失败!");
					}
					break;
				case "划分":
				case "mark":
					{
						if (args.Parameters.Count < 2 || args.Parameters.Count > 3)
						{
							args.Player.SendErrorMessage("语法无效. 正确语法: /am mark <区域名> [Clear:true/false]");
							return;
						}
						var name = args.Parameters[1];
						var plot = Plots.GetPlotByName(name);
						if (plot == null)
						{
							args.Player.SendErrorMessage("未找到属地!");
							return;
						}
						var clear = true;
						if (args.Parameters.Count == 3)
						{
							switch (args.Parameters[2].ToLower())
							{
								case "true":
									break;
								case "false":
									clear = false;
									break;
								default:
									args.Player.SendErrorMessage("Clear属性值只能为 true/false");
									return;
							}
						}
						plot.Generate(clear);
					}
					break;
				case "信息":
				case "info":
					{
						if (args.Parameters.Count != 2)
						{
							args.Player.SendErrorMessage("语法无效. 正确语法: /am info <区域名>");
							return;
						}
						var name = args.Parameters[1];
						var plot = Plots.GetPlotByName(name);
						if (plot == null)
						{
							args.Player.SendErrorMessage("未找到属地!");
							return;
						}
						int pageNumber;
						if (!PaginationTools.TryParsePageNumber(args.Parameters, 2, args.Player, out pageNumber))
						{
							return;
						}
						var list = new List<string>
						{
							$" * 区域信息: {{{plot.X}, {plot.Y}, {plot.Width}, {plot.Height}}}",
							$" * 格子信息: w={plot.CellWidth}, h={plot.CellHeight}, cur={plot.Cells.Count}, used={plot.Cells.Count(c=>!string.IsNullOrWhiteSpace(c.Owner))}",
							$" * 创建者名: {plot.Owner}"
						};
						PaginationTools.SendPage(args.Player, pageNumber, list,
							new PaginationTools.Settings
							{
								HeaderFormat = "属地 " + plot.Name + " 说明 ({0}/{1}):",
								FooterFormat = "键入 {0}pm info {1} {{0}} 以获取下一页列表.".SFormat(Commands.Specifier, plot.Name),
								NothingToDisplayString = "当前没有说明."
							});
					}
					break;
				case "列表":
				case "list":
					{
						int pageNumber;
						if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
						{
							return;
						}

						var plots = Plots.Plots.Select(p => p.Name);

						PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(plots),
							new PaginationTools.Settings
							{
								HeaderFormat = "属地列表 ({0}/{1}):",
								FooterFormat = "键入 {0}pm list {{0}} 以获取下一页列表.".SFormat(Commands.Specifier),
								NothingToDisplayString = "当前没有属地."
							});
					}
					break;
				case "重载":
				case "reload":
					{
						Plots.Reload();
						Config = Configuration.Read(Configuration.ConfigPath);
						Config.Write(Configuration.ConfigPath);
						args.Player.SendSuccessMessage("重载完毕.");
					}
					break;
				case "帮助":
				case "help":
					{
						int pageNumber;
						if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
						{
							return;
						}
						var list = new List<string>
						{
							"point <1/2> - 选中点",
							"area - 选择区域",
							"define <属地名> - 定义属地",
							"del <属地名> - 删除属地",
							"mark <属地名> - 在属地中生成格子",
							"info <属地名> - 查看属地属性",
							"list [页码] - 查看现有的属地",
							"help [页码] - 获取帮助",
							"reload - 载入数据库数据"
						};
						PaginationTools.SendPage(args.Player, pageNumber, list,
							new PaginationTools.Settings
							{
								HeaderFormat = "属地管理子指令说明 ({0}/{1}):",
								FooterFormat = "键入 {0}am help {{0}} 以获取下一页列表.".SFormat(Commands.Specifier),
								NothingToDisplayString = "当前没有说明."
							});
					}
					break;
				default:
					{
						args.Player.SendWarningMessage("子指令无效! 输入 {0} 获取帮助信息.",
							TShock.Utils.ColorTag("/am help", Color.Cyan));
					}
					break;
			}
		}

		private static void MyPlot(CommandArgs args)
		{
			if (!args.Player.IsLoggedIn)
			{
				args.Player.SendErrorMessage("你未登录, 无法使用属地.");
				return;
			}

			var cmd = args.Parameters.Count > 0 ? args.Parameters[0].ToLower() : "help";
			var info = args.Player.GetInfo();

			switch (cmd)
			{
				case "点":
				case "point":
					{
						if (args.Parameters.Count != 1)
						{
							args.Player.SendErrorMessage("语法无效. 正确语法: {0}", TShock.Utils.ColorTag("/属地 点", Color.Cyan));
							return;
						}
						info.Point = 4;
						args.Player.SendInfoMessage("在空白属地内放置任意物块.");
					}
					break;
				case "获取":
				case "get":
					{
						if (args.Parameters.Count != 1)
						{
							args.Player.SendErrorMessage("语法无效. 正确语法: {0}", TShock.Utils.ColorTag("/属地 获取", Color.Cyan));
							return;
						}

						var count = Plots.GetTotalCells(args.Player.User.Name);
						var max = args.Player.GetMaxCells();
						if (max != -1 && count >= args.Player.GetMaxCells())
						{
							args.Player.SendErrorMessage("你无法获取更多属地. (你当前有{0}个/最多{1}个)", count, max);
							return;
						}
						info.Point = 5;
						info.OnGetPoint = (x, y, receiver) =>
						{
							Plots.ApplyForCell(receiver, x, y);
						};
						args.Player.SendInfoMessage("在空白属地内放置任意物块, 来确定你的属地位置.");
					}
					break;
				case "允许":
				case "添加":
				case "allow":
					{
						if (args.Parameters.Count < 2)
						{
							args.Player.SendErrorMessage("语法无效. 正确语法: {0}", TShock.Utils.ColorTag("/属地 允许 <玩家名>", Color.Cyan));
							return;
						}
						Cell cell = null;

						var count = Plots.GetTotalCells(args.Player.User.Name);
						if (count == 0)
						{
							args.Player.SendErrorMessage("你没有属地!");
							return;
						}
						else if (count == 1)
						{
							cell = Plots.GetOnlyCellOfPlayer(args.Player.User.Name);
							if (cell == null)
							{
								args.Player.SendErrorMessage("载入属地失败! (不唯一或缺少)");
								return;
							}
						}
						else if (count > 1)
						{
							if (info.CellPoint == Point.Zero)
							{
								args.Player.SendErrorMessage("你有多个属地, 使用 {0} 以选中欲修改的属地.", TShock.Utils.ColorTag("/属地 点", Color.Cyan));
								return;
							}
							else
							{
								cell = Plots.GetCellByPosition(info.CellPoint.X, info.CellPoint.Y);
							}
						}

						var playerName = string.Join(" ", args.Parameters.Skip(1));

						if (TShock.Users.GetUserByName(playerName) == null)
						{
							args.Player.SendErrorMessage("玩家 " + playerName + " 未找到");
							return;
						}

						if (cell != null)
						{
							if (cell.Owner != args.Player.User.Name && !args.Player.HasPermission("pm.admin.editall"))
							{
								args.Player.SendErrorMessage("你不是该属地的主人.");
								return;
							}

							if (Plots.AddCellUser(cell, playerName))
							{
								args.Player.SendInfoMessage("添加用户 " + playerName + " 完毕.");
							}
							else
								args.Player.SendErrorMessage("添加用户时出现问题.");
						}
						else
						{
							args.Player.SendErrorMessage("该点坐标不在属地内.");
						}
					}
					break;
				case "禁止":
				case "删除":
				case "disallow":
					{
						if (args.Parameters.Count < 2)
						{
							args.Player.SendErrorMessage("语法无效. 正确语法: {0}", TShock.Utils.ColorTag("/属地 禁止 <玩家名>", Color.Cyan));
							return;
						}
						Cell cell = null;

						var count = Plots.GetTotalCells(args.Player.User.Name);
						if (count == 0)
						{
							args.Player.SendErrorMessage("你没有属地!");
							return;
						}
						else if (count == 1)
						{
							cell = Plots.GetOnlyCellOfPlayer(args.Player.User.Name);
							if (cell == null)
							{
								args.Player.SendErrorMessage("载入属地失败! (不唯一或缺少)");
								return;
							}
						}
						else if (count > 1)
						{
							if (info.CellPoint == Point.Zero)
							{
								args.Player.SendErrorMessage("你有多个属地, 使用 {0} 以选中欲修改的属地.", TShock.Utils.ColorTag("/属地 点", Color.Cyan));
								return;
							}
							else
							{
								cell = Plots.GetCellByPosition(info.CellPoint.X, info.CellPoint.Y);
							}
						}

						var playerName = string.Join(" ", args.Parameters.Skip(1));

						if (TShock.Users.GetUserByName(playerName) == null)
						{
							args.Player.SendErrorMessage("玩家 " + playerName + " 未找到");
							return;
						}

						if (cell != null)
						{
							if (cell.Owner != args.Player.User.Name && !args.Player.HasPermission("pm.admin.editall"))
							{
								args.Player.SendErrorMessage("你不是该属地的主人.");
								return;
							}

							if (Plots.RemoveCellUser(cell, playerName))
							{
								args.Player.SendInfoMessage("移除用户 " + playerName + " 完毕.");
							}
							else
								args.Player.SendErrorMessage("移除用户时出现问题.");
						}
						else
						{
							args.Player.SendErrorMessage("该点坐标不在属地内.");
						}
					}
					break;
				case "信息":
				case "查询":
				case "info":
					{
						if (args.Parameters.Count != 1)
						{
							args.Player.SendErrorMessage("语法无效. 正确语法: {0}", TShock.Utils.ColorTag("/属地 信息", Color.Cyan));
							return;
						}
						info.Point = 5;
						info.OnGetPoint = (tileX, tileY, player) =>
						{
							var cell = Plots.GetCellByPosition(tileX, tileY);
							if (cell != null)
							{
								if (cell.Owner != args.Player.User.Name && !args.Player.HasPermission("pm.admin.editall"))
								{
									args.Player.SendErrorMessage("你不是该属地的主人.");
									return;
								}
								cell.GetInfo(player);
								return;
							}
							player.SendErrorMessage("选择点不在属地内.");
						};
						args.Player.SendInfoMessage("在你的属地内放置任意物块, 来查看你的属地信息.");
					}
					break;
				case "帮助":
				case "help":
					{
						int pageNumber;
						if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
						{
							return;
						}
						var list = new List<string>
						{
							"获取 - 获取选中点区域 (get/获取)",
							"点 - 设置临时点 (point/获取)",
							"允许 <玩家名> - 给自己的属地增加协助者 (allow/允许/添加)",
							"禁止 <玩家名> - 移除协助者 (disallow/禁止/删除)",
							"信息 - 查看当前点坐标所在属地的信息 (info/信息/查询)",
							"帮助 [页码] - 获取帮助 (help/帮助)"
						};
						PaginationTools.SendPage(args.Player, pageNumber, list,
							new PaginationTools.Settings
							{
								HeaderFormat = "玩家属地子指令说明 ({0}/{1}):",
								FooterFormat = "键入 {0}属地 帮助 {{0}} 以获取下一页列表.".SFormat(Commands.Specifier),
								NothingToDisplayString = "当前没有说明."
							});
					}
					break;
				default:
					{
						args.Player.SendWarningMessage("子指令无效! 输入 {0} 获取帮助信息.",
							TShock.Utils.ColorTag("/属地 帮助", Color.Cyan));
					}
					break;
			}
		}

		private static void CellManage(CommandArgs args)
		{
			var cmd = args.Parameters.Count > 0 ? args.Parameters[0].ToLower() : "help";
			var info = args.Player.GetInfo();

			switch (cmd)
			{
				case "fuck":
				case "艹":
					{
						if (args.Parameters.Count != 1)
						{
							args.Player.SendErrorMessage("语法无效. 正确语法: /mp fuck");
							return;
						}
						info.Point = 5;
						info.OnGetPoint = (tileX, tileY, player) =>
						{
							var cell = Plots.GetCellByPosition(tileX, tileY);
							if (cell != null)
							{
								Plots.FuckCell(cell);
								player.SendSuccessMessage("愉悦, 艹完了.");
								return;
							}
							player.SendErrorMessage("选择点不在属地内.");
						};
						args.Player.SendErrorMessage("在属地内放置任意物块, 来艹无聊的东西.");
					}
					break;
				case "info":
					{
						if (args.Parameters.Count != 1)
						{
							args.Player.SendErrorMessage("语法无效. 正确语法: /gm get");
							return;
						}
						info.Point = 5;
						info.OnGetPoint = (tileX, tileY, player) =>
						{
							var cell = Plots.GetCellByPosition(tileX, tileY);
							if (cell != null)
							{
								cell.GetInfo(player);
								return;
							}
							player.SendErrorMessage("选择点不在属地内.");
						};
						args.Player.SendInfoMessage("在属地内放置任意物块, 来查看属地信息.");
					}
					break;
				case "help":
					{
						int pageNumber;
						if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
						{
							return;
						}
						var list = new List<string>
						{
							"info - 获取选中点区域信息",
							"fuck - 重置选中点区域",
							"chown - 更改选中点区域所有者(未完成)",
							"help [页码] - 获取帮助"
						};
						PaginationTools.SendPage(args.Player, pageNumber, list,
							new PaginationTools.Settings
							{
								HeaderFormat = "玩家属地管理子指令说明 ({0}/{1}):",
								FooterFormat = "键入 {0}cm help {{0}} 以获取下一页列表.".SFormat(Commands.Specifier),
								NothingToDisplayString = "当前没有说明."
							});
					}
					break;
				default:
					{
						args.Player.SendWarningMessage("子指令无效! 输入 {0} 获取帮助信息.",
							TShock.Utils.ColorTag("/cm help", Color.Cyan));
					}
					break;
			}
		}

		public static bool BlockModify(TSPlayer player, int tileX, int tileY)
		{
			if (!BlockModify_Inner(player, tileX, tileY))
			{
				return false;
			}
			var info = player.GetInfo();
			if (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - info.BPm > 2000)
			{
				player.SendErrorMessage("该属地被保护, 无法更改物块.");
				info.BPm = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
			}

			return true;
		}

		private static bool BlockModify_Inner(TSPlayer player, int tileX, int tileY)
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
				var na = plot.Cells[index].AllowedIDs?.Contains(player.User.ID);
				if (na.HasValue && na.Value)
				{
					return false;
				}
			}

			return true;
		}
	}
}
