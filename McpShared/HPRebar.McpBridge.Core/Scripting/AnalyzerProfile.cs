namespace HPRebar.McpBridge.Core.Scripting;

/// <summary>
///     How <see cref="ScriptAnalyzer"/> recognises "this script opens its own transaction" in a given
///     host. Revit scripts construct <c>new Transaction(doc, …)</c>; AutoCAD scripts call
///     <c>db.TransactionManager.StartTransaction()</c>. Plain name lists so Core stays host-free.
/// </summary>
public sealed class AnalyzerProfile
{
    public static readonly AnalyzerProfile Revit = new AnalyzerProfile(
        transactionTypeNames: new[] { "Transaction", "TransactionGroup", "SubTransaction" },
        transactionMethodNames: Array.Empty<string>());

    public static readonly AnalyzerProfile Autocad = new AnalyzerProfile(
        transactionTypeNames: Array.Empty<string>(),
        transactionMethodNames: new[] { "StartTransaction", "StartOpenCloseTransaction" });

    /// <summary>
    ///     Navisworks scripts may open a transaction either way — <c>doc.BeginTransaction(...)</c> or the public
    ///     <c>new Transaction(doc, ...)</c> constructor. Both are denied by <see cref="GuardProfile.Navis"/>; the
    ///     analyzer still names them so a proposal is told "the script manages a transaction" rather than only "guard".
    /// </summary>
    public static readonly AnalyzerProfile Navis = new AnalyzerProfile(
        transactionTypeNames: new[] { "Transaction" },
        transactionMethodNames: new[] { "BeginTransaction" });

    public AnalyzerProfile(IReadOnlyCollection<string> transactionTypeNames, IReadOnlyCollection<string> transactionMethodNames)
    {
        TransactionTypeNames = new HashSet<string>(transactionTypeNames, StringComparer.Ordinal);
        TransactionMethodNames = new HashSet<string>(transactionMethodNames, StringComparer.Ordinal);
    }

#if NET48
    // IReadOnlySet<T> does not exist on .NET Framework; see GuardProfile for the reasoning behind the read-only view.
    /// <summary>Type names whose construction means the script manages a transaction (`new Transaction(...)`).</summary>
    public IReadOnlyCollection<string> TransactionTypeNames { get; }

    /// <summary>Method names whose invocation means the script manages a transaction (`StartTransaction()`).</summary>
    public IReadOnlyCollection<string> TransactionMethodNames { get; }
#else
    /// <summary>Type names whose construction means the script manages a transaction (`new Transaction(...)`).</summary>
    public IReadOnlySet<string> TransactionTypeNames { get; }

    /// <summary>Method names whose invocation means the script manages a transaction (`StartTransaction()`).</summary>
    public IReadOnlySet<string> TransactionMethodNames { get; }
#endif
}
