namespace Patersoft.AOC;

using Microsoft.Extensions.Logging;
using System.IO;
using System.Net;

public class InputFetcher : IDisposable
{
    private HttpClientHandler _handler;
    private HttpClient _client;
    private AOCEnvironment _env;
    private readonly ILogger<AOCRunner> logger;

    public string Year { get; init; }

    public InputFetcher(AOCEnvironment env, string year)
    {
        _env = env;
        logger = env.Logger;
        this.Year = year;

        CookieContainer cookies = new CookieContainer();
        cookies.Add(new Cookie("session", _env.Session, "/", ".adventofcode.com"));
        _handler = new HttpClientHandler();
        _handler.CookieContainer = cookies;
        _client = new HttpClient(_handler);
    }

    public async Task<string> Fetch(int day)
    {
        using (logger.BeginScope("[InputFetcher]"))
        {
            string input;
            logger.LogDebug($"Fetching input for day {day}");
            try
            {
                logger.LogDebug("Trying from file");
                input = FetchFromFile(day);
            }
            catch (FileNotFoundException)
            {
                logger.LogDebug("Trying from website");
                input = await FetchFromWebsite(day);
            }
            logger.LogDebug("Success");
            return input;
        }
    }

    private string FetchFromFile(int day)
    {
        string filename = DayFilename(day);
        try
        {
            logger.LogDebug($"Attempting to read from {filename}");
            return File.ReadAllText(filename);
        }
        catch (Exception e) when (e is UnauthorizedAccessException || e is System.Security.SecurityException)
        {
            logger.LogError(e, "Unauthorized");
            throw new InputException($"Couldn't read input from {filename} due to a permissions issue. Please check directory/file permissions.", e);
        }
        catch (PathTooLongException e)
        {
            logger.LogError(e, "Path too long");
            throw new InputException($"Couldn't read input from {filename} because the path would be too long. Please run the program higher up the directory tree.", e);
        }
        catch (FileNotFoundException e)
        {
            logger.LogDebug("File not found");
            throw e;
        }
        catch (IOException e)
        {
            logger.LogError(e, "Unexpected exception");
            throw new InputException($"Couldn't read input from {filename} due to an unexpected error.", e);
        }

    }

    private async Task<string> FetchFromWebsite(int day)
    {
        Uri url = new Uri($"https://adventofcode.com/{Year}/day/{day}/input");
        string input;
        try
        {
            logger.LogDebug($"Attempting to fetch from {url}");
            HttpResponseMessage response = await _client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                logger.LogDebug("Success");
                input = await response.Content.ReadAsStringAsync();
            }
            else
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        logger.LogWarning("Day not available yet");
                        throw new DayNotAvailableException($"Day {day} isn't available yet. Please don't run this program specifying all days until after the season is completed.");
                    case HttpStatusCode.BadRequest:
                        logger.LogError("Stored session is invalid");
                        throw new InputException($"The stored session cookie is invalid. Please delete {_env.SessionFilename} and run the program again.");
                    default:
                        logger.LogError($"Received error code {response.StatusCode}");
                        throw new InputException($"Received unexpected response code {response.StatusCode} while attempting to download input for day {day}. Try deleting {_env.SessionFilename} and run the program again.");
                }
            }
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e, "HTTP exception");
            throw new InputException($"Couldn't download the input for day {day} from the AoC website. Check your Internet connection.", e);
        }

        string filename = DayFilename(day);
        try
        {
            logger.LogDebug($"Attempting to store downloaded input in {filename}");
            await File.WriteAllTextAsync(filename, input);
        }
        catch (UnauthorizedAccessException e)
        {
            logger.LogError(e, "Unauthorized");
            throw new InputException($"Couldn't save an input file. Input was successfully downloaded from the AoC website, but the program doesn't have permissions to create {filename}. Please check directory permissions.", e);
        }
        catch (PathTooLongException e)
        {
            logger.LogError(e, "Path too long");
            throw new InputException($"Couldn't save an input file. Input was successfully downloaded from the AoC website, but the path {filename} would be too long. Please run the program higher up the directory tree.", e);
        }
        catch (IOException e)
        {
            logger.LogError(e, "Unexpected exception");
            throw new InputException($"Couldn't save an input file. Input was successfully downloaded from the AoC website, but the program hit an unexpected error while trying to save it in {filename}.", e);
        }

        return input;
    }

    private string DayFilename(int day)
    {
        return $"{_env.Directory}{Path.DirectorySeparatorChar}{Year}-{day}";
    }

    public void Dispose()
    {
        _client.Dispose();
        _handler.Dispose();
    }
}