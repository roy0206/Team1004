using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

[InputControlLayout(stateType = typeof(GestureDeviceState), displayName = "Gesture")]
public sealed class GestureDevice : InputDevice
{
    public const string LayoutName = "Gesture";

    public ButtonControl Tap { get; private set; }
    public ButtonControl DoubleTap { get; private set; }
    public ButtonControl LongPress { get; private set; }
    public ButtonControl SwipeUp { get; private set; }
    public ButtonControl SwipeDown { get; private set; }
    public ButtonControl SwipeLeft { get; private set; }
    public ButtonControl SwipeRight { get; private set; }
    public ButtonControl Dragging { get; private set; }
    public AxisControl Pinch { get; private set; }
    public AxisControl Rotate { get; private set; }
    public Vector2Control Drag { get; private set; }
    public Vector2Control Position { get; private set; }

    public static GestureDevice Current => InputSystem.GetDevice(LayoutName) as GestureDevice;

    public static void EnsureLayout()
    {
        if (InputSystem.LoadLayout(LayoutName) == null)
            InputSystem.RegisterLayout<GestureDevice>(LayoutName);
    }

    public static GestureDevice Add()
    {
        EnsureLayout();
        return (GestureDevice)InputSystem.AddDevice(LayoutName);
    }

    protected override void FinishSetup()
    {
        base.FinishSetup();

        Tap = GetChildControl<ButtonControl>("tap");
        DoubleTap = GetChildControl<ButtonControl>("doubleTap");
        LongPress = GetChildControl<ButtonControl>("longPress");
        SwipeUp = GetChildControl<ButtonControl>("swipeUp");
        SwipeDown = GetChildControl<ButtonControl>("swipeDown");
        SwipeLeft = GetChildControl<ButtonControl>("swipeLeft");
        SwipeRight = GetChildControl<ButtonControl>("swipeRight");
        Dragging = GetChildControl<ButtonControl>("dragging");
        Pinch = GetChildControl<AxisControl>("pinch");
        Rotate = GetChildControl<AxisControl>("rotate");
        Drag = GetChildControl<Vector2Control>("drag");
        Position = GetChildControl<Vector2Control>("position");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterOnLoad()
    {
        EnsureLayout();
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterInEditor()
    {
        EnsureLayout();
    }
#endif
}
