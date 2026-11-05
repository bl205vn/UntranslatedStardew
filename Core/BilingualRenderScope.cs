using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace UntranslatedStardew.Core;

/// <summary>
/// Scopes render and measurement operations to own formatted bilingual tooltips and menu scopes.
/// Guarantees that unscoped draws and measurements outside our surfaces remain byte-for-byte unchanged.
/// Tracks ambient role context (Primary/Secondary) and scoped text-to-role mappings for wrapping
/// and separate label drawing without global leakage across unrelated contexts.
/// Uses an idempotent reference cookie per invocation to prevent premature scope exits when both
/// Harmony Postfix and Finalizer run on the same invocation.
/// </summary>
public static class BilingualRenderScope
{
    private static readonly ThreadLocal<int> Depth = new(() => 0);
    private static readonly ThreadLocal<Stack<FontRole?>> RoleStack = new(() => new Stack<FontRole?>());
    private static readonly ThreadLocal<Stack<Dictionary<string, FontRole>?>> ScopedRolesStack = new(() => new Stack<Dictionary<string, FontRole>?>());

    /// <summary>
    /// Current recursion depth for diagnostic and test inspection.
    /// </summary>
    public static int CurrentDepth => Depth.Value;

    /// <summary>
    /// Indicates whether execution is currently inside an active bilingual render or measurement scope.
    /// </summary>
    public static bool IsActive => Depth.Value > 0;

    /// <summary>
    /// Gets the current ambient font role for wrapping or separate label measurement/drawing,
    /// or null if operating in composite mode.
    /// </summary>
    public static FontRole? ActiveRole
    {
        get
        {
            foreach (FontRole? role in RoleStack.Value!)
                if (role.HasValue) return role;
            return null;
        }
    }

    /// <summary>
    /// Begins a bilingual render scope and returns an idempotent IDisposable cookie to exit on dispose.
    /// Supports nested scopes and ambient role context cleanly via reference counting.
    /// </summary>
    public static ScopeCookie Begin(FontRole? role = null)
    {
        Enter(role);
        return new ScopeCookie();
    }

    /// <summary>
    /// Enters a bilingual render scope with an optional ambient role.
    /// </summary>
    public static void Enter(FontRole? role = null)
    {
        Depth.Value++;
        RoleStack.Value!.Push(role);
        ScopedRolesStack.Value!.Push(null);
    }

    /// <summary>
    /// Registers an explicit font role for a specific text string strictly within the current render scope.
    /// Automatically discarded when the scope exits or disposes.
    /// </summary>
    public static void RegisterScopedRole(string? text, FontRole role)
    {
        if (string.IsNullOrEmpty(text) || Depth.Value <= 0)
            return;

        Dictionary<string, FontRole>? dict = ScopedRolesStack.Value!.Count > 0 ? ScopedRolesStack.Value!.Pop() : null;
        dict ??= new Dictionary<string, FontRole>(StringComparer.Ordinal);
        string normalized = text.Normalize(NormalizationForm.FormC);
        dict[text] = role;
        dict[normalized] = role;
        ScopedRolesStack.Value!.Push(dict);
    }

    /// <summary>
    /// Resolves the effective font role for a given text inside the current scope.
    /// Inspects scoped role overrides first, then falls back to ambient ActiveRole.
    /// Returns null if outside any scope or unassigned.
    /// </summary>
    public static FontRole? ResolveActiveRole(string? text = null)
    {
        if (Depth.Value <= 0)
            return null;

        if (ActiveRole.HasValue) return ActiveRole;

        if (!string.IsNullOrEmpty(text) && ScopedRolesStack.Value!.Count > 0)
        {
            foreach (Dictionary<string, FontRole>? dict in ScopedRolesStack.Value!)
            {
                if (dict != null)
                {
                    if (dict.TryGetValue(text, out FontRole r))
                        return r;
                    string normalized = text.Normalize(NormalizationForm.FormC);
                    if (dict.TryGetValue(normalized, out r))
                        return r;
                }
            }
        }

        return ActiveRole;
    }

    /// <summary>
    /// Exits a bilingual render scope safely without underflowing.
    /// </summary>
    public static void Exit()
    {
        if (Depth.Value > 0)
        {
            Depth.Value--;
            if (RoleStack.Value!.Count > 0)
            {
                RoleStack.Value!.Pop();
            }
            if (ScopedRolesStack.Value!.Count > 0)
            {
                ScopedRolesStack.Value!.Pop();
            }
        }
    }

    /// <summary>
    /// Resets all render scope depth and stacks (used for test isolation).
    /// </summary>
    public static void Reset()
    {
        Depth.Value = 0;
        RoleStack.Value!.Clear();
        ScopedRolesStack.Value!.Clear();
    }

    /// <summary>
    /// Idempotent reference cookie. Exactly one Exit() call occurs when Dispose() is invoked,
    /// even if called repeatedly across Harmony Postfix and Finalizer.
    /// </summary>
    public sealed class ScopeCookie : IDisposable
    {
        private int _disposed; // 0 = active, 1 = disposed

        public bool IsDisposed => _disposed != 0;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                Exit();
            }
        }
    }
}
