using CommandLine;

namespace TranSoon;

internal class Options
{
    [Option('k', "api-key", Required = true, HelpText = "Google Translate API Key")]
    public string ApiKey { get; set; } = null!;

    [Option('d', "directory", Required = true, HelpText = "Directory containing .cs files")]
    public string DirectoryPath { get; set; } = null!;

    [Option('l', "language", Required = false, HelpText = "Language to translate to")]
    public string Language { get; set; } = "en-US";
}