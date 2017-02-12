using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace PlotMarker
{
	internal sealed class PlotManager
	{
		public List<Plot> Plots = new List<Plot>();

		private readonly IDbConnection _database;

		private readonly object _addCellLock = new object();

		public PlotManager(IDbConnection connection)
		{
			_database = connection;

			var plotTable = new SqlTable("Plots",
				new SqlColumn("Id", MySqlDbType.Int32) { AutoIncrement = true, Primary = true },
				new SqlColumn("Name", MySqlDbType.VarChar, 10) { NotNull = true, Unique = true },
				new SqlColumn("X", MySqlDbType.Int32),
				new SqlColumn("Y", MySqlDbType.Int32),
				new SqlColumn("Width", MySqlDbType.Int32),
				new SqlColumn("Height", MySqlDbType.Int32),
				new SqlColumn("CellWidth", MySqlDbType.Int32),
				new SqlColumn("CellHeight", MySqlDbType.Int32),
				new SqlColumn("WorldId", MySqlDbType.VarChar, 50),
				new SqlColumn("Owner", MySqlDbType.VarChar, 50)
			);

			var cellTable = new SqlTable("Cells",
				new SqlColumn("Id", MySqlDbType.Int32) { AutoIncrement = true, Primary = true },
				new SqlColumn("Position", MySqlDbType.VarChar, 5) { Unique = true },
				new SqlColumn("X", MySqlDbType.Int32),
				new SqlColumn("Y", MySqlDbType.Int32),
				new SqlColumn("Owner", MySqlDbType.VarChar, 50),
				new SqlColumn("UserIds", MySqlDbType.Text),
				new SqlColumn("GetTime", MySqlDbType.Text),
				new SqlColumn("LastAccess", MySqlDbType.Text)
			);

			var creator = new SqlTableCreator(_database,
											  _database.GetSqlType() == SqlType.Sqlite
												  ? (IQueryBuilder)new SqliteQueryCreator()
												  : new MysqlQueryCreator());

			try
			{
				creator.EnsureTableStructure(plotTable);
				creator.EnsureTableStructure(cellTable);
			}
			catch (Exception ex)
			{
				Debugger.Break();
				TShock.Log.Error(ex.ToString());
			}
		}

		public void Reload()
		{
			Plots.Clear();

			using (var reader = _database.QueryReader("SELECT * FROM Plots WHERE WorldId = @0", Main.worldID.ToString()))
			{
				while (reader.Read())
				{
					var plot = new Plot
					{
						Id = reader.Get<int>("Id"),
						Name = reader.Get<string>("Name"),
						X = reader.Get<int>("X"),
						Y = reader.Get<int>("Y"),
						Width = reader.Get<int>("Width"),
						Height = reader.Get<int>("Height"),
						CellWidth = reader.Get<int>("CellWidth"),
						CellHeight = reader.Get<int>("CellHeight"),
						Owner = reader.Get<string>("Owner"),
					};
					plot.Cells = LoadCells(plot);
					Plots.Add(plot);
				}
			}
		}

		public List<Cell> LoadCells(Plot parent)
		{
			var list = new List<Cell>();
			using (var reader = _database.QueryReader("SELECT * FROM `cells` WHERE `cells`.`Position` LIKE @0 ORDER BY `cells`.`Id` ASC",
				string.Concat(parent.Id, ":%")))
			{
				while (reader.Read())
				{
					DateTime dt;
					var cell = new Cell
					{
						Parent = parent,
						Id = GetCellIndex(parent, reader),
						X = reader.Get<int>("X"),
						Y = reader.Get<int>("Y"),
						Owner = reader.Get<string>("Owner"),
						GetTime = DateTime.TryParse(reader.Get<string>("GetTime"), out dt) ? dt : default(DateTime),
						LastAccess = DateTime.TryParse(reader.Get<string>("LastAccess"), out dt) ? dt : default(DateTime),
						AllowedIDs = new List<int>()
					};
					var mergedids = reader.Get<string>("UserIds") ?? "";
					var splitids = mergedids.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					try
					{
						for (var i = 0; i < splitids.Length; i++)
						{
							int userid;

							if (int.TryParse(splitids[i], out userid)) // if unparsable, it's not an int, so silently skip
								cell.AllowedIDs.Add(userid);
							else
								TShock.Log.Warn("UserIDs 有一列不可用数据: " + splitids[i]);
						}
					}
					catch (Exception e)
					{
						TShock.Log.Error("数据库中含有无效的用户ID. (UserIDs 数据类型是整数).");
						TShock.Log.Error("很多操作会受到影响. 你必须手动删除这些数据并修复.");
						TShock.Log.Error(e.ToString());
						TShock.Log.Error(e.StackTrace);
					}
					list.Add(cell);
				}
#if DEBUG
				for (var i = 0; i < list.Count; i++)
				{
					if (list[i].Id != i)
						Debugger.Break();
				}
#endif
			}
			return list;
		}

		public bool AddPlot(int x, int y, int width, int height, string name, string owner, string worldid, Style style)
		{
			try
			{
				if (GetPlotByName(name) != null)
				{
					return false;
				}

				_database.Query(
					"INSERT INTO Plots (Name, X, Y, Width, Height, CellWidth, CellHeight, WorldId, Owner) VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8);",
					name, x, y, width, height, style.CellWidth, style.CellHeight, worldid, owner);

				using (var res = _database.QueryReader("SELECT Id FROM Plots WHERE Name = @0 AND WorldId = @1",
					name, worldid))
				{
					if (res.Read())
					{
						Plots.Add(new Plot
						{
							Id = res.Get<int>("Id"),
							Name = name,
							X = x,
							Y = y,
							Width = width,
							Height = height,
							CellHeight = style.CellHeight,
							CellWidth = style.CellWidth,
							Owner = owner
						});

						return true;
					}
					return false;
				}
			}
			catch (MySqlException ex)
			{
				if (ex.Number == (int)MySqlErrorCode.DuplicateKeyEntry)
				{
					return false;
				}
				Debugger.Break();
				TShock.Log.Error(ex.ToString());
			}
			catch (Exception ex)
			{
				Debugger.Break();
				TShock.Log.Error(ex.ToString());
			}
			return false;
		}

		public bool DelPlot(Plot plot)
		{
			if (plot == null || !Plots.Contains(plot))
			{
				return false;
			}
			try
			{
				Plots.Remove(plot);
				_database.Query("DELETE FROM Cells WHERE Position LIKE @0", string.Concat(plot.Id, ":%"));
				_database.Query("DELETE FROM Plots WHERE Id = @0", plot.Id);
				return true;
			}
			catch (Exception e)
			{
				TShock.Log.ConsoleError("[PlotMarker] 删除属地期间出现异常.");
				TShock.Log.Error(e.ToString());
			}
			return false;
		}

		public async void AddCells(Plot plot)
		{
			await Task.Run(() =>
			{
				lock (_addCellLock)
				{
					_database.Query("DELETE FROM Cells WHERE Position LIKE @0", string.Concat(plot.Id, ":%"));
					var stopwatch = new Stopwatch();
					stopwatch.Start();
					var count = 0;
					foreach (var cell in plot.Cells)
					{
						AddCell(cell);
						count++;
					}
					stopwatch.Stop();
					TShock.Log.Info("记录完毕. 共有{0}个. ({1}ms)", count, stopwatch.ElapsedMilliseconds);
				}
			});
		}

		public void AddCell(Cell cell)
		{
			try
			{
				if (_database.Query(
					"INSERT INTO Cells (Position, X, Y, UserIds, Owner, GetTime, LastAccess) VALUES (@0, @1, @2, @3, @4, @5, @6);",
					string.Concat(cell.Parent.Id, ':', cell.Id), cell.X, cell.Y, string.Empty, string.Empty, string.Empty, string.Empty) == 1)
					return;
				throw new Exception("No affected rows.");
			}
			catch (Exception e)
			{
				TShock.Log.ConsoleError("[PM] Cell数值导入数据库失败. ({0}: {1})", cell.Parent.Name, cell.Id);
				TShock.Log.Error(e.ToString());
			}
		}

		public bool ApplyForCell(TSPlayer player, Plot plot, out Cell cell)
		{
			try
			{
				cell = plot.Cells.LastOrDefault(c => string.IsNullOrWhiteSpace(c.Owner));
				if (cell == null)
				{
					player.SendWarningMessage("现在没有可用属地了.. 请联系管理.");
					return false;
				}
				cell.Owner = player.Name;
				cell.GetTime = DateTime.Now;

				if (_database.Query("UPDATE `cells` SET `Owner` = @0, `GetTime` = @1 WHERE `cells`.`Position` = @2;",
					player.User.Name,
					DateTime.Now.ToString("s"),
					string.Concat(plot.Id, ':', cell.Id)) == 1)
				{
					player.SendSuccessMessage("系统已经分配给你一块地.");
					return true;
				}
				throw new Exception("No affected rows.");
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
				player.SendErrorMessage("系统错误, 获取属地失败. 请联系管理.");
			}

			cell = null;
			return false;
		}

		public void ApplyForCell(TSPlayer player, int tileX, int tileY)
		{
			var cell = GetCellByPosition(tileX, tileY);
			if (cell == null)
			{
				player.SendErrorMessage("在选中点位置没有属地.");
				return;
			}
			if (!string.IsNullOrWhiteSpace(cell.Owner) && !player.HasPermission("plotmarker.admin.editall"))
			{
				player.SendErrorMessage("该属地已被占用.");
				return;
			}
			cell.Owner = player.Name;
			cell.GetTime = DateTime.Now;

			if (_database.Query("UPDATE `cells` SET `Owner` = @0, `GetTime` = @1 WHERE `cells`.`Position` = @2;",
				player.User.Name,
				DateTime.Now.ToString("s"),
				string.Concat(cell.Parent.Id, ':', cell.Id)) == 1)
			{
				player.SendSuccessMessage("系统已经分配给你一块地.");
				return;
			}
			throw new Exception("No affected rows.");
		}

		public bool AddCellUser(Cell cell, string userName)
		{
			try
			{
				var mergedIDs = string.Empty;
				using (
					var reader = _database.QueryReader("SELECT UserIds FROM Cells WHERE Position = @0",
													  string.Concat(cell.Parent.Id, ':', cell.Id)))
				{
					if (reader.Read())
						mergedIDs = reader.Get<string>("UserIds");
				}

				var userIdToAdd = Convert.ToString(TShock.Users.GetUserID(userName));
				var ids = mergedIDs.Split(',');
				// Is the user already allowed to the region?
				if (ids.Contains(userIdToAdd))
					return true;

				mergedIDs = string.IsNullOrEmpty(mergedIDs) ? userIdToAdd : string.Concat(mergedIDs, ",", userIdToAdd);

				var q = _database.Query("UPDATE Cells SET UserIds=@0 WHERE Position = @1",
					mergedIDs, string.Concat(cell.Parent.Id, ':', cell.Id));
				cell.SetAllowedIDs(mergedIDs);
				return q != 0;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
			return false;
		}

		public bool RemoveCellUser(Cell cell, string userName)
		{
			if (cell != null)
			{
				if (!cell.RemoveId(TShock.Users.GetUserID(userName)))
				{
					return false;
				}

				var ids = string.Join(",", cell.AllowedIDs);
				return _database.Query("UPDATE Cells SET UserIds=@0 WHERE Position = @1", ids,
					string.Concat(cell.Parent.Id, ':', cell.Id)) > 0;
			}

			return false;
		}

		public async void UpdateLastAccess(Cell cell)
		{
			await Task.Run(() =>
			{
				try
				{
					_database.Query("UPDATE Cells SET LastAccess=@0 WHERE Position=@1",
						cell.LastAccess.ToString("s"),
						string.Concat(cell.Parent.Id, ":", cell.Id));
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
				}
			});
		}

		public Cell GetCellByPosition(int tileX, int tileY)
		{
			var plot = PlotMarker.Plots.Plots.FirstOrDefault(p => p.Contains(tileX, tileY));
			if (plot == null)
			{
				return null;
			}
			if (plot.IsWall(tileX, tileY))
			{
				return null;
			}
			var index = plot.FindCell(tileX, tileY);
			if (index > -1 && index < plot.Cells.Count)
			{
				return plot.Cells[index];
			}
			return null;
		}

		public Plot GetPlotByName(string plotname)
		{
			return Plots.FirstOrDefault(p => p.Name == plotname);
		}

		public void FuckCell(Cell cell)
		{
			_database.Query("UPDATE `cells` SET `GetTime` = @0, `Owner` = @1, `UserIds` = @2, `LastAccess` = @3 WHERE `cells`.`Position` = @4;",
				string.Empty, string.Empty, string.Empty, string.Empty,
				string.Concat(cell.Parent.Id, ':', cell.Id));
			cell.Owner = string.Empty;
			cell.GetTime = default(DateTime);
			cell.LastAccess = default(DateTime);
			cell.AllowedIDs.Clear();
			cell.ClearTiles();
		}

		public int GetTotalCells(string playerName)
		{
			const string query = @"SELECT COUNT(*) FROM `cells`, `plots`
WHERE `plots`.`WorldId` = @0
AND `cells`.`Position` LIKE CONCAT(`plots`.`Id`, ':%')
AND `cells`.`Owner` = @1";
			using (var reader = _database.QueryReader(query, Main.worldID.ToString(), playerName))
			{
				if (reader.Read())
				{
					return reader.Get<int>("COUNT(*)");
				}
			}
			throw new Exception("数据库错误");
		}

		private static int GetCellIndex(Plot plot, QueryResult reader)
		{
			var text = reader.Get<string>("Position");
			if (string.IsNullOrWhiteSpace(text) || text.Length > 5)
			{
				throw new Exception($"属地 {plot.Name} 区域某一位置数据无效.");
			}
			var args = text.Split(':');
#if DEBUG
			int test;
			if (args.Any(a => a == null) || args.Length != 2 || args.Any(a => !int.TryParse(a, out test)))
			{
				Debugger.Break();
				throw new Exception("fuck you, wrong data(string[])");
			}
#endif
			int plotId, cellId;
			if (!int.TryParse(args[0], out plotId) || !int.TryParse(args[1], out cellId))
			{
				throw new Exception("fuck you, wrong data(int.Parse)");
			}
			if (plot.Id != plotId)
			{
				throw new Exception("fuck you, wrong data(plot.Id != plotId)");
			}
			return cellId;
		}

		public Cell GetOnlyCellOfPlayer(string name)
		{
			return GetCellsOfPlayer(name).SingleOrDefault();
		}

		public Cell[] GetCellsOfPlayer(string name)
		{
			return (from plot in Plots
				from cell in plot.Cells
				where cell.Owner.Equals(name, StringComparison.Ordinal)
				select cell).ToArray();
		}
	}
}
