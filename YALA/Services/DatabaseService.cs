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
	public IDbConnection? connection;
	public string absolutePath = "";

	public void Initialize(string dbPath)
	{
		connection = new SqliteConnection($"Data Source={dbPath}");
		connection.Open();
		absolutePath = dbPath;

		connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Images (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Path TEXT NOT NULL UNIQUE
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
		absolutePath = dbPath;
	}

	public void Close()
	{
		connection?.Close();
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
			labellingClasses.First().IsSelected = true;
			return new ObservableCollection<LabelingClass>(labellingClasses);
		}
		return new ObservableCollection<LabelingClass>();
	}

	public void AddImages(List<string> imagesPaths)
	{
		var sql = "INSERT INTO Images (Path) VALUES (@Path);";

		using var transaction = connection?.BeginTransaction();
		foreach (var imagePath in imagesPaths)
		{
			connection?.Execute(sql, new { Path = imagePath }, transaction);
		}
		transaction?.Commit();
	}
	public void RemoveImage(string path)
	{
		const string deleteAnnotations = @"
		DELETE FROM Annotations
		WHERE ImageId = (SELECT Id FROM Images WHERE Path = @Path);";

		const string deleteImage = "DELETE FROM Images WHERE Path = @Path;";

		using var transaction = connection?.BeginTransaction();
		connection?.Execute(deleteAnnotations, new { Path = path }, transaction);
		connection?.Execute(deleteImage, new { Path = path }, transaction);
		transaction?.Commit();
	}


	public ObservableCollection<string> GetImagesPaths()
	{
		var imagesPaths = connection?.Query<string>("SELECT Path FROM Images").ToList();
		if (imagesPaths != null)
		{
			return new ObservableCollection<string>(imagesPaths);
		}
		return new ObservableCollection<string>();
	}

	public void AddBoundingBox(BoundingBox boundingBox, string imagePath)
	{
		const string getImageIdSql = "SELECT Id FROM Images WHERE Path = @Path;";
		const string insertAnnotationSql = @"
		INSERT INTO Annotations (ImageId, ClassId, X, Y, Width, Height)
		VALUES (@ImageId, @ClassId, @X, @Y, @Width, @Height);";

		using var transaction = connection?.BeginTransaction();

		var imageId = connection?.ExecuteScalar<int?>(getImageIdSql, new { Path = imagePath }, transaction);
		if (imageId is null)
			throw new InvalidOperationException($"Image path '{imagePath}' not found in database.");

		connection?.Execute(insertAnnotationSql, new
		{
			ImageId = imageId,
			ClassId = boundingBox.ClassId,
			X = boundingBox.Tlx,
			Y = boundingBox.Tly,
			Width = boundingBox.Width,
			Height = boundingBox.Height
		}, transaction);

		transaction?.Commit();
	}

	public void RemoveBoundingBox(BoundingBox boundingBox, string imagePath)
	{
		var sql = @"
		DELETE FROM Annotations
		WHERE ImageId = (SELECT Id FROM Images WHERE Path = @Path)
		  AND ClassId = @ClassId AND X = @X AND Y = @Y AND Width = @Width AND Height = @Height;";
		using var transaction = connection?.BeginTransaction();
		connection?.Execute(sql, new
		{
			Path = imagePath,
			ClassId = boundingBox.ClassId,
			X = boundingBox.Tlx,
			Y = boundingBox.Tly,
			Width = boundingBox.Width,
			Height = boundingBox.Height,
		}, transaction);
		transaction?.Commit();
	}

	public void RemoveAllImagesBoundingBoxes(string imagePath)
	{
		var sql = @"
		DELETE FROM Annotations
		WHERE ImageId = (SELECT Id FROM Images WHERE Path = @Path);";
		using var transaction = connection?.BeginTransaction();
		connection?.Execute(sql, new { Path = imagePath }, transaction);
		transaction?.Commit();
	}

	public ObservableCollection<BoundingBox> GetBoundingBoxes(string imagePath, bool editingEnabled)
	{
		var boundingBoxes = connection?.Query<BoundingBox>(@"
		SELECT 
		A.ClassId,
		A.X Tlx,
		A.Y Tly,
		A.Width,
		A.Height,
		(SELECT Name FROM Classes C WHERE C.Id = A.ClassId) AS ClassName,
		(SELECT Color FROM Classes C WHERE C.Id = A.ClassId) AS Color
		FROM Annotations A
		WHERE A.ImageId = (SELECT Id FROM Images WHERE Path = @Path);",
			new { Path = imagePath }).ToList();

		if (boundingBoxes != null)
		{
			foreach (var box in boundingBoxes)
				box.EditingEnabled = editingEnabled;
			return new ObservableCollection<BoundingBox>(boundingBoxes);
		}
		return new ObservableCollection<BoundingBox>();
	}


}
