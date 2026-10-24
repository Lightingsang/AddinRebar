# Port ledger — Kata adapters copied from the WPF tool (ADR-0009)

Each row: source file in the rebar add-in, the commit it was copied at, the toolkit file, what changed. Before porting more, or when the WPF file changes later, compare with `git log <commit>..HEAD -- <source>`.

| Source (`HPRebar/HPRebar/…`) | Source commit | Toolkit file (`HPRebar/HPBimToolkit/…`) | Changes |
|---|---|---|---|
| `KataExport/Service/ExcelComAttach.cs` | 0002421 (2026-09-27) | `Services/Kata/Excel/ExcelComAttach.cs` | namespace; doc comment |
| `KataExport/Service/ComLateBinding.cs` | 0002421 (2026-09-27) | `Services/Kata/Excel/ComLateBinding.cs` | namespace; `ArgumentNullException.ThrowIfNull`; `Release` catches only `InvalidComObjectException` (was every exception) |
