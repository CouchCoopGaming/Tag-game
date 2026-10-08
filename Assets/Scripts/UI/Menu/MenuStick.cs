using System.Globalization;
using System.Reflection;
using Tag.Settings;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Stick inner deadzone, outer deadzone, response curve, and gamepad look
    /// accel. Lane A owns those fields on GameSettings. This page binds to them
    /// when they exist and shows a disabled row when they do not. GamepadLook
    /// is the shared look speed and is not one of these rows.
    /// </summary>
    public static class MenuStick
    {
        public const int Rows = 4;

        static readonly string[] Title =
        {
            "Stick inner deadzone",
            "Stick outer deadzone",
            "Stick response curve",
            "Gamepad look accel"
        };

        static readonly string[][] Names =
        {
            new[] { "StickInner", "StickInnerDeadzone", "InnerDeadzone", "StickDeadzoneInner", "DeadzoneInner" },
            new[] { "StickOuter", "StickOuterDeadzone", "OuterDeadzone", "StickDeadzoneOuter", "DeadzoneOuter" },
            new[] { "StickCurve", "StickResponseCurve", "StickResponse", "ResponseCurve", "LookResponse" },
            new[] { "LookAccel", "GamepadLookAccel", "StickLookAccel", "GamepadAccel" }
        };

        static readonly float[] Step = { 0.05f, 0.05f, 0.25f, 0.10f };
        static readonly float[] Lo = { 0.00f, 0.05f, 0.50f, 0.00f };
        static readonly float[] Hi = { 0.50f, 1.00f, 3.00f, 1.00f };

        static readonly FieldInfo[] Fields = new FieldInfo[Rows];
        static readonly PropertyInfo[] Props = new PropertyInfo[Rows];
        static bool _ready;

        public static string Label(int row)
        {
            if (row < 0 || row >= Rows) return "";
            Ensure();
            float value;
            if (!TryRead(row, out value)) return Title[row];
            return Title[row] + "  " + value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        public static string Detail(int row)
        {
            if (row < 0 || row >= Rows) return "";
            Ensure();
            if (!Bound(row)) return "Coming soon";
            return "Left / Right";
        }

        public static bool Nudge(int row, int dir)
        {
            if (row < 0 || row >= Rows || dir == 0) return false;
            Ensure();
            if (!Bound(row)) return false;
            float value;
            if (!TryRead(row, out value)) return false;
            float next = value + (dir < 0 ? -Step[row] : Step[row]);
            if (next < Lo[row]) next = Lo[row];
            if (next > Hi[row]) next = Hi[row];
            if (row == 0 && TryRead(1, out float outer) && next > outer - 0.05f)
                next = outer - 0.05f;
            if (row == 1 && TryRead(0, out float inner) && next < inner + 0.05f)
                next = inner + 0.05f;
            if (next < Lo[row]) next = Lo[row];
            if (next > Hi[row]) next = Hi[row];
            return TryWrite(row, next);
        }

        static bool Bound(int row)
        {
            return Fields[row] != null || Props[row] != null;
        }

        static void Ensure()
        {
            if (_ready) return;
            _ready = true;
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            System.Type type = typeof(GameSettings);
            for (int row = 0; row < Rows; row++)
            {
                for (int n = 0; n < Names[row].Length; n++)
                {
                    string name = Names[row][n];
                    if (name == "GamepadLook") continue;
                    FieldInfo field = type.GetField(name, flags);
                    if (field != null && field.FieldType == typeof(float))
                    {
                        Fields[row] = field;
                        break;
                    }
                    PropertyInfo prop = type.GetProperty(name, flags);
                    if (prop != null && prop.PropertyType == typeof(float) && prop.CanRead && prop.CanWrite)
                    {
                        Props[row] = prop;
                        break;
                    }
                }
            }
        }

        static bool TryRead(int row, out float value)
        {
            value = 0f;
            GameSettings settings = GameSettings.Current;
            if (settings == null) return false;
            if (Fields[row] != null)
            {
                value = (float)Fields[row].GetValue(settings);
                return true;
            }
            if (Props[row] != null)
            {
                value = (float)Props[row].GetValue(settings, null);
                return true;
            }
            return false;
        }

        static bool TryWrite(int row, float value)
        {
            GameSettings settings = GameSettings.Current ?? GameSettings.Defaults();
            GameSettings.Current = settings;
            if (Fields[row] != null)
            {
                Fields[row].SetValue(settings, value);
                return true;
            }
            if (Props[row] != null)
            {
                Props[row].SetValue(settings, value, null);
                return true;
            }
            return false;
        }
    }
}
