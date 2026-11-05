using System;
using System.Threading;

namespace UntranslatedStardew.Core;

/// <summary>
/// Scopes render and measurement operations to own formatted bilingual tooltips and menu scopes.
/// Guarantees that unscoped draws and measurements outside our surfaces remain byte-for-byte unchanged.
/// Uses an idempotent reference cookie per invocation to prevent premature scope exits when both
/// Harmony Postfix and Finalizer run on the same invocation.
/// </summary>
public static class BilingualRenderScope
{
    private static readonly ThreadLocal<int> Depth = new(() => 0);

    /// <summary>
    /// Current recursion depth for diagnostic and test inspection.
    /// </summary>
    public static int CurrentDepth => Depth.Value;

    /// <summary>
    /// Indicates whether execution is currently inside an active bilingual render or measurement scope.
    /// </summary>
    public static bool IsActive => Depth.Value > 0;

    /// <summary>
    /// Begins a bilingual render scope and returns an idempotent IDisposable cookie to exit on dispose.
    /// Supports nested scopes cleanly via reference counting.
    /// </summary>
    public static ScopeCookie Begin()
    {
        Enter();
        return new ScopeCookie();
    }

    /// <summary>
    /// Enters a bilingual render scope.
    /// </summary>
    public static void Enter()
    {
        Depth.Value++;
    }

    /// <summary>
    /// Exits a bilingual render scope safely without underflowing.
    /// </summary>
    public static void Exit()
    {
        if (Depth.Value > 0)
        {
            Depth.Value--;
        }
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
