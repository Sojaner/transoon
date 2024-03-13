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

            ChildProgressBar childProgressBar = progressBar.Spawn(1, filePath, childOptions);

            try
            {
                string source = await File.ReadAllTextAsync(file);

                string code = await Translate(source);

                if (code != source)
                {
                    await File.WriteAllTextAsync(file, code);
                }

                childProgressBar.Tick();

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

    private async Task<string> Translate(string codeText)
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
            return codeText;
        }

        foreach (SyntaxTrivia syntaxTrivia in trivia.Where(node => _shouldTranslate(node.ToFullString())))
        {
            if (syntaxTrivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
            {
                string documentation = syntaxTrivia.ToFullString();

                MatchCollection matches = DocumentationCommentLine().Matches(documentation);

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

                if (ReplaceCode(code, syntaxTrivia.FullSpan, documentation, result))
                {
                    return await Translate(code.ToString());
                }
            }
            else if (syntaxTrivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                string comment = syntaxTrivia.ToFullString();

                Match match = SingleLineComment().Match(comment);

                string content = match.Groups["content"].Value;

                string translation = _shouldTranslate(comment)

                    ? CapitalizeFirstLetter(await translator.TranslateAsync(content), capitalizeFirstLetter)

                    : content;

                string result = $"{match.Groups["space"]}{match.Groups["between"]}{translation}";

                if (ReplaceCode(code, syntaxTrivia.FullSpan, comment, result))
                {
                    return await Translate(code.ToString());
                }
            }
            else if (syntaxTrivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
            {
                string comment = syntaxTrivia.ToFullString();

                Match whole = WholeMultiLineComment().Match(comment);

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

                if (ReplaceCode(code, syntaxTrivia.FullSpan, comment, result))
                {
                    return await Translate(code.ToString());
                }
            }
        }

        foreach (SyntaxNode syntaxNode in nodes)
        {
            string text = syntaxNode.ToFullString();

            Match whole = StringToken().Match(text);

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

            if (ReplaceCode(code, syntaxNode.FullSpan, text, result))
            {
                return await Translate(code.ToString());
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