# Advent of Code Runner for .NET
This project provides a simple framework library that makes it easy for you to tackle the Advent of Code in a .NET language while writing as little code as possible that isn't directly solving each day's task. The framework will take care of fetching the input from the AoC website, caching it locally and providing it to your code, as well as timing the execution and outputting the results.

To use it:
* Reference this package:

```xml
<PackageReference Include="Patersoft.AOC.Runner" />
```
* Write each day's solution as a subclass of `Patersoft.AOC.Day`, providing a constructor that takes a single `AOCEnvironment` parameter:

```C#
using Patersoft.AOC;

public class Day1 : Day
{
    public Day1(AOCEnvironment env) : base("2022", 1, env) { }

    protected override string ExecutePart1()
    {
        // Some clever logic
        return "the answer";
    }

    protected override string ExecutePart2()
    {
        // More clever logic
        return "another answer";
    }
}
```
* Optionally, override the `GetExampleInput()`, `GetExamplePart1Answer()` and `GetExamplePart2Answer()` methods, just returning the examples given to you in the question. If you do this, your solution will be tested with this data as well.
```C#
    protected override string? GetExampleInput()
    {
        return @"A Y
B X
C Z";
    }

    protected override string? GetExamplePart1Answer()
    {
        return "15";
    }

    protected override string? GetExamplePart2Answer()
    {
        return "12";
    }
```
* Write a `Main()` method that passes all of your subclasses to an `AOCRunner` and then calls `Run()`.

```C#
using Patersoft.AOC;
using Microsoft.Extensions.Logging.Abstractions;

public class AOC22
{
    public static void Main(string[] args)
    {
        AOCRunner runner = AOCRunner.BuildRunner(NullLogger<AOCRunner>.Instance, new System.Type[] {
            typeof(Day1),
            typeof(Day2),
            typeof(Day3),
            typeof(Day4),
        });
        runner.Run(1, 4);
    }
}
```

This example does no logging, but you can provide an alternative `ILogger<AOCRunner>` implementation if you'd like logging, e.g.:
```C#
        using ILoggerFactory loggerFactory =
            LoggerFactory.Create(builder =>
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                }).SetMinimumLevel(LogLevel.Warning));

        ILogger<AOCRunner> logger = loggerFactory.CreateLogger<AOCRunner>();
```
