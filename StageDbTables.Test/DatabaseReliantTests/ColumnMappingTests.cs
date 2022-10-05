using NUnit.Framework;
using Shouldly;
using StageDbTables.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace StageDbTables.Test.DatabaseReliantTests;

[TestFixture]
public class ColumnMappingTests
{
    private const string sourceTableName = "dbo.TestTable";
    private const string destinationTableName = "dbo.DestTable";

    [SetUp]
    public void SetUp()
    {
        //Use testconnection
        using var testConnection = SetupDatabaseForTests.TestDatabaseConnection;
        testConnection.Open();

        //Create source and dest tables
        const string createSourceTableStatement = $"CREATE TABLE {sourceTableName}(Id int IDENTITY(0,1) NOT NULL PRIMARY KEY," +
            " TestString varchar(256) NOT NULL, TestDate date NULL);";
        const string createDestinationTableStatement = $"CREATE TABLE {destinationTableName}(Id int NOT NULL PRIMARY KEY," +
            " TestString varchar(256) NOT NULL, OtherDate date NULL, ThirdDate date NULL);";

        using var createSourceTableCommand = new SqlCommand(createSourceTableStatement, testConnection);
        createSourceTableCommand.ExecuteNonQuery();
        using var createDestTableCommand = new SqlCommand(createDestinationTableStatement, testConnection);
        createDestTableCommand.ExecuteNonQuery();
    }

    [TearDown]
    public void TearDown()
    {
        //Use testconnection
        using var testConnection = SetupDatabaseForTests.TestDatabaseConnection;
        testConnection.Open();

        //Drop source and dest tables
        const string dropSourceTableStatement = $"DROP TABLE {sourceTableName};";
        const string dropDestinationTableStatement = $"DROP TABLE {destinationTableName};";

        using var createSourceTableCommand = new SqlCommand(dropSourceTableStatement, testConnection);
        createSourceTableCommand.ExecuteNonQuery();
        using var createDestTableCommand = new SqlCommand(dropDestinationTableStatement, testConnection);
        createDestTableCommand.ExecuteNonQuery();
    }

    [Test]
    public async Task ShouldMapColumns()
    {
        //Use testconnection
        using var testConnection = SetupDatabaseForTests.TestDatabaseConnection;
        testConnection.Open();

        //Insert a bit of test data
        using var insertDataCommand = new SqlCommand($"INSERT INTO {sourceTableName}(TestString, TestDate) VALUES (@0, @1);", testConnection);
        insertDataCommand.Parameters.Add("@0", SqlDbType.VarChar);
        insertDataCommand.Parameters.Add("@1", SqlDbType.Date);
        for (int i = 0; i < 10; i++)
        {
            insertDataCommand.Parameters["@0"].Value = $"Text {i}";
            insertDataCommand.Parameters["@1"].Value = new DateTime(2020, 1, 1 + i);
            insertDataCommand.ExecuteNonQuery();
        }

        //Try moving with a dataflow
        var columnMap = new List<ColumnMapping>() { new("Id", "Id"), new("TestString", "TestString") ,new("TestDate", "ThirdDate") };
        var dataflow = new Dataflow(sourceTableName, destinationTableName, SetupDatabaseForTests.ConnectionString
            , SetupDatabaseForTests.ConnectionString, columnMap);
        await dataflow.ExecuteAsync();

        //Assert
        using var queryCommand = new SqlCommand($"SELECT * FROM {destinationTableName};", testConnection);
        var reader = queryCommand.ExecuteReader();
        var count = 0;
        while (reader.Read())
        {
            reader.GetString(1).ShouldBe($"Text {reader.GetInt32(0)}");
            reader.IsDBNull(2).ShouldBeTrue();
            reader.GetDateTime(3).ShouldBe(new DateTime(2020, 1, 1 + reader.GetInt32(0)));
            count++;
        }
        count.ShouldBe(10);
    }

    [Test]
    public void ShouldGenerateMapForColumnsInBothTables()
    {
        //Setup dataflow to generate map
        var dataflow = new Dataflow(sourceTableName, destinationTableName, SetupDatabaseForTests.ConnectionString
            , SetupDatabaseForTests.ConnectionString);

        //Retieve a map
        var columnMap = dataflow.GetMatchingColumns();

        //Assert
        columnMap.Count.ShouldBe(2);
        columnMap.ShouldContain(new ColumnMapping("Id", "Id"));
        columnMap.ShouldContain(new ColumnMapping("TestString", "TestString"));
    }

    [Test]
    public async Task ShouldMapSimplyWithDefaultsAndAdded()
    {
        //Use testconnection
        using var testConnection = SetupDatabaseForTests.TestDatabaseConnection;
        testConnection.Open();

        //Insert a bit of test data
        using var insertDataCommand = new SqlCommand($"INSERT INTO {sourceTableName}(TestString, TestDate) VALUES (@0, @1);", testConnection);
        insertDataCommand.Parameters.Add("@0", SqlDbType.VarChar);
        insertDataCommand.Parameters.Add("@1", SqlDbType.Date);
        for (int i = 0; i < 10; i++)
        {
            insertDataCommand.Parameters["@0"].Value = $"Text {i}";
            insertDataCommand.Parameters["@1"].Value = new DateTime(2020, 1, 1 + i);
            insertDataCommand.ExecuteNonQuery();
        }

        //Try moving with a dataflow
        var dataflow = new Dataflow(sourceTableName, destinationTableName, SetupDatabaseForTests.ConnectionString
            , SetupDatabaseForTests.ConnectionString);
        var map = dataflow.GetMatchingColumns();
        map.Add(new("TestDate", "ThirdDate"));
        dataflow.Map = map;
        await dataflow.ExecuteAsync();

        //Assert
        using var queryCommand = new SqlCommand($"SELECT * FROM {destinationTableName};", testConnection);
        var reader = queryCommand.ExecuteReader();
        var count = 0;
        while (reader.Read())
        {
            reader.GetString(1).ShouldBe($"Text {reader.GetInt32(0)}");
            reader.IsDBNull(2).ShouldBeTrue();
            reader.GetDateTime(3).ShouldBe(new DateTime(2020, 1, 1 + reader.GetInt32(0)));
            count++;
        }
        count.ShouldBe(10);
    }
}
