using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class InputBridge
{
    public static float GetHorizontal()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            float val = 0f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) val += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) val -= 1f;
            return val;
        }
#endif
        try { return Input.GetAxisRaw("Horizontal"); } catch { return 0f; }
    }

    public static float GetVertical()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            float val = 0f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) val += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) val -= 1f;
            return val;
        }
#endif
        try { return Input.GetAxisRaw("Vertical"); } catch { return 0f; }
    }

    public static Vector2 GetMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null)
        {
            // Mouse delta in new input system is in pixels, scale to match standard sensitivity
            return mouse.delta.ReadValue() * 0.1f;
        }
#endif
        try { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); } catch { return Vector2.zero; }
    }

    public static bool GetJumpDown()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null) return kb.spaceKey.wasPressedThisFrame;
#endif
        try { return Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space); } catch { return false; }
    }

    public static bool GetSprint()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null) return kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
#endif
        try { return Input.GetKey(KeyCode.LeftShift); } catch { return false; }
    }

    public static bool GetFire()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null) return mouse.leftButton.isPressed;
#endif
        try { return Input.GetButton("Fire1") || Input.GetMouseButton(0); } catch { return false; }
    }

    public static bool GetReloadDown()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null) return kb.rKey.wasPressedThisFrame;
#endif
        try { return Input.GetKeyDown(KeyCode.R); } catch { return false; }
    }

    public static bool GetEscapeDown()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null) return kb.escapeKey.wasPressedThisFrame;
#endif
        try { return Input.GetKeyDown(KeyCode.Escape); } catch { return false; }
    }
}
