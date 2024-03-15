using System.Globalization;
using CommandLine;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;
using TranSooner;
using TranSooner.Translators;
using Analyzer = TranSooner.Analyzer;
using Utilities = TranSooner.Utilities;

ConsoleColor consoleColor = Console.ForegroundColor;

await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(options =>
{
    if (!options.NoLogo)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;

        Console.WriteLine(Utilities.Logo);

        Console.ForegroundColor = consoleColor;
    }

    Console.WriteLine("Translating comments...");

    if (options.Translator.Equals("google", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(options.ApiKey) && !options.Acknowledged)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;

        Console.WriteLine("NOTE: Using Google Translate without and API Key will switch to Google's free Translation API that is provided only for demo purposes and should not be used in commercial and production environments.");

        Console.ResetColor();

        Console.WriteLine();

        Console.Write("Do you want to continue? [y/n] ");

        string? answer = Console.ReadLine();

        if (!Utilities.Answer().IsMatch((answer ?? "").Trim()))
        {
            Console.WriteLine();

            Console.WriteLine("Terminating the translation...");

            return Task.CompletedTask;
        }

        Console.Clear();

        Console.ForegroundColor = consoleColor;
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

    if (options.Includes.Where(include => !string.IsNullOrWhiteSpace(include)).ToList() is { Count: > 0 } includes)
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

    return new Analyzer(new Regex(options.RegexPattern), translator, options.CapitalizeFirstLetter, options.PreprocessorSymbols).TranslateComments(options.DirectoryPath, matcher);
});

Console.ForegroundColor = consoleColor;