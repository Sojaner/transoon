using System.Globalization;
using Google.Cloud.Translation.V2;

namespace TranSooner.Translators;

public class GoogleTranslateTranslator(CultureInfo targetCultureInfo, string apiKey, CultureInfo? sourceCultureInfo = null) : ITranslator
{
    private readonly TranslationClient _client = TranslationClient.CreateFromApiKey(apiKey);

    public async Task<string> TranslateAsync(string text)
    {
        return (await _client.TranslateTextAsync(text, targetCultureInfo.TwoLetterISOLanguageName,

            sourceCultureInfo?.TwoLetterISOLanguageName)).TranslatedText;
    }
}