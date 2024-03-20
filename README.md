## ***Tran-sooner*** (**sooner** rather than ~~later~~), for C# source code documentation, comments, and strings
**TranSooner** is a **.NET Global Tool** for translating all the ***documentation***, ***comments***, and ***strings*** in a **C#** codebase.

### Installation
```bash
dotnet tool install -g transooner
```
- This tool relies on .NET 8.0

### Usage example
```bash
transooner -d /Users/sojaner/Git/GitHub/DotnetSpider -k AIzaSyD-7kgBVqzyJb0e6k3yvh1PEw0F42xS4T8 -t google -s Debug
```
- This tool was originally created for translating the codebase of **DotnetSpider** library from [Chinese](https://github.com/dotnetcore/DotnetSpider) to [English](https://github.com/Sojaner/DotnetSpider/tree/en-updated).

### Usage options
| Option              | Description                                                                                                  | Default                    |
|---------------------|--------------------------------------------------------------------------------------------------------------|----------------------------|
| -d, --directory     | Directory containing .cs files.                                                                              | Current working directory |
| -k, --api-key       | Google Translate API Key.                                                                                    |                            |
| -t, --translator    | Translator to use (google, deepl).                                                                           | google                     |
| -l, --language      | Language to translate to.                                                                                    | en-US                      |
| -r, --regex         | Regex pattern to match comments to be translated.                                                            | [^\x00-\x7F]               |
| -i, --includes      | Glob pattern to include files.                                                                               | **/*.cs                   |
| -e, --excludes      | Glob pattern to exclude files.                                                                               | bin/*.* obj/*.*            |
| -c, --capitalize    | Capitalize first letter of translated comments.                                                               | true                       |
| -y, --yes           | Acknowledge and disable the "Google's free Translation API" usage warning by implicitly answering "yes".     | false                      |
| -s, --symbols       | Preprocessor symbols to use with the C# parser for translating conditional code parts.                       | no symbols                 |
| -n, --no-logo       | Skip printing the logo.                                                                                      | false                      |
| --no-comments       | Skip translating comments.                                                                                   | false                      |
| --no-strings        | Skip translating strings.                                                                                    | false                      |
| --no-documentation  | Skip translating documentation comments.                                                                     | false                      |
| --no-progress       | Skip printing progress.                                                                                      | false                      |
| --help              | Display this help screen.                                                                                    |                            |
| --version           | Display version information.                                                                                 |                            |
