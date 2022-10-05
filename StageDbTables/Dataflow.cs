using StageDbTables.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace StageDbTables;

/// <summary>
/// Dataflows are pipelines moving data from source to destination with ExecuteAsync
/// </summary>
public class Dataflow
{
    private readonly Table sourceTable;
    private readonly Table destinationTable;
    private readonly string sourceConnectionString;
    private readonly string destinationConnectionString;

    public List<ColumnMapping> Map { get; set; }

    public Dataflow(string sourceTable, string destinationTable, string sourceConnectionString, string destinationConnectionString)
        : this(sourceTable, destinationTable, sourceConnectionString, destinationConnectionString, new()){}

    public Dataflow(string sourceTable, string destinationTable, string sourceConnectionString, string destinationConnectionString, List<ColumnMapping> columnMap)
    {
        //Take SqlConnection, ConnectionString or a new Database class?
        this.sourceTable = new Table(sourceTable);
        this.destinationTable = new Table(destinationTable);
        this.sourceConnectionString = sourceConnectionString;
        this.destinationConnectionString = destinationConnectionString;
        Map = columnMap;
    }

    /// <summary>
    /// Flows the data from source to destination
    /// </summary>
    public async Task<Result> ExecuteAsync()
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

            if (Map.Count > 0)
            {
                foreach (var column in Map)
                {
                    bulkCopy.ColumnMappings.Add(column.SourceColumnName, column.DestinationColumnName);
                }
            }

            bulkCopy.DestinationTableName = destinationTable.GetFullTableName();

            await bulkCopy.WriteToServerAsync(reader);
        }
        catch (Exception e)
        {
            return Result.Failure(e.Message);
            throw;
        }

        return Result.Success();
    }

    /// <summary>
    /// Finds column names from source and destination that match on data type and name and returns a map of those
    /// </summary>
    public List<ColumnMapping> GetMatchingColumns()
    {
        var sourceSchema = GetSchema(sourceConnectionString, sourceTable);
        var destinationSchema = GetSchema(destinationConnectionString, destinationTable);

        //Note casing in column names matter with this
        return sourceSchema.Intersect(destinationSchema).Select(x => new ColumnMapping(x.ColumnName, x.ColumnName)).ToList();
    }

    private static IEnumerable<Column> GetSchema(string connectionString, Table table)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        string[] restrictions = new string[4];
        restrictions[0] = connection.Database;
        restrictions[1] = table.SchemaName;
        restrictions[2] = table.TableName;

        var dtCols = connection.GetSchema("Columns", restrictions);

        if (dtCols.Rows.Count == 0)
        {
            throw new InvalidOperationException($"{table.GetFullTableName()} does not exist on given connection.");
        }

        foreach (DataRow row in dtCols.Rows)
        {
            var dataType = row["DATA_TYPE"].ToString();
            var columnName = row["COLUMN_NAME"].ToString();
            yield return new Column(dataType!, columnName!);
        }
    }

    private record Column(string DataType, string ColumnName);
}
