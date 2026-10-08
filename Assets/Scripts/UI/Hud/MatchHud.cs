using System.Collections.Generic;
using Tag.Core;
using Tag.Couch;
using Tag.Experimental;
using Tag.Gameplay;
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
        public Image DashBg;
        public Image DashFill;
        public Text DashWord;
        public Image RopeMark;
        public Text RopeWord;
        public Image SafeBg;
        public Image SafeFill;
        public Text SafeWord;
        public Image Arrow;
        public RectTransform Board;
        public Text BoardTitle;
        public Text BoardRound;
        public Text BoardClock;
        public readonly Text[] BoardName = new Text[4];
        public readonly Text[] BoardValue = new Text[4];
        public readonly Text[] Feed = new Text[3];
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
            for (int row = 0; row < 3; row++)
            {
                Text line = pane.Feed[row];
                if (line == null) continue;
                bool on = TagFeed.On(row, now);
                line.enabled = on;
                if (!on) continue;
                Set(line, TagFeed.Text(row));
                Color tint = MenuTheme.Seat(TagFeed.From(row));
                float a = TagFeed.Alpha(row, now, hold);
                line.color = new Color(tint.r, tint.g, tint.b, a);
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
            if (Clock != null) Clock.color = MenuTheme.Cream;
            Set(RoundLabel, MatchHudText.Round(humans >= 4 ? 2 : 1, humans >= 4 ? 3 : 1));
            int itPane = humans <= 1 ? 0 : (humans == 2 ? 1 : 2);
            int panes = CouchPlay.Panes(humans);
            for (int i = 0; i < 4; i++)
            {
                bool on = i < panes && !(humans == 3 && i == 3);
                if (!on) continue;
                PaintPreviewPane(i, i == itPane, humans);
                PreviewArrow(i, itPane, humans, split);
            }
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
            if (modes.SelectedMode == TagModeId.FreePlay) Set(Clock, MatchHudText.Free);
            else Set(Clock, MatchHudText.Clock(modes.Remaining));
            if (Clock != null)
            {
                bool hot = modes.SelectedMode != TagModeId.FreePlay && modes.Remaining <= 10f && modes.Remaining > 0f;
                Clock.color = hot ? MenuTheme.Gold : MenuTheme.Cream;
            }
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

        void PaintPreviewPane(int index, bool it, int humans)
        {
            HudPane pane = Panes[index];
            if (pane == null) return;
            Set(pane.Name, MatchHudText.Seat[index]);
            Set(pane.Profile, MatchHudText.PreviewProfile[index]);
            if (index == 1) Set(pane.Metric, MatchHudText.Metric(TagModeId.HotPotato));
            else if (index == 2) Set(pane.Metric, MatchHudText.Metric(TagModeId.TrailTag));
            else if (index == 3) Set(pane.Metric, MatchHudText.Metric(TagModeId.FreePlay));
            else Set(pane.Metric, MatchHudText.Metric(TagModeId.LeastIt));
            if (index == 1) Set(pane.Value, HudDigits.Whole0(1f));
            else if (index == 2) Set(pane.Value, it ? MatchHudText.In : MatchHudText.Out);
            else if (index == 3) Set(pane.Value, HudDigits.Whole0(3f));
            else Set(pane.Value, HudDigits.Tenth0(12.4f));
            Set(pane.Tags, MatchHudText.Tags);
            Set(pane.TagsValue, HudDigits.Whole0(index));
            Color tint = MenuTheme.Seat(index);
            TintEdges(pane, it ? Color.Lerp(tint, MenuTheme.Gold, 0.7f) : tint);
            if (pane.Glow != null)
            {
                pane.Glow.enabled = it;
                pane.Glow.color = new Color(MenuTheme.Gold.r, MenuTheme.Gold.g, MenuTheme.Gold.b, it ? 0.42f : 0f);
            }
            if (pane.Badge != null) pane.Badge.enabled = it;
            if (pane.BadgeWord != null) pane.BadgeWord.enabled = it;
            if (pane.DashFill != null) pane.DashFill.fillAmount = it ? 1f : 0.4f;
            Set(pane.DashWord, it ? MatchHudText.Blank : HudDigits.DashCd(18f));
            Set(pane.RopeWord, MatchHudText.Blank);
            if (pane.RopeMark != null) pane.RopeMark.color = index == 0 ? MenuTheme.Ready : MenuTheme.Gold;
            if (pane.SafeFill != null) pane.SafeFill.fillAmount = index == 2 ? 0.6f : 0f;
            Set(pane.SafeWord, index == 2 ? HudDigits.Tenth0(0.6f) : MatchHudText.Blank);
            bool call = it || (index == 0 && humans > 1);
            if (pane.Call != null)
            {
                pane.Call.enabled = call;
                Set(pane.Call, MatchHudText.Comic(it ? MatchHudText.YoureIt : MatchHudText.Tagged));
                pane.Call.rectTransform.localScale = Vector3.one;
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
            Set(pane.Profile, profile);
            TagModeId mode = modes.SelectedMode;
            Set(pane.Metric, MatchHudText.Metric(mode));
            Set(pane.Value, ValueOf(modes, pawn));
            Set(pane.Tags, MatchHudText.Tags);
            Set(pane.TagsValue, pawn != null ? HudDigits.Whole0(pawn.TagsLanded) : MatchHudText.Off);

            bool isIt = pawn != null && pawn.IsIt;
            Color tint = SeatTint(seat);
            TintEdges(pane, isIt ? Color.Lerp(tint, MenuTheme.Gold, 0.72f) : tint);
            float glowA = 0f;
            if (isIt)
            {
                glowA = 0.85f;
                if (!MenuVideo.ReduceMotion)
                {
                    float s = Mathf.Sin(Time.unscaledTime * 5.5f);
                    if (s < 0f) s = -s;
                    glowA = 0.28f + 0.62f * s;
                }
            }
            if (pane.Glow != null)
            {
                pane.Glow.enabled = isIt;
                pane.Glow.color = new Color(MenuTheme.Gold.r, MenuTheme.Gold.g, MenuTheme.Gold.b, glowA);
            }
            if (pane.Badge != null) pane.Badge.enabled = isIt;
            if (pane.BadgeWord != null) pane.BadgeWord.enabled = isIt;
            PaintVerbs(pane, index, pawn);
            PaintPawnCall(pane, index, isIt);
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
                if (dashing) dashWord = MatchHudText.Comic(MatchHudText.DashGo);
                else if (dashRem > 0.05f) dashWord = HudDigits.DashCd(dashRem);
            }
            Set(pane.DashWord, dashWord);

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
            Set(pane.SafeWord, safe > 0.001f ? HudDigits.Tenth0(safe) : MatchHudText.Blank);
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
            if (kind == RoundCard.Sudden) word = RoundCard.SuddenText;
            else if (kind == RoundCard.Final) word = RoundCard.FinalText;
            else if (kind == RoundCard.Round2) word = RoundCard.Round2Text;
            else if (kind == RoundCard.Point) word = RoundCard.PointText;
            else if (phase == MatchPhase.Countdown)
            {
                float sec = modes.PhaseSeconds;
                int show = (int)sec;
                if (sec > show) show++;
                if (show < 1) show = 1;
                if (show > 9) show = 9;
                word = HudDigits.Whole0(show);
            }
            else if (now < _goUntil) word = MatchHudText.Comic(MatchHudText.Go);
            else if (now < _endUntil) word = MatchHudText.RoundEnd;
            bool on = word.Length != 0;
            if (CenterPlate != null) CenterPlate.enabled = on;
            if (CenterCall == null) return;
            CenterCall.enabled = on;
            Set(CenterCall, word);
            if (!on) return;
            if (MenuVideo.ReduceMotion)
            {
                CenterCall.rectTransform.localScale = Vector3.one;
                return;
            }
            if (word == MatchHudText.Go)
            {
                float span = 0.8f;
                float left = _goUntil - now;
                float u = 1f - left / span;
                if (u < 0f) u = 0f;
                if (u > 1f) u = 1f;
                float pop = u < 0.22f ? Mathf.Lerp(1.28f, 1f, u / 0.22f) : 1f;
                CenterCall.rectTransform.localScale = new Vector3(pop, pop, 1f);
                return;
            }
            if (phase != MatchPhase.Countdown)
            {
                CenterCall.rectTransform.localScale = Vector3.one;
                return;
            }
            float frac = modes.PhaseSeconds - (int)modes.PhaseSeconds;
            if (frac < 0f) frac = 0f;
            float beat = frac > 0.78f ? Mathf.Lerp(1.24f, 1f, (1f - frac) / 0.22f) : 1f;
            CenterCall.rectTransform.localScale = new Vector3(beat, beat, 1f);
        }

        void PaintArrow(TagModeController modes, int index)
        {
            HudPane pane = Panes[index];
            if (pane == null || pane.Arrow == null) return;
            if (modes.Phase != MatchPhase.Playing && modes.Phase != MatchPhase.PostRound)
            {
                pane.Arrow.enabled = false;
                return;
            }
            ItController me = Pawns[index];
            ItController target = modes.CurrentIt;
            if (me != null && me.IsIt) target = NearestRunner(modes, me);
            if (target == null || target == me)
            {
                pane.Arrow.enabled = false;
                return;
            }
            PlaceArrow(pane.Arrow, Cams[index], target.transform.position);
        }

        void PaintScore(int humans)
        {
            if (ScoreRoot == null || humans != 3) return;
            if (ScoreTitle != null) Set(ScoreTitle, MatchHudText.Score);
            for (int i = 0; i < 4; i++)
            {
                Text line = ScoreLine[i];
                if (line == null) continue;
                if (!CouchPlay.HumanAt(i) && !CouchPlay.AiAt(i))
                {
                    Set(line, MatchHudText.Blank);
                    continue;
                }
                string text = CouchPlay.ScoreText(i);
                if (text.Length == 0) text = CouchPlay.SeatLine(i);
                Set(line, text);
                CouchPlay.Tint(i, out float r, out float g, out float b);
                line.color = new Color(r, g, b, 1f);
            }
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
                pane.Root.anchorMin = new Vector2(x, y);
                pane.Root.anchorMax = new Vector2(x + w, y + h);
                pane.Root.offsetMin = Vector2.zero;
                pane.Root.offsetMax = Vector2.zero;
                PlaceIdentity(pane, humans, split, i, UiFit.Current(), nameSize, callSize);
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
            if (floor > UiFit.FloorFont && pane.Verbs != null)
            {
                float chipH = (floor + 8f) / 0.42f;
                float chipW = floor * 3.0f;
                if (chipW < 52f) chipW = 52f;
                float gap = 4f;
                pane.Verbs.sizeDelta = new Vector2(chipW * 3f + gap * 2f, chipH);
                for (int c = 0; c < pane.Verbs.childCount; c++)
                {
                    RectTransform child = pane.Verbs.GetChild(c) as RectTransform;
                    if (child == null) continue;
                    child.anchoredPosition = new Vector2(c * (chipW + gap), 0f);
                    child.sizeDelta = new Vector2(chipW, chipH);
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
                    feed.anchorMin = new Vector2(1f, 0f);
                    feed.anchorMax = new Vector2(1f, 0f);
                    feed.pivot = new Vector2(1f, 0f);
                    feed.anchoredPosition = new Vector2(-lay.VerbX, lay.VerbY);
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
