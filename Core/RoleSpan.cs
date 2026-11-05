namespace UntranslatedStardew.Core;

/// <summary>
/// A character range with an explicit language font role.
/// </summary>
public readonly record struct RoleSpan(int Start, int Length, FontRole Role)
{
    public int End => Start + Length;

    public bool ContainsIndex(int index) => index >= Start && index < End;
}
