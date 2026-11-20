# Page construction benchmarks

`benchmarks/PageConstruction` (BenchmarkDotNet, not packed) builds one registration form five
ways: a `ContentPage` with a 7x2 grid, a title label, four label + entry rows, two buttons with
`Clicked` handlers. Every variant sets the same effective values; `Equivalence.Check()` runs in
`[GlobalSetup]` and fails the run if a variant drifts.

| Variant | How the looks are set |
|---------|-----------------------|
| Plain C# setters (baseline) | object initializers, `Grid.SetRow`, `+=` |
| PureMarkup fluent | generated methods + sugar, enum-named grid (`Rows.Define`, `.Row(Row.X)`) |
| CommunityToolkit.Maui.Markup 8.0.0 | initializers + toolkit extensions, enum-named grid |
| Plain C# + shared Styles | prebuilt static `Style` objects (one `BasedOn`), `Style = ...` |
| PureMarkup + presets | `Preset.Once` property getters (one `.With`), `.New()` |

## Rerun
```
dotnet run -c Release --project benchmarks/PageConstruction -- --filter *
dotnet run -c Release --project benchmarks/PageConstruction -- --check
```
`--check` only verifies the variants match. Results land in `BenchmarkDotNet.Artifacts/` (pass
`--artifacts <dir>` to move them).

## Results (2026-11-20)
AMD Ryzen 5 9600X, Windows 11, .NET 10.0.14, MAUI Controls 10.0.90 (CommunityToolkit.Maui.Markup
8.0.0 needs >= 10.0.90; the library itself builds against 10.0.20). Two full runs:

| Variant | Mean, run 1 | Mean, run 2 | Allocated |
|---------|------------:|------------:|----------:|
| Plain C# setters | 152.4 us | 116.0 us | 96.82 KB |
| PureMarkup fluent | 156.8 us (1.03) | 123.1 us (1.07) | 99.33 KB (1.03) |
| CommunityToolkit.Maui.Markup | 135.0 us (0.89) | 119.7 us (1.04) | 99.60 KB (1.03) |
| Plain C# + shared Styles | 153.3 us (1.01) | 143.0 us (1.24) | 100.43 KB (1.04) |
| PureMarkup + presets | 157.6 us (1.03) | 147.0 us (1.27) | 103.47 KB (1.07) |

Reading:
- Absolute times move by ~25% between runs on this desktop (StdDev 3-10 us), so only ratios
  within one run mean much, and differences under ~10% are noise. Allocations are stable.
- PureMarkup fluent costs about the same as plain setters: +2.5 KB (+3%), the same as the toolkit.
  The extra bytes are most likely the `Rows.Define`/`Columns.Define` tuple arrays (not profiled).
- Presets cost what plain MAUI Styles cost (run 2: 1.27 vs 1.24); both pay MAUI's style
  application per element. Presets add 3 KB over shared Styles: each documented
  `static Preset<T> X => Preset.Once(...)` getter allocates a small `Preset<T>` per access
  before the cache lookup. Caching the preset in a `static readonly` field would remove that;
  not worth it at 3 KB per page.

## Limits
- Headless net10.0: no `Application`, no handlers, no platform views. This measures the object
  graph, BindableProperty writes and Style application, not handler creation, layout or
  rendering, which dominate a real page load. Running a real platform loop under
  BenchmarkDotNet was not attempted.
- No bindings: `Binding.Create(lambda)` needs MAUI's source-generated interceptor, which does not
  run in this plain net10.0 project (it throws without it). Binding cost is the same for every
  variant anyway, since all of them call `SetBinding` with the same `BindingBase`.
- No implicit (`UsePresets`/`UseDefaultLook`) styles: those resolve through the resource tree
  when the element is parented, which this setup only partly exercises.
