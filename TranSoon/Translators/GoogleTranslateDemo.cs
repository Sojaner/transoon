using System.Globalization;
using Newtonsoft.Json.Linq;

namespace TranSoon.Translators
{
    internal class GoogleTranslateDemo(CultureInfo targetCultureInfo, CultureInfo? sourceCultureInfo = null): ITranslator
    {
        private readonly HttpClient _client = new();

        public async Task<string> TranslateAsync(string text)
        {
            string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceCultureInfo?.TwoLetterISOLanguageName ?? "auto"}&tl={targetCultureInfo.TwoLetterISOLanguageName}&dt=t&q={Uri.EscapeDataString(text)}";

            HttpResponseMessage result = await _client.GetAsync(url);

            if (!result.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to translate text: {result.ReasonPhrase}");
            }

            JArray array = JArray.Parse(await result.Content.ReadAsStringAsync());

            return ((JValue)array.First!.First!.First!).Value!.ToString()!;
        }
    }
}
