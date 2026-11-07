using UntranslatedStardew.Core;

namespace UntranslatedStardew.Tests;

public static partial class Program
{
    private static void TestProviderWarningLifecycle()
    {
        var registry = new LanguageSourceRegistry();
        var warnings = new List<(string Language, string Mode)>();
        using var provider = new RawContentProvider(() => null, Path.GetTempPath(), "vi",
            onFallbackWarning: (language, mode) => warnings.Add((language, mode)), sourceRegistry: registry);
        Assert(warnings.Count == 1 && !provider.IsSecondaryAvailable,
            "Unavailable custom language still reports an initial primary-only warning");
        provider.Rebind();
        provider.Rebind("VI");
        using var unrelated = registry.Register(new LanguageSourceDescriptor { LanguageCode = "eo", SourceModId = "tests.eo",
            Strings = new() { ["Strings/UI:Hello"] = "Saluton" } });
        Assert(warnings.Count == 1 && !provider.IsSecondaryAvailable,
            "Repeated rebinding and unrelated locale registrations do not repeat the same fallback warning");
        var source = registry.Register(new LanguageSourceDescriptor { LanguageCode = "vi", SourceModId = "tests.vi",
            Strings = new() { ["Characters/Dialogue/Alex:Wed_01_01"] = "Thật sao?" } });
        Assert(provider.IsSecondaryAvailable && provider.TryGetDialogueString("Characters/Dialogue/Alex", "Wed_01_01", out string? text)
            && text == "Thật sao?" && warnings.Count == 1,
            "A late source registration immediately restores translated dialogue without recreating the provider");
        source.Dispose();
        provider.Rebind();
        Assert(!provider.IsSecondaryAvailable && warnings.Count == 2,
            "Losing a previously available source emits a fresh warning once");
        var empty = registry.Register(new LanguageSourceDescriptor { LanguageCode = "vi", SourceModId = "tests.empty" });
        provider.Rebind();
        Assert(warnings.Count == 3 && warnings[^1].Mode.Contains("missing content"),
            "A genuinely different fallback reason is reported without repeated identical warnings");
        empty.Dispose();
        provider.Rebind("eo");
        provider.Rebind("vi");
        Assert(warnings.Count == 5 && !provider.IsSecondaryAvailable,
            "Changing through an available locale resets warning suppression for a later failure");
        provider.Dispose();
        using var afterDispose = registry.Register(new LanguageSourceDescriptor { LanguageCode = "vi", SourceModId = "tests.after",
            Strings = new() { ["Strings/UI:Hello"] = "Xin chào" } });
        Assert(warnings.Count == 5, "Disposed providers no longer react to registry notifications");
    }
}
