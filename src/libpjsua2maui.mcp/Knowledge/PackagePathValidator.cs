namespace libpjsua2maui.mcp.Knowledge;

internal static class PackagePathValidator
{
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value[0] is '/' or '\\' || value.Contains('\\'))
        {
            return false;
        }

        if (value.Length >= 2 && char.IsAsciiLetter(value[0]) && value[1] == ':')
        {
            return false;
        }

        return value.Split('/').All(segment => segment is not ("" or "." or ".."));
    }
}
