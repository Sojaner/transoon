using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.FileSystemGlobbing;
using ShellProgressBar;

namespace TranSoon;

internal partial class Analyzer(Regex translatable, ITranslator translator, bool capitalizeFirstLetter, IEnumerable<string> preprocessorSymbols)
{
    private readonly Func<string, bool> _shouldTranslate = translatable.IsMatch;

    public async Task TranslateComments(string directoryPath, Matcher matcher)
    {
        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"Folder {directoryPath} does not exist.");

            return;
        }

        string[] csFiles = matcher.GetResultsInFullPath(directoryPath).ToArray();

        int totalTicks = csFiles.Length;

        ProgressBarOptions options = new ()
        {
            ForegroundColor = ConsoleColor.Yellow,

            BackgroundColor = ConsoleColor.DarkYellow,

            ProgressCharacter = '─',

            CollapseWhenFinished = false
        };

        ProgressBar progressBar = new(totalTicks, directoryPath, options);

        foreach (string file in csFiles)
        {
            ProgressBarOptions childOptions = new()
            {
                ForegroundColor = ConsoleColor.Green,

                BackgroundColor = ConsoleColor.DarkGreen,

                ProgressCharacter = '─',

                CollapseWhenFinished = true
            };

            string filePath = Path.GetRelativePath(directoryPath, file);

            ChildProgressBar childProgressBar = progressBar.Spawn(0, filePath, childOptions);

            try
            {
                string source = await File.ReadAllTextAsync(file);

                string code = await Translate(source, childProgressBar);

                if (code != source)
                {
                    await File.WriteAllTextAsync(file, code);
                }

                progressBar.Tick();
            }
            catch (Exception e)
            {
                childOptions.ForegroundColor = ConsoleColor.DarkRed;

                childOptions.CollapseWhenFinished = false;

                childProgressBar.Tick($"{filePath}: {e.Message}");

                progressBar.Tick();
            }
        }
    }

    private async Task<string> Translate(string codeText, ProgressBarBase progressBar)
    {
        StringBuilder code = new(codeText);

        SyntaxTree tree = CSharpSyntaxTree.ParseText(code.ToString(), new CSharpParseOptions(preprocessorSymbols: preprocessorSymbols));

        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        List<SyntaxNode> nodes = (await tree.GetRootAsync()).DescendantNodes()

            .Where(node => node is InterpolatedStringExpressionSyntax or LiteralExpressionSyntax
            {
                Token.RawKind: (int)SyntaxKind.StringLiteralToken or (int)SyntaxKind.SingleLineRawStringLiteralToken or (int)SyntaxKind.MultiLineRawStringLiteralToken

            }).ToList();

        List<SyntaxTrivia> trivia = root.DescendantTrivia()

            .Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||

                             trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||

                             trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)).ToList();

        if (trivia.Count == 0 && nodes.Count == 0)
        {
            progressBar.MaxTicks = 1;

            progressBar.Tick();

            return codeText;
        }

        if (progressBar.MaxTicks == 0)
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

                        ? CapitalizeFirstLetter(await translator.TranslateAsync(text), capitalizeFirstLetter)

                        : text;

                    outputs.Add($"{match.Groups["space"]}///{match.Groups["between"]}{translation}{match.Groups["end"]}");
                }

                string result = string.Join("", outputs);
                
                progressBar.Tick();

                if (ReplaceCode(code, syntaxTrivia.FullSpan, codeSegment, result))
                {
                    return await Translate(code.ToString(), progressBar);
                }
            }
            else if (shouldTranslate && syntaxTrivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                Match match = SingleLineComment().Match(codeSegment);

                string content = match.Groups["content"].Value;

                string translation = _shouldTranslate(codeSegment)

                    ? CapitalizeFirstLetter(await translator.TranslateAsync(content), capitalizeFirstLetter)

                    : content;

                string result = $"{match.Groups["space"]}{match.Groups["between"]}{translation}";

                progressBar.Tick();

                if (ReplaceCode(code, syntaxTrivia.FullSpan, codeSegment, result))
                {
                    return await Translate(code.ToString(), progressBar);
                }
            }
            else if (shouldTranslate && syntaxTrivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
            {
                Match whole = WholeMultiLineComment().Match(codeSegment);

                MatchCollection matches = MultiLineCommentLine().Matches(whole.Groups["content"].Value);

                List<string> outputs = [];

                foreach (Match match in matches.Cast<Match>())
                {
                    string text = match.Groups["content"].Value;

                    string translation = _shouldTranslate(text)

                        ? CapitalizeFirstLetter(await translator.TranslateAsync(text), capitalizeFirstLetter)

                        : text;

                    outputs.Add($"{match.Groups["space"]}{translation}");
                }

                string result = $"{whole.Groups["start"]}/*{string.Join("", outputs)}{whole.Groups["end"]}*/";

                progressBar.Tick();

                if (ReplaceCode(code, syntaxTrivia.FullSpan, codeSegment, result))
                {
                    return await Translate(code.ToString(), progressBar);
                }
            }
            else
            {
                progressBar.Tick();
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
                    string content = match.Groups["content"].Value;

                    string translation = _shouldTranslate(content)

                        ? CapitalizeFirstLetter(await translator.TranslateAsync(content), capitalizeFirstLetter)

                        : content;

                    outputs.Add($"{match.Groups["start"]}{translation}{match.Groups["end"]}");
                }

                string result = $"{whole.Groups["start"]}{string.Join("", outputs)}{whole.Groups["end"]}";

                progressBar.Tick();

                if (ReplaceCode(code, syntaxNode.FullSpan, codeSegment, result))
                {
                    return await Translate(code.ToString(), progressBar);
                }
            }
            else
            {
                progressBar.Tick();
            }
        }

        return code.ToString();

        static bool ReplaceCode(StringBuilder stringBuilder, TextSpan textSpan, string source, string result)
        {
            if (source == result)
            {
                return false;
            }

            stringBuilder.Remove(textSpan.Start, textSpan.Length)

                .Insert(textSpan.Start, result);

            return true;
        }
    }

    private static string CapitalizeFirstLetter(string input, bool capitalize)
    {
        if (!capitalize || string.IsNullOrEmpty(input)) return input;

        if (char.IsLetter(input[0]) && !char.IsUpper(input[0]) && IsLatinLetter(input[0]) && input.Length >= 1)
        {
            return char.ToUpper(input[0]) + input[1..];
        }
        else
        {
            return input;
        }
    }

    private static bool IsLatinLetter(char c)
    {
        return c is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    [GeneratedRegex(@"(?<start>\s*)/\*(?<content>.*?)(?<end>\s*)\*/\s*", RegexOptions.Multiline)]
    private static partial Regex WholeMultiLineComment();

    [GeneratedRegex(@"^(?<space>\s*\*?\s*)(?<content>.*?)\s*?$")]
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