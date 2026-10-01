using System.Text;

namespace ToJePrivela.Domain.Common;

/// <summary>
/// The form in which two names count as the same: trimmed, composed (NFC) and lower-cased by Unicode rules, so
/// "Štefan" and "štefan" collide while "Štefan" and "Stefan" do not. SQLite's NOCASE folds ASCII letters only,
/// which is why entities store this key and the database keeps it unique.
/// </summary>
public static class NameKeys
{
    public static string Of(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return name.Trim().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
