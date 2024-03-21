using CodeChallengeBase;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

static class Program
{
    const string ROOT_PATH = "../../../";
    const string INPUTS_DIRECTORY = ROOT_PATH + "inputs/";
    const string OUTPUTS_DIRECTORY = ROOT_PATH + "outputs/";
    const string SCORES_DIRECTORY = ROOT_PATH + "scores/";

    static object mainLockObject = new();
    static Dictionary<string, ConsoleColor> threadColor = new();

    public static void Main()
    {
        ConsoleColor[] relevantColors = new[]
        {
            ConsoleColor.Green,
            ConsoleColor.Red,
            ConsoleColor.Yellow,
            ConsoleColor.Magenta,
            ConsoleColor.Cyan,
            ConsoleColor.Blue,
            ConsoleColor.DarkYellow
        };

        var cts = new CancellationTokenSource();
        List<Thread> threads = new();

        int i = 0;
        foreach (var input in Directory.GetFiles(INPUTS_DIRECTORY))
        {
            string filename = Path.GetFileName(input);
            if (filename.StartsWith("."))
            {
                continue;
            }
            threadColor.Add(filename, relevantColors[i % relevantColors.Length]);
            threads.Add(new Thread(() =>
            {
                try
                {
                    SimpleSolution(filename, cts.Token);
                }
                catch (Exception e)
                {
                    Log(filename, $"{e.Message} \n{e.StackTrace}");
                }
                finally
                {
                    Log(filename, "End");
                }
            }));
            i++;
        }

        threads.ForEach(t => t.Start());

        Console.ReadLine();

        cts.Cancel();
        threads.ForEach(t => t.Join());
        cts.Dispose();
    }

    public static void SimpleSolution(string problemName, CancellationToken cancelToken)
    {
        Log(problemName, "Start new simple solution");
        InputData data = GetInputData(problemName);
        Solution solution = new(data);
        solution.SimpleSolution();
        SaveSolutionIfBetter(problemName, solution, true);
    }

    public static void BruteForceSolution(string problemName, CancellationToken cancelToken)
    {
        Log(problemName, "Start new brute force solution");
        InputData data = GetInputData(problemName);
        Solution solution = new(data);
        for (long i = 0; i < long.MaxValue; i++)
        {
            solution.BruteForceSolution(i);
            SaveSolutionIfBetter(problemName, solution);
            if (cancelToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public static InputData GetInputData(string problemName)
    {
        var data = new InputData(File.ReadAllLines(INPUTS_DIRECTORY + problemName));
        Log(problemName, $"Read all data from {problemName}");
        return data;
    }

    public static bool SaveSolutionIfBetter(string problemName, Solution solution, bool forceSave = false)
    {
        string scorePath = SCORES_DIRECTORY + problemName;
        int oldScore = 0;
        int newScore = solution.Score();
        bool save = true;

        if (File.Exists(scorePath))
        {
            oldScore = int.Parse(File.ReadAllText(scorePath));
            save = newScore > oldScore;
        }

        if (save || forceSave)
        {
            File.WriteAllText(scorePath, newScore.ToString());
            File.WriteAllText(OUTPUTS_DIRECTORY + problemName, solution.ToString());
            Log(problemName, $"Saved new solution ({newScore})");
        }
        if (newScore > oldScore)
        {
            Log(problemName, $"Improved score {newScore} (+{newScore - oldScore})");
        }
        return save;
    }

    public static void Log(string problemName, string message)
    {
        lock (mainLockObject)
        {
            Console.ForegroundColor = threadColor[problemName];
            Console.Write($"[{problemName[..2]}]\t");
            Console.ResetColor();
            Console.WriteLine(message);
        }
    }
}