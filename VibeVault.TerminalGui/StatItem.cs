namespace Tessera.Controls;

internal readonly record struct StatItem(string Key, string Value)
{
    public override string ToString() => $"{Key,-11} {Value}";
}
