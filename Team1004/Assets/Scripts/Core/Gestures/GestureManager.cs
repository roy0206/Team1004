using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public sealed class GestureManager : Singleton<GestureManager>
{
    private GestureSettings settings;
    private bool isInitialized;
    private GestureDevice device;
    private GestureDeviceState deviceState;

    private bool isDragging;
    private bool longPressFired;
    private Vector2 startPosition;
    private float startTime;

    private int pointerCount;
    private int peakPointerCount;
    private GestureSource pointerSource;
    private GesturePointerButtons pointerButtons;
    private Vector2 centroid;
    private float pinchDistance;
    private float pinchAngle;
    private float rotationAccumulator;

    private float lastTapTime = float.NegativeInfinity;
    private Vector2 lastTapPosition;

    public event Action<GestureContext> Tapped;
    public event Action<GestureContext> DoubleTapped;
    public event Action<GestureContext> LongPressed;
    public event Action<GestureContext> DragStarted;
    public event Action<GestureContext> Dragged;
    public event Action<GestureContext> DragEnded;
    public event Action<GestureContext> Swiped;
    public event Action<GestureContext> Pinched;
    public event Action<GestureContext> Rotated;

    public bool IsInitialized => isInitialized;
    public GestureDevice Device => device;
    public bool IsDragging => isDragging;
    public int PointerCount => pointerCount;
    public GestureSource PointerSource => pointerSource;
    public GesturePointerButtons PointerButtons => pointerButtons;
    public Vector2 Position => centroid;
    public Vector2 DragOrigin => startPosition;
    public Vector2 DragDelta => isDragging ? centroid - startPosition : Vector2.zero;

    protected override void OnUnregistering()
    {
        Shutdown();
    }

    public void Initialize(GestureSettings gestureSettings)
    {
        if (gestureSettings == null)
            throw new ArgumentNullException(nameof(gestureSettings));

        Validate(gestureSettings);

        if (isInitialized)
            throw new InvalidOperationException("GestureManager is already initialized.");

        settings = gestureSettings;
        EnhancedTouchSupport.Enable();

        if (gestureSettings.PublishDevice)
            device = GestureDevice.Add();

        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized)
            return;

        var count = ReadPointers(out var position, out var distance, out var angle);

        if (count == 0)
        {
            EndPointers();
            ReadScrollPinch();
            return;
        }

        if (pointerCount == 0)
            BeginPointers(count, position, distance, angle);
        else if (pointerCount != count)
            RebasePointers(count, position, distance, angle);
        else
            ContinuePointers(count, position, distance, angle);

        UpdateLongPress();
        PublishPosition(position);
    }

    private int ReadPointers(out Vector2 position, out float distance, out float angle)
    {
        position = Vector2.zero;
        distance = 0f;
        angle = 0f;

        var touches = Touch.activeTouches;
        var count = touches.Count;

        if (count > 0)
        {
            for (var i = 0; i < count; i++)
                position += touches[i].screenPosition;

            position /= count;

            if (count >= 2)
            {
                var span = touches[1].screenPosition - touches[0].screenPosition;
                distance = span.magnitude;
                angle = Mathf.Atan2(span.y, span.x) * Mathf.Rad2Deg;
            }

            pointerSource = GestureSource.Touch;
            pointerButtons = GesturePointerButtons.None;
            return count;
        }

        var mouse = Mouse.current;
        if (mouse == null)
            return 0;

        var buttons = ReadMouseButtons(mouse);
        if (buttons == GesturePointerButtons.None)
            return 0;

        pointerSource = GestureSource.Mouse;
        pointerButtons = buttons;
        position = mouse.position.ReadValue();
        return 1;
    }

    private GesturePointerButtons ReadMouseButtons(Mouse mouse)
    {
        var allowed = settings.PointerButtons;
        var pressed = GesturePointerButtons.None;

        if ((allowed & GesturePointerButtons.Left) != 0 && mouse.leftButton.isPressed)
            pressed |= GesturePointerButtons.Left;

        if ((allowed & GesturePointerButtons.Right) != 0 && mouse.rightButton.isPressed)
            pressed |= GesturePointerButtons.Right;

        if ((allowed & GesturePointerButtons.Middle) != 0 && mouse.middleButton.isPressed)
            pressed |= GesturePointerButtons.Middle;

        return pressed;
    }

    private void BeginPointers(int count, Vector2 position, float distance, float angle)
    {
        isDragging = false;
        longPressFired = false;
        startPosition = position;
        startTime = Time.unscaledTime;
        peakPointerCount = count;
        rotationAccumulator = 0f;

        SetBaseline(count, position, distance, angle);
    }

    private void RebasePointers(int count, Vector2 position, float distance, float angle)
    {
        peakPointerCount = Mathf.Max(peakPointerCount, count);

        if (!isDragging)
            startPosition = position;

        rotationAccumulator = 0f;
        SetBaseline(count, position, distance, angle);
    }

    private void ContinuePointers(int count, Vector2 position, float distance, float angle)
    {
        if (!isDragging)
        {
            if (Vector2.Distance(position, startPosition) >= settings.DragThreshold)
            {
                isDragging = true;
                DragStarted?.Invoke(Context(count, position));
                PublishButton(GestureButton.Dragging, true);

                var travel = position - startPosition;
                Dragged?.Invoke(Context(count, position, travel, 1f, 0f, 0f));
                PublishDrag(travel);
            }
        }
        else
        {
            var delta = position - centroid;
            if (delta != Vector2.zero)
            {
                Dragged?.Invoke(Context(count, position, delta, 1f, 0f, 0f));
                PublishDrag(delta);
            }
        }

        if (count >= 2)
        {
            RaisePinch(count, position, distance);
            RaiseRotation(count, position, angle);
        }

        SetBaseline(count, position, distance, angle);
    }

    private void EndPointers()
    {
        if (pointerCount == 0)
            return;

        var count = peakPointerCount;
        var duration = Time.unscaledTime - startTime;

        if (isDragging)
        {
            DragEnded?.Invoke(Context(count, centroid));
            PublishButton(GestureButton.Dragging, false);
            RaiseSwipe(count, duration);
        }
        else if (!longPressFired)
        {
            RaiseTap(count, centroid, duration);
        }

        isDragging = false;
        longPressFired = false;
        pointerCount = 0;
        peakPointerCount = 0;
        pinchDistance = 0f;
        pinchAngle = 0f;
    }

    private void UpdateLongPress()
    {
        if (longPressFired || isDragging)
            return;

        if (Time.unscaledTime - startTime < settings.LongPressDuration)
            return;

        longPressFired = true;
        LongPressed?.Invoke(Context(peakPointerCount, centroid));
        PublishPulse(GestureButton.LongPress);
    }

    private void RaiseTap(int count, Vector2 position, float duration)
    {
        Tapped?.Invoke(Context(count, position));
        PublishPulse(GestureButton.Tap);

        if (duration > settings.TapMaxDuration)
        {
            lastTapTime = float.NegativeInfinity;
            return;
        }

        if (Time.unscaledTime - lastTapTime <= settings.DoubleTapMaxInterval &&
            Vector2.Distance(position, lastTapPosition) <= settings.DoubleTapMaxDistance)
        {
            DoubleTapped?.Invoke(Context(count, position));
            PublishPulse(GestureButton.DoubleTap);
            lastTapTime = float.NegativeInfinity;
            return;
        }

        lastTapTime = Time.unscaledTime;
        lastTapPosition = position;
    }

    private void RaiseSwipe(int count, float duration)
    {
        if (duration <= 0f)
            return;

        var travel = centroid - startPosition;
        var distance = travel.magnitude;
        if (distance < settings.SwipeMinDistance)
            return;

        var speed = distance / duration;
        if (speed < settings.SwipeMinSpeed)
            return;

        Swiped?.Invoke(Context(count, centroid, travel, 1f, 0f, speed));
        PublishPulse(SwipeButton(travel));
    }

    private void RaisePinch(int count, Vector2 position, float distance)
    {
        if (pinchDistance <= 0f || distance <= 0f)
            return;

        var scale = distance / pinchDistance;
        if (Mathf.Approximately(scale, 1f))
            return;

        Pinched?.Invoke(Context(count, position, Vector2.zero, scale, 0f, 0f));
        PublishPinch(scale);
    }

    private void RaiseRotation(int count, Vector2 position, float angle)
    {
        rotationAccumulator += Mathf.DeltaAngle(pinchAngle, angle);
        if (Mathf.Abs(rotationAccumulator) < settings.RotationThreshold)
            return;

        var delta = rotationAccumulator;
        rotationAccumulator = 0f;
        Rotated?.Invoke(Context(count, position, Vector2.zero, 1f, delta, 0f));
        PublishRotate(delta);
    }

    private void ReadScrollPinch()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        var scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f))
            return;

        var notches = Mathf.Clamp(scroll, -settings.ScrollPinchMaxNotches, settings.ScrollPinchMaxNotches);
        var scale = Mathf.Pow(settings.ScrollPinchStep, notches);
        Pinched?.Invoke(new GestureContext(
            2,
            mouse.position.ReadValue(),
            Vector2.zero,
            scale,
            0f,
            0f,
            GestureSource.Mouse,
            GesturePointerButtons.None));
        PublishPinch(scale);
    }

    private void SetBaseline(int count, Vector2 position, float distance, float angle)
    {
        pointerCount = count;
        centroid = position;
        pinchDistance = distance;
        pinchAngle = angle;
    }

    private void Shutdown()
    {
        Tapped = null;
        DoubleTapped = null;
        LongPressed = null;
        DragStarted = null;
        Dragged = null;
        DragEnded = null;
        Swiped = null;
        Pinched = null;
        Rotated = null;

        if (!isInitialized)
            return;

        if (device != null && device.added)
            InputSystem.RemoveDevice(device);

        device = null;
        deviceState = default;
        pointerSource = GestureSource.Touch;
        pointerButtons = GesturePointerButtons.None;
        EnhancedTouchSupport.Disable();
        isInitialized = false;
    }

    private void PublishPosition(Vector2 position)
    {
        if (device == null || deviceState.Position == position)
            return;

        deviceState.Position = position;
        Queue(deviceState);
    }

    private void PublishButton(GestureButton button, bool pressed)
    {
        if (device == null)
            return;

        deviceState = deviceState.WithButton(button, pressed);
        Queue(deviceState);
    }

    private void PublishPulse(GestureButton button)
    {
        if (device == null)
            return;

        Queue(deviceState.WithButton(button, true));
        Queue(deviceState);
    }

    private void PublishDrag(Vector2 delta)
    {
        if (device == null)
            return;

        var moved = deviceState;
        moved.Drag = delta;
        Queue(moved);
        Queue(deviceState);
    }

    private void PublishPinch(float scale)
    {
        if (device == null)
            return;

        var pinched = deviceState;
        pinched.Pinch = scale - 1f;
        Queue(pinched);
        Queue(deviceState);
    }

    private void PublishRotate(float delta)
    {
        if (device == null)
            return;

        var rotated = deviceState;
        rotated.Rotate = delta;
        Queue(rotated);
        Queue(deviceState);
    }

    private void Queue(GestureDeviceState state)
    {
        InputSystem.QueueStateEvent(device, state);
    }

    private static GestureButton SwipeButton(Vector2 travel)
    {
        if (Mathf.Abs(travel.x) >= Mathf.Abs(travel.y))
            return travel.x >= 0f ? GestureButton.SwipeRight : GestureButton.SwipeLeft;

        return travel.y >= 0f ? GestureButton.SwipeUp : GestureButton.SwipeDown;
    }

    private GestureContext Context(int count, Vector2 position)
    {
        return Context(count, position, Vector2.zero, 1f, 0f, 0f);
    }

    private GestureContext Context(
        int count,
        Vector2 position,
        Vector2 delta,
        float pinchScale,
        float rotationDelta,
        float speed)
    {
        return new GestureContext(
            count,
            position,
            delta,
            pinchScale,
            rotationDelta,
            speed,
            pointerSource,
            pointerButtons);
    }

    private static void Validate(GestureSettings gestureSettings)
    {
        Require(gestureSettings.DragThreshold > 0f, "Drag threshold must be greater than zero.");
        Require(gestureSettings.TapMaxDuration > 0f, "Tap max duration must be greater than zero.");
        Require(gestureSettings.LongPressDuration > gestureSettings.TapMaxDuration,
            "Long press duration must be greater than tap max duration.");
        Require(gestureSettings.DoubleTapMaxInterval > 0f, "Double tap max interval must be greater than zero.");
        Require(gestureSettings.DoubleTapMaxDistance > 0f, "Double tap max distance must be greater than zero.");
        Require(gestureSettings.SwipeMinDistance > 0f, "Swipe min distance must be greater than zero.");
        Require(gestureSettings.SwipeMinSpeed > 0f, "Swipe min speed must be greater than zero.");
        Require(gestureSettings.RotationThreshold > 0f, "Rotation threshold must be greater than zero.");
        Require(gestureSettings.ScrollPinchStep > 1f, "Scroll pinch step must be greater than one.");
        Require(gestureSettings.ScrollPinchMaxNotches > 0f, "Scroll pinch max notches must be greater than zero.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new ArgumentException(message, "gestureSettings");
    }
}
