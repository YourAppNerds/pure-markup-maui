// Shape adapted from CommunityToolkit.Maui.Markup (GestureExtensions), MIT;
// see THIRD-PARTY-NOTICES.md.
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace PureMarkup.Maui;

/// <summary>
/// Adds gesture recognizers to views and spans. Named <c>...Gesture</c> rather than
/// <c>On...</c>: the generated layer reserves <c>On&lt;Event&gt;</c> for real events.
/// </summary>
public static class GestureSugar
{
    /// <summary>Adds a <see cref="TapGestureRecognizer"/> that calls <paramref name="handler"/>.</summary>
    public static T TapGesture<T>(this T self, Action handler, int numberOfTaps = 1) where T : IGestureRecognizers =>
        self.TapGesture((_, _) => handler(), numberOfTaps);

    /// <summary>Adds a <see cref="TapGestureRecognizer"/> with a <see cref="TapGestureRecognizer.Tapped"/> handler.</summary>
    public static T TapGesture<T>(this T self, EventHandler<TappedEventArgs> handler, int numberOfTaps = 1) where T : IGestureRecognizers
    {
        var tap = new TapGestureRecognizer { NumberOfTapsRequired = numberOfTaps };
        tap.Tapped += handler;
        self.GestureRecognizers.Add(tap);
        return self;
    }

    /// <summary>Adds a <see cref="SwipeGestureRecognizer"/> for <paramref name="direction"/>.</summary>
    public static T SwipeGesture<T>(this T self, SwipeDirection direction, Action handler) where T : IGestureRecognizers =>
        self.SwipeGesture(direction, (_, _) => handler());

    /// <summary>Adds a <see cref="SwipeGestureRecognizer"/> with a <see cref="SwipeGestureRecognizer.Swiped"/> handler.</summary>
    public static T SwipeGesture<T>(this T self, SwipeDirection direction, EventHandler<SwipedEventArgs> handler) where T : IGestureRecognizers
    {
        var swipe = new SwipeGestureRecognizer { Direction = direction };
        swipe.Swiped += handler;
        self.GestureRecognizers.Add(swipe);
        return self;
    }

    /// <summary>Adds a <see cref="PanGestureRecognizer"/> with a <see cref="PanGestureRecognizer.PanUpdated"/> handler.</summary>
    public static T PanGesture<T>(this T self, EventHandler<PanUpdatedEventArgs> handler, int touchPoints = 1) where T : IGestureRecognizers
    {
        var pan = new PanGestureRecognizer { TouchPoints = touchPoints };
        pan.PanUpdated += handler;
        self.GestureRecognizers.Add(pan);
        return self;
    }

    /// <summary>Adds a <see cref="PinchGestureRecognizer"/> with a <see cref="PinchGestureRecognizer.PinchUpdated"/> handler.</summary>
    public static T PinchGesture<T>(this T self, EventHandler<PinchGestureUpdatedEventArgs> handler) where T : IGestureRecognizers
    {
        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += handler;
        self.GestureRecognizers.Add(pinch);
        return self;
    }
}
