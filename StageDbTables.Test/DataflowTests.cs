using NUnit.Framework;
using Shouldly;
using System.Data;
using System.Data.SqlClient;

namespace StageDbTables.Test;

[TestFixture]
public class DataflowTests
{
    [Test]
    public void ShouldCopyData()
    {
        //Use testconnection
        using var testConnection = SetupDatabaseForTests.TestDatabaseConnection;
        testConnection.Open();

        //Create source and dest tables
        using var createSourceTableCommand = new SqlCommand();
        createSourceTableCommand.Connection = testConnection;
        createSourceTableCommand.CommandText = "CREATE TABLE dbo.TestTable(Id int IDENTITY(0,1) NOT NULL PRIMARY KEY," +
            " TestString varchar(256) NOT NULL);";
        createSourceTableCommand.ExecuteNonQuery();
        using var createDestTableCommand = new SqlCommand();
        createDestTableCommand.Connection = testConnection;
        createDestTableCommand.CommandText = "CREATE TABLE dbo.DestTable(Id int NOT NULL PRIMARY KEY," +
            " TestString varchar(256) NOT NULL);";
        createDestTableCommand.ExecuteNonQuery();

        //Insert a bit of test data
        using var insertDataCommand = new SqlCommand();
        insertDataCommand.Connection = testConnection;
        insertDataCommand.CommandText = "INSERT INTO TestTable(TestString) VALUES (@0);";
        insertDataCommand.Parameters.Add("@0", SqlDbType.VarChar);
        for (int i = 0; i < 10; i++)
        {
            insertDataCommand.Parameters["@0"].Value = $"Text {i}";
            insertDataCommand.ExecuteNonQuery();
        }

        //Try moving with a dataflow
        var dataflow = new Dataflow("dbo.TestTable", "dbo.DestTable", SetupDatabaseForTests.ConnectionString
            , SetupDatabaseForTests.ConnectionString);
        dataflow.Execute();

        //Assert
        using var queryCommand = new SqlCommand();
        queryCommand.Connection = testConnection;
        queryCommand.CommandText = "SELECT * FROM dbo.DestTable";
        var reader = queryCommand.ExecuteReader();
        var count = 0;
        while (reader.Read())
        {
            reader.GetString(1).ShouldContain("Text ");
            reader.GetString(1).ShouldContain(reader.GetInt32(0).ToString());
            count++;
        }
        count.ShouldBe(10);
    }
}
