using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public struct DungeonInputFrame
{
    public Vector2 Move;
    public bool Confirm, Restart, Menu, Back, Quit, Attack;
    public int DifficultyKey; // 0 = none, 1..3 = existing keyboard shortcuts.
}

public static class DungeonInput
{
    public static DungeonInputFrame Read()
    {
        if (!Application.isFocused) return default;
        var frame = ReadGamepads(Gamepad.all);
        var keyboard = new Vector2(
            (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
            (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0));
        frame.Move = CombineMovement(keyboard, frame.Move);
        frame.Restart |= Input.GetKeyDown(KeyCode.R);
        frame.Menu |= Input.GetKeyDown(KeyCode.N);
        frame.Quit = Input.GetKeyDown(KeyCode.Escape);
        frame.Confirm |= Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
        frame.Attack |= Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J);
        frame.DifficultyKey = Input.GetKeyDown(KeyCode.Alpha1) ? 1 : Input.GetKeyDown(KeyCode.Alpha2) ? 2 : Input.GetKeyDown(KeyCode.Alpha3) ? 3 : 0;
        return frame;
    }

    // Enumerate connected devices every frame: unplugging clears movement immediately,
    // and reconnecting does not retain a stale device or held-button state.
    public static DungeonInputFrame ReadGamepads(IEnumerable<Gamepad> devices)
    {
        var frame = new DungeonInputFrame();
        foreach (var pad in devices) {
            if (!pad.added || !pad.enabled) continue;
            var stick = DeadZone(pad.leftStick.ReadUnprocessedValue());
            var dpad = Vector2.ClampMagnitude(pad.dpad.ReadValue(), 1);
            var move = dpad.sqrMagnitude > 0 ? dpad : stick;
            if (move.sqrMagnitude > frame.Move.sqrMagnitude) frame.Move = move;
            frame.Confirm |= pad.buttonSouth.wasPressedThisFrame;
            frame.Restart |= pad.buttonNorth.wasPressedThisFrame;
            frame.Menu |= pad.startButton.wasPressedThisFrame;
            frame.Back |= pad.buttonEast.wasPressedThisFrame;
            frame.Attack |= pad.buttonWest.wasPressedThisFrame;
        }
        return frame;
    }
    public static Vector2 DeadZone(Vector2 value)
    {
        const float dead = .22f;
        float magnitude = value.magnitude;
        return magnitude <= dead ? Vector2.zero : value / magnitude * Mathf.Clamp01((magnitude - dead) / (1 - dead));
    }
    public static Vector2 CombineMovement(Vector2 keyboard, Vector2 gamepad)
    {
        // A keyboard direction always remains usable even when a controller is held.
        return keyboard.sqrMagnitude > 0 ? Vector2.ClampMagnitude(keyboard, 1) : Vector2.ClampMagnitude(gamepad, 1);
    }
}

public sealed class MenuNavigation
{
    int held;
    float repeatAt;
    public void Reset() { held = 0; repeatAt = 0; }
    public int Step(float axis, float now)
    {
        if (Mathf.Abs(axis) < .3f) { Reset(); return 0; }
        int direction = axis > .55f ? 1 : axis < -.55f ? -1 : 0;
        if (direction == 0) return 0;
        if (direction != held) { held = direction; repeatAt = now + .4f; return direction; }
        if (now < repeatAt) return 0;
        repeatAt = now + .17f; return direction;
    }
}
