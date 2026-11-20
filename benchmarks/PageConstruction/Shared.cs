using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace PageConstruction.Bench;

/// <summary>The values every variant uses, so the pages match.</summary>
internal static class Form
{
    public static readonly string[] Labels = ["Name", "Email", "Phone", "Code"];
    public static readonly string[] Placeholders = ["Full name", "you@example.com", "+1 555 0100", "Enter number"];
    public static readonly Keyboard[] Keyboards = [Keyboard.Text, Keyboard.Email, Keyboard.Telephone, Keyboard.Numeric];

    public static readonly Color Primary = Color.FromArgb("#512BD4");
    public static readonly Color Danger = Colors.Red;

    public static void OnRegister(object? sender, EventArgs e) { }

    public static void OnReset(object? sender, EventArgs e) { }
}
