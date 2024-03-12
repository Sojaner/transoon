using CommandLine;
using TranSoon;

await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(Analyzer.TranslateComments);

