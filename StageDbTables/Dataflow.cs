using StageDbTables.Model;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace StageDbTables;

/// <summary>
/// Dataflows are pipelines moving data from source to destination with ExecuteAsync
/// </summary>
public class Dataflow
{
    private readonly Link link;

    public List<ColumnMapping> Map { get; set; }

    public Dataflow(string sourceTable, string destinationTable, string sourceConnectionString, string destinationConnectionString)
        : this(sourceTable, destinationTable, sourceConnectionString, destinationConnectionString, new()){}

    public Dataflow(string sourceTable, string destinationTable, string sourceConnectionString, string destinationConnectionString, List<ColumnMapping> columnMap)
        : this(new Link(sourceTable, destinationTable, sourceConnectionString, destinationConnectionString), columnMap){}

    public Dataflow(Link link, List<ColumnMapping> columnMap)
    {
        this.link = link;
        Map = columnMap;
    }

    public Dataflow(Link link) : this(link, new()){}

    /// <summary>
    /// Flows the data from source to destination
    /// </summary>
    public async Task<Result> ExecuteAsync()
    {
        try
        {
            //Setup bulkcopy with destination
            using var destinationConnection = link.GetDestinationConnection();
            using var bulkCopy = new SqlBulkCopy(destinationConnection);

            if (Map.Count > 0)
            {
                foreach (var column in Map)
                {
                    bulkCopy.ColumnMappings.Add(column.SourceColumnName, column.DestinationColumnName);
                }
            }

            bulkCopy.DestinationTableName = link.DestinationTableName;

            //Read from source
            using var sourceConnection = link.GetSourceConnection();
            var commandSourceData = new SqlCommand($"SELECT * FROM {link.SourceTableName};", sourceConnection);
            using var reader = commandSourceData.ExecuteReader();

            //Write to destination
            await bulkCopy.WriteToServerAsync(reader);
        }
        catch (Exception e)
        {
            return Result.Failure(e.Message);
            throw;
        }

        return Result.Success();
    }
}
