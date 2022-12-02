namespace Patersoft.AOC;

using Microsoft.Extensions.Logging;
using System.IO;

public class InputException : Exception
{
    public InputException()
    {
    }

    public InputException(string message) : base(message)
    {
    }

    public InputException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class DayNotAvailableException : InputException
{
    public DayNotAvailableException()
    {
    }

    public DayNotAvailableException(string message) : base(message)
    {
    }

    public DayNotAvailableException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class AOCEnvironment
{
    private static readonly string SESSION_FILENAME = "session";

    public ILogger<AOCRunner> Logger { get; init; }
    public string Directory { get; init; }
    public string Session { get; init; }

    public string SessionFilename
    {
        get
        {
            return BuildSessionFilename(Directory);
        }
    }

    private AOCEnvironment(ILogger<AOCRunner> logger, string directory, string session)
    {
        this.Logger = logger;
        this.Directory = directory;
        this.Session = session;
    }

    public static string BuildSessionFilename(string directory)
    {
        return directory + Path.DirectorySeparatorChar + SESSION_FILENAME;
    }

    public static async Task<AOCEnvironment> Initialise(ILogger<AOCRunner> logger)
    {
        using (logger.BeginScope("[Env]"))
        {
            string inputDir = AppDomain.CurrentDomain.BaseDirectory + "inputs";
            try
            {
                logger.LogDebug($"Creating directory {inputDir}");
                System.IO.Directory.CreateDirectory(inputDir);
            }
            catch (PathTooLongException e)
            {
                logger.LogError(e, "Path too long");
                throw new InputException("Unable to create a subdirectory as the path would be too long. Please run this program higher up the directory stack.", e);
            }
            catch (IOException e)
            {
                logger.LogError(e, "IO exception hit");
                throw new InputException("This program must be run with adequate permissions to create a subdirectory under the current directory.", e);
            }

            string sessionFilename = BuildSessionFilename(inputDir);
            string session;
            try
            {
                logger.LogDebug($"Attempting to read session file ${sessionFilename}");
                session = File.ReadAllText(sessionFilename);
                logger.LogDebug($"Cached session is {session}");
            }
            catch (PathTooLongException e)
            {
                logger.LogError(e, "Path too long");
                throw new InputException("Unable to read input as the path would be too long. Please run this program higher up the directory stack.", e);
            }
            catch (UnauthorizedAccessException e)
            {
                logger.LogError(e, "Not authorized");
                throw new InputException("Unable to read the session file as the application does not have permissions. Please check file permissions.", e);
            }
            catch (FileNotFoundException)
            {
                logger.LogInformation("No session file found, prompt user");
                Console.WriteLine("In order to download the inputs from the Advent of Code website, this program requires your session cookie.");
                Console.WriteLine("Please log into the Advent of Code website, then check your browser cookies and enter the value of the 'session' cookie now.");
                string? input = Console.ReadLine();
                if (input != null)
                {
                    session = input;
                    logger.LogDebug($"Session entered: {session}");
                    logger.LogDebug($"Attempting to store in {sessionFilename}");
                    try
                    {
                        await File.WriteAllTextAsync(sessionFilename, session);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        logger.LogError(e, "Not authorized");
                        throw new InputException($"Couldn't save the session cookie. The program doesn't have permissions to create {sessionFilename}. Please check directory permissions.", e);
                    }
                    catch (PathTooLongException e)
                    {
                        logger.LogError(e, "Path too long");
                        throw new InputException($"Couldn't save the session cookie. The path {sessionFilename} would be too long. Please run the program higher up the directory tree.", e);
                    }
                    catch (IOException e)
                    {
                        logger.LogError(e, "Unexpected exception");
                        throw new InputException($"Couldn't save the session cookie. The program hit an unexpected error while trying to save it in {sessionFilename}.", e);
                    }
                }
                else
                {
                    logger.LogError("User didn't enter session cookie");
                    throw new InputException("You didn't enter your session cookie, so the program is unable to download your puzzle inputs.");
                }
            }
            catch (IOException e)
            {
                logger.LogError(e, "Unexpected exception");
                throw new InputException("An unexpected error occurred while reading the session file.", e);
            }

            return new AOCEnvironment(logger, inputDir, session);
        }
    }
}
