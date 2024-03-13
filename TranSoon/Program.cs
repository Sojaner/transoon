using System.Globalization;
using CommandLine;
using System.Text.RegularExpressions;
using TranSoon;
using TranSoon.Translators;

await Parser.Default.ParseArguments<Options>(args)
    .WithParsedAsync(options => new Analyzer(new Regex(options.RegexPattern), new GoogleTranslateDemo(CultureInfo.GetCultureInfo(options.Language)), true).TranslateComments(options.DirectoryPath));