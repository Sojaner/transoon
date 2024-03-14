using CommandLine;

namespace TranSooner;

internal class Options
{
    [Option('d', "directory", Required = false, HelpText = "Directory containing .cs files. (Default is current working directory)")]
    public string DirectoryPath { get; set; } = ".";

    [Option('k', "api-key", Required = false, HelpText = "Google Translate API Key.")]
    public string ApiKey { get; set; } = null!;

    [Option('t', "translator", Required = false, HelpText = "Translator to use (google, deepl). (Default is google)")]
    public string Translator { get; set; } = "google";

    [Option('l', "language", Required = false, HelpText = "Language to translate to. (Default is en-US)")]
    public string Language { get; set; } = "en-US";

    [Option('r', "regex", Required = false, HelpText = @"Regex pattern to match comments to be translated. (Default is [^\x00-\x7F])")]
    public string RegexPattern { get; set; } = @"[^\x00-\x7F]";

    [Option('i', "includes", Required = false, HelpText = "Glob pattern to include files. (Default is **/*.cs)")]
    public IEnumerable<string> Includes { get; set; } = ["**/*.cs"];

    [Option('e', "excludes", Required = false, HelpText = "Glob pattern to exclude files. (Default is bin/*.* obj/*.*)")]
    public IEnumerable<string> Excludes { get; set; } = ["bin/*.*", "obj/*.*"];

    [Option('c', "capitalize", Required = false, HelpText = "Capitalize first letter of translated comments. (Default is true)")]
    public bool CapitalizeFirstLetter { get; set; } = true;

    [Option('y', "yes", Required = false, HelpText = "Acknowledge and disable the \"Google's free Translation API\" usage warning by implicitly answering \"yes\". (Default is false)")]
    public bool Acknowledged { get; set;} = false;

    [Option('s', "symbols", Required = false, HelpText = "Preprocessor symbols to use with the C# parser for translating conditional code parts. (Default is no symbols)")]
    public IEnumerable<string> PreprocessorSymbols { get; set; } = [];

    [Option('n', "no-logo", Required = false, HelpText = "Skip printing the logo. (Default is false)")]
    public bool NoLogo { get; set; } = false;
}