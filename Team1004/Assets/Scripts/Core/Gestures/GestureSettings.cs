using System;

[Flags]
public enum GesturePointerButtons
{
    None = 0,
    Left = 1,
    Right = 2,
    Middle = 4
}

public sealed class GestureSettings
{
    public float DragThreshold { get; set; } = 10f;
    public float TapMaxDuration { get; set; } = 0.2f;
    public float LongPressDuration { get; set; } = 0.5f;
    public float DoubleTapMaxInterval { get; set; } = 0.3f;
    public float DoubleTapMaxDistance { get; set; } = 40f;
    public float SwipeMinDistance { get; set; } = 50f;
    public float SwipeMinSpeed { get; set; } = 300f;
    public float RotationThreshold { get; set; } = 0.5f;
    public float ScrollPinchStep { get; set; } = 1.1f;
    public float ScrollPinchMaxNotches { get; set; } = 3f;
    public GesturePointerButtons PointerButtons { get; set; } = GesturePointerButtons.Left;
    public bool PublishDevice { get; set; } = true;
}
