## ***Tran-sooner*** (**sooner** rather than ~~later~~), for C# source code documentation, comments, and strings
**TranSooner** is a **.NET Global Tool** for translating all the ***documentation***, ***comments***, and ***strings*** in a **C#** codebase.
- This tool was originally created for translating the codebase of **DotnetSpider** library from [Chinese](https://github.com/dotnetcore/DotnetSpider) to [English](https://github.com/Sojaner/DotnetSpider/tree/en-updated).

### Installation
```bash
dotnet tool install -g transooner
```
- This tool relies on .NET 8.0

### Usage example
```bash
transooner -d /Users/sojaner/Git/GitHub/DotnetSpider -k AIzaSyD-7kgBVqzyJb0e6k3yvh1PEw0F42xS4T8 -t google -s Debug #The API key here is a dummy API key
```
- **Important note:** Using ***Google Translate*** without an API Key will switch to ***Google's free Translation API** that is provided only for ***demo purposes*** and should not be used in commercial and production environments.

### Usage options
| Option              | Description                                                                                                  | Default                    | Optional |
|---------------------|--------------------------------------------------------------------------------------------------------------|----------------------------|----------|
| -d, --directory     | Directory containing .cs files.                                                                              | Current working directory  | true     |
| -k, --api-key       | Translate API Key.                                                                                           |                            | true     |
| -t, --translator    | Translator to use (google, deepl).                                                                           | google                     | true     |
| -l, --language      | Language to translate to.                                                                                    | en-US                      | true     |
| -r, --regex         | Regex pattern to match comments to be translated.                                                            | [^\x00-\x7F]               | true     |
| -i, --includes      | Glob pattern to include files.                                                                               | **/*.cs                    | true     |
| -e, --excludes      | Glob pattern to exclude files.                                                                               | bin/*.* obj/*.*            | true     |
| -c, --capitalize    | Capitalize first letter of translated comments.                                                              | true                       | true     |
| -y, --yes           | Acknowledge and disable the "Google's free Translation API" usage warning by implicitly answering "yes".     | false                      | true     |
| -s, --symbols       | Preprocessor symbols to use with the C# parser for translating conditional code parts.                       | no symbols                 | true     |
| -n, --no-logo       | Skip printing the logo.                                                                                      | false                      | true     |
| --no-comments       | Skip translating comments.                                                                                   | false                      | true     |
| --no-strings        | Skip translating strings.                                                                                    | false                      | true     |
| --no-documentation  | Skip translating documentation comments.                                                                     | false                      | true     |
| --no-progress       | Skip printing progress.                                                                                      | false                      | true     |
| --help              | Display this help screen.                                                                                    |                            |          |
| --version           | Display version information.                                                                                 |                            |          |
