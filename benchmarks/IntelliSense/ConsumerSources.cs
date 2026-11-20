namespace PureMarkup.Maui.Benchmarks;

/// <summary>A small but realistic consumer app: a page, a view model, a custom control.</summary>
internal static class ConsumerSources
{
    /// <summary>Marker for the completion caret after <c>new Button().</c>.</summary>
    public const string ButtonCaret = "$BUTTON$";

    /// <summary>Marker for the completion caret on a <c>PresetContext&lt;Button&gt;</c> receiver.</summary>
    public const string PresetCaret = "$PRESET$";

    /// <summary>Marker for the completion caret on an unrelated receiver (<c>string</c>).</summary>
    public const string StringCaret = "$STRING$";

    /// <summary>Replaced on every simulated keystroke (a string literal that grows by one character).</summary>
    public const string Typed = "$TYPED$";

    public const string ViewModel = """
        using System.ComponentModel;
        using System.Runtime.CompilerServices;

        namespace Consumer;

        public sealed class MainViewModel : INotifyPropertyChanged
        {
            string _name = "";
            string _status = "Ready";
            int _rating = 3;

            public event PropertyChangedEventHandler? PropertyChanged;

            public string Name { get => _name; set => Set(ref _name, value); }
            public string Status { get => _status; set => Set(ref _status, value); }
            public int Rating { get => _rating; set => Set(ref _rating, value); }

            public void Save() => Status = $"Saved {Name} ({Rating})";
            public void Reset() { Name = ""; Rating = 3; Status = "Ready"; }

            void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
            {
                if (Equals(field, value)) return;
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
        """;

    public const string RatingView = """
        using Microsoft.Maui.Controls;

        namespace Consumer;

        public class RatingView : ContentView
        {
            public static readonly BindableProperty ValueProperty =
                BindableProperty.Create(nameof(Value), typeof(int), typeof(RatingView), 0);

            public static readonly BindableProperty MaximumProperty =
                BindableProperty.Create(nameof(Maximum), typeof(int), typeof(RatingView), 5);

            public int Value { get => (int)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
            public int Maximum { get => (int)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
        }
        """;

    /// <summary>The page written with PureMarkup. <c>{USINGS}</c> picks the namespace of the generated methods.</summary>
    public const string MarkupPage = """
        using Microsoft.Maui;
        using Microsoft.Maui.Controls;
        using Microsoft.Maui.Graphics;
        {USINGS}

        namespace Consumer;

        public class MainPage : ContentPage
        {
            readonly MainViewModel _vm = new();
            string _typed = "$TYPED$";

            public MainPage()
            {
                this.BindingContext(_vm).Padding(new Thickness(24))
                    .Content(new VerticalStackLayout
                    {
                        Children =
                        {
                            new Label().Text("Profile").FontSize(22).FontAttributes(FontAttributes.Bold),
                            new Entry().Placeholder("Name").Keyboard(Keyboard.Text)
                                .Text(Binding.Create(static (MainViewModel vm) => vm.Name, BindingMode.TwoWay)),
                            new RatingView().Value(Binding.Create(static (MainViewModel vm) => vm.Rating, BindingMode.TwoWay)).Maximum(5),
                            new HorizontalStackLayout
                            {
                                Children =
                                {
                                    new Button().Text("Save").OnClicked((_, _) => _vm.Save()).WidthRequest(120),
                                    new Button().Text("Reset").OnClicked((_, _) => _vm.Reset()).WidthRequest(120),
                                },
                            }.Spacing(12),
                            new Label().Text(Binding.Create(static (MainViewModel vm) => vm.Status)).TextColor(Colors.Gray),
                        },
                    }.Spacing(16));
            }

            void ProbeButton()
            {
                _ = new Button().$BUTTON$;
            }

            void ProbeString()
            {
                _ = "text".$STRING$;
            }

            static void ProbePreset(global::PureMarkup.Maui.PresetContext<Button> p)
            {
                p.$PRESET$;
            }
        }
        """;

    /// <summary>The same page in plain MAUI (object initializers), for the no-PureMarkup baseline.</summary>
    public const string PlainPage = """
        using Microsoft.Maui;
        using Microsoft.Maui.Controls;
        using Microsoft.Maui.Graphics;

        namespace Consumer;

        public class MainPage : ContentPage
        {
            readonly MainViewModel _vm = new();
            string _typed = "$TYPED$";

            public MainPage()
            {
                BindingContext = _vm;
                Padding = new Thickness(24);
                var name = new Entry { Placeholder = "Name", Keyboard = Keyboard.Text };
                name.SetBinding(Entry.TextProperty, Binding.Create(static (MainViewModel vm) => vm.Name, BindingMode.TwoWay));
                var rating = new RatingView { Maximum = 5 };
                rating.SetBinding(RatingView.ValueProperty, Binding.Create(static (MainViewModel vm) => vm.Rating, BindingMode.TwoWay));
                var save = new Button { Text = "Save", WidthRequest = 120 };
                save.Clicked += (_, _) => _vm.Save();
                var reset = new Button { Text = "Reset", WidthRequest = 120 };
                reset.Clicked += (_, _) => _vm.Reset();
                var status = new Label { TextColor = Colors.Gray };
                status.SetBinding(Label.TextProperty, Binding.Create(static (MainViewModel vm) => vm.Status));
                Content = new VerticalStackLayout
                {
                    Spacing = 16,
                    Children =
                    {
                        new Label { Text = "Profile", FontSize = 22, FontAttributes = FontAttributes.Bold },
                        name,
                        rating,
                        new HorizontalStackLayout { Spacing = 12, Children = { save, reset } },
                        status,
                    },
                };
            }

            void ProbeButton()
            {
                _ = new Button().$BUTTON$;
            }

            void ProbeString()
            {
                _ = "text".$STRING$;
            }
        }
        """;
}
