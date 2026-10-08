using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// One type scale, one spacing step, and the seat colors every screen already
    /// reads from MenuTheme.Seat. New cards use these sizes.
    /// </summary>
    public static class MenuTokens
    {
        public const int Display = 48;
        public const int Title = 42;
        public const int Section = 32;
        public const int Body = UiFit.FloorFont;
        public const int Fine = UiFit.FloorFont;
        public const float Space = UiFit.SpaceStep;

        public static Color Seat(int seat)
        {
            return MenuTheme.Seat(seat);
        }
    }
}
