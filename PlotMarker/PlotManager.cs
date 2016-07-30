using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
				new SqlColumn("GetTime", MySqlDbType.Text)
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
						WorldId = Main.worldID.ToString(),
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
			using (var reader = _database.QueryReader("SELECT * FROM `cells` WHERE `cells`.`Position` LIKE @0 ORDER BY `cells`.`Id` ASC", string.Concat(parent.Id, ":%")))
			{
				while (reader.Read())
				{
					DateTime dt;
					list.Add(new Cell
					{
						Parent = parent,
						Id = GetIndex(parent, reader),
						X = reader.Get<int>("X"),
						Y = reader.Get<int>("Y"),
						Owner = reader.Get<string>("Owner"),
						GetTime = DateTime.TryParse(reader.Get<string>("GetTime"), out dt) ? dt : default(DateTime)
					});
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
							WorldId = worldid,
							Owner = owner
						});

						return true;
					}
					return false;
				}
			}
			catch (MySqlException ex)
			{
				if (ex.Number == (int) MySqlErrorCode.DuplicateKeyEntry)
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

		public void AddCells(Plot plot)
		{
			lock (_addCellLock)
			{
				_database.Query("DELETE FROM Cells WHERE Position LIKE @0", plot.Id + ":%");
				var stopwatch = new Stopwatch();
				stopwatch.Start();
				var count = 0;
				foreach (var cell in plot.Cells)
				{
					AddCell(cell);
					count++;
				}
				stopwatch.Stop();
				Console.WriteLine("记录完毕. 共有{0}个. ({1}ms)", count.ToString(), stopwatch.ElapsedMilliseconds.ToString());
			}
		}

		public void AddCell(Cell cell)
		{
			try
			{
				if (_database.Query(
					"INSERT INTO Cells (Position, X, Y) VALUES (@0, @1, @2);",
					string.Concat(cell.Parent.Id, ':', cell.Id), cell.X, cell.Y) == 1)
					return;
				throw new Exception("No affected rows.");
			}
			catch (Exception e)
			{
				TShock.Log.ConsoleError("[PM] Cell数值导入数据库失败. ({0}: {1})", cell.Parent.Name, cell.Id);
				TShock.Log.Error(e.ToString());
			}
		}

		public void ApplyForCell(TSPlayer player, Plot plot)
		{
			try
			{
				if (plot.Cells.All(c => !string.IsNullOrWhiteSpace(c.Owner)))
				{
					player.SendWarningMessage("操作失败. 没有剩余区域了.");
					return;
				}
				Cell cell = null;
				for (var i = plot.Cells.Count - 1; i > 0; i--)
				{
					if (string.IsNullOrWhiteSpace(plot.Cells[i].Owner))
					{
						cell = plot.Cells[i];
					}
				}
				Debug.Assert(cell != null, "cell != null");
				cell.Owner = player.Name;
				cell.GetTime = DateTime.Now;

				//_database.Query("UPDATE")

			}
			catch (Exception e)
			{
				throw;
			}
		}

		public Plot GetPlotByName(string plotname)
		{
			return Plots.FirstOrDefault(p => p.Name == plotname && p.WorldId == Main.worldID.ToString());
		}

		private static int GetIndex(Plot plot, QueryResult reader)
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
	}
}
