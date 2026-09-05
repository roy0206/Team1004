using UnityEngine;

public interface ISceneTransition
{
    Awaitable CoverAsync(float duration);
    Awaitable RevealAsync(float duration);
}
