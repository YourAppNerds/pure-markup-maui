using System.Globalization;
using System.Reflection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using static PureMarkup.Maui.GridRowsColumns;

namespace PureMarkup.Maui.Generators.Tests;

/// <summary>The hand-written sugar layer (src/PureMarkup.Maui/Sugar) on real MAUI objects.</summary>
public class SugarTests
{
    private enum Row { Title, Body, Footer }

    private enum Col { Label, Input }

    [Fact]
    public void SizeSetsRequests()
    {
        var label = new Label().Height(44).Width(120);
        Assert.Equal((120d, 44d), (label.WidthRequest, label.HeightRequest));

        var box = new BoxView().Size(10, 20);
        Assert.Equal((10d, 20d), (box.WidthRequest, box.HeightRequest));

        box.Size(30);
        Assert.Equal((30d, 30d), (box.WidthRequest, box.HeightRequest));
    }

    [Fact]
    public void GeneratedSameNameMethodsStillWork()
    {
        // Height/Width/Size/Center/Fill also exist on non-VisualElement types in the generated layer.
#pragma warning disable CS0618 // ViewCell is obsolete but still generated
        Assert.Equal(50, new ViewCell().Height(50).Height);
#pragma warning restore CS0618
        Assert.Equal(new GridLength(3), new ColumnDefinition().Width(3).Width);
        Assert.Equal(new Point(1, 2), new EllipseGeometry().Center(new Point(1, 2)).Center);
        Assert.Same(Brush.Red, new Rectangle().Fill(Brush.Red).Fill);
    }

    [Fact]
    public void MarginShapes()
    {
        Assert.Equal(new Thickness(5, 6), new Label().Margin(5, 6).Margin);
        Assert.Equal(new Thickness(1, 2, 3, 4), new Label().Margin(1, 2, 3, 4).Margin);
        Assert.Equal(new Thickness(7), new Label().Margin(7).Margin);

        }

    [Fact]
    public void PaddingOnLabel() => Assert.Equal(new Thickness(5, 6), new Label().Padding(5, 6).Padding);

    [Fact]
    public void PaddingOnGrid() => Assert.Equal(new Thickness(1, 2, 3, 4), new Grid().Padding(1, 2, 3, 4).Padding);

    [Fact]
    public void PaddingOnButton() => Assert.Equal(new Thickness(5, 6), new Button().Padding(5, 6).Padding);

    [Fact]
    public void PaddingOnBorder() => Assert.Equal(new Thickness(5, 6), new Border().Padding(5, 6).Padding);

    [Fact]
    public void PaddingOnImageButton() => Assert.Equal(new Thickness(5, 6), new ImageButton().Padding(5, 6).Padding);

    [Fact]
    public void PaddingOnContentPage() => Assert.Equal(new Thickness(5, 6), new ContentPage().Padding(5, 6).Padding);

    [Fact]
    public void PaddingOnScrollView() => Assert.Equal(new Thickness(5, 6), new ScrollView().Padding(5, 6).Padding);

    [Fact]
    public void PaddingOnContentView() => Assert.Equal(new Thickness(5, 6), new ContentView().Padding(5, 6).Padding);

    // new ContentPresenter() hangs without a running MAUI app, so only check that it compiles.
    internal static ContentPresenter PaddingOnContentPresenter(ContentPresenter p) => p.Padding(5, 6).Padding(1, 2, 3, 4);

    [Fact]
    public void AlignmentHelpers()
    {
        var v = new Label().Center();
        Assert.Equal((LayoutOptions.Center, LayoutOptions.Center), (v.HorizontalOptions, v.VerticalOptions));

        v = new Label().Start().Top();
        Assert.Equal((LayoutOptions.Start, LayoutOptions.Start), (v.HorizontalOptions, v.VerticalOptions));

        v = new Label().End().Bottom();
        Assert.Equal((LayoutOptions.End, LayoutOptions.End), (v.HorizontalOptions, v.VerticalOptions));

        v = new Label().Start().Top().FillBoth();
        Assert.Equal((LayoutOptions.Fill, LayoutOptions.Fill), (v.HorizontalOptions, v.VerticalOptions));

        v = new Label().CenterHorizontal();
        Assert.Equal((LayoutOptions.Center, LayoutOptions.Fill), (v.HorizontalOptions, v.VerticalOptions));

        v = new Label().CenterVertical().FillHorizontal();
        Assert.Equal((LayoutOptions.Fill, LayoutOptions.Center), (v.HorizontalOptions, v.VerticalOptions));

        v = new Label().Center().FillVertical();
        Assert.Equal((LayoutOptions.Center, LayoutOptions.Fill), (v.HorizontalOptions, v.VerticalOptions));
    }

    [Fact]
    public void FontSetsOnlyGivenParts()
    {
        var label = new Label().Font(size: 18, bold: true, italic: true, family: "OpenSans");
        Assert.Equal(18, label.FontSize);
        Assert.Equal(FontAttributes.Bold | FontAttributes.Italic, label.FontAttributes);
        Assert.Equal("OpenSans", label.FontFamily);

        var button = new Button().FontFamily("Keep").Font(size: 12);
        Assert.Equal(12, button.FontSize);
        Assert.Equal("Keep", button.FontFamily);
        Assert.Equal(FontAttributes.None, button.FontAttributes);

        Assert.Equal(FontAttributes.Italic, new Span().Font(italic: true).FontAttributes);
        Assert.Equal(20, new Entry().Font(20).FontSize);
    }

    [Fact]
    public void RowsAndColumnsDefine()
    {
        var rows = Rows.Define(Auto, Star, Stars(2), 44);
        Assert.Equal([GridLength.Auto, GridLength.Star, new GridLength(2, GridUnitType.Star), new GridLength(44)],
            rows.Select(r => r.Height));

        var cols = Columns.Define((Col.Label, Auto), (Col.Input, Star));
        Assert.Equal([GridLength.Auto, GridLength.Star], cols.Select(c => c.Width));

        var grid = new Grid().RowDefinitions(Rows.Define((Row.Title, Auto), (Row.Body, Star), (Row.Footer, 30)))
            .ColumnDefinitions(Columns.Define(Auto, Star));
        Assert.Equal(3, grid.RowDefinitions.Count);
        Assert.Equal(2, grid.ColumnDefinitions.Count);
    }

    [Fact]
    public void DefineRejectsEnumMembersOutOfOrder()
    {
        var ex = Assert.Throws<ArgumentException>(() => Rows.Define((Row.Body, Auto), (Row.Title, Star)));
        Assert.Contains("Row.Body", ex.Message);
    }

    [Fact]
    public void EnumRowsAndColumns()
    {
        var label = new Label().Row(Row.Footer).Column(Col.Input);
        Assert.Equal((2, 1), (Grid.GetRow(label), Grid.GetColumn(label)));

        label = new Label().Row(Row.Body, Row.Footer).Column(Col.Label, Col.Input);
        Assert.Equal((1, 2), (Grid.GetRow(label), Grid.GetRowSpan(label)));
        Assert.Equal((0, 2), (Grid.GetColumn(label), Grid.GetColumnSpan(label)));

        // int overloads from the generated layer still bind (literal 0 converts to any enum).
        label = new Label().Row(0).Column(1).RowSpan(3).ColumnSpan(1);
        Assert.Equal((0, 1, 3), (Grid.GetRow(label), Grid.GetColumn(label), Grid.GetRowSpan(label)));
    }

    [Fact]
    public void TapGestureAddsRecognizer()
    {
        var count = 0;
        var label = new Label().TapGesture(() => count++, numberOfTaps: 2);
        var tap = Assert.IsType<TapGestureRecognizer>(Assert.Single(label.GestureRecognizers));
        Assert.Equal(2, tap.NumberOfTapsRequired);

        // SendTapped is internal; tests may reach it.
        var send = typeof(TapGestureRecognizer).GetMethod("SendTapped", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
        send.Invoke(tap, [label, null]);
        Assert.Equal(1, count);

        object? sender = null;
        var span = new Span().TapGesture((s, _) => sender = s);
        send.Invoke(Assert.Single(span.GestureRecognizers), [label, null]);
        Assert.Same(label, sender);
    }

    [Fact]
    public void SwipePanPinchGesturesAddRecognizers()
    {
        var swiped = 0;
        PanUpdatedEventArgs? pan = null;
        PinchGestureUpdatedEventArgs? pinch = null;
        var box = new BoxView()
            .SwipeGesture(SwipeDirection.Left, () => swiped++)
            .SwipeGesture(SwipeDirection.Up, (_, e) => swiped += e.Direction == SwipeDirection.Up ? 10 : 0)
            .PanGesture((_, e) => pan = e, touchPoints: 2)
            .PinchGesture((_, e) => pinch = e);

        Assert.Equal(4, box.GestureRecognizers.Count);
        var left = Assert.IsType<SwipeGestureRecognizer>(box.GestureRecognizers[0]);
        var up = Assert.IsType<SwipeGestureRecognizer>(box.GestureRecognizers[1]);
        Assert.Equal((SwipeDirection.Left, SwipeDirection.Up), (left.Direction, up.Direction));
        left.SendSwiped(box, SwipeDirection.Left);
        up.SendSwiped(box, SwipeDirection.Up);
        Assert.Equal(11, swiped);

        var panRecognizer = Assert.IsType<PanGestureRecognizer>(box.GestureRecognizers[2]);
        Assert.Equal(2, panRecognizer.TouchPoints);
        ((IPanGestureController)panRecognizer).SendPanStarted(box, 7);
        Assert.Equal(GestureStatus.Started, pan?.StatusType);

        var pinchRecognizer = Assert.IsType<PinchGestureRecognizer>(box.GestureRecognizers[3]);
        ((IPinchGestureController)pinchRecognizer).SendPinchStarted(box, Point.Zero);
        Assert.Equal(GestureStatus.Started, pinch?.Status);
    }

    [Fact]
    public void FuncConverterConvertsBothWays()
    {
        var converter = new FuncConverter<int, string>(i => $"#{i}", s => int.Parse(s.TrimStart('#'), CultureInfo.InvariantCulture));
        Assert.Equal("#5", converter.Convert(5, typeof(string), null, CultureInfo.InvariantCulture));
        Assert.Equal("#0", converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture));
        Assert.Equal(7, converter.ConvertBack("#7", typeof(int), null, CultureInfo.InvariantCulture));
        Assert.Throws<ArgumentException>(() => converter.Convert("x", typeof(string), null, CultureInfo.InvariantCulture));

        var oneWay = new FuncConverter<int, string>(i => $"{i}");
        Assert.Throws<NotSupportedException>(() => oneWay.ConvertBack("1", typeof(int), null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void FuncConverterWorksInABinding()
    {
        // Binding.Create needs MAUI's interceptor, which does not run in this project; the
        // converter contract is the same for a path binding.
        // The source is a plain object: a BindableObject source needs a dispatcher here.
        var source = new Model { Code = "41" };
        var target = new Label();
        target.SetBinding(Label.TextProperty, new Binding(nameof(Model.Code), BindingMode.TwoWay, source: source,
            converter: new FuncConverter<string, string>(s => $"[{s}]", s => s.Trim('[', ']'))));
        Assert.Equal("[41]", target.Text);

        target.Text = "[42]";
        Assert.Equal("42", source.Code);
    }

    private sealed class Model
    {
        public string Code { get; set; } = "";
    }
}
