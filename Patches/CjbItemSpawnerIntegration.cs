using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

/// <summary>
/// Dynamic, optional Harmony patch for CJB Item Spawner.
/// Hooks into CJBItemSpawner.Framework.ItemMenu.SearchItems to provide bilingual
/// and diacritic-insensitive search matching without modifying CJB source or requiring
/// a compile-time assembly reference. Preserves all category tabs and sort orders.
/// </summary>
public static class CjbItemSpawnerIntegration
{
    private static bool _initialized;
    private static bool _warnedUnsupportedShape;
    private static bool _loggedFilterError;

    private static FieldInfo? _searchBoxField;
    private static PropertyInfo? _textBoxTextProp;
    private static PropertyInfo? _spawnableItemItemProp;
    private static PropertyInfo? _spawnableItemNameProp;
    private static PropertyInfo? _spawnableItemDisplayNameProp;
    private static PropertyInfo? _spawnableItemIdProp;
    private static PropertyInfo? _spawnableItemQualifiedIdProp;
    private static MethodInfo? _castMethod;

    public static ItemNameResolver? TestResolverOverride { get; set; }
    public static bool WarnedUnsupportedShape => _warnedUnsupportedShape;
    public static bool LoggedFilterError => _loggedFilterError;

    /// <summary>
    /// Checks whether CJB Item Spawner is loaded, validates its internal shape, and applies the Harmony patch.
    /// Safely skips patching with a single warning if the installed version has an unexpected structure.
    /// </summary>
    public static bool TryApply(Harmony harmony, IModRegistry modRegistry, IMonitor monitor)
    {
        if (_initialized)
            return true;

        if (!modRegistry.IsLoaded("CJBok.ItemSpawner"))
            return false;

        Type? itemMenuType = Type.GetType("CJBItemSpawner.Framework.ItemMenu, CJBItemSpawner");
        if (itemMenuType == null)
        {
            Assembly? asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "CJBItemSpawner");
            itemMenuType = asm?.GetType("CJBItemSpawner.Framework.ItemMenu");
        }

        Type? spawnableItemType = Type.GetType("CJBItemSpawner.Framework.SpawnableItem, CJBItemSpawner")
            ?? (itemMenuType != null ? itemMenuType.Assembly.GetType("CJBItemSpawner.Framework.SpawnableItem") : null);

        return TryApplyInternal(harmony, itemMenuType, spawnableItemType, monitor);
    }

    /// <summary>
    /// Internal shape validation and patch application, exposed for unit and integration testing.
    /// </summary>
    public static bool TryApplyInternal(Harmony harmony, Type? itemMenuType, Type? spawnableItemType, IMonitor? monitor = null)
    {
        if (_initialized)
            return true;

        if (itemMenuType == null)
        {
            WarnUnsupportedShape(monitor, "ItemMenu type could not be located.");
            return false;
        }

        MethodInfo? searchItemsMethod = itemMenuType.GetMethod("SearchItems", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (searchItemsMethod == null)
        {
            WarnUnsupportedShape(monitor, "SearchItems method not found on ItemMenu.");
            return false;
        }

        if (!typeof(IEnumerable).IsAssignableFrom(searchItemsMethod.ReturnType))
        {
            WarnUnsupportedShape(monitor, $"SearchItems return type '{searchItemsMethod.ReturnType.FullName}' does not implement IEnumerable.");
            return false;
        }

        if (spawnableItemType == null)
        {
            WarnUnsupportedShape(monitor, "SpawnableItem type could not be located.");
            return false;
        }

        FieldInfo? searchBoxField = itemMenuType.GetField("SearchBox", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (searchBoxField == null)
        {
            WarnUnsupportedShape(monitor, "SearchBox field not found on ItemMenu.");
            return false;
        }

        PropertyInfo? textBoxTextProp = searchBoxField.FieldType.GetProperty("Text", BindingFlags.Instance | BindingFlags.Public);
        if (textBoxTextProp == null || !textBoxTextProp.CanRead || !textBoxTextProp.CanWrite)
        {
            WarnUnsupportedShape(monitor, "SearchBox does not expose a readable and writable Text property.");
            return false;
        }

        PropertyInfo? nameProp = spawnableItemType.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public);
        PropertyInfo? displayNameProp = spawnableItemType.GetProperty("DisplayName", BindingFlags.Instance | BindingFlags.Public);
        if (nameProp == null || displayNameProp == null)
        {
            WarnUnsupportedShape(monitor, "SpawnableItem does not expose expected Name or DisplayName properties.");
            return false;
        }

        MethodInfo? castMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast))?.MakeGenericMethod(spawnableItemType);
        if (castMethod == null)
        {
            WarnUnsupportedShape(monitor, "Unable to create generic Enumerable.Cast for SpawnableItem.");
            return false;
        }

        // Cache validated members
        _searchBoxField = searchBoxField;
        _textBoxTextProp = textBoxTextProp;
        _spawnableItemItemProp = spawnableItemType.GetProperty("Item", BindingFlags.Instance | BindingFlags.Public);
        _spawnableItemNameProp = nameProp;
        _spawnableItemDisplayNameProp = displayNameProp;
        _spawnableItemIdProp = spawnableItemType.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public);
        _spawnableItemQualifiedIdProp = spawnableItemType.GetProperty("QualifiedItemId", BindingFlags.Instance | BindingFlags.Public);
        _castMethod = castMethod;

        try
        {
            harmony.Patch(
                original: searchItemsMethod,
                prefix: new HarmonyMethod(typeof(CjbItemSpawnerIntegration), nameof(Prefix)),
                postfix: new HarmonyMethod(typeof(CjbItemSpawnerIntegration), nameof(Postfix)),
                finalizer: new HarmonyMethod(typeof(CjbItemSpawnerIntegration), nameof(Finalizer))
            );

            _initialized = true;
            monitor?.Log("Successfully applied bilingual search integration to CJB Item Spawner.", LogLevel.Info);
            return true;
        }
        catch (Exception ex)
        {
            WarnUnsupportedShape(monitor, $"Harmony patching failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Prefix hook: if a search query is active, temporarily sets SearchBox.Text to empty string.
    /// Preserves the exact, untrimmed original query string in __state.
    /// This allows CJB's SearchItems() to execute its sorting and category filtering on the full set
    /// of candidate items without discarding matches prematurely.
    /// </summary>
    public static void Prefix(object __instance, out string? __state)
    {
        __state = null;
        try
        {
            if (_searchBoxField != null && _textBoxTextProp != null)
            {
                object? searchBox = _searchBoxField.GetValue(__instance);
                if (searchBox != null)
                {
                    string? currentText = _textBoxTextProp.GetValue(searchBox) as string;
                    if (!string.IsNullOrEmpty(currentText))
                    {
                        // Preserve exact original query (untrimmed) for restoration
                        __state = currentText;
                        _textBoxTextProp.SetValue(searchBox, string.Empty);
                    }
                }
            }
        }
        catch
        {
            __state = null;
        }
    }

    /// <summary>
    /// Postfix hook: applies bilingual and diacritic-insensitive filtering to the sorted,
    /// category-filtered items returned by CJB's SearchItems().
    /// Guaranteed to restore SearchBox.Text via finally block.
    /// </summary>
    public static void Postfix(object __instance, string? __state, ref object? __result)
    {
        if (__state == null)
            return;

        try
        {
            if (__result is IEnumerable enumerable && _castMethod != null)
            {
                ItemNameResolver? resolver = TestResolverOverride ?? ModEntry.Instance?.Resolver;
                ModConfig? config = ModEntry.Instance?.Config;
                bool ignoreDiacritics = config?.DiacriticInsensitiveSearch ?? true;

                var matched = new List<object>();
                foreach (object itemObj in enumerable)
                {
                    bool isMatch;
                    try
                    {
                        Item? item = _spawnableItemItemProp?.GetValue(itemObj) as Item;
                        string? name = _spawnableItemNameProp?.GetValue(itemObj) as string;
                        string? displayName = _spawnableItemDisplayNameProp?.GetValue(itemObj) as string;
                        string? id = _spawnableItemIdProp?.GetValue(itemObj) as string;
                        string? qualifiedId = _spawnableItemQualifiedIdProp?.GetValue(itemObj) as string;

                        isMatch = CjbSearchFilter.Matches(item, name, displayName, id, qualifiedId, __state, resolver, ignoreDiacritics);
                    }
                    catch (Exception itemEx)
                    {
                        LogFilterErrorOnce(itemEx);
                        // Fall back to native name/displayName matching for this item rather than dropping it or breaking the loop
                        string? name = _spawnableItemNameProp?.GetValue(itemObj) as string;
                        string? displayName = _spawnableItemDisplayNameProp?.GetValue(itemObj) as string;
                        string trimmed = __state.Trim();
                        isMatch = (name != null && name.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase) >= 0)
                               || (displayName != null && displayName.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase) >= 0);
                    }

                    if (isMatch)
                    {
                        matched.Add(itemObj);
                    }
                }

                __result = _castMethod.Invoke(null, new object[] { matched });
            }
        }
        catch (Exception ex)
        {
            LogFilterErrorOnce(ex);
            // In case of outer error, fall back to native query filtering across candidate enumerable
            // rather than exposing all items in category with query cleared
            if (__result is IEnumerable fallbackEnumerable && _castMethod != null)
            {
                try
                {
                    string trimmed = __state.Trim();
                    var fallbackMatched = new List<object>();
                    foreach (object itemObj in fallbackEnumerable)
                    {
                        string? name = _spawnableItemNameProp?.GetValue(itemObj) as string;
                        string? displayName = _spawnableItemDisplayNameProp?.GetValue(itemObj) as string;
                        if ((name != null && name.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase) >= 0)
                         || (displayName != null && displayName.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            fallbackMatched.Add(itemObj);
                        }
                    }
                    __result = _castMethod.Invoke(null, new object[] { fallbackMatched });
                }
                catch
                {
                    // Suppress if even fallback fails; __result remains what CJB returned
                }
            }
        }
        finally
        {
            RestoreSearchBoxText(__instance, __state);
        }
    }

    /// <summary>
    /// Harmony Finalizer hook: ensures SearchBox.Text is restored even if SearchItems() throws an unhandled exception.
    /// Preserves original exception throwing behavior.
    /// </summary>
    public static Exception? Finalizer(object __instance, string? __state, Exception? __exception)
    {
        if (__state != null)
        {
            RestoreSearchBoxText(__instance, __state);
        }
        return __exception;
    }

    /// <summary>
    /// Restores the SearchBox.Text property safely.
    /// </summary>
    public static void RestoreSearchBoxText(object? instance, string? originalText)
    {
        if (instance == null || originalText == null || _searchBoxField == null || _textBoxTextProp == null)
            return;

        try
        {
            object? searchBox = _searchBoxField.GetValue(instance);
            if (searchBox != null)
            {
                string? current = _textBoxTextProp.GetValue(searchBox) as string;
                if (current != originalText)
                {
                    _textBoxTextProp.SetValue(searchBox, originalText);
                }
            }
        }
        catch
        {
            // Do not throw from text restoration
        }
    }

    private static void WarnUnsupportedShape(IMonitor? monitor, string reason)
    {
        if (!_warnedUnsupportedShape)
        {
            _warnedUnsupportedShape = true;
            monitor?.Log($"CJB Item Spawner integration skipped (unsupported version or shape): {reason}", LogLevel.Warn);
        }
    }

    public static void LogFilterErrorOnce(Exception ex)
    {
        if (!_loggedFilterError)
        {
            _loggedFilterError = true;
            ModEntry.Instance?.Monitor.Log($"Error during CJB bilingual search filtering, falling back to native query filter: {ex.Message}", LogLevel.Warn);
        }
    }

    /// <summary>
    /// Resets static reflection state and flags for testing purposes.
    /// </summary>
    public static void ResetStateForTests()
    {
        _initialized = false;
        _warnedUnsupportedShape = false;
        _loggedFilterError = false;
        TestResolverOverride = null;
        _searchBoxField = null;
        _textBoxTextProp = null;
        _spawnableItemItemProp = null;
        _spawnableItemNameProp = null;
        _spawnableItemDisplayNameProp = null;
        _spawnableItemIdProp = null;
        _spawnableItemQualifiedIdProp = null;
        _castMethod = null;
    }
}
