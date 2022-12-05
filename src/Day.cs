namespace Patersoft.AOC;

using Microsoft.Extensions.Logging;
using System;

public class DayResult
{
    public string Part1Result { get; init; }
    public TimeSpan Part1Time { get; init; }
    public string Part2Result { get; init; }
    public TimeSpan Part2Time { get; init; }

    public DayResult (string part1Result, TimeSpan part1Time, string part2Result, TimeSpan part2Time)
    {
        this.Part1Result = part1Result;
        this.Part1Time = part1Time;
        this.Part2Result = part2Result;
        this.Part2Time = part2Time;
    }
}

public class TestResult : DayResult
{
    public bool? Part1Correct { get; init; }
    public bool? Part2Correct { get; init; }

    public TestResult (string part1Result, bool? part1Correct, TimeSpan part1Time, string part2Result, bool? part2Correct, TimeSpan part2Time)
        : base(part1Result, part1Time, part2Result, part2Time)
    {
        this.Part1Correct = part1Correct;
        this.Part2Correct = part2Correct;
    }
}

public abstract class Day
{
    private string? _input;

    public string Year { get; init; }
    public int DayNumber { get; init; }
    protected AOCEnvironment Env { get; init; }
    protected string Input
    {
        get
        {
            if (_input != null)
            {
                return _input;
            } else {
                throw new InputException("Input has not been fetched!");
            }
        }
    }

    public Day(string year, int dayNumber, AOCEnvironment env)
    {
        this.Year = year;
        this.DayNumber = dayNumber;
        this.Env = env;
    }

    public TestResult? Test()
    {
        using (Env.Logger.BeginScope($"[Day {DayNumber}]"))
        {
            Env.Logger.LogDebug("Running in test mode");
            _input = GetExampleInput();
            if (_input != null)
            {
                Env.Logger.LogDebug("Test input available, begin execution");
                DateTime start = DateTime.Now;
                string part1Result = ExecutePart1();
                TimeSpan part1Time = DateTime.Now - start;
                string part2Result = ExecutePart2();
                TimeSpan part2Time = DateTime.Now - start - part1Time;

                bool? part1Correct = null;
                bool? part2Correct = null;

                string? part1ExpectedResult = GetExamplePart1Answer();
                if (part1ExpectedResult != null)
                {
                    part1Correct = (part1Result == part1ExpectedResult);
                }
                string? part2ExpectedResult = GetExamplePart2Answer();
                if (part2ExpectedResult != null)
                {
                    part2Correct = (part2Result == part2ExpectedResult);
                }

                return new TestResult(part1Result, part1Correct, part1Time, part2Result, part2Correct, part2Time);
            } else {
                Env.Logger.LogDebug("Test input not available");
                return null;
            }
        }
    }

    public async Task<DayResult> Execute()
    {
        InputFetcher fetcher = new InputFetcher(Env, Year);
        _input = await fetcher.Fetch(DayNumber);

        using (Env.Logger.BeginScope($"[Day {DayNumber}]"))
        {
            Env.Logger.LogDebug("Begin execution");
            DateTime start = DateTime.Now;
            string part1Result = ExecutePart1();
            TimeSpan part1Time = DateTime.Now - start;
            string part2Result = ExecutePart2();
            TimeSpan part2Time = DateTime.Now - start - part1Time;
            return new DayResult(part1Result, part1Time, part2Result, part2Time);
        }
    }

    protected abstract string ExecutePart1();
    protected abstract string ExecutePart2();

    protected virtual string? GetExampleInput()
    {
        return null;
    }

    protected virtual string? GetExamplePart1Answer()
    {
        return null;
    }

    protected virtual string? GetExamplePart2Answer()
    {
        return null;
    }
}