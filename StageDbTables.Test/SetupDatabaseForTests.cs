using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using NUnit.Framework;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace StageDbTables.Test;

#nullable disable

[SetUpFixture]
public class SetupDatabaseForTests
{
    private static TestcontainerDatabase masterDatabase;
    private const string testDatabaseName = "StageDbTablesTest";
    private static readonly MsSqlTestcontainerConfiguration configuration = new()
    {
        //For MsSql-testContainers can't set Database or Username.
        //Database defaults to master, Username to sa
        Password = "StrongPa$$word",
    };

    public static string ConnectionString => masterDatabase.ConnectionString;
    public static SqlConnection TestDatabaseConnection => new(masterDatabase.ConnectionString);

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        //Create container and master database
        Console.WriteLine("OneTimeSetUp started");
        masterDatabase = new TestcontainersBuilder<MsSqlTestcontainer>()
            .WithDatabase(configuration)
            .Build();
        await masterDatabase.StartAsync();
        Console.WriteLine("Master started");
        //Create a testDatabase
        using var defaultConnection = new SqlConnection(masterDatabase.ConnectionString);
        using var createDatabaseCommand = new SqlCommand();
        defaultConnection.Open();
        createDatabaseCommand.Connection = defaultConnection;
        createDatabaseCommand.CommandText = $"CREATE DATABASE {testDatabaseName};";
        await createDatabaseCommand.ExecuteNonQueryAsync();
        Console.WriteLine(masterDatabase.ConnectionString);
        masterDatabase.Database = testDatabaseName;
        Console.WriteLine(masterDatabase.ConnectionString);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await masterDatabase.DisposeAsync();
    }
}
