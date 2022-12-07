namespace Patersoft.AOC;

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;

public enum RunMode
{
    TodayOnly,
    SoFar,
    AllDays
}

public class AOCRunner
{
    private AOCEnvironment Env { get; init; }
    private IDictionary<int, ConstructorInfo> Days { get; init; }

    private AOCRunner(AOCEnvironment env, IDictionary<int, ConstructorInfo> days)
    {
        this.Env = env;
        this.Days = days;
    }

    public static AOCRunner BuildRunner(ILogger<AOCRunner> logger, Type[] dayTypes)
    {
        using (logger.BeginScope("[Runner]"))
        {
            logger.LogDebug("Instantiating");
            AOCEnvironment env = AOCEnvironment.Initialise(logger).GetAwaiter().GetResult();
            IDictionary<int, ConstructorInfo> days = InstantiateDays(env, dayTypes);
            return new AOCRunner(env, days);
        }
    }

    /// <summary>
    /// Run Advent of Code challenge days, using a sensible default behaviour.
    /// </summary>
    public void Run()
    {
        Run(new string[0]);
    }

    /// <summary>
    /// Run Advent of Code challenge days, using user-controlled inputs.
    /// </summary>
    /// <param name="commandLineParams">Command-line arguments passed to the program, omitting the executable. Accepted options:
    /// -t, --today: run the day number corresponding to today's date (also the default if no arguments are passed)
    /// -s, --sofar: run days from 1 to today
    /// -a, --all: run all days, 1-25
    /// start finish: run days from _start_ to _finish_, inclusive
    /// </param>
    public void Run(string[] commandLineParams)
    {
        switch (commandLineParams.Length)
        {
            case 0:
                // No parameters passed, invoke default behaviour: today only.
                try
                {
                    Run(RunMode.TodayOnly);
                }
                catch (Exception)
                {
                    Console.WriteLine("Can only run in 'today' mode during the Advent of Code.");
                }
                break;

            case 1:
                switch (commandLineParams[0])
                {
                    case "-t":
                    case "--today":
                        try
                        {
                            Run(RunMode.TodayOnly);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Can only run in 'today' mode during the Advent of Code.");
                        }
                        break;

                    case "-s":
                    case "--sofar":
                        try
                        {
                            Run(RunMode.SoFar);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Can only run in 'so far' mode during the Advent of Code.");
                        }
                        break;

                    case "-a":
                    case "--all":
                        Run(RunMode.AllDays);
                        break;

                    default:
                        Console.WriteLine("Invalid command line arguments provided.");
                        break;
                }
                break;

            case 2:
                try
                {
                    int firstDay = int.Parse(commandLineParams[0]);
                    int lastDay = int.Parse(commandLineParams[1]);
                    Run(firstDay, lastDay);
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid command line arguments provided.");
                }
                catch (Exception)
                {
                    Console.WriteLine("The first and last days must both be within the range 1-25, and the last day must be equal to or greater than the first day.");
                }
                break;

            default:
                Console.WriteLine("Invalid command line arguments provided.");
                break;
        }
    }

    /// <summary>
    /// Run Advent of Code challenge days, using an unspecified range.
    /// </summary>
    /// <param name="mode">Whether to run all days, or just today.</param>
    public void Run(RunMode mode)
    {
        switch (mode)
        {
            case RunMode.AllDays:
                Run(1, 25);
                break;

            case RunMode.SoFar:
                Run(1, DateTime.Now.Day);
                break;

            case RunMode.TodayOnly:
                int today = DateTime.Now.Day;
                Run(today, today);
                break;
        }
    }

    /// <summary>
    /// Run a contiguous set of Advent of Code challenge days, in sequence.
    /// </summary>
    /// <param name="firstDay">The day number of the first day to run.</param>
    /// <param name="lastDay">The day number of the last day to run, inclusive.</param>
    public void Run(int firstDay, int lastDay)
    {
        using (Env.Logger.BeginScope("[Runner]"))
        {
            Env.Logger.LogDebug($"Beginning run");

            if ((firstDay < 1) || (firstDay > 25) || (lastDay < 1) || (lastDay > 25))
            {
                Env.Logger.LogError($"Invalid firstDay or lastDay: {firstDay}-{lastDay}");
                throw new Exception("firstDay and lastDay must be 1-25 inclusive");
            }
            if (firstDay > lastDay)
            {
                Env.Logger.LogError("Days in wrong order");
                throw new Exception("firstDay must be less than or equal to lastDay");
            }

            TimeSpan totalTime = TimeSpan.Zero;
            for (int dayNumber = firstDay; dayNumber <= lastDay; dayNumber++)
            {
                try
                {
                    using (Env.Logger.BeginScope($"[Day {dayNumber}]"))
                    {
                        Task<TimeSpan> task = RunDay(dayNumber);
                        task.Wait();
                        totalTime += task.Result;
                    }
                }
                catch (Exception e)
                {
                    // The only exception not handled by RunDay is a DayNotAvailableException,
                    // but Wait() wraps it in an AggregateException so we can't handle it directly.
                    // We don't want to keep trying further days if this one isn't available yet,
                    // they'll all have the same result.
                    PrintFriendlyException(e);
                    break;
                }
            }

            Console.WriteLine($"--------------------------\nTotal time: {totalTime}");
        }
    }

    private async Task<TimeSpan> RunDay(int dayNumber)
    {
        try
        {
            Env.Logger.LogDebug($"Attempting day {dayNumber}");
            Console.WriteLine($"--------------------------\nDay {dayNumber}");
            ConstructorInfo dayConst = Days[dayNumber];

            // Running the solution is not necessarily idempotent, so
            // we need separate instances to run Test() and Execute() on.
            Object[] constructorParams = new Object[] { Env };
            Day testDay = (Day)dayConst.Invoke(constructorParams);
            TestResult? testResult = testDay.Test();
            if (testResult != null)
            {
                Console.WriteLine("--Example input--");
                Console.Write($"Part 1: {testResult.Part1Result} ");
                WriteResult(testResult.Part1Correct);
                Console.WriteLine($"({testResult.Part1Time})");
                Console.Write($"Part 2: {testResult.Part2Result} ");
                WriteResult(testResult.Part2Correct);
                Console.WriteLine($"({testResult.Part2Time})");
                Console.WriteLine("--Real input--");
            }
            Day day = (Day)dayConst.Invoke(constructorParams);
            DayResult result = await day.Execute();
            Console.WriteLine($"Part 1: {result.Part1Result} ({result.Part1Time})\nPart 2: {result.Part2Result} ({result.Part2Time})");
            return result.Part1Time + result.Part2Time;
        }
        catch (KeyNotFoundException)
        {
            Env.Logger.LogError("Day class not provided");
            Console.WriteLine($"The set of days to run included {dayNumber}, but no class was provided implementing that day.");
            return TimeSpan.Zero;
        }
        catch (DayNotAvailableException e)
        {
            Env.Logger.LogWarning("Day not available yet", e);
            throw e;
        }
        catch (InputException e)
        {
            Env.Logger.LogError("Input exception hit", e);
            Console.WriteLine($"Couldn't fetch input for day {dayNumber}");
            PrintFriendlyException(e);
            return TimeSpan.Zero;
        }
        catch (Exception e)
        {
            Env.Logger.LogError("Exception hit during day code", e);
            PrintFriendlyException(e);
            return TimeSpan.Zero;
        }
    }

    private static void WriteResult(bool? correct)
    {
        if (correct.HasValue)
        {
            if (correct.Value)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[CORRECT] ");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"[INCORRECT] ");
                Console.ResetColor();
            }
        }
    }

    private static IDictionary<int, ConstructorInfo> InstantiateDays(AOCEnvironment env, Type[] dayTypes)
    {
        IDictionary<int, ConstructorInfo> days = new Dictionary<int, ConstructorInfo>(25);
        Type[] constructorParamTypes = new Type[] { typeof(AOCEnvironment) };
        Object[] constructorParams = new Object[] { env };
        foreach (Type dayType in dayTypes)
        {
            env.Logger.LogDebug($"Adding day class {dayType}");
            if (!dayType.IsSubclassOf(typeof(Day)))
            {
                env.Logger.LogError("Not a subclass of Day");
                throw new Exception($"Type {dayType} is not a subclass of Patersoft.AOC.Day.");
            }

            ConstructorInfo? constructor = dayType.GetConstructor(constructorParamTypes);
            if (constructor != null)
            {
                if (constructor.IsPublic)
                {
                    try
                    {
                        Day newDay = (Day)constructor.Invoke(constructorParams);
                        env.Logger.LogDebug($"Day constructed: {newDay.Year}-{newDay.DayNumber}");
                        if (days.ContainsKey(newDay.DayNumber))
                        {
                            env.Logger.LogError("Duplicate day");
                            throw new Exception($"Two classes were provided that both claimed to be day {newDay.DayNumber}. Make sure you only provide each day once.");
                        }
                        days.Add(newDay.DayNumber, constructor);
                    }
                    catch (Exception e)
                    {
                        env.Logger.LogError(e, "Unexpected exception invoking Day subclass constructor");
                        throw new Exception($"An unexpected exception was hit attempting to construct an instance of type {dayType}. Please check the inner exception for details.", e);
                    }
                }
                else
                {
                    env.Logger.LogError($"Type {dayType} does not have public constructor");
                    throw new Exception($"Type {dayType} does not have the required constructor. The constructor taking a single AOCEnvironment parameter must be public.");
                }
            }
            else
            {
                env.Logger.LogError("Doesn't have required constructor");
                throw new Exception($"Type {dayType} does not have the required constructor. Day classes must have a public constructor taking a single AOCEnvironment parameter.");
            }
        }

        return days;
    }

    static void PrintFriendlyException(Exception e)
    {
        Console.WriteLine($"{e.Message}\n\nDebug details:\n{e}");
    }
}
