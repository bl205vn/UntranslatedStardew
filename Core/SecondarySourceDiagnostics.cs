namespace UntranslatedStardew.Core;

/// <summary>Defers startup fallback diagnostics until custom language discovery has finished.</summary>
internal sealed class SecondarySourceDiagnostics
{
    private readonly Action<string, string> _warn;
    private (string Language, string Mode)? _pending;
    private bool _ready;

    internal SecondarySourceDiagnostics(Action<string, string> warn) => _warn = warn;

    internal void Report(string language, string mode)
    {
        if (!_ready) _pending = (language, mode);
        else _warn(language, mode);
    }

    internal void CompleteInitialization(string language, bool available)
    {
        if (_ready) return;
        _ready = true;
        var pending = _pending;
        _pending = null;
        if (!available && pending is { } failure
            && LanguageHelper.CanonicalizeLanguageCode(failure.Language).Equals(
                LanguageHelper.CanonicalizeLanguageCode(language), StringComparison.OrdinalIgnoreCase))
            _warn(failure.Language, failure.Mode);
    }
}
