using System;
using System.Globalization;
using System.Text;

namespace Tag.Practice
{
    /// <summary>
    /// PB ghost. Position and pose at a fixed rate, written into buffers
    /// allocated once. Recording does not allocate. The figure that plays
    /// this back has no collider, no CharacterController, and no rigidbody.
    /// </summary>
    public static class PracticeGhost
    {
        public const int Hz = 20;
        public const int Cap = 512;
        public const int Slots = 8;
        public const bool HasCharacterController = false;
        public const bool HasCollider = false;
        public const bool HasRigidbody = false;
        public const bool RootMotion = false;

        public static readonly float[] X = new float[Cap];
        public static readonly float[] Y = new float[Cap];
        public static readonly float[] Z = new float[Cap];
        public static readonly float[] Yaw = new float[Cap];
        public static readonly byte[] Pose = new byte[Cap];
        public static int Count;
        public static float Accum;

        static readonly float[] SavedX = new float[Slots * Cap];
        static readonly float[] SavedY = new float[Slots * Cap];
        static readonly float[] SavedZ = new float[Slots * Cap];
        static readonly float[] SavedYaw = new float[Slots * Cap];
        static readonly byte[] SavedPose = new byte[Slots * Cap];
        static readonly int[] SavedCount = new int[Slots];
        static readonly string[] SavedId = new string[Slots];

        public static void Clear()
        {
            Count = 0;
            Accum = 0f;
        }

        public static void ClearSaved()
        {
            for (int i = 0; i < Slots; i++)
            {
                SavedId[i] = null;
                SavedCount[i] = 0;
            }
        }

        public static bool HasAnySaved()
        {
            for (int i = 0; i < Slots; i++)
            {
                if (!string.IsNullOrEmpty(SavedId[i]) && SavedCount[i] > 0) return true;
            }
            return false;
        }

        public static void ExportSaved(string[] ids, int[] counts, float[] x, float[] y, float[] z, float[] yaw, byte[] pose)
        {
            for (int s = 0; s < Slots; s++)
            {
                ids[s] = SavedId[s];
                int n = SavedCount[s];
                if (n < 0) n = 0;
                if (n > Cap) n = Cap;
                counts[s] = string.IsNullOrEmpty(SavedId[s]) ? 0 : n;
                int o = s * Cap;
                for (int i = 0; i < n; i++)
                {
                    x[o + i] = SavedX[o + i];
                    y[o + i] = SavedY[o + i];
                    z[o + i] = SavedZ[o + i];
                    yaw[o + i] = SavedYaw[o + i];
                    pose[o + i] = SavedPose[o + i];
                }
            }
        }

        public static void ImportSaved(string[] ids, int[] counts, float[] x, float[] y, float[] z, float[] yaw, byte[] pose)
        {
            ClearSaved();
            if (ids == null || counts == null) return;
            for (int s = 0; s < Slots; s++)
            {
                if (string.IsNullOrEmpty(ids[s]) || counts[s] < 1) continue;
                SavedId[s] = ids[s];
                int n = counts[s];
                if (n > Cap) n = Cap;
                SavedCount[s] = n;
                int o = s * Cap;
                for (int i = 0; i < n; i++)
                {
                    SavedX[o + i] = x != null ? x[o + i] : 0f;
                    SavedY[o + i] = y != null ? y[o + i] : 0f;
                    SavedZ[o + i] = z != null ? z[o + i] : 0f;
                    SavedYaw[o + i] = yaw != null ? yaw[o + i] : 0f;
                    SavedPose[o + i] = pose != null ? pose[o + i] : (byte)0;
                }
            }
        }

        public static void Offer(float dt, float x, float y, float z, float yaw, byte pose)
        {
            float step = 1f / Hz;
            if (Count == 0)
            {
                Push(x, y, z, yaw, pose);
                Accum = 0f;
                return;
            }
            Accum += dt;
            if (Accum < step) return;
            Accum -= step;
            if (Accum > step) Accum = step;
            Push(x, y, z, yaw, pose);
        }

        static void Push(float x, float y, float z, float yaw, byte pose)
        {
            if (Count >= Cap) return;
            X[Count] = x;
            Y[Count] = y;
            Z[Count] = z;
            Yaw[Count] = yaw;
            Pose[Count] = pose;
            Count++;
        }

        public static void CopyLiveTo(float[] bx, float[] by, float[] bz, float[] byaw, byte[] bpose, out int n)
        {
            n = Count;
            for (int i = 0; i < n; i++)
            {
                bx[i] = X[i];
                by[i] = Y[i];
                bz[i] = Z[i];
                byaw[i] = Yaw[i];
                bpose[i] = Pose[i];
            }
        }

        public static bool Matches(float[] bx, float[] by, float[] bz, float[] byaw, byte[] bpose, int n)
        {
            if (n != Count) return false;
            for (int i = 0; i < n; i++)
            {
                if (bx[i] != X[i] || by[i] != Y[i] || bz[i] != Z[i]) return false;
                if (byaw[i] != Yaw[i] || bpose[i] != Pose[i]) return false;
            }
            return true;
        }

        public static void At(float time, out float x, out float y, out float z, out float yaw, out byte pose)
        {
            x = 0f;
            y = 0f;
            z = 0f;
            yaw = 0f;
            pose = 0;
            if (Count < 1) return;
            if (Count == 1 || time <= 0f)
            {
                x = X[0];
                y = Y[0];
                z = Z[0];
                yaw = Yaw[0];
                pose = Pose[0];
                return;
            }
            float u = time * Hz;
            int i = (int)u;
            if (i >= Count - 1) i = Count - 2;
            if (i < 0) i = 0;
            float f = u - i;
            if (f < 0f) f = 0f;
            if (f > 1f) f = 1f;
            x = X[i] + (X[i + 1] - X[i]) * f;
            y = Y[i] + (Y[i + 1] - Y[i]) * f;
            z = Z[i] + (Z[i + 1] - Z[i]) * f;
            yaw = Yaw[i] + (Yaw[i + 1] - Yaw[i]) * f;
            pose = f < 0.5f ? Pose[i] : Pose[i + 1];
        }

        public static bool HasReplay(string id)
        {
            return SavedSamples(id) > 1;
        }

        public static bool Replay(string id, float time, out float x, out float y, out float z, out float yaw, out byte pose)
        {
            x = 0f;
            y = 0f;
            z = 0f;
            yaw = 0f;
            pose = 0;
            int slot = Slot(id, false);
            if (slot < 0 || SavedCount[slot] < 1) return false;
            int n = SavedCount[slot];
            int o = slot * Cap;
            if (n == 1 || time <= 0f)
            {
                x = SavedX[o];
                y = SavedY[o];
                z = SavedZ[o];
                yaw = SavedYaw[o];
                pose = SavedPose[o];
                return true;
            }
            float u = time * Hz;
            int i = (int)u;
            if (i >= n - 1) i = n - 2;
            if (i < 0) i = 0;
            float f = u - i;
            if (f < 0f) f = 0f;
            if (f > 1f) f = 1f;
            int a = o + i;
            int b = a + 1;
            x = SavedX[a] + (SavedX[b] - SavedX[a]) * f;
            y = SavedY[a] + (SavedY[b] - SavedY[a]) * f;
            z = SavedZ[a] + (SavedZ[b] - SavedZ[a]) * f;
            yaw = SavedYaw[a] + (SavedYaw[b] - SavedYaw[a]) * f;
            pose = f < 0.5f ? SavedPose[a] : SavedPose[b];
            return true;
        }

        public static void Keep(string id)
        {
            if (string.IsNullOrEmpty(id) || Count < 1) return;
            int slot = Slot(id, true);
            if (slot < 0) return;
            SavedId[slot] = id;
            int n = Count;
            if (n > Cap) n = Cap;
            SavedCount[slot] = n;
            int o = slot * Cap;
            for (int i = 0; i < n; i++)
            {
                SavedX[o + i] = X[i];
                SavedY[o + i] = Y[i];
                SavedZ[o + i] = Z[i];
                SavedYaw[o + i] = Yaw[i];
                SavedPose[o + i] = Pose[i];
            }
        }

        public static bool Load(string id)
        {
            int slot = Slot(id, false);
            if (slot < 0) return false;
            Clear();
            int n = SavedCount[slot];
            int o = slot * Cap;
            for (int i = 0; i < n; i++)
            {
                X[i] = SavedX[o + i];
                Y[i] = SavedY[o + i];
                Z[i] = SavedZ[o + i];
                Yaw[i] = SavedYaw[o + i];
                Pose[i] = SavedPose[o + i];
            }
            Count = n;
            return n > 0;
        }

        public static int SavedSamples(string id)
        {
            int slot = Slot(id, false);
            if (slot < 0) return 0;
            return SavedCount[slot];
        }

        public static int CopyRoute(string id, float[] x, float[] y, float[] z, float[] yaw, byte[] pose)
        {
            int slot = Slot(id, false);
            if (slot < 0) return 0;
            int n = SavedCount[slot];
            if (n < 0) n = 0;
            if (n > Cap) n = Cap;
            int o = slot * Cap;
            for (int i = 0; i < n; i++)
            {
                if (x != null) x[i] = SavedX[o + i];
                if (y != null) y[i] = SavedY[o + i];
                if (z != null) z[i] = SavedZ[o + i];
                if (yaw != null) yaw[i] = SavedYaw[o + i];
                if (pose != null) pose[i] = SavedPose[o + i];
            }
            return n;
        }

        public static void Write(StringBuilder text)
        {
            for (int s = 0; s < Slots; s++)
            {
                if (string.IsNullOrEmpty(SavedId[s]) || SavedCount[s] < 1) continue;
                text.Append("gh.");
                text.Append(SavedId[s]);
                text.Append('=');
                text.Append(SavedCount[s].ToString(CultureInfo.InvariantCulture));
                int o = s * Cap;
                int n = SavedCount[s];
                for (int i = 0; i < n; i++)
                {
                    text.Append(';');
                    text.Append(SavedX[o + i].ToString("R", CultureInfo.InvariantCulture));
                    text.Append(',');
                    text.Append(SavedY[o + i].ToString("R", CultureInfo.InvariantCulture));
                    text.Append(',');
                    text.Append(SavedZ[o + i].ToString("R", CultureInfo.InvariantCulture));
                    text.Append(',');
                    text.Append(SavedYaw[o + i].ToString("R", CultureInfo.InvariantCulture));
                    text.Append(',');
                    text.Append(SavedPose[o + i].ToString(CultureInfo.InvariantCulture));
                }
                text.Append('\n');
            }
        }

        public static void Read(string id, string value)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(value)) return;
            int slot = Slot(id, true);
            if (slot < 0) return;
            SavedId[slot] = id;
            int semi = value.IndexOf(';');
            string head = semi < 0 ? value : value.Substring(0, semi);
            if (!int.TryParse(head, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                n = 0;
            if (n < 0) n = 0;
            if (n > Cap) n = Cap;
            int o = slot * Cap;
            int written = 0;
            int i = semi < 0 ? value.Length : semi + 1;
            while (written < n && i < value.Length)
            {
                int next = value.IndexOf(';', i);
                if (next < 0) next = value.Length;
                if (ParseSample(value.Substring(i, next - i), out float x, out float y, out float z, out float yaw, out byte pose))
                {
                    SavedX[o + written] = x;
                    SavedY[o + written] = y;
                    SavedZ[o + written] = z;
                    SavedYaw[o + written] = yaw;
                    SavedPose[o + written] = pose;
                    written++;
                }
                i = next + 1;
            }
            SavedCount[slot] = written;
        }

        static bool ParseSample(string text, out float x, out float y, out float z, out float yaw, out byte pose)
        {
            x = y = z = yaw = 0f;
            pose = 0;
            int c0 = text.IndexOf(',');
            if (c0 < 0) return false;
            int c1 = text.IndexOf(',', c0 + 1);
            if (c1 < 0) return false;
            int c2 = text.IndexOf(',', c1 + 1);
            if (c2 < 0) return false;
            int c3 = text.IndexOf(',', c2 + 1);
            if (c3 < 0) return false;
            if (!float.TryParse(text.Substring(0, c0), NumberStyles.Float, CultureInfo.InvariantCulture, out x)) return false;
            if (!float.TryParse(text.Substring(c0 + 1, c1 - c0 - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out y)) return false;
            if (!float.TryParse(text.Substring(c1 + 1, c2 - c1 - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out z)) return false;
            if (!float.TryParse(text.Substring(c2 + 1, c3 - c2 - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out yaw)) return false;
            if (!byte.TryParse(text.Substring(c3 + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out pose)) return false;
            return true;
        }

        static int Slot(string id, bool claim)
        {
            int free = -1;
            for (int i = 0; i < Slots; i++)
            {
                if (SavedId[i] == id) return i;
                if (free < 0 && string.IsNullOrEmpty(SavedId[i])) free = i;
            }
            return claim ? free : -1;
        }
    }
}
