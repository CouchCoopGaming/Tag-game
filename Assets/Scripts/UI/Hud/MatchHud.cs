using System.Collections.Generic;
using Tag.Core;
using Tag.Couch;
using Tag.Experimental;
using Tag.Gameplay;
using Tag.MatchStats;
using Tag.Modes;
using Tag.Profiles;
using Tag.Settings;
using Tag.Ui.Menu;
using TagArena.Movement;
using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Hud
{
    public sealed class HudPane
    {
        public RectTransform Root;
        public RectTransform Identity;
        public RectTransform Verbs;
        public Image EdgeT;
        public Image EdgeB;
        public Image EdgeL;
        public Image EdgeR;
        public Image Glow;
        public Image Badge;
        public Text Name;
        public Text Profile;
        public Text Metric;
        public Text Value;
        public Text Tags;
        public Text TagsValue;
        public Text BadgeWord;
        public Text Call;
        public Image LockPlate;
        public Text Lock;
        public Image WordSlot;
        public Image DashBg;
        public Image DashFill;
        public Text DashWord;
        public Image RopeMark;
        public Text RopeWord;
        public Image SafeBg;
        public Image SafeFill;
        public Text SafeWord;
        public Image Arrow;
        public Image CompassPlate;
        public Text Compass;
        public Text Timer;
        public Text TimerRound;
        public Image ItPlate;
        public Text ItBig;
        public Image SeatMark;
        public Image SafeGlow;
        public CanvasGroup ItFrame;
        public RectTransform Board;
        public Text BoardTitle;
        public Text BoardRound;
        public Text BoardClock;
        public readonly Text[] BoardName = new Text[4];
        public readonly Text[] BoardValue = new Text[4];
        public readonly Text[] Feed = new Text[3];
        public readonly Image[] FeedPlate = new Image[3];
        public readonly Image[] FeedChip = new Image[3];
        public readonly Image[] FeedMark = new Image[3];
    }

    /// <summary>
    /// Couch match HUD. Replaces the OnGUI cluster while Tag.Ui.Legacy is off.
    /// Update only reads cached strings and writes widget fields.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class MatchHud : MonoBehaviour
    {
        public static MatchHud Instance { get; private set; }

        public static bool Active => Instance != null && !MenuHost.Legacy;

        public Canvas Root;
        public CanvasScaler Scaler;
        public Text Clock;
        public Text RoundLabel;
        public Text ComicHint;
        public Text CenterCall;
        public Image CenterPlate;
        public RectTransform ScoreRoot;
        public Text ScoreTitle;
        public readonly Text[] ScoreLine = new Text[4];
        public readonly Image[] ScoreChip = new Image[4];
        public readonly Image[] ScoreMark = new Image[4];
        public readonly Text[] ScoreRank = new Text[4];
        public readonly Text[] ScoreTime = new Text[4];
        public readonly Text[] ScoreTagsN = new Text[4];
        public readonly Text[] ScoreGot = new Text[4];
        public readonly Text[] ScoreWins = new Text[4];
        public readonly Image[] ScoreHi = new Image[4];
        public readonly RectTransform[] ScoreSlide = new RectTransform[4];
        public readonly Text[] ScoreHead = new Text[5];
        public Text ScoreFoot;
        public readonly HudPane[] Panes = new HudPane[4];
        public readonly ItController[] Pawns = new ItController[4];
        public readonly Camera[] Cams = new Camera[4];
        public readonly PlayerMotor[] Motors = new PlayerMotor[4];
        public readonly ExperimentalGrapple[] Ropes = new ExperimentalGrapple[4];
        public readonly int[] Seat = new int[4];
        public int Humans;
        public int Split;
        public int Shown;

        readonly int[] _wasIt = new int[4];
        readonly int[] _callKind = new int[4];
        readonly float[] _callUntil = new float[4];
        MatchPhase _phase = MatchPhase.Idle;
        float _goUntil;
        float _endUntil;
        int _layout = -1;
        int _preview;
        bool _live;
        float _uiScale = -1f;
        int _tagSerial = -1;
        readonly int[] _rank = { 0, 1, 2, 3 };
        readonly bool[] _wasIn = new bool[4];
        int _leftSeat = -1;
        bool _rosterSeen;
        bool _cardOn;
        float _cardAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Auto()
        {
            if (MenuHost.Legacy) return;
            Ensure();
        }

        public static MatchHud Ensure()
        {
            if (MenuHost.Legacy) return null;
            if (Instance != null) return Instance;
            var go = new GameObject("MatchHud");
            DontDestroyOnLoad(go);
            return go.AddComponent<MatchHud>();
        }

        public static void Preview(int humans)
        {
            MatchHud hud = Ensure();
            if (hud == null) return;
            if (humans < 1) humans = 1;
            if (humans > 4) humans = 4;
            hud._preview = humans;
            hud._layout = -1;
        }

        public static void EndPreview()
        {
            if (Instance == null) return;
            Instance._preview = 0;
            Instance._layout = -1;
        }

        public void LayoutDirty()
        {
            _layout = -1;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            for (int i = 0; i < 4; i++) _wasIt[i] = -1;
            MatchHudView.Build(this);
            if (Root != null) Root.enabled = false;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            ApplyScale();
            if (MenuHost.Legacy)
            {
                Hide();
                return;
            }
            if (_preview > 0)
            {
                PaintPreview();
                return;
            }
            TagModeController modes = TagModeController.Instance;
            if (modes == null || !modes.RoundActive || MenuHost.CoversFront || MenuHost.CoversResults)
            {
                Hide();
                return;
            }
            Show();
            MatchHudBind.Refresh(this);
            PaintLive(modes);
        }

        void Hide()
        {
            if (!_live && (Root == null || !Root.enabled)) return;
            _live = false;
            _tagSerial = -1;
            _leftSeat = -1;
            _rosterSeen = false;
            _cardOn = false;
            TagFeed.Reset();
            if (Root != null) Root.enabled = false;
            for (int i = 0; i < 4; i++)
            {
                _wasIt[i] = -1;
                HudPane pane = Panes[i];
                if (pane != null && pane.Board != null && pane.Board.gameObject.activeSelf)
                    pane.Board.gameObject.SetActive(false);
            }
        }

        void ApplyScale()
        {
            float s = UiFit.Current();
            if (Scaler == null || s == _uiScale) return;
            _uiScale = s;
            UiFit.Ref(s, out float w, out float h);
            Scaler.referenceResolution = new Vector2(w, h);
            _layout = -1;
        }

        void NoteTag(TagModeController modes)
        {
            int serial = modes.TagSerial;
            if (serial == _tagSerial) return;
            _tagSerial = serial;
            int from = SeatOf(modes, modes.LastFromId);
            int to = SeatOf(modes, modes.LastToId);
            if (from < 0 || to < 0) return;
            TagFeed.Push(from, to, Time.unscaledTime);
        }

        int SeatOf(TagModeController modes, string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            for (int i = 0; i < 4; i++)
            {
                ItController pawn = Pawns[i];
                if (pawn == null || pawn.PlayerId != id) continue;
                int seat = Seat[i];
                if (seat < 0 || seat > 3) seat = i;
                return seat;
            }
            IReadOnlyList<ItController> list = modes.PlayersForHud;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    ItController pawn = list[i];
                    if (pawn == null || pawn.PlayerId != id) continue;
                    if (i < 4) return i;
                }
            }
            for (int i = 0; i < 4; i++)
            {
                if (id == MatchHudText.Seat[i]) return i;
            }
            return -1;
        }

        void PaintFeed(int index)
        {
            HudPane pane = Panes[index];
            if (pane == null) return;
            float now = Time.unscaledTime;
            bool hold = MenuVideo.ReduceMotion;
            int shift = _leftSeat >= 0 && _leftSeat < MatchHudText.Left.Length ? 1 : 0;
            if (shift == 1)
                ShowFeedLine(pane, 0, _leftSeat, MatchHudText.Left[_leftSeat]);
            for (int row = 0; row < 3; row++)
            {
                int dest = row + shift;
                if (dest >= 3) break;
                Text line = pane.Feed[dest];
                if (line == null) continue;
                bool on = TagFeed.On(row, now);
                line.enabled = on;
                if (!on)
                {
                    if (pane.FeedPlate[dest] != null) pane.FeedPlate[dest].enabled = false;
                    if (pane.FeedChip[dest] != null) pane.FeedChip[dest].enabled = false;
                    if (pane.FeedMark[dest] != null) pane.FeedMark[dest].enabled = false;
                    continue;
                }
                Set(line, TagFeed.Text(row));
                Font face = MatchHudText.ComicWords ? MenuTheme.Display : MenuTheme.Font;
                if (line.font != face) line.font = face;
                float a = TagFeed.Alpha(row, now, hold);
                line.color = new Color(MenuTheme.Cream.r, MenuTheme.Cream.g, MenuTheme.Cream.b, a);
                int from = TagFeed.From(row);
                Color seat = MenuTheme.Seat(from);
                Image plate = pane.FeedPlate[dest];
                if (plate != null)
                {
                    plate.enabled = true;
                    plate.color = new Color(MenuTheme.Ink.r, MenuTheme.Ink.g, MenuTheme.Ink.b, a);
                }
                Image chip = pane.FeedChip[dest];
                if (chip != null)
                {
                    chip.enabled = true;
                    chip.color = new Color(seat.r, seat.g, seat.b, a);
                }
                Image mark = pane.FeedMark[dest];
                if (mark != null)
                {
                    bool shapes = SeatMarks();
                    mark.enabled = shapes;
                    if (shapes)
                    {
                        Sprite sprite = SeatShape.For(from);
                        if (mark.sprite != sprite) mark.sprite = sprite;
                        mark.color = new Color(MenuTheme.Cream.r, MenuTheme.Cream.g, MenuTheme.Cream.b, a);
                    }
                }
            }
        }

        void PaintBoard(TagModeController modes, int index)
        {
            HudPane pane = Panes[index];
            if (pane == null || pane.Board == null) return;
            int seat = Seat[index];
            if (seat < 0 || seat > 3) seat = index;
            int device = CouchPlay.DeviceOf(seat);
            bool show = device >= 0 && ScoreHold.Down(device);
            if (pane.Board.gameObject.activeSelf != show)
                pane.Board.gameObject.SetActive(show);
            if (!show) return;
            Set(pane.BoardTitle, ScorePeek.Title);
            Set(pane.BoardRound, MatchHudText.Round(modes.RoundShown, modes.RoundCap));
            if (modes.SelectedMode == TagModeId.FreePlay) Set(pane.BoardClock, MatchHudText.Free);
            else Set(pane.BoardClock, MatchHudText.Clock(modes.Remaining));
            for (int i = 0; i < 4; i++)
            {
                bool occupied = CouchPlay.HumanAt(i) || CouchPlay.AiAt(i);
                Text name = pane.BoardName[i];
                Text value = pane.BoardValue[i];
                if (name != null) name.enabled = occupied;
                if (value != null) value.enabled = occupied;
                if (!occupied) continue;
                string who = CouchPlay.Name(i);
                if (string.IsNullOrEmpty(who)) who = MatchHudText.Seat[i];
                Set(name, who);
                Set(value, ValueOf(modes, PawnForSeat(i)));
                CouchPlay.Tint(i, out float r, out float g, out float b);
                Color tint = new Color(r, g, b, 1f);
                if (name != null) name.color = tint;
                if (value != null) value.color = tint;
            }
        }

        ItController PawnForSeat(int seat)
        {
            for (int i = 0; i < 4; i++)
            {
                if (Pawns[i] == null) continue;
                int at = Seat[i];
                if (at < 0) at = i;
                if (at == seat) return Pawns[i];
            }
            return null;
        }

        static string ValueOf(TagModeController modes, ItController pawn)
        {
            if (modes == null || pawn == null) return MatchHudText.Off;
            TagModeId mode = modes.SelectedMode;
            if (mode == TagModeId.HotPotato) return HudDigits.Whole0(modes.RoundWinsOf(pawn.PlayerId));
            if (mode == TagModeId.TrailTag) return pawn.IsAlive ? MatchHudText.In : MatchHudText.Out;
            if (mode == TagModeId.FreePlay) return HudDigits.Whole0(pawn.TagsLanded);
            if (pawn.TimeAsIt >= 120f) return HudDigits.Whole0(pawn.TimeAsIt);
            return HudDigits.Tenth0(pawn.TimeAsIt);
        }

        static bool MatchPoint(TagModeController modes)
        {
            if (modes == null || modes.SelectedMode != TagModeId.HotPotato) return false;
            int need = 2;
            HotPotatoTuning tune = modes.HotPotatoTuningAsset;
            if (tune != null && tune.winsToTakeMatch > 1) need = tune.winsToTakeMatch;
            GameSettings menu = GameSettings.Current;
            if (menu != null && menu.WinTarget != GameSettings.WinTargetDefault)
                need = menu.WinTarget;
            int bar = need - 1;
            if (bar < 1) return false;
            IReadOnlyList<ItController> list = modes.PlayersForHud;
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++)
            {
                ItController pawn = list[i];
                if (pawn == null) continue;
                if (modes.RoundWinsOf(pawn.PlayerId) == bar) return true;
            }
            return false;
        }

        void Show()
        {
            _live = true;
            if (Root != null && !Root.enabled) Root.enabled = true;
        }

        void PaintPreview()
        {
            Show();
            int humans = _preview;
            int split = GameSettingsSplit();
            ApplyLayout(humans, split, true);
            Set(Clock, MatchHudText.Clock(84f));
            if (Clock != null)
            {
                Clock.color = MenuTheme.Cream;
                Clock.rectTransform.localScale = Vector3.one;
            }
            Set(RoundLabel, MatchHudText.Round(humans >= 4 ? 2 : 1, humans >= 4 ? 3 : 1));
            HudState.Snap snap = HudState.Script();
            int panes = CouchPlay.Panes(humans);
            for (int i = 0; i < 4; i++)
            {
                bool on = i < panes && !(humans == 3 && i == 3);
                if (!on) continue;
                PaintPreviewPane(i, snap, humans);
                PreviewAim(i, snap);
            }
            PaintPreviewScore(humans);
            bool center = humans <= 1;
            if (CenterPlate != null) CenterPlate.enabled = center;
            if (CenterCall != null)
            {
                CenterCall.enabled = center;
                Set(CenterCall, center ? MatchHudText.Comic(MatchHudText.Go) : MatchHudText.Blank);
                CenterCall.rectTransform.localScale = Vector3.one;
            }
        }

        void PaintLive(TagModeController modes)
        {
            int humans = Humans < 1 ? 1 : Humans;
            ApplyLayout(humans, Split, false);
            NoteRoster();
            if (modes.SelectedMode == TagModeId.FreePlay) Set(Clock, MatchHudText.Free);
            else Set(Clock, MatchHudText.Clock(modes.Remaining));
            bool hot = modes.SelectedMode != TagModeId.FreePlay && HudState.ClockHot(modes.Remaining);
            PulseClock(Clock, hot);
            Set(RoundLabel, MatchHudText.Round(modes.RoundShown, modes.RoundCap));
            PaintCenter(modes);
            NoteTag(modes);
            int panes = CouchPlay.Panes(humans);
            for (int i = 0; i < 4; i++)
            {
                bool on = i < panes && !(humans == 3 && i == 3);
                if (!on) continue;
                PaintPawn(modes, i);
                PaintArrow(modes, i);
                PaintFeed(i);
                PaintBoard(modes, i);
            }
            PaintScore(humans);
        }

        void PaintPreviewPane(int index, HudState.Snap snap, int humans)
        {
            HudPane pane = Panes[index];
            if (pane == null) return;
            bool it = index == snap.It;
            bool safe = index == snap.Safe;
            Set(pane.Name, MatchHudText.Seat[index]);
            Set(pane.Profile, MatchHudText.PreviewProfile[index]);
            Set(pane.Metric, MatchHudText.Metric(TagModeId.LeastIt));
            if (it) Set(pane.Value, HudDigits.Tenth0(0.2f));
            else if (safe) Set(pane.Value, HudDigits.Tenth0(12.4f));
            else if (index == 3) Set(pane.Value, HudDigits.Tenth0(4.0f));
            else Set(pane.Value, HudDigits.Tenth0(8.1f));
            Set(pane.Tags, MatchHudText.Tags);
            int tags = index == 0 || safe ? 1 : 0;
            Set(pane.TagsValue, HudDigits.Whole0(tags));
            if (pane.Timer != null && pane.Timer.transform.parent.gameObject.activeSelf)
            {
                Set(pane.Timer, MatchHudText.Clock(84f));
                Set(pane.TimerRound, MatchHudText.Round(humans >= 4 ? 2 : 1, humans >= 4 ? 3 : 1));
            }
            ShowItMark(pane, it);
            ShowSeatMark(pane, index);
            ShowItFrame(pane, it);
            ShowSafeGlow(pane, safe);
            PaintPreviewFeed(pane, snap);
            TintEdges(pane, MenuTheme.Seat(index));
            if (pane.Glow != null) pane.Glow.enabled = false;
            if (pane.Badge != null) pane.Badge.enabled = it;
            if (pane.BadgeWord != null) pane.BadgeWord.enabled = it;
            if (pane.DashFill != null) pane.DashFill.fillAmount = safe ? 0.4f : 1f;
            Set(pane.DashWord, safe ? HudDigits.DashCd(18f) : MatchHudText.DashLabel);
            Set(pane.RopeWord, MatchHudText.Blank);
            if (pane.RopeMark != null) pane.RopeMark.color = MenuTheme.Gold;
            float safeMax = TagBackImmunity.DefaultSeconds;
            if (safeMax < 0.01f) safeMax = 1f;
            if (pane.SafeFill != null) pane.SafeFill.fillAmount = safe ? snap.SafeLeft / safeMax : 0f;
            Set(pane.SafeWord, safe ? MatchHudText.SafeAt(snap.SafeLeft) : MatchHudText.Blank);
            if (pane.Call != null)
            {
                pane.Call.enabled = it;
                Set(pane.Call, it ? MatchHudText.Comic(MatchHudText.YoureIt) : MatchHudText.Blank);
                pane.Call.rectTransform.localScale = Vector3.one;
            }
        }

        void PaintPreviewFeed(HudPane pane, HudState.Snap snap)
        {
            for (int row = 0; row < 3; row++)
            {
                if (row >= snap.Count)
                {
                    HideFeedRow(pane, row);
                    continue;
                }
                int from = snap.From(row);
                ShowFeedLine(pane, row, from, TagFeed.Line[from, snap.To(row)]);
            }
        }

        void PaintPawn(TagModeController modes, int index)
        {
            HudPane pane = Panes[index];
            if (pane == null) return;
            int seat = Seat[index];
            if (seat < 0 || seat > 3) seat = index;
            ItController pawn = Pawns[index];
            string name = CouchPlay.Name(seat);
            if (string.IsNullOrEmpty(name)) name = MatchHudText.Seat[seat];
            Set(pane.Name, name);
            string profile = LocalProfiles.SeatName(seat);
            if (string.IsNullOrEmpty(profile)) profile = CouchPlay.SeatLine(seat);
            if (CouchPlay.AiAt(seat) && !CouchPlay.HumanAt(seat)) profile = MatchHudText.SoloAi;
            Set(pane.Profile, profile);
            TagModeId mode = modes.SelectedMode;
            Set(pane.Metric, MatchHudText.Metric(mode));
            Set(pane.Value, ValueOf(modes, pawn));
            Set(pane.Tags, MatchHudText.Tags);
            Set(pane.TagsValue, pawn != null ? HudDigits.Whole0(pawn.TagsLanded) : MatchHudText.Off);
            if (pane.Timer != null && pane.Timer.transform.parent.gameObject.activeSelf)
            {
                if (mode == TagModeId.FreePlay) Set(pane.Timer, MatchHudText.Free);
                else Set(pane.Timer, MatchHudText.Clock(modes.Remaining));
                Set(pane.TimerRound, MatchHudText.Round(modes.RoundShown, modes.RoundCap));
                bool paneHot = mode != TagModeId.FreePlay && HudState.ClockHot(modes.Remaining);
                PulseClock(pane.Timer, paneHot);
            }
            ShowSeatMark(pane, seat);

            bool isIt = pawn != null && pawn.IsIt;
            TintEdges(pane, SeatTint(seat));
            bool counting = modes.Phase == MatchPhase.Countdown;
            bool going = modes.Phase == MatchPhase.Playing && Time.unscaledTime < _goUntil;
            bool slim = counting && !going;
            ShowStats(pane, !slim);
            ShowLock(pane, slim);
            FitNamePlate(pane, slim);
            if (!slim && pane.Identity != null && pane.Identity.sizeDelta.y <= 64f)
                _layout = -1;
            if (counting || going)
            {
                int reveal = RevealSeat(modes);
                bool shown = isIt || seat == reveal;
                ShowItMark(pane, shown);
                ShowItFrame(pane, shown);
                if (pane.Badge != null) pane.Badge.enabled = shown;
                if (pane.BadgeWord != null) pane.BadgeWord.enabled = shown;
                ShowCountCall(pane, going ? HudState.Digit(3) : CountWord(modes.PhaseSeconds));
                ShowOpeningFlash(pane, counting && seat == reveal);
            }
            else
            {
                ShowItMark(pane, isIt);
                ShowItFrame(pane, isIt);
                if (pane.Badge != null) pane.Badge.enabled = isIt;
                if (pane.BadgeWord != null) pane.BadgeWord.enabled = isIt;
                if (pane.Glow != null) pane.Glow.enabled = false;
                PaintPawnCall(pane, index, isIt);
                ShowTagFlash(pane, index);
            }
            PaintVerbs(pane, index, pawn);
        }

        void PaintVerbs(HudPane pane, int index, ItController pawn)
        {
            PlayerMotor motor = Motors[index];
            float dashMax = 30f;
            float dashRem = 0f;
            bool dashing = false;
            if (motor != null)
            {
                if (motor.cfg != null && motor.cfg.airDashCooldown > 0.01f) dashMax = motor.cfg.airDashCooldown;
                dashRem = motor.AirDashCooldownRemaining;
                dashing = motor.IsAirDashing;
            }
            float dashFill = 0f;
            if (dashing) dashFill = 1f;
            else if (dashMax > 0.01f) dashFill = 1f - dashRem / dashMax;
            if (dashFill < 0f) dashFill = 0f;
            if (dashFill > 1f) dashFill = 1f;
            if (pane.DashFill != null) pane.DashFill.fillAmount = dashFill;
            string dashWord = MatchHudText.Blank;
            if (motor != null)
            {
                if (dashing) dashWord = MatchHudText.ComicWords ? MatchHudText.DashGo : MatchHudText.DashLabel;
                else if (dashRem > 0.05f) dashWord = HudDigits.DashCd(dashRem);
                else dashWord = MatchHudText.DashLabel;
            }
            Set(pane.DashWord, dashWord);
            if (pane.DashWord != null)
            {
                bool word = dashing || (motor != null && dashRem <= 0.05f);
                Font face = word && MatchHudText.ComicWords ? MenuTheme.Display : MenuTheme.Font;
                if (pane.DashWord.font != face) pane.DashWord.font = face;
            }

            ExperimentalGrapple rope = Ropes[index];
            string ropeWord = MatchHudText.Blank;
            Color ropeColor = new Color(0.55f, 0.62f, 0.72f, 0.4f);
            if (rope != null && rope.enableGrapple)
            {
                if (rope.Pulling)
                {
                    ropeWord = MatchHudText.Comic(MatchHudText.Pull);
                    ropeColor = MenuTheme.Ready;
                }
                else if (rope.IsPulling)
                {
                    ropeWord = MatchHudText.Comic(MatchHudText.Hook);
                    ropeColor = MenuTheme.Gold;
                }
                else if (rope.IsAiming)
                {
                    ropeWord = MatchHudText.Comic(MatchHudText.Aim);
                    ropeColor = MenuTheme.Cream;
                }
                else ropeColor = new Color(0.82f, 0.88f, 0.96f, 1f);
            }
            Set(pane.RopeWord, ropeWord);
            if (pane.RopeMark != null) pane.RopeMark.color = ropeColor;

            float safe = pawn != null ? pawn.TagBackRemaining : 0f;
            float safeMax = TagBackImmunity.DefaultSeconds;
            if (safeMax < 0.01f) safeMax = 1f;
            float safeFill = safe / safeMax;
            if (safeFill < 0f) safeFill = 0f;
            if (safeFill > 1f) safeFill = 1f;
            if (pane.SafeFill != null) pane.SafeFill.fillAmount = safeFill;
            Set(pane.SafeWord, safe > 0.001f ? MatchHudText.SafeAt(safe) : MatchHudText.Blank);
            ShowSafeGlow(pane, safe > 0.001f);
        }

        void PaintPawnCall(HudPane pane, int index, bool isIt)
        {
            int flag = isIt ? 1 : 0;
            int was = _wasIt[index];
            float now = Time.unscaledTime;
            float hold = MenuVideo.ReduceMotion ? 0.7f : 1.5f;
            if (was < 0 && isIt)
            {
                _callKind[index] = 1;
                _callUntil[index] = now + hold;
            }
            else if (was >= 0 && flag != was)
            {
                _callKind[index] = isIt ? 1 : 2;
                _callUntil[index] = now + hold;
            }
            _wasIt[index] = flag;
            bool show = now < _callUntil[index] && _callKind[index] != 0;
            if (pane.Call == null) return;
            pane.Call.enabled = show;
            bool tagged = show && _callKind[index] == 1;
            PlaceCall(pane, tagged);
            ShowWordSlot(pane, tagged);
            if (!show) return;
            Set(pane.Call, MatchHudText.Comic(_callKind[index] == 1 ? MatchHudText.YoureIt : MatchHudText.Tagged));
            if (MenuVideo.ReduceMotion)
            {
                pane.Call.rectTransform.localScale = Vector3.one;
                return;
            }
            float left = _callUntil[index] - now;
            float u = 1f - left / hold;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            float pop = u < 0.2f ? 1.2f - u : 1f;
            pane.Call.rectTransform.localScale = new Vector3(pop, pop, 1f);
        }

        void PaintCenter(TagModeController modes)
        {
            MatchPhase phase = modes.Phase;
            float now = Time.unscaledTime;
            if (phase != _phase)
            {
                if (_phase == MatchPhase.Countdown && phase == MatchPhase.Playing)
                    _goUntil = now + (MenuVideo.ReduceMotion ? 0.45f : 0.8f);
                if (phase == MatchPhase.PostRound)
                    _endUntil = now + (MenuVideo.ReduceMotion ? 0.7f : 1.4f);
                _phase = phase;
            }
            bool countdown = phase == MatchPhase.Countdown || now < _goUntil;
            bool point = MatchPoint(modes);
            bool sudden = modes.SuddenDeath || CouchPlay.TieText.Length > 0;
            int kind = RoundCard.Pick(sudden, countdown, modes.RoundShown, modes.RoundCap, point);
            string word = MatchHudText.Blank;
            bool standings = false;
            if (phase == MatchPhase.Countdown)
                word = kind == RoundCard.Sudden ? RoundCard.SuddenText : MatchHudText.Blank;
            else if (now < _goUntil) word = MatchHudText.Go;
            else if (phase == MatchPhase.PostRound && now < _endUntil)
                word = MatchHudText.RoundOver;
            else if (phase == MatchPhase.PostRound)
            {
                bool final = modes.RoundCap > 1 && modes.RoundShown >= modes.RoundCap;
                word = final ? HudState.AfterRound(true) : MatchHudText.Blank;
                standings = !final;
            }
            else if (kind == RoundCard.Point) word = RoundCard.PointText;
            if (standings) PaintStandings(modes);
            else HideStandings(Humans < 1 ? 1 : Humans);
            bool on = word.Length != 0;
            if (CenterPlate != null) CenterPlate.enabled = on;
            if (CenterCall == null) return;
            CenterCall.enabled = on;
            Set(CenterCall, word);
            PlaceCenter(standings ? -300f : 0f);
            if (!on) return;
            if (MenuVideo.ReduceMotion || word != MatchHudText.Go)
            {
                CenterCall.rectTransform.localScale = Vector3.one;
                return;
            }
            float span = 0.8f;
            float left = _goUntil - now;
            float u = 1f - left / span;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            float pop = u < 0.22f ? Mathf.Lerp(1.28f, 1f, u / 0.22f) : 1f;
            CenterCall.rectTransform.localScale = new Vector3(pop, pop, 1f);
        }

        void NoteRoster()
        {
            for (int i = 0; i < 4; i++)
            {
                bool inn = CouchPlay.HumanAt(i) || CouchPlay.AiAt(i);
                if (_rosterSeen && _wasIn[i] && !inn) _leftSeat = i;
                _wasIn[i] = inn;
            }
            _rosterSeen = true;
        }

        static string CountWord(float sec)
        {
            int show = (int)sec;
            if (sec > show) show++;
            if (show >= 3) return HudState.Digit(0);
            if (show == 2) return HudState.Digit(1);
            return HudState.Digit(2);
        }

        int RevealSeat(TagModeController modes)
        {
            int n = Humans < 1 ? 1 : Humans;
            if (n > 4) n = 4;
            for (int i = 0; i < n; i++)
            {
                ItController pawn = Pawns[i];
                if (pawn == null || !pawn.IsIt) continue;
                int seat = Seat[i];
                if (seat < 0 || seat > 3) seat = i;
                return seat;
            }
            GameSettings rules = GameSettings.Current;
            int start = rules != null ? rules.StartIt : GameSettings.StartRandom;
            if (start == GameSettings.StartChosen && rules != null)
            {
                int pick = rules.StartSeat;
                if (pick < 0) pick = 0;
                if (pick > 3) pick = 3;
                return pick;
            }
            if (start == GameSettings.StartLast)
            {
                int best = 0;
                float worst = -1f;
                for (int i = 0; i < n; i++)
                {
                    ItController pawn = Pawns[i];
                    float t = pawn != null ? pawn.TimeAsIt : 0f;
                    if (t < worst) continue;
                    worst = t;
                    int seat = Seat[i];
                    if (seat < 0 || seat > 3) seat = i;
                    best = seat;
                }
                return best;
            }
            int step = (int)(Time.unscaledTime * 8f);
            if (step < 0) step = 0;
            int index = step % n;
            int spin = Seat[index];
            if (spin < 0 || spin > 3) spin = index;
            return spin;
        }

        static void ShowCountCall(HudPane pane, string word)
        {
            if (pane.Call == null) return;
            pane.Call.enabled = true;
            Set(pane.Call, word);
            Font face = MenuTheme.Display;
            if (pane.Call.font != face) pane.Call.font = face;
            pane.Call.rectTransform.localScale = Vector3.one;
            PlaceCall(pane, false);
            ShowWordSlot(pane, false);
        }

        static void ShowStats(HudPane pane, bool on)
        {
            if (pane.Profile != null) pane.Profile.enabled = on;
            if (pane.Metric != null) pane.Metric.enabled = on;
            if (pane.Value != null) pane.Value.enabled = on;
            if (pane.Tags != null) pane.Tags.enabled = on;
            if (pane.TagsValue != null) pane.TagsValue.enabled = on;
            if (pane.Verbs != null && pane.Verbs.gameObject.activeSelf != on)
                pane.Verbs.gameObject.SetActive(on);
        }

        static void ShowLock(HudPane pane, bool on)
        {
            if (pane.LockPlate != null) pane.LockPlate.enabled = on;
            if (pane.Lock == null) return;
            pane.Lock.enabled = on;
            if (on) Set(pane.Lock, MatchHudText.Locked);
        }

        static void FitNamePlate(HudPane pane, bool slim)
        {
            if (pane.Identity == null) return;
            Vector2 size = pane.Identity.sizeDelta;
            float want = slim ? 58f : size.y;
            if (!slim && size.y <= 64f) return;
            if (slim && size.y <= 64f) return;
            if (!slim) return;
            pane.Identity.sizeDelta = new Vector2(size.x, want);
        }

        static void PlaceCall(HudPane pane, bool tagged)
        {
            if (pane.Call == null) return;
            RectTransform rt = pane.Call.rectTransform;
            if (!tagged)
            {
                rt.anchorMin = new Vector2(0.08f, 0.34f);
                rt.anchorMax = new Vector2(0.92f, 0.68f);
                return;
            }
            HudState.ItPlate(out float x, out float y, out float w, out float h);
            rt.anchorMin = new Vector2(x, y);
            rt.anchorMax = new Vector2(x + w, y + h);
        }

        static void ShowWordSlot(HudPane pane, bool on)
        {
            if (pane.WordSlot == null) return;
            pane.WordSlot.enabled = on;
            if (!on) return;
            HudState.WordBox(out float x, out float y, out float w, out float h);
            RectTransform rt = pane.WordSlot.rectTransform;
            rt.anchorMin = new Vector2(x, y);
            rt.anchorMax = new Vector2(x + w, y + h);
        }

        static void ShowOpeningFlash(HudPane pane, bool on)
        {
            if (pane.Glow == null) return;
            pane.Glow.enabled = on;
            if (!on) return;
            float a = 0.34f;
            if (!MenuVideo.ReduceMotion)
            {
                float s = Mathf.Sin(Time.unscaledTime * 10f);
                if (s < 0f) s = -s;
                a = 0.22f + 0.28f * s;
            }
            pane.Glow.color = new Color(1f, 0.86f, 0.2f, a);
        }

        void ShowTagFlash(HudPane pane, int index)
        {
            if (pane.Glow == null) return;
            float now = Time.unscaledTime;
            float hold = MenuVideo.ReduceMotion ? 0.7f : 1.5f;
            float flash = MenuVideo.ReduceMotion ? 0.2f : 0.35f;
            bool tagged = _callKind[index] == 1 && now < _callUntil[index];
            float elapsed = hold - (_callUntil[index] - now);
            bool on = tagged && elapsed >= 0f && elapsed < flash;
            pane.Glow.enabled = on;
            if (!on) return;
            float u = elapsed / flash;
            pane.Glow.color = new Color(1f, 0.84f, 0.12f, 0.55f * (1f - u));
        }

        static void PulseClock(Text text, bool hot)
        {
            if (text == null) return;
            text.color = hot ? MenuTheme.Gold : MenuTheme.Cream;
            float scale = 1f;
            if (hot && !MenuVideo.ReduceMotion)
            {
                float s = Mathf.Sin(Time.unscaledTime * 9f);
                if (s < 0f) s = -s;
                scale = 1f + 0.12f * s;
            }
            text.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        void PlaceCenter(float y)
        {
            if (CenterCall == null) return;
            RectTransform plate = CenterCall.rectTransform.parent as RectTransform;
            if (plate == null) return;
            plate.anchoredPosition = new Vector2(0f, y);
        }

        void PaintStandings(TagModeController modes)
        {
            if (ScoreRoot == null) return;
            if (!_cardOn) _cardAt = Time.unscaledTime;
            _cardOn = true;
            int shown = 0;
            for (int i = 0; i < 4; i++)
            {
                _rank[i] = i;
                if (CouchPlay.HumanAt(i) || CouchPlay.AiAt(i)) shown++;
            }
            if (shown < 1) shown = 1;
            for (int a = 0; a < 3; a++)
            {
                for (int b = a + 1; b < 4; b++)
                {
                    if (ItSeconds(modes, _rank[b]) >= ItSeconds(modes, _rank[a])) continue;
                    int tmp = _rank[a];
                    _rank[a] = _rank[b];
                    _rank[b] = tmp;
                }
            }
            float h = 0.22f + shown * 0.11f;
            if (h > 0.78f) h = 0.78f;
            float y0 = 0.5f - h * 0.5f;
            ScoreRoot.anchorMin = new Vector2(0.16f, y0);
            ScoreRoot.anchorMax = new Vector2(0.84f, y0 + h);
            ScoreRoot.offsetMin = Vector2.zero;
            ScoreRoot.offsetMax = Vector2.zero;
            ScoreRoot.gameObject.SetActive(true);
            Set(ScoreTitle, MatchHudText.LeastWins);
            if (ScoreFoot != null)
            {
                ScoreFoot.enabled = true;
                Set(ScoreFoot, MatchHudText.NextRound);
            }
            for (int i = 0; i < ScoreHead.Length; i++)
                if (ScoreHead[i] != null) ScoreHead[i].enabled = true;
            float age = Time.unscaledTime - _cardAt;
            for (int row = 0; row < 4; row++)
            {
                int seat = _rank[row];
                bool on = CouchPlay.HumanAt(seat) || CouchPlay.AiAt(seat);
                bool win = on && row == 0;
                PaintRankRow(row, seat, on, win, modes);
                SlideRow(row, age);
            }
        }

        void SlideRow(int row, float age)
        {
            RectTransform slide = ScoreSlide[row];
            if (slide == null) return;
            float u = 1f;
            if (!MenuVideo.ReduceMotion)
            {
                float start = row * 0.12f;
                u = (age - start) / 0.28f;
                if (u < 0f) u = 0f;
                if (u > 1f) u = 1f;
            }
            float x = (1f - u) * 640f;
            Vector2 pos = slide.anchoredPosition;
            pos.x = x;
            slide.anchoredPosition = pos;
        }

        float ItSeconds(TagModeController modes, int seat)
        {
            ItController pawn = PawnForSeat(seat);
            if (pawn == null) return 9999f;
            return pawn.TimeAsIt;
        }

        void PaintRankRow(int row, int seat, bool on, bool win, TagModeController modes)
        {
            Color ink = win ? MenuTheme.Ink : MenuTheme.Cream;
            Text line = ScoreLine[row];
            if (line != null)
            {
                line.enabled = on;
                if (on)
                {
                    Set(line, MatchHudText.Seat[seat]);
                    line.color = ink;
                    StretchLine(line, false);
                }
            }
            SetRank(ScoreRank[row], on, HudDigits.Whole0(row + 1), ink);
            ItController pawn = on ? PawnForSeat(seat) : null;
            float time = pawn != null ? pawn.TimeAsIt : 0f;
            int tags = pawn != null ? pawn.TagsLanded : 0;
            int got = TimesTagged(pawn);
            int wins = 0;
            if (pawn != null && modes != null) wins = modes.RoundWinsOf(pawn.PlayerId);
            SetRank(ScoreTime[row], on, HudDigits.Tenth0(time), ink);
            SetRank(ScoreTagsN[row], on, HudDigits.Whole0(tags), ink);
            SetRank(ScoreGot[row], on, HudDigits.Whole0(got), ink);
            SetRank(ScoreWins[row], on, HudDigits.Whole0(wins), ink);
            Image chip = ScoreChip[row];
            if (chip != null)
            {
                chip.enabled = on;
                if (on) chip.color = MenuTheme.Seat(seat);
            }
            Image hi = ScoreHi[row];
            if (hi != null)
            {
                hi.enabled = on && win;
                if (on && win) hi.color = MenuTheme.Gold;
            }
            Image mark = ScoreMark[row];
            if (mark == null) return;
            mark.enabled = on;
            if (!on) return;
            Sprite sprite = SeatShape.For(seat);
            if (mark.sprite != sprite) mark.sprite = sprite;
            mark.color = win ? MenuTheme.Ink : MenuTheme.Cream;
        }

        static void SetRank(Text line, bool on, string text, Color ink)
        {
            if (line == null) return;
            line.enabled = on;
            if (!on) return;
            Set(line, text);
            line.color = ink;
        }

        static int TimesTagged(ItController pawn)
        {
            if (pawn == null || string.IsNullOrEmpty(pawn.PlayerId)) return 0;
            int n = MatchBook.Count;
            if (n > MatchBook.Cap) n = MatchBook.Cap;
            for (int i = 0; i < n; i++)
            {
                if (MatchBook.Name[i] != pawn.PlayerId) continue;
                return MatchBook.TimesTagged[i];
            }
            return 0;
        }

        void HideStandings(int humans)
        {
            if (_cardOn)
            {
                _cardOn = false;
                _cardAt = 0f;
                _layout = -1;
                for (int i = 0; i < ScoreHead.Length; i++)
                    if (ScoreHead[i] != null) ScoreHead[i].enabled = false;
                if (ScoreFoot != null) ScoreFoot.enabled = false;
            }
            if (ScoreRoot != null && humans != 3 && ScoreRoot.gameObject.activeSelf)
                ScoreRoot.gameObject.SetActive(false);
        }

        static void StretchLine(Text line, bool wide)
        {
            if (line == null) return;
            RectTransform rt = line.rectTransform;
            rt.anchorMin = new Vector2(0.28f, 0f);
            rt.anchorMax = new Vector2(wide ? 0.96f : 0.42f, 1f);
        }

        void HideRankExtras(int row)
        {
            if (ScoreRank[row] != null) ScoreRank[row].enabled = false;
            if (ScoreTime[row] != null) ScoreTime[row].enabled = false;
            if (ScoreTagsN[row] != null) ScoreTagsN[row].enabled = false;
            if (ScoreGot[row] != null) ScoreGot[row].enabled = false;
            if (ScoreWins[row] != null) ScoreWins[row].enabled = false;
            if (ScoreHi[row] != null) ScoreHi[row].enabled = false;
            if (ScoreSlide[row] != null)
            {
                Vector2 pos = ScoreSlide[row].anchoredPosition;
                pos.x = 0f;
                ScoreSlide[row].anchoredPosition = pos;
            }
        }

        void PaintArrow(TagModeController modes, int index)
        {
            HudPane pane = Panes[index];
            if (pane == null || pane.Arrow == null) return;
            if (modes.Phase != MatchPhase.Playing && modes.Phase != MatchPhase.PostRound)
            {
                pane.Arrow.enabled = false;
                PlaceHeadChip(pane, false, new Rect(0f, 0f, 1f, 1f), 0f, 0f);
                return;
            }
            ItController me = Pawns[index];
            ItController target = modes.CurrentIt;
            if (me != null && me.IsIt) target = NearestRunner(modes, me);
            if (target == null || target == me || Cams[index] == null)
            {
                pane.Arrow.enabled = false;
                PlaceHeadChip(pane, false, new Rect(0f, 0f, 1f, 1f), 0f, 0f);
                return;
            }
            bool hunt = me != null && me.IsIt;
            Vector3 vp = Cams[index].WorldToViewportPoint(target.transform.position);
            AimAt(pane, Cams[index].rect, vp.x, vp.y, vp.z, !hunt);
        }

        void PreviewAim(int index, HudState.Snap snap)
        {
            HudPane pane = Panes[index];
            if (pane == null || pane.Arrow == null || pane.Root == null) return;
            if (index == snap.It)
            {
                pane.Arrow.enabled = false;
                PlaceHeadChip(pane, false, new Rect(0f, 0f, 1f, 1f), 0f, 0f);
                return;
            }
            HudState.ViewOf(index, out float vx, out float vy, out float vz);
            Rect paneNorm = new Rect(
                pane.Root.anchorMin.x,
                pane.Root.anchorMin.y,
                pane.Root.anchorMax.x - pane.Root.anchorMin.x,
                pane.Root.anchorMax.y - pane.Root.anchorMin.y);
            AimAt(pane, paneNorm, vx, vy, vz, true);
        }

        static void AimAt(HudPane pane, Rect paneNorm, float vx, float vy, float vz, bool chip)
        {
            if (pane == null || pane.Arrow == null) return;
            HudState.Aim aim = HudState.Project(vx, vy, vz);
            if (aim.OnScreen)
            {
                pane.Arrow.enabled = false;
                PlaceHeadChip(pane, chip, paneNorm, aim.X, aim.Y);
                return;
            }
            PlaceHeadChip(pane, false, paneNorm, 0f, 0f);
            float px = paneNorm.x + aim.X * paneNorm.width;
            float py = paneNorm.y + aim.Y * paneNorm.height;
            RectTransform rt = pane.Arrow.rectTransform;
            rt.anchorMin = new Vector2(px, py);
            rt.anchorMax = new Vector2(px, py);
            rt.anchoredPosition = Vector2.zero;
            rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(aim.DirY, aim.DirX) * 57.29578f);
            pane.Arrow.enabled = true;
        }

        static void PlaceHeadChip(HudPane pane, bool on, Rect paneNorm, float nx, float ny)
        {
            if (pane == null || pane.Compass == null) return;
            if (pane.CompassPlate != null) pane.CompassPlate.enabled = on;
            pane.Compass.enabled = on;
            GameObject plate = pane.Compass.transform.parent.gameObject;
            if (plate.activeSelf != on) plate.SetActive(on);
            if (!on) return;
            RectTransform rt = plate.transform as RectTransform;
            float px = paneNorm.x + nx * paneNorm.width;
            float py = paneNorm.y + ny * paneNorm.height;
            rt.anchorMin = new Vector2(px, py);
            rt.anchorMax = new Vector2(px, py);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 8f);
            rt.localEulerAngles = Vector3.zero;
            rt.sizeDelta = new Vector2(84f, 44f);
        }

        void PaintPreviewScore(int humans)
        {
            if (ScoreRoot == null || humans != 3) return;
            if (ScoreTitle != null) Set(ScoreTitle, MatchHudText.Score);
            for (int i = 0; i < 4; i++)
                PaintScoreRow(i, MatchHudText.PreviewScore[i], true);
        }

        void PaintScore(int humans)
        {
            if (ScoreRoot == null || humans != 3) return;
            if (ScoreTitle != null) Set(ScoreTitle, MatchHudText.Score);
            for (int i = 0; i < 4; i++)
            {
                if (!CouchPlay.HumanAt(i) && !CouchPlay.AiAt(i))
                {
                    PaintScoreRow(i, MatchHudText.Blank, false);
                    continue;
                }
                string text = CouchPlay.ScoreText(i);
                if (text.Length == 0) text = CouchPlay.SeatLine(i);
                PaintScoreRow(i, text, true);
            }
        }

        void PaintScoreRow(int seat, string text, bool on)
        {
            Text line = ScoreLine[seat];
            if (line != null)
            {
                line.enabled = on;
                if (on)
                {
                    Set(line, text);
                    line.color = MenuTheme.Cream;
                    StretchLine(line, true);
                }
            }
            HideRankExtras(seat);
            Image chip = ScoreChip[seat];
            if (chip != null)
            {
                chip.enabled = on;
                if (on) chip.color = MenuTheme.Seat(seat);
            }
            Image mark = ScoreMark[seat];
            if (mark == null) return;
            bool shapes = on && SeatMarks();
            mark.enabled = shapes;
            if (!shapes) return;
            Sprite sprite = SeatShape.For(seat);
            if (mark.sprite != sprite) mark.sprite = sprite;
            mark.color = MenuTheme.Cream;
        }

        void ApplyLayout(int humans, int split, bool preview)
        {
            int camBits = 0;
            if (!preview)
            {
                for (int i = 0; i < 4; i++)
                    if (Cams[i] != null) camBits |= 1 << i;
            }
            int scaleBits = (int)(UiFit.Current() * 100f);
            int hudBits = 100;
            GameSettings hudSettings = GameSettings.Current;
            if (hudSettings != null)
            {
                float hud = hudSettings.HudScale;
                if (hud < GameSettings.HudMin) hud = GameSettings.HudMin;
                if (hud > GameSettings.HudMax) hud = GameSettings.HudMax;
                hudBits = (int)(hud * 100f + 0.5f);
            }
            int sig = humans * 64 + split * 8 + camBits + (preview ? 32 : 0) + scaleBits * 1024 + hudBits * 65536;
            if (sig == _layout) return;
            _layout = sig;
            int panes = CouchPlay.Panes(humans);
            bool topCenter = humans <= 1;
            int nameSize = UiFit.TextPx(humans >= 3 ? UiFit.FloorFont : 36);
            int callSize = UiFit.TextPx(humans >= 3 ? 40 : 68);
            FitText(Clock, 46);
            FitText(RoundLabel, UiFit.FloorFont);
            FitText(CenterCall, 96);
            if (Clock != null)
            {
                RectTransform plate = Clock.transform.parent as RectTransform;
                if (plate != null)
                {
                    plate.anchoredPosition = new Vector2(0f, -UiFit.SafeY);
                    int clockPx = UiFit.TextPx(46);
                    if (clockPx != 46)
                    {
                        float h = clockPx / 0.84f + 8f;
                        if (h < HudCorner.ClockH) h = HudCorner.ClockH;
                        float w = HudCorner.ClockW;
                        float want = clockPx * 3.6f;
                        if (want > w) w = want;
                        if (w > 420f) w = 420f;
                        plate.sizeDelta = new Vector2(w, h);
                    }
                }
            }
            for (int i = 0; i < 4; i++)
            {
                HudPane pane = Panes[i];
                if (pane == null || pane.Root == null) continue;
                bool scoreSlot = humans == 3 && i == 3;
                bool on = i < panes && !scoreSlot;
                pane.Root.gameObject.SetActive(on);
                if (pane.Arrow != null && !on) pane.Arrow.enabled = false;
                if (!on) continue;
                float x;
                float y;
                float w;
                float h;
                if (!preview && Cams[i] != null)
                {
                    Rect rect = Cams[i].rect;
                    x = rect.x;
                    y = rect.y;
                    w = rect.width;
                    h = rect.height;
                }
                else CouchPlay.Norm(i, humans, split, out x, out y, out w, out h);
                if (x < 0.45f && x + w > 0.55f && y + h > 0.92f) topCenter = true;
                pane.Root.anchorMin = new Vector2(x, y);
                pane.Root.anchorMax = new Vector2(x + w, y + h);
                pane.Root.offsetMin = Vector2.zero;
                pane.Root.offsetMax = Vector2.zero;
                PlaceIdentity(pane, humans, split, i, UiFit.Current(), nameSize, callSize);
            }
            if (Clock != null)
            {
                GameObject shared = Clock.transform.parent.gameObject;
                if (shared.activeSelf != topCenter) shared.SetActive(topCenter);
            }
            if (ScoreRoot != null)
            {
                bool score = humans == 3;
                ScoreRoot.gameObject.SetActive(score);
                if (score)
                {
                    CouchPlay.Norm(3, humans, split, out float x, out float y, out float w, out float h);
                    ScoreRoot.anchorMin = new Vector2(x + 0.02f, y + 0.03f);
                    ScoreRoot.anchorMax = new Vector2(x + w - 0.02f, y + h - 0.04f);
                    ScoreRoot.offsetMin = Vector2.zero;
                    ScoreRoot.offsetMax = Vector2.zero;
                }
            }
        }

        static void FitText(Text text, int px)
        {
            if (text == null) return;
            int n = UiFit.TextPx(px);
            text.fontSize = n;
            text.resizeTextMaxSize = n;
            int min = UiFit.TextPx(UiFit.FloorFont);
            if (min > n) min = n;
            text.resizeTextMinSize = min;
        }

        static void PlaceIdentity(HudPane pane, int humans, int split, int index, float scale, int nameSize, int callSize)
        {
            HudCorner.Lay lay = HudCorner.Measure(humans, split, index, scale);
            bool right = lay.Right;
            int floor = UiFit.TextPx(UiFit.FloorFont);
            if (nameSize < floor) nameSize = floor;
            float badgeH = HudCorner.BadgeH;
            if (floor + 8f > badgeH) badgeH = floor + 8f;
            float nameH = HudCorner.NameH;
            if (nameSize > nameH * 0.34f || floor > nameH * 0.32f || floor > nameH * 0.38f)
            {
                float need = nameSize / 0.34f;
                float mid = floor / 0.32f;
                float low = floor / 0.38f;
                if (mid > need) need = mid;
                if (low > need) need = low;
                nameH = need + 4f;
            }
            if (pane.Badge != null)
            {
                RectTransform rt = pane.Badge.rectTransform;
                rt.anchorMin = new Vector2(right ? 1f : 0f, 1f);
                rt.anchorMax = rt.anchorMin;
                rt.pivot = new Vector2(right ? 1f : 0f, 1f);
                rt.sizeDelta = new Vector2(HudCorner.BadgeW, badgeH);
                rt.anchoredPosition = new Vector2(lay.BadgeX, lay.BadgeY);
            }
            if (pane.Identity != null)
            {
                RectTransform rt = pane.Identity;
                rt.anchorMin = new Vector2(right ? 1f : 0f, 1f);
                rt.anchorMax = rt.anchorMin;
                rt.pivot = new Vector2(right ? 1f : 0f, 1f);
                rt.anchoredPosition = new Vector2(lay.NameX, lay.NameY);
                rt.sizeDelta = new Vector2(lay.NameW, nameH);
            }
            float rowW = 188f;
            float rowH = 48f;
            if (pane.Verbs != null)
            {
                float disc = floor + 8f;
                if (disc < 36f) disc = 36f;
                float wordW = floor * 5.6f;
                if (wordW < 168f) wordW = 168f;
                rowW = disc + 12f + wordW;
                rowH = disc + 10f;
                float gap = 6f;
                pane.Verbs.sizeDelta = new Vector2(rowW, rowH * 3f + gap * 2f);
                int rows = pane.Verbs.childCount;
                for (int c = 0; c < rows; c++)
                {
                    RectTransform child = pane.Verbs.GetChild(c) as RectTransform;
                    if (child == null) continue;
                    int fromTop = rows - 1 - c;
                    child.anchoredPosition = new Vector2(0f, -fromTop * (rowH + gap));
                    child.sizeDelta = new Vector2(rowW, rowH);
                    if (child.childCount > 0)
                    {
                        RectTransform discRt = child.GetChild(0) as RectTransform;
                        if (discRt != null)
                        {
                            discRt.anchoredPosition = new Vector2(2f, -((rowH - disc) * 0.5f));
                            discRt.sizeDelta = new Vector2(disc, disc);
                        }
                    }
                    if (child.childCount > 1)
                    {
                        RectTransform wordRt = child.GetChild(1) as RectTransform;
                        if (wordRt != null)
                        {
                            wordRt.anchorMin = Vector2.zero;
                            wordRt.anchorMax = Vector2.one;
                            wordRt.pivot = new Vector2(0f, 0.5f);
                            wordRt.offsetMin = new Vector2(disc + 12f, 0f);
                            wordRt.offsetMax = new Vector2(-4f, 0f);
                        }
                    }
                }
            }
            TextAnchor align = right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            if (pane.Name != null)
            {
                pane.Name.fontSize = nameSize;
                pane.Name.resizeTextMaxSize = nameSize;
                pane.Name.resizeTextMinSize = floor;
                pane.Name.alignment = align;
            }
            if (pane.Profile != null)
            {
                pane.Profile.fontSize = floor;
                pane.Profile.resizeTextMaxSize = floor;
                pane.Profile.resizeTextMinSize = floor;
                pane.Profile.alignment = align;
            }
            if (pane.Metric != null)
            {
                pane.Metric.fontSize = floor;
                pane.Metric.resizeTextMaxSize = floor;
                pane.Metric.resizeTextMinSize = floor;
                pane.Metric.alignment = align;
            }
            if (pane.Value != null)
            {
                pane.Value.fontSize = floor;
                pane.Value.resizeTextMaxSize = floor;
                pane.Value.resizeTextMinSize = floor;
                pane.Value.alignment = align;
            }
            FitText(pane.Tags, UiFit.FloorFont);
            FitText(pane.TagsValue, UiFit.FloorFont);
            FitText(pane.BadgeWord, UiFit.FloorFont);
            FitText(pane.Timer, humans > 1 ? (humans >= 4 ? 32 : 36) : 36);
            FitText(pane.TimerRound, UiFit.FloorFont);
            int itPx = humans >= 4 ? 48 : humans == 3 ? 56 : humans == 2 ? 72 : 96;
            FitText(pane.ItBig, itPx);
            if (pane.Timer != null)
            {
                GameObject clock = pane.Timer.transform.parent.gameObject;
                bool coversTop = pane.Root != null
                    && pane.Root.anchorMin.x < 0.45f
                    && pane.Root.anchorMax.x > 0.55f
                    && pane.Root.anchorMax.y > 0.92f;
                bool splitClock = humans > 1 && !coversTop;
                if (clock.activeSelf != splitClock) clock.SetActive(splitClock);
                if (splitClock)
                {
                    RectTransform plate = clock.transform as RectTransform;
                    float drop = -lay.BadgeY;
                    if (drop < 14f) drop = 14f;
                    plate.anchorMin = new Vector2(0.5f, 1f);
                    plate.anchorMax = new Vector2(0.5f, 1f);
                    plate.pivot = new Vector2(0.5f, 1f);
                    plate.anchoredPosition = new Vector2(0f, -drop);
                    int clockPx = UiFit.TextPx(humans >= 4 ? 32 : 36);
                    float cw = clockPx * 6.2f;
                    if (cw < 200f) cw = 200f;
                    if (cw > lay.NameW && lay.NameW > 160f) cw = lay.NameW;
                    float ch = clockPx + 28f;
                    if (ch < 56f) ch = 56f;
                    plate.sizeDelta = new Vector2(cw, ch);
                }
            }
            if (pane.ItPlate != null)
            {
                RectTransform plate = pane.ItPlate.rectTransform;
                float itW = humans >= 4 ? 168f : humans == 3 ? 196f : humans == 2 ? 240f : 300f;
                float itH = humans >= 4 ? 76f : humans == 3 ? 88f : 112f;
                plate.sizeDelta = new Vector2(itW, itH);
                plate.anchorMin = new Vector2(0.5f, humans >= 4 ? 0.58f : 0.62f);
                plate.anchorMax = plate.anchorMin;
            }
            if (pane.Arrow != null)
            {
                float arrow = humans >= 4 ? 64f : 84f;
                pane.Arrow.rectTransform.sizeDelta = new Vector2(arrow, arrow);
            }
            FitText(pane.DashWord, UiFit.FloorFont);
            FitText(pane.RopeWord, UiFit.FloorFont);
            FitText(pane.SafeWord, UiFit.FloorFont);
            if (pane.Feed != null)
            {
                for (int f = 0; f < pane.Feed.Length; f++)
                    FitText(pane.Feed[f], UiFit.FloorFont);
            }
            if (pane.Tags != null) pane.Tags.alignment = align;
            if (pane.TagsValue != null) pane.TagsValue.alignment = align;
            if (pane.Call != null)
            {
                pane.Call.fontSize = callSize;
                pane.Call.resizeTextMaxSize = callSize;
            }
            if (pane.Verbs != null)
            {
                pane.Verbs.anchorMin = new Vector2(0f, 0f);
                pane.Verbs.anchorMax = new Vector2(0f, 0f);
                pane.Verbs.pivot = new Vector2(0f, 0f);
                pane.Verbs.anchoredPosition = new Vector2(lay.VerbX, lay.VerbY);
                pane.Verbs.localScale = Vector3.one;
            }
            if (pane.Board != null)
            {
                pane.Board.anchorMin = new Vector2(0.5f, 0.5f);
                pane.Board.anchorMax = new Vector2(0.5f, 0.5f);
                pane.Board.pivot = new Vector2(0.5f, 0.5f);
                pane.Board.anchoredPosition = Vector2.zero;
            }
            if (pane.Feed != null && pane.Feed.Length > 0 && pane.Feed[0] != null)
            {
                RectTransform feed = pane.Feed[0].rectTransform.parent as RectTransform;
                if (feed != null)
                {
                    UiFit.Ref(scale, out float sw, out _);
                    float panePx = (pane.Root.anchorMax.x - pane.Root.anchorMin.x) * sw;
                    bool touchR = pane.Root.anchorMax.x > 0.98f;
                    float rightInset = touchR ? UiFit.SafeX : 18f;
                    float room = panePx - lay.VerbX - rightInset - rowW - 16f;
                    float feedW = 420f;
                    if (feedW > room) feedW = room;
                    if (feedW < 220f) feedW = 220f;
                    feed.anchorMin = new Vector2(1f, 0f);
                    feed.anchorMax = new Vector2(1f, 0f);
                    feed.pivot = new Vector2(1f, 0f);
                    feed.anchoredPosition = new Vector2(-rightInset, lay.VerbY);
                    feed.sizeDelta = new Vector2(feedW, feed.sizeDelta.y);
                    for (int f = 0; f < feed.childCount; f++)
                    {
                        RectTransform line = feed.GetChild(f) as RectTransform;
                        if (line == null) continue;
                        line.sizeDelta = new Vector2(feedW, line.sizeDelta.y);
                        if (line.childCount > 2)
                        {
                            RectTransform word = line.GetChild(2) as RectTransform;
                            if (word != null)
                            {
                                float textW = feedW - 82f;
                                if (textW < 120f) textW = 120f;
                                word.sizeDelta = new Vector2(textW, word.sizeDelta.y);
                            }
                        }
                    }
                }
            }
        }

        void PreviewArrow(int index, int itPane, int humans, int split)
        {
            HudPane pane = Panes[index];
            if (pane == null || pane.Arrow == null) return;
            if (index == itPane)
            {
                pane.Arrow.enabled = false;
                return;
            }
            CouchPlay.Norm(index, humans, split, out float x, out float y, out float w, out float h);
            CouchPlay.Norm(itPane, humans, split, out float tx, out float ty, out float tw, out float th);
            float dx = (tx + tw * 0.5f) - (x + w * 0.5f);
            float dy = (ty + th * 0.5f) - (y + h * 0.5f);
            float ax = dx < 0f ? -dx : dx;
            float ay = dy < 0f ? -dy : dy;
            float sx = ax < 0.0001f ? 1000f : (w * 0.40f) / ax;
            float sy = ay < 0.0001f ? 1000f : (h * 0.34f) / ay;
            float scale = sx < sy ? sx : sy;
            float px = x + w * 0.5f + dx * scale;
            float py = y + h * 0.5f + dy * scale;
            RectTransform rt = pane.Arrow.rectTransform;
            rt.anchorMin = new Vector2(px, py);
            rt.anchorMax = new Vector2(px, py);
            rt.anchoredPosition = Vector2.zero;
            rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(dy, dx) * 57.29578f);
            pane.Arrow.enabled = true;
        }

        static void PlaceCompass(HudPane pane, bool show)
        {
            if (pane == null || pane.Compass == null) return;
            bool on = show && pane.Arrow != null && pane.Arrow.enabled;
            if (pane.CompassPlate != null) pane.CompassPlate.enabled = on;
            pane.Compass.enabled = on;
            GameObject plate = pane.Compass.transform.parent.gameObject;
            if (plate.activeSelf != on) plate.SetActive(on);
            if (!on) return;
            RectTransform rt = plate.transform as RectTransform;
            RectTransform arrow = pane.Arrow.rectTransform;
            float chipW = 72f;
            float chipH = 40f;
            float gap = arrow.sizeDelta.x * 0.55f;
            if (gap < 28f) gap = 28f;
            RectTransform canvas = rt.parent as RectTransform;
            float canvasW = 1920f;
            float canvasH = 1080f;
            if (canvas != null)
            {
                if (canvas.rect.width > 1f) canvasW = canvas.rect.width;
                if (canvas.rect.height > 1f) canvasH = canvas.rect.height;
            }
            float ax = arrow.anchorMin.x;
            float ay = arrow.anchorMin.y;
            float leftN = 0f;
            float rightN = 1f;
            float bottomN = 0f;
            float topN = 1f;
            if (pane.Root != null)
            {
                leftN = pane.Root.anchorMin.x;
                rightN = pane.Root.anchorMax.x;
                bottomN = pane.Root.anchorMin.y;
                topN = pane.Root.anchorMax.y;
            }
            float margin = 28f;
            float roomRight = (rightN - ax) * canvasW - margin;
            float roomLeft = (ax - leftN) * canvasW - margin;
            bool placeLeft = roomRight < gap + chipW && roomLeft >= roomRight;
            float ox = placeLeft ? -gap : gap;
            float edge = placeLeft ? roomLeft - chipW : roomRight - chipW;
            if (placeLeft && ox < -edge) ox = -edge;
            if (!placeLeft && ox > edge) ox = edge;
            float oy = 16f;
            float topRoom = (topN - ay) * canvasH - margin;
            float botRoom = (ay - bottomN) * canvasH - margin;
            if (oy + chipH * 0.5f > topRoom) oy = topRoom - chipH * 0.5f;
            if (-oy + chipH * 0.5f > botRoom) oy = -(botRoom - chipH * 0.5f);
            rt.anchorMin = arrow.anchorMin;
            rt.anchorMax = arrow.anchorMax;
            rt.pivot = new Vector2(placeLeft ? 1f : 0f, 0.5f);
            rt.anchoredPosition = new Vector2(ox, oy);
            rt.localEulerAngles = Vector3.zero;
            rt.sizeDelta = new Vector2(chipW, chipH);
        }

        static void HideFeedRow(HudPane pane, int row)
        {
            if (pane.Feed == null || row < 0 || row >= pane.Feed.Length) return;
            if (pane.Feed[row] != null) pane.Feed[row].enabled = false;
            if (pane.FeedPlate[row] != null) pane.FeedPlate[row].enabled = false;
            if (pane.FeedChip[row] != null) pane.FeedChip[row].enabled = false;
            if (pane.FeedMark[row] != null) pane.FeedMark[row].enabled = false;
        }

        static void ShowItFrame(HudPane pane, bool on)
        {
            if (pane.ItFrame == null) return;
            if (pane.ItFrame.gameObject.activeSelf != on) pane.ItFrame.gameObject.SetActive(on);
            if (!on) return;
            float a = 0.9f;
            if (!MenuVideo.ReduceMotion)
            {
                float s = Mathf.Sin(Time.unscaledTime * 5.5f);
                if (s < 0f) s = -s;
                a = 0.35f + 0.65f * s;
            }
            pane.ItFrame.alpha = a;
        }

        static void ShowItMark(HudPane pane, bool it)
        {
            if (pane.ItPlate != null) pane.ItPlate.enabled = it;
            if (pane.ItBig != null)
            {
                pane.ItBig.enabled = it;
                if (it) Set(pane.ItBig, MatchHudText.It);
            }
        }

        static void ShowSeatMark(HudPane pane, int seat)
        {
            if (pane.SeatMark == null) return;
            bool on = SeatMarks();
            pane.SeatMark.enabled = on;
            if (!on) return;
            Sprite sprite = SeatShape.For(seat);
            if (pane.SeatMark.sprite != sprite) pane.SeatMark.sprite = sprite;
            pane.SeatMark.color = MenuTheme.Cream;
        }

        static void ShowSafeGlow(HudPane pane, bool on)
        {
            if (pane.SafeGlow == null) return;
            pane.SafeGlow.enabled = on;
            if (!on) return;
            float a = 0.22f;
            if (!MenuVideo.ReduceMotion)
            {
                float s = Mathf.Sin(Time.unscaledTime * 8f);
                if (s < 0f) s = -s;
                a = 0.14f + 0.24f * s;
            }
            pane.SafeGlow.color = new Color(1f, 0.93f, 0.62f, a);
        }

        static void ShowFeedLine(HudPane pane, int row, int from, string text)
        {
            if (pane.Feed == null || row < 0 || row >= pane.Feed.Length) return;
            Text line = pane.Feed[row];
            if (line == null) return;
            line.enabled = true;
            Set(line, text);
            Font face = MatchHudText.ComicWords ? MenuTheme.Display : MenuTheme.Font;
            if (line.font != face) line.font = face;
            line.color = MenuTheme.Cream;
            if (pane.FeedPlate[row] != null)
            {
                pane.FeedPlate[row].enabled = true;
                pane.FeedPlate[row].color = new Color(MenuTheme.Ink.r, MenuTheme.Ink.g, MenuTheme.Ink.b, 0.92f);
            }
            if (pane.FeedChip[row] != null)
            {
                pane.FeedChip[row].enabled = true;
                pane.FeedChip[row].color = MenuTheme.Seat(from);
            }
            if (pane.FeedMark[row] != null)
            {
                bool shapes = SeatMarks();
                pane.FeedMark[row].enabled = shapes;
                if (shapes)
                {
                    Sprite sprite = SeatShape.For(from);
                    if (pane.FeedMark[row].sprite != sprite) pane.FeedMark[row].sprite = sprite;
                }
            }
        }

        static bool SeatMarks()
        {
            GameSettings settings = GameSettings.Current;
            return settings != null && settings.CvdSeats != SeatCvd.Off;
        }

        static void PlaceArrow(Image arrow, Camera cam, Vector3 world)
        {
            if (arrow == null) return;
            if (cam == null)
            {
                arrow.enabled = false;
                return;
            }
            Vector3 vp = cam.WorldToViewportPoint(world);
            float dx = vp.x - 0.5f;
            float dy = vp.y - 0.5f;
            if (vp.z < 0.05f)
            {
                dx = -dx;
                dy = -dy;
            }
            float ax = dx < 0f ? -dx : dx;
            float ay = dy < 0f ? -dy : dy;
            if (vp.z >= 0.05f && ax < 0.40f && ay < 0.36f)
            {
                arrow.enabled = false;
                return;
            }
            float sx = ax < 0.0001f ? 1000f : 0.46f / ax;
            float sy = ay < 0.0001f ? 1000f : 0.40f / ay;
            float scale = sx < sy ? sx : sy;
            float ex = 0.5f + dx * scale;
            float ey = 0.5f + dy * scale;
            if (ex < 0.05f) ex = 0.05f;
            if (ex > 0.95f) ex = 0.95f;
            if (ey < 0.08f) ey = 0.08f;
            if (ey > 0.92f) ey = 0.92f;
            Rect rect = cam.rect;
            float px = rect.x + ex * rect.width;
            float py = rect.y + ey * rect.height;
            RectTransform rt = arrow.rectTransform;
            rt.anchorMin = new Vector2(px, py);
            rt.anchorMax = new Vector2(px, py);
            rt.anchoredPosition = Vector2.zero;
            rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(dy, dx) * 57.29578f);
            arrow.enabled = true;
        }

        static ItController NearestRunner(TagModeController modes, ItController me)
        {
            IReadOnlyList<ItController> list = modes.PlayersForHud;
            if (list == null || me == null) return null;
            Vector3 at = me.transform.position;
            ItController best = null;
            float bestD = 0f;
            int n = list.Count;
            for (int i = 0; i < n; i++)
            {
                ItController other = list[i];
                if (other == null || other == me || !other.IsAlive || other.IsIt) continue;
                Vector3 p = other.transform.position;
                float dx = p.x - at.x;
                float dy = p.y - at.y;
                float dz = p.z - at.z;
                float d = dx * dx + dy * dy + dz * dz;
                if (best == null || d < bestD)
                {
                    best = other;
                    bestD = d;
                }
            }
            return best;
        }

        static Color SeatTint(int seat)
        {
            if (CouchPlay.HumanAt(seat) || CouchPlay.AiAt(seat))
            {
                CouchPlay.Tint(seat, out float r, out float g, out float b);
                return new Color(r, g, b, 1f);
            }
            return MenuTheme.Seat(seat);
        }

        static int GameSettingsSplit()
        {
            if (Tag.Settings.GameSettings.Current == null) return Tag.Settings.GameSettings.SplitVertical;
            return Tag.Settings.GameSettings.Current.SplitAxis;
        }

        static void TintEdges(HudPane pane, Color color)
        {
            if (pane.EdgeT != null) pane.EdgeT.color = color;
            if (pane.EdgeB != null) pane.EdgeB.color = color;
            if (pane.EdgeL != null) pane.EdgeL.color = color;
            if (pane.EdgeR != null) pane.EdgeR.color = color;
        }

        static void Set(Text label, string value)
        {
            if (label == null || value == null) return;
            if (label.text == value) return;
            label.text = value;
        }
    }
}
