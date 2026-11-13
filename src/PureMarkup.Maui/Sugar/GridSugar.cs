// Shape adapted from CommunityToolkit.Maui.Markup (GridRowsColumns, GridExtensions), MIT;
// see THIRD-PARTY-NOTICES.md.
using System.Globalization;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace PureMarkup.Maui;

/// <summary>
/// Row and column definitions in one call. Add <c>using static PureMarkup.Maui.GridRowsColumns;</c>
/// to write <c>Rows.Define(Auto, Star, Stars(2), 44)</c>.
/// </summary>
public static class GridRowsColumns
{
    /// <summary><see cref="GridLength.Auto"/>.</summary>
    public static GridLength Auto => GridLength.Auto;

    /// <summary><see cref="GridLength.Star"/> (one star).</summary>
    public static GridLength Star => GridLength.Star;

    /// <summary>A star length with the given weight.</summary>
    public static GridLength Stars(double value) => new(value, GridUnitType.Star);

    /// <summary>Builds a <see cref="RowDefinitionCollection"/>.</summary>
    public static class Rows
    {
        /// <summary>One row per height, in order. Plain numbers are absolute heights.</summary>
        public static RowDefinitionCollection Define(params GridLength[] heights) =>
            new([.. heights.Select(h => new RowDefinition(h))]);

        /// <summary>
        /// One row per enum member; the members must be listed in order, starting at 0
        /// (<c>(Row.Title, Auto), (Row.Body, Star)</c>).
        /// </summary>
        public static RowDefinitionCollection Define<TRow>(params (TRow Row, GridLength Height)[] rows)
            where TRow : struct, Enum =>
            new([.. rows.Select((r, i) => new RowDefinition(Checked(r.Height, r.Row, i)))]);
    }

    /// <summary>Builds a <see cref="ColumnDefinitionCollection"/>.</summary>
    public static class Columns
    {
        /// <summary>One column per width, in order. Plain numbers are absolute widths.</summary>
        public static ColumnDefinitionCollection Define(params GridLength[] widths) =>
            new([.. widths.Select(w => new ColumnDefinition(w))]);

        /// <summary>
        /// One column per enum member; the members must be listed in order, starting at 0
        /// (<c>(Col.Label, Auto), (Col.Input, Star)</c>).
        /// </summary>
        public static ColumnDefinitionCollection Define<TColumn>(params (TColumn Column, GridLength Width)[] columns)
            where TColumn : struct, Enum =>
            new([.. columns.Select((c, i) => new ColumnDefinition(Checked(c.Width, c.Column, i)))]);
    }

    internal static int ToIndex<TEnum>(TEnum value) where TEnum : struct, Enum =>
        Convert.ToInt32(value, CultureInfo.InvariantCulture);

    private static GridLength Checked<TEnum>(GridLength length, TEnum member, int index) where TEnum : struct, Enum =>
        ToIndex(member) == index
            ? length
            : throw new ArgumentException($"{typeof(TEnum).Name}.{member} has value {ToIndex(member)} but is listed at position {index}.");
}

/// <summary>Grid attached properties from enum members (<c>.Row(Row.Body)</c>, <c>.Column(Col.Input)</c>).</summary>
public static class GridEnumSugar
{
    /// <summary>Sets <see cref="Grid.RowProperty"/> to the enum member's value.</summary>
    public static T Row<T, TRow>(this T self, TRow row) where T : BindableObject where TRow : struct, Enum
    {
        Grid.SetRow(self, GridRowsColumns.ToIndex(row));
        return self;
    }

    /// <summary>Sets the row to <paramref name="first"/> and the row span to reach <paramref name="last"/>.</summary>
    public static T Row<T, TRow>(this T self, TRow first, TRow last) where T : BindableObject where TRow : struct, Enum
    {
        var start = GridRowsColumns.ToIndex(first);
        Grid.SetRow(self, start);
        Grid.SetRowSpan(self, GridRowsColumns.ToIndex(last) - start + 1);
        return self;
    }

    /// <summary>Sets <see cref="Grid.ColumnProperty"/> to the enum member's value.</summary>
    public static T Column<T, TColumn>(this T self, TColumn column) where T : BindableObject where TColumn : struct, Enum
    {
        Grid.SetColumn(self, GridRowsColumns.ToIndex(column));
        return self;
    }

    /// <summary>Sets the column to <paramref name="first"/> and the column span to reach <paramref name="last"/>.</summary>
    public static T Column<T, TColumn>(this T self, TColumn first, TColumn last) where T : BindableObject where TColumn : struct, Enum
    {
        var start = GridRowsColumns.ToIndex(first);
        Grid.SetColumn(self, start);
        Grid.SetColumnSpan(self, GridRowsColumns.ToIndex(last) - start + 1);
        return self;
    }
}
