using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Cg.ProjectName.Domain.Extensions;

public static class StringExtension
{
    public static string ToUrl(this String text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // 1. Minúsculo
        text = text.ToLowerInvariant();

        // 2. Remover acentos
        text = RemoveDiacritics(text);

        // 3. Remover caracteres inválidos (mantém letras, números e espaços)
        text = Regex.Replace(text, @"[^a-z0-9\s-]", "");

        // 4. Substituir espaços por hífen
        text = Regex.Replace(text, @"\s+", "-");

        // 5. Remover múltiplos hífens
        text = Regex.Replace(text, @"-+", "-");

        // 6. Remover hífen do início/fim
        text = text.Trim('-');

        return text;
    }

    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value);
    }

    public static bool IsNullOrWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
