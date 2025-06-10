// N8nWorkflowGenerator/Helpers.cs
using System;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text; // Required for StringBuilder

namespace N8nWorkflowGenerator;

public static class Helpers
{
    public static string GenerateUuid()
    {
        return Guid.NewGuid().ToString();
    }

    public static string Slugify(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        // Remove diacritics
        string decomposed = name.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        string noDiacritics = sb.ToString().Normalize(NormalizationForm.FormC);

        // Replace non-alphanumeric with hyphen, convert to lowercase
        string slug = Regex.Replace(noDiacritics, @"[^a-z0-9\s-]", "", RegexOptions.IgnoreCase).Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"\s+", "-"); // Replace spaces with hyphens
        slug = Regex.Replace(slug, @"-+", "-");    // Replace multiple hyphens with single hyphen

        return slug;
    }
}
