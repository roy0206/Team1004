using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

public enum GestureButton
{
    Tap = 0,
    DoubleTap = 1,
    LongPress = 2,
    SwipeUp = 3,
    SwipeDown = 4,
    SwipeLeft = 5,
    SwipeRight = 6,
    Dragging = 7
}

[StructLayout(LayoutKind.Explicit, Size = Size)]
public struct GestureDeviceState : IInputStateTypeInfo
{
    private const int ButtonsOffset = 0;
    private const int PinchOffset = 4;
    private const int RotateOffset = 8;
    private const int DragOffset = 12;
    private const int PositionOffset = 20;
    private const int Size = 28;

    [FieldOffset(ButtonsOffset)] private uint buttons;
    [FieldOffset(PinchOffset)] private float pinch;
    [FieldOffset(RotateOffset)] private float rotate;
    [FieldOffset(DragOffset)] private Vector2 drag;
    [FieldOffset(PositionOffset)] private Vector2 position;

    public static FourCC Format => new('G', 'E', 'S', 'T');

    [InputControl(name = "tap", layout = "Button", offset = ButtonsOffset, bit = (int)GestureButton.Tap)]
    [InputControl(name = "doubleTap", layout = "Button", offset = ButtonsOffset, bit = (int)GestureButton.DoubleTap)]
    [InputControl(name = "longPress", layout = "Button", offset = ButtonsOffset, bit = (int)GestureButton.LongPress)]
    [InputControl(name = "swipeUp", layout = "Button", offset = ButtonsOffset, bit = (int)GestureButton.SwipeUp)]
    [InputControl(name = "swipeDown", layout = "Button", offset = ButtonsOffset, bit = (int)GestureButton.SwipeDown)]
    [InputControl(name = "swipeLeft", layout = "Button", offset = ButtonsOffset, bit = (int)GestureButton.SwipeLeft)]
    [InputControl(name = "swipeRight", layout = "Button", offset = ButtonsOffset, bit = (int)GestureButton.SwipeRight)]
    [InputControl(name = "dragging", layout = "Button", offset = ButtonsOffset, bit = (int)GestureButton.Dragging)]
    public uint Buttons
    {
        get => buttons;
        set => buttons = value;
    }

    [InputControl(name = "pinch", layout = "Axis", offset = PinchOffset)]
    public float Pinch
    {
        get => pinch;
        set => pinch = value;
    }

    [InputControl(name = "rotate", layout = "Axis", offset = RotateOffset)]
    public float Rotate
    {
        get => rotate;
        set => rotate = value;
    }

    [InputControl(name = "drag", layout = "Vector2", offset = DragOffset)]
    public Vector2 Drag
    {
        get => drag;
        set => drag = value;
    }

    [InputControl(name = "position", layout = "Vector2", offset = PositionOffset)]
    public Vector2 Position
    {
        get => position;
        set => position = value;
    }

    FourCC IInputStateTypeInfo.format => Format;

    public bool IsPressed(GestureButton button)
    {
        return (buttons & (1u << (int)button)) != 0;
    }

    public GestureDeviceState WithButton(GestureButton button, bool pressed)
    {
        var mask = 1u << (int)button;
        var copy = this;
        copy.buttons = pressed ? copy.buttons | mask : copy.buttons & ~mask;
        return copy;
    }
}
