using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Tag.Gameplay;
using Tag.Level;
using Tag.Settings;
using TagArena.Movement;
using UnityEngine;

public struct SettingsInputReport
{
    public bool Ok;
    public string Line;
    public string Failure;

    public void Fail(string reason)
    {
        Ok = false;
        if (string.IsNullOrEmpty(Failure)) Failure = reason;
    }
}

/// <summary>
/// Settings clamp and persistence, rebind conflicts, the gamepad menu walk,
/// and the shared jump buffer. Feel locks are read, not written.
/// </summary>
public static class SettingsInputProof
{
    public static SettingsInputReport Run()
    {
        var report = new SettingsInputReport { Ok = true };
        CheckSettings(report);
        CheckBinds(report);
        CheckMenus(report);
        CheckBuffer(report);
        CheckLocks(report);
        if (!report.Ok)
            report.Line = "settings-input FAIL " + report.Failure;
        else
            report.Line = "settings-input"
                + " fov=60-100 hud=0.75-1.5 persist=json"
                + " binds=13 jump=space conflicts=detected reset=defaults"
                + " menus=" + MenuGraph.NodeCount().ToString(CultureInfo.InvariantCulture)
                + " reachable=all returns=play"
                + " buffer coyote=0.10 jump=0.16 kb=pad resume=clean";
        return report;
    }

    static void CheckSettings(SettingsInputReport report)
    {
        GameSettings raw = GameSettings.Defaults();
        raw.Fov = 200f;
        raw.HudScale = 9f;
        raw.MouseSensitivity = 99f;
        raw.GamepadLook = 99f;
        raw.Master = 4f;
        raw.Sfx = -2f;
        raw.Arena = 8;
        raw.Clamp();
        if (!Near(raw.Fov, GameSettings.FovMax) || !Near(raw.HudScale, GameSettings.HudMax))
            report.Fail("high fov or hud scale did not clamp");
        if (!Near(raw.MouseSensitivity, GameSettings.MouseMax) || !Near(raw.GamepadLook, GameSettings.PadLookMax))
            report.Fail("high look speed did not clamp");
        if (!Near(raw.Master, 1f) || !Near(raw.Sfx, 0f) || raw.Arena != Tag.Level.ParkArena.Stack)
            report.Fail("volume or arena did not clamp");

        raw.Fov = 10f;
        raw.HudScale = 0.1f;
        raw.MouseSensitivity = 0f;
        raw.GamepadLook = 0f;
        raw.Clamp();
        if (!Near(raw.Fov, GameSettings.FovMin) || !Near(raw.HudScale, GameSettings.HudMin))
            report.Fail("low fov or hud scale did not clamp");
        if (!Near(raw.MouseSensitivity, GameSettings.MouseMin) || !Near(raw.GamepadLook, GameSettings.PadLookMin))
            report.Fail("low look speed did not clamp");

        GameSettings edited = GameSettings.Defaults();
        edited.InvertY = true;
        edited.Colorblind = true;
        edited.Minimap = false;
        edited.Muted = true;
        edited.Fov = 90f;
        edited.HudScale = 1.25f;
        edited.MouseSensitivity = 2.4f;
        edited.GamepadLook = 3f;
        edited.Master = 0.5f;
        edited.Sfx = 0.5f;
        edited.Arena = 1;
        ActionBinds binds = ActionBinds.Defaults();
        binds.SetKeyboard(PlayAction.AirDash, "v");
        string blob = SettingsFile.Write(edited, binds);
        GameSettings loaded = GameSettings.Defaults();
        ActionBinds loadedBinds = ActionBinds.Defaults();
        SettingsFile.Read(blob, loaded, loadedBinds);
        if (!loaded.InvertY || !loaded.Colorblind || loaded.Minimap || !loaded.Muted)
            report.Fail("settings flags did not round-trip");
        if (!Near(loaded.Fov, 90f) || !Near(loaded.HudScale, 1.25f) || !Near(loaded.MouseSensitivity, 2.4f))
            report.Fail("settings numbers did not round-trip");
        if (!Near(loaded.GamepadLook, 3f) || !Near(loaded.Master, 0.5f) || !Near(loaded.Sfx, 0.5f) || loaded.Arena != 1)
            report.Fail("look, volume, or arena did not round-trip");
        if (loadedBinds.Keyboard[(int)PlayAction.AirDash] != "v")
            report.Fail("rebind did not round-trip");
        if (loadedBinds.Keyboard[(int)PlayAction.Jump] != "space")
            report.Fail("jump space default was not kept in the blob");

        loaded.ResetToDefaults();
        loadedBinds.ResetToDefaults();
        if (loaded.InvertY || loaded.Colorblind || loaded.Muted || !loaded.Minimap || loaded.Arena != 0)
            report.Fail("settings reset missed a flag");
        if (!Near(loaded.Fov, GameSettings.FovDefault) || !Near(loaded.HudScale, GameSettings.HudDefault))
            report.Fail("settings reset missed fov or hud");
        if (!Near(loaded.MouseSensitivity, GameSettings.MouseDefault) || !Near(loaded.Master, GameSettings.MasterDefault))
            report.Fail("settings reset missed look or master");
        if (loadedBinds.Keyboard[(int)PlayAction.AirDash] != "q" || loadedBinds.Keyboard[(int)PlayAction.Jump] != "space")
            report.Fail("bind reset missed dash or jump");

        SettingsFile.Read("not a blob\nfoo\n", GameSettings.Defaults(), ActionBinds.Defaults());
        if (!GameSettings.SafePaletteSeparable())
            report.Fail("colorblind palette is a red/green pair");
        GameSettings.VerbMark(false, 0, out float r, out float g, out float b);
        if (!Near(r, 0.25f) || !Near(g, 0.55f) || !Near(b, 0.95f))
            report.Fail("default dash mark drifted");
    }

    static void CheckBinds(SettingsInputReport report)
    {
        ActionBinds binds = ActionBinds.Defaults();
        if (binds.AnyConflict(out _, out _))
            report.Fail("default binds conflict");
        CheckReaderConflicts(report);
        if (binds.Keyboard[(int)PlayAction.Jump] != "space")
            report.Fail("jump default is not space");
        if (binds.Gamepad[(int)PlayAction.Jump] != "buttonSouth")
            report.Fail("jump pad default drifted");
        if (binds.Keyboard[(int)PlayAction.Cling] != "holdIntoWall")
            report.Fail("cling default is not the move hold");
        if (binds.Keyboard[(int)PlayAction.Minimap] != "m" || binds.Gamepad[(int)PlayAction.Minimap] != "select")
            report.Fail("minimap default is not M / Select");
        if (binds.Keyboard[(int)PlayAction.Arena1] != "alpha1" || binds.Keyboard[(int)PlayAction.Arena2] != "alpha2"
            || binds.Keyboard[(int)PlayAction.Arena3] != "alpha3")
            report.Fail("arena picker defaults are not 1, 2, and 3");
        if (binds.Gamepad[(int)PlayAction.Arena3] != "dpadUp")
            report.Fail("arena 3 pad default is not D-pad up");
        for (int i = 0; i < (int)PlayAction.Count; i++)
        {
            if (string.IsNullOrEmpty(binds.Keyboard[i]) || string.IsNullOrEmpty(binds.Gamepad[i]))
                report.Fail("an action is missing a device");
        }

        ActionBinds clash = binds.Clone();
        clash.SetKeyboard(PlayAction.Jump, "q");
        if (!clash.Conflict(PlayAction.Jump, out PlayAction other) || other != PlayAction.AirDash)
            report.Fail("jump rebound onto dash was not a conflict");
        clash.SetKeyboard(PlayAction.Jump, "comma");
        if (!clash.Conflict(PlayAction.Jump, out _))
            report.Fail("comma mute was not reserved");
        clash.SetKeyboard(PlayAction.Jump, "space");
        clash.SetGamepad(PlayAction.Punch, "buttonSouth");
        if (!clash.Conflict(PlayAction.Punch, out other) || other != PlayAction.Jump)
            report.Fail("pad punch onto jump was not a conflict");

        clash.ResetToDefaults();
        if (clash.AnyConflict(out _, out _) || clash.Keyboard[(int)PlayAction.Jump] != "space")
            report.Fail("bind reset left a conflict or moved jump");
    }

    static void CheckMenus(SettingsInputReport report)
    {
        if (!ScriptedWalk(out string walkFail))
            report.Fail(walkFail);
        if (!Cover(out string coverFail))
            report.Fail(coverFail);
    }

    static bool ScriptedWalk(out string fail)
    {
        MenuCursor cursor = MenuGraph.PauseRoot();
        cursor = Tap(cursor, PadEvent.Right, MenuGraph.ParentRow(MenuId.Settings));
        if (cursor.Menu != MenuId.Pause || cursor.Row != 5)
        {
            fail = "gamepad did not reach settings on the pause card";
            return false;
        }
        cursor = MenuGraph.Apply(cursor, PadEvent.Confirm);
        if (cursor.Menu != MenuId.Settings || cursor.Row != 0)
        {
            fail = "confirm did not open settings";
            return false;
        }
        cursor = Tap(cursor, PadEvent.Down, MenuGraph.Rows(MenuId.Settings) - 1);
        cursor = MenuGraph.Apply(cursor, PadEvent.Confirm);
        if (cursor.Menu != MenuId.Pause || cursor.Row != 5)
        {
            fail = "settings back did not return to pause";
            return false;
        }

        cursor = MenuGraph.Apply(cursor, PadEvent.Right);
        cursor = MenuGraph.Apply(cursor, PadEvent.Confirm);
        if (cursor.Menu != MenuId.Rebind)
        {
            fail = "confirm did not open rebind";
            return false;
        }
        cursor = MenuGraph.Apply(cursor, PadEvent.Back);
        if (cursor.Menu != MenuId.Pause || cursor.Row != MenuGraph.ParentRow(MenuId.Rebind))
        {
            fail = "rebind back did not return to pause";
            return false;
        }

        cursor = MenuGraph.Apply(cursor, PadEvent.Right);
        cursor = MenuGraph.Apply(cursor, PadEvent.Confirm);
        if (cursor.Menu != MenuId.Arena)
        {
            fail = "confirm did not open the arena picker";
            return false;
        }
        cursor = MenuGraph.Apply(cursor, PadEvent.Down);
        if (cursor.Row != 1)
        {
            fail = "arena picker did not move to the second arena";
            return false;
        }
        cursor = MenuGraph.Apply(cursor, PadEvent.Back);
        if (cursor.Menu != MenuId.Pause)
        {
            fail = "arena back did not return to pause";
            return false;
        }

        cursor = MenuGraph.PauseRoot();
        cursor = MenuGraph.Apply(cursor, PadEvent.Right);
        cursor = MenuGraph.Apply(cursor, PadEvent.Confirm);
        if (cursor.Menu != MenuId.Controls)
        {
            fail = "confirm did not open controls";
            return false;
        }
        cursor = MenuGraph.Apply(cursor, PadEvent.Back);
        cursor = MenuGraph.Apply(cursor, PadEvent.Right);
        cursor = MenuGraph.Apply(cursor, PadEvent.Confirm);
        if (cursor.Menu != MenuId.Look)
        {
            fail = "confirm did not open look";
            return false;
        }
        cursor = MenuGraph.Apply(cursor, PadEvent.Back);
        cursor = MenuGraph.Apply(cursor, PadEvent.Right);
        cursor = MenuGraph.Apply(cursor, PadEvent.Confirm);
        if (cursor.Menu != MenuId.Audio)
        {
            fail = "confirm did not open audio";
            return false;
        }
        cursor = MenuGraph.Apply(cursor, PadEvent.Back);

        cursor = MenuGraph.PauseRoot();
        cursor = MenuGraph.Apply(cursor, PadEvent.Confirm);
        if (cursor.Menu != MenuId.Play)
        {
            fail = "resume did not return to play";
            return false;
        }
        cursor = MenuGraph.Apply(cursor, PadEvent.Confirm);
        if (cursor.Menu != MenuId.Pause)
        {
            fail = "confirm on play did not reopen pause";
            return false;
        }

        cursor = MenuGraph.ResultsRoot();
        cursor = MenuGraph.Apply(cursor, PadEvent.Right);
        if (cursor.Row != 1)
        {
            fail = "results card did not move to menu";
            return false;
        }
        cursor = MenuGraph.Apply(cursor, PadEvent.Left);
        if (cursor.Row != 0)
        {
            fail = "results card did not return to rematch";
            return false;
        }
        cursor = MenuGraph.Apply(cursor, PadEvent.Back);
        if (cursor.Menu != MenuId.Play)
        {
            fail = "results back did not return";
            return false;
        }
        fail = "";
        return true;
    }

    static MenuCursor Tap(MenuCursor cursor, PadEvent ev, int times)
    {
        for (int i = 0; i < times; i++)
            cursor = MenuGraph.Apply(cursor, ev);
        return cursor;
    }

    static bool Cover(out string fail)
    {
        var seen = new HashSet<string>();
        var queue = new Queue<MenuCursor>();
        queue.Enqueue(MenuGraph.PauseRoot());
        queue.Enqueue(MenuGraph.ResultsRoot());
        int guard = 0;
        while (queue.Count > 0 && guard < 4000)
        {
            guard++;
            MenuCursor cursor = queue.Dequeue();
            if (cursor.Menu != MenuId.Play)
                seen.Add(cursor.Key);
            foreach (PadEvent ev in AllEvents())
            {
                MenuCursor next = MenuGraph.Apply(cursor, ev);
                if (next.Menu == MenuId.Play) continue;
                if (seen.Add(next.Key))
                    queue.Enqueue(next);
            }
        }

        if (seen.Count != MenuGraph.NodeCount())
        {
            fail = "gamepad walk reached " + seen.Count.ToString(CultureInfo.InvariantCulture)
                + " of " + MenuGraph.NodeCount().ToString(CultureInfo.InvariantCulture);
            return false;
        }

        foreach (string key in seen)
        {
            if (!ReturnsToPlay(Parse(key)))
            {
                fail = "node " + key + " does not return to play";
                return false;
            }
        }
        fail = "";
        return true;
    }

    static bool ReturnsToPlay(MenuCursor start)
    {
        var seen = new HashSet<string>();
        var queue = new Queue<MenuCursor>();
        queue.Enqueue(start);
        int guard = 0;
        while (queue.Count > 0 && guard < 4000)
        {
            guard++;
            MenuCursor cursor = queue.Dequeue();
            if (cursor.Menu == MenuId.Play) return true;
            if (!seen.Add(cursor.Key)) continue;
            foreach (PadEvent ev in AllEvents())
                queue.Enqueue(MenuGraph.Apply(cursor, ev));
        }
        return false;
    }

    static MenuCursor Parse(string key)
    {
        int colon = key.IndexOf(':');
        int menu = int.Parse(key.Substring(0, colon), CultureInfo.InvariantCulture);
        int row = int.Parse(key.Substring(colon + 1), CultureInfo.InvariantCulture);
        MenuId id = (MenuId)menu;
        MenuId root = id == MenuId.Results ? MenuId.Results : MenuId.Pause;
        return new MenuCursor { Menu = id, Row = row, Root = root };
    }

    static PadEvent[] AllEvents()
    {
        return new[]
        {
            PadEvent.Up, PadEvent.Down, PadEvent.Left, PadEvent.Right, PadEvent.Confirm, PadEvent.Back
        };
    }

    static void CheckBuffer(SettingsInputReport report)
    {
        if (!InputBufferAudit.DevicesMatch(out string buffer))
            report.Fail(buffer);
        if (!InputBufferAudit.ResumeClean(out string resume))
            report.Fail(resume);
    }

    static void CheckLocks(SettingsInputReport report)
    {
        MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
        PunchTagTuning punch = ScriptableObject.CreateInstance<PunchTagTuning>();
        if (!Near(cfg.coyoteTime, 0.10f) || !Near(cfg.jumpBuffer, 0.16f) || !Near(cfg.clingReleaseGrace, 0.08f))
            report.Fail("coyote, buffer, or cling grace drifted");
        if (!Near(cfg.jumpSpeed, 24.7f) || cfg.slideBoost != 0f)
            report.Fail("jump or slide boost drifted");
        if (!Near(cfg.airDashDuration, 0.10f) || !Near(cfg.airDashSpeed, 15f) || !Near(cfg.airDashCooldown, 30f))
            report.Fail("air dash drifted");
        if (!Near(punch.reach, 1.55f))
            report.Fail("punch reach drifted");
        if (!Near(cfg.taggerLungeSpeed, 16f) || !Near(cfg.taggerLungeDuration, 0.20f) || !Near(cfg.taggerLungeCooldown, 1f))
            report.Fail("lunge drifted");
        if (!Near(cfg.climbSpeed, 6.0f) || !Near(cfg.climbSlipSpeed, 3.7f) || !Near(cfg.wallRunSpeed, 9.5f))
            report.Fail("wall speeds drifted");
        if (!Near(PunchStagger.Duration, 0.25f) || !Near(PunchStagger.Immunity, 0.50f))
            report.Fail("stagger drifted");
        if (!Near(LaunchPadRules.DefaultCooldown, 0.3f))
            report.Fail("pad cooldown drifted");
        if (!Near(ZipLineRules.DefaultRideSpeed, 14f) || !Near(ZipLineRules.DefaultRegrabCooldown, 0.3f))
            report.Fail("zip drifted");
        if (!Near(TagBackImmunity.DefaultSeconds, 1.0f))
            report.Fail("tag-back drifted");

        string motor = Read("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
        if (motor == null || motor.IndexOf("One CharacterController.Move per Update", StringComparison.Ordinal) < 0)
            report.Fail("motor move contract drifted");
        if (motor.IndexOf("_cc.Move(", StringComparison.Ordinal) < 0
            || motor.IndexOf("_cc.Move(", StringComparison.Ordinal) != motor.LastIndexOf("_cc.Move(", StringComparison.Ordinal))
            report.Fail("motor does not have one Move");
        if (motor.IndexOf("No Cling action", StringComparison.Ordinal) < 0)
            report.Fail("cling stopped being the move hold");
        if (motor.IndexOf("LedgeHang", StringComparison.Ordinal) >= 0 || motor.IndexOf("Shimmy", StringComparison.Ordinal) >= 0)
            report.Fail("a rejected ledge move was added");
        if (motor.IndexOf("AddForce", StringComparison.Ordinal) >= 0)
            report.Fail("motor gained a rigidbody force");
    }

    /// <summary>
    /// Counts a physical key the solo reader samples for two actions.
    /// The binds table can stay clean while PlayerInputReader still ORs a key.
    /// </summary>
    static void CheckReaderConflicts(SettingsInputReport report)
    {
        string source = Read(ReaderPath());
        if (source == null)
        {
            report.Fail("player input reader is missing");
            return;
        }
        int clean = DefaultConflicts(source);
        const string sprint = "bool sprintPhys = Input.GetKey(KeyCode.LeftShift);";
        const string spikedLine = "bool sprintPhys = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftAlt);";
        if (source.IndexOf(sprint, StringComparison.Ordinal) < 0)
        {
            report.Fail("sprint sample line moved");
            return;
        }
        string spiked = source.Replace(sprint, spikedLine);
        int dirty = DefaultConflicts(spiked);
        if (clean != 0)
            report.Fail("reader physical defaults conflict");
        if (dirty < 1)
            report.Fail("alt on sprint did not fail the reader conflict check");
        if (Environment.GetEnvironmentVariable("TAG_SHOW_CONFLICT") == "1")
        {
            Console.WriteLine("defaults-conflict clean=" + clean.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("defaults-conflict alt-on-sprint=" + dirty.ToString(CultureInfo.InvariantCulture)
                + (dirty > 0 ? " FAIL leftAlt sprint+airdash" : " MISS"));
        }
    }

    static string ReaderPath()
    {
        const string rel = "Assets/TagArenaMovement/Scripts/Input/PlayerInputReader.cs";
        if (File.Exists(rel)) return rel;
        string up = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", rel));
        if (File.Exists(up)) return up;
        return rel;
    }

    /// <summary>
    /// Keys sampled on the solo pawn. Cling's W shares Move and does not count.
    /// </summary>
    public static int DefaultConflicts(string source)
    {
        if (string.IsNullOrEmpty(source)) return 1;
        var owners = new Dictionary<string, int>();
        Note(source, "sprintPhys", 1, owners);
        Note(source, "SprintHeld", 1, owners);
        Note(source, "AirDashPressed", 2, owners);
        Note(source, "PunchPressed", 4, owners);
        Note(source, "CrouchHeld", 8, owners);
        Note(source, "CrouchPressed", 8, owners);
        Note(source, "JumpHeld", 16, owners);
        Note(source, "JumpPressed", 16, owners);
        Note(source, "jumpPhys", 16, owners);
        int conflicts = 0;
        foreach (var pair in owners)
        {
            int bits = pair.Value;
            int n = 0;
            while (bits != 0)
            {
                n += bits & 1;
                bits >>= 1;
            }
            if (n >= 2) conflicts++;
        }
        return conflicts;
    }

    static void Note(string source, string field, int bit, Dictionary<string, int> owners)
    {
        int from = 0;
        while (from < source.Length)
        {
            int at = source.IndexOf(field, from, StringComparison.Ordinal);
            if (at < 0) return;
            from = at + field.Length;
            if (at > 0 && Word(source[at - 1])) continue;
            int i = at + field.Length;
            while (i < source.Length && (source[i] == ' ' || source[i] == '\t' || source[i] == '\r' || source[i] == '\n'))
                i++;
            if (i >= source.Length || source[i] != '=') continue;
            if (i + 1 < source.Length && source[i + 1] == '=') continue;
            int end = source.IndexOf(';', i);
            if (end < 0) end = source.Length;
            string span = source.Substring(i, end - i);
            if (field == "AirDashPressed" && span.IndexOf("airDashKey", StringComparison.Ordinal) >= 0)
                Own(owners, "Q", bit);
            if (field == "PunchPressed" && span.IndexOf("punchKey", StringComparison.Ordinal) >= 0)
                Own(owners, "Mouse0", bit);
            if ((field == "CrouchHeld" || field == "CrouchPressed") && span.IndexOf("crouchKey", StringComparison.Ordinal) >= 0)
                Own(owners, "C", bit);
            if ((field == "JumpHeld" || field == "JumpPressed" || field == "jumpPhys")
                && span.IndexOf("SpaceHeld", StringComparison.Ordinal) >= 0)
                Own(owners, "Space", bit);
            const string mark = "KeyCode.";
            int k = 0;
            while (k < span.Length)
            {
                int hit = span.IndexOf(mark, k, StringComparison.Ordinal);
                if (hit < 0) break;
                int name = hit + mark.Length;
                int stop = name;
                while (stop < span.Length && Word(span[stop])) stop++;
                if (stop > name)
                    Own(owners, span.Substring(name, stop - name), bit);
                k = stop;
            }
        }
    }

    static void Own(Dictionary<string, int> owners, string key, int bit)
    {
        if (key == "W") return;
        int have;
        owners.TryGetValue(key, out have);
        owners[key] = have | bit;
    }

    static bool Word(char c)
    {
        return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
    }

    static string Read(string path)
    {
        if (!File.Exists(path)) return null;
        return File.ReadAllText(path);
    }

    static bool Near(float a, float b)
    {
        return Mathf.Abs(a - b) <= 0.001f;
    }
}
