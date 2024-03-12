using DeepL;
using DeepL.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TranSoon;

internal class Analyzer
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

        Translator client = new (apiKey);

        foreach (string file in csFiles)
        {
            string code = await File.ReadAllTextAsync(file);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            List<SyntaxTrivia> commentNodes = root.DescendantTrivia()
                .Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                                 trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                                 trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)).ToList();

            if (commentNodes.Count == 0)
            {
                Console.WriteLine($"No comments found in {file}");
                continue;
            }

            foreach (SyntaxTrivia commentNode in commentNodes)
            {
                SyntaxToken commentNodeToken = commentNode.Token;
                /*string commentText = commentNode.ToString();
                TextResult response = await client.TranslateTextAsync(commentText, null, options.Language);
                string translatedComment = response.Text;
                code = code.Replace(commentText, translatedComment);
                Thread.Sleep(1000);*/
            }

            //await File.WriteAllTextAsync(file, code);
            Console.WriteLine($"Comments in {file} translated and saved.");
        }

        Console.WriteLine("Translation completed.");
    }
}