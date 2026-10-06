namespace Tag.Audio
{
    /// <summary>
    /// One dry tick for each whole second of the last 10 seconds of a round.
    /// Flat, on the SFX bus. Not a new hook.
    /// </summary>
    public static class RoundChime
    {
        static int _sec = int.MinValue;

        public static void Tick(float remaining)
        {
            AudioMix.Pump();
            if (remaining > 10f || remaining <= 0f)
            {
                _sec = int.MinValue;
                return;
            }
            int sec = (int)remaining;
            if (remaining > sec) sec++;
            if (sec < 1 || sec > 10 || sec == _sec) return;
            _sec = sec;
            TagSfx.RoundTick();
        }
    }
}
