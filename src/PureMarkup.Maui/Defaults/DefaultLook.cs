using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using static Microsoft.Maui.Controls.VisualStateManager.CommonStates;
using T = PureMarkup.Maui.Defaults.DefaultTokens;

namespace PureMarkup.Maui.Defaults;

/// <summary>
/// The look of the .NET MAUI app template (<c>Resources/Styles/Styles.xaml</c> in <c>maui-mobile</c>,
/// without the sample-content variant) as presets, so a XAML-free app starts from the same defaults.
/// Register all of them with <see cref="UseDefaultLook"/>; derive your own with
/// <c>DefaultLook.Button.With(...)</c>. Not ported: styles for non-visual types (SearchHandler,
/// Shadow, SwipeItem) and the commented-out TitleBar style.
/// </summary>
public static class DefaultLook
{
    /// <summary>Resource key of the font family every text preset reads (dynamic resource).</summary>
    public const string FontFamilyKey = "PureMarkup.Defaults.FontFamily";

    /// <summary>The template's font alias (registered in its MauiProgram as <c>OpenSans-Regular.ttf</c>).</summary>
    public const string TemplateFontFamily = "OpenSansRegular";

    /// <summary>
    /// Registers every preset of the default look as an implicit style in <paramref name="resources"/>
    /// and sets the font family (<see langword="null"/> keeps the platform font). Call inside
    /// <c>app.UsePresets(r => r.UseDefaultLook())</c> so the tokens follow the theme. A later
    /// <c>.Default(...)</c> for the same type replaces that type's default look.
    /// </summary>
    public static ResourceDictionary UseDefaultLook(this ResourceDictionary resources, string? fontFamily = TemplateFontFamily)
    {
        ArgumentNullException.ThrowIfNull(resources);
        if (fontFamily is not null)
        {
            resources[FontFamilyKey] = fontFamily;
        }

        return resources
            .Default(ActivityIndicator).Default(IndicatorView).Default(Border).Default(BoxView)
            .Default(Button).Default(CheckBox).Default(DatePicker).Default(Editor).Default(Entry)
            .Default(ImageButton).Default(Label).Default(Picker).Default(ProgressBar).Default(RadioButton)
            .Default(RefreshView).Default(SearchBar).Default(Slider).Default(Switch).Default(TimePicker)
            .Default(Page).Default(Shell).Default(NavigationPage).Default(TabbedPage);
    }

    /// <summary>ActivityIndicator: accent color.</summary>
    public static Preset<ActivityIndicator> ActivityIndicator => Preset.Once<ActivityIndicator>(static p => p
        .Color(T.Accent));

    /// <summary>IndicatorView: muted dots, strong selected dot.</summary>
    public static Preset<IndicatorView> IndicatorView => Preset.Once<IndicatorView>(static p => p
        .IndicatorColor(T.Muted).SelectedIndicatorColor(T.SelectedIndicator));

    /// <summary>Border: 1px muted rectangle.</summary>
    public static Preset<Border> Border => Preset.Once<Border>(static p => p
        .Stroke(MutedStroke).StrokeShape(new Rectangle()).StrokeThickness(1));

    // Border.Stroke is a Brush; the template converts the Muted colors.
    private static readonly Token<Brush> MutedStroke =
        new(new SolidColorBrush(DefaultColors.Gray200), new SolidColorBrush(DefaultColors.Gray500), "Default.MutedStroke");

    /// <summary>BoxView: strong fill.</summary>
    public static Preset<BoxView> BoxView => Preset.Once<BoxView>(static p => p
        .BackgroundColor(T.Strong));

    /// <summary>Button: primary fill, 8px corners, 44px minimum; Disabled greys out.</summary>
    public static Preset<Button> Button => Preset.Once<Button>(static p => p
        .TextColor(T.ButtonText).BackgroundColor(T.ButtonBackground).Font()
        .FontSize(14).BorderWidth(0).CornerRadius(8).Padding(new Thickness(14, 10)).MinSize()
        .State(Disabled, static s => s.TextColor(T.Strong).BackgroundColor(T.Track)));

    /// <summary>CheckBox: accent color; Disabled greys out.</summary>
    public static Preset<CheckBox> CheckBox => Preset.Once<CheckBox>(static p => p
        .Color(T.Accent).MinSize()
        .State(Disabled, static s => s.Color(T.Disabled)));

    /// <summary>DatePicker: transparent, 14pt; Disabled mutes the text.</summary>
    public static Preset<DatePicker> DatePicker => Preset.Once<DatePicker>(static p => p
        .TextColor(T.PickerText).BackgroundColor(Colors.Transparent).Font().FontSize(14).MinSize()
        .State(Disabled, static s => s.TextColor(T.Muted)));

    /// <summary>Editor: transparent, 14pt, muted placeholder; Disabled greys out.</summary>
    public static Preset<Editor> Editor => Preset.Once<Editor>(static p => p
        .TextColor(T.Text).BackgroundColor(Colors.Transparent).Font().FontSize(14)
        .PlaceholderColor(T.Muted).MinSize()
        .State(Disabled, static s => s.TextColor(T.Disabled)));

    /// <summary>Entry: transparent, 14pt, muted placeholder; Disabled greys out.</summary>
    public static Preset<Entry> Entry => Preset.Once<Entry>(static p => p
        .TextColor(T.Text).BackgroundColor(Colors.Transparent).Font().FontSize(14)
        .PlaceholderColor(T.Muted).MinSize()
        .State(Disabled, static s => s.TextColor(T.Disabled)));

    /// <summary>ImageButton: no border; Disabled at half opacity.</summary>
    public static Preset<ImageButton> ImageButton => Preset.Once<ImageButton>(static p => p
        .Opacity(1).BorderColor(Colors.Transparent).BorderWidth(0).CornerRadius(0).MinSize()
        .State(Disabled, static s => s.Opacity(0.5)));

    /// <summary>Label: 14pt text; Disabled greys out.</summary>
    public static Preset<Label> Label => Preset.Once<Label>(static p => p
        .TextColor(T.Text).BackgroundColor(Colors.Transparent).Font().FontSize(14)
        .State(Disabled, static s => s.TextColor(T.Disabled)));

    /// <summary>The template's keyed <c>Headline</c> label style: 32pt, centered.</summary>
    public static Preset<Label> Headline => Preset.Once<Label>(static p => p
        .TextColor(T.Headline).FontSize(32)
        .HorizontalOptions(LayoutOptions.Center).HorizontalTextAlignment(TextAlignment.Center));

    /// <summary>The template's keyed <c>SubHeadline</c> label style: 24pt, centered.</summary>
    public static Preset<Label> SubHeadline => Preset.Once<Label>(static p => p
        .TextColor(T.Headline).FontSize(24)
        .HorizontalOptions(LayoutOptions.Center).HorizontalTextAlignment(TextAlignment.Center));

    /// <summary>Picker: transparent, 14pt; Disabled greys out text and title.</summary>
    public static Preset<Picker> Picker => Preset.Once<Picker>(static p => p
        .TextColor(T.PickerText).TitleColor(T.Secondary).BackgroundColor(Colors.Transparent)
        .Font().FontSize(14).MinSize()
        .State(Disabled, static s => s.TextColor(T.Disabled).TitleColor(T.Disabled)));

    /// <summary>ProgressBar: accent progress; Disabled greys out.</summary>
    public static Preset<ProgressBar> ProgressBar => Preset.Once<ProgressBar>(static p => p
        .ProgressColor(T.Accent)
        .State(Disabled, static s => s.ProgressColor(T.Disabled)));

    /// <summary>RadioButton: 14pt text; Disabled greys out.</summary>
    public static Preset<RadioButton> RadioButton => Preset.Once<RadioButton>(static p => p
        .BackgroundColor(Colors.Transparent).TextColor(T.Text).Font().FontSize(14).MinSize()
        .State(Disabled, static s => s.TextColor(T.Disabled)));

    /// <summary>RefreshView: secondary spinner.</summary>
    public static Preset<RefreshView> RefreshView => Preset.Once<RefreshView>(static p => p
        .RefreshColor(T.Secondary));

    /// <summary>SearchBar: transparent, 14pt, Gray500 placeholder; Disabled greys out.</summary>
    public static Preset<SearchBar> SearchBar => Preset.Once<SearchBar>(static p => p
        .TextColor(T.PickerText).PlaceholderColor(DefaultColors.Gray500).CancelButtonColor(DefaultColors.Gray500)
        .BackgroundColor(Colors.Transparent).Font().FontSize(14).MinSize()
        .State(Disabled, static s => s.TextColor(T.Disabled).PlaceholderColor(T.Disabled)));

    /// <summary>Slider: accent track and thumb; Disabled greys out.</summary>
    public static Preset<Slider> Slider => Preset.Once<Slider>(static p => p
        .MinimumTrackColor(T.Accent).MaximumTrackColor(T.Track).ThumbColor(T.Accent)
        .State(Disabled, static s => s.MinimumTrackColor(T.Disabled).MaximumTrackColor(T.Disabled).ThumbColor(T.Disabled)));

    /// <summary>Switch: accent; On, Off and Disabled states as in the template.</summary>
    public static Preset<Switch> Switch => Preset.Once<Switch>(static p => p
        .OnColor(T.Accent).ThumbColor(DefaultColors.White)
        .State(Disabled, static s => s.OnColor(T.Disabled).ThumbColor(T.Disabled))
        .State(Microsoft.Maui.Controls.Switch.SwitchOnVisualState, static s => s.OnColor(T.SwitchOnTrack).ThumbColor(T.Accent))
        .State(Microsoft.Maui.Controls.Switch.SwitchOffVisualState, static s => s.ThumbColor(T.SwitchOffThumb)));

    /// <summary>TimePicker: transparent, 14pt; Disabled greys out.</summary>
    public static Preset<TimePicker> TimePicker => Preset.Once<TimePicker>(static p => p
        .TextColor(T.PickerText).BackgroundColor(Colors.Transparent).Font().FontSize(14).MinSize()
        .State(Disabled, static s => s.TextColor(T.Disabled)));

    /// <summary>Every page: no padding, white / off-black background.</summary>
    public static Preset<Page> Page => Preset.Once<Page>(static p => p
        .Padding(new Thickness(0)).BackgroundColor(T.PageBackground));

    /// <summary>Shell: the template's Shell colors.</summary>
    public static Preset<Shell> Shell => Preset.Once<Shell>(static p => p
        .Set(Microsoft.Maui.Controls.Shell.BackgroundColorProperty, T.PageBackground)
        .Set(Microsoft.Maui.Controls.Shell.ForegroundColorProperty, T.ShellForeground)
        .Set(Microsoft.Maui.Controls.Shell.TitleColorProperty, T.ShellForeground)
        .Set(Microsoft.Maui.Controls.Shell.DisabledColorProperty, T.Inactive)
        .Set(Microsoft.Maui.Controls.Shell.UnselectedColorProperty, DefaultColors.Gray200)
        .Set(Microsoft.Maui.Controls.Shell.NavBarHasShadowProperty, false)
        .Set(Microsoft.Maui.Controls.Shell.TabBarBackgroundColorProperty, T.TabBarBackground)
        .Set(Microsoft.Maui.Controls.Shell.TabBarForegroundColorProperty, T.TabBarForeground)
        .Set(Microsoft.Maui.Controls.Shell.TabBarTitleColorProperty, T.TabBarForeground)
        .Set(Microsoft.Maui.Controls.Shell.TabBarUnselectedColorProperty, T.Secondary));

    /// <summary>NavigationPage: bar colors.</summary>
    public static Preset<NavigationPage> NavigationPage => Preset.Once<NavigationPage>(static p => p
        .BarBackgroundColor(T.PageBackground).BarTextColor(T.NavigationBarText).IconColor(T.NavigationBarText));

    /// <summary>TabbedPage: bar and tab colors.</summary>
    public static Preset<TabbedPage> TabbedPage => Preset.Once<TabbedPage>(static p => p
        .BarBackgroundColor(T.TabbedBarBackground).BarTextColor(T.TabBarForeground)
        .UnselectedTabColor(T.Inactive).SelectedTabColor(T.Strong));

    private static PresetContext<TView> Font<TView>(this PresetContext<TView> self) =>
        self.DynamicResource(Microsoft.Maui.Controls.Label.FontFamilyProperty, FontFamilyKey);

    private static PresetContext<TView> MinSize<TView>(this PresetContext<TView> self) where TView : VisualElement =>
        self.MinimumHeightRequest(44).MinimumWidthRequest(44);
}
