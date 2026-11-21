# Presets and theming

A preset is a reusable look for one control type.
You write it with the same methods you use on controls.
It compiles to one shared MAUI `Style`.

## Writing a preset

```csharp
public static class AppPresets
{
    public static Preset<Entry> Field => Preset.Once<Entry>(static p => p
        .FontSize(Tokens.Body).Height(44).TextColor(Tokens.Text)
        .State(VisualStateManager.CommonStates.Disabled, static s => s.Opacity(0.5)));

    public static Preset<Entry> Numeric => Field.With(static p => p
        .Keyboard(Keyboard.Numeric));
}
```

- `Preset.Once<T>(...)` creates a preset for `T`.
  `T` must derive from `VisualElement`.
- The compiled `Style` is cached per base preset and lambda.
  So a property getter like `Field` returns the same `Style` every time.
- Use `static` lambdas.
  A lambda that captures a variable is a new delegate on every call, so every call builds a new `Style`.
- `.With(...)` derives a new preset.
  It becomes a `Style` with `BasedOn` set to the base.
  Its setters beat the base's setters.

### What a preset can hold

| Call | Effect |
|---|---|
| Any generated method, e.g. `p.FontSize(15)` | A setter. A binding is bound per element. A token follows the theme. |
| `p.State(name, s => ...)` | Setters for a visual state in the `CommonStates` group. |
| `p.State(group, name, s => ...)` | Setters for a visual state in another group. |
| `p.Trigger(property, value, s => ...)` | Setters that apply while `property` equals `value`. |
| `p.DataTrigger(binding, value, s => ...)` | Setters that apply while the binding equals `value`. |
| `p.Set(property, value)` | A setter for any `BindableProperty`. |
| `p.DynamicResource(property, key)` | A setter that follows a resource key. |
| `p.Also(e => ...)` | Code a `Style` cannot hold: events, plain properties, children. |

```csharp
public static Preset<Label> Status => Preset.Once<Label>(static p => p
    .FontSize(13)
    .DataTrigger(Binding.Create(static (ProfileViewModel m) => m.IsBusy), true, static s => s
        .TextColor(Colors.Gray)));

public static Preset<Entry> Required => Field.With(static p => p
    .Trigger(Entry.TextProperty, "", static s => s.BackgroundColor(Colors.MistyRose)));
```

`.Also` runs on the element at `.Use` or `.New`, in order.
It does not run for an implicit default (see below).
`.Also`, `.State`, `.Trigger` and `.DataTrigger` are not allowed inside a state or trigger.

## Using a preset

| Call | Effect |
|---|---|
| `new Entry().Use(AppPresets.Numeric)` | Sets the element's `Style` and runs the preset's `.Also` actions. |
| `AppPresets.Field.New()` | Shorthand for `new Entry().Use(AppPresets.Field)`. |
| `r.Default(AppPresets.Field)` | Makes the preset the implicit style for `Entry` and derived types. |

```csharp
var a = new Entry().Use(AppPresets.Numeric).FontSize(18);   // FontSize 18 wins
var b = AppPresets.Field.New().Placeholder("Name");
```

A preset for a base type works on derived types.
For example, a `Preset<InputView>` can be used on an `Entry`.

### Which value wins

MAUI's style rules decide:

1. Local values beat preset values, in any call order.
   `.FontSize(18).Use(p)` and `.Use(p).FontSize(18)` give the same result.
2. `.State(...)`, `.Trigger(...)` and `.DataTrigger(...)` setters beat local values while they are active.
3. `.With` beats its base.
4. A second `.Use` replaces the first.
5. `.Use` beats the implicit default where both set a property.
   The default's other setters still apply underneath.

One exception to rule 2.
Visual-state setters of an implicit default rank below local values.
XAML implicit styles behave the same way.
So `new Entry().BackgroundColor(Colors.Red)` stays red when disabled, even if the default has a Disabled state.
The same state from `.Use(...)` still wins.

## App-wide defaults

Register defaults once, in your `App` constructor:

```csharp
public class App : Application
{
    public App() =>
        this.UsePresets(r => r
            .UseDefaultLook()
            .Default(AppPresets.Field));
}
```

`UsePresets` does two things.
It sets up live theme tokens (see below).
Then it runs your callback on `app.Resources`.
A plain `new Entry()` anywhere in the app now gets the `Field` look.

A later `.Default` for the same type replaces the earlier one.

Without `UsePresets`, do the same by hand:

```csharp
Theme.Attach(this);
Resources.Default(AppPresets.Field);
```

## The default look

`PureMarkup.Maui.Defaults` ports the stock MAUI template's `Styles.xaml` and `Colors.xaml`.
`r.UseDefaultLook()` registers its presets as implicit styles.
It covers 23 control and page types, with their Disabled states and the Switch On/Off states.

- Each preset is a static property on `DefaultLook`: `DefaultLook.Button`, `DefaultLook.Entry`, and so on.
- `DefaultLook.Headline` and `DefaultLook.SubHeadline` are the template's keyed label styles.
  Use them with `.New()` or `.Use(...)`.
- Colors come from `DefaultColors`.
  The light and dark pairs are tokens in `DefaultTokens`.
- The font is the template's `OpenSansRegular` alias.
  `r.UseDefaultLook(fontFamily: null)` keeps the platform font.

Derive your own looks from it:

```csharp
public static Preset<Button> PrimaryButton => DefaultLook.Button.With(static p => p
    .Font(bold: true));
```

To change a type's look app-wide, register a default after `UseDefaultLook()`.
`r.UseDefaultLook().Default(AppPresets.PrimaryButton)` replaces the Button default.

## Theme tokens

A token is a value with a light and a dark variant:

```csharp
public static class Tokens
{
    public static readonly Token<Color> Text = new(Colors.Black, Colors.White, nameof(Text));
    public static readonly Token<Color> Surface = new(Colors.White, Color.FromArgb("#1E1E1E"), nameof(Surface));
    public static readonly Token<double> Body = new(15, nameof(Body));
}
```

- `new Token<T>(light, dark, name)` has separate values.
- `new Token<T>(value, name)` has one value for both themes, which suits sizes and spacing.
- The name is optional.
  It only makes the resource key easier to read when you debug.

Pass a token to any bindable property method.
It works on elements and in presets:

```csharp
new Label().TextColor(Tokens.Text).BackgroundColor(Tokens.Surface);
```

### How live theme switching works

On net10, MAUI's `AppThemeBinding` is internal.
So PureMarkup cannot use it.
Tokens use dynamic resources instead:

- Each token has a resource key.
- A token on an element or in a preset becomes a dynamic resource reference to that key.
- `UsePresets` (or `Theme.Attach(app)`) writes every token's value for the current theme into `app.Resources`.
- It listens to `Application.RequestedThemeChanged`.
  On a change, it writes the other theme's values.
  MAUI then updates every element that uses them.

Both an OS theme change and `Application.Current.UserAppTheme = AppTheme.Dark` switch the theme live.
Tokens created after `Attach` are written too.

Things to know:

- Call `UsePresets` or `Theme.Attach` in your `App`.
  Without it, the token keys are missing and the properties keep their default values.
- `Theme.Apply(resources, theme)` writes all tokens for one theme into any `ResourceDictionary`.
- On a `BindableObject` that is not an `Element`, a token sets its light value once and does not follow the theme.

## Hot Reload

A Hot Reload update clears the preset cache.
PureMarkup then swaps the recompiled styles into live elements.
This covers implicit defaults and `.Use`.
It tracks elements through weak references.
It is only on when the runtime supports metadata updates.
