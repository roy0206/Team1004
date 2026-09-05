using UnityEngine;

public enum GestureSource
{
    Touch = 0,
    Mouse = 1
}

public readonly struct GestureContext
{
    public int PointerCount { get; }
    public Vector2 Position { get; }
    public Vector2 Delta { get; }
    public float PinchScale { get; }
    public float RotationDelta { get; }
    public float Speed { get; }
    public GestureSource Source { get; }
    public GesturePointerButtons Buttons { get; }

    public GestureContext(
        int pointerCount,
        Vector2 position,
        Vector2 delta,
        float pinchScale,
        float rotationDelta,
        float speed,
        GestureSource source,
        GesturePointerButtons buttons)
    {
        PointerCount = pointerCount;
        Position = position;
        Delta = delta;
        PinchScale = pinchScale;
        RotationDelta = rotationDelta;
        Speed = speed;
        Source = source;
        Buttons = buttons;
    }
}
