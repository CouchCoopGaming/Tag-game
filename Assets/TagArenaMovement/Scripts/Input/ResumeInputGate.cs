using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// Cursor lock recenters the pointer. That recenter, and the key or click that
    /// resumed, must not yaw the camera or fire punch / jump / dash / lunge.
    /// Arm at the lock itself so script order cannot sample the spike: this frame
    /// and the next one (the frame Mouse X/Y actually contain the recenter).
    /// </summary>
    public static class ResumeInputGate
    {
        static int _blockThroughFrame = -1;

        public static bool Blocking => Time.frameCount <= _blockThroughFrame;

        /// <summary>Block look and one-shots through the end of the next frame.</summary>
        public static void Arm()
        {
            int through = Time.frameCount + 1;
            if (through > _blockThroughFrame)
                _blockThroughFrame = through;
        }

        /// <summary>Lock the play cursor and drop any one-shots already latched this frame.</summary>
        public static void LockPlayCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Arm();
            var readers = Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None);
            for (int i = 0; i < readers.Length; i++)
            {
                if (readers[i] != null)
                    readers[i].SuppressResumeOneShots();
            }
        }
    }
}
