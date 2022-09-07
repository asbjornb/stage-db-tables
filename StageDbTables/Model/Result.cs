using System.Collections.Generic;

namespace StageDbTables.Model;

public class Result
{
    public List<string> Errors { get; }

    public bool IsSuccess => Errors.Count == 0;

    public Result(List<string> errors)
    {
        Errors = errors;
    }

    public Result()
    {
        Errors = new();
    }

    public static Result Success()
    {
        return new Result();
    }

    public static Result Failure(string error)
    {
        return new Result(new List<string>() { error });
    }

    public void AddError(string error)
    {
        Errors.Add(error);
    }
}
