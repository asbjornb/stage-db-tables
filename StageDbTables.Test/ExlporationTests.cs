using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using NUnit.Framework;
using Shouldly;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace StageDbTables.Test
{
    [TestFixture]
    public class ExplorationTests //Check features of test packages
    {
        private TestcontainerDatabase testDatabase;
        private static readonly MsSqlTestcontainerConfiguration configuration = new()
        {
            //For MsSql-testContainers can't set Database or Username.
            //Database defaults to master, Username to sa
            Password = "StrongPa$$word",
        };

        [OneTimeSetUp]
        public async Task OneTimeSetUpAsync()
        {
            testDatabase = new TestcontainersBuilder<MsSqlTestcontainer>()
                .WithDatabase(configuration)
                .Build();
            await testDatabase.StartAsync();
        }

        [Test]
        public void ShouldConnectToDefaultDockerDatabase()
        {
            using var connection = new SqlConnection(testDatabase.ConnectionString);
            connection.Open();
            using var command = new SqlCommand("SELECT 1", connection);
            var reader = command.ExecuteReader();
            var count = 0;
            while (reader.Read())
            {
                count++;
            }
            count.ShouldBe(1);
        }

        [Test]
        public void ShouldConnectToNewDatabase()
        {
            //Create a new database named StageDbTablesTest
            const string testDatabaseName = "StageDbTablesTest";
            using var defaultConnection = new SqlConnection(testDatabase.ConnectionString);
            defaultConnection.Open();
            using var createDatabaseCommand = new SqlCommand($"CREATE DATABASE {testDatabaseName};", defaultConnection);
            createDatabaseCommand.ExecuteNonQuery();
            testDatabase.Database = testDatabaseName;

            //Connect to new database and check name
            using var connection = new SqlConnection(testDatabase.ConnectionString);
            connection.Open();
            using var queryCommand = new SqlCommand("SELECT DB_NAME()", connection);
            var reader = queryCommand.ExecuteReader();
            var count = 0;
            while (reader.Read())
            {
                reader.GetString(0).ShouldBe(testDatabaseName);
                count++;
            }
            count.ShouldBe(1);
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await testDatabase.DisposeAsync();
        }
    }
}