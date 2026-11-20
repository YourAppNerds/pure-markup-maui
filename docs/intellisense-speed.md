# IntelliSense speed

Design risk: about 2,100 generated methods, nearly doubled by the `PresetContext<T>` overloads, might
slow completion. This page records an automated check of that risk.

**Verdict: fine, no library change.** With the package referenced, completion after `new Button().`
costs about 10 to 20 ms more than plain MAUI (warm median), well under the ~100 ms per keystroke
budget. The generator does under 1 ms of work per keystroke and its whole-assembly walk stays
cached. The one costly setup is a consumer-time `WholeAssembly = true` walk over a big assembly:
about 55 to 65 ms added to every completion, from Roslyn binding the generated source, not from
the generator.

## Method

`benchmarks/IntelliSense/` is a console tool (not packed). It builds a small consumer app in a
Roslyn `AdhocWorkspace`: a page, a view model and a custom `RatingView` control. Its references are
what a MAUI app compiles against (runtime, `Microsoft.Maui.*`, `Microsoft.Extensions.*`, net10.0).
The generator is attached as an analyzer reference, the way the package's `analyzers/` folder
attaches it. Three scenarios:

| Scenario | References | Generator input |
|---|---|---|
| Baseline | plain MAUI, page written with object initializers | none |
| PureMarkup | + `PureMarkup.Maui.dll` (the ~1,583 MAUI methods and their preset overloads come from metadata) | `[assembly: GenerateMarkup(typeof(RatingView))]` |
| PureMarkup + WholeAssembly | as above | also `GenerateMarkup(typeof(View), WholeAssembly = true, Namespace = "Consumer.Markup")`: all of Microsoft.Maui.Controls generated again as source (148 files), as the largest assembly at hand |

Each measured iteration types one character into a string literal in the page, then:

- **Completion**: `CompletionService.GetCompletionsAsync` with a `.` insertion trigger, after
  `new Button().`, on a `PresetContext<Button>` parameter, and on an unrelated receiver
  (`"text".`) to check that constraints keep the methods off other types.
- **Workspace compilation**: `Project.GetCompilationAsync()`, which runs the generator.
- **Generator driver**: `CSharpGeneratorDriver.RunGenerators` on the edited compilation, with step
  tracking, to report which steps re-run.

Warm numbers are the median and p90 of 30 iterations. Cold is the first call in a fresh workspace
with fresh metadata references, after a JIT warm-up round in the same process.

## Results

Windows 11, 12 logical CPUs, .NET 10.0.14, Roslyn 4.14. Absolute numbers depend on the machine and
vary by a few ms between runs; compare the ratios.

Completion:

| Scenario | Receiver | Items | Extension items | Cold ms | Warm median ms | Warm p90 ms |
|---|---|---:|---:|---:|---:|---:|
| Baseline | `new Button().` | 167 | 44 | 42 | 6.3 | 7.0 |
| Baseline | `"text".` | 38 | 0 | 48 | 3.8 | 4.4 |
| PureMarkup | `new Button().` | 318 | 194 | 94 | 19.5 | 33.6 |
| PureMarkup | `PresetContext<Button>` | 121 | 111 | 90 | 14.5 | 17.1 |
| PureMarkup | `"text".` | 39 | 1 (`Use`) | 72 | 6.5 | 10.0 |
| + WholeAssembly | `new Button().` | 299 | 176 | 218 | 59.4 | 78.1 |
| + WholeAssembly | `PresetContext<Button>` | 114 | 104 | 236 | 69.5 | 91.8 |
| + WholeAssembly | `"text".` | 38 | 0 | 198 | 58.4 | 81.2 |

Completion versus baseline (warm median; the baseline for `PresetContext<Button>` is
`new Button().`). Across three 30-iteration runs, PureMarkup added 9 to 18 ms after `new Button().`.

| Scenario | `new Button().` | `PresetContext<Button>` | `"text".` |
|---|---|---|---|
| PureMarkup | 3.1x, +13 ms | 2.3x, +8 ms | 1.7x, +3 ms |
| + WholeAssembly | 9.4x, +53 ms | 11.0x, +63 ms | 15.3x, +55 ms |

Per keystroke, generator and compilation:

| Scenario | Workspace `GetCompilationAsync` warm median ms | Generator cold ms | Generator warm median ms | Generated files |
|---|---:|---:|---:|---:|
| Baseline | 0.1 | n/a | n/a | 0 |
| PureMarkup | 0.7 | 8 | 0.6 | 2 |
| + WholeAssembly | 1.1 | 34 | 0.7 | 148 |

Step reasons on a keystroke run, in both generator scenarios: `WholeAssemblyKey` Unchanged,
`WholeAssemblyWalk` Cached, `Local` Unchanged, every `SourceOutput` Cached. Only the two
`Compilation` input steps show Modified. So an unrelated edit never re-walks an assembly and
re-emits no files.

## Reading the numbers

- **Package use (the normal case)** adds about 10 to 20 ms per completion. Constraints work: an
  unrelated receiver gains a single item, `Use`. Its `TElement : TPreset` constraint cannot be
  checked from the receiver alone, so Roslyn lists it on every type. That is a small listing quirk,
  not a speed problem.
- **Consumer-time WholeAssembly** adds about 55 to 65 ms per completion on any receiver, including
  unrelated ones. The generator is not the cause: its walk stays cached and costs under 1 ms per
  keystroke. The cause is that every edit creates a new compilation, so Roslyn binds the signatures
  of the 148 generated source files again before it can test extension methods. Metadata (the
  package case) has no such cost. It stays under the 100 ms budget, but it is the one setup to
  avoid. Consumers never need it for MAUI itself, since the package ships those methods. Use
  `WholeAssembly` for your own control libraries, which are usually much smaller.
- **`EditorBrowsable(Advanced)` would not help speed.** Roslyn filters by browsability after it has
  looked up and tested the extension methods. Also, the "Hide advanced members" option is off by
  default for C# in Visual Studio. It would only shorten the list for users who turn that option
  on, so it was not applied.

Limits: an `AdhocWorkspace` runs completion in-process. Visual Studio and Rider add UI and
out-of-process overhead, which is the same in every scenario. Their "show items from unimported
namespaces" lists are filled in the background. A manual spot check in Visual Studio or Rider is
still possible but optional.

## Rerun

```
dotnet run -c Release --project benchmarks/IntelliSense -- 30
```

The argument is the number of warm iterations (default 30). The tool prints the tables above as
Markdown. It takes under a minute.
