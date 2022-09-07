using StageDbTables.Model;
using System;
using System.Data.SqlClient;

namespace StageDbTables;

/// <summary>
/// Dataflows are pipelines moving data from source to destination with Execute or ExecuteAsync
/// </summary>
public class Dataflow
{
    private readonly Table sourceTable;
    private readonly Table destinationTable;
    private readonly string sourceConnectionString;
    private readonly string destinationConnectionString;

    public Dataflow(string sourceTable, string destinationTable, string sourceConnectionString, string destinationConnectionString)
    {
        //Take SqlConnection, ConnectionString or a new Database class?
        this.sourceTable = new Table(sourceTable);
        this.destinationTable = new Table(destinationTable);
        this.sourceConnectionString = sourceConnectionString;
        this.destinationConnectionString = destinationConnectionString;
    }

    public Result Execute()
    {
        using var sourceConnection = new SqlConnection(sourceConnectionString);
        sourceConnection.Open();
        try
        {
            var commandSourceData = new SqlCommand($"SELECT * FROM {sourceTable.GetFullTableName()};", sourceConnection);
            using var reader = commandSourceData.ExecuteReader();

            using var destinationConnection = new SqlConnection(destinationConnectionString);
            destinationConnection.Open();

            using var bulkCopy = new SqlBulkCopy(destinationConnection);
            bulkCopy.DestinationTableName = destinationTable.GetFullTableName();

            bulkCopy.WriteToServer(reader);
        }
        catch (Exception e)
        {
            return Result.Failure(e.Message);
            throw;
        }

        return Result.Success();
    }
}
