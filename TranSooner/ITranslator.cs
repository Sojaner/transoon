namespace TranSooner
{
    internal interface ITranslator
    {
        Task<string> TranslateAsync(string text);
    }
}
