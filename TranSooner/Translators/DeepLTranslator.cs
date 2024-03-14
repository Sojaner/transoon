using System.Globalization;
using DeepL;

namespace TranSooner.Translators;

public class DeepLTranslator(CultureInfo targetCultureInfo, string apiKey, CultureInfo? sourceCultureInfo = null) : ITranslator
{
    private readonly Translator _client = new(apiKey);

    public async Task<string> TranslateAsync(string text)
    {
        return (await _client.TranslateTextAsync(text, sourceCultureInfo?.Name, targetCultureInfo.Name)).Text;
    }
}