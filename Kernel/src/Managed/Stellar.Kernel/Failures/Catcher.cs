using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Services;

namespace Stellar.Kernel.Failures;

/// <summary>
/// Catches failures around suspicious calls, reports them, and lets the work go on, or stops it, as the reaction says.
/// </summary>
/// <remarks>
/// <para>Used as a scope, with catch blocks of its own:</para>
/// <code>
/// using (var catcher = new Catcher(FailureKind.FileSystem, FailureLevel.Warning))
/// {
///     catcher.Catch&lt;FileNotFoundException&gt;(_ =&gt; firstRun = true);
///
///     var save = catcher.Try(() =&gt; LoadSave(slot), fallback: Save.Empty);
///     catcher.Run(() =&gt; UpgradeSettings());
/// }
/// </code>
/// <para>A call that fails becomes a <see cref="Failure"/> of the kind and level of the catcher, reported to the failure
/// system of the current engine, or to the one given. Then:</para>
/// <list type="bullet">
/// <item>if the reaction stops the work (<see cref="FailureReaction.StopExecution"/>), the failure is thrown on, as a
/// <see cref="FailureException"/>, as if the call had not been caught: the whole <c>try</c> around it stops;</item>
/// <item>otherwise the call answers no (<see cref="Run"/>), or gives the fallback (<see cref="Try{T}(Func{T},T,string)"/>), and
/// the block goes on.</item>
/// </list>
/// <para>A catch block (<see cref="Catch{TException}(Action{TException})"/>) handles its exceptions itself: they are
/// not reported, as with a <c>catch</c> clause. <see cref="Catch{TException}(FailureLevel)"/> reports its exceptions
/// at another level.</para>
/// <para>Outside any engine, and with no system given, a catcher stands alone: it records its failures with the
/// default reactions, and that is all. Cancellation (<see cref="OperationCanceledException"/>) is never a failure: it
/// goes through.</para>
/// <para><see cref="Guard"/> and <see cref="Report(Exception,string,FailureLevel?,FailureKind?,IQuantumObject,FramePhase?)"/>
/// never throw: they report and give the failure back, for code that decides itself what a failure means, such as the
/// loops of the engine.</para>
/// </remarks>
public sealed class Catcher
    : IDisposable
{
    private readonly IFailureSystem? _system;
    private readonly Func<Failure, FailureReaction>? _report;
    private readonly object _sync = new();
    private volatile Rule[] _rules = [];
    private volatile Failure[] _failures = [];
    private volatile bool _disposed;

    /// <summary>
    /// Makes a catcher that reports to the failure system of the current engine, or stands alone outside any engine.
    /// </summary>
    /// <param name="kind">The kind of its failures.</param>
    /// <param name="level">The level of its failures.</param>
    /// <param name="sender">The object its failures happen in, if any.</param>
    public Catcher(FailureKind kind = default, FailureLevel level = FailureLevel.Error, IQuantumObject? sender = null)
    {
        Kind = kind;
        Level = level;
        Sender = sender;
    }

    /// <summary>
    /// Makes a catcher that reports to a failure system.
    /// </summary>
    /// <param name="system">The failure system.</param>
    /// <param name="kind">The kind of its failures.</param>
    /// <param name="level">The level of its failures.</param>
    /// <param name="sender">The object its failures happen in, if any.</param>
    public Catcher(IFailureSystem system, FailureKind kind = default, FailureLevel level = FailureLevel.Error,
        IQuantumObject? sender = null)
        : this(kind, level, sender)
    {
        _system = system ?? throw new ArgumentNullException(nameof(system));
    }

    /// <summary>
    /// Makes a catcher that reports through a function of its own: for the runtime, before an engine has a system.
    /// </summary>
    /// <param name="report">Reports a failure and gives its reaction.</param>
    /// <param name="kind">The kind of its failures.</param>
    /// <param name="level">The level of its failures.</param>
    /// <param name="sender">The object its failures happen in, if any.</param>
    public Catcher(Func<Failure, FailureReaction> report, FailureKind kind = default, FailureLevel level = FailureLevel.Error,
        IQuantumObject? sender = null)
        : this(kind, level, sender)
    {
        _report = report ?? throw new ArgumentNullException(nameof(report));
    }

    /// <summary>Gets the kind of its failures.</summary>
    public FailureKind Kind { get; }

    /// <summary>Gets the level of its failures.</summary>
    public FailureLevel Level { get; }

    /// <summary>Gets the object its failures happen in, if any.</summary>
    public IQuantumObject? Sender { get; }

    /// <summary>Gets the failures it caught, in order.</summary>
    public IReadOnlyList<Failure> Failures => _failures;

    /// <summary>Gets whether it caught a failure.</summary>
    public bool HasFailed => _failures.Length > 0;

    /// <summary>
    /// Adds a catch block: the exceptions of a type are handled here and not reported. The first block that matches
    /// wins, as with <c>catch</c> clauses.
    /// </summary>
    /// <typeparam name="TException">The type of the exceptions.</typeparam>
    /// <param name="handler">What to do with one.</param>
    /// <returns>This catcher.</returns>
    public Catcher Catch<TException>(Action<TException> handler)
        where TException : Exception
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));
        AddRule(new Rule(typeof(TException), exception => handler((TException)exception), null));
        return this;
    }

    /// <summary>
    /// Reports the exceptions of a type at another level: a missing file as a warning, for instance.
    /// </summary>
    /// <typeparam name="TException">The type of the exceptions.</typeparam>
    /// <param name="level">Their level.</param>
    /// <returns>This catcher.</returns>
    public Catcher Catch<TException>(FailureLevel level)
        where TException : Exception
    {
        AddRule(new Rule(typeof(TException), null, level));
        return this;
    }

    /// <summary>
    /// Runs a suspicious call.
    /// </summary>
    /// <param name="action">The call.</param>
    /// <param name="operation">What it does, for the message of a failure.</param>
    /// <returns><c>true</c> if it succeeded; <c>false</c> if it failed and the work goes on.</returns>
    /// <exception cref="FailureException">Thrown on if the reaction to the failure stops the work.</exception>
    public bool Run(Action action, string? operation = null)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        ThrowIfDisposed();

        try
        {
            action();
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ThrowIfStops(Handle(exception, operation, null, null, null, null), exception);
            return false;
        }
    }

    /// <summary>
    /// Runs a suspicious call that gives a value.
    /// </summary>
    /// <param name="function">The call.</param>
    /// <param name="value">What it gave, or <c>default</c>.</param>
    /// <param name="operation">What it does, for the message of a failure.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns><c>true</c> if it succeeded; <c>false</c> if it failed and the work goes on.</returns>
    /// <exception cref="FailureException">Thrown on if the reaction to the failure stops the work.</exception>
    public bool Try<T>(Func<T> function, out T value, string? operation = null)
    {
        if (function is null) throw new ArgumentNullException(nameof(function));
        ThrowIfDisposed();

        try
        {
            value = function();
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ThrowIfStops(Handle(exception, operation, null, null, null, null), exception);
            value = default!;
            return false;
        }
    }

    /// <summary>
    /// Runs a suspicious call that gives a value, with a value to fall back on.
    /// </summary>
    /// <param name="function">The call.</param>
    /// <param name="fallback">The value if it fails and the work goes on.</param>
    /// <param name="operation">What it does, for the message of a failure.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>What it gave, or <paramref name="fallback"/>.</returns>
    /// <exception cref="FailureException">Thrown on if the reaction to the failure stops the work.</exception>
    public T Try<T>(Func<T> function, T fallback, string? operation = null)
    {
        return Try(function, out var value, operation) ? value : fallback;
    }

    /// <summary>
    /// Runs a suspicious call without blocking.
    /// </summary>
    /// <param name="action">The call.</param>
    /// <param name="operation">What it does, for the message of a failure.</param>
    /// <returns><c>true</c> if it succeeded; <c>false</c> if it failed and the work goes on.</returns>
    /// <exception cref="FailureException">Thrown on if the reaction to the failure stops the work.</exception>
    public async ValueTask<bool> RunAsync(Func<ValueTask> action, string? operation = null)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        ThrowIfDisposed();

        try
        {
            await action().ConfigureAwait(false);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ThrowIfStops(Handle(exception, operation, null, null, null, null), exception);
            return false;
        }
    }

    /// <summary>
    /// Runs a suspicious call that gives a value without blocking, with a value to fall back on.
    /// </summary>
    /// <param name="function">The call.</param>
    /// <param name="fallback">The value if it fails and the work goes on.</param>
    /// <param name="operation">What it does, for the message of a failure.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>What it gave, or <paramref name="fallback"/>.</returns>
    /// <exception cref="FailureException">Thrown on if the reaction to the failure stops the work.</exception>
    public async ValueTask<T> TryAsync<T>(Func<ValueTask<T>> function, T fallback, string? operation = null)
    {
        if (function is null) throw new ArgumentNullException(nameof(function));
        ThrowIfDisposed();

        try
        {
            return await function().ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ThrowIfStops(Handle(exception, operation, null, null, null, null), exception);
            return fallback;
        }
    }

    /// <summary>
    /// Runs a call, reports what fails, and never throws: the caller decides what the failure means.
    /// </summary>
    /// <param name="action">The call.</param>
    /// <param name="operation">What it does, for the message of a failure.</param>
    /// <param name="kind">The kind of a failure, instead of the one of the catcher.</param>
    /// <param name="sender">The object a failure happens in, instead of the one of the catcher.</param>
    /// <param name="phase">The phase of the frame, if any.</param>
    /// <returns>The reported failure, with its reaction; <c>null</c> if the call succeeded or a catch block handled it.</returns>
    public Failure? Guard(Action action, string? operation = null, FailureKind? kind = null, IQuantumObject? sender = null,
        FramePhase? phase = null)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));

        try
        {
            action();
            return null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Handle(exception, operation, null, kind, sender, phase);
        }
    }

    /// <summary>
    /// Runs a call without blocking, reports what fails, and never throws.
    /// </summary>
    /// <param name="action">The call.</param>
    /// <param name="operation">What it does, for the message of a failure.</param>
    /// <param name="kind">The kind of a failure, instead of the one of the catcher.</param>
    /// <param name="sender">The object a failure happens in, instead of the one of the catcher.</param>
    /// <returns>The reported failure, with its reaction; <c>null</c> if the call succeeded or a catch block handled it.</returns>
    public async ValueTask<Failure?> GuardAsync(Func<ValueTask> action, string? operation = null, FailureKind? kind = null,
        IQuantumObject? sender = null)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));

        try
        {
            await action().ConfigureAwait(false);
            return null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Handle(exception, operation, null, kind, sender, null);
        }
    }

    /// <summary>
    /// Reports an exception caught by hand, in a <c>catch</c> clause; never throws.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="operation">What was being done, for the message.</param>
    /// <param name="level">Its level, instead of the one of the catcher.</param>
    /// <param name="kind">Its kind, instead of the one of the catcher.</param>
    /// <param name="sender">The object it happened in, instead of the one of the catcher.</param>
    /// <param name="phase">The phase of the frame, if any.</param>
    /// <returns>The reported failure, with its reaction; <c>null</c> if a catch block handled it.</returns>
    public Failure? Report(Exception exception, string? operation = null, FailureLevel? level = null, FailureKind? kind = null,
        IQuantumObject? sender = null, FramePhase? phase = null)
    {
        if (exception is null) throw new ArgumentNullException(nameof(exception));
        return Handle(exception, operation, level, kind, sender, phase);
    }

    /// <summary>
    /// Reports a failure that is no exception: a missing asset that has a placeholder, for instance. Never throws.
    /// </summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="level">Its level, instead of the one of the catcher.</param>
    /// <returns>The reported failure, with its reaction.</returns>
    public Failure Report(string message, FailureLevel? level = null)
    {
        var failure = new Failure(Kind, level ?? Level, message, null, Sender);
        Record(failure);
        return failure;
    }

    /// <summary>
    /// Throws the most serious failure caught, if there is one: for a block that went on, to fail as a whole after.
    /// </summary>
    /// <exception cref="FailureException">Thrown if the catcher caught a failure.</exception>
    public void ThrowIfFailed()
    {
        var failures = _failures;
        if (failures.Length == 0) return;

        var worst = failures.OrderByDescending(failure => failure.Level).First();
        if (worst.Exception is FailureException engine) ExceptionDispatchInfo.Capture(engine).Throw();
        throw new FailureException(worst);
    }

    /// <summary>
    /// Ends the scope of the catcher: it catches nothing after.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
    }

    private Failure? Handle(Exception exception, string? operation, FailureLevel? level, FailureKind? kind,
        IQuantumObject? sender, FramePhase? phase)
    {
        foreach (var rule in _rules)
        {
            if (!rule.Type.IsInstanceOfType(exception)) continue;
            if (rule.Handler is { } handler)
            {
                handler(exception);
                return null;
            }

            level ??= rule.Level;
            break;
        }

        var failure = Failure.From(exception, kind ?? Kind, level ?? Level, sender ?? Sender, operation, phase);
        Record(failure);
        return failure;
    }

    // Each failure is reported once: one caught again by an outer catcher keeps its first reaction
    private void Record(Failure failure)
    {
        lock (_sync) _failures = [.. _failures, failure];
        if (failure.IsReported) return;

        FailureReaction reaction;
        if (_report is not null) reaction = _report(failure);
        else if ((_system ?? EngineScope.Current?.Services?.Find<IFailureSystem>()) is { } system) reaction = system.Report(failure);
        else reaction = FailureReactions.Default(failure.Level);

        failure.MarkReported(reaction);
    }

    private static void ThrowIfStops(Failure? failure, Exception exception)
    {
        if (failure?.Reaction is not { } reaction || !reaction.Stops()) return;

        // The exception of the engine as it was thrown; any other inside a failure of the engine
        if (exception is FailureException) ExceptionDispatchInfo.Capture(exception).Throw();
        throw new FailureException(failure);
    }

    private void AddRule(Rule rule)
    {
        ThrowIfDisposed();
        lock (_sync) _rules = [.. _rules, rule];
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Catcher));
    }

    private sealed record Rule(Type Type, Action<Exception>? Handler, FailureLevel? Level);
}
