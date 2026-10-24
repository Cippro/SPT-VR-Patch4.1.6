using System;

namespace TarkovVR.Source.UI
{
    [Flags]
    internal enum UiConfirmEvent
    {
        None = 0,
        Down = 1,
        Up = 2,
        Click = 4,
        ContextClick = 8
    }

    internal sealed class UiConfirmState
    {
        private bool pressed;
        private bool contextClicked;
        private bool clickCanceled;
        private float heldSeconds;

        internal UiConfirmEvent Step(bool down, bool held, bool up, bool hasTarget, bool sameTarget, bool dragging, float deltaSeconds)
        {
            UiConfirmEvent result = UiConfirmEvent.None;
            if (down)
            {
                Reset();
                pressed = hasTarget;
                if (pressed) result |= UiConfirmEvent.Down;
            }
            if (!pressed) return result;
            clickCanceled |= dragging;
            if (held && !contextClicked && !clickCanceled)
            {
                heldSeconds += Math.Max(0f, deltaSeconds);
                if (heldSeconds >= 0.25f && hasTarget && sameTarget)
                {
                    contextClicked = true;
                    result |= UiConfirmEvent.ContextClick;
                }
            }
            if (up)
            {
                result |= UiConfirmEvent.Up;
                if (hasTarget && sameTarget && !contextClicked && !clickCanceled)
                    result |= UiConfirmEvent.Click;
                Reset();
            }
            return result;
        }

        internal void Reset()
        {
            pressed = false;
            contextClicked = false;
            clickCanceled = false;
            heldSeconds = 0f;
        }
    }
}
