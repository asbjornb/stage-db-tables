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
            using var command = new SqlCommand();
            connection.Open();
            command.Connection = connection;
            command.CommandText = "SELECT 1";
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
            using var defaultConnection = new SqlConnection(testDatabase.ConnectionString);
            using var createDatabaseCommand = new SqlCommand();
            defaultConnection.Open();
            createDatabaseCommand.Connection = defaultConnection;
            createDatabaseCommand.CommandText = "CREATE DATABASE [StageDbTablesTest]";
            createDatabaseCommand.ExecuteNonQuery();
            testDatabase.Database = "StageDbTablesTest";
            
            //Connect to new database and check name
            using var connection = new SqlConnection(testDatabase.ConnectionString);
            using var queryCommand = new SqlCommand();
            connection.Open();
            queryCommand.Connection = connection;
            queryCommand.CommandText = "SELECT DB_NAME()";
            var reader = queryCommand.ExecuteReader();
            var count = 0;
            while (reader.Read())
            {
                reader.GetString(0).ShouldBe("StageDbTablesTest");
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