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

        const int OptWindow = 8;

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
        readonly Image[] _glyphIcon = new Image[3];
        readonly Text[] _glyphWord = new Text[3];
        float _actAt;
        readonly Text[] _readyStamp = new Text[4];
        readonly Text[] _castJoin = new Text[4];
        readonly Image[] _castPlate = new Image[4];
        readonly MenuPodium.Row[] _rows = new MenuPodium.Row[4];
        readonly MenuSplitPause.Card[] _cards = new MenuSplitPause.Card[4];
        Image _lostPlate;
        Text _lostWho;

        readonly Text[] _castMark = new Text[6];
        readonly RawImage[] _castView = new RawImage[4];
        readonly Text[] _castReady = new Text[4];

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
            if (MenuCapture.Drive(this)) return;
            if (_actAt > 0f)
            {
                if (_screen == MenuScreenId.Pause || _pauseChild)
                    EatPause = true;
                if (Time.unscaledTime < _actAt) return;
                _actAt = 0f;
                Activate();
                return;
            }
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
            if (MenuCapture.Running) return;
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
            MenuFlow.Clear();
            MenuFlow.Quiet();
            Open(MenuScreenId.Title);
        }

        public void HideForMatch()
        {
            _screen = MenuScreenId.Hidden;
            _pauseChild = false;
            _holdResults = false;
            _fromResults = false;
            _capturing = false;
            MenuFlow.Clear();
            MenuFlow.PreviewLost(false);
            MenuReveal.Clear();
            if (_canvas != null) _canvas.enabled = false;
            if (_preview != null) _preview.Hide();
        }

        void Open(MenuScreenId id)
        {
            MenuFlow.Enter(_screen);
            MenuReveal.Clear();
            MenuAttract.Clear();
            MenuReady.Stop();
            _screen = id;
            if (_canvas != null) _canvas.enabled = true;
            _focus = 0;
            _cols = 1;
            _window = 0;
            _gate = Time.unscaledTime + (MenuVideo.ReduceMotion ? 0.05f : MenuFlow.SlideSeconds);
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
            if (id != MenuScreenId.Hidden)
                MenuAudio.EnsureBed();
        }

        public void Present(MenuScreenId id)
        {
            MenuFlow.PreviewLost(false);
            Open(id);
        }

        public void PresentSplit(int humans)
        {
            MenuFlow.PreviewLost(false);
            MenuSplitPause.Preview = humans;
            Open(MenuScreenId.Pause);
            MenuSplitPause.Preview = 0;
        }

        public void PresentOptions(int page)
        {
            MenuFlow.PreviewLost(false);
            Open(MenuScreenId.Options);
            MenuDepth.Page = page;
            _focus = 0;
            _window = 0;
            PaintOptions();
        }

        public void PresentLost()
        {
            Open(MenuScreenId.Pause);
            MenuFlow.PreviewLost(true);
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
            BuildLost(root);
            _canvas.enabled = false;
        }

        void BuildLost(RectTransform root)
        {
            var shell = MenuWidgets.Place(root, "Lost", 452f, 272f, 1016f, 376f);
            _lostPlate = shell.gameObject.AddComponent<Image>();
            MenuArt.Plate(_lostPlate, MenuTheme.Gold, true);
            _lostPlate.raycastTarget = true;
            var plate = MenuWidgets.Place(shell, "Plate", 8f, 8f, 1000f, 360f);
            var plateImage = plate.gameObject.AddComponent<Image>();
            MenuArt.Plate(plateImage, MenuTheme.Navy, true);
            plateImage.raycastTarget = false;
            MenuWidgets.Words(plate, "Controller disconnected", 42, TextAnchor.MiddleCenter, MenuTheme.Gold, new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.92f));
            _lostWho = MenuWidgets.Words(plate, "", 48, TextAnchor.MiddleCenter, MenuTheme.Cream, new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.64f));
            MenuWidgets.Words(plate, "Plug that pad back in. Resume waits.", 28, TextAnchor.MiddleCenter, MenuTheme.Mute, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.34f));
            MenuFlow.BindLost(_lostPlate, _lostWho);
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
                _castJoin[i] = null;
                _castPlate[i] = null;
                _readyStamp[i] = null;
            }
            if (_body == null) return;
            for (int i = _body.childCount - 1; i >= 0; i--)
                DestroyImmediate(_body.GetChild(i).gameObject);
        }

        MenuTile AddTile(float x, float y, float w, float h, int index, string label, string detail, bool allow)
        {
            MenuTile tile = MenuWidgets.Tile(_body, x, y, w, h, index, label, detail, allow, Hover, Press);
            MenuJuice.AddSweep(tile);
            _tiles.Add(tile);
            if (index + 1 > _count) _count = index + 1;
            return tile;
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
            if (MenuFlow.Locked(_slide, _gate)) return;
            if (!Allow(index)) return;
            _focus = index;
            RefreshFocus();
            ArmActivate();
        }

        void ArmActivate()
        {
            MenuTile tile = TileAt(_focus);
            if (tile != null) tile.PunchIn();
            if (MenuVideo.ReduceMotion || MenuCapture.Running)
            {
                Activate();
                return;
            }
            _actAt = Time.unscaledTime + 0.12f;
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
            return MenuFlow.Locked(_slide, _gate);
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
                else Retreat(MenuScreenId.Pause);
                return;
            }
            if (dx != 0 || dy != 0) Move(dx, dy);
            if (back)
            {
                MenuAudio.Back();
                GoBack();
                return;
            }
            if (confirm || start) ArmActivate();
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
                case MenuScreenId.Credits:
                    MenuAudio.Back();
                    GoBack();
                    break;
                case MenuScreenId.Practice: ActivatePractice(); break;
            }
        }

        void GoBack()
        {
            switch (_screen)
            {
                case MenuScreenId.Main:
                    Retreat(MenuScreenId.Title);
                    break;
                case MenuScreenId.Join:
                    Retreat(MenuScreenId.Main);
                    break;
                case MenuScreenId.Cast:
                    Retreat(SafeScreen(_castBack, MenuScreenId.Join));
                    break;
                case MenuScreenId.Rules:
                    Retreat(_fromResults ? MenuScreenId.Results : MenuScreenId.Cast);
                    break;
                case MenuScreenId.Arena:
                    Retreat(SafeScreen(_arenaBack, MenuScreenId.Rules));
                    break;
                case MenuScreenId.Options:
                    if (MenuDepth.ClosePage())
                    {
                        _focus = 0;
                        _window = 0;
                        PaintOptions();
                        Rewind();
                        break;
                    }
                    Retreat(MenuFlow.Parent(_pauseChild ? MenuScreenId.Pause : MenuScreenId.Main));
                    break;
                case MenuScreenId.Controls:
                case MenuScreenId.Credits:
                case MenuScreenId.Practice:
                    Retreat(MenuFlow.Parent(_pauseChild ? MenuScreenId.Pause : MenuScreenId.Main));
                    break;
                case MenuScreenId.Loading:
                    Retreat(_loadPractice ? MenuScreenId.Practice : SafeScreen(_arenaBack, MenuScreenId.Arena));
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
                    Retreat(MenuFlow.Parent(MenuScreenId.Main));
                    break;
            }
        }

        void Retreat(MenuScreenId id)
        {
            if (id == MenuScreenId.Hidden || id == MenuScreenId.Loading)
                id = _pauseChild ? MenuScreenId.Pause : MenuScreenId.Main;
            MenuFlow.TrimTo(id);
            if (id == MenuScreenId.Title) ShowTitle();
            else if (id == MenuScreenId.Main) ShowMain();
            else if (id == MenuScreenId.Pause) ShowPause();
            else Open(id);
        }

        static MenuScreenId SafeScreen(MenuScreenId id, MenuScreenId fallback)
        {
            if (id == MenuScreenId.Hidden || id == MenuScreenId.Loading) return fallback;
            return id;
        }

        void Rewind()
        {
            bool snap = MenuVideo.ReduceMotion || MenuCapture.Running;
            _gate = Time.unscaledTime + (snap ? 0.05f : MenuFlow.SlideSeconds);
            _slide = snap ? 0f : 1f;
            _fade = MenuVideo.ReduceMotion ? 1f : 0f;
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
            MenuReady.Stop();
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
            MenuAudio.StartMatch();
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
                var iconRt = MenuWidgets.Place(chip, "Icon", 8f, 6f, 72f, 40f);
                var icon = iconRt.gameObject.AddComponent<Image>();
                icon.sprite = MenuIcons.Keys;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                _glyphIcon[i] = icon;
                _glyphWord[i] = MenuWidgets.Words(chip, words[i], 22, TextAnchor.MiddleCenter, MenuTheme.Cream, Vector2.zero, Vector2.one);
                _glyphWord[i].rectTransform.offsetMin = new Vector2(86f, 4f);
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
                if (_glyphIcon[i] != null) _glyphIcon[i].sprite = FooterIcon(pad, i);
            }
        }

        void Animate()
        {
            MenuJuice.Tick(Time.unscaledDeltaTime);
            if (MenuCapture.Running)
            {
                _fade = 1f;
                _slide = 0f;
            }
            if (_group != null && _screen != MenuScreenId.Hidden)
            {
                _fade = Mathf.MoveTowards(_fade, 1f, Time.unscaledDeltaTime / MenuFlow.SlideSeconds);
                _group.alpha = MenuVideo.ReduceMotion ? 1f : _fade;
            }
            _slide = Mathf.MoveTowards(_slide, 0f, Time.unscaledDeltaTime / MenuFlow.SlideSeconds);
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
                float amp = _screen == MenuScreenId.Title ? 0.07f : 0.04f;
                Rect uv = _flyover.uvRect;
                uv.x = 0.04f + Mathf.Sin(Time.unscaledTime * 0.12f) * amp;
                uv.y = 0.02f + Mathf.Cos(Time.unscaledTime * 0.09f) * (amp * 0.7f);
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
            if (_screen == MenuScreenId.Title) MenuAttract.Tick();
            MenuReveal.Tick();
            MenuFlow.Watch();
            MenuFlow.PaintLost();
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
                        MenuAudio.Join();
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
                        GoBack();
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
                            GoBack();
                            return;
                        }
                        if (edge.Join || edge.Confirm || edge.Start)
                        {
                            if (CouchPlay.Join(edge.Device))
                            {
                                int slot = SeatOf(edge.Device);
                                if (slot >= 0 && LocalProfiles.SeatName(slot) == null)
                                    LocalProfiles.SeatGuest(slot);
                                if (slot >= 0) MenuSession.PullLook(slot);
                                MenuAudio.Join();
                                RefreshCast();
                            }
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
                        MenuTile picked = TileAt(MenuSession.Cursor[seat]);
                        if (picked != null) picked.PunchIn();
                        if (MenuSession.Ready[seat]) MenuAudio.Ready();
                        else MenuAudio.Back();
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
                            GoBack();
                            return;
                        }
                    }
                }
            }
            if (MenuReady.Advance(MenuSession.AllReady(), _banner))
            {
                MenuAudio.Confirm();
                MenuSession.CommitLooks();
                ShowRules();
                return;
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
                GoBack();
                return;
            }
            if (dy != 0) MoveRules(dy);
            if (dx != 0) StepRules(dx);
            if (confirm || start) ArmActivate();
        }

        void TickArena()
        {
            if (Gated()) return;
            ReadNav(out int dx, out int dy, out bool confirm, out bool back, out bool start);
            if (back)
            {
                MenuAudio.Back();
                GoBack();
                return;
            }
            if (dx != 0 || dy != 0)
            {
                Move(dx, dy);
                ShowArenaPreview();
            }
            if (confirm || start) ArmActivate();
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
            GoBack();
        }

        void TickPause()
        {
            if (Gated()) return;
            ReadNav(out int dx, out int dy, out bool confirm, out bool back, out bool start);
            if (start || back)
            {
                EatPause = true;
                if (MenuFlow.Disconnected) return;
                ResumeMatch();
                return;
            }
            if (dx != 0 || dy != 0) Move(dx, dy);
            if (confirm) ArmActivate();
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
                GoBack();
                return;
            }
            if (dy != 0) MoveOptions(dy);
            if (dx != 0) StepOptions(dx);
            if (confirm) ArmActivate();
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
                GoBack();
                return;
            }
            if (dy != 0) MoveOptions(dy);
            if (confirm) ArmActivate();
            if (dx != 0 && MenuStick.Nudge(_focus - (int)PlayAction.Count, dx))
            {
                MenuAudio.Move();
                UnlockLooks();
                PaintControls();
            }
        }

        void TickPractice()
        {
            if (Gated()) return;
            ReadNav(out int dx, out int dy, out bool confirm, out bool back, out bool start);
            if (back)
            {
                MenuAudio.Back();
                PracticeSession.Stop();
                GoBack();
                return;
            }
            if (dy != 0) Move(0, dy);
            if (dx != 0 && _focus <= 4)
            {
                PracticeSession.StepRow(_focus, dx);
                MenuAudio.Move();
                PaintPractice();
            }
            if (confirm || start) ArmActivate();
        }

        void BuildTitle()
        {
            _count = 0;
            if (_header != null) _header.text = "";
            if (_banner != null) _banner.text = "";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            ShowFlyover(MenuSession.Arena, 0.9f);
            var plate = MenuWidgets.Place(_body, "LogoPlate", 360f, 40f, 1100f, 280f);
            var plateImage = plate.gameObject.AddComponent<Image>();
            MenuArt.Plate(plateImage, new Color(0.05f, 0.12f, 0.32f, 0.55f), true);
            plateImage.raycastTarget = false;
            RectTransform logo = MenuWidgets.Logo(_body, 410f, 50f, 1000f, 240f, 150);
            Text sub = MenuWidgets.Words(_body, "COUCH TAG", MenuTokens.Title, TextAnchor.MiddleCenter, MenuTheme.Cream, new Vector2(0.2f, 0.22f), new Vector2(0.8f, 0.36f));
            sub.alignment = TextAnchor.MiddleCenter;
            _press = MenuWidgets.Words(_body, "Press  Start   /   South   /   Space", MenuTokens.Section, TextAnchor.MiddleCenter, MenuTheme.Cream, new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.22f));
            MenuAttract.Bind(logo, _press);
        }

        void BuildMain()
        {
            _count = 0;
            _cols = 1;
            if (_header != null) _header.text = "  TAG";
            if (_banner != null) _banner.text = "";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            MenuWidgets.Logo(_body, 24f, 24f, 860f, 220f, 120);
            MenuWidgets.Words(_body, "Local couch. One keyboard, four pads.", 28, TextAnchor.UpperLeft, MenuTheme.Cream, new Vector2(0f, 0.22f), new Vector2(0.46f, 0.42f));
            float y = 8f;
            AddTile(980f, y, 760f, 96f, 0, "Play", "Local couch", true); y += 108f;
            AddTile(980f, y, 760f, 96f, 1, "Practice", "Free arena, routes you already have", true); y += 108f;
            AddTile(980f, y, 760f, 96f, 2, "Options", "Sound, picture, access", true); y += 108f;
            AddTile(980f, y, 760f, 96f, 3, "Controls", "Binds. Space still jumps.", true); y += 108f;
            AddTile(980f, y, 360f, 96f, 4, "Credits", "", true);
            AddTile(1380f, y, 360f, 96f, 5, "Quit", "", true); y += 108f;
            AddTile(980f, y, 760f, 80f, 6, "Online", "Coming soon", false);
            MenuWidgets.Mark(TileAt(0), MenuIcons.Play, MenuIcons.PlayTint, 72f);
            MenuWidgets.Mark(TileAt(1), MenuIcons.Cone, MenuIcons.PracticeTint, 72f);
            MenuWidgets.Mark(TileAt(2), MenuIcons.Gear, MenuIcons.OptionsTint, 72f);
            MenuWidgets.Mark(TileAt(3), MenuIcons.Pad, MenuIcons.ControlsTint, 72f);
            MenuWidgets.Mark(TileAt(4), MenuIcons.Star, MenuIcons.CreditsTint, 64f);
            MenuWidgets.Mark(TileAt(5), MenuIcons.Door, MenuIcons.QuitTint, 64f);
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
                bool pad = human && CouchPlay.DeviceOf(s) != CouchPlay.DeviceKeyboard;
                MenuWidgets.Glyph(tile, human ? (pad ? MenuIcons.Pad : MenuIcons.Keys) : MenuIcons.Either, MenuTheme.Seat(s));
                if (tile != null)
                {
                    tile.KeepBar = true;
                    tile.BarColor = MenuTheme.Seat(s);
                    if (tile.Bar != null) tile.Bar.color = tile.BarColor;
                    tile.Tint(Color.Lerp(MenuTheme.Ink, MenuTheme.Seat(s), human ? 0.72f : 0.55f));
                }
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
            for (int s = 0; s < 4; s++)
            {
                float x = 16f + s * 448f;
                var card = MenuWidgets.Place(_body, "Cast" + s.ToString(), x, 8f, 428f, 420f);
                var plate = card.gameObject.AddComponent<Image>();
                MenuArt.Plate(plate, MenuTheme.Seat(s), true);
                plate.raycastTarget = false;
                _castPlate[s] = plate;
                var viewRt = MenuWidgets.Place(card, "View", 16f, 16f, 396f, 280f);
                var raw = viewRt.gameObject.AddComponent<RawImage>();
                raw.raycastTarget = false;
                _castView[s] = raw;
                _castJoin[s] = MenuWidgets.Words(card, "Press A / Space to join", 32, TextAnchor.MiddleCenter, MenuTheme.Ink, new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.78f));
                Outline joinEdge = _castJoin[s].GetComponent<Outline>();
                if (joinEdge != null) joinEdge.effectColor = new Color(1f, 0.98f, 0.92f, 0.95f);
                _castReady[s] = MenuWidgets.Words(card, "", 22, TextAnchor.MiddleLeft, MenuTheme.Cream, new Vector2(0f, 0f), new Vector2(1f, 0.28f));
                _readyStamp[s] = MenuWidgets.Words(card, "READY!", 48, TextAnchor.MiddleCenter, MenuTheme.Gold, new Vector2(0.12f, 0.30f), new Vector2(0.88f, 0.62f));
                _readyStamp[s].rectTransform.localRotation = Quaternion.Euler(0f, 0f, -14f);
                _readyStamp[s].gameObject.SetActive(false);
            }
            for (int c = 0; c < 6; c++)
            {
                int col = c % 3;
                int row = c / 3;
                float x = 140f + col * 540f;
                float y = 446f + row * 172f;
                string name = c < LocalProfiles.HierNames.Length ? LocalProfiles.HierNames[c] : "Color";
                AddTile(x, y, 500f, 160f, c, name.ToUpperInvariant(), "", true);
                MenuTile tile = TileAt(c);
                if (tile != null)
                {
                    MenuWidgets.Portrait(tile, MenuPortraits.Of(c), MenuPortraits.Tint(c));
                    _castMark[c] = tile.Detail;
                }
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
            ShowHow();
            RefreshFocus();
        }

        void ShowHow()
        {
            TagModeId id = MenuSession.Mode;
            if (_focus >= 0 && _focus <= 3) id = (TagModeId)_focus;
            MenuHowTo.Show(_body, id);
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
                AddTile(24f, 12f + i * 156f, 760f, 144f, i, ParkArena.NameOf(i), MenuArenaCard.Blurb(i), true);
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
            _arenaShotName = MenuWidgets.Words(shot, "", MenuTokens.Display, TextAnchor.MiddleLeft, MenuTheme.Gold, new Vector2(0f, 0.08f), new Vector2(1f, 0.28f));
            _arenaShotBlurb = MenuWidgets.Words(shot, "", MenuTokens.Body, TextAnchor.UpperLeft, MenuTheme.Cream, new Vector2(0f, 0f), new Vector2(1f, 0.14f));
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
            {
                string size = MenuArenaCard.Size(shown);
                _arenaShotBlurb.text = random ? size + "\nOne of the three parks." : size + "\n" + MenuArenaCard.Flavor(shown);
            }
            MenuArenaCard.Paint(_arenaShot != null ? _arenaShot.transform.parent : null, shown);
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
            _count = MenuSplitPause.Items;
            _cols = 1;
            _focus = 0;
            if (_dim != null) _dim.color = MenuTheme.Dim;
            int opener = 0;
            if (GameSettings.Current != null) opener = GameSettings.Current.AccessSeat;
            bool preview = MenuSplitPause.Preview > 0;
            string who = MenuSplitPause.SeatLabel(opener, preview);
            if (_header != null) _header.text = "  Paused";
            if (_banner != null) _banner.text = "Paused by " + who;
            int n = MenuSplitPause.Fill(_cards);
            for (int c = 0; c < n; c++)
            {
                MenuSplitPause.Card card = _cards[c];
                if (!card.Show) continue;
                var plate = MenuWidgets.Place(_body, "PauseCard", card.X + 12f, card.Y + 8f, card.W - 24f, card.H - 16f);
                var plateImage = plate.gameObject.AddComponent<Image>();
                MenuArt.Plate(plateImage, new Color(0.05f, 0.12f, 0.32f, 0.78f), true);
                plateImage.raycastTarget = false;
                string seatName = MenuSplitPause.SeatLabel(card.Seat, preview);
                var nameRt = MenuWidgets.Place(_body, "PauseName", card.X + 28f, card.Y + 16f, card.W - 56f, 48f);
                Text name = MenuWidgets.Words(nameRt, seatName, 32, TextAnchor.MiddleCenter, MenuTheme.Seat(card.Seat), Vector2.zero, Vector2.one);
                name.alignment = TextAnchor.MiddleCenter;
                float labelH = 56f;
                float gap = 10f;
                float avail = card.H - labelH - 36f;
                float bh = (avail - gap * (MenuSplitPause.Items - 1)) / MenuSplitPause.Items;
                if (bh > 100f) bh = 100f;
                if (bh < 58f) bh = 58f;
                float stack = MenuSplitPause.Items * bh + (MenuSplitPause.Items - 1) * gap;
                float bw = card.W - 72f;
                if (bw > 760f) bw = 760f;
                if (bw < 240f) bw = card.W - 36f;
                float bx = card.X + (card.W - bw) * 0.5f;
                float by = card.Y + labelH + (avail - stack) * 0.5f;
                if (by < card.Y + labelH) by = card.Y + labelH;
                for (int i = 0; i < MenuSplitPause.Items; i++)
                    AddTile(bx, by + i * (bh + gap), bw, bh, i, MenuSplitPause.Item[i], MenuSplitPause.Blurb[i], true);
            }
            _count = MenuSplitPause.Items;
            _cols = 1;
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
            var stage = MenuWidgets.Place(_body, "ResultsView", 80f, 8f, 1680f, 460f);
            var frame = stage.gameObject.AddComponent<Image>();
            MenuArt.Plate(frame, MenuTheme.Navy, true);
            frame.raycastTarget = false;
            var viewRt = MenuWidgets.Place(stage, "View", 16f, 16f, 1648f, 428f);
            var view = viewRt.gameObject.AddComponent<RawImage>();
            view.raycastTarget = false;
            if (_preview != null) _preview.ShowPodium(n, _rows, view);
            Text results = MenuWidgets.Words(stage, "RESULTS", MenuTokens.Title, TextAnchor.MiddleCenter, MenuTheme.Gold, new Vector2(0f, 0.84f), new Vector2(1f, 1f));
            results.alignment = TextAnchor.MiddleCenter;
            for (int rank = 0; rank < n; rank++)
            {
                string detail = MenuPodium.Stats(_rows[rank]);
                int seat = _rows[rank].Seat;
                if (seat < 0) seat = rank;
                MenuTile tile = AddTile(16f + rank * 452f, 470f, 436f, 136f, 20 + rank, MenuTheme.Place(rank) + "  " + _rows[rank].Name, detail, false);
                if (tile != null)
                {
                    tile.KeepBar = true;
                    tile.BarColor = MenuTheme.Seat(seat);
                    if (tile.Bar != null) tile.Bar.color = tile.BarColor;
                    tile.Tint(Color.Lerp(MenuTheme.Ink, MenuTheme.Seat(seat), 0.62f));
                    if (tile.Stroke != null && _rows[rank].Winner)
                        tile.Stroke.color = MenuTheme.Gold;
                    MenuReveal.Row(tile.transform as RectTransform);
                }
            }
            MenuTile rematch = AddTile(16f, 628f, 436f, 96f, 0, "Rematch", "Same setup", true);
            MenuTile modeBtn = AddTile(468f, 628f, 436f, 96f, 1, "Change mode", "", true);
            MenuTile castBtn = AddTile(920f, 628f, 436f, 96f, 2, "Character select", "", true);
            MenuTile menuBtn = AddTile(1372f, 628f, 436f, 96f, 3, "Main menu", "", true);
            if (rematch != null) MenuReveal.Action(rematch.transform as RectTransform);
            if (modeBtn != null) MenuReveal.Action(modeBtn.transform as RectTransform);
            if (castBtn != null) MenuReveal.Action(castBtn.transform as RectTransform);
            if (menuBtn != null) MenuReveal.Action(menuBtn.transform as RectTransform);
            MenuReveal.Begin();
            _count = 4;
            _cols = 4;
        }

        int FillRanks()
        {
            int n = MenuPodium.Fill(_rows);
            if (n == 0 && MenuCapture.Running)
                n = MenuPodium.Sample(_rows);
            return n;
        }

        void BuildOptions()
        {
            MenuDepth.Reset();
            _cols = 1;
            _window = 0;
            if (_dim != null) _dim.color = MenuTheme.Veil;
            PaintOptions();
        }

        void PaintOptions()
        {
            ClearKeepHeader();
            _count = MenuDepth.Count;
            if (_header != null) _header.text = "  " + MenuDepth.Header();
            if (_banner != null) _banner.text = MenuDepth.Banner();
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
                MenuTile tile = AddTile(360f, 8f + v * 96f, 1120f, 88f, index, MenuDepth.Title(index), MenuDepth.Detail(index), true);
                float meter = MenuDepth.Meter(index);
                if (tile != null && meter >= 0f)
                    PaintMeter(tile.transform, meter);
            }
            MenuDepth.PaintSwatches(_body);
            _count = MenuDepth.Count;
            RefreshFocus();
        }

        static void PaintMeter(Transform tile, float meter)
        {
            if (meter < 0f) meter = 0f;
            if (meter > 1f) meter = 1f;
            var track = MenuWidgets.Place(tile, "Meter", 620f, 52f, 440f, 16f);
            var trackImage = track.gameObject.AddComponent<Image>();
            MenuArt.Plate(trackImage, new Color(0f, 0f, 0f, 0.35f), true);
            trackImage.raycastTarget = false;
            float width = 440f * meter;
            if (width < 10f) width = 10f;
            var fill = MenuWidgets.Place(track, "Fill", 0f, 0f, width, 16f);
            var fillImage = fill.gameObject.AddComponent<Image>();
            MenuArt.Plate(fillImage, MenuTheme.Gold, true);
            fillImage.raycastTarget = false;
        }

        void BuildControls()
        {
            _count = (int)PlayAction.Count + MenuStick.Rows + 2;
            _cols = 1;
            _window = 0;
            if (_header != null) _header.text = "  Controls";
            if (_banner != null) _banner.text = "The list is the current binds. Confirm changes one. Space still jumps.";
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
                int actions = (int)PlayAction.Count;
                int stick = index - actions;
                if (index < actions)
                {
                    var action = (PlayAction)index;
                    title = ActionBinds.Name(action);
                    detail = ActionBinds.Show(binds.Keyboard[index]) + "    /    " + ActionBinds.Show(binds.Gamepad[index]);
                    if (_capturing && index == _captureAction) detail = "Press a key or a button";
                }
                else if (stick >= 0 && stick < MenuStick.Rows)
                {
                    title = MenuStick.Label(stick);
                    detail = MenuStick.Detail(stick);
                }
                else if (index == actions + MenuStick.Rows)
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
            if (_focus == 6)
            {
                MenuAudio.Error();
                return;
            }
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
                GoBack();
                return;
            }
            StepRules(1);
        }

        void ActivateArena()
        {
            if (_focus >= 4)
            {
                MenuAudio.Back();
                GoBack();
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
                    EatPause = true;
                    MenuAudio.Back();
                    QuitMatch();
                    break;
                default:
                    EatPause = true;
                    if (MenuFlow.Disconnected) return;
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
                    ShowRules();
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
                    GameFlow again = GameFlow.Instance;
                    if (again != null) again.Rematch();
                    break;
            }
        }

        void ActivateOptions()
        {
            int act = MenuDepth.Activate(_focus);
            if (act == MenuDepth.OpenControls)
            {
                MenuAudio.Confirm();
                ShowControls(_pauseChild);
                return;
            }
            if (act == MenuDepth.Leave)
            {
                MenuAudio.Back();
                GoBack();
                return;
            }
            if (act == MenuDepth.Rebuild)
            {
                MenuAudio.Confirm();
                _focus = 0;
                _window = 0;
                PaintOptions();
                Rewind();
                return;
            }
            UnlockLooks();
            MenuAudio.Move();
            PaintOptions();
        }

        void ActivateControls()
        {
            int actions = (int)PlayAction.Count;
            int stick = _focus - actions;
            if (stick >= 0 && stick < MenuStick.Rows)
            {
                if (MenuStick.Nudge(stick, 1))
                {
                    UnlockLooks();
                    MenuAudio.Move();
                }
                else
                    MenuAudio.Back();
                PaintControls();
                return;
            }
            if (_focus == actions + MenuStick.Rows)
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
            if (_focus > actions + MenuStick.Rows)
            {
                MenuAudio.Back();
                GoBack();
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
                GoBack();
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
            ShowHow();
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
                ShowHow();
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
            if (!MenuDepth.Step(_focus, dir)) return;
            UnlockLooks();
            MenuAudio.Move();
            PaintOptions();
        }

        void TakeBind(string token)
        {
            var action = (PlayAction)_captureAction;
            bool pad = IsPad(token);
            if (!pad && action == PlayAction.Jump && !ActionBinds.KnownKeyboard(token))
            {
                _conflict = "That key is not kept for Jump. Space stays jump.";
                _capturing = false;
                MenuAudio.Error();
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
                MenuAudio.Error();
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
                bool human = CouchPlay.HumanAt(s);
                if (_castJoin[s] != null) _castJoin[s].gameObject.SetActive(!human);
                if (_castView[s] != null) _castView[s].gameObject.SetActive(human);
                if (!human)
                {
                    if (_readyStamp[s] != null) _readyStamp[s].gameObject.SetActive(false);
                    if (_castReady[s] != null) _castReady[s].text = "";
                    if (_castPlate[s] != null) _castPlate[s].color = MenuTheme.Seat(s);
                    continue;
                }
                if (_preview != null) _preview.Apply(s, MenuSession.Hier[s], MenuSession.Accent[s], MenuSession.Hat[s], _castView[s]);
                if (_castReady[s] == null) continue;
                string skin = MenuSession.Hier[s] >= 0 && MenuSession.Hier[s] < LocalProfiles.HierNames.Length
                    ? LocalProfiles.HierNames[MenuSession.Hier[s]] : "Tan";
                string trim = MenuSession.Accent[s] >= 0 && MenuSession.Accent[s] < LocalProfiles.HierNames.Length
                    ? LocalProfiles.HierNames[MenuSession.Accent[s]] : skin;
                string ready = MenuSession.Ready[s] ? "READY" : "Not ready";
                string hat = MenuSession.Hat[s] == 0 ? "Hat off" : "Hat on";
                _castReady[s].text = CouchPlay.Name(s) + "   " + skin + " / " + trim + "   " + hat + "   " + ready;
                _castReady[s].color = MenuTheme.Ink;
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
                    _castPlate[s].color = MenuTheme.Seat(s);
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

        static Sprite FooterIcon(bool pad, int index)
        {
            if (pad)
            {
                if (index == 1) return MenuIcons.South;
                if (index == 2) return MenuIcons.East;
                return MenuIcons.Stick;
            }
            if (index == 1) return MenuIcons.KeySpace;
            if (index == 2) return MenuIcons.KeyEsc;
            return MenuIcons.KeyArrows;
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
