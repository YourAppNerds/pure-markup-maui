# Diagnostics

PureMarkup reports seven diagnostics.
All use the category `PureMarkup`.

| ID | Severity | Title | Source |
|---|---|---|---|
| [PM0001](#pm0001) | Info | Read-only bindable property skipped | Generator |
| [PM0002](#pm0002) | Info | Delegate-typed property skipped | Generator |
| [PM0003](#pm0003) | Info | Name collision skipped | Generator |
| [PM0004](#pm0004) | Info | Generic type skipped | Generator |
| [PM0005](#pm0005) | Info | Attached property target not supported | Generator |
| [PM0006](#pm0006) | Warning | Microsoft.Maui.Controls not referenced | Generator |
| [PM0007](#pm0007) | Warning | String-path binding passed to a markup method | Analyzer |

The generator diagnostics appear where `[assembly: GenerateMarkup(...)]` runs.
In the PureMarkup.Maui library, that is the build of the library itself.
In your app, they cover only the types you name in your own `GenerateMarkup` attributes.

The info diagnostics (PM0001 to PM0005) tell you a method is missing and why.
They need no fix.
You can still set the member the plain C# way.

To hide one, set its severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.PM0003.severity = none
```

## PM0001

**Read-only bindable property skipped.**
Message: `'{0}' is read-only; no markup method generated`.

The property has a `BindableProperty`, but no public setter.
Examples are `IsFocused` or `Width`.
There is nothing to set, so no method is generated.

Fix: none needed.
Read the property in code if you need its value.

## PM0002

**Delegate-typed property skipped.**
Message: `'{0}' has a delegate type; no markup method generated`.

The property (or attached property) holds a delegate, such as a `Func<...>`.
A method for it would look like an event handler, but behave differently.
So it is skipped.

Fix: set the property directly, for example in `.Also(...)` in a preset, or after the chain.

## PM0003

**Name collision skipped.**
Message: `'{0}' collides with '{1}'; no markup method generated`.

Two members on related types would get methods with the same name and a matching parameter type.
C# could not choose between them.
The generator first gives a colliding attached property its type name as a prefix (`Shell.BackgroundColor` becomes `ShellBackgroundColor`).
If that is not enough, it keeps the method on the most basic receiver and skips the other one.

It also skips a method when the type has a public instance method of the same name that takes an argument.
C# would always call that instance method instead.

Fix: none needed.
Set the skipped member the plain C# way.
If it is your own control, you can rename the member.

## PM0004

**Generic type skipped.**
Message: `'{0}' is generic; no markup methods generated for it`.

An extension method cannot infer a generic type's own type arguments from its receiver.
So the generator skips generic types.
Members they inherit from non-generic base types still have methods.

Fix: none needed.
Set the members the plain C# way.
For your own control, move the bindable properties to a non-generic base class and generate for that class.

## PM0005

**Attached property target not supported.**
Message: `'{0}' attaches to '{1}', which is not a BindableObject class; no markup method generated`.

The attached property's `SetX` method takes a target that is not a `BindableObject` class, such as an interface.
Methods need a `BindableObject` receiver.
If another `SetX` overload takes a `BindableObject`, that one is used and no diagnostic appears.

Fix: call the static `SetX` method directly.

## PM0006

**Microsoft.Maui.Controls not referenced.**
Message: `GenerateMarkup needs a reference to Microsoft.Maui.Controls`.

The project has a `GenerateMarkup` attribute, but no reference to `Microsoft.Maui.Controls`.
Nothing is generated.

Fix: make it a MAUI project (`<UseMaui>true</UseMaui>`) or reference the `Microsoft.Maui.Controls` package.
Or remove the attribute.

## PM0007

**String-path binding passed to a markup method.**
Message: `'{0}' gets a string-path Binding, which needs reflection and breaks under trimming and NativeAOT; use Binding.Create(static (TSource s) => s.Property)`.

A `Binding` built from a string path reaches a PureMarkup method.
That includes generated methods, preset methods such as `p.Text(...)` or `p.DataTrigger(...)`, and methods your own `GenerateMarkup` attribute produces.
A string-path binding resolves the path with reflection at run time.
The trimmer cannot see that, so the binding can break in a trimmed or NativeAOT app.

MAUI marks those constructors with `[RequiresUnreferencedCode]`.
But MAUI's build turns the trim analyzer off unless `TrimMode=full` or `PublishAot` is set.
So PM0007 is the warning you see on a normal build.

```csharp
// PM0007
new Label().Text(new Binding("Name"));

// Fix: a compiled binding
new Label().Text(Binding.Create(static (ProfileViewModel m) => m.Name));
```

`Binding.Create` with a lambda is turned into a compiled binding by MAUI's source generator.
It needs no reflection.

PM0007 also follows a binding through local variables in the same method, and looks inside a `MultiBinding`:

```csharp
BindingBase b = new Binding("Name");
new Label().Text(b);                                      // PM0007

new Label().Text(new MultiBinding
{
    Bindings = { new Binding("First"), new Binding("Last") }, // PM0007 (once, on the MultiBinding)
    StringFormat = "{0} {1}",
});

var m = new MultiBinding();
m.Bindings.Add(new Binding("Name"));
new Label().Text(m);                                      // PM0007
```

For a local variable, any assignment in the method counts, wherever it is (the check does not track the order of statements).

Limits: PM0007 does not follow fields, properties, method parameters or return values.
It does not check bindings set with `SetBinding(...)`.
