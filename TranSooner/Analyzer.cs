using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileSystemGlobbing;
using ShellProgressBar;

namespace TranSooner;

internal partial class Analyzer(Regex translatable, ITranslator translator, Options options, bool noColor)
{
    private readonly Func<string, bool> _shouldTranslate = text => !string.IsNullOrWhiteSpace(text) && translatable.IsMatch(text);

    private readonly IMemoryCache _translationsCache = new MemoryCache(new MemoryCacheOptions());

    public async Task TranslateAsync(Matcher matcher)
    {
        string directoryPath = Path.GetFullPath(Path.IsPathRooted(options.DirectoryPath) ? options.DirectoryPath : Path.Combine(Directory.GetCurrentDirectory(), options.DirectoryPath));
        
        ConsoleColor consoleColor = Console.ForegroundColor;

        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"Folder {directoryPath} does not exist.");

            return;
        }

        string[] csFiles = matcher.GetResultsInFullPath(directoryPath).ToArray();

        int totalTicks = csFiles.Length;

        ProgressBarOptions progressBarOptions = new ()
        {
            ForegroundColor = noColor ? consoleColor : ConsoleColor.Yellow,

            BackgroundColor = noColor ? consoleColor : ConsoleColor.DarkYellow,

            ProgressCharacter = '─',

            CollapseWhenFinished = false
        };

        using ProgressBar? progressBar = options.NoProgress || Console.IsOutputRedirected ? null : new ProgressBar(totalTicks, directoryPath, progressBarOptions);

        int translated = 0;

        int processed = 0;

        int failed = 0;

        Statistics statistics = new();

        foreach (string file in csFiles)
        {
            ProgressBarOptions childOptions = new()
            {
                ForegroundColor = noColor ? consoleColor : ConsoleColor.Green,

                BackgroundColor = noColor ? consoleColor : ConsoleColor.DarkGreen,

                ProgressCharacter = '─',

                CollapseWhenFinished = true
            };

            string filePath = Path.GetRelativePath(directoryPath, file);

            using ChildProgressBar? childProgressBar = progressBar?.Spawn(0, filePath, childOptions);

            try
            {
                string source = await File.ReadAllTextAsync(file);

                string code = await TranslateAsync(source, childProgressBar, statistics);

                if (code != source)
                {
                    await File.WriteAllTextAsync(file, code);

                    translated++;
                }

                progressBar?.Tick();

                processed++;
            }
            catch (Exception e)
            {
                childOptions.ForegroundColor = noColor ? consoleColor : ConsoleColor.DarkRed;

                if(childProgressBar != null) childProgressBar.MaxTicks = childProgressBar.CurrentTick + 1;

                childProgressBar?.Tick();

                progressBar?.Tick();

                progressBar?.WriteErrorLine($"{filePath}: {e.Message}");

                failed++;
            }
        }

        Action<string> writeLine = progressBar != null ? progressBar.WriteLine : Console.WriteLine;

        writeLine("Translation completed!");
        writeLine($"Processed source files: {processed}");
        writeLine($"Translated source files: {translated}");
        writeLine($"Failed source files: {failed}");
        writeLine($"""Characters sent to "{translator.Name}": {statistics.Characters}""");
    }

    private async Task<string> TranslateAsync(string codeText, ProgressBarBase? progressBar, Statistics statistics)
    {
        StringBuilder code = new(codeText);

        SyntaxTree tree = CSharpSyntaxTree.ParseText(code.ToString(), new CSharpParseOptions(preprocessorSymbols: options.PreprocessorSymbols));

        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        List<SyntaxNode> nodes =  options.NoStrings ? [] : (await tree.GetRootAsync()).DescendantNodes()

            .Where(node => node is InterpolatedStringExpressionSyntax or LiteralExpressionSyntax
            {
                Token.RawKind: (int)SyntaxKind.StringLiteralToken or (int)SyntaxKind.SingleLineRawStringLiteralToken or (int)SyntaxKind.MultiLineRawStringLiteralToken

            }).ToList();

        List<SyntaxTrivia> trivia = root.DescendantTrivia()

            .Where(trivia => (!options.NoComments && trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)) ||

                             (!options.NoComments && trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)) ||

                             (!options.NoDocs && trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)) ||

                             (!options.NoDocs && trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))).ToList();

        if (trivia.Count == 0 && nodes.Count == 0)
        {
            if (progressBar != null) progressBar.MaxTicks = 1;

            progressBar?.Tick();

            return codeText;
        }

        if (progressBar?.MaxTicks == 0)
        {
            progressBar.MaxTicks = trivia.Count + nodes.Count;
        }

        foreach (SyntaxTrivia syntaxTrivia in trivia)
        {
            string codeSegment = syntaxTrivia.ToFullString();

            bool shouldTranslate = _shouldTranslate(codeSegment);

            if (shouldTranslate && syntaxTrivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
            {
                MatchCollection matches = DocumentationCommentLine().Matches(codeSegment);

                List<string> outputs = [];

                foreach (Match match in matches.Cast<Match>())
                {
                    string text = match.Groups["content"].Value;

                    string translation = _shouldTranslate(text)

                        ? await TranslateAsync(text, statistics)

                        : text;

                    outputs.Add($"{match.Groups["space"]}///{match.Groups["between"]}{translation}{match.Groups["end"]}");
                }

                string result = string.Join("", outputs);
                
                progressBar?.Tick();

                if (ReplaceCode(code, syntaxTrivia.FullSpan, codeSegment, result))
                {
                    return await TranslateAsync(code.ToString(), progressBar, statistics);
                }
            }
            else if (shouldTranslate && syntaxTrivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                Match match = SingleLineComment().Match(codeSegment);

                string text = match.Groups["content"].Value;

                string translation = _shouldTranslate(codeSegment)

                    ? await TranslateAsync(text, statistics)

                    : text;

                string result = $"{match.Groups["space"]}{match.Groups["between"]}{translation}";

                progressBar?.Tick();

                if (ReplaceCode(code, syntaxTrivia.FullSpan, codeSegment, result))
                {
                    return await TranslateAsync(code.ToString(), progressBar, statistics);
                }
            }
            else if (shouldTranslate && (syntaxTrivia.IsKind(SyntaxKind.MultiLineCommentTrivia) || syntaxTrivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)))
            {
                Match whole = WholeMultiLineComment().Match(codeSegment);

                MatchCollection matches = MultiLineCommentLine().Matches(whole.Groups["content"].Value);

                List<string> outputs = [];

                foreach (Match match in matches.Cast<Match>())
                {
                    string text = match.Groups["content"].Value;

                    string translation = _shouldTranslate(text)

                        ? await TranslateAsync(text, statistics)

                        : text;

                    outputs.Add($"{match.Groups["start"]}{translation}{match.Groups["end"]}");
                }

                string result = $"{whole.Groups["start"]}/*{whole.Groups["comment"]}{string.Join("", outputs)}{whole.Groups["end"]}*/";

                progressBar?.Tick();

                if (ReplaceCode(code, syntaxTrivia.FullSpan, codeSegment, result))
                {
                    return await TranslateAsync(code.ToString(), progressBar, statistics);
                }
            }
            else
            {
                progressBar?.Tick();
            }
        }

        foreach (SyntaxNode syntaxNode in nodes)
        {
            string codeSegment = syntaxNode.ToFullString();

            if (_shouldTranslate(codeSegment))
            {
                Match whole = StringToken().Match(codeSegment);

                MatchCollection matches = StringLine().Matches(whole.Groups["content"].Value);

                List<string> outputs = [];

                foreach (Match match in matches.Cast<Match>())
                {
                    string text = match.Groups["content"].Value;

                    string translation = _shouldTranslate(text)

                        ? await TranslateAsync(text, statistics)

                        : text;

                    outputs.Add($"{match.Groups["start"]}{translation}{match.Groups["end"]}");
                }

                string result = $"{whole.Groups["start"]}{string.Join("", outputs)}{whole.Groups["end"]}";

                progressBar?.Tick();

                if (ReplaceCode(code, syntaxNode.FullSpan, codeSegment, result))
                {
                    return await TranslateAsync(code.ToString(), progressBar, statistics);
                }
            }
            else
            {
                progressBar?.Tick();
            }
        }

        return code.ToString();
    }

    private static bool ReplaceCode(StringBuilder stringBuilder, TextSpan textSpan, string source, string result)
    {
        if (source == result)
        {
            return false;
        }

        stringBuilder.Remove(textSpan.Start, textSpan.Length).Insert(textSpan.Start, result);

        return true;
    }

    private async Task<string> TranslateAsync(string text, Statistics statistics)
    {
        return (await _translationsCache.GetOrCreateAsync(text, async entry =>
        {
            entry.AbsoluteExpiration = DateTimeOffset.MaxValue;

            statistics.Characters += text.Length;

            return CapitalizeFirstLetter(text, await translator.TranslateAsync(text), options.CapitalizeFirstLetter);

        }))!;
    }

    private static string CapitalizeFirstLetter(string source, string translated, bool capitalize)
    {
        if (!capitalize || string.IsNullOrEmpty(translated)) return translated;

        if (translated.Length >= 1 && (source.Length < 1 || source[0] != translated[0]) && char.IsLetter(translated[0]) && !char.IsUpper(translated[0]))
        {
            return char.ToUpper(translated[0]) + translated[1..];
        }
        else
        {
            return translated;
        }
    }

    [GeneratedRegex(@"(?<start>\s*)/\*(?<comment>\*?)(?<content>.*?)(?<end>\s*)\*/\s*", RegexOptions.Singleline)]
    private static partial Regex WholeMultiLineComment();

    [GeneratedRegex(@"^(?<start>\s*\*?\s*)(?<content>.*?)(?<end>\s*?)$", RegexOptions.Multiline)]
    private static partial Regex MultiLineCommentLine();

    [GeneratedRegex(@"^(?<space>\s*)(?<between>//\s*)(?<content>.*?)\s*$", RegexOptions.Multiline)]
    private static partial Regex SingleLineComment();

    [GeneratedRegex(@"^(?<space>\s*)///(?<between>\s*)(?<content>.*?)(?<end>\s*\n)", RegexOptions.Multiline)]
    private static partial Regex DocumentationCommentLine();

    [GeneratedRegex("""^(?<start>\s*[@$]*"+\s*)(?<content>.*?)(?<end>\s*"+\s*)$""", RegexOptions.Singleline)]
    private static partial Regex StringToken();

    [GeneratedRegex(@"^(?<start>\s*)(?<content>.*)(?<end>\n?)", RegexOptions.Multiline)]
    private static partial Regex StringLine();
}