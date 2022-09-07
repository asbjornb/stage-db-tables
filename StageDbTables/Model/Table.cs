namespace StageDbTables.Model;

/// <summary>
/// Tables are the sources and destinations for data operations
/// </summary>
internal class Table
{
    /// <summary>
    /// Name of the table in the database. Omit brackets. Schema is separate.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Schema name for the table. Omit brackets.
    /// </summary>
    public string SchemaName { get; }

    public Table(string schemaName, string tableName)
    {
        TableName = tableName;
        SchemaName = schemaName;
    }

    /// <summary>
    /// Constructor that takes a single string and splits it into schema and table name. Eg. "dbo.TableName" becomes "dbo" and "TableName"
    /// </summary>
    /// <param name="tableName">2-part table name without brackets and a single . separation </param>
    public Table(string tableName)
    {
        SchemaName = tableName.Split('.')[0];
        TableName = tableName.Split('.')[1];
    }

    internal string GetFullTableName()
    {
        return $"[{SchemaName}].[{TableName}]";
    }
}
