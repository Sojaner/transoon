namespace TranSooner
{
    internal interface ITranslator
    {
        string Name { get; }

        Task<string> TranslateAsync(string text);
    }
}
