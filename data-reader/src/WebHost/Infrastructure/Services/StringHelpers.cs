public static class StringHelpers
{
    public static string ClearFromTrashSymbols(this string str)
    {
        var clearedStrTokens = str
                .Replace("&nbsp", string.Empty)
                .Replace(";", string.Empty)
                .Replace("\n", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var clearedStr =   String.Join(' ', clearedStrTokens);

        return clearedStr;
    }
}

