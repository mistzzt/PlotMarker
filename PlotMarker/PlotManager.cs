using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
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
				new SqlColumn("WorldId", MySqlDbType.VarChar, 50) { Unique = true },
				new SqlColumn("Owner", MySqlDbType.VarChar, 50)
			);

			var cellTable = new SqlTable("Cells",
				new SqlColumn("PlotId", MySqlDbType.Int32) { NotNull = true },
				new SqlColumn("CellId", MySqlDbType.Int32) { NotNull = true },
				new SqlColumn("X", MySqlDbType.Int32) { Unique = true },
				new SqlColumn("Y", MySqlDbType.Int32) { Unique = true },
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
			using (var reader = _database.QueryReader("SELECT * FROM `cells` WHERE PlotId = @0 ORDER BY `cells`.`CellId` ASC", parent.Id))
			{
				while (reader.Read())
				{
					list.Add(new Cell
					{
						Parent = parent,
						Id = reader.Get<int>("CellId"),
						X = reader.Get<int>("X"),
						Y = reader.Get<int>("Y"),
						Owner = reader.Get<string>("Owner"),
						GetTime = reader.Get<DateTime?>("GetTime") ?? default(DateTime)
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

		public Plot GetPlotByName(string plotname)
		{
			return Plots.FirstOrDefault(p => p.Name == plotname && p.WorldId == Main.worldID.ToString());
		}

		public void AddCells(Plot plot)
		{
			_database.Query("DELETE FROM Cells WHERE PlotId = @0", plot.Id);
			Task.Run(() =>
			{
				var stopwatch = new Stopwatch();
				stopwatch.Start();
				var count = 0;
				foreach (var cell in plot.Cells)
				{
					AddCell(cell);
					count++;
				}
				stopwatch.Stop();
				Console.WriteLine("记录完毕. 共有{0}个. ({1}ms)", count.ToString(), stopwatch.ElapsedMilliseconds);
			});
		}

		public void AddCell(Cell cell)
		{
			try
			{
				if (_database.Query(
					"INSERT INTO Cells (PlotId, CellId, X, Y) VALUES (@0, @1, @2, @3);",
					cell.Parent.Id, cell.Id, cell.X, cell.Y) == 1)
					return;
				throw new Exception("No affected rows.");
			}
			catch (Exception e)
			{
				throw;
			}
		}
	}
}
