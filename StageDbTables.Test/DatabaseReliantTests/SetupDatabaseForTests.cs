using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using NUnit.Framework;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace StageDbTables.Test.DatabaseReliantTests;

[SetUpFixture]
public class SetupDatabaseForTests
{
    private static TestcontainerDatabase testDatabase;
    private const string testDatabaseName = "StageDbTablesTest";
    private static readonly MsSqlTestcontainerConfiguration configuration = new()
    {
        //For MsSql-testContainers can't set Database or Username.
        //Database defaults to master, Username to sa
        Password = "StrongPa$$word",
    };

    public static string ConnectionString => testDatabase.ConnectionString;
    public static SqlConnection TestDatabaseConnection => new(testDatabase.ConnectionString);

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        //Create container and master database
        testDatabase = new TestcontainersBuilder<MsSqlTestcontainer>()
            .WithDatabase(configuration)
            .Build();
        await testDatabase.StartAsync();
        //Create a testDatabase
        using var defaultConnection = new SqlConnection(testDatabase.ConnectionString);
        defaultConnection.Open();
        using var createDatabaseCommand = new SqlCommand($"CREATE DATABASE {testDatabaseName};", defaultConnection);
        await createDatabaseCommand.ExecuteNonQueryAsync();
        testDatabase.Database = testDatabaseName; //Use new test database rather than master
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await testDatabase.DisposeAsync();
    }
}
