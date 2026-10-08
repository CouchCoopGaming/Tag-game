namespace Tag.Art
{
    /// <summary>
    /// Arm clearance on the Hier rest pose. Same bone math the stills use.
    /// Head sphere and the spine, chest, and neck capsules. Not called from Update.
    /// </summary>
    static class AirClear
    {
        const int Stride = 13;

        // Rest matrix, row-major 3x3, then translation, then length.
        static readonly float[] B =
        {
            1f, 0f, 0f, 0f, 0f, -1f, 0f, 1f, 0f, 0f, 0f, 0f, 1.01000f,
            1f, 0f, 0f, 0f, 0f, -1f, 0f, 1f, 0f, 0f, 0f, 1.01000f, 0.11746f,
            1f, 0f, 0f, 0f, 0f, -1f, 0f, 1f, 0f, 0f, 0f, 1.11000f, 0.07000f,
            1f, 0f, 0f, 0f, 0f, -1f, 0f, 1f, 0f, 0f, 0f, 1.18000f, 0.24159f,
            1f, 0f, 0f, 0f, 0f, -1f, 0f, 1f, 0f, 0f, 0f, 1.40000f, 0.13500f,
            1f, 0f, 0f, 0f, 0f, -1f, 0f, 1f, 0f, 0f, 0f, 1.53500f, 0.13500f,
            0.25242f, 0.96762f, 0f, -0.96762f, 0.25242f, 0f, 0f, 0f, 1f, 0.12000f, 0.03000f, 1.40000f, 0.11885f,
            0.80626f, 0.40074f, 0.43514f, -0.40074f, -0.17109f, 0.90008f, 0.43514f, -0.90008f, 0.02265f, 0.23500f, 0.06000f, 1.40000f, 0.37000f,
            0.69154f, 0.44148f, 0.57172f, -0.44148f, -0.36813f, 0.81828f, 0.57172f, -0.81828f, -0.05967f, 0.38327f, -0.00330f, 1.06697f, 0.33000f,
            0.86032f, 0.20751f, 0.46561f, -0.20751f, -0.69171f, 0.69171f, 0.46561f, -0.69171f, -0.55203f, 0.52896f, -0.12478f, 0.79694f, 0.33000f,
            0.25242f, -0.96762f, 0f, 0.96762f, 0.25242f, 0f, 0f, 0f, 1f, -0.12000f, 0.03000f, 1.40000f, 0.11885f,
            0.80626f, -0.40074f, -0.43514f, 0.40074f, -0.17109f, 0.90008f, -0.43514f, -0.90008f, 0.02265f, -0.23500f, 0.06000f, 1.40000f, 0.37000f,
            0.69154f, -0.44148f, -0.57172f, 0.44148f, -0.36813f, 0.81828f, -0.57172f, -0.81828f, -0.05967f, -0.38327f, -0.00330f, 1.06697f, 0.33000f,
            0.86032f, -0.20751f, -0.46561f, 0.20751f, -0.69171f, 0.69171f, -0.46561f, -0.69171f, -0.55203f, -0.52896f, -0.12478f, 0.79694f, 0.33000f,
        };

        static readonly int[] Parent = { -1, 0, 1, 2, 3, 4, 3, 6, 7, 8, 3, 10, 11, 12 };

        public struct Hand
        {
            public float X, Y, Z;
            public float Flex;
        }

        public static float Centimetres(float hip, float spine, float pitch, float yawL, float yawR, float elbow, out Hand left, out Hand right)
        {
            Mat[] pose = new Mat[14];
            for (int i = 0; i < 14; i++)
            {
                Mat basis = Ident();
                if (i == 1) basis = EulerX(hip);
                else if (i == 2) basis = EulerX(spine);
                else if (i == 7) basis = EulerXY(pitch, yawL);
                else if (i == 8) basis = EulerX(elbow);
                else if (i == 11) basis = EulerXY(pitch, yawR);
                else if (i == 12) basis = EulerX(elbow);
                Mat local = Mul(Rest(i), basis);
                pose[i] = Parent[i] < 0 ? local : Mul(Mul(pose[Parent[i]], InvRest(Parent[i])), local);
            }

            Vec head = Mid(HeadOf(pose, 5), TailOf(pose, 5));
            float min = 99f;
            left = new Hand();
            right = new Hand();
            for (int side = 0; side < 2; side++)
            {
                int upper = side == 0 ? 7 : 11;
                int lower = upper + 1;
                int hand = upper + 2;
                Vec a0 = HeadOf(pose, upper);
                Vec a1 = TailOf(pose, upper);
                Vec b0 = HeadOf(pose, lower);
                Vec b1 = TailOf(pose, lower);
                Vec c0 = HeadOf(pose, hand);
                Vec c1 = TailOf(pose, hand);
                for (int s = 0; s <= 4; s++)
                {
                    float t = s / 4f;
                    min = Min(min, Ball(Lerp(a0, a1, t), head, 0.11f));
                    min = Min(min, Ball(Lerp(b0, b1, t), head, 0.11f));
                    min = Min(min, Ball(Lerp(c0, c1, t), head, 0.11f));
                    min = Min(min, Capsule(Lerp(a0, a1, t), HeadOf(pose, 2), TailOf(pose, 2), 0.12f));
                    min = Min(min, Capsule(Lerp(b0, b1, t), HeadOf(pose, 2), TailOf(pose, 2), 0.12f));
                    min = Min(min, Capsule(Lerp(c0, c1, t), HeadOf(pose, 2), TailOf(pose, 2), 0.12f));
                    min = Min(min, Capsule(Lerp(a0, a1, t), HeadOf(pose, 3), TailOf(pose, 3), 0.14f));
                    min = Min(min, Capsule(Lerp(b0, b1, t), HeadOf(pose, 3), TailOf(pose, 3), 0.14f));
                    min = Min(min, Capsule(Lerp(c0, c1, t), HeadOf(pose, 3), TailOf(pose, 3), 0.14f));
                    min = Min(min, Capsule(Lerp(a0, a1, t), HeadOf(pose, 4), TailOf(pose, 4), 0.07f));
                    min = Min(min, Capsule(Lerp(b0, b1, t), HeadOf(pose, 4), TailOf(pose, 4), 0.07f));
                    min = Min(min, Capsule(Lerp(c0, c1, t), HeadOf(pose, 4), TailOf(pose, 4), 0.07f));
                }
                if (side == 0)
                {
                    Vec other = TailOf(pose, 13);
                    min = Min(min, Ball(c1, other, 0.04f));
                    min = Min(min, Capsule(c1, HeadOf(pose, 12), TailOf(pose, 12), 0.04f));
                    min = Min(min, Capsule(other, HeadOf(pose, 8), TailOf(pose, 8), 0.04f));
                }
                Hand h = new Hand
                {
                    X = c1.X,
                    Y = c1.Y,
                    Z = c1.Z,
                    Flex = Degrees(a1, a0, b1),
                };
                if (side == 0) left = h;
                else right = h;
            }
            return min * 100f;
        }

        static float Degrees(Vec elbow, Vec shoulder, Vec wrist)
        {
            Vec ua = Norm(Sub(elbow, shoulder));
            Vec la = Norm(Sub(wrist, elbow));
            float d = ua.X * la.X + ua.Y * la.Y + ua.Z * la.Z;
            if (d > 1f) d = 1f;
            if (d < -1f) d = -1f;
            return MathA(d) * 57.2957795f;
        }

        struct Vec
        {
            public float X, Y, Z;
        }

        struct Mat
        {
            public float A00, A01, A02, A03;
            public float A10, A11, A12, A13;
            public float A20, A21, A22, A23;
        }

        static Mat Rest(int i)
        {
            int o = i * Stride;
            return new Mat
            {
                A00 = B[o], A01 = B[o + 1], A02 = B[o + 2], A03 = B[o + 9],
                A10 = B[o + 3], A11 = B[o + 4], A12 = B[o + 5], A13 = B[o + 10],
                A20 = B[o + 6], A21 = B[o + 7], A22 = B[o + 8], A23 = B[o + 11],
            };
        }

        static float Len(int i)
        {
            return B[i * Stride + 12];
        }

        static Mat Ident()
        {
            return new Mat { A00 = 1f, A11 = 1f, A22 = 1f };
        }

        static Mat EulerX(float deg)
        {
            float a = deg * 0.0174532925f;
            float c = Cos(a);
            float s = Sin(a);
            return new Mat { A00 = 1f, A11 = c, A12 = -s, A21 = s, A22 = c };
        }

        static Mat EulerXY(float xDeg, float yDeg)
        {
            float x = xDeg * 0.0174532925f;
            float y = yDeg * 0.0174532925f;
            float cx = Cos(x);
            float sx = Sin(x);
            float cy = Cos(y);
            float sy = Sin(y);
            // Ry * Rx
            return new Mat
            {
                A00 = cy, A01 = sx * sy, A02 = cx * sy,
                A10 = 0f, A11 = cx, A12 = -sx,
                A20 = -sy, A21 = sx * cy, A22 = cx * cy,
            };
        }

        static Mat InvRest(int i)
        {
            Mat m = Rest(i);
            Mat r = new Mat
            {
                A00 = m.A00, A01 = m.A10, A02 = m.A20,
                A10 = m.A01, A11 = m.A11, A12 = m.A21,
                A20 = m.A02, A21 = m.A12, A22 = m.A22,
            };
            r.A03 = -(r.A00 * m.A03 + r.A01 * m.A13 + r.A02 * m.A23);
            r.A13 = -(r.A10 * m.A03 + r.A11 * m.A13 + r.A12 * m.A23);
            r.A23 = -(r.A20 * m.A03 + r.A21 * m.A13 + r.A22 * m.A23);
            return r;
        }

        static Mat Mul(Mat a, Mat b)
        {
            return new Mat
            {
                A00 = a.A00 * b.A00 + a.A01 * b.A10 + a.A02 * b.A20,
                A01 = a.A00 * b.A01 + a.A01 * b.A11 + a.A02 * b.A21,
                A02 = a.A00 * b.A02 + a.A01 * b.A12 + a.A02 * b.A22,
                A03 = a.A00 * b.A03 + a.A01 * b.A13 + a.A02 * b.A23 + a.A03,
                A10 = a.A10 * b.A00 + a.A11 * b.A10 + a.A12 * b.A20,
                A11 = a.A10 * b.A01 + a.A11 * b.A11 + a.A12 * b.A21,
                A12 = a.A10 * b.A02 + a.A11 * b.A12 + a.A12 * b.A22,
                A13 = a.A10 * b.A03 + a.A11 * b.A13 + a.A12 * b.A23 + a.A13,
                A20 = a.A20 * b.A00 + a.A21 * b.A10 + a.A22 * b.A20,
                A21 = a.A20 * b.A01 + a.A21 * b.A11 + a.A22 * b.A21,
                A22 = a.A20 * b.A02 + a.A21 * b.A12 + a.A22 * b.A22,
                A23 = a.A20 * b.A03 + a.A21 * b.A13 + a.A22 * b.A23 + a.A23,
            };
        }

        static Vec HeadOf(Mat[] pose, int i)
        {
            return new Vec { X = pose[i].A03, Y = pose[i].A13, Z = pose[i].A23 };
        }

        static Vec TailOf(Mat[] pose, int i)
        {
            float len = Len(i);
            Mat m = pose[i];
            return new Vec
            {
                X = m.A01 * len + m.A03,
                Y = m.A11 * len + m.A13,
                Z = m.A21 * len + m.A23,
            };
        }

        static Vec Mid(Vec a, Vec b)
        {
            return new Vec { X = (a.X + b.X) * 0.5f, Y = (a.Y + b.Y) * 0.5f, Z = (a.Z + b.Z) * 0.5f };
        }

        static Vec Lerp(Vec a, Vec b, float t)
        {
            return new Vec { X = a.X + (b.X - a.X) * t, Y = a.Y + (b.Y - a.Y) * t, Z = a.Z + (b.Z - a.Z) * t };
        }

        static Vec Sub(Vec a, Vec b)
        {
            return new Vec { X = a.X - b.X, Y = a.Y - b.Y, Z = a.Z - b.Z };
        }

        static Vec Norm(Vec v)
        {
            float m = v.X * v.X + v.Y * v.Y + v.Z * v.Z;
            if (m < 1e-8f) return v;
            m = (float)System.Math.Sqrt(m);
            return new Vec { X = v.X / m, Y = v.Y / m, Z = v.Z / m };
        }

        static float Ball(Vec p, Vec c, float radius)
        {
            float dx = p.X - c.X;
            float dy = p.Y - c.Y;
            float dz = p.Z - c.Z;
            return (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz) - radius;
        }

        static float Capsule(Vec p, Vec a, Vec b, float radius)
        {
            float abx = b.X - a.X;
            float aby = b.Y - a.Y;
            float abz = b.Z - a.Z;
            float den = abx * abx + aby * aby + abz * abz;
            float t = 0f;
            if (den > 1e-8f)
            {
                t = ((p.X - a.X) * abx + (p.Y - a.Y) * aby + (p.Z - a.Z) * abz) / den;
                if (t < 0f) t = 0f;
                if (t > 1f) t = 1f;
            }
            float dx = a.X + abx * t - p.X;
            float dy = a.Y + aby * t - p.Y;
            float dz = a.Z + abz * t - p.Z;
            return (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz) - radius;
        }

        static float Min(float a, float b)
        {
            return a < b ? a : b;
        }

        static float Sin(float a)
        {
            return (float)System.Math.Sin(a);
        }

        static float Cos(float a)
        {
            return (float)System.Math.Cos(a);
        }

        static float MathA(float d)
        {
            return (float)System.Math.Acos(d);
        }
    }
}
