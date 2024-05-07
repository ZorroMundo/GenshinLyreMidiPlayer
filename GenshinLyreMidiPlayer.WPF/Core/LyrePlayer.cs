using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GenshinLyreMidiPlayer.Data.Entities;
using SimWinInput;
using WindowsInput;
using WindowsInput.Native;
using static GenshinLyreMidiPlayer.WPF.Core.Keyboard;

namespace GenshinLyreMidiPlayer.WPF.Core;

public static class LyrePlayer
{
    private static readonly IInputSimulator Input = new InputSimulator();

    private enum JoyInputType
    {
        Down,
        Up,
        Press,
    }

    public static int TransposeNote(
        Instrument instrument, ref int noteId,
        Transpose direction = Transpose.Ignore)
    {
        if (direction is Transpose.Ignore) return noteId;
        var notes = GetNotes(instrument);
        while (true)
        {
            if (notes.Contains(noteId))
                return noteId;

            if (noteId < notes.First())
                noteId += 12;
            else if (noteId > notes.Last())
                noteId -= 12;
            else
            {
                return direction switch
                {
                    Transpose.Up   => ++noteId,
                    Transpose.Down => --noteId,
                    _              => noteId
                };
            }
        }
    }

    public static void NoteDown(int noteId, Layout layout, Instrument instrument)
    {
        if (layout != Layout.Joystick)
            InteractNote(noteId, layout, instrument, Input.Keyboard.KeyDown);
        else
            InteractNoteJoy(noteId, layout, instrument, JoyInputType.Down);
    }

    public static void NoteUp(int noteId, Layout layout, Instrument instrument)
    {
        if (layout != Layout.Joystick)
            InteractNote(noteId, layout, instrument, Input.Keyboard.KeyUp);
        else
            InteractNoteJoy(noteId, layout, instrument, JoyInputType.Up);
    }

    public static void PlayNote(int noteId, Layout layout, Instrument instrument)
    {
        if (layout != Layout.Joystick)
            InteractNote(noteId, layout, instrument, Input.Keyboard.KeyPress);
        else
            InteractNoteJoy(noteId, layout, instrument, JoyInputType.Press);
    }

    public static bool TryGetKey(Layout layout, Instrument instrument, int noteId, out VirtualKeyCode key)
    {
        var keys = GetLayout(layout);
        var notes = GetNotes(instrument);
        return TryGetKey(keys, notes, noteId, out key);
    }


    private static bool TryGetKey(
        this IEnumerable<VirtualKeyCode> keys, IList<int> notes,
        int noteId, out VirtualKeyCode key)
    {
        var keyIndex = notes.IndexOf(noteId);
        key = keys.ElementAtOrDefault(keyIndex);

        return keyIndex != -1;
    }

    public static bool TryGetKeyJoy(Layout layout, Instrument instrument, int noteId, out GamePadControl key)
    {
        var keys = GetJoyLayout(layout);
        var notes = GetNotes(instrument);
        return TryGetKeyJoy(keys, notes, noteId, out key);
    }

    private static bool TryGetKeyJoy(
        this IEnumerable<GamePadControl> keys, IList<int> notes,
        int noteId, out GamePadControl key)
    {
        var keyIndex = notes.IndexOf(noteId);
        key = keys.ElementAtOrDefault(keyIndex);

        return keyIndex != -1;
    }

    private static void InteractNote(
        int noteId, Layout layout, Instrument instrument,
        Func<VirtualKeyCode, IKeyboardSimulator> action)
    {
        if (TryGetKey(layout, instrument, noteId, out var key))
            action.Invoke(key);
    }

    private static void InteractNoteJoy(
        int noteId, Layout layout, Instrument instrument,
        JoyInputType action)
    {
        if (TryGetKeyJoy(layout, instrument, noteId, out var key))
            switch (action)
            {
                case JoyInputType.Down:
                    SimGamePad.Instance.ReleaseControl(GamePadControl.LeftShoulder | GamePadControl.RightShoulder);
                    SimGamePad.Instance.SetControl(key);
                    Thread.Sleep(20);
                    SimGamePad.Instance.ReleaseControl(GamePadControl.LeftShoulder | GamePadControl.RightShoulder);
                    break;
                case JoyInputType.Up:
                    SimGamePad.Instance.ReleaseControl(key & ~(GamePadControl.LeftShoulder | GamePadControl.RightShoulder));
                    break;
                case JoyInputType.Press:
                    SimGamePad.Instance.ReleaseControl(GamePadControl.LeftShoulder | GamePadControl.RightShoulder);
                    SimGamePad.Instance.SetControl(key);
                    Thread.Sleep(20);
                    SimGamePad.Instance.ReleaseControl(key);
                    break;
            }
    }
}