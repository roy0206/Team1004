using System;

[Flags]
public enum ModuleTick
{
    None = 0,
    Update = 1 << 0,
    LateUpdate = 1 << 1,
    FixedUpdate = 1 << 2,
    Interval = 1 << 3
}
