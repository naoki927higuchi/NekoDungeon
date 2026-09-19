using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class DungeonInputTests
{
    static void Check(bool value, string message) { if(!value) throw new Exception(message); }
    public static void Run()
    {
        Gamepad pad = null;
        var mode = InputSystem.settings.updateMode;
        try {
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            pad = InputSystem.AddDevice<Gamepad>();
            var devices = new[] { pad };
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(.1f,.1f) }); InputSystem.Update();
            Check(DungeonInput.ReadGamepads(devices).Move == Vector2.zero, "Stick drift not filtered");
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.up }); InputSystem.Update();
            Check(DungeonInput.ReadGamepads(devices).Move == Vector2.up, "Stick up mapped incorrectly");
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(.6f,0) }); InputSystem.Update();
            var slow = DungeonInput.ReadGamepads(devices).Move.x;
            Check(slow > 0 && slow < 1, "Analog magnitude lost");
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.DpadLeft).WithButton(GamepadButton.DpadUp)); InputSystem.Update();
            var diagonal = DungeonInput.ReadGamepads(devices).Move;
            Check(diagonal.x < 0 && diagonal.y > 0 && diagonal.magnitude <= 1.001f, "Dpad diagonal speed incorrect");
            foreach(var button in new[] { GamepadButton.South, GamepadButton.North, GamepadButton.Start, GamepadButton.East }) {
                InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update();
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button)); InputSystem.Update();
                var frame = DungeonInput.ReadGamepads(devices);
                Check(frame.Confirm == (button == GamepadButton.South) && frame.Restart == (button == GamepadButton.North)
                    && frame.Menu == (button == GamepadButton.Start) && frame.Back == (button == GamepadButton.East), "Button mapping incorrect");
                InputSystem.Update(); frame = DungeonInput.ReadGamepads(devices);
                Check(!frame.Confirm && !frame.Restart && !frame.Menu && !frame.Back, "Held button repeated action");
            }
            Check(DungeonInput.CombineMovement(Vector2.right,Vector2.left)==Vector2.right, "Keyboard blocked by controller");
            Check(Mathf.Abs(DungeonInput.CombineMovement(Vector2.one,Vector2.zero).magnitude-1)<.001f, "Keyboard diagonal faster");
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick=Vector2.right }); InputSystem.Update();
            InputSystem.RemoveDevice(pad);
            Check(DungeonInput.ReadGamepads(devices).Move==Vector2.zero, "Disconnected device still moved player");
            InputSystem.AddDevice(pad);
            InputSystem.QueueStateEvent(pad,new GamepadState()); InputSystem.Update();
            Check(DungeonInput.ReadGamepads(devices).Move==Vector2.zero, "Reconnection drift");
            InputSystem.QueueStateEvent(pad,new GamepadState { leftStick=Vector2.left }); InputSystem.Update();
            Check(DungeonInput.ReadGamepads(devices).Move==Vector2.left, "Reconnected controller unusable");
            var nav = new MenuNavigation();
            Check(nav.Step(.1f,0)==0 && nav.Step(1,0)==1 && nav.Step(1,.1f)==0 && nav.Step(1,.41f)==1, "Menu debounce/repeat failed");
            Check(nav.Step(-1,.42f)==-1, "Menu direction change failed");
            nav.Step(0,.43f); Check(nav.Step(-1,.44f)==-1, "Menu release failed");
            nav.Reset(); Check(nav.Step(1,.45f)==1, "Menu reset failed");
        } finally {
            if(pad!=null && pad.added) InputSystem.RemoveDevice(pad);
            InputSystem.settings.updateMode = mode;
        }
    }
}
