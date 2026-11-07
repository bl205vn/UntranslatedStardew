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
    private static void TestStartupSourceDiagnostics()
    {
        var warnings = new List<string>();
        var diagnostics = new SecondarySourceDiagnostics((language, mode) => warnings.Add(language + ":" + mode));
        diagnostics.Report("vi", "unregistered");
        diagnostics.Report("vi", "unregistered");
        Assert(warnings.Count == 0, "Startup waits for source discovery before warning about a provisional missing locale");
        diagnostics.CompleteInitialization("vi", true);
        Assert(warnings.Count == 0, "A discovered usable source cancels provisional startup warnings");
        diagnostics.Report("vi", "removed");
        Assert(warnings.Count == 1, "Real source loss after startup is still reported immediately");
        diagnostics.CompleteInitialization("vi", false);
        Assert(warnings.Count == 1, "Completing initialization twice cannot replay a provisional warning");

        warnings.Clear();
        var missing = new SecondarySourceDiagnostics((language, mode) => warnings.Add(language + ":" + mode));
        missing.Report("vi", "unregistered");
        missing.Report("VI", "missing content");
        missing.CompleteInitialization("vi", false);
        Assert(warnings.Count == 1 && warnings[0].Contains("missing content"),
            "An unresolved custom source reports the latest actual fallback reason once after discovery");
        warnings.Clear();
        var changed = new SecondarySourceDiagnostics((language, mode) => warnings.Add(mode));
        changed.Report("vi", "unregistered");
        changed.CompleteInitialization("en", false);
        Assert(warnings.Count == 0, "A stale provisional warning for a previous requested locale is discarded");

        var registry = new LanguageSourceRegistry();
        var late = new SecondarySourceDiagnostics((language, mode) => warnings.Add(mode));
        using var provider = new RawContentProvider(() => null, Path.GetTempPath(), "vi",
            onFallbackWarning: late.Report, sourceRegistry: registry);
        var token = registry.Register(new LanguageSourceDescriptor { LanguageCode = "vi", SourceModId = "tests.startup",
            Strings = new() { ["Strings/UI:Hello"] = "Xin chào" } });
        late.CompleteInitialization(provider.RequestedLanguage, provider.IsSecondaryAvailable);
        Assert(warnings.Count == 0 && provider.IsSecondaryAvailable,
            "Real provider rebinding during discovery restores a late source without a false startup warning");
        token.Dispose();
        Assert(warnings.Count == 1 && !provider.IsSecondaryAvailable,
            "The same provider reports genuine source removal after discovery");
    }

}
