# stage-db-tables

A library to ease copying/synchronizing data across tables, databases or servers with .Net

## Usage

To copy data from one table to another with a similar schema in the same database:

```c#
var sourceTable = "dbo.Test"
var destTable = "dbo.TestCopy"
var sourceConnectionString = "Data Source=(local); " +
            " Integrated Security=true;" +
            "Initial Catalog=TestDb;";
var destConnectionString = sourceConnectionString;
var flow = new Dataflow(sourceTable, sourceConnectionString, destTable, destConnectionString);
var result = await flow.ExecuteAsync();
```

It is possible to map columns if the naming is different by supplying a ColumnMap:

```c#
var sourceName = "Id";
var destName = "SourceId";
var customMapping = new ColumnMapping(sourceName, destName);
var map = new List<ColumnMapping>() {customMapping};
var flow = new Dataflow(sourceTable, sourceConnectionString, destTable, destConnectionString, map);
```

By supplying a function from source data-objects to destination objects a transformation can be made:

```c#
Func<(int id, DateTime date, string name), (int, DateTime, string, DateTime)> map = (x) => (x.id, x.date, "SourceValue=" + x.name, DateTime.Now);
var flow = new Dataflow(sourceTable, sourceConnectionString, destTable, destConnectionString, map);
```

## Load Patterns

By default data is just directly copied from source to destination but there are a few ways to gain more control. The simplest is supplying an IEnumerable instead of a source table - that allows for arbitrary control over input to the destination:

```c#
//Example of streaming from database query and list to the database
```

Some common standard patterns exist as well. Those are:

* TruncateAndLoad: Truncates the destination table, copies the input and optionally calls a stored procedure at the destination
* IncrementalChangeTracking: Uses change tracking in source to merge only new changes into the destination
* IncrementalDate: Uses date or time to merge new changes into the destination - be wary of deletes in the source
* IncrementalId: Uses incrementing ids in the source to copy only new changes into the destination - be wary of updates and deletes in the source

Patterns are given as optional arguments to Dataflows:
```c#
var pattern = new IncrementalIdPattern(Id: 133);
var flow = new Dataflow(sourceTable, sourceConnectionString, destTable, destConnectionString, pattern);
```