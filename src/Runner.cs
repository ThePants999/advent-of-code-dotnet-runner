namespace Patersoft.AOC;

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;

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

    public void Run(int firstDay, int lastDay)
    {
        using (Env.Logger.BeginScope("[Runner]"))
        {
            Env.Logger.LogDebug($"Beginning run");

            if (firstDay > lastDay)
            {
                Env.Logger.LogError("Days in wrong order");
                throw new Exception("firstDay must be less than or equal to lastDay");
            }

            for (int dayNumber = firstDay; dayNumber <= lastDay; dayNumber++)
            {
                try
                {
                    using (Env.Logger.BeginScope($"[Day {dayNumber}]"))
                    {
                        RunDay(dayNumber).Wait();
                    }
                }
                catch (Exception)
                {
                    // The only exception not handled by RunDay is a DayNotAvailableException,
                    // but Wait() wraps it in an AggregateException so we can't handle it directly.
                    // We don't want to keep trying further days if this one isn't available yet,
                    // they'll all have the same result.
                    break;
                }
            }
        }
    }

    private async Task RunDay(int dayNumber)
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
        }
        catch (KeyNotFoundException)
        {
            Env.Logger.LogError("Day class not provided");
            throw new Exception($"No class was provided for day {dayNumber}");
        }
        catch (DayNotAvailableException e)
        {
            Env.Logger.LogWarning("Day not available yet", e);
            PrintFriendlyException(e);
            throw e;
        }
        catch (InputException e)
        {
            Env.Logger.LogError("Input exception hit", e);
            Console.WriteLine($"Couldn't fetch input for day {dayNumber}");
            PrintFriendlyException(e);
        }
        catch (Exception e)
        {
            Env.Logger.LogError("Exception hit during day code", e);
            PrintFriendlyException(e);
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
            } else {
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
