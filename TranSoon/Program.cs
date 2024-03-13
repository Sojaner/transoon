using System.Globalization;
using CommandLine;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;
using TranSoon;
using TranSoon.Translators;

await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(options =>
{
    if (options.Translator.Equals("google", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(options.ApiKey))
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;

        Console.WriteLine("Using Google Translate without and API Key will switch to Google's free API that is provided only for demo purposes and should not be used in commercial and production environments.");

        Console.WriteLine("Please use with caution and at your own risk!");

        Console.ResetColor();

        Console.WriteLine();

        Console.Write("Do you want to continue? [y/n] ");

        string? answer = Console.ReadLine();

        if (!Regex.IsMatch((answer ?? "").Trim(), "^y(?:es)?$"))
        {
            Console.WriteLine("Terminating the translation...");

            return Task.CompletedTask;
        }

        Console.Clear();
    }

    ITranslator translator;

    if (options.Translator.Equals("google", StringComparison.OrdinalIgnoreCase))
    {
        translator = string.IsNullOrWhiteSpace(options.ApiKey) ? new GoogleTranslateDemoTranslator(CultureInfo.GetCultureInfo(options.Language)) : new GoogleTranslateTranslator(CultureInfo.GetCultureInfo(options.Language), options.ApiKey);
    }
    else if (options.Translator.Equals("deepl", StringComparison.OrdinalIgnoreCase))
    {
        translator = new DeepLTranslator(CultureInfo.GetCultureInfo(options.Language), options.ApiKey);
    }
    else
    {
        Console.WriteLine("Invalid translator. Please use 'google' or 'deepl'.");

        return Task.CompletedTask;
    }

    Matcher matcher = new ();

    if (options.Includes.Where(include => !string.IsNullOrWhiteSpace(include)).ToList() is {Count: > 0} includes)
    {
        matcher.AddIncludePatterns(includes);
    }
    else
    {
        matcher.AddInclude("**/*.cs");
    }

    if (options.Excludes.Where(exclude => !string.IsNullOrWhiteSpace(exclude)).ToList() is { Count: > 0 } excludes)
    {
        matcher.AddExcludePatterns(excludes);
    }

    return new Analyzer(new Regex(options.RegexPattern), translator, options.CapitalizeFirstLetter).TranslateComments(options.DirectoryPath, matcher);
});