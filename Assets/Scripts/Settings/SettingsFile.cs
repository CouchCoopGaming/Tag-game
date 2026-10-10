using System;
using System.Globalization;
using System.IO;
using System.Text;
using Tag.Practice;
using Tag.Profiles;
using Tag.Ui.Menu;

namespace Tag.Settings
{
    /// <summary>
    /// Line-oriented blob. PlayerPrefs and tag-settings.json store the same text.
    /// Missing lines keep the defaults the caller started from.
    /// </summary>
    public static class SettingsFile
    {
        public static string Write(GameSettings settings, ActionBinds binds)
        {
            var s = settings ?? GameSettings.Defaults();
            var b = binds ?? ActionBinds.Defaults();
            var text = new StringBuilder();
            text.Append("v=2\n");
            Line(text, "mouse", s.MouseSensitivity);
            Line(text, "padLook", s.GamepadLook);
            Line(text, "stickInner", s.StickInner);
            Line(text, "stickOuter", s.StickOuter);
            Line(text, "stickCurve", s.StickCurve);
            Line(text, "lookAccel", s.LookAccel);
            Line(text, "invertY", s.InvertY ? 1f : 0f);
            Line(text, "fov", s.Fov);
            Line(text, "master", s.Master);
            Line(text, "sfx", s.Sfx);
            Line(text, "ui", s.Ui);
            Line(text, "music", s.Music);
            Line(text, "mute", s.Muted ? 1f : 0f);
            Line(text, "hud", s.HudScale);
            Line(text, "uiScale", s.UiScale);
            Line(text, "comic", s.ComicWords ? 1f : 0f);
            Line(text, "motion", s.ReduceMotion ? 1f : 0f);
            Line(text, "quality", s.PictureQuality);
            // Picture index space. Missing qv means quality 0 was the only level, Medium.
            Line(text, "qv", QualityVersion);
            Line(text, "res", s.ResIndex);
            Line(text, "full", s.Fullscreen ? 1f : 0f);
            Line(text, "vsync", s.VSync ? 1f : 0f);
            Line(text, "speedLines", s.SpeedLines ? 1f : 0f);
            Line(text, "colorblind", s.Colorblind ? 1f : 0f);
            Line(text, "cvdSeats", s.CvdSeats);
            Line(text, "minimap", s.Minimap ? 1f : 0f);
            Line(text, "accessSeat", s.AccessSeat);
            for (int i = 0; i < GameSettings.SeatCount; i++)
            {
                string n = i == 0 ? "" : i.ToString(CultureInfo.InvariantCulture);
                Line(text, "palette" + n, s.Palette[i]);
                Line(text, "captions" + n, s.Captions[i] ? 1f : 0f);
                Line(text, "rumble" + n, s.Rumble[i]);
                Line(text, "flash" + n, s.ReduceFlash[i] ? 1f : 0f);
            }
            Line(text, "arena", s.Arena);
            Line(text, "ai", s.AiOpponents);
            Line(text, "diff", s.DifficultyTier);
            Line(text, "roundLen", s.RoundLengthIndex);
            Line(text, "rounds", s.RoundsPerMatch);
            Line(text, "split", s.SplitAxis);
            Line(text, "listen", s.Listener);
            Line(text, "wallRibbon", s.WallRibbon ? 1f : 0f);
            Line(text, "inkCard", s.InkCard ? 1f : 0f);
            Line(text, "itWedge", s.ItWedge ? 1f : 0f);
            Line(text, "crownPlate", s.CrownPlate ? 1f : 0f);
            Line(text, "edgeStreaks", s.EdgeStreaks ? 1f : 0f);
            Line(text, "contactMarks", s.ContactMarks ? 1f : 0f);
            Line(text, "wallMarks", s.WallMarks ? 1f : 0f);
            Line(text, "bodyStroke", s.BodyStroke ? 1f : 0f);
            Line(text, "seatInk", s.SeatInk ? 1f : 0f);
            Line(text, "oneWord", s.OneWord ? 1f : 0f);
            Line(text, "ribbonLane", s.RibbonLane ? 1f : 0f);
            Line(text, "strokeYield", s.StrokeYield ? 1f : 0f);
            Line(text, "landStain", s.LandStain ? 1f : 0f);
            Line(text, "effects", s.Effects);
            Line(text, "startIt", s.StartIt);
            Line(text, "startSeat", s.StartSeat);
            Line(text, "winTarget", s.WinTarget);
            Line(text, "pads", s.HazardPads ? 1f : 0f);
            Line(text, "zips", s.HazardZips ? 1f : 0f);
            for (int i = 0; i < GameSettings.SeatCount; i++)
            {
                string n = i == 0 ? "" : i.ToString(CultureInfo.InvariantCulture);
                Line(text, "confirm" + n, s.ConfirmFace[i]);
                Line(text, "hand" + n, s.Handicap[i]);
            }
            for (int i = 0; i < (int)PlayAction.Count; i++)
            {
                var action = (PlayAction)i;
                text.Append("kb.");
                text.Append(action.ToString());
                text.Append('=');
                text.Append(b.Keyboard[i] ?? "");
                text.Append('\n');
                text.Append("pad.");
                text.Append(action.ToString());
                text.Append('=');
                text.Append(b.Gamepad[i] ?? "");
                text.Append('\n');
            }
            if (!string.IsNullOrEmpty(b.JumpAlt))
            {
                text.Append("jumpAlt=");
                text.Append(b.JumpAlt);
                text.Append('\n');
            }
            if (!string.IsNullOrEmpty(b.GrappleKey) && b.GrappleKey != ActionBinds.GrappleKeyDefault)
            {
                text.Append("grappleKey=");
                text.Append(b.GrappleKey);
                text.Append('\n');
            }
            if (!string.IsNullOrEmpty(b.GrapplePad) && b.GrapplePad != ActionBinds.GrapplePadDefault)
            {
                text.Append("grapplePad=");
                text.Append(b.GrapplePad);
                text.Append('\n');
            }
            PracticeBests.Write(text);
            PracticeGhost.Write(text);
            LocalProfiles.Write(text);
            MenuSession.Write(text);
            return text.ToString();
        }

        public const int Version = 2;
        /// <summary>Picture-quality index space. Blobs without qv stored 0 for Medium.</summary>
        public const int QualityVersion = 1;

        /// <summary>
        /// Write a temp file, read it back, then replace the destination.
        /// A crash after the temp write leaves the previous file intact.
        /// </summary>
        public static bool CommitText(string path, string text)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string tmp = path + ".tmp";
            string body = text ?? "";
            try
            {
                File.WriteAllText(tmp, body);
                string back = File.ReadAllText(tmp);
                if (back != body)
                {
                    if (File.Exists(tmp)) File.Delete(tmp);
                    return false;
                }
                if (File.Exists(path))
                    File.Replace(tmp, path, null);
                else
                    File.Move(tmp, path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>The destination only. A half-written temp file is not the settings blob.</summary>
        public static string ReadStable(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return "";
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static void Read(string blob, GameSettings settings, ActionBinds binds)
        {
            Read(blob, settings, binds, true);
        }

        /// <summary>
        /// touchProfiles false still runs the quality migration. It skips the profile rewrite
        /// so a probe blob does not create a Player row beside the live lobby.
        /// </summary>
        public static void Read(string blob, GameSettings settings, ActionBinds binds, bool touchProfiles)
        {
            if (settings == null || binds == null || string.IsNullOrEmpty(blob)) return;
            string[] lines = blob.Split('\n');
            int version = -1;
            bool badVersion = false;
            bool known = false;
            bool sawQv = false;
            bool sawQuality = false;
            int storedQuality = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (!Split(lines[i], out string key, out string value)) continue;
                if (key == "v")
                {
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out version))
                        badVersion = true;
                    continue;
                }
                if (key == "qv") sawQv = true;
                if (key == "quality")
                {
                    sawQuality = true;
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int q))
                        storedQuality = q;
                }
                if (Known(key)) known = true;
            }
            // A newer or unreadable version is not applied in part. Reset.
            // No version and no known key is garbage. An older version migrates.
            if (badVersion || version > Version || (version < 0 && !known))
            {
                settings.ResetToDefaults();
                binds.ResetToDefaults();
                if (touchProfiles)
                {
                    LocalProfiles.Clear();
                    MenuSession.Reset();
                }
                return;
            }
            if (touchProfiles)
            {
                PracticeBests.Clear();
                PracticeGhost.ClearSaved();
                LocalProfiles.BeginRead();
            }
            for (int i = 0; i < lines.Length; i++)
            {
                if (!Split(lines[i], out string key, out string value)) continue;
                if (key == "v" || key == "qv") continue;
                Apply(settings, binds, key, value);
            }
            if (settings.Colorblind && blob.IndexOf("palette=", StringComparison.Ordinal) < 0)
                settings.Palette[0] = AccessibilityPalette.Deuteranopia;
            // Version-less and older blobs stored 0 when Medium was the only level.
            if (!sawQv && sawQuality && storedQuality == GameSettings.QualityLow)
                settings.PictureQuality = GameSettings.QualityMedium;
            settings.Clamp();
            if (touchProfiles)
                LocalProfiles.EndRead(version, settings, binds);
        }

        static bool Split(string raw, out string key, out string value)
        {
            key = "";
            value = "";
            if (string.IsNullOrEmpty(raw)) return false;
            string line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') return false;
            int eq = line.IndexOf('=');
            if (eq <= 0) return false;
            key = line.Substring(0, eq).Trim();
            value = line.Substring(eq + 1).Trim();
            return key.Length > 0;
        }

        static bool Known(string key)
        {
            if (key == "mouse" || key == "padLook" || key == "invertY" || key == "fov") return true;
            if (key == "stickInner" || key == "stickOuter" || key == "stickCurve" || key == "lookAccel") return true;
            if (key == "master" || key == "sfx" || key == "ui" || key == "music" || key == "mute") return true;
            if (key == "hud" || key == "uiScale" || key == "colorblind" || key == "minimap" || key == "accessSeat") return true;
            if (key == "comic" || key == "motion" || key == "quality" || key == "qv" || key == "res" || key == "full" || key == "vsync") return true;
            if (key == "cvdSeats" || key == "speedLines" || key == "jumpAlt" || key == "grappleKey" || key == "grapplePad") return true;
            if (key == "arena" || key == "ai" || key == "diff" || key == "roundLen" || key == "rounds") return true;
            if (key == "split" || key == "listen") return true;
            if (key == "startIt" || key == "startSeat" || key == "winTarget" || key == "pads" || key == "zips") return true;
            if (SeatKey(key, "confirm", out _)) return true;
            if (SeatKey(key, "hand", out _)) return true;
            if (key == "effects") return true;
            if (key.StartsWith("kb.", StringComparison.Ordinal) || key.StartsWith("pad.", StringComparison.Ordinal))
                return true;
            if (key.StartsWith("pb.", StringComparison.Ordinal) || key.StartsWith("sp.", StringComparison.Ordinal))
                return true;
            if (key.StartsWith("gh.", StringComparison.Ordinal))
                return true;
            if (SeatKey(key, "palette", out _)) return true;
            if (SeatKey(key, "captions", out _)) return true;
            if (SeatKey(key, "rumble", out _)) return true;
            if (SeatKey(key, "flash", out _)) return true;
            if (LocalProfiles.IsKey(key)) return true;
            if (MenuSession.IsKey(key)) return true;
            return false;
        }

        static void Apply(GameSettings settings, ActionBinds binds, string key, string value)
        {
            if (key == "mouse") settings.MouseSensitivity = Num(value, settings.MouseSensitivity);
            else if (key == "padLook") settings.GamepadLook = Num(value, settings.GamepadLook);
            else if (key == "stickInner") settings.StickInner = Num(value, settings.StickInner);
            else if (key == "stickOuter") settings.StickOuter = Num(value, settings.StickOuter);
            else if (key == "stickCurve") settings.StickCurve = Num(value, settings.StickCurve);
            else if (key == "lookAccel") settings.LookAccel = Num(value, settings.LookAccel);
            else if (key == "invertY") settings.InvertY = Flag(value);
            else if (key == "fov") settings.Fov = Num(value, settings.Fov);
            else if (key == "master") settings.Master = Num(value, settings.Master);
            else if (key == "sfx") settings.Sfx = Num(value, settings.Sfx);
            else if (key == "ui") settings.Ui = Num(value, settings.Ui);
            else if (key == "music") settings.Music = Num(value, settings.Music);
            else if (key == "mute") settings.Muted = Flag(value);
            else if (key == "hud") settings.HudScale = Num(value, settings.HudScale);
            else if (key == "uiScale") settings.UiScale = Num(value, settings.UiScale);
            else if (key == "comic") settings.ComicWords = Flag(value);
            else if (key == "motion") settings.ReduceMotion = Flag(value);
            else if (key == "quality") settings.PictureQuality = (int)Num(value, settings.PictureQuality);
            else if (key == "res") settings.ResIndex = (int)Num(value, settings.ResIndex);
            else if (key == "full") settings.Fullscreen = Flag(value);
            else if (key == "vsync") settings.VSync = Flag(value);
            else if (key == "speedLines") settings.SpeedLines = Flag(value);
            else if (key == "colorblind") settings.Colorblind = Flag(value);
            else if (key == "cvdSeats") settings.CvdSeats = (int)Num(value, settings.CvdSeats);
            else if (key == "minimap") settings.Minimap = Flag(value);
            else if (key == "accessSeat") settings.AccessSeat = (int)Num(value, settings.AccessSeat);
            else if (SeatKey(key, "palette", out int paletteSeat)) settings.Palette[paletteSeat] = (int)Num(value, settings.Palette[paletteSeat]);
            else if (SeatKey(key, "captions", out int captionSeat)) settings.Captions[captionSeat] = Flag(value);
            else if (SeatKey(key, "rumble", out int rumbleSeat)) settings.Rumble[rumbleSeat] = (int)Num(value, settings.Rumble[rumbleSeat]);
            else if (SeatKey(key, "flash", out int flashSeat)) settings.ReduceFlash[flashSeat] = Flag(value);
            else if (key == "arena")
            {
                settings.Arena = (int)Num(value, settings.Arena);
                MenuSession.Arena = settings.Arena;
            }
            else if (key == "ai") settings.AiOpponents = (int)Num(value, settings.AiOpponents);
            else if (key == "diff") settings.DifficultyTier = (int)Num(value, settings.DifficultyTier);
            else if (key == "roundLen") settings.RoundLengthIndex = (int)Num(value, settings.RoundLengthIndex);
            else if (key == "rounds") settings.RoundsPerMatch = (int)Num(value, settings.RoundsPerMatch);
            else if (key == "split") settings.SplitAxis = (int)Num(value, settings.SplitAxis);
            else if (key == "listen") settings.Listener = (int)Num(value, settings.Listener);
            else if (key == "wallRibbon") settings.WallRibbon = Flag(value);
            else if (key == "inkCard") settings.InkCard = Flag(value);
            else if (key == "itWedge") settings.ItWedge = Flag(value);
            else if (key == "crownPlate") settings.CrownPlate = Flag(value);
            else if (key == "edgeStreaks") settings.EdgeStreaks = Flag(value);
            else if (key == "contactMarks") settings.ContactMarks = Flag(value);
            else if (key == "wallMarks") settings.WallMarks = Flag(value);
            else if (key == "bodyStroke") settings.BodyStroke = Flag(value);
            else if (key == "seatInk") settings.SeatInk = Flag(value);
            else if (key == "oneWord") settings.OneWord = Flag(value);
            else if (key == "ribbonLane") settings.RibbonLane = Flag(value);
            else if (key == "strokeYield") settings.StrokeYield = Flag(value);
            else if (key == "landStain") settings.LandStain = Flag(value);
            else if (key == "effects") settings.Effects = (int)Num(value, settings.Effects);
            else if (key == "startIt") settings.StartIt = (int)Num(value, settings.StartIt);
            else if (key == "startSeat") settings.StartSeat = (int)Num(value, settings.StartSeat);
            else if (key == "winTarget") settings.WinTarget = (int)Num(value, settings.WinTarget);
            else if (key == "pads") settings.HazardPads = Flag(value);
            else if (key == "zips") settings.HazardZips = Flag(value);
            else if (SeatKey(key, "confirm", out int confirmSeat)) settings.ConfirmFace[confirmSeat] = (int)Num(value, settings.ConfirmFace[confirmSeat]);
            else if (SeatKey(key, "hand", out int handSeat)) settings.Handicap[handSeat] = (int)Num(value, settings.Handicap[handSeat]);
            else if (key == "jumpAlt")
            {
                if (ActionBinds.KnownKeyboard(value))
                    binds.SetJumpAlt(value);
            }
            else if (key == "grappleKey")
            {
                if (ActionBinds.KnownKeyboard(value))
                    binds.SetGrappleKey(value);
            }
            else if (key == "grapplePad")
            {
                if (!string.IsNullOrEmpty(value))
                    binds.SetGrapplePad(value);
            }
            else if (key.StartsWith("kb.", StringComparison.Ordinal))
                Assign(binds, key.Substring(3), value, true);
            else if (key.StartsWith("pad.", StringComparison.Ordinal))
                Assign(binds, key.Substring(4), value, false);
            else if (key.StartsWith("pb.", StringComparison.Ordinal))
                PracticeBests.SetTime(key.Substring(3), Num(value, 0f));
            else if (key.StartsWith("sp.", StringComparison.Ordinal))
                PracticeBests.SetSplits(key.Substring(3), value);
            else if (key.StartsWith("gh.", StringComparison.Ordinal))
                PracticeGhost.Read(key.Substring(3), value);
            else if (!MenuSession.ApplyKey(key, value))
                LocalProfiles.ApplyKey(key, value);
        }

        static bool SeatKey(string key, string prefix, out int seat)
        {
            seat = 0;
            if (key == prefix) return true;
            if (!key.StartsWith(prefix, StringComparison.Ordinal)) return false;
            string tail = key.Substring(prefix.Length);
            if (tail.Length != 1 || tail[0] < '1' || tail[0] > '3') return false;
            seat = tail[0] - '0';
            return true;
        }

        static void Assign(ActionBinds binds, string name, string value, bool keyboard)
        {
            if (string.IsNullOrEmpty(value)) return;
            if (!Enum.TryParse(name, false, out PlayAction action)) return;
            if (action < 0 || action >= PlayAction.Count) return;
            if (keyboard && action == PlayAction.Jump && !ActionBinds.KnownKeyboard(value))
                value = "space";
            if (keyboard) binds.SetKeyboard(action, value);
            else binds.SetGamepad(action, value);
        }

        static float Num(string value, float fallback)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float n)
                && !float.IsNaN(n) && !float.IsInfinity(n))
                return n;
            return fallback;
        }

        static bool Flag(string value)
        {
            return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        static void Line(StringBuilder text, string key, float value)
        {
            text.Append(key);
            text.Append('=');
            text.Append(value.ToString("0.###", CultureInfo.InvariantCulture));
            text.Append('\n');
        }
    }
}
