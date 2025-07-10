using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Data.Sqlite;
using Dapper;
using YALA.Models;
using System.Collections.ObjectModel;

namespace YALA.Services;

public class DatabaseService
{
	IDbConnection? connection;
	//LabelingDatabase LabelingDatabase;
	//public DatabaseService(LabelingDatabase labelingDatabase) 
	//{ 
	//    LabelingDatabase = labelingDatabase;
	//}

	public void Initialize(string dbPath)
	{
		connection = new SqliteConnection($"Data Source={dbPath}");
		connection.Open();

		connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Images (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Path TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Classes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                Color TEXT
            );

            CREATE TABLE IF NOT EXISTS Annotations (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ImageId INTEGER NOT NULL,
                ClassId INTEGER NOT NULL,
                X REAL NOT NULL,
                Y REAL NOT NULL,
                Width REAL NOT NULL,
                Height REAL NOT NULL,
                FOREIGN KEY (ImageId) REFERENCES Images(Id),
                FOREIGN KEY (ClassId) REFERENCES Classes(Id)
            );
        ");
	}

	public bool TablesExist(string dbPath)
	{
		using IDbConnection connection = new SqliteConnection($"Data Source={dbPath}");
		connection.Open();

		var result = connection.Query<string>(@"
		SELECT name FROM sqlite_master 
		WHERE type = 'table' AND name IN ('Images', 'Classes', 'Annotations');
	").ToList();

		return result.Count == 3;
	}

	public void Open(string dbPath)
	{
		connection = new SqliteConnection($"Data Source={dbPath}");
		connection.Open();
	}

	public void AddClasses(List<string> classes)
	{
		var sql = "INSERT INTO Classes (Name) VALUES (@Name);";

		using var transaction = connection?.BeginTransaction();
		foreach (var className in classes)
		{
			connection?.Execute(sql, new { Name = className }, transaction);
		}
		transaction?.Commit();
	}

	public void SetClassColor(string className, string colorHex)
	{
		var sql = "UPDATE Classes SET Color = @colorHex WHERE Name = @className;";

		using var transaction = connection?.BeginTransaction();
		connection?.Execute(sql, new { colorHex = colorHex, className = className }, transaction);
		transaction?.Commit();
	}

	public string GetClassColor(string className)
	{
		var colorHex = connection?.QuerySingleOrDefault<string>(
			"SELECT Color FROM Classes WHERE Name = @className",
			new { className });

		return colorHex ?? string.Empty;
	}


	public ObservableCollection<LabelingClass> GetLabellingClasses()
	{
		var labellingClasses = connection?.Query<LabelingClass>("SELECT Id, Name, Color FROM Classes").ToList();
		if (labellingClasses != null)
		{
			return new ObservableCollection<LabelingClass>(labellingClasses);
		}
		return new ObservableCollection<LabelingClass>();
	}


	public void Close()
	{
		connection?.Close();
	}
}
