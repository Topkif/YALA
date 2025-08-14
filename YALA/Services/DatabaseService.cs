using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using YALA.Models;

namespace YALA.Services;

public class DatabaseService
{
	public IDbConnection? connection;
	public string absolutePath = "";

	public void CreateYalaTables(string dbPath)
	{
		Open(dbPath);
		connection!.Execute(@"
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
                Tlx REAL NOT NULL,
                Tly REAL NOT NULL,
                Width REAL NOT NULL,
                Height REAL NOT NULL,
                FOREIGN KEY (ImageId) REFERENCES Images(Id),
                FOREIGN KEY (ClassId) REFERENCES Classes(Id)
            );
        ");
	}

	public bool DoTablesExist(string dbPath)
	{
		Open(dbPath);
		var result = connection!.Query<string>(@"
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
		connection?.Dispose();
	}

	public void UpdateNumberOfInstances(ObservableCollection<LabellingClass> labellingClasses)
	{
		var counts = connection?.Query<(int ClassId, int Count)>(
			@"SELECT ClassId, COUNT(*) AS Count FROM Annotations GROUP BY ClassId;")
			.ToDictionary(x => x.ClassId, x => x.Count);

		if (counts != null)
		{
			foreach (var cls in labellingClasses)
			{
				if (counts.TryGetValue(cls.Id, out var count))
					cls.NumberOfInstances = count;
				else
					cls.NumberOfInstances = 0;
			}
		}
	}
	public int GetInstancesOfClass(string className)
	{
		var sql = @"
		SELECT COUNT(*) FROM Annotations
		WHERE ClassId = (SELECT Id FROM Classes WHERE Name = @ClassName);";
		return connection?.ExecuteScalar<int>(sql, new { ClassName = className }) ?? 0;
	}

	public void AddClasses(List<string> classes)
	{
		var sql = "INSERT OR IGNORE INTO Classes (Name) VALUES (@Name);";

		using var transaction = connection?.BeginTransaction();
		foreach (var className in classes)
		{
			connection?.Execute(sql, new { Name = className }, transaction);
		}
		transaction?.Commit();
	}

	public void AddClassesAndColor(List<(string, string)> classes)
	{
		var sql = "INSERT OR IGNORE INTO Classes (Name, Color) VALUES (@Name, @Color);";

		using var transaction = connection?.BeginTransaction();
		foreach (var className in classes)
		{
			connection?.Execute(sql, new { Name = className.Item1, Color = className.Item2 }, transaction);
		}
		transaction?.Commit();
	}
	public void DeleteClass(LabellingClass labelingClass)
	{
		const string deleteAnnotations = @"
		DELETE FROM Annotations
		WHERE ClassId = (SELECT Id FROM Classes WHERE Name = @Name);";

		const string deleteImage = "DELETE FROM Classes WHERE Name = @Name;";

		using var transaction = connection?.BeginTransaction();
		connection?.Execute(deleteAnnotations, new { Name = labelingClass.Name }, transaction);
		connection?.Execute(deleteImage, new { Name = labelingClass.Name }, transaction);
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
	public ObservableCollection<LabellingClass> GetLabellingClasses()
	{
		var labellingClasses = connection?.Query<LabellingClass>(@"SELECT 
		Id, 
		Name, 
		Color, 
		(SELECT COUNT(*) FROM Annotations WHERE ClassId = Classes.Id) AS NumberOfInstances
		FROM 
		Classes;").ToList();
		if (labellingClasses != null && labellingClasses.Count > 0)
		{
			labellingClasses.First().IsSelected = true;
			return new ObservableCollection<LabellingClass>(labellingClasses);
		}
		return new ObservableCollection<LabellingClass>();
	}

	public void AddImages(List<string> imagesPaths)
	{
		var sql = "INSERT OR IGNORE INTO Images (Path) VALUES (@Path);";

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
		const string insertAnnotationSql = @"
		INSERT INTO Annotations (ImageId, ClassId, Tlx, Tly, Width, Height)
		VALUES (
			(SELECT Id FROM Images WHERE Path = @Path),
			@ClassId, @Tlx, @Tly, @Width, @Height);
		SELECT last_insert_rowid();";

		using var transaction = connection?.BeginTransaction();

		boundingBox.Id = connection!.ExecuteScalar<int>(insertAnnotationSql, new
		{
			Path = imagePath,
			ClassId = boundingBox.ClassId,
			Tlx = boundingBox.Tlx,
			Tly = boundingBox.Tly,
			Width = boundingBox.Width,
			Height = boundingBox.Height
		}, transaction);

		transaction?.Commit();
	}

	public void AddBoundingBoxListSafe(List<BoundingBox> boundingBoxList, string imagePath)
	{
		const string insertAnnotationSql = @"
		INSERT INTO Annotations (ImageId, ClassId, Tlx, Tly, Width, Height)
		VALUES (@ImageId, @ClassId, @Tlx, @Tly, @Width, @Height);";

		using var transaction = connection?.BeginTransaction();

		// Get ImageId once
		var imageId = connection?.ExecuteScalar<int?>(
			"SELECT Id FROM Images WHERE Path = @Path;",
			new { Path = imagePath }, transaction);

		if (imageId is null)
		{
			transaction?.Rollback(); // Calling rollback explicitly is cleaner to avoid leaving a transaction open
			return;
		}

		foreach (var boundingBox in boundingBoxList)
		{
			// Get ClassId for each bounding box
			var classId = connection?.ExecuteScalar<int?>(
				"SELECT Id FROM Classes WHERE Name = @ClassName;",
				new { ClassName = boundingBox.ClassName }, transaction);

			if (classId is null)
				continue; // Skip if class not found

			connection!.Execute(insertAnnotationSql, new
			{
				ImageId = imageId.Value,
				ClassId = classId.Value,
				Tlx = boundingBox.Tlx,
				Tly = boundingBox.Tly,
				Width = boundingBox.Width,
				Height = boundingBox.Height
			}, transaction);
		}

		transaction?.Commit();
	}


	public void UpdateBoundingBox(BoundingBox newBoundingBox)
	{
		const string sql = @"
		UPDATE Annotations
		SET ClassId = @ClassId, Tlx = @Tlx, Tly = @Tly, Width = @Width, Height = @Height
		WHERE Id = @Id;";

		using var transaction = connection?.BeginTransaction();
		connection?.Execute(sql, new
		{
			Id = newBoundingBox.Id,
			ClassId = newBoundingBox.ClassId,
			Tlx = newBoundingBox.Tlx,
			Tly = newBoundingBox.Tly,
			Width = newBoundingBox.Width,
			Height = newBoundingBox.Height
		}, transaction);
		transaction?.Commit();
	}

	public void RemoveBoundingBox(BoundingBox boundingBox)
	{
		var sql = @"DELETE FROM Annotations	WHERE Id = @Id;";
		using var transaction = connection?.BeginTransaction();
		connection?.Execute(sql, new
		{
			Id = boundingBox.Id,
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
	public void RemoveImageAnnotationsForClasses(string imagePath, List<string> classNamesToRemoveForCurrentImage)
	{
		const string sql = @"
		DELETE FROM Annotations
		WHERE ImageId = (SELECT Id FROM Images WHERE Path = @Path)
		  AND ClassId IN (SELECT Id FROM Classes WHERE Name IN @ClassNames);";

		using var transaction = connection?.BeginTransaction();

		connection?.Execute(sql, new
		{
			Path = imagePath,
			ClassNames = classNamesToRemoveForCurrentImage
		}, transaction);

		transaction?.Commit();
	}

	public ObservableCollection<BoundingBox> GetBoundingBoxes(string imagePath, bool editingEnabled = false)
	{
		var boundingBoxes = connection?.Query<BoundingBox>(@"
		SELECT
		A.Id,
		A.ClassId,
		A.Tlx,
		A.Tly,
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

	public void RemoveAllImagesBoundingBoxesFromProject()
	{
		const string sql = @"
		DELETE FROM Annotations;";

		using var transaction = connection?.BeginTransaction();

		connection?.Execute(sql, transaction);
		transaction?.Commit();
	}
}
