using System.Text.RegularExpressions;
using GTranslatorAPI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TranSoon;

internal partial class Analyzer
{
    public static async Task TranslateComments(Options options)
    {
        string folderPath = options.DirectoryPath;
        string apiKey = options.ApiKey;

        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine($"Folder {folderPath} does not exist.");
            return;
        }

        string[] csFiles = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);

        GTranslatorAPIClient client = new ();

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

            foreach (SyntaxTrivia node in nodes.Where(node => MatchFunc(node.ToFullString(), options)))
            {
                if (node.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                {
                    string documentation = node.ToFullString();

                    MatchCollection matches = DocumentationCommentLine().Matches(documentation);

                    List<string> outputs = [];

                    foreach (Match match in matches.Cast<Match>())
                    {
                        string text = match.Groups["content"].Value;

                        string translation = MatchFunc(text, options) ? CapitalizeFirstLetter(await TranslateTextAsync(text, client, options.Language)) : text;

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

                    string translation = MatchFunc(comment, options) ? CapitalizeFirstLetter(await TranslateTextAsync(content, client, options.Language)) : content;

                    string result = $"{match.Groups["space"]}//{match.Groups["between"]}{translation}";

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

                        string translation = MatchFunc(text, options) ? CapitalizeFirstLetter(await TranslateTextAsync(text, client, options.Language)) : text;

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

    private static async Task<string> TranslateTextAsync(string text, GTranslatorAPIClient client, string language)
    {
        Translation response = await client.TranslateAsync(Languages.zh_CN, Languages.en, text);

        return response.TranslatedText;
    }

    private static bool MatchFunc(string text, Options options) => string.IsNullOrWhiteSpace(options.RegexPattern) || Regex.IsMatch(text, options.RegexPattern);

    private static string CapitalizeFirstLetter(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Check if the first character is a Latin letter and not already capitalized
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
        // Latin alphabet ranges in Unicode
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