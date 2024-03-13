using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TranSoon;

internal partial class Analyzer(Regex translatable, ITranslator translator, bool capitalizeFirstLetter)
{
    private readonly Func<string, bool> _shouldTranslate = translatable.IsMatch;

    public async Task TranslateComments(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine($"Folder {folderPath} does not exist.");
            return;
        }

        string[] csFiles = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);

        foreach (string file in csFiles)
        {
            string code = await File.ReadAllTextAsync(file);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            List<SyntaxTrivia> nodes = root.DescendantTrivia()
                .Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                                 trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                                 trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)).ToList();

            if (nodes.Count == 0)
            {
                Console.WriteLine($"No comments found in {file}");
                continue;
            }

            foreach (SyntaxTrivia node in nodes.Where(node => _shouldTranslate(node.ToFullString())))
            {
                if (node.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                {
                    string documentation = node.ToFullString();

                    MatchCollection matches = DocumentationCommentLine().Matches(documentation);

                    List<string> outputs = [];

                    foreach (Match match in matches.Cast<Match>())
                    {
                        string text = match.Groups["content"].Value;

                        string translation = _shouldTranslate(text) ? CapitalizeFirstLetter(await translator.TranslateAsync(text), capitalizeFirstLetter) : text;

                        outputs.Add($"{match.Groups["space"]}///{match.Groups["between"]}{translation}{match.Groups["end"]}");
                    }

                    string result = string.Join("", outputs);

                    code = code.Replace(documentation, result);
                }
                else if (node.IsKind(SyntaxKind.SingleLineCommentTrivia))
                {
                    string comment = node.ToFullString();

                    Match match = SingleLineComment().Match(comment);

                    string content = match.Groups["content"].Value;

                    string translation = _shouldTranslate(comment) ? CapitalizeFirstLetter(await translator.TranslateAsync(content), capitalizeFirstLetter) : content;

                    string result = $"{match.Groups["space"]}{match.Groups["between"]}{translation}";

                    code = code.Replace(comment, result);
                }
                else if (node.IsKind(SyntaxKind.MultiLineCommentTrivia))
                {
                    string comment = node.ToFullString();

                    Match whole = WholeMultiLineComment().Match(comment);

                    MatchCollection matches = MultiLineCommentLine().Matches(whole.Groups["content"].Value);

                    List<string> outputs = [];

                    foreach (Match match in matches.Cast<Match>())
                    {
                        string text = match.Groups["content"].Value;

                        string translation = _shouldTranslate(text) ? CapitalizeFirstLetter(await translator.TranslateAsync(text), capitalizeFirstLetter) : text;

                        outputs.Add($"{match.Groups["space"]}{translation}");
                    }

                    string result = $"{whole.Groups["start"]}/*{string.Join("", outputs)}{whole.Groups["end"]}*/";

                    code = code.Replace(comment, result);
                }
            }

            await File.WriteAllTextAsync(file, code);

            Console.WriteLine($"Comments in {file} translated and saved.");
        }

        Console.WriteLine("Translation completed.");
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
}