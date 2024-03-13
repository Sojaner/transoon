namespace TranSoon
{
    internal interface ITranslator
    {
        Task<string> TranslateAsync(string text);
    }
}
