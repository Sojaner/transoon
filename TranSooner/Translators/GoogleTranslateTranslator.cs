using System.Globalization;

namespace TranSooner.Translators;

public class GoogleTranslateTranslator : ITranslator
{
    public GoogleTranslateTranslator(CultureInfo cultureInfo, string optionsApiKey)
    {
        throw new NotImplementedException();
    }

    public Task<string> TranslateAsync(string text)
    {
        throw new NotImplementedException();
    }
}