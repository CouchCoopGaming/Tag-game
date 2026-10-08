using System.Collections.Generic;
using Tag.Core;
using Tag.Couch;
using Tag.Front;
using Tag.Gameplay;
using Tag.Level;
using Tag.MatchStats;
using Tag.Modes;
using Tag.Onboard;
using Tag.Practice;
using Tag.Profiles;
using Tag.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    public enum MenuScreenId
    {
        Hidden = 0,
        Title = 1,
        Main = 2,
        Join = 3,
        Cast = 4,
        Rules = 5,
        Arena = 6,
        Loading = 7,
        Pause = 8,
        Results = 9,
        Options = 10,
        Controls = 11,
        Credits = 12,
        Practice = 13
    }

    /// <summary>
    /// Couch front end. Boot and an unarmed Play scene open here.
    /// Set PlayerPrefs Tag.Ui.Legacy to 1 to keep the old OnGUI cards.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class MenuHost : MonoBehaviour
    {
        public static MenuHost Instance { get; private set; }
        public static bool EatPause;

        public static bool Legacy => PlayerPrefs.GetInt("Tag.Ui.Legacy", 0) == 1;

        public static bool CoversFront
        {
            get
            {
                MenuHost host = Instance;
                if (host == null || Legacy) return false;
                if (host._pauseChild) return false;
                MenuScreenId id = host._screen;
                return id != MenuScreenId.Hidden && id != MenuScreenId.Pause && id != MenuScreenId.Results;
            }
        }

        public static bool CoversPause
        {
            get
            {
                MenuHost host = Instance;
                if (host == null || Legacy) return false;
                return host._screen == MenuScreenId.Pause || host._pauseChild;
            }
        }

        public static bool CoversResults
        {
            get
            {
                MenuHost host = Instance;
                if (host == null || Legacy) return false;
                return host._holdResults;
            }
        }

        public MenuScreenId Screen => _screen;

        const int OptCount = 22;
        const int OptWindow = 8;

        static readonly int[] OptMap =
        {
            GameSettingsRow.Mouse,
            GameSettingsRow.Pad,
            GameSettingsRow.Invert,
            GameSettingsRow.Fov,
            GameSettingsRow.Master,
            GameSettingsRow.Sfx,
            GameSettingsRow.Ui,
            GameSettingsRow.Music,
            GameSettingsRow.Mute,
            GameSettingsRow.Hud,
            GameSettingsRow.Player,
            GameSettingsRow.Palette,
            GameSettingsRow.Captions,
            GameSettingsRow.Rumble,
            GameSettingsRow.Flash
        };

        Canvas _canvas;
        Image _dim;
        RectTransform _body;
        Text _header;
        Text _footer;
        Text _banner;
        Text _press;
        CanvasGroup _group;
        MenuPreview _preview;
        readonly List<MenuTile> _tiles = new List<MenuTile>(16);
        readonly RectTransform[] _ribbons = new RectTransform[7];

        MenuScreenId _screen = MenuScreenId.Hidden;
        int _focus;
        int _cols = 1;
        int _count;
        int _window;
        float _gate;
        float _fade;
        bool _pauseChild;
        bool _holdResults;
        bool _fromResults;
        bool _loadFired;
        bool _loadPractice;
        float _loadAt;
        bool _capturing;
        int _captureAction = -1;
        int _captureFrame = -1;
        string _conflict = "";
        float _allReadyAt;
        int _joinSig = int.MinValue;
        int _castSig = int.MinValue;
        int _tip = 3;
        InputDeviceKind _footerKind;
        MenuScreenId _castBack = MenuScreenId.Join;
        MenuScreenId _arenaBack = MenuScreenId.Rules;
        RawImage _flyover;
        RawImage _pattern;
        Image _sweep;
        float _slide = 1f;
        RawImage _arenaShot;
        Text _arenaShotName;
        Text _arenaShotBlurb;
        readonly Image[] _glyphChip = new Image[3];
        readonly Text[] _glyphWord = new Text[3];
        readonly Text[] _readyStamp = new Text[4];
        readonly Image[] _castPlate = new Image[4];
        readonly MenuPodium.Row[] _rows = new MenuPodium.Row[4];

        readonly Text[] _castMark = new Text[6];
        readonly RawImage[] _castView = new RawImage[4];
        readonly Text[] _castReady = new Text[4];

        static class GameSettingsRow
        {
            public const int Mouse = 0;
            public const int Pad = 1;
            public const int Invert = 2;
            public const int Fov = 3;
            public const int Master = Tag.Settings.GameSettings.RowMaster;
            public const int Sfx = Tag.Settings.GameSettings.RowSfx;
            public const int Ui = Tag.Settings.GameSettings.RowUi;
            public const int Music = Tag.Settings.GameSettings.RowMusic;
            public const int Mute = Tag.Settings.GameSettings.RowMute;
            public const int Hud = Tag.Settings.GameSettings.RowHud;
            public const int Player = Tag.Settings.GameSettings.RowPlayer;
            public const int Palette = Tag.Settings.GameSettings.RowColorblind;
            public const int Captions = Tag.Settings.GameSettings.RowCaptions;
            public const int Rumble = Tag.Settings.GameSettings.RowRumble;
            public const int Flash = Tag.Settings.GameSettings.RowReduceFlash;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Auto()
        {
            if (Legacy) return;
            MenuHost host = Ensure();
            if (host == null) return;
            string scene = SceneManager.GetActiveScene().name;
            if (scene == "Play" && FrontSession.Armed)
            {
                if (host._screen != MenuScreenId.Loading)
                    host.HideForMatch();
                return;
            }
            if (!FrontSession.Armed)
                host.ShowTitle();
        }

        public static MenuHost Ensure()
        {
            if (Legacy) return null;
            if (Instance != null) return Instance;
            var go = new GameObject("MenuHost");
            DontDestroyOnLoad(go);
            return go.AddComponent<MenuHost>();
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
            MenuVideo.Load();
            BuildShell();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            EatPause = false;
            if (_screen == MenuScreenId.Hidden) return;
            MenuInput.Poll();
            switch (_screen)
            {
                case MenuScreenId.Title: TickTitle(); break;
                case MenuScreenId.Main: TickShared(); break;
                case MenuScreenId.Join: TickJoin(); break;
                case MenuScreenId.Cast: TickCast(); break;
                case MenuScreenId.Rules: TickRules(); break;
                case MenuScreenId.Arena: TickArena(); break;
                case MenuScreenId.Loading: TickLoading(); break;
                case MenuScreenId.Pause: TickPause(); break;
                case MenuScreenId.Results: TickShared(); break;
                case MenuScreenId.Options: TickOptions(); break;
                case MenuScreenId.Controls: TickControls(); break;
                case MenuScreenId.Credits: TickShared(); break;
                case MenuScreenId.Practice: TickPractice(); break;
            }
            if (_footerKind != MenuInput.LastKind)
                PaintFooter();
        }

        void LateUpdate()
        {
            Animate();
            if (_screen == MenuScreenId.Loading)
            {
                FinishLoad();
                return;
            }
            if (_screen != MenuScreenId.Hidden && _screen != MenuScreenId.Pause && _screen != MenuScreenId.Results)
                return;
            WatchMatch();
        }

        public void ShowTitle()
        {
            _holdResults = false;
            _fromResults = false;
            _pauseChild = false;
            if (FrontSession.Armed || FrontSession.Screen != FrontScreen.Title)
                FrontSession.ShowTitle();
            Open(MenuScreenId.Title);
        }

        public void HideForMatch()
        {
            _screen = MenuScreenId.Hidden;
            _pauseChild = false;
            _holdResults = false;
            _fromResults = false;
            _capturing = false;
            if (_canvas != null) _canvas.enabled = false;
            if (_preview != null) _preview.Hide();
        }

        void Open(MenuScreenId id)
        {
            _screen = id;
            if (_canvas != null) _canvas.enabled = true;
            _focus = 0;
            _cols = 1;
            _window = 0;
            _gate = Time.unscaledTime + (MenuVideo.ReduceMotion ? 0.05f : 0.18f);
            _fade = MenuVideo.ReduceMotion ? 1f : 0f;
            _slide = MenuVideo.ReduceMotion ? 0f : 1f;
            HideFlyover();
            if (_group != null) _group.alpha = _fade;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ClearBody();
            switch (id)
            {
                case MenuScreenId.Title: BuildTitle(); break;
                case MenuScreenId.Main: BuildMain(); break;
                case MenuScreenId.Join: BuildJoin(); break;
                case MenuScreenId.Cast: BuildCast(); break;
                case MenuScreenId.Rules: BuildRules(); break;
                case MenuScreenId.Arena: BuildArena(); break;
                case MenuScreenId.Loading: BuildLoading(); break;
                case MenuScreenId.Pause: BuildPause(); break;
                case MenuScreenId.Results: BuildResults(); break;
                case MenuScreenId.Options: BuildOptions(); break;
                case MenuScreenId.Controls: BuildControls(); break;
                case MenuScreenId.Credits: BuildCredits(); break;
                case MenuScreenId.Practice: BuildPractice(); break;
            }
            RefreshFocus();
            PaintFooter();
            if (id == MenuScreenId.Cast)
            {
                if (_preview != null) _preview.Show();
            }
            else if (id != MenuScreenId.Results && _preview != null)
                _preview.Hide();
        }

        void BuildShell()
        {
            var canvasGo = new GameObject("MenuCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 400;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _group = canvasGo.AddComponent<CanvasGroup>();

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("MenuEventSystem");
                es.transform.SetParent(transform, false);
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            RectTransform root = canvasGo.GetComponent<RectTransform>();
            var sky = MenuWidgets.Fill(root, Color.white);
            sky.sprite = MenuArt.Sky;
            sky.type = Image.Type.Simple;
            sky.raycastTarget = false;

            var patternRt = MenuWidgets.Box(root, "Pattern", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            _pattern = patternRt.gameObject.AddComponent<RawImage>();
            _pattern.texture = MenuArt.Chevron;
            _pattern.color = new Color(1f, 1f, 1f, 0.22f);
            _pattern.raycastTarget = false;
            _pattern.uvRect = new Rect(0f, 0f, 10f, 6f);

            var flyRt = MenuWidgets.Box(root, "Flyover", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            _flyover = flyRt.gameObject.AddComponent<RawImage>();
            _flyover.color = new Color(1f, 1f, 1f, 0f);
            _flyover.raycastTarget = false;

            _dim = MenuWidgets.Fill(root, MenuTheme.Veil);
            _dim.raycastTarget = true;

            var sweepRt = MenuWidgets.Place(root, "Sweep", -1920f, 120f, 420f, 18f);
            _sweep = sweepRt.gameObject.AddComponent<Image>();
            MenuArt.Plate(_sweep, MenuTheme.Gold, true);
            _sweep.raycastTarget = false;

            var back = MenuWidgets.Box(root, "Back", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            BuildRibbons(back);

            _header = MenuWidgets.Words(root, "TAG", 42, TextAnchor.MiddleLeft, MenuTheme.Gold, new Vector2(0f, 1f), new Vector2(1f, 1f));
            RectTransform headerRt = _header.rectTransform;
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0f, 96f);
            headerRt.anchoredPosition = Vector2.zero;

            _body = MenuWidgets.Box(root, "Body", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            _body.offsetMin = new Vector2(48f, 78f);
            _body.offsetMax = new Vector2(-48f, -108f);

            _banner = MenuWidgets.Words(root, "", 28, TextAnchor.MiddleCenter, MenuTheme.Gold, new Vector2(0f, 0f), new Vector2(1f, 0f));
            RectTransform bannerRt = _banner.rectTransform;
            bannerRt.anchorMin = new Vector2(0f, 0f);
            bannerRt.anchorMax = new Vector2(1f, 0f);
            bannerRt.pivot = new Vector2(0.5f, 0f);
            bannerRt.sizeDelta = new Vector2(0f, 36f);
            bannerRt.anchoredPosition = new Vector2(0f, 64f);

            _footer = MenuWidgets.Words(root, "", 24, TextAnchor.MiddleCenter, MenuTheme.Cream, new Vector2(0f, 0f), new Vector2(1f, 0f));
            RectTransform footRt = _footer.rectTransform;
            footRt.anchorMin = new Vector2(0f, 0f);
            footRt.anchorMax = new Vector2(1f, 0f);
            footRt.pivot = new Vector2(0.5f, 0f);
            footRt.sizeDelta = new Vector2(0f, 56f);
            footRt.anchoredPosition = Vector2.zero;
            BuildGlyphs(root);

            var previewGo = new GameObject("MenuPreview");
            previewGo.transform.SetParent(transform, false);
            _preview = previewGo.AddComponent<MenuPreview>();
            _preview.Build();
            _canvas.enabled = false;
        }

        void BuildRibbons(RectTransform parent)
        {
            Color[] colors =
            {
                new Color(0.95f, 0.28f, 0.30f, 0.35f),
                new Color(0.28f, 0.62f, 0.98f, 0.30f),
                new Color(0.98f, 0.78f, 0.22f, 0.28f),
                new Color(0.24f, 0.82f, 0.42f, 0.28f),
                new Color(0.70f, 0.40f, 0.90f, 0.25f),
                new Color(0.95f, 0.45f, 0.20f, 0.22f),
                new Color(0.20f, 0.80f, 0.80f, 0.22f)
            };
            for (int i = 0; i < _ribbons.Length; i++)
            {
                var rt = MenuWidgets.Place(parent, "Ribbon" + i.ToString(), -400f, 80f + i * 120f, 520f, 36f);
                var image = rt.gameObject.AddComponent<Image>();
                image.color = colors[i];
                image.raycastTarget = false;
                _ribbons[i] = rt;
            }
        }

        void ClearBody()
        {
            _tiles.Clear();
            _press = null;
            for (int i = 0; i < _castMark.Length; i++) _castMark[i] = null;
            for (int i = 0; i < _castView.Length; i++)
            {
                _castView[i] = null;
                _castReady[i] = null;
            }
            if (_body == null) return;
            for (int i = _body.childCount - 1; i >= 0; i--)
                DestroyImmediate(_body.GetChild(i).gameObject);
        }

        void AddTile(float x, float y, float w, float h, int index, string label, string detail, bool allow)
        {
            MenuTile tile = MenuWidgets.Tile(_body, x, y, w, h, index, label, detail, allow, Hover, Press);
            _tiles.Add(tile);
            if (index + 1 > _count) _count = index + 1;
        }

        void Hover(int index)
        {
            if (!Allow(index) || index == _focus) return;
            _focus = index;
            MenuAudio.Move();
            RefreshFocus();
        }

        void Press(int index)
        {
            if (Time.unscaledTime < _gate) return;
            if (!Allow(index)) return;
            _focus = index;
            RefreshFocus();
            Activate();
        }

        bool Allow(int index)
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                MenuTile tile = _tiles[i];
                if (tile != null && tile.Index == index) return tile.Allow;
            }
            return false;
        }

        void RefreshFocus()
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                MenuTile tile = _tiles[i];
                if (tile != null) tile.SetHot(tile.Index == _focus);
            }
        }

        MenuTile TileAt(int index)
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                if (_tiles[i] != null && _tiles[i].Index == index) return _tiles[i];
            }
            return null;
        }

        void Move(int dx, int dy)
        {
            if (_count < 1) return;
            int cols = _cols < 1 ? 1 : _cols;
            int rows = (_count + cols - 1) / cols;
            int x = _focus % cols;
            int y = _focus / cols;
            for (int guard = 0; guard < _count; guard++)
            {
                if (dx != 0) x = (x + (dx > 0 ? 1 : -1) + cols) % cols;
                if (dy != 0) y = (y + (dy > 0 ? -1 : 1) + rows) % rows;
                int next = y * cols + x;
                if (next >= _count) next = _count - 1;
                if (!Allow(next)) continue;
                if (next == _focus) return;
                _focus = next;
                MenuAudio.Move();
                RefreshFocus();
                return;
            }
        }

        bool Gated()
        {
            return Time.unscaledTime < _gate;
        }

        void ReadNav(out int dx, out int dy, out bool confirm, out bool back, out bool start)
        {
            dx = 0;
            dy = 0;
            confirm = false;
            back = false;
            start = false;
            for (int i = 0; i < MenuInput.Count; i++)
            {
                MenuEdge edge = MenuInput.Edges[i];
                if (edge.X != 0) dx = edge.X;
                if (edge.Y != 0) dy = edge.Y;
                if (edge.Confirm) confirm = true;
                if (edge.Back) back = true;
                if (edge.Start) start = true;
            }
        }

        void TickShared()
        {
            if (Gated()) return;
            ReadNav(out int dx, out int dy, out bool confirm, out bool back, out bool start);
            if (_pauseChild && (start || back))
            {
                EatPause = true;
                if (start) ResumeMatch();
                else ShowPause();
                return;
            }
            if (dx != 0 || dy != 0) Move(dx, dy);
            if (back)
            {
                MenuAudio.Back();
                GoBack();
                return;
            }
            if (confirm || start) Activate();
        }

        void Activate()
        {
            switch (_screen)
            {
                case MenuScreenId.Main: ActivateMain(); break;
                case MenuScreenId.Join: ActivateJoin(); break;
                case MenuScreenId.Cast: ActivateCast(); break;
                case MenuScreenId.Rules: ActivateRules(); break;
                case MenuScreenId.Arena: ActivateArena(); break;
                case MenuScreenId.Pause: ActivatePause(); break;
                case MenuScreenId.Results: ActivateResults(); break;
                case MenuScreenId.Options: ActivateOptions(); break;
                case MenuScreenId.Controls: ActivateControls(); break;
                case MenuScreenId.Credits: ShowMain(); break;
                case MenuScreenId.Practice: ActivatePractice(); break;
            }
        }

        void GoBack()
        {
            switch (_screen)
            {
                case MenuScreenId.Main: ShowTitle(); break;
                case MenuScreenId.Join: ShowMain(); break;
                case MenuScreenId.Cast: Open(_castBack); break;
                case MenuScreenId.Rules: Open(MenuScreenId.Cast); break;
                case MenuScreenId.Arena: Open(_arenaBack); break;
                case MenuScreenId.Options:
                case MenuScreenId.Controls:
                case MenuScreenId.Credits:
                case MenuScreenId.Practice:
                    if (_pauseChild) ShowPause();
                    else ShowMain();
                    break;
                case MenuScreenId.Results:
                    if (_focus >= 3) QuitMatch();
                    else
                    {
                        _focus = 3;
                        RefreshFocus();
                    }
                    break;
                default:
                    ShowMain();
                    break;
            }
        }

        void ShowMain()
        {
            _pauseChild = false;
            Open(MenuScreenId.Main);
        }

        void ShowJoin()
        {
            _joinSig = int.MinValue;
            Open(MenuScreenId.Join);
        }

        void ShowCast()
        {
            _castSig = int.MinValue;
            _allReadyAt = 0f;
            MenuSession.ClearReady();
            for (int s = 0; s < 4; s++)
                MenuSession.PullLook(s);
            Open(MenuScreenId.Cast);
        }

        void ShowRules()
        {
            Open(MenuScreenId.Rules);
        }

        void ShowArena()
        {
            Open(MenuScreenId.Arena);
        }

        void ShowPause()
        {
            _pauseChild = false;
            Open(MenuScreenId.Pause);
        }

        void ShowOptions(bool fromPause)
        {
            _pauseChild = fromPause;
            Open(MenuScreenId.Options);
        }

        void ShowControls(bool fromPause)
        {
            _pauseChild = fromPause;
            _capturing = false;
            _conflict = "";
            Open(MenuScreenId.Controls);
        }

        void BeginLoading(bool practice)
        {
            _loadPractice = practice;
            _loadFired = false;
            _loadAt = Time.unscaledTime + 0.9f;
            Open(MenuScreenId.Loading);
        }

        void BuildGlyphs(RectTransform root)
        {
            string[] words = { "Move", "Confirm", "Back" };
            for (int i = 0; i < 3; i++)
            {
                var chip = MenuWidgets.Place(root, "Glyph" + i.ToString(), 280f + i * 460f, 1010f, 420f, 52f);
                var image = chip.gameObject.AddComponent<Image>();
                MenuArt.Plate(image, MenuTheme.Navy, true);
                image.raycastTarget = false;
                _glyphChip[i] = image;
                _glyphWord[i] = MenuWidgets.Words(chip, words[i], 22, TextAnchor.MiddleCenter, MenuTheme.Cream, Vector2.zero, Vector2.one);
            }
        }

        void PaintFooter()
        {
            _footerKind = MenuInput.LastKind;
            if (_footer == null) return;
            bool pad = _footerKind == Tag.Onboard.InputDeviceKind.Gamepad;
            string move = pad ? "Stick" : "Arrows";
            string confirm = pad ? "South" : "Space";
            string back = pad ? "East" : "Esc";
            _footer.text = "";
            string[] words = { move + "   move", confirm + "   confirm", back + "   back" };
            Color chip = pad ? new Color(0.10f, 0.22f, 0.55f, 1f) : new Color(0.08f, 0.18f, 0.36f, 1f);
            for (int i = 0; i < 3; i++)
            {
                if (_glyphWord[i] != null) _glyphWord[i].text = words[i];
                if (_glyphChip[i] != null) _glyphChip[i].color = chip;
            }
        }

        void Animate()
        {
            if (_group != null && _screen != MenuScreenId.Hidden)
            {
                _fade = Mathf.MoveTowards(_fade, 1f, Time.unscaledDeltaTime / 0.2f);
                _group.alpha = MenuVideo.ReduceMotion ? 1f : _fade;
            }
            _slide = Mathf.MoveTowards(_slide, 0f, Time.unscaledDeltaTime / 0.2f);
            if (_body != null)
            {
                float slide = MenuVideo.ReduceMotion ? 0f : 160f * _slide;
                _body.offsetMin = new Vector2(48f + slide, 78f);
                _body.offsetMax = new Vector2(-48f + slide, -108f);
            }
            if (_sweep != null)
            {
                float u = 1f - _slide;
                RectTransform sweepRt = _sweep.rectTransform;
                Vector2 sp = sweepRt.anchoredPosition;
                sp.x = Mathf.Lerp(-500f, 2100f, u);
                sweepRt.anchoredPosition = sp;
                _sweep.enabled = _slide > 0.02f && !MenuVideo.ReduceMotion;
            }
            if (_pattern != null && !MenuVideo.ReduceMotion)
            {
                Rect uv = _pattern.uvRect;
                uv.x = Time.unscaledTime * 0.08f;
                uv.y = Time.unscaledTime * 0.03f;
                _pattern.uvRect = uv;
            }
            if (_flyover != null && _flyover.color.a > 0.01f && !MenuVideo.ReduceMotion)
            {
                Rect uv = _flyover.uvRect;
                uv.x = 0.04f + Mathf.Sin(Time.unscaledTime * 0.15f) * 0.04f;
                uv.y = 0.02f + Mathf.Cos(Time.unscaledTime * 0.11f) * 0.03f;
                uv.width = 0.92f;
                uv.height = 0.92f;
                _flyover.uvRect = uv;
            }
            if (MenuVideo.ReduceMotion || _screen == MenuScreenId.Hidden) return;
            float t = Time.unscaledTime;
            for (int i = 0; i < _ribbons.Length; i++)
            {
                if (_ribbons[i] == null) continue;
                float x = Mathf.Repeat(t * (40f + i * 12f) + i * 180f, 2200f) - 500f;
                Vector2 p = _ribbons[i].anchoredPosition;
                p.x = x;
                _ribbons[i].anchoredPosition = p;
            }
            if (_press != null && _screen == MenuScreenId.Title)
            {
                Color c = _press.color;
                c.a = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 2.6f));
                _press.color = c;
            }
        }

        void WatchMatch()
        {
            GameFlow flow = GameFlow.Instance;
            if (flow == null) return;
            if (flow.State == GameFlowState.Paused)
            {
                if (_screen != MenuScreenId.Pause && !_pauseChild)
                    ShowPause();
                return;
            }
            if (_screen == MenuScreenId.Pause)
                HideForMatch();
            TagModeController mode = TagModeController.Instance;
            if (_screen == MenuScreenId.Results && (mode == null || mode.Phase != MatchPhase.Results))
            {
                HideForMatch();
                return;
            }
            if (mode != null && mode.Phase == MatchPhase.Results && _screen == MenuScreenId.Hidden)
            {
                _holdResults = true;
                _fromResults = false;
                Open(MenuScreenId.Results);
            }
        }

        void ResumeMatch()
        {
            MenuAudio.Back();
            GameFlow flow = GameFlow.Instance;
            if (flow != null) flow.ReturnToPlay();
            HideForMatch();
        }

        void FinishLoad()
        {
            if (!_loadFired) return;
            TagModeController mode = TagModeController.Instance;
            if (mode != null && mode.Phase != MatchPhase.Idle)
                HideForMatch();
            else if (Time.unscaledTime > _loadAt + 2.5f)
                HideForMatch();
        }

        static void UnlockLooks()
        {
            Tag.Settings.SettingsRuntime.Apply();
            Tag.Settings.SettingsRuntime.Save();
        }


        void TickTitle()
        {
            if (Gated()) return;
            if (!MenuInput.AnyAdvance()) return;
            MenuAudio.Confirm();
            ShowMain();
        }

        void TickJoin()
        {
            if (Gated()) return;
            for (int i = 0; i < MenuInput.Count; i++)
            {
                MenuEdge edge = MenuInput.Edges[i];
                bool seated = CouchPlay.Joined(edge.Device);
                if (!seated && (edge.Join || edge.Confirm || edge.Start))
                {
                    if (CouchPlay.Join(edge.Device))
                    {
                        int slot = SeatOf(edge.Device);
                        if (slot >= 0 && LocalProfiles.SeatName(slot) == null)
                            LocalProfiles.SeatGuest(slot);
                        MenuAudio.Confirm();
                    }
                    continue;
                }
                if (edge.Back)
                {
                    if (seated)
                    {
                        CouchPlay.Leave(edge.Device);
                        MenuAudio.Back();
                    }
                    else if (edge.Device == CouchPlay.DeviceKeyboard)
                    {
                        MenuAudio.Back();
                        ShowMain();
                        return;
                    }
                    continue;
                }
                if (!seated) continue;
                int seat = SeatOf(edge.Device);
                if (edge.X != 0 && seat >= 0)
                {
                    CouchPlay.CycleProfile(seat, edge.X);
                    MenuAudio.Move();
                }
                if ((edge.Confirm || edge.Start) && CouchPlay.Humans >= 1)
                {
                    MenuAudio.Confirm();
                    _castBack = MenuScreenId.Join;
                    _fromResults = false;
                    ShowCast();
                    return;
                }
            }
            if (JoinSig() != _joinSig) BuildJoin();
        }

        void TickCast()
        {
            if (!Gated())
            {
                for (int i = 0; i < MenuInput.Count; i++)
                {
                    MenuEdge edge = MenuInput.Edges[i];
                    int seat = SeatOf(edge.Device);
                    if (seat < 0)
                    {
                        if (edge.Back && edge.Device == CouchPlay.DeviceKeyboard)
                        {
                            MenuAudio.Back();
                            Open(_castBack);
                            return;
                        }
                        continue;
                    }
                    bool moved = false;
                    int cursor = MenuSession.Cursor[seat];
                    int x = cursor % 3;
                    int y = cursor / 3;
                    if (edge.X != 0)
                    {
                        x = (x + edge.X + 3) % 3;
                        moved = true;
                    }
                    if (edge.Y != 0)
                    {
                        y = (y + (edge.Y > 0 ? -1 : 1) + 2) % 2;
                        moved = true;
                    }
                    if (moved)
                    {
                        MenuSession.Cursor[seat] = y * 3 + x;
                        MenuSession.Hier[seat] = MenuSession.Cursor[seat];
                        MenuSession.Ready[seat] = false;
                        MenuAudio.Move();
                    }
                    if (edge.ShoulderL || edge.ShoulderR)
                    {
                        int dir = edge.ShoulderR ? 1 : -1;
                        int accent = MenuSession.Accent[seat] + dir;
                        int n = LocalProfiles.HierNames.Length;
                        if (n < 1) n = 1;
                        if (accent < 0) accent = n - 1;
                        if (accent >= n) accent = 0;
                        MenuSession.Accent[seat] = accent;
                        MenuSession.Ready[seat] = false;
                        MenuAudio.Move();
                    }
                    if (edge.North)
                    {
                        MenuSession.Hat[seat] = MenuSession.Hat[seat] == 0 ? 1 : 0;
                        MenuSession.Ready[seat] = false;
                        MenuAudio.Move();
                    }
                    if (edge.Confirm || edge.Start)
                    {
                        MenuSession.Ready[seat] = !MenuSession.Ready[seat];
                        MenuAudio.Confirm();
                    }
                    if (edge.Back)
                    {
                        if (MenuSession.Ready[seat])
                        {
                            MenuSession.Ready[seat] = false;
                            MenuAudio.Back();
                        }
                        else
                        {
                            MenuAudio.Back();
                            Open(_castBack);
                            return;
                        }
                    }
                }
            }
            if (MenuSession.AllReady())
            {
                if (_allReadyAt <= 0f) _allReadyAt = Time.unscaledTime + 0.45f;
                if (_banner != null) _banner.text = "ALL READY";
                if (Time.unscaledTime >= _allReadyAt)
                {
                    MenuAudio.Confirm();
                    MenuSession.CommitLooks();
                    ShowRules();
                    return;
                }
            }
            else
            {
                _allReadyAt = 0f;
                if (_banner != null) _banner.text = "";
            }
            int sig = CastSig();
            if (sig != _castSig)
            {
                _castSig = sig;
                RefreshCast();
            }
        }

        void TickRules()
        {
            if (Gated()) return;
            ReadNav(out int dx, out int dy, out bool confirm, out bool back, out bool start);
            if (back)
            {
                MenuAudio.Back();
                Open(MenuScreenId.Cast);
                return;
            }
            if (dy != 0) MoveRules(dy);
            if (dx != 0) StepRules(dx);
            if (confirm || start) ActivateRules();
        }

        void TickArena()
        {
            if (Gated()) return;
            ReadNav(out int dx, out int dy, out bool confirm, out bool back, out bool start);
            if (back)
            {
                MenuAudio.Back();
                Open(_arenaBack);
                return;
            }
            if (dx != 0 || dy != 0)
            {
                Move(dx, dy);
                ShowArenaPreview();
            }
            if (confirm || start) ActivateArena();
        }

        void TickLoading()
        {
            if (!_loadFired && Time.unscaledTime >= _loadAt)
            {
                _loadFired = true;
                if (_loadPractice) MenuMatch.StartPractice();
                else MenuMatch.StartMatch();
                return;
            }
            if (Gated() || _loadFired) return;
            ReadNav(out _, out _, out _, out bool back, out _);
            if (!back) return;
            MenuAudio.Back();
            _loadFired = true;
            if (_loadPractice) Open(MenuScreenId.Practice);
            else Open(_arenaBack);
        }

        void TickPause()
        {
            if (Gated()) return;
            ReadNav(out int dx, out int dy, out bool confirm, out bool back, out bool start);
            if (start || back)
            {
                EatPause = true;
                ResumeMatch();
                return;
            }
            if (dx != 0 || dy != 0) Move(dx, dy);
            if (confirm) ActivatePause();
        }

        void TickOptions()
        {
            if (Gated()) return;
            ReadNav(out int dx, out int dy, out bool confirm, out bool back, out bool start);
            if (_pauseChild && start)
            {
                EatPause = true;
                ResumeMatch();
                return;
            }
            if (back)
            {
                if (_pauseChild) EatPause = true;
                MenuAudio.Back();
                if (_pauseChild) ShowPause();
                else ShowMain();
                return;
            }
            if (dy != 0) MoveOptions(dy);
            if (dx != 0) StepOptions(dx);
            if (confirm) ActivateOptions();
        }

        void TickControls()
        {
            if (_capturing)
            {
                ReadNav(out _, out _, out _, out bool cancel, out _);
                if (cancel || UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    if (_pauseChild) EatPause = true;
                    _capturing = false;
                    _conflict = "";
                    MenuAudio.Back();
                    PaintControls();
                    return;
                }
                if (Time.frameCount <= _captureFrame) return;
                string token = BindSampler.AnyPressedToken();
                if (string.IsNullOrEmpty(token) || token == "escape") return;
                TakeBind(token);
                return;
            }
            if (Gated()) return;
            ReadNav(out int dx, out int dy, out bool confirm, out bool back, out bool start);
            if (_pauseChild && start)
            {
                EatPause = true;
                ResumeMatch();
                return;
            }
            if (back)
            {
                if (_pauseChild) EatPause = true;
                MenuAudio.Back();
                if (_pauseChild) ShowPause();
                else ShowMain();
                return;
            }
            if (dy != 0) MoveOptions(dy);
            if (confirm) ActivateControls();
            if (dx != 0) { }
        }

        void TickPractice()
        {
            if (Gated()) return;
            ReadNav(out int dx, out int dy, out bool confirm, out bool back, out bool start);
            if (back)
            {
                MenuAudio.Back();
                PracticeSession.Stop();
                ShowMain();
                return;
            }
            if (dy != 0) Move(0, dy);
            if (dx != 0 && _focus <= 4)
            {
                PracticeSession.StepRow(_focus, dx);
                MenuAudio.Move();
                PaintPractice();
            }
            if (confirm || start) ActivatePractice();
        }

        void BuildTitle()
        {
            _count = 0;
            if (_header != null) _header.text = "";
            if (_banner != null) _banner.text = "";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            ShowFlyover(MenuSession.Arena, 0.9f);
            var plate = MenuWidgets.Place(_body, "LogoPlate", 360f, 70f, 1100f, 250f);
            var plateImage = plate.gameObject.AddComponent<Image>();
            MenuArt.Plate(plateImage, new Color(0.05f, 0.12f, 0.32f, 0.78f), true);
            plateImage.raycastTarget = false;
            Text word = MenuWidgets.Words(_body, "TAG", 180, TextAnchor.MiddleCenter, MenuTheme.Gold, new Vector2(0.08f, 0.40f), new Vector2(0.92f, 0.92f));
            word.alignment = TextAnchor.MiddleCenter;
            Text sub = MenuWidgets.Words(_body, "COUCH TAG", 42, TextAnchor.MiddleCenter, MenuTheme.Cream, new Vector2(0.2f, 0.28f), new Vector2(0.8f, 0.42f));
            sub.alignment = TextAnchor.MiddleCenter;
            _press = MenuWidgets.Words(_body, "Press  Start   /   South   /   Space", 32, TextAnchor.MiddleCenter, MenuTheme.Cream, new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.22f));
        }

        void BuildMain()
        {
            _count = 0;
            _cols = 1;
            if (_header != null) _header.text = "  TAG";
            if (_banner != null) _banner.text = "";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            Text word = MenuWidgets.Words(_body, "TAG", 120, TextAnchor.MiddleLeft, MenuTheme.Gold, new Vector2(0f, 0.45f), new Vector2(0.48f, 0.92f));
            MenuWidgets.Words(_body, "Local couch. One keyboard, four pads.", 28, TextAnchor.UpperLeft, MenuTheme.Cream, new Vector2(0f, 0.28f), new Vector2(0.46f, 0.48f));
            float y = 8f;
            AddTile(980f, y, 760f, 96f, 0, "Play", "Local couch", true); y += 108f;
            AddTile(980f, y, 760f, 96f, 1, "Practice", "Free arena, routes you already have", true); y += 108f;
            AddTile(980f, y, 760f, 96f, 2, "Options", "Sound, picture, access", true); y += 108f;
            AddTile(980f, y, 760f, 96f, 3, "Controls", "Binds. Space still jumps.", true); y += 108f;
            AddTile(980f, y, 360f, 96f, 4, "Credits", "", true);
            AddTile(1380f, y, 360f, 96f, 5, "Quit", "", true); y += 108f;
            AddTile(980f, y, 760f, 80f, 6, "Online", "Coming soon", false);
            _count = 7;
        }

        void BuildJoin()
        {
            _count = 0;
            _cols = 4;
            if (_header != null) _header.text = "  Who's playing";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            _joinSig = JoinSig();
            for (int s = 0; s < 4; s++)
            {
                bool human = CouchPlay.HumanAt(s);
                string title = human ? CouchPlay.Name(s) : "P" + (s + 1).ToString();
                string detail = human ? CouchPlay.SeatLine(s) : "Press a button to join";
                if (human)
                {
                    string profile = LocalProfiles.SeatName(s);
                    if (!string.IsNullOrEmpty(profile)) detail = detail + "\n" + profile;
                    detail = detail + "\nLeft / Right picks a profile";
                }
                float x = 16f + s * 448f;
                AddTile(x, 36f, 428f, 520f, s, title, detail, true);
                MenuTile tile = TileAt(s);
                if (tile != null && tile.Plate != null && human)
                    tile.Plate.color = Color.Lerp(MenuTheme.Panel, MenuTheme.Seat(s), 0.45f);
            }
            _count = 4;
            if (_banner != null)
                _banner.text = CouchPlay.Humans > 0 ? "Seated players press South to continue" : "Anyone can join";
        }

        void BuildCast()
        {
            _count = 0;
            if (_header != null) _header.text = "  Characters";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            int shown = 0;
            for (int s = 0; s < 4; s++)
            {
                if (!CouchPlay.HumanAt(s)) continue;
                float x = 16f + shown * 448f;
                var card = MenuWidgets.Place(_body, "Cast" + s.ToString(), x, 8f, 428f, 420f);
                var plate = card.gameObject.AddComponent<Image>();
                MenuArt.Plate(plate, Color.Lerp(MenuTheme.Panel, MenuTheme.Seat(s), 0.55f), true);
                plate.raycastTarget = false;
                _castPlate[s] = plate;
                var viewRt = MenuWidgets.Place(card, "View", 16f, 16f, 396f, 280f);
                var raw = viewRt.gameObject.AddComponent<RawImage>();
                raw.raycastTarget = false;
                _castView[s] = raw;
                _castReady[s] = MenuWidgets.Words(card, "", 26, TextAnchor.MiddleLeft, MenuTheme.Cream, new Vector2(0f, 0f), new Vector2(1f, 0.28f));
                _readyStamp[s] = MenuWidgets.Words(card, "READY!", 54, TextAnchor.MiddleCenter, MenuTheme.Gold, new Vector2(0.15f, 0.28f), new Vector2(0.85f, 0.62f));
                _readyStamp[s].rectTransform.localRotation = Quaternion.Euler(0f, 0f, -14f);
                _readyStamp[s].gameObject.SetActive(false);
                shown++;
            }
            for (int c = 0; c < 6; c++)
            {
                int col = c % 3;
                int row = c / 3;
                float x = 180f + col * 500f;
                float y = 450f + row * 150f;
                string name = c < LocalProfiles.HierNames.Length ? LocalProfiles.HierNames[c] : "Color";
                AddTile(x, y, 460f, 136f, c, name, "", true);
                MenuTile tile = TileAt(c);
                if (tile != null)
                    _castMark[c] = tile.Detail;
            }
            _count = 6;
            _castSig = int.MinValue;
            RefreshCast();
        }

        void BuildRules()
        {
            _count = 12;
            _cols = 2;
            _focus = (int)MenuSession.Mode;
            if (_focus < 0 || _focus > 3) _focus = 1;
            if (_header != null) _header.text = "  Mode and rules";
            if (_banner != null) _banner.text = "";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            for (int i = 0; i < 4; i++)
            {
                var id = (TagModeId)i;
                int col = i % 2;
                int row = i / 2;
                string mark = MenuSession.Mode == id ? "Selected" : " ";
                AddTile(16f + col * 460f, 12f + row * 168f, 440f, 152f, i, MenuCatalog.ModeName(id), MenuCatalog.ModeBlurb(id) + "\n" + mark, true);
            }
            PaintRuleRows();
        }

        void PaintRuleRows()
        {
            for (int i = _tiles.Count - 1; i >= 0; i--)
            {
                if (_tiles[i] != null && _tiles[i].Index >= 4)
                {
                    DestroyImmediate(_tiles[i].gameObject);
                    _tiles.RemoveAt(i);
                }
            }
            GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
            string[] labels =
            {
                "Round length",
                "Rounds",
                "AI opponents",
                "Difficulty",
                "Split",
                "Listener",
                "Arena select",
                "Back"
            };
            string[] details =
            {
                s.RoundSeconds().ToString("0") + " s",
                s.RoundsPerMatch.ToString(),
                s.AiOpponents.ToString() + "   (0-3, seats left over)",
                s.DifficultyLabel(),
                s.SplitAxis == GameSettings.SplitHorizontal ? "Horizontal" : "Vertical",
                s.Listener == GameSettings.ListenAverage ? "Average" : "P1",
                "Next",
                "Characters"
            };
            for (int i = 0; i < labels.Length; i++)
                AddTile(980f, 12f + i * 92f, 760f, 84f, 4 + i, labels[i], details[i], true);
            _count = 12;
            RefreshFocus();
        }

        void BuildArena()
        {
            _count = 5;
            _cols = 1;
            _focus = MenuSession.RandomArena ? 3 : MenuSession.Arena;
            if (_focus < 0 || _focus > 4) _focus = 0;
            if (_header != null) _header.text = "  Arena";
            if (_banner != null) _banner.text = "Random picks one of the three parks";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            HideFlyover();
            for (int i = 0; i < ParkArena.Count; i++)
            {
                AddTile(24f, 12f + i * 156f, 760f, 144f, i, ParkArena.NameOf(i), MenuCatalog.ArenaBlurb(i), true);
                MenuWidgets.Thumb(TileAt(i), MenuArenaArt.Thumb(i), 18f, 18f, 200f, 108f);
            }
            AddTile(24f, 12f + 3 * 156f, 760f, 144f, 3, "Random", "One of Mega Park, Pocket Park, or Stack Yard.", true);
            AddTile(24f, 12f + 4 * 156f, 360f, 100f, 4, "Back", "", true);
            var shot = MenuWidgets.Place(_body, "ArenaShot", 820f, 12f, 980f, 620f);
            var frame = shot.gameObject.AddComponent<Image>();
            MenuArt.Plate(frame, MenuTheme.Navy, true);
            frame.raycastTarget = false;
            var view = MenuWidgets.Place(shot, "Shot", 18f, 18f, 944f, 460f);
            _arenaShot = view.gameObject.AddComponent<RawImage>();
            _arenaShot.raycastTarget = false;
            _arenaShotName = MenuWidgets.Words(shot, "", 48, TextAnchor.MiddleLeft, MenuTheme.Gold, new Vector2(0f, 0.08f), new Vector2(1f, 0.28f));
            _arenaShotBlurb = MenuWidgets.Words(shot, "", 26, TextAnchor.UpperLeft, MenuTheme.Cream, new Vector2(0f, 0f), new Vector2(1f, 0.14f));
            ShowArenaPreview();
        }

        void ShowArenaPreview()
        {
            if (_arenaShot == null) return;
            int id = _focus;
            bool random = id == 3 || id < 0 || id >= ParkArena.Count;
            int shown = random ? MenuSession.Arena : id;
            if (shown < 0 || shown >= ParkArena.Count) shown = 0;
            Texture tex = MenuArenaArt.Thumb(shown);
            _arenaShot.texture = tex;
            _arenaShot.color = tex != null ? Color.white : MenuTheme.Panel;
            if (_arenaShotName != null)
                _arenaShotName.text = random ? "Random" : ParkArena.NameOf(shown);
            if (_arenaShotBlurb != null)
                _arenaShotBlurb.text = random ? "One of the three parks." : MenuCatalog.ArenaBlurb(shown);
        }

        void ShowFlyover(int arena, float alpha)
        {
            if (_flyover == null) return;
            Texture tex = MenuArenaArt.Thumb(arena);
            _flyover.texture = tex;
            _flyover.color = tex != null ? new Color(1f, 1f, 1f, alpha) : new Color(1f, 1f, 1f, 0f);
        }

        void HideFlyover()
        {
            if (_flyover == null) return;
            _flyover.color = new Color(1f, 1f, 1f, 0f);
        }

        void BuildLoading()
        {
            _count = 0;
            if (_dim != null) _dim.color = MenuTheme.Veil;
            string name = MenuSession.RandomArena ? "Random" : ParkArena.NameOf(MenuSession.Arena);
            if (_loadPractice) name = PracticeSession.ArenaName();
            if (_header != null) _header.text = "  Loading";
            int fly = MenuSession.Arena;
            if (fly < 0 || fly >= ParkArena.Count) fly = 0;
            ShowFlyover(fly, 0.55f);
            MenuWidgets.Words(_body, name, 84, TextAnchor.MiddleCenter, MenuTheme.Gold, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.78f));
            string tip = MenuCatalog.Tip(_tip);
            _tip++;
            MenuWidgets.Words(_body, tip, 32, TextAnchor.MiddleCenter, MenuTheme.Cream, new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.40f));
            if (_banner != null) _banner.text = "";
        }

        void BuildPause()
        {
            _count = 5;
            _cols = 1;
            _focus = 0;
            if (_dim != null) _dim.color = MenuTheme.Dim;
            int seat = 0;
            if (GameSettings.Current != null) seat = GameSettings.Current.AccessSeat;
            string who = CouchPlay.Name(seat);
            if (string.IsNullOrEmpty(who)) who = "P" + (seat + 1).ToString();
            if (_header != null) _header.text = "  Paused";
            if (_banner != null) _banner.text = "Paused by " + who;
            float x = CouchPlay.Humans >= 2 ? 560f : 480f;
            AddTile(x, 20f, 720f, 100f, 0, "Resume", "", true);
            AddTile(x, 136f, 720f, 100f, 1, "Restart", "Same arena, same rules", true);
            AddTile(x, 252f, 720f, 100f, 2, "Options", "", true);
            AddTile(x, 368f, 720f, 100f, 3, "Controls", "", true);
            AddTile(x, 484f, 720f, 100f, 4, "Quit to menu", "", true);
        }

        void BuildResults()
        {
            _holdResults = true;
            _count = 4;
            _cols = 4;
            _focus = 0;
            if (_dim != null) _dim.color = new Color(0.03f, 0.05f, 0.09f, 0.72f);
            HideFlyover();
            TagModeController mode = TagModeController.Instance;
            TagModeId modeId = mode != null ? mode.SelectedMode : MenuSession.Mode;
            string headline = "Results";
            if (mode != null && !string.IsNullOrEmpty(mode.ResultMessage))
                headline = mode.ResultMessage;
            if (_header != null) _header.text = "  " + headline;
            if (_banner != null) _banner.text = MenuCatalog.ModeName(modeId);
            int n = FillRanks();
            var stage = MenuWidgets.Place(_body, "PodiumView", 80f, 8f, 1680f, 460f);
            var frame = stage.gameObject.AddComponent<Image>();
            MenuArt.Plate(frame, MenuTheme.Navy, true);
            frame.raycastTarget = false;
            var viewRt = MenuWidgets.Place(stage, "View", 16f, 16f, 1648f, 360f);
            var view = viewRt.gameObject.AddComponent<RawImage>();
            view.raycastTarget = false;
            if (_preview != null) _preview.ShowPodium(n, _rows, view);
            for (int rank = 0; rank < n; rank++)
            {
                string detail = MenuPodium.Detail(modeId, _rows[rank]);
                if (_rows[rank].Winner) detail = "WIN  " + detail;
                AddTile(40f + rank * 460f, 480f, 440f, 120f, 20 + rank, MenuTheme.Place(rank) + "  " + _rows[rank].Name, detail, false);
                MenuTile tile = TileAt(20 + rank);
                if (tile != null && tile.Bar != null) tile.Bar.color = MenuTheme.Seat(rank);
                if (tile != null && tile.Stroke != null && _rows[rank].Winner)
                    tile.Stroke.color = MenuTheme.Gold;
            }
            AddTile(40f, 620f, 420f, 100f, 0, "Next round", "Same setup", true);
            AddTile(480f, 620f, 420f, 100f, 1, "Change arena", "", true);
            AddTile(920f, 620f, 420f, 100f, 2, "Change characters", "", true);
            AddTile(1360f, 620f, 420f, 100f, 3, "Quit to menu", "", true);
            _count = 4;
            _cols = 4;
        }

        int FillRanks()
        {
            return MenuPodium.Fill(_rows);
        }

        void BuildOptions()
        {
            _count = OptCount;
            _cols = 1;
            _window = 0;
            if (_header != null) _header.text = "  Options";
            if (_banner != null) _banner.text = "Look is shared. Palette, captions, rumble, and flash are per player.";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            PaintOptions();
        }

        void PaintOptions()
        {
            ClearKeepHeader();
            int win = OptWindow;
            if (_focus < _window) _window = _focus;
            if (_focus >= _window + win) _window = _focus - (win - 1);
            int max = _count - win;
            if (max < 0) max = 0;
            if (_window > max) _window = max;
            if (_window < 0) _window = 0;
            for (int v = 0; v < win; v++)
            {
                int index = _window + v;
                if (index >= OptCount) break;
                AddTile(360f, 8f + v * 96f, 1120f, 88f, index, OptionTitle(index), OptionDetail(index), true);
            }
            RefreshFocus();
        }

        void BuildControls()
        {
            _count = (int)PlayAction.Count + 2;
            _cols = 1;
            _window = 0;
            if (_header != null) _header.text = "  Controls";
            if (_banner != null) _banner.text = "Space still jumps. A saved Jump key that is not a real key comes back as Space.";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            PaintControls();
        }

        void PaintControls()
        {
            ClearKeepHeader();
            ActionBinds binds = ActionBinds.Current ?? ActionBinds.Defaults();
            int win = OptWindow;
            if (_focus < _window) _window = _focus;
            if (_focus >= _window + win) _window = _focus - (win - 1);
            int max = _count - win;
            if (max < 0) max = 0;
            if (_window > max) _window = max;
            if (_window < 0) _window = 0;
            for (int v = 0; v < win; v++)
            {
                int index = _window + v;
                if (index >= _count) break;
                string title;
                string detail;
                if (index < (int)PlayAction.Count)
                {
                    var action = (PlayAction)index;
                    title = ActionBinds.Name(action);
                    detail = ActionBinds.Show(binds.Keyboard[index]) + "    /    " + ActionBinds.Show(binds.Gamepad[index]);
                    if (_capturing && index == _captureAction) detail = "Press a key or a button";
                }
                else if (index == (int)PlayAction.Count)
                {
                    title = "Reset bindings";
                    detail = "Back to the defaults. Jump is Space.";
                }
                else
                {
                    title = "Back";
                    detail = _conflict ?? "";
                }
                AddTile(280f, 8f + v * 96f, 1280f, 88f, index, title, detail, true);
            }
            RefreshFocus();
        }

        void BuildCredits()
        {
            _count = 1;
            _cols = 1;
            if (_header != null) _header.text = "  Credits";
            if (_banner != null) _banner.text = "";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            MenuWidgets.Words(_body, MenuCatalog.Credits(), 28, TextAnchor.UpperLeft, MenuTheme.Cream, new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.96f));
            AddTile(680f, 700f, 480f, 88f, 0, "Back", "", true);
        }

        void BuildPractice()
        {
            _count = PracticeSession.Rows;
            _cols = 1;
            if (_header != null) _header.text = "  Practice";
            if (_banner != null) _banner.text = "Free roam. Dummy stays passive when it is on.";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            PaintPractice();
        }

        void PaintPractice()
        {
            ClearKeepHeader();
            for (int i = 0; i < PracticeSession.Rows; i++)
                AddTile(420f, 12f + i * 100f, 1000f, 90f, i, PracticeSession.RowLabel(i), "", true);
            _count = PracticeSession.Rows;
            RefreshFocus();
        }

        void ClearKeepHeader()
        {
            _tiles.Clear();
            if (_body == null) return;
            for (int i = _body.childCount - 1; i >= 0; i--)
                DestroyImmediate(_body.GetChild(i).gameObject);
        }

        void ActivateMain()
        {
            MenuAudio.Confirm();
            switch (_focus)
            {
                case 1:
                    PracticeSession.Open();
                    Open(MenuScreenId.Practice);
                    break;
                case 2:
                    ShowOptions(false);
                    break;
                case 3:
                    ShowControls(false);
                    break;
                case 4:
                    Open(MenuScreenId.Credits);
                    break;
                case 5:
                    QuitApp();
                    break;
                case 6:
                    break;
                default:
                    ShowJoin();
                    break;
            }
        }

        void ActivateJoin()
        {
            if (!CouchPlay.Joined(CouchPlay.DeviceKeyboard))
            {
                if (CouchPlay.Join(CouchPlay.DeviceKeyboard))
                {
                    int slot = SeatOf(CouchPlay.DeviceKeyboard);
                    if (slot >= 0 && LocalProfiles.SeatName(slot) == null)
                        LocalProfiles.SeatGuest(slot);
                    MenuAudio.Confirm();
                    _joinSig = int.MinValue;
                    BuildJoin();
                }
                return;
            }
            MenuAudio.Confirm();
            _castBack = MenuScreenId.Join;
            _fromResults = false;
            ShowCast();
        }

        void ActivateCast()
        {
            int seat = SeatOf(CouchPlay.DeviceKeyboard);
            if (seat < 0)
            {
                for (int s = 0; s < CouchPlay.Max; s++)
                {
                    if (!CouchPlay.HumanAt(s)) continue;
                    seat = s;
                    break;
                }
            }
            if (seat < 0) return;
            if (_focus < 0 || _focus > 5) return;
            MenuSession.Cursor[seat] = _focus;
            MenuSession.Hier[seat] = _focus;
            MenuSession.Ready[seat] = false;
            MenuAudio.Move();
            RefreshCast();
            _castSig = CastSig();
        }

        void ActivateRules()
        {
            if (_focus <= 3)
            {
                MenuSession.Mode = (TagModeId)_focus;
                MenuAudio.Confirm();
                PaintRuleRows();
                return;
            }
            if (_focus == 10)
            {
                MenuAudio.Confirm();
                MenuMatch.RememberRules();
                _arenaBack = MenuScreenId.Rules;
                ShowArena();
                return;
            }
            if (_focus >= 11)
            {
                MenuAudio.Back();
                Open(MenuScreenId.Cast);
                return;
            }
            StepRules(1);
        }

        void ActivateArena()
        {
            if (_focus >= 4)
            {
                MenuAudio.Back();
                Open(_arenaBack);
                return;
            }
            MenuAudio.Confirm();
            MenuSession.RandomArena = _focus == 3;
            if (!MenuSession.RandomArena) MenuSession.Arena = _focus;
            BeginLoading(false);
        }

        void ActivatePause()
        {
            switch (_focus)
            {
                case 1:
                    MenuAudio.Confirm();
                    HideForMatch();
                    GameFlow flow = GameFlow.Instance;
                    if (flow != null) flow.Rematch();
                    break;
                case 2:
                    MenuAudio.Confirm();
                    ShowOptions(true);
                    break;
                case 3:
                    MenuAudio.Confirm();
                    ShowControls(true);
                    break;
                case 4:
                    EatPause = true;
                    MenuAudio.Back();
                    QuitMatch();
                    break;
                default:
                    EatPause = true;
                    ResumeMatch();
                    break;
            }
        }

        void ActivateResults()
        {
            switch (_focus)
            {
                case 1:
                    MenuAudio.Confirm();
                    _fromResults = true;
                    _arenaBack = MenuScreenId.Results;
                    ShowArena();
                    break;
                case 2:
                    MenuAudio.Confirm();
                    _fromResults = true;
                    _castBack = MenuScreenId.Results;
                    ShowCast();
                    break;
                case 3:
                    MenuAudio.Back();
                    QuitMatch();
                    break;
                default:
                    MenuAudio.Confirm();
                    _holdResults = false;
                    HideForMatch();
                    GameFlow flow = GameFlow.Instance;
                    if (flow != null) flow.Rematch();
                    break;
            }
        }

        void ActivateOptions()
        {
            if (_focus >= 21)
            {
                MenuAudio.Back();
                if (_pauseChild) ShowPause();
                else ShowMain();
                return;
            }
            if (_focus == 20)
            {
                GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
                GameSettings.Current = s;
                s.ResetToDefaults();
                UnlockLooks();
                MenuAudio.Confirm();
                PaintOptions();
                return;
            }
            StepOptions(1);
        }

        void ActivateControls()
        {
            if (_focus == (int)PlayAction.Count)
            {
                ActionBinds binds = ActionBinds.Current ?? ActionBinds.Defaults();
                binds.ResetToDefaults();
                ActionBinds.Current = binds;
                LocalProfiles.StoreBinds(0, binds);
                SettingsRuntime.Save();
                _conflict = "";
                MenuAudio.Confirm();
                PaintControls();
                return;
            }
            if (_focus > (int)PlayAction.Count)
            {
                MenuAudio.Back();
                if (_pauseChild) ShowPause();
                else ShowMain();
                return;
            }
            _capturing = true;
            _captureAction = _focus;
            _captureFrame = Time.frameCount;
            _conflict = "";
            MenuAudio.Confirm();
            PaintControls();
        }

        void ActivatePractice()
        {
            if (_focus >= 6)
            {
                MenuAudio.Back();
                PracticeSession.Stop();
                ShowMain();
                return;
            }
            if (_focus == 5)
            {
                MenuAudio.Confirm();
                BeginLoading(true);
                return;
            }
            PracticeSession.StepRow(_focus, 1);
            MenuAudio.Move();
            PaintPractice();
        }

        void MoveRules(int dy)
        {
            int next = _focus + (dy > 0 ? -1 : 1);
            if (next < 0) next = 0;
            if (next >= _count) next = _count - 1;
            if (next == _focus) return;
            _focus = next;
            MenuAudio.Move();
            RefreshFocus();
        }

        void StepRules(int dir)
        {
            if (_focus <= 3)
            {
                int next = _focus + (dir > 0 ? 1 : -1);
                if (next < 0) next = 0;
                if (next > 3) next = 3;
                if (next == _focus) return;
                _focus = next;
                MenuAudio.Move();
                RefreshFocus();
                return;
            }
            GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
            GameSettings.Current = s;
            int step = dir > 0 ? 1 : -1;
            if (_focus == 4) s.RoundLengthIndex += step;
            else if (_focus == 5) s.RoundsPerMatch += step;
            else if (_focus == 6) s.AiOpponents += step;
            else if (_focus == 7) s.DifficultyTier += step;
            else if (_focus == 8)
                s.SplitAxis = s.SplitAxis == GameSettings.SplitHorizontal ? GameSettings.SplitVertical : GameSettings.SplitHorizontal;
            else if (_focus == 9)
                s.Listener = s.Listener == GameSettings.ListenAverage ? GameSettings.ListenP1 : GameSettings.ListenAverage;
            else return;
            s.Clamp();
            MenuMatch.RememberRules();
            MenuAudio.Move();
            PaintRuleRows();
        }

        void MoveOptions(int dy)
        {
            int next = _focus + (dy > 0 ? -1 : 1);
            if (next < 0) next = 0;
            if (next >= _count) next = _count - 1;
            if (next == _focus) return;
            int before = _window;
            _focus = next;
            MenuAudio.Move();
            if (_screen == MenuScreenId.Options)
            {
                int span = OptWindow;
                if (_focus < _window || _focus >= _window + span || before != _window)
                    PaintOptions();
                else
                    RefreshFocus();
            }
            else if (_screen == MenuScreenId.Controls)
            {
                int span = OptWindow;
                if (_focus < _window || _focus >= _window + span)
                    PaintControls();
                else
                    RefreshFocus();
            }
            else
                RefreshFocus();
        }

        void StepOptions(int dir)
        {
            GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
            GameSettings.Current = s;
            if (_focus >= 0 && _focus < OptMap.Length)
                s.Nudge(OptMap[_focus], dir > 0 ? 1 : -1);
            else if (_focus == 15) MenuVideo.ToggleMotion();
            else if (_focus == 16) MenuVideo.CycleRes(dir);
            else if (_focus == 17) MenuVideo.ToggleFull();
            else if (_focus == 18) MenuVideo.ToggleVSync();
            else if (_focus == 19) MenuVideo.CycleQuality(dir);
            else return;
            s.Clamp();
            UnlockLooks();
            MenuAudio.Move();
            MenuTile tile = TileAt(_focus);
            if (tile != null && tile.Detail != null) tile.Detail.text = OptionDetail(_focus);
            if (tile != null && tile.Label != null) tile.Label.text = OptionTitle(_focus);
        }

        string OptionTitle(int index)
        {
            if (index >= 0 && index < OptMap.Length)
            {
                GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
                return s.RowLabel(OptMap[index]);
            }
            switch (index)
            {
                case 15: return "Reduce motion  " + (MenuVideo.ReduceMotion ? "On" : "Off");
                case 16: return "Resolution  " + MenuVideo.ResLabel();
                case 17: return "Fullscreen  " + (MenuVideo.Full ? "On" : "Window");
                case 18: return "VSync  " + (MenuVideo.VSync ? "On" : "Off");
                case 19: return "Quality  " + MenuVideo.QualityLabel();
                case 20: return "Reset look and audio";
                default: return "Back";
            }
        }

        string OptionDetail(int index)
        {
            if (index == 15) return "Menu slides and the title pulse only";
            if (index == 20) return "Does not change the park or the binds";
            if (index >= 21) return "";
            return "Left / Right";
        }

        void TakeBind(string token)
        {
            var action = (PlayAction)_captureAction;
            bool pad = IsPad(token);
            if (!pad && action == PlayAction.Jump && !ActionBinds.KnownKeyboard(token))
            {
                _conflict = "That key is not kept for Jump. Space stays jump.";
                _capturing = false;
                MenuAudio.Back();
                PaintControls();
                return;
            }
            ActionBinds trial = (ActionBinds.Current ?? ActionBinds.Defaults()).Clone();
            if (pad) trial.SetGamepad(action, token);
            else trial.SetKeyboard(action, token);
            if (trial.Conflict(action, out PlayAction other))
            {
                _conflict = ActionBinds.Name(action) + " conflicts with " + ActionBinds.Name(other);
                _capturing = false;
                MenuAudio.Back();
                PaintControls();
                return;
            }
            ActionBinds.Current = trial;
            LocalProfiles.StoreBinds(0, trial);
            SettingsRuntime.Save();
            _capturing = false;
            _conflict = "";
            MenuAudio.Confirm();
            PaintControls();
        }

        static bool IsPad(string token)
        {
            return token == "buttonSouth" || token == "buttonEast" || token == "buttonWest"
                || token == "buttonNorth" || token == "leftShoulder" || token == "rightShoulder"
                || token == "leftStickPress" || token == "rightStickPress"
                || token == "start" || token == "select"
                || token == "dpadLeft" || token == "dpadRight" || token == "dpadUp" || token == "dpadDown";
        }

        void RefreshCast()
        {
            for (int c = 0; c < _castMark.Length; c++)
            {
                if (_castMark[c] == null) continue;
                string who = "";
                for (int s = 0; s < 4; s++)
                {
                    if (!CouchPlay.HumanAt(s)) continue;
                    if (MenuSession.Cursor[s] != c) continue;
                    if (who.Length > 0) who += "  ";
                    who += "P" + (s + 1).ToString();
                    if (MenuSession.Ready[s]) who += " ready";
                }
                _castMark[c].text = who;
            }
            for (int s = 0; s < 4; s++)
            {
                if (!CouchPlay.HumanAt(s)) continue;
                if (_preview != null) _preview.Apply(s, MenuSession.Hier[s], MenuSession.Accent[s], MenuSession.Hat[s], _castView[s]);
                if (_castReady[s] == null) continue;
                string skin = MenuSession.Hier[s] >= 0 && MenuSession.Hier[s] < LocalProfiles.HierNames.Length
                    ? LocalProfiles.HierNames[MenuSession.Hier[s]] : "Tan";
                string trim = MenuSession.Accent[s] >= 0 && MenuSession.Accent[s] < LocalProfiles.HierNames.Length
                    ? LocalProfiles.HierNames[MenuSession.Accent[s]] : skin;
                string ready = MenuSession.Ready[s] ? "READY" : "Not ready";
                string hat = MenuSession.Hat[s] == 0 ? "Hat off" : "Hat on";
                _castReady[s].text = CouchPlay.Name(s) + "   " + skin + " / " + trim + "   " + hat + "   " + ready;
                _castReady[s].color = MenuSession.Ready[s] ? MenuTheme.Ready : MenuTheme.Cream;
                if (_readyStamp[s] != null)
                {
                    _readyStamp[s].gameObject.SetActive(MenuSession.Ready[s]);
                    if (MenuSession.Ready[s] && !MenuVideo.ReduceMotion)
                    {
                        float punch = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f);
                        _readyStamp[s].rectTransform.localScale = new Vector3(punch, punch, 1f);
                    }
                }
                if (_castPlate[s] != null)
                    _castPlate[s].color = Color.Lerp(MenuTheme.Panel, MenuTheme.Seat(s), MenuSession.Ready[s] ? 0.75f : 0.45f);
            }
        }

        void QuitMatch()
        {
            _holdResults = false;
            HideForMatch();
            GameFlow flow = GameFlow.Instance;
            if (flow != null) flow.QuitToMenu();
            else ShowTitle();
        }

        void QuitApp()
        {
            MenuAudio.Back();
            Application.Quit();
        }

        static int SeatOf(int device)
        {
            for (int s = 0; s < CouchPlay.Max; s++)
            {
                if (CouchPlay.HumanAt(s) && CouchPlay.DeviceOf(s) == device) return s;
            }
            return -1;
        }

        static int JoinSig()
        {
            int n = CouchPlay.Humans;
            for (int s = 0; s < 4; s++)
            {
                n = n * 17 + CouchPlay.DeviceOf(s) + 3;
                n = n * 13 + LocalProfiles.ProfileAt(s);
            }
            return n;
        }

        static int CastSig()
        {
            int n = CouchPlay.Humans;
            for (int s = 0; s < 4; s++)
            {
                n = n * 11 + MenuSession.Cursor[s];
                n = n * 7 + MenuSession.Accent[s];
                n = n * 3 + MenuSession.Hat[s];
                if (MenuSession.Ready[s]) n += 1;
            }
            return n;
        }
    }
}
