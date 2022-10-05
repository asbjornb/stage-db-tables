using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace StageDbTables.Model;

/// <summary>
/// A link is a source and a destination table including connections
/// </summary>
public class Link
{
    private readonly Table sourceTable;
    private readonly Table destinationTable;
    private readonly string sourceConnectionString;
    private readonly string destinationConnectionString;

    public Link(Table sourceTable, Table destinationTable, string sourceConnectionString, string destinationConnectionString)
    {
        //Take SqlConnection, ConnectionString or a new Database class?
        this.sourceTable = sourceTable;
        this.destinationTable = destinationTable;
        this.sourceConnectionString = sourceConnectionString;
        this.destinationConnectionString = destinationConnectionString;
    }

    public Link(string sourceTable, string destinationTable, string sourceConnectionString, string destinationConnectionString)
        : this(new Table(sourceTable), new Table(destinationTable), sourceConnectionString, destinationConnectionString) { }

    public SqlConnection GetSourceConnection()
    {
        var connection = new SqlConnection(sourceConnectionString);
        connection.Open();
        return connection;
    }

    public SqlConnection GetDestinationConnection()
    {
        var connection = new SqlConnection(destinationConnectionString);
        connection.Open();
        return connection;
    }

    public string SourceTableName => sourceTable.GetFullTableName();

    public string DestinationTableName => destinationTable.GetFullTableName();

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
