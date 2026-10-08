using System.Collections.Generic;
using Tag.Core;
using Tag.Couch;
using Tag.Experimental;
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
        Practice = 13,
        Records = 14
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
        CanvasScaler _scaler;
        float _uiScale = -1f;
        Image _dim;
        RectTransform _body;
        Text _header;
        Text _footer;
        Text _banner;
        Image _bannerPlate;
        Image _bannerSpace;
        Image _bannerStart;
        Image _startGlyph;
        CanvasGroup _startPrompt;
        Text _promptWord;
        Text _promptTail;
        GameObject _vignette;
        RawImage _parade;
        CanvasGroup _group;
        MenuPreview _preview;
        readonly List<MenuTile> _tiles = new List<MenuTile>(16);
        readonly RectTransform[] _ribbons = new RectTransform[7];
        readonly RectTransform[] _orbs = new RectTransform[4];
        float _bannerPunch;

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
        Image _loadFill;
        Text _loadWord;
        readonly Text[] _loadTip = new Text[4];
        readonly Image[] _loadBar = new Image[4];
        // Unity uv origin is the bottom left. Left yard, chase yard, right yard, close path.
            static readonly float[] LoadCamX = { 0.00f, 0.16f, 0.52f, 0.26f };
            static readonly float[] LoadCamY = { 0.22f, 0.22f, 0.22f, 0.22f };
            static readonly float[] LoadCamW = { 0.50f, 0.48f, 0.48f, 0.50f };
            static readonly float[] LoadCamH = { 0.40f, 0.44f, 0.40f, 0.44f };
        int _loadStep = -1;
        int _tipBase;
        int _tipSpin = int.MinValue;
        bool _capturing;
        bool _captureGrapple;
        int _captureAction = -1;
        int _captureFrame = -1;
        float _captureUntil;
        string _notice = "";
        int _bindSeat;
        int _barChip;
        bool _resetArmed;
        int _swapAction = -1;
        int _swapOther = -1;
        int _swapGrapple;
        bool _swapPad;
        string _swapToken = "";
        int _swapPick;
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
        readonly Image[] _glyphMate = new Image[3];
        readonly Text[] _glyphWord = new Text[3];
        float _actAt;
        readonly RectTransform[] _readyBurst = new RectTransform[4];
        readonly float[] _readyPop = new float[4];
        readonly Image[] _castGlyph = new Image[4];
        readonly Image[] _castWell = new Image[4];
        readonly RectTransform[] _swatch = new RectTransform[24];
        readonly RectTransform[] _swatchRing = new RectTransform[4];
        readonly Text[] _castJoin = new Text[4];
        readonly Image[] _castPlate = new Image[4];
        readonly MenuPodium.Row[] _rows = new MenuPodium.Row[4];
        readonly MenuSplitPause.Card[] _cards = new MenuSplitPause.Card[4];
        Image _lostPlate;
        Text _lostWho;

        readonly Text[] _castMark = new Text[6];
        readonly RawImage[] _castView = new RawImage[4];
        readonly Text[] _castName = new Text[4];
        readonly Text[] _castReady = new Text[4];
        readonly Text[] _loadRule = new Text[8];
        readonly Text[] _keyWord = new Text[40];
        readonly Image[] _keyPlate = new Image[40];
        RectTransform _keys;
        Text _nameWord;
        int _naming = -1;

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
            ApplyScale();
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
                case MenuScreenId.Records: TickRecords(); break;
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
            _naming = -1;
            _gate = Time.unscaledTime + (MenuVideo.ReduceMotion ? 0.05f : MenuFlow.SlideSeconds);
            _fade = MenuVideo.ReduceMotion ? 1f : 0f;
            _slide = MenuVideo.ReduceMotion ? 0f : 1f;
            HideFlyover();
            if (_group != null) _group.alpha = _fade;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ClearBody();
            FitHeader();
            if (_bannerPlate != null) _bannerPlate.enabled = false;
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
                case MenuScreenId.Records: BuildRecords(); break;
            }
            if (_bannerPlate != null)
                _bannerPlate.enabled = MenuSheet.WantsPark((int)id) && _banner != null && _banner.text.Length > 0;
            RefreshFocus();
            PaintFooter();
            SyncStartMarks();
            bool title = id == MenuScreenId.Title;
            bool photo = title || id == MenuScreenId.Main;
            bool loading = id == MenuScreenId.Loading;
            if (_vignette != null) _vignette.SetActive(photo);
            if (_pattern != null)
            {
                Color wash = _pattern.color;
                wash.a = photo || loading ? 0f : 0.22f;
                _pattern.color = wash;
            }
            for (int i = 0; i < _ribbons.Length; i++)
            {
                if (_ribbons[i] != null) _ribbons[i].gameObject.SetActive(!title && !loading);
            }
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i] != null) _orbs[i].gameObject.SetActive(!loading);
            }
            if (id == MenuScreenId.Cast)
            {
                if (_preview != null) _preview.Show();
            }
            else if (title)
            {
                if (_preview != null) _preview.ShowParade(_parade);
            }
            else if (id != MenuScreenId.Results && _preview != null)
                _preview.Hide();
            if (id == MenuScreenId.Results)
                MenuAudio.Results();
            if (id != MenuScreenId.Hidden)
                MenuAudio.EnsureBed();
            WashSecondary(id);
            if (MenuSheet.Wipes((int)id) && _canvas != null)
                MenuWipe.Play(_canvas.transform as RectTransform);
        }

        void WashSecondary(MenuScreenId id)
        {
            int screen = (int)id;
            if (!MenuSheet.WantsPark(screen)) return;
            if (_flyover != null && _flyover.color.a > 0.2f) return;
            int arena = MenuSession.Arena;
            if (arena < 0 || arena >= ParkArena.Count) arena = ParkArena.Mega;
            ShowFlyover(arena, MenuSheet.ParkAlpha(screen));
            if (_vignette != null && id != MenuScreenId.Pause)
                _vignette.SetActive(true);
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
            _scaler = canvasGo.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(UiFit.RefW, UiFit.RefH);
            _scaler.matchWidthOrHeight = 0.5f;
            _uiScale = -1f;
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
            BuildVignette(root);

            var sweepRt = MenuWidgets.Place(root, "Sweep", -1920f, 0f, 280f, 1080f);
            _sweep = sweepRt.gameObject.AddComponent<Image>();
            MenuArt.Plate(_sweep, new Color(1f, 0.84f, 0.12f, 0.28f), true);
            _sweep.raycastTarget = false;

            var back = MenuWidgets.Box(root, "Back", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            BuildRibbons(back);
            BuildOrbs(back);

            _header = MenuWidgets.Heading(root, "TAG", 42, TextAnchor.MiddleLeft, MenuTheme.Gold, new Vector2(0f, 1f), new Vector2(1f, 1f));
            RectTransform headerRt = _header.rectTransform;
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0f, 96f);
            headerRt.anchoredPosition = Vector2.zero;

            _body = MenuWidgets.Box(root, "Body", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            _body.offsetMin = new Vector2(UiFit.SafeX, 78f);
            _body.offsetMax = new Vector2(-UiFit.SafeX, -108f);

            var bannerBack = MenuWidgets.Box(root, "BannerPlate", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
            _bannerPlate = bannerBack.gameObject.AddComponent<Image>();
            MenuArt.Plate(_bannerPlate, MenuTheme.Navy, true);
            _bannerPlate.raycastTarget = false;
            _bannerPlate.enabled = false;
            RectTransform plateRt = _bannerPlate.rectTransform;
            plateRt.sizeDelta = new Vector2(-120f, 44f);
            plateRt.anchoredPosition = new Vector2(0f, 66f);
            _banner = MenuWidgets.Words(root, "", UiFit.FloorFont, TextAnchor.MiddleCenter, MenuTheme.Gold, new Vector2(0f, 0f), new Vector2(1f, 0f));
            RectTransform bannerRt = _banner.rectTransform;
            bannerRt.anchorMin = new Vector2(0f, 0f);
            bannerRt.anchorMax = new Vector2(1f, 0f);
            bannerRt.pivot = new Vector2(0.5f, 0f);
            bannerRt.sizeDelta = new Vector2(0f, 48f);
            bannerRt.anchoredPosition = new Vector2(0f, 64f);

            _footer = MenuWidgets.Words(root, "", UiFit.FloorFont, TextAnchor.MiddleCenter, MenuTheme.Cream, new Vector2(0f, 0f), new Vector2(1f, 0f));
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
            FitHeader();
            _canvas.enabled = false;
        }

        void FitHeader()
        {
            if (_header == null) return;
            RectTransform headerRt = _header.rectTransform;
            if (UiFit.IdentityText())
            {
                _header.resizeTextForBestFit = true;
                _header.fontSize = 42;
                _header.resizeTextMinSize = UiFit.FloorFont;
                _header.resizeTextMaxSize = 42;
                headerRt.sizeDelta = new Vector2(0f, 96f);
                return;
            }
            int px = UiFit.TextPx(42);
            _header.resizeTextForBestFit = false;
            _header.fontSize = px;
            _header.resizeTextMinSize = px;
            _header.resizeTextMaxSize = px;
            float band = 28f + px;
            if (band < 96f) band = 96f;
            headerRt.sizeDelta = new Vector2(0f, band);
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
            MenuWidgets.Words(plate, "Plug that pad back in. Resume waits.", UiFit.FloorFont, TextAnchor.MiddleCenter, MenuTheme.Mute, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.34f));
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

        void BuildOrbs(RectTransform parent)
        {
            Color[] colors =
            {
                new Color(0.95f, 0.22f, 0.28f, 0.10f),
                new Color(0.20f, 0.48f, 1f, 0.09f),
                new Color(1f, 0.82f, 0.16f, 0.08f),
                new Color(0.18f, 0.82f, 0.36f, 0.08f)
            };
            float[] x = { 80f, 980f, 240f, 1280f };
            float[] y = { 80f, 160f, 620f, 540f };
            for (int i = 0; i < _orbs.Length; i++)
            {
                var rt = MenuWidgets.Place(parent, "Orb" + i.ToString(), x[i], y[i], 460f, 460f);
                var image = rt.gameObject.AddComponent<Image>();
                image.sprite = MenuArt.Soft;
                image.color = colors[i];
                image.raycastTarget = false;
                _orbs[i] = rt;
            }
        }

        void ClearBody()
        {
            _tiles.Clear();
            _startGlyph = null;
            _startPrompt = null;
            _promptWord = null;
            _promptTail = null;
            _parade = null;
            for (int i = 0; i < _castMark.Length; i++) _castMark[i] = null;
            for (int i = 0; i < _castView.Length; i++)
            {
                _castView[i] = null;
                _castName[i] = null;
                _castReady[i] = null;
                _castJoin[i] = null;
                _castPlate[i] = null;
                _readyBurst[i] = null;
                _readyPop[i] = 0f;
                _castGlyph[i] = null;
                _castWell[i] = null;
                _swatchRing[i] = null;
            }
            for (int i = 0; i < _swatch.Length; i++) _swatch[i] = null;
            for (int i = 0; i < _loadRule.Length; i++) _loadRule[i] = null;
            for (int i = 0; i < _loadTip.Length; i++)
            {
                _loadTip[i] = null;
                _loadBar[i] = null;
            }
            for (int i = 0; i < _keyWord.Length; i++)
            {
                _keyWord[i] = null;
                _keyPlate[i] = null;
            }
            _keys = null;
            _nameWord = null;
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
                case MenuScreenId.Records: ActivateRecords(); break;
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
            _notice = "";
            _resetArmed = false;
            ClearSwap();
            Open(MenuScreenId.Controls);
        }

        void BeginLoading(bool practice)
        {
            _loadPractice = practice;
            _loadFired = false;
            _loadAt = Time.unscaledTime + 0.9f;
            Tag.Core.GameFlow flow = Tag.Core.GameFlow.Instance;
            if (flow != null) flow.ClearBootLoad();
            MenuAudio.StartMatch();
            Open(MenuScreenId.Loading);
        }

        void BuildGlyphs(RectTransform root)
        {
            string[] words = { "Move", "Confirm", "Back" };
            for (int i = 0; i < 3; i++)
            {
                var chip = MenuWidgets.Place(root, "Glyph" + i.ToString(), 0f, 0f, 420f, 64f);
                var image = chip.gameObject.AddComponent<Image>();
                MenuArt.Plate(image, MenuTheme.Navy, true);
                image.raycastTarget = false;
                _glyphChip[i] = image;
                var iconRt = MenuWidgets.Place(chip, "Icon", 8f, 14f, 36f, 36f);
                var icon = iconRt.gameObject.AddComponent<Image>();
                icon.sprite = MenuIcons.Keys;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                _glyphIcon[i] = icon;
                var mateRt = MenuWidgets.Place(chip, "Mate", 48f, 14f, 36f, 36f);
                var mate = mateRt.gameObject.AddComponent<Image>();
                mate.sprite = MenuIcons.Pad;
                mate.preserveAspect = true;
                mate.raycastTarget = false;
                _glyphMate[i] = mate;
                _glyphWord[i] = MenuWidgets.Words(chip, words[i], UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Cream, Vector2.zero, Vector2.one);
                _glyphWord[i].rectTransform.offsetMin = new Vector2(92f, 4f);
            }
            PlaceGlyphs();
        }

        void PlaceGlyphs()
        {
            UiFit.Ref(UiFit.Current(), out float rw, out float rh);
            float gap = 16f;
            float h = 64f;
            float y = rh - UiFit.SafeY - h;
            float inner = rw - UiFit.SafeX * 2f;
            float w = (inner - gap * 2f) / 3f;
            if (w > 420f) w = 420f;
            if (w < 180f) w = 180f;
            float total = w * 3f + gap * 2f;
            float x0 = (rw - total) * 0.5f;
            for (int i = 0; i < 3; i++)
            {
                if (_glyphChip[i] == null) continue;
                RectTransform rt = _glyphChip[i].rectTransform;
                rt.anchoredPosition = new Vector2(x0 + i * (w + gap), -y);
                rt.sizeDelta = new Vector2(w, h);
            }
        }

        void ApplyScale()
        {
            float s = UiFit.Current();
            if (_scaler == null || s == _uiScale) return;
            _uiScale = s;
            UiFit.Ref(s, out float w, out float h);
            _scaler.referenceResolution = new Vector2(w, h);
            PlaceGlyphs();
        }

        void PaintFooter()
        {
            _footerKind = MenuInput.LastKind;
            if (_footer == null) return;
            int family = PadGlyph.Keyboard;
            int device = MenuInput.LastDevice;
            if (_footerKind == Tag.Onboard.InputDeviceKind.Gamepad)
                family = PadGlyph.Family(device);
            else
                device = CouchPlay.DeviceKeyboard;
            int face = FaceFor(device, family);
            int padFamily = family == PadGlyph.Keyboard ? PadGlyph.Xbox : family;
            int padFace = family == PadGlyph.Keyboard ? FaceMap.DefaultOf(PadGlyph.Xbox) : face;
            _footer.text = "";
            bool hints = _screen != MenuScreenId.Title && _screen != MenuScreenId.Loading;
            Color chip = new Color(0.08f, 0.16f, 0.36f, 1f);
            for (int i = 0; i < 3; i++)
            {
                if (_glyphWord[i] != null)
                {
                    _glyphWord[i].enabled = hints;
                    if (hints) _glyphWord[i].text = FooterLine(i, padFamily, padFace);
                }
                if (_glyphChip[i] != null)
                {
                    _glyphChip[i].enabled = hints;
                    if (hints) _glyphChip[i].color = chip;
                }
                if (_glyphIcon[i] != null)
                {
                    _glyphIcon[i].enabled = hints;
                    if (hints) _glyphIcon[i].sprite = MenuIcons.Slot(PadGlyph.Keyboard, i, FaceMap.DefaultOf(PadGlyph.Keyboard));
                }
                if (_glyphMate[i] != null)
                {
                    _glyphMate[i].enabled = hints;
                    if (hints) _glyphMate[i].sprite = MenuIcons.Slot(padFamily, i, padFace);
                }
            }
            bool keyboard = family == PadGlyph.Keyboard;
            if (_startGlyph != null && !keyboard)
            {
                _startGlyph.sprite = MenuIcons.Slot(family, 1, face);
            }
            LayoutPrompt(keyboard);
        }

        void LayoutPrompt(bool keyboard)
        {
            if (_promptWord == null) return;
            RectTransform word = _promptWord.rectTransform;
            word.anchorMin = new Vector2(0.5f, 0.5f);
            word.anchorMax = new Vector2(0.5f, 0.5f);
            if (_promptTail != null) _promptTail.gameObject.SetActive(false);
            if (keyboard)
            {
                if (_promptWord.text != "PRESS START") _promptWord.text = "PRESS START";
                _promptWord.alignment = TextAnchor.MiddleCenter;
                word.pivot = new Vector2(0.5f, 0.5f);
                word.sizeDelta = new Vector2(720f, 110f);
                word.anchoredPosition = Vector2.zero;
                if (_startGlyph != null) _startGlyph.enabled = false;
                return;
            }
            if (_promptWord.text != "PRESS") _promptWord.text = "PRESS";
            _promptWord.alignment = TextAnchor.MiddleRight;
            word.pivot = new Vector2(1f, 0.5f);
            word.sizeDelta = new Vector2(320f, 110f);
            word.anchoredPosition = new Vector2(-12f, 0f);
            if (_startGlyph == null) return;
            _startGlyph.enabled = true;
            RectTransform glyph = _startGlyph.rectTransform;
            glyph.anchorMin = new Vector2(0.5f, 0.5f);
            glyph.anchorMax = new Vector2(0.5f, 0.5f);
            glyph.pivot = new Vector2(0f, 0.5f);
            glyph.sizeDelta = new Vector2(92f, 92f);
            glyph.anchoredPosition = new Vector2(12f, 0f);
        }

        static void Snug(Text label)
        {
            if (label == null) return;
            RectTransform rt = label.rectTransform;
            rt.offsetMin = new Vector2(8f, 2f);
            rt.offsetMax = new Vector2(-8f, -2f);
        }

        static void LockFit(Text label, int size)
        {
            if (label == null || UiFit.IdentityText()) return;
            int px = UiFit.TextPx(size);
            label.resizeTextForBestFit = false;
            label.fontSize = px;
            label.resizeTextMinSize = px;
            label.resizeTextMaxSize = px;
        }

        static void Pull(Text label, float x)
        {
            if (label == null) return;
            RectTransform rt = label.rectTransform;
            Vector2 min = rt.offsetMin;
            if (min.x < x) min.x = x;
            rt.offsetMin = min;
        }

        static void SeatBadge(Transform parent, float x, float y, int seat)
        {
            if (parent == null) return;
            if (seat < 0) seat = 0;
            if (seat > 3) seat = 3;
            Color seatColor = MenuTheme.Seat(seat);
            float tagW = 108f;
            var rt = MenuWidgets.Place(parent, "SeatTag", x, y, tagW, 40f);
            var plate = rt.gameObject.AddComponent<Image>();
            MenuArt.Plate(plate, new Color(0.02f, 0.02f, 0.04f, 1f), true);
            plate.raycastTarget = false;
            SeatShape.Stamp(rt, seat, 6f, 6f, 28f, MenuTheme.SeatFill(seat));
            Text word = MenuWidgets.Words(rt, "P" + (seat + 1).ToString(), UiFit.FloorFont, TextAnchor.MiddleCenter, MenuTheme.Cream, Vector2.zero, Vector2.one);
            Snug(word);
            LockFit(word, UiFit.FloorFont);
            if (word != null)
            {
                Vector2 min = word.rectTransform.offsetMin;
                if (min.x < 36f) min.x = 36f;
                word.rectTransform.offsetMin = min;
            }
        }

        static void BadgePaint(Color seat, out Color plate, out Color ink)
        {
            plate = seat;
            ink = BadgeInk(plate);
            if (BadgeContrast(ink, plate) >= 5f) return;
            for (int i = 0; i < 8; i++)
            {
                plate = Color.Lerp(plate, Color.black, 0.12f);
                plate.a = 1f;
                if (BadgeContrast(MenuTheme.Cream, plate) >= 5f)
                {
                    ink = MenuTheme.Cream;
                    return;
                }
            }
            ink = MenuTheme.Cream;
        }

        static Color BadgeInk(Color seat)
        {
            return BadgeContrast(MenuTheme.Ink, seat) >= BadgeContrast(MenuTheme.Cream, seat)
                ? MenuTheme.Ink
                : MenuTheme.Cream;
        }

        static float BadgeContrast(Color a, Color b)
        {
            float la = BadgeLuma(a);
            float lb = BadgeLuma(b);
            float hi = la > lb ? la : lb;
            float lo = la > lb ? lb : la;
            return (hi + 0.05f) / (lo + 0.05f);
        }

        static float BadgeLuma(Color c)
        {
            return 0.2126f * BadgeLin(c.r) + 0.7152f * BadgeLin(c.g) + 0.0722f * BadgeLin(c.b);
        }

        static float BadgeLin(float u)
        {
            if (u <= 0.04045f) return u / 12.92f;
            return Mathf.Pow((u + 0.055f) / 1.055f, 2.4f);
        }

        static void SeatChip(Transform parent, float x, float y, int seat)
        {
            if (parent == null) return;
            if (seat < 0) seat = 0;
            if (seat > 3) seat = 3;
            float tagW = 108f;
            var rt = MenuWidgets.Place(parent, "SeatTag", x, y, tagW, 40f);
            var plate = rt.gameObject.AddComponent<Image>();
            MenuArt.Plate(plate, new Color(0.02f, 0.02f, 0.04f, 1f), true);
            plate.raycastTarget = false;
            SeatShape.Stamp(rt, seat, 6f, 6f, 28f, MenuTheme.SeatFill(seat));
            Text word = MenuWidgets.Words(rt, "P" + (seat + 1).ToString(), UiFit.FloorFont, TextAnchor.MiddleCenter, MenuTheme.Cream, Vector2.zero, Vector2.one);
            Snug(word);
            LockFit(word, UiFit.FloorFont);
            if (word != null)
            {
                Vector2 min = word.rectTransform.offsetMin;
                if (min.x < 36f) min.x = 36f;
                word.rectTransform.offsetMin = min;
            }
        }

        void Animate()
        {
            MenuJuice.Tick(Time.unscaledDeltaTime);
            TickLoadDash();
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
                _body.offsetMin = new Vector2(UiFit.SafeX + slide, 78f);
                _body.offsetMax = new Vector2(-UiFit.SafeX + slide, -108f);
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
            if (_flyover != null && _flyover.color.a > 0.01f && !MenuVideo.ReduceMotion && _screen != MenuScreenId.Loading)
            {
                float amp = _screen == MenuScreenId.Title ? 0.07f : 0.04f;
                Rect uv = _flyover.uvRect;
                uv.x = 0.04f + Mathf.Sin(Time.unscaledTime * 0.12f) * amp;
                uv.y = 0.02f + Mathf.Cos(Time.unscaledTime * 0.09f) * (amp * 0.7f);
                uv.width = 0.92f;
                uv.height = 0.92f;
                _flyover.uvRect = uv;
            }
            if (_banner != null)
            {
                float punch = 1f;
                if (_bannerPunch > 0f && !MenuVideo.ReduceMotion)
                {
                    _bannerPunch -= Time.unscaledDeltaTime / 0.28f;
                    if (_bannerPunch < 0f) _bannerPunch = 0f;
                    float u = 1f - _bannerPunch;
                    punch = u < 0.28f ? Mathf.Lerp(1.22f, 1f, u / 0.28f) : 1f;
                }
                _banner.rectTransform.localScale = new Vector3(punch, punch, 1f);
            }
            for (int i = 0; i < _readyBurst.Length; i++)
            {
                RectTransform burst = _readyBurst[i];
                if (burst == null || !burst.gameObject.activeSelf) continue;
                float pop = 1.08f;
                if (!MenuVideo.ReduceMotion && _readyPop[i] > 0f)
                {
                    _readyPop[i] -= Time.unscaledDeltaTime / 0.32f;
                    if (_readyPop[i] < 0f) _readyPop[i] = 0f;
                    float u = 1f - _readyPop[i];
                    float e = u * u * (3f - 2f * u);
                    pop = 1.45f + (1.08f - 1.45f) * e;
                }
                float wobble = MenuVideo.ReduceMotion ? 1f : 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 7.5f + i);
                float burstScale = pop * wobble;
                burst.localScale = new Vector3(burstScale, burstScale, 1f);
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
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i] == null) continue;
                float ox = i == 1 ? 980f : (i == 2 ? 240f : (i == 3 ? 1280f : 80f));
                float oy = i == 1 ? 160f : (i == 2 ? 620f : (i == 3 ? 540f : 80f));
                Vector2 o = _orbs[i].anchoredPosition;
                o.x = ox + Mathf.Sin(t * 0.07f + i * 1.4f) * 70f;
                o.y = -oy + Mathf.Cos(t * 0.05f + i) * 36f;
                _orbs[i].anchoredPosition = o;
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
                bool gone = seated && CouchPlay.InputBlockedDevice(edge.Device);
                if ((!seated || gone) && (edge.Join || edge.Confirm || edge.Start))
                {
                    OfferSeat(edge.Device, false);
                    continue;
                }
                if (edge.North && seated)
                {
                    int readySeat = SeatOf(edge.Device);
                    if (readySeat >= 0)
                    {
                        MenuSession.Ready[readySeat] = !MenuSession.Ready[readySeat];
                        if (MenuSession.Ready[readySeat]) MenuAudio.Ready(readySeat);
                        else MenuAudio.Back();
                    }
                }
                if (edge.Back)
                {
                    if (seated)
                    {
                        int leaving = SeatOf(edge.Device);
                        if (leaving >= 0) MenuSession.Ready[leaving] = false;
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
                            if (OfferSeat(edge.Device, true))
                                RefreshCast();
                        }
                        continue;
                    }
                    if (_naming == seat)
                    {
                        TickName(seat, edge);
                        continue;
                    }
                    if (_naming >= 0) continue;
                    bool moved = false;
                    int cursor = MenuSession.Cursor[seat];
                    int x = cursor % 3;
                    int y = cursor / 3;
                    if (edge.X != 0)
                    {
                        x = (x + edge.X + 3) % 3;
                        moved = true;
                    }
                    if (edge.Y < 0 && y == 1)
                    {
                        OpenName(seat);
                        continue;
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
                        if (MenuSession.Ready[seat])
                        {
                            MenuAudio.Ready(seat);
                            _readyPop[seat] = 1f;
                        }
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
            bool ready = _naming < 0 && MenuSession.AllReady();
            if (_naming >= 0) MenuReady.Stop();
            if (ready && !MenuLobby.Enough(MenuSession.Mode, CouchPlay.Humans))
            {
                MenuReady.Stop();
                if (_banner != null) _banner.text = MenuLobby.Short(MenuSession.Mode);
            }
            int digit = MenuReady.Digit;
            if (MenuReady.Advance(ready, _banner))
            {
                MenuAudio.Confirm();
                MenuSession.CommitLooks();
                ShowRules();
                return;
            }
            if (MenuReady.Digit != digit) _bannerPunch = 1f;
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
            bool dropped = SoakSeats();
            ReadNav(out int dx, out int dy, out bool confirm, out bool back, out bool start);
            if (dropped)
            {
                confirm = false;
                start = false;
            }
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
            bool dropped = SoakSeats();
            ReadNav(out int dx, out int dy, out bool confirm, out bool back, out bool start);
            if (dropped)
            {
                confirm = false;
                start = false;
            }
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
            SpinTips();
            PaintLoad();
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
                if (cancel || UnityEngine.Input.GetKeyDown(KeyCode.Escape) || Time.unscaledTime >= _captureUntil)
                {
                    if (_pauseChild) EatPause = true;
                    StopCapture();
                    return;
                }
                if (Time.frameCount <= _captureFrame) return;
                string token = BindSampler.AnyPressedToken();
                if (string.IsNullOrEmpty(token) || token == "escape") return;
                if (token == "buttonEast")
                {
                    StopCapture();
                    return;
                }
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
                if (_swapOther >= 0)
                {
                    CancelSwap();
                    return;
                }
                MenuAudio.Back();
                GoBack();
                return;
            }
            if (dy != 0) MoveOptions(dy);
            if (confirm) ArmActivate();
            if (dx != 0)
            {
                if (_swapOther >= 0)
                {
                    int pick = dx > 0 ? 1 : 0;
                    if (pick != _swapPick)
                    {
                        _swapPick = pick;
                        MenuAudio.Move();
                        PaintControls();
                    }
                    return;
                }
                ControlBands(out _, out _, out int stickAt, out int confirmAt, out int barAt);
                if (_focus == 0)
                {
                    int next = _bindSeat + (dx > 0 ? 1 : -1);
                    if (next < 0) next = 0;
                    if (next > 3) next = 3;
                    if (next != _bindSeat)
                    {
                        _bindSeat = next;
                        _resetArmed = false;
                        MenuAudio.Move();
                        PaintControls();
                    }
                    return;
                }
                if (_focus == barAt)
                {
                    int chip = dx > 0 ? 1 : 0;
                    if (chip != _barChip)
                    {
                        _barChip = chip;
                        _resetArmed = false;
                        MenuAudio.Move();
                        PaintControls();
                    }
                    return;
                }
                int stick = _focus - stickAt;
                if (stick >= 0 && stick < MenuStick.Rows && MenuStick.Nudge(stick, dx))
                {
                    MenuAudio.Move();
                    UnlockLooks();
                    PaintControls();
                }
                else if (_focus >= confirmAt && _focus < barAt)
                {
                    GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
                    GameSettings.Current = s;
                    s.StepConfirm(_focus - confirmAt, dx);
                    UnlockLooks();
                    MenuAudio.Move();
                    PaintControls();
                }
            }
        }

        void StopCapture()
        {
            _capturing = false;
            _captureAction = -1;
            _captureGrapple = false;
            MenuAudio.Back();
            PaintControls();
        }

        void TickRecords()
        {
            if (Gated()) return;
            ReadNav(out int dx, out int dy, out bool confirm, out bool back, out bool start);
            if (back)
            {
                MenuAudio.Back();
                GoBack();
                return;
            }
            if (dy != 0 || dx != 0)
            {
                int next = _focus + (dy > 0 || dx < 0 ? -1 : 1);
                if (dy == 0 && dx == 0) next = _focus;
                if (next < 0) next = 0;
                if (next >= _count) next = _count - 1;
                if (next != _focus)
                {
                    _focus = next;
                    MenuAudio.Move();
                    int span = UiFit.Window(UiFit.Current(), UiFit.RowStep(128f, 120f), 8f);
                    if (_focus < _window || _focus >= _window + span)
                        PaintRecords();
                    else
                        RefreshFocus();
                }
            }
            if (confirm || start) ArmActivate();
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
            if (_dim != null) _dim.color = new Color(0.02f, 0.04f, 0.10f, 0.25f);
            ShowFlyover(ParkArena.Mega, 1f);
            var paradeRt = MenuWidgets.Box(_body, "Parade", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            _parade = paradeRt.gameObject.AddComponent<RawImage>();
            _parade.raycastTarget = false;
            _parade.color = Color.white;
            RectTransform logo = MenuWidgets.Logo(_body, 0f, 0f, 1100f, 280f, 168);
            logo.anchorMin = new Vector2(0.5f, 0.5f);
            logo.anchorMax = new Vector2(0.5f, 0.5f);
            logo.pivot = new Vector2(0.5f, 0.5f);
            logo.sizeDelta = new Vector2(1100f, 280f);
            logo.anchoredPosition = new Vector2(0f, 150f);
            var prompt = MenuWidgets.Box(_body, "StartPrompt", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            prompt.sizeDelta = new Vector2(980f, 128f);
            prompt.anchoredPosition = new Vector2(0f, -250f);
            _startPrompt = prompt.gameObject.AddComponent<CanvasGroup>();
            _startPrompt.blocksRaycasts = false;
            _startPrompt.interactable = false;
            _promptWord = MenuWidgets.Heading(prompt, "PRESS", 72, TextAnchor.MiddleRight, MenuTheme.Cream, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var glyphRt = MenuWidgets.Box(prompt, "StartGlyph", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f));
            _startGlyph = glyphRt.gameObject.AddComponent<Image>();
            _startGlyph.preserveAspect = true;
            _startGlyph.raycastTarget = false;
            int startFamily = MenuInput.LastKind == InputDeviceKind.Gamepad
                ? PadGlyph.Family(MenuInput.LastDevice)
                : PadGlyph.Keyboard;
            int startDevice = MenuInput.LastKind == InputDeviceKind.Gamepad ? MenuInput.LastDevice : CouchPlay.DeviceKeyboard;
            if (startFamily != PadGlyph.Keyboard)
                _startGlyph.sprite = MenuIcons.Slot(startFamily, 1, FaceFor(startDevice, startFamily));
            _promptTail = null;
            LayoutPrompt(startFamily == PadGlyph.Keyboard);
            MenuAttract.Bind(logo, _startPrompt);
        }

        void BuildVignette(RectTransform root)
        {
            var shell = MenuWidgets.Box(root, "Vignette", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            Edge(shell, "Top", new Vector2(0f, 0.86f), Vector2.one);
            Edge(shell, "Bottom", Vector2.zero, new Vector2(1f, 0.16f));
            Edge(shell, "Left", Vector2.zero, new Vector2(0.08f, 1f));
            Edge(shell, "Right", new Vector2(0.92f, 0f), Vector2.one);
            _vignette = shell.gameObject;
            _vignette.SetActive(false);
        }

        static void Edge(RectTransform parent, string name, Vector2 min, Vector2 max)
        {
            var rt = MenuWidgets.Box(parent, name, min, max, new Vector2(0.5f, 0.5f));
            var image = rt.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.55f);
            image.raycastTarget = false;
        }

        bool OfferSeat(int device, bool pull)
        {
            if (CouchPlay.Joined(device) && !CouchPlay.InputBlockedDevice(device))
                return false;
            bool had = CouchPlay.Joined(device);
            bool reclaimed = CouchPlay.Reclaim(device);
            if (!reclaimed && !CouchPlay.Join(device))
                return false;
            if (!reclaimed && !had)
            {
                int slot = SeatOf(device);
                if (slot >= 0 && LocalProfiles.SeatName(slot) == null)
                    LocalProfiles.SeatGuest(slot);
                if (pull && slot >= 0)
                    MenuSession.PullLook(slot);
            }
            MenuAudio.Join();
            return true;
        }

        bool SoakSeats()
        {
            bool any = false;
            for (int i = 0; i < MenuInput.Count; i++)
            {
                MenuEdge edge = MenuInput.Edges[i];
                if (!(edge.Join || edge.Confirm || edge.Start)) continue;
                if (OfferSeat(edge.Device, true)) any = true;
            }
            return any;
        }

        void BuildMain()
        {
            _count = 0;
            _cols = 1;
            if (_header != null) _header.text = "  Menu";
            if (_banner != null) _banner.text = "";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            ShowFlyover(ParkArena.Mega, 0.88f);
            float scale = UiFit.Current();
            UiFit.MainSplit(scale, out float logoW, out float tileX, out float tileW);
            float bodyH = UiFit.BodyH(scale);
            float logoH = bodyH < 720f ? 128f : 168f;
            MenuWidgets.Logo(_body, 8f, 4f, logoW - 20f, logoH, 84);
            var blurb = MenuWidgets.Place(_body, "Blurb", 8f, logoH + 2f, logoW - 28f, 48f);
            MenuWidgets.Words(blurb, "Local couch. One keyboard, four pads.", UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Cream, Vector2.zero, Vector2.one);
            float heroY = logoH + 54f;
            float tipH = 16f + (UiFit.FloorFont + 6f) * 4f;
            float heroH = bodyH - heroY - tipH - 8f;
            if (heroH > 440f) heroH = 440f;
            if (heroH < 88f) heroH = 88f;
            var heroRt = MenuWidgets.Place(_body, "Hero", 8f, heroY, logoW - 28f, heroH);
            var hero = heroRt.gameObject.AddComponent<RawImage>();
            hero.texture = MenuBackdrop.Chase;
            hero.raycastTarget = false;
            hero.color = hero.texture != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            float tipY = heroY + heroH + 6f;
            if (tipY + tipH > bodyH) tipY = bodyH - tipH;
            if (tipY < heroY) tipY = heroY;
            var tipRt = MenuWidgets.Place(_body, "Tip", 8f, tipY, logoW - 28f, tipH);
            var tipPlate = tipRt.gameObject.AddComponent<Image>();
            MenuArt.Plate(tipPlate, MenuTheme.Navy, true);
            tipPlate.raycastTarget = false;
            MenuWidgets.Words(tipRt, "Tip of the day", UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Mute, new Vector2(0.04f, 0.76f), new Vector2(0.96f, 0.98f));
            MenuWidgets.Words(tipRt, MenuTips.At(0), UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Cream, new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.74f));
            MenuWidgets.Words(tipRt, MenuTips.At(2), UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Cream, new Vector2(0.04f, 0.28f), new Vector2(0.96f, 0.50f));
            MenuWidgets.Words(tipRt, MenuTips.At(7), UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Cream, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.26f));
            float rowH = UiFit.RowH(96f);
            float step = UiFit.RowStep(108f, 96f);
            float y = 8f;
            AddTile(tileX, y, tileW, rowH, 0, "Play", "Local couch", true); y += step;
            AddTile(tileX, y, tileW, rowH, 1, "Practice", "Free run any arena, no tagger", true); y += step;
            AddTile(tileX, y, tileW, rowH, 2, "Options", "Sound, picture, access", true); y += step;
            AddTile(tileX, y, tileW, rowH, 3, "Controls", "Binds. Space still jumps.", true); y += step;
            float gap = 12f;
            float btn = (tileW - gap * 2f) / 3f;
            AddTile(tileX, y, btn, rowH, 4, "Credits", "", true);
            AddTile(tileX + btn + gap, y, btn, rowH, 6, "Records", "Profiles", true);
            AddTile(tileX + (btn + gap) * 2f, y, btn, rowH, 5, "Quit", "", true);
            MenuWidgets.Mark(TileAt(0), MenuIcons.Play, MenuIcons.PlayTint, 72f);
            MenuWidgets.Mark(TileAt(1), MenuIcons.Cone, MenuIcons.PracticeTint, 72f);
            MenuWidgets.Mark(TileAt(2), MenuIcons.Gear, MenuIcons.OptionsTint, 72f);
            MenuWidgets.Mark(TileAt(3), MenuIcons.Pad, MenuIcons.ControlsTint, 72f);
            MenuWidgets.Mark(TileAt(4), MenuIcons.Star, MenuIcons.CreditsTint, 48f);
            MenuWidgets.Mark(TileAt(6), MenuIcons.Star, MenuIcons.CreditsTint, 48f);
            MenuWidgets.Mark(TileAt(5), MenuIcons.Door, MenuIcons.QuitTint, 48f);
            _count = 7;
        }

        void BuildJoin()
        {
            _count = 0;
            _cols = 4;
            if (_header != null) _header.text = "  Who's playing";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            _joinSig = JoinSig();
            float span = UiFit.BodyW(UiFit.Current());
            float bodyH = UiFit.BodyH(UiFit.Current());
            float cardH = 420f;
            if (36f + cardH > bodyH) cardH = bodyH - 48f;
            for (int s = 0; s < 4; s++)
            {
                bool human = CouchPlay.HumanAt(s);
                string title = "P" + (s + 1).ToString();
                string detail = human ? "" : "Press a button to join";
                string profile = human ? LocalProfiles.SeatName(s) : "";
                if (human && string.IsNullOrEmpty(profile)) profile = CouchPlay.Name(s);
                int device = human ? CouchPlay.DeviceOf(s) : -1;
                string deviceLine = device <= CouchPlay.DeviceKeyboard ? "Keyboard" : "Gamepad";
                float cardW = (span - 16f * 5f) / 4f;
                if (cardW > 428f) cardW = 428f;
                if (cardW < 180f) cardW = 180f;
                float x = 16f + s * (cardW + 16f);
                AddTile(x, 24f, cardW, cardH, s, title, detail, true);
                MenuTile tile = TileAt(s);
                if (!human && tile != null && tile.Detail != null)
                    tile.Detail.text = MenuSheet.JoinPrompt;
                if (tile != null)
                {
                    Color seat = MenuTheme.Seat(s);
                    tile.KeepBar = true;
                    tile.BarColor = seat;
                    if (tile.Bar != null) tile.Bar.color = tile.BarColor;
                    tile.Tint(Color.Lerp(MenuTheme.Ink, seat, UiSweep.SeatMix));
                    tile.LockColors = true;
                    MenuWidgets.JoinDress(tile, seat, human, human && MenuSession.Ready[s], profile, deviceLine);
                    if (!human) MenuBindRow.JoinPair(tile);
                }
            }
            _count = 4;
            if (_banner != null)
            {
                _banner.text = CouchPlay.Humans > 0
                    ? "Everyone Ready? Press Start"
                    : "Anyone can join";
            }
            float hintY = 24f + cardH + 12f;
            float hintH = 44f;
            if (hintY + hintH < bodyH - 8f)
            {
                var hintRt = MenuWidgets.Place(_body, "CvdHint", 16f, hintY, span - 32f, hintH);
                var hintPlate = hintRt.gameObject.AddComponent<Image>();
                MenuArt.Plate(hintPlate, MenuTheme.Navy, true);
                hintPlate.raycastTarget = false;
                Text hint = MenuWidgets.Words(hintRt, "Color-blind seat colors in Options > Accessibility", UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Cream, Vector2.zero, Vector2.one);
                LockFit(hint, UiFit.FloorFont);
            }
            SyncStartMarks();
        }

        void SyncStartMarks()
        {
            bool show = _banner != null && _banner.text == "Everyone Ready? Press Start";
            if (show && _bannerSpace == null) BuildStartMarks();
            if (_bannerSpace != null) _bannerSpace.enabled = show;
            if (_bannerStart != null) _bannerStart.enabled = show;
        }

        void BuildStartMarks()
        {
            if (_banner == null) return;
            _bannerSpace = BannerMark("BannerSpace", MenuIcons.KeySpace, 236f);
            _bannerStart = BannerMark("BannerStart", MenuIcons.StartButton, 284f);
        }

        Image BannerMark(string name, Sprite sprite, float x)
        {
            var rt = MenuWidgets.Box(_banner.transform, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f));
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(40f, 32f);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        static string FooterLine(int slot, int padFamily, int padFace)
        {
            string kb = PadGlyph.Line(PadGlyph.Keyboard, slot, FaceMap.DefaultOf(PadGlyph.Keyboard));
            string pad = PadGlyph.Line(padFamily, slot, padFace);
            int kbCut = kb.LastIndexOf("   ", System.StringComparison.Ordinal);
            int padCut = pad.LastIndexOf("   ", System.StringComparison.Ordinal);
            string kbName = kbCut >= 0 ? kb.Substring(0, kbCut) : kb;
            string padName = padCut >= 0 ? pad.Substring(0, padCut) : pad;
            string verb = kbCut >= 0 ? kb.Substring(kbCut) : "";
            return kbName + " / " + padName + verb;
        }

        void BuildCast()
        {
            _count = 0;
            _naming = -1;
            if (_header != null) _header.text = "  Characters";
            if (_banner != null) _banner.text = "Down from a color opens the name keys";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            float span = UiFit.BodyW(UiFit.Current());
            UiFit.CastBands(UiFit.Current(), out float cardH, out float gridTop, out float gridH, out float gridStep);
            float cardW = (span - 16f * 5f) / 4f;
            if (cardW > 428f) cardW = 428f;
            for (int s = 0; s < 4; s++)
            {
                float x = 16f + s * (cardW + 16f);
                var card = MenuWidgets.Place(_body, "Cast" + s.ToString(), x, 8f, cardW, cardH);
                var plate = card.gameObject.AddComponent<Image>();
                MenuArt.Plate(plate, MenuTheme.Seat(s), true);
                plate.raycastTarget = false;
                _castPlate[s] = plate;
                var castChip = MenuWidgets.Place(card, "CastChip", 16f, cardH - 78f, 56f, 56f);
                Image castBack = castChip.gameObject.AddComponent<Image>();
                castBack.color = new Color(0.02f, 0.02f, 0.04f, 1f);
                castBack.raycastTarget = false;
                SeatShape.Stamp(castChip, s, 6f, 6f, 44f, MenuTheme.SeatFill(s));
                float nameH = UiFit.CastNameBand();
                float statusH = UiFit.CastStatusBand();
                float textH = nameH + statusH;
                float swH = 26f;
                float textY = cardH - 8f - textH;
                if (textY < 80f) textY = 80f;
                float swY = textY - 8f - swH;
                float viewRoom = swY - 16f;
                float viewSide = cardW - 24f;
                if (viewSide > viewRoom) viewSide = viewRoom;
                if (viewSide < 88f) viewSide = 88f;
                float viewX = (cardW - viewSide) * 0.5f;
                float viewH = viewSide;
                var viewRt = MenuWidgets.Place(card, "View", viewX, 10f, viewSide, viewH);
                var raw = viewRt.gameObject.AddComponent<RawImage>();
                raw.raycastTarget = false;
                _castView[s] = raw;
                var well = MenuWidgets.Place(card, "JoinWell", viewX, 10f, viewSide, viewH);
                var wellImage = well.gameObject.AddComponent<Image>();
                MenuArt.Plate(wellImage, MenuTheme.Navy, true);
                wellImage.raycastTarget = false;
                _castWell[s] = wellImage;
                float chip = (cardW - 28f) / 6f;
                if (chip > 36f) chip = 36f;
                float rowW = chip * 6f;
                float swX = (cardW - rowW) * 0.5f;
                var ring = MenuWidgets.Place(card, "SwatchRing", swX - 2f, swY - 3f, chip + 4f, swH + 6f);
                var ringImage = ring.gameObject.AddComponent<Image>();
                ringImage.color = MenuTheme.Gold;
                ringImage.raycastTarget = false;
                _swatchRing[s] = ring;
                for (int c = 0; c < 6; c++)
                {
                    var bit = MenuWidgets.Place(card, "Swatch", swX + c * chip + 3f, swY, chip - 6f, swH);
                    var bitImage = bit.gameObject.AddComponent<Image>();
                    bitImage.color = MenuPortraits.Tint(c);
                    bitImage.raycastTarget = false;
                    _swatch[s * 6 + c] = bit;
                }
                var glyphRt = MenuWidgets.Place(card, "Pad", cardW - 64f, 8f, 52f, 52f);
                var glyphPlate = glyphRt.gameObject.AddComponent<Image>();
                MenuArt.Plate(glyphPlate, MenuTheme.Navy, true);
                glyphPlate.raycastTarget = false;
                var glyphIcon = MenuWidgets.Place(glyphRt, "Glyph", 6f, 6f, 40f, 40f);
                var glyphImage = glyphIcon.gameObject.AddComponent<Image>();
                glyphImage.preserveAspect = true;
                glyphImage.raycastTarget = false;
                glyphImage.enabled = false;
                _castGlyph[s] = glyphImage;
                var nameRt = MenuWidgets.Place(card, "NameLine", 4f, textY, cardW - 8f, nameH);
                var namePlate = nameRt.gameObject.AddComponent<Image>();
                MenuArt.Plate(namePlate, MenuTheme.Navy, true);
                namePlate.raycastTarget = false;
                _castName[s] = MenuWidgets.Words(nameRt, "", UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Cream, Vector2.zero, Vector2.one);
                Snug(_castName[s]);
                LockFit(_castName[s], UiFit.FloorFont);
                if (_castName[s] != null)
                {
                    _castName[s].rectTransform.offsetMin = new Vector2(12f, 6f);
                    _castName[s].rectTransform.offsetMax = new Vector2(-12f, -6f);
                }
                var statRt = MenuWidgets.Place(card, "StatusLine", 4f, textY + nameH, cardW - 8f, statusH);
                var statPlate = statRt.gameObject.AddComponent<Image>();
                MenuArt.Plate(statPlate, MenuTheme.Navy, true);
                statPlate.raycastTarget = false;
                _castReady[s] = MenuWidgets.Words(statRt, "", UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Cream, Vector2.zero, Vector2.one);
                Snug(_castReady[s]);
                LockFit(_castReady[s], UiFit.FloorFont);
                if (_castReady[s] != null)
                {
                    _castReady[s].rectTransform.offsetMin = new Vector2(12f, 8f);
                    _castReady[s].rectTransform.offsetMax = new Vector2(-12f, -8f);
                }
                SeatChip(card, 12f, 12f, s);
                _castJoin[s] = MenuWidgets.Words(card, PadGlyph.Join(PadGlyph.Generic), 32, TextAnchor.MiddleCenter, MenuTheme.Cream, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.72f));
                float burstW = viewSide * 0.42f;
                float burstH = viewH * 0.22f;
                if (burstH < 48f) burstH = 48f;
                float burstX = viewX + viewSide - burstW - 6f;
                float burstY = 10f + viewH - burstH - 8f;
                var burst = MenuWidgets.Place(card, "ReadyBurst", burstX, burstY, burstW, burstH);
                burst.pivot = new Vector2(1f, 0f);
                burst.anchoredPosition = new Vector2(burstX + burstW, -(burstY + burstH));
                var burstImage = burst.gameObject.AddComponent<RawImage>();
                burstImage.texture = MenuBackdrop.Ready;
                burstImage.raycastTarget = false;
                burstImage.color = burstImage.texture != null ? Color.white : new Color(1f, 1f, 1f, 0f);
                if (burstImage.texture == null)
                {
                    Text edge = MenuWidgets.Heading(burst, "READY!", 72, TextAnchor.MiddleCenter, MenuTheme.Stroke, Vector2.zero, Vector2.one);
                    edge.rectTransform.anchoredPosition = new Vector2(5f, -5f);
                    MenuWidgets.Heading(burst, "READY!", 72, TextAnchor.MiddleCenter, MenuTheme.Gold, Vector2.zero, Vector2.one);
                }
                burst.gameObject.SetActive(false);
                _readyBurst[s] = burst;
            }
            float colW = 220f;
            float gridSpan = colW * 3f + 16f;
            if (gridSpan > span - 16f)
                colW = (span - 32f) / 3f;
            float gridX = (span - (colW * 3f + 16f)) * 0.5f;
            if (gridX < 8f) gridX = 8f;
            for (int c = 0; c < 6; c++)
            {
                int col = c % 3;
                int row = c / 3;
                float x = gridX + col * (colW + 8f);
                float y = gridTop + row * gridStep;
                string name = c < LocalProfiles.HierNames.Length ? LocalProfiles.HierNames[c] : "Color";
                AddTile(x, y, colW, gridH, c, name.ToUpperInvariant(), " ", true);
                MenuTile tile = TileAt(c);
                if (tile != null)
                {
                    tile.KeepBar = true;
                    tile.BarColor = MenuPortraits.Tint(c);
                    if (tile.Bar != null) tile.Bar.color = tile.BarColor;
                    tile.Tint(Color.Lerp(MenuTheme.Panel, MenuPortraits.Tint(c), UiSweep.PortraitMix));
                    _castMark[c] = tile.Detail;
                }
            }
            float keysH = 216f;
            float keysRoom = UiFit.BodyH(UiFit.Current()) - gridTop;
            if (keysH > keysRoom) keysH = keysRoom;
            if (keysH < gridH) keysH = gridH;
            BuildKeys(gridTop, keysH);
            _count = 6;
            _castSig = int.MinValue;
            RefreshCast();
        }

        void BuildKeys(float top, float height)
        {
            float span = UiFit.BodyW(UiFit.Current());
            _keys = MenuWidgets.Place(_body, "Keys", 8f, top, span - 16f, height);
            var back = _keys.gameObject.AddComponent<Image>();
            MenuArt.Plate(back, MenuTheme.Navy, true);
            back.raycastTarget = false;
            _nameWord = MenuWidgets.Words(_keys, "", UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Gold, new Vector2(0f, 0.86f), new Vector2(1f, 1f));
            LockFit(_nameWord, UiFit.FloorFont);
            float cellW = (span - 48f) / LocalProfiles.Cols;
            if (cellW > 150f) cellW = 150f;
            float cellH = (height - 56f) / LocalProfiles.Rows;
            if (cellH > 72f) cellH = 72f;
            if (cellH < 36f) cellH = 36f;
            for (int i = 0; i < LocalProfiles.Cols * LocalProfiles.Rows; i++)
            {
                int col = i % LocalProfiles.Cols;
                int row = i / LocalProfiles.Cols;
                char cell = LocalProfiles.KeyAt(col, row);
                string word = KeyWord(cell);
                var rt = MenuWidgets.Place(_keys, "Key", 12f + col * (cellW + 4f), 48f + row * (cellH + 4f), cellW, cellH);
                var plate = rt.gameObject.AddComponent<Image>();
                MenuArt.Plate(plate, MenuTheme.Panel, true);
                plate.raycastTarget = false;
                _keyPlate[i] = plate;
                _keyWord[i] = MenuWidgets.Words(rt, word, UiFit.FloorFont, TextAnchor.MiddleCenter, MenuTheme.Cream, Vector2.zero, Vector2.one);
                LockFit(_keyWord[i], UiFit.FloorFont);
            }
            _keys.gameObject.SetActive(false);
        }

        void BuildRules()
        {
            _count = RuleBook.Count;
            _cols = 2;
            _window = 0;
            _focus = (int)MenuSession.Mode;
            if (_focus < 0 || _focus > 3) _focus = 1;
            if (_header != null) _header.text = "  Mode and rules";
            if (_banner != null) _banner.text = "Up and down move. Left and right change a rule. Left at the end returns to the modes.";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            UiFit.Columns(UiFit.Current(), out float leftX, out float leftW, out _, out _);
            float colW = (leftW - 16f) * 0.5f;
            if (colW > 440f) colW = 440f;
            for (int i = 0; i < 4; i++)
            {
                var id = (TagModeId)i;
                int col = i % 2;
                int row = i / 2;
                string mark = MenuSession.Mode == id ? "Selected" : " ";
                float modeH = UiFit.BlockH(152f, 4);
                float modeStep = UiFit.IdentityText() ? 168f : modeH + 16f;
                AddTile(leftX + col * (colW + 12f), 12f + row * modeStep, colW, modeH, i, MenuCatalog.ModeName(id), MenuCatalog.ModeBlurb(id) + "\n" + mark, true);
            }
            MenuWidgets.Mark(TileAt(0), MenuIcons.Play, MenuIcons.PlayTint, 56f);
            MenuWidgets.Mark(TileAt(1), MenuIcons.Star, MenuIcons.CreditsTint, 56f);
            MenuWidgets.Mark(TileAt(2), MenuIcons.Cone, MenuIcons.PracticeTint, 56f);
            MenuWidgets.Mark(TileAt(3), MenuIcons.Pad, MenuIcons.ControlsTint, 56f);
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
            ClampRuleWindow();
            GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
            UiFit.Columns(UiFit.Current(), out _, out _, out float rightX, out float rightW);
            float ruleH = UiFit.RowH(80f);
            float ruleStep = UiFit.RowStep(84f, 80f);
            int win = UiFit.Window(UiFit.Current(), ruleStep, 12f);
            int shown = 0;
            for (int index = 4; index < RuleBook.Count; index++)
            {
                if (index < 4 + _window || index >= 4 + _window + win) continue;
                AddTile(rightX, 12f + shown * ruleStep, rightW, ruleH, index, RuleBook.Title(index), RuleBook.Detail(s, index), true);
                shown++;
            }
            _count = RuleBook.Count;
            PaintRuleScroll(win, shown);
            ShowHow();
            RefreshFocus();
        }

        void PaintRuleScroll(int win, int shown)
        {
            if (_body != null)
            {
                for (int i = _body.childCount - 1; i >= 0; i--)
                {
                    if (_body.GetChild(i).name == "RuleScroll")
                        DestroyImmediate(_body.GetChild(i).gameObject);
                }
            }
            int rows = RuleBook.Count - 4;
            if (rows <= win) return;
            UiFit.Columns(UiFit.Current(), out _, out _, out float rightX, out float rightW);
            float trackH = shown * 84f - 8f;
            if (trackH < 160f) trackH = 160f;
            var track = MenuWidgets.Place(_body, "RuleScroll", rightX + rightW + 8f, 16f, 14f, trackH);
            Image trackImage = track.gameObject.AddComponent<Image>();
            MenuArt.Plate(trackImage, new Color(0f, 0f, 0f, 0.55f), true);
            trackImage.raycastTarget = false;
            int max = rows - win;
            if (max < 1) max = 1;
            float thumbH = trackH * (win / (float)rows);
            if (thumbH < 48f) thumbH = 48f;
            if (thumbH > trackH) thumbH = trackH;
            float travel = trackH - thumbH;
            float t = _window / (float)max;
            var thumb = MenuWidgets.Place(track, "RuleThumb", 2f, travel * t, 10f, thumbH);
            Image thumbImage = thumb.gameObject.AddComponent<Image>();
            MenuArt.Plate(thumbImage, MenuTheme.Gold, true);
            thumbImage.raycastTarget = false;
        }

        void ClampRuleWindow()
        {
            if (_focus < 4) return;
            int win = UiFit.Window(UiFit.Current(), 84f, 12f);
            int right = _focus - 4;
            int rows = RuleBook.Count - 4;
            if (right < _window) _window = right;
            if (right >= _window + win) _window = right - (win - 1);
            int max = rows - win;
            if (max < 0) max = 0;
            if (_window > max) _window = max;
            if (_window < 0) _window = 0;
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
            _cols = MenuSheet.ArenaCols;
            _focus = MenuSession.RandomArena ? 3 : MenuSession.Arena;
            if (_focus < 0 || _focus > 4) _focus = 0;
            if (_header != null) _header.text = "  Arena";
            if (_banner != null) _banner.text = "Left and right pick a park";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            float span = UiFit.BodyW(UiFit.Current());
            float bodyH = UiFit.BodyH(UiFit.Current());
            float gap = 16f;
            float cardW = (span - gap * 4f) / 3f;
            if (cardW > 560f) cardW = 560f;
            float rowW = cardW * 3f + gap * 2f;
            float x0 = (span - rowW) * 0.5f;
            if (x0 < 8f) x0 = 8f;
            float btnH = UiFit.RowH(100f);
            float cardH = bodyH - btnH - 36f;
            if (cardH > 620f) cardH = 620f;
            if (cardH < 240f) cardH = 240f;
            for (int i = 0; i < ParkArena.Count; i++)
            {
                AddTile(x0 + i * (cardW + gap), 12f, cardW, cardH, i, ParkArena.NameOf(i), MenuArenaCard.Blurb(i), true);
                SeatArenaCard(TileAt(i), i, cardW, cardH);
            }
            float half = (rowW - gap) * 0.5f;
            AddTile(x0, 12f + cardH + 16f, half, btnH, 3, "Random", "One of Mega Park, Pocket Park, or Stack Yard.", true);
            AddTile(x0 + half + gap, 12f + cardH + 16f, half, btnH, 4, "Back", "", true);
            ShowArenaPreview();
        }

        static void SeatArenaCard(MenuTile tile, int id, float cardW, float cardH)
        {
            if (tile == null) return;
            float cap = 128f;
            if (cap > cardH * 0.4f) cap = cardH * 0.4f;
            float photoH = cardH - cap - 8f;
            if (photoH < 80f) photoH = 80f;
            MenuWidgets.Thumb(tile, MenuArenaArt.Thumb(id), 14f, 14f, cardW - 28f, photoH);
            if (tile.Label != null)
            {
                RectTransform rt = tile.Label.rectTransform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(20f, 58f);
                rt.sizeDelta = new Vector2(cardW - 40f, 46f);
            }
            if (tile.Detail != null)
            {
                RectTransform rt = tile.Detail.rectTransform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(20f, 10f);
                rt.sizeDelta = new Vector2(cardW - 40f, 48f);
            }
        }

        void ShowArenaPreview()
        {
            if (_arenaShot == null) return;
            int id = _focus;
            bool random = id == 3 || id < 0 || id >= ParkArena.Count;
            int shown = random ? MenuSession.Arena : id;
            if (shown < 0 || shown >= ParkArena.Count) shown = 0;
            Texture tex = MenuBackdrop.Shot(shown);
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
            ShowFlyover(shown, 0.2f);
        }

        void ShowFlyover(int arena, float alpha)
        {
            if (_flyover == null) return;
            Texture tex = MenuBackdrop.Fly(arena);
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
            if (_dim != null) _dim.color = new Color(0f, 0f, 0f, 0f);
            string name = MenuSession.RandomArena ? "Random" : ParkArena.NameOf(MenuSession.Arena);
            if (_loadPractice) name = PracticeSession.ArenaName();
            if (_header != null) _header.text = "  Loading";
            int fly = MenuSession.Arena;
            if (fly < 0 || fly >= ParkArena.Count) fly = 0;
            ShowFlyover(fly, 1f);
            if (_flyover != null)
            {
                Texture bright = MenuBackdrop.Bright(fly);
                if (bright != null) _flyover.texture = bright;
            }
            _tipBase = _tip;
            _tipSpin = int.MinValue;
            BuildLoadSeats(name, fly);
            _tip++;
            _loadStep = -1;
            if (_banner != null) _banner.text = "";
        }

        void BuildLoadSeats(string arena, int fly)
        {
            int humans = CouchPlay.Humans;
            if (humans < 1) humans = 1;
            if (humans > 4) humans = 4;
            int split = GameSettings.Current != null ? GameSettings.Current.SplitAxis : GameSettings.SplitVertical;
            int panes = CouchPlay.Panes(humans);
            Texture look = MenuBackdrop.Bright(fly);
            _loadFill = null;
            _loadWord = null;
            for (int i = 0; i < panes; i++)
            {
                if (humans == 3 && i == 3) continue;
                CouchPlay.Norm(i, humans, split, out float x, out float y, out float w, out float h);
                if (look != null)
                {
                    var frameRt = MenuWidgets.Box(_body, "LoadBand", new Vector2(x, y), new Vector2(x + w, y + h), new Vector2(0.5f, 0.5f));
                    Image band = frameRt.gameObject.AddComponent<Image>();
                    band.color = MenuTheme.Seat(i);
                    band.raycastTarget = false;
                    const float edge = 0.012f;
                    var camRt = MenuWidgets.Box(frameRt, "LoadCam", new Vector2(edge, edge), new Vector2(1f - edge, 1f - edge), new Vector2(0.5f, 0.5f));
                    RawImage cam = camRt.gameObject.AddComponent<RawImage>();
                    cam.texture = look;
                    cam.color = Color.white;
                    cam.raycastTarget = false;
                    cam.uvRect = new Rect(LoadCamX[i], LoadCamY[i], LoadCamW[i], LoadCamH[i]);
                    if (i == 1)
                    {
                        Texture runners = MenuBackdrop.Chase;
                        if (runners != null)
                        {
                            var runRt = MenuWidgets.Box(camRt, "LoadChase", new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.94f), new Vector2(0.5f, 0.5f));
                            RawImage run = runRt.gameObject.AddComponent<RawImage>();
                            run.texture = runners;
                            run.color = Color.white;
                            run.raycastTarget = false;
                            run.uvRect = new Rect(0.16f, 0.19f, 0.67f, 0.74f);
                        }
                    }
                }
                float pad = 0.02f;
                float y0 = y + 0.012f;
                float y1 = y + h * 0.32f;
                var card = MenuWidgets.Box(_body, "LoadSeat", new Vector2(x + pad, y0), new Vector2(x + w - pad, y1), new Vector2(0.5f, 0.5f));
                Image plate = card.gameObject.AddComponent<Image>();
                MenuArt.Plate(plate, new Color(0.04f, 0.07f, 0.16f, 0.82f), true);
                plate.raycastTarget = false;
                var well = MenuWidgets.Box(card, "Well", new Vector2(0.04f, 0.62f), new Vector2(0.16f, 0.94f), new Vector2(0.5f, 0.5f));
                Image wellImage = well.gameObject.AddComponent<Image>();
                wellImage.color = new Color(0.02f, 0.02f, 0.04f, 1f);
                wellImage.raycastTarget = false;
                var chipRt = MenuWidgets.Box(well, "Chip", new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f), new Vector2(0.5f, 0.5f));
                Image chip = chipRt.gameObject.AddComponent<Image>();
                chip.color = MenuTheme.Seat(i);
                chip.raycastTarget = false;
                var shapeRt = MenuWidgets.Box(chipRt, "SeatShape", new Vector2(0.16f, 0.16f), new Vector2(0.84f, 0.84f), new Vector2(0.5f, 0.5f));
                Image shape = shapeRt.gameObject.AddComponent<Image>();
                shape.sprite = SeatShape.For(i);
                shape.preserveAspect = true;
                shape.raycastTarget = false;
                shape.color = MenuTheme.SeatFill(i);
                chip.color = new Color(0.02f, 0.02f, 0.04f, 1f);
                MenuWidgets.Words(card, arena, 36, TextAnchor.MiddleLeft, MenuTheme.Gold, new Vector2(0.24f, 0.82f), new Vector2(0.78f, 0.96f));
                MenuWidgets.Words(card, MenuArenaCard.Size(fly), UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Cream, new Vector2(0.24f, 0.68f), new Vector2(0.78f, 0.80f));
                string who = i == 0 ? "P1" : i == 1 ? "P2" : i == 2 ? "P3" : "P4";
                MenuWidgets.Words(card, who, UiFit.FloorFont, TextAnchor.MiddleRight, MenuTheme.Cream, new Vector2(0.80f, 0.74f), new Vector2(0.96f, 0.94f));
                var tipRt = MenuWidgets.Box(card, "TipPlate", new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.58f), new Vector2(0.5f, 0.5f));
                Image tipPlate = tipRt.gameObject.AddComponent<Image>();
                MenuArt.Plate(tipPlate, MenuTheme.Gold, true);
                tipPlate.raycastTarget = false;
                Text tip = MenuWidgets.Words(tipRt, MenuTips.Shown(i, _tipBase), UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Ink, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.88f));
                if (tip != null) tip.horizontalOverflow = HorizontalWrapMode.Wrap;
                _loadTip[i] = tip;
                var track = MenuWidgets.Box(card, "Track", new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.30f), new Vector2(0.5f, 0.5f));
                Image trackImage = track.gameObject.AddComponent<Image>();
                MenuArt.Plate(trackImage, new Color(0.02f, 0.05f, 0.12f, 1f), true);
                trackImage.raycastTarget = false;
                var fill = MenuWidgets.Box(track, "Fill", new Vector2(0f, 0.42f), new Vector2(0f, 0.58f), new Vector2(0f, 0.5f));
                Image fillImage = fill.gameObject.AddComponent<Image>();
                MenuArt.Plate(fillImage, new Color(1f, 0.84f, 0.35f, 0.38f), true);
                fillImage.raycastTarget = false;
                fillImage.enabled = false;
                _loadBar[i] = fillImage;
                Text word = MenuWidgets.Words(card, LoadCaption(false, false), UiFit.FloorFont, TextAnchor.MiddleCenter, MenuTheme.Cream, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.14f));
                if (i == 0)
                {
                    _loadFill = fillImage;
                    _loadWord = word;
                }
            }
        }

        void SpinTips()
        {
            if (MenuVideo.ReduceMotion) return;
            int step = (int)(Time.unscaledTime * 0.35f);
            if (step == _tipSpin) return;
            _tipSpin = step;
            for (int i = 0; i < 4; i++)
            {
                if (_loadTip[i] == null) continue;
                _loadTip[i].text = MenuTips.Shown(i, _tipBase + step);
            }
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
            if (_header != null) _header.text = "  Paused by " + who;
            if (_banner != null) _banner.text = PausePlace();
            int n = MenuSplitPause.Fill(_cards);
            for (int c = 0; c < n; c++)
            {
                MenuSplitPause.Card card = _cards[c];
                if (!card.Show) continue;
                float gap = 8f;
                float bh = 84f;
                if (card.H < 640f) bh = 68f;
                if (!UiFit.IdentityText())
                {
                    float need = UiFit.RowH(84f);
                    if (bh < need) bh = need;
                }
                float stack = MenuSplitPause.Items * bh + (MenuSplitPause.Items - 1) * gap;
                float room = card.H * 0.46f;
                if (stack > room && room > 240f)
                {
                    bh = (room - gap * (MenuSplitPause.Items - 1)) / MenuSplitPause.Items;
                    if (bh < 58f) bh = 58f;
                    stack = MenuSplitPause.Items * bh + (MenuSplitPause.Items - 1) * gap;
                }
                float bw = card.W - 72f;
                if (bw > 760f) bw = 760f;
                if (bw < 240f) bw = card.W - 36f;
                float bx = card.X + (card.W - bw) * 0.5f;
                float by = card.Y + card.H - stack - 20f;
                if (by < card.Y + 64f) by = card.Y + 64f;
                float plateX = bx - 16f;
                float plateY = by - 52f;
                float plateW = bw + 32f;
                float plateH = stack + 68f;
                var plate = MenuWidgets.Place(_body, "PauseCard", plateX, plateY, plateW, plateH);
                var plateImage = plate.gameObject.AddComponent<Image>();
                MenuArt.Plate(plateImage, new Color(0.05f, 0.12f, 0.32f, 0.92f), true);
                plateImage.raycastTarget = false;
                var stripe = MenuWidgets.Place(plate, "PauseBand", 0f, 0f, plateW, 8f);
                Image stripeImage = stripe.gameObject.AddComponent<Image>();
                stripeImage.color = MenuTheme.Seat(card.Seat);
                stripeImage.raycastTarget = false;
                var markWell = MenuWidgets.Place(plate, "PauseWell", 14f, 14f, 32f, 32f);
                Image markBack = markWell.gameObject.AddComponent<Image>();
                markBack.color = new Color(0.02f, 0.02f, 0.04f, 1f);
                markBack.raycastTarget = false;
                SeatShape.Stamp(markWell, card.Seat, 2f, 2f, 28f, MenuTheme.SeatFill(card.Seat));
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
            if (_dim != null) _dim.color = new Color(0.02f, 0.04f, 0.10f, 0.28f);
            ShowFlyover(ParkArena.Mega, 1f);
            TagModeController mode = TagModeController.Instance;
            TagModeId modeId = mode != null ? mode.SelectedMode : MenuSession.Mode;
            string headline = MenuSheet.ResultsWord;
            int n = FillRanks();
            string resultLine = MenuCatalog.ModeName(modeId);
            if (mode != null && !string.IsNullOrEmpty(mode.ResultMessage))
                resultLine = MenuCatalog.ModeName(modeId) + "  ·  " + mode.ResultMessage;
            if (n > 0)
            {
                string bodyName = MenuMannequin.NameOf(_rows[0].Hier);
                string accentName = MenuMannequin.NameOf(_rows[0].Accent);
                resultLine = resultLine + "  ·  " + bodyName + " / " + accentName;
            }
            if (_header != null) _header.text = "  " + headline;
            if (_banner != null) _banner.text = resultLine;
            float span = UiFit.BodyW(UiFit.Current());
            UiFit.Bands(UiFit.Current(), out float viewH, out float rankY, out float rankH, out float btnY, out float btnH);
            float stageW = span - 32f;
            if (stageW > 1680f) stageW = 1680f;
            float stageX = (span - stageW) * 0.5f;
            var stage = MenuWidgets.Place(_body, "ResultsView", stageX, 8f, stageW, viewH);
            var frame = stage.gameObject.AddComponent<Image>();
            frame.color = new Color(0f, 0f, 0f, 0f);
            frame.raycastTarget = false;
            var viewRt = MenuWidgets.Place(stage, "View", 0f, 0f, stageW, viewH);
            var view = viewRt.gameObject.AddComponent<RawImage>();
            view.raycastTarget = false;
            if (_preview != null) _preview.ShowPodium(n, _rows, view);
            for (int rank = 0; rank < n; rank++)
            {
                string detail = MenuPodium.Stats(_rows[rank]);
                int seat = _rows[rank].Seat;
                if (seat < 0) seat = rank;
                float rankW = (span - 32f) / 4f;
                if (rankW > 436f) rankW = 436f;
                int slot = rank == 0 ? 1 : rank == 1 ? 0 : rank;
                MenuTile tile = AddTile(8f + slot * (rankW + 8f), rankY, rankW, rankH, 20 + rank, MenuTheme.Place(rank) + "  " + _rows[rank].Name, detail, false);
                if (tile != null)
                {
                    tile.KeepBar = false;
                    tile.Tint(new Color(0.06f, 0.12f, 0.28f, 1f));
                    tile.WinnerStroke = _rows[rank].Winner;
                    if (tile.Stroke != null)
                        tile.Stroke.color = tile.WinnerStroke ? MenuTheme.Gold : MenuTheme.Stroke;
                    SeatBadge(tile.transform, 28f, UiFit.StripeClear(), seat);
                    Pull(tile.Label, 100f);
                    Pull(tile.Detail, 100f);
                    MenuReveal.Row(tile.transform as RectTransform);
                }
            }
            float btnW = (span - 40f) / 4f;
            if (btnW > 436f) btnW = 436f;
            MenuTile rematch = AddTile(8f, btnY, btnW, btnH, 0, "Rematch", "Same setup", true);
            MenuTile modeBtn = AddTile(8f + (btnW + 8f), btnY, btnW, btnH, 1, "Change mode", "", true);
            MenuTile castBtn = AddTile(8f + (btnW + 8f) * 2f, btnY, btnW, btnH, 2, "Character select", "", true);
            MenuTile menuBtn = AddTile(8f + (btnW + 8f) * 3f, btnY, btnW, btnH, 3, "Main menu", "", true);
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
            FitHeader();
            ClearKeepHeader();
            _count = MenuDepth.Count;
            if (_header != null) _header.text = "  " + MenuDepth.Header();
            if (_banner != null) _banner.text = MenuDepth.Banner();
            int win = OptionWindow();
            int pinned = MenuDepth.Page == MenuDepth.Access ? 1 : 0;
            int scrollRows = _count - pinned;
            int scrollFocus = _focus;
            if (pinned > 0 && scrollFocus >= scrollRows) scrollFocus = scrollRows - 1;
            if (scrollFocus < 0) scrollFocus = 0;
            if (scrollFocus < _window) _window = scrollFocus;
            if (scrollFocus >= _window + win) _window = scrollFocus - (win - 1);
            int max = scrollRows - win;
            if (max < 0) max = 0;
            if (_window > max) _window = max;
            if (_window < 0) _window = 0;
            UiFit.OptionSpan(out float rowH, out float step);
            for (int v = 0; v < win; v++)
            {
                int index = _window + v;
                if (index >= scrollRows) break;
                UiFit.RowBox(UiFit.Current(), 1120f, out float rowX, out float rowW);
                if (MenuDepth.Page == MenuDepth.Hub && index == MenuDepth.Count - 1)
                {
                    PaintHubBar(rowX, 8f + v * step, rowW);
                    continue;
                }
                MenuTile tile = AddTile(rowX, 8f + v * step, rowW, rowH, index, MenuDepth.Title(index), MenuDepth.Detail(index), true);
                if (MenuDepth.Page == MenuDepth.Hub)
                    MarkOption(tile, index);
                if (_pauseChild && MenuDepth.Page == MenuDepth.Hub && index == MenuDepth.Count - 1 && tile != null && tile.Detail != null)
                    tile.Detail.text = "Pause";
                if (tile != null && MenuDepth.Page == MenuDepth.Access)
                {
                    if (index == 0) MenuWidgets.Toggle(tile, MenuVideo.ReduceMotion);
                    else if (index == 5) MenuWidgets.Toggle(tile, Tag.Ui.Hud.MatchHudText.ComicWords);
                }
                float meter = MenuDepth.Meter(index);
                if (tile != null && meter >= 0f)
                    PaintMeter(tile.transform, meter);
            }
            if (MenuDepth.Page == MenuDepth.Access)
            {
                UiFit.RowBox(UiFit.Current(), 1120f, out float barX, out float barW);
                float barY = UiFit.BodyH(UiFit.Current()) - MenuDepth.BarBlock;
                if (barY < 8f) barY = 8f;
                PaintPromptBar(barX, barY, barW, MenuDepth.Access);
            }
            MenuDepth.PaintSwatches(_body);
            _count = MenuDepth.Count;
            RefreshFocus();
        }

        static void MarkOption(MenuTile tile, int index)
        {
            if (index == 0) MenuWidgets.Mark(tile, MenuIcons.Gear, MenuIcons.OptionsTint, 56f);
            else if (index == 1) MenuWidgets.Mark(tile, MenuIcons.Star, MenuIcons.CreditsTint, 56f);
            else if (index == 2) MenuWidgets.Mark(tile, MenuIcons.Play, MenuIcons.PlayTint, 56f);
            else if (index == 3) MenuWidgets.Mark(tile, MenuIcons.Pad, MenuIcons.ControlsTint, 56f);
            else if (index == 4) MenuWidgets.Mark(tile, MenuIcons.Cone, MenuIcons.PracticeTint, 56f);
            else if (index == 5) MenuWidgets.Mark(tile, MenuIcons.Star, MenuIcons.CreditsTint, 56f);
        }

        static void PaintMeter(Transform tile, float meter)
        {
            if (meter < 0f) meter = 0f;
            if (meter > 1f) meter = 1f;
            var track = MenuWidgets.Place(tile, "Meter", 620f, 74f, 440f, 16f);
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
            _count = ControlCount();
            _cols = 1;
            _window = 1;
            if (_header != null) _header.text = "  Controls";
            if (_banner != null) _banner.text = "Keyboard and pad glyphs. Space always jumps.";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            PaintControls();
        }

        void PaintControls()
        {
            ClearKeepHeader();
            FitHeader();
            ControlBands(out int action0, out int noteAt, out int stickAt, out int confirmAt, out int barAt);
            if (_banner != null)
            {
                if (_swapOther >= 0 || _swapGrapple != 0)
                    _banner.text = SwapNames() + " use that button.";
                else if (!string.IsNullOrEmpty(_notice))
                    _banner.text = _notice;
                else if (_capturing)
                    _banner.text = "Esc or B cancels. This waits 5 seconds.";
                else
                    _banner.text = "Keyboard and pad glyphs. Space always jumps.";
            }
            ActionBinds keys = KeyboardBinds();
            ActionBinds pad = PadBinds();
            int win = ControlWindow();
            int last = barAt - 1;
            if (_focus > 0 && _focus < barAt)
            {
                if (_focus < _window) _window = _focus;
                if (_focus >= _window + win) _window = _focus - (win - 1);
            }
            if (_swapOther >= 0 || _swapGrapple != 0)
            {
                int lo = SwapIndex(action0, noteAt, true);
                int hi = SwapIndex(action0, noteAt, false);
                if (hi - lo < win)
                {
                    if (lo < _window) _window = lo;
                    if (hi >= _window + win) _window = hi - (win - 1);
                }
            }
            int maxStart = last - win + 1;
            if (maxStart < 1) maxStart = 1;
            if (_window > maxStart) _window = maxStart;
            if (_window < 1) _window = 1;
            UiFit.RowBox(UiFit.Current(), 1680f, out float rowX, out float rowW);
            UiFit.OptionSpan(out float rowH, out float step);
            const float seatBlock = 80f;
            PaintSeatStrip(rowX, 8f, rowW);
            float swapBlock = _swapOther >= 0 ? 84f : 0f;
            float listTop = 8f + seatBlock;
            if (swapBlock > 0f)
            {
                PaintSwapChips(rowX, listTop, rowW);
                listTop += swapBlock;
            }
            for (int v = 0; v < win; v++)
            {
                int index = _window + v;
                if (index >= barAt) break;
                string title;
                string detail;
                int stick = index - stickAt;
                int note = index - noteAt;
                int action = index - action0;
                if (action >= 0 && action < ShownActions())
                {
                    var act = (PlayAction)action;
                    title = ActionBinds.Name(act);
                    detail = ActionDetail(act, keys);
                    if (_capturing && !_captureGrapple && action == _captureAction) detail = "Press any button to bind";
                }
                else if (note >= 0 && note < ContextNotes)
                {
                    title = NoteTitle(note);
                    detail = NoteDetail(note);
                    if (note == 0 && _capturing && _captureGrapple) detail = "Press any button to bind";
                }
                else if (stick >= 0 && stick < MenuStick.Rows)
                {
                    title = MenuStick.Label(stick);
                    detail = MenuStick.Detail(stick);
                }
                else if (index >= confirmAt && index < barAt)
                {
                    int seat = index - confirmAt;
                    title = ConfirmTitle(seat);
                    GameSettings settings = GameSettings.Current ?? GameSettings.Defaults();
                    detail = FaceMap.Word(settings.ConfirmFace[seat]);
                }
                else
                {
                    title = "Back";
                    detail = "";
                }
                MenuTile row = AddTile(rowX, listTop + v * step, rowW, rowH, index, title, detail, true);
                if (UiFit.IdentityText() && row != null && row.Detail != null && detail != null && detail.Length > 48)
                {
                    row.Detail.resizeTextForBestFit = true;
                    row.Detail.resizeTextMinSize = 18;
                    row.Detail.resizeTextMaxSize = UiFit.FloorFont;
                }
                if (action >= 0 && action < ShownActions() && row != null)
                    MenuBindRow.Stamp(row, action, keys, pad.Gamepad[action]);
                else if (note == 0 && row != null)
                    MenuBindRow.StampToken(row, keys.GrappleKey, pad.GrapplePad);
            }
            float barY = UiFit.BodyH(UiFit.Current()) - MenuDepth.BarBlock;
            PaintControlBar(rowX, barY, rowW, barAt);
            PaintControlScroll(win, listTop);
            RefreshFocus();
            if (_swapOther >= 0 || _swapGrapple != 0)
            {
                MenuTile a = TileAt(SwapIndex(action0, noteAt, true));
                MenuTile b = TileAt(SwapIndex(action0, noteAt, false));
                if (a != null) a.SetHot(true);
                if (b != null) b.SetHot(true);
            }
        }

        void PaintControlScroll(int win, float top)
        {
            if (win < 1) win = 1;
            ControlBands(out _, out _, out _, out _, out int barAt);
            int scrollCount = barAt - 1;
            UiFit.RowBox(UiFit.Current(), 1680f, out float rowX, out float rowW);
            UiFit.OptionSpan(out _, out float step);
            float trackH = win * step - 16f;
            if (trackH < 120f) trackH = 120f;
            float trackX = rowX + rowW + 8f;
            var track = MenuWidgets.Place(_body, "ScrollTrack", trackX, top, 14f, trackH);
            var trackImage = track.gameObject.AddComponent<Image>();
            MenuArt.Plate(trackImage, new Color(0f, 0f, 0f, 0.55f), true);
            trackImage.raycastTarget = false;
            int max = scrollCount - win;
            if (max < 0) max = 0;
            float span = scrollCount <= win ? 1f : win / (float)scrollCount;
            float thumbH = trackH * span;
            if (thumbH < 56f) thumbH = 56f;
            if (thumbH > trackH) thumbH = trackH;
            float travel = trackH - thumbH;
            int start = _window - 1;
            if (start < 0) start = 0;
            float t = max <= 0 ? 0f : start / (float)max;
            var thumb = MenuWidgets.Place(track, "ScrollThumb", 2f, travel * t, 10f, thumbH);
            var thumbImage = thumb.gameObject.AddComponent<Image>();
            MenuArt.Plate(thumbImage, MenuTheme.Gold, true);
            thumbImage.raycastTarget = false;
        }

        void PaintSeatStrip(float x, float y, float w)
        {
            float gap = 12f;
            float chip = 132f;
            for (int s = 0; s < 4; s++)
            {
                bool selected = s == _bindSeat;
                Color seat = MenuTheme.Seat(s);
                RectTransform rt = MenuWidgets.Place(_body, "Seat" + s.ToString(), x + s * (chip + gap), y, chip, 68f);
                Image plate = rt.gameObject.AddComponent<Image>();
                MenuArt.Plate(plate, seat, true);
                plate.raycastTarget = true;
                var stroke = MenuWidgets.Place(rt, "Stroke", 0f, 0f, chip, 68f);
                Image ring = stroke.gameObject.AddComponent<Image>();
                ring.sprite = plate.sprite;
                ring.type = plate.type;
                ring.color = selected ? MenuTheme.Gold : MenuTheme.Stroke;
                ring.raycastTarget = false;
                var inner = MenuWidgets.Place(rt, "Fill", 6f, 6f, chip - 12f, 56f);
                Image fill = inner.gameObject.AddComponent<Image>();
                MenuArt.Plate(fill, seat, true);
                fill.raycastTarget = false;
                string word = "P" + (s + 1).ToString();
                var tag = MenuWidgets.Place(rt, "Tag", 16f, 10f, 100f, 48f);
                Image tagPlate = tag.gameObject.AddComponent<Image>();
                MenuArt.Plate(tagPlate, MenuTheme.Ink, true);
                tagPlate.raycastTarget = false;
                Text label = MenuWidgets.Words(tag, word, UiFit.FloorFont, TextAnchor.MiddleCenter, MenuTheme.Cream, Vector2.zero, Vector2.one);
                LockFit(label, UiFit.FloorFont);
                int seatIndex = s;
                Button button = rt.gameObject.AddComponent<Button>();
                button.targetGraphic = plate;
                button.onClick.AddListener(() =>
                {
                    _focus = 0;
                    if (_bindSeat != seatIndex)
                    {
                        _bindSeat = seatIndex;
                        _resetArmed = false;
                        if (_swapPad) ClearSwap();
                    }
                    MenuAudio.Move();
                    PaintControls();
                });
            }
            MenuBindRow.Columns(w, out float keyRight, out float padRight);
            float keyHeadW = MenuBindRow.KeyCol;
            RectTransform keyHead = MenuWidgets.Place(_body, "KeyHead", x + keyRight - keyHeadW, y, keyHeadW, 68f);
            Image keyPlate = keyHead.gameObject.AddComponent<Image>();
            MenuArt.Plate(keyPlate, MenuTheme.Navy, true);
            keyPlate.raycastTarget = false;
            Text keys = MenuWidgets.Words(keyHead, "Keyboard / Mouse", UiFit.FloorFont, TextAnchor.MiddleRight, MenuTheme.Cream, Vector2.zero, Vector2.one);
            LockFit(keys, UiFit.FloorFont);
            RectTransform padHead = MenuWidgets.Place(_body, "PadHead", x + padRight - MenuBindRow.PadCol, y, MenuBindRow.PadCol, 68f);
            Image padPlate = padHead.gameObject.AddComponent<Image>();
            MenuArt.Plate(padPlate, MenuTheme.Navy, true);
            padPlate.raycastTarget = false;
            Text pads = MenuWidgets.Words(padHead, "Pad", UiFit.FloorFont, TextAnchor.MiddleRight, MenuTheme.Cream, Vector2.zero, Vector2.one);
            LockFit(pads, UiFit.FloorFont);
        }

        void PaintSwapChips(float x, float y, float w)
        {
            float gap = 16f;
            float chipW = (w - gap) * 0.5f;
            SwapChip(x, y, chipW, 72f, 0, "Swap");
            SwapChip(x + chipW + gap, y, chipW, 72f, 1, "Cancel");
        }

        void SwapChip(float x, float y, float w, float h, int pick, string word)
        {
            bool hot = _swapPick == pick;
            RectTransform rt = MenuWidgets.Place(_body, pick == 0 ? "SwapChip" : "CancelChip", x, y, w, h);
            Image plate = rt.gameObject.AddComponent<Image>();
            MenuArt.Plate(plate, hot ? MenuTheme.PanelHot : MenuTheme.Navy, true);
            plate.raycastTarget = true;
            Text label = MenuWidgets.Words(rt, word, UiFit.FloorFont, TextAnchor.MiddleCenter, hot ? MenuTheme.Ink : MenuTheme.Cream, Vector2.zero, Vector2.one);
            LockFit(label, UiFit.FloorFont);
            int chosen = pick;
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = plate;
            button.onClick.AddListener(() =>
            {
                _swapPick = chosen;
                if (chosen == 0) CommitSwap();
                else CancelSwap();
            });
        }

        void PaintControlBar(float x, float y, float w, int barAt)
        {
            ActionBinds binds = ActionBinds.Defaults();
            string confirmKey = binds.Keyboard[(int)PlayAction.Jump];
            string confirmPad = binds.Gamepad[(int)PlayAction.Jump];
            string backKey = binds.Keyboard[(int)PlayAction.Pause];
            string backPad = binds.Gamepad[(int)PlayAction.Slide];
            float gap = 16f;
            float chipW = (w - gap) * 0.5f;
            bool onBar = _focus == barAt;
            string resetWord = _resetArmed ? "Reset bindings?" : "Reset";
            float chipH = UiFit.IdentityText() ? 72f : UiFit.LineH(72f);
            ControlChip(x, y, chipW, chipH, 0, resetWord, confirmKey, confirmPad, onBar && _barChip == 0);
            ControlChip(x + chipW + gap, y, chipW, chipH, 1, "Back", backKey, backPad, onBar && _barChip == 1);
        }

        void ControlChip(float x, float y, float w, float h, int chip, string word, string keyToken, string padToken, bool hot)
        {
            RectTransform rt = MenuWidgets.Place(_body, chip == 0 ? "ResetChip" : "BackChip", x, y, w, h);
            Image plate = rt.gameObject.AddComponent<Image>();
            MenuArt.Plate(plate, hot ? MenuTheme.PanelHot : MenuTheme.Navy, true);
            plate.raycastTarget = true;
            RectTransform keyRt = MenuWidgets.Place(rt, "Key", 12f, 16f, 56f, 40f);
            Image key = keyRt.gameObject.AddComponent<Image>();
            key.sprite = MenuIcons.Glyph(keyToken);
            key.preserveAspect = true;
            key.raycastTarget = false;
            RectTransform padRt = MenuWidgets.Place(rt, "Pad", 76f, 16f, 44f, 40f);
            Image pad = padRt.gameObject.AddComponent<Image>();
            pad.sprite = MenuIcons.Glyph(padToken);
            pad.preserveAspect = true;
            pad.raycastTarget = false;
            Text label = MenuWidgets.Words(rt, word, UiFit.FloorFont, TextAnchor.MiddleLeft, hot ? MenuTheme.Ink : MenuTheme.Cream, Vector2.zero, Vector2.one);
            label.rectTransform.offsetMin = new Vector2(132f, 4f);
            label.rectTransform.offsetMax = new Vector2(-12f, -4f);
            LockFit(label, UiFit.FloorFont);
            int which = chip;
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = plate;
            button.onClick.AddListener(() =>
            {
                ControlBands(out _, out _, out _, out _, out int barAt);
                _focus = barAt;
                _barChip = which;
                ActivateControls();
            });
        }

        int OptionWindow()
        {
            float top = 8f;
            if (MenuDepth.Page == MenuDepth.Access)
                top += MenuDepth.SwatchReserve + MenuDepth.BarBlock;
            UiFit.OptionSpan(out _, out float step);
            int n = UiFit.Window(UiFit.Current(), step, top);
            if (n > OptWindow) n = OptWindow;
            if (n < 1) n = 1;
            return n;
        }

        void PaintHubBar(float x, float y, float w)
        {
            PaintPromptBar(x, y, w, MenuDepth.Hub);
        }

        void PaintPromptBar(float x, float y, float w, int page)
        {
            ActionBinds binds = ActionBinds.Defaults();
            string confirmKey = binds.Keyboard[(int)PlayAction.Jump];
            string confirmPad = binds.Gamepad[(int)PlayAction.Jump];
            string backKey = binds.Keyboard[(int)PlayAction.Pause];
            string backPad = binds.Gamepad[(int)PlayAction.Slide];
            float gap = 16f;
            float chipW = (w - gap) * 0.5f;
            if (chipW < 180f) chipW = 180f;
            bool onBar = _focus == MenuDepth.Count - 1;
            bool armed = OptionApply.ArmedRow(page, MenuDepth.Count - 1);
            string resetWord = armed ? "Reset?" : "Reset";
            string backWord = page == MenuDepth.Hub && _pauseChild ? "Pause" : "Back";
            float chipH = UiFit.IdentityText() ? 72f : UiFit.LineH(72f);
            HubChip(x, y, chipW, chipH, MenuDepth.BarReset, resetWord, confirmKey, confirmPad, onBar && MenuDepth.Bar == MenuDepth.BarReset);
            HubChip(x + chipW + gap, y, chipW, chipH, MenuDepth.BarBack, backWord, backKey, backPad, onBar && MenuDepth.Bar == MenuDepth.BarBack);
        }

        void HubChip(float x, float y, float w, float h, int chip, string word, string keyToken, string padToken, bool hot)
        {
            RectTransform rt = MenuWidgets.Place(_body, chip == MenuDepth.BarReset ? "ResetChip" : "BackChip", x, y, w, h);
            Image plate = rt.gameObject.AddComponent<Image>();
            MenuArt.Plate(plate, hot ? MenuTheme.PanelHot : MenuTheme.Navy, true);
            plate.raycastTarget = true;
            RectTransform keyRt = MenuWidgets.Place(rt, "Key", 12f, 16f, 56f, 40f);
            Image key = keyRt.gameObject.AddComponent<Image>();
            key.sprite = MenuIcons.Glyph(keyToken);
            key.preserveAspect = true;
            key.raycastTarget = false;
            RectTransform padRt = MenuWidgets.Place(rt, "Pad", 76f, 16f, 44f, 40f);
            Image pad = padRt.gameObject.AddComponent<Image>();
            pad.sprite = MenuIcons.Glyph(padToken);
            pad.preserveAspect = true;
            pad.raycastTarget = false;
            Text label = MenuWidgets.Words(rt, word, UiFit.FloorFont, TextAnchor.MiddleLeft, hot ? MenuTheme.Ink : MenuTheme.Cream, Vector2.zero, Vector2.one);
            label.rectTransform.offsetMin = new Vector2(132f, 4f);
            label.rectTransform.offsetMax = new Vector2(-12f, -4f);
            label.resizeTextForBestFit = false;
            label.fontSize = UiFit.TextPx(UiFit.FloorFont);
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = plate;
            button.onClick.AddListener(() =>
            {
                MenuDepth.Bar = chip;
                _focus = MenuDepth.Count - 1;
                ActivateOptions();
            });
        }

        int ControlWindow()
        {
            float reserve = 8f + 80f + MenuDepth.BarBlock;
            if (_swapOther >= 0) reserve += 84f;
            UiFit.OptionSpan(out _, out float step);
            int n = UiFit.Window(UiFit.Current(), step, reserve);
            if (n > OptWindow) n = OptWindow;
            if (n < 3) n = 3;
            return n;
        }

        void PaintLoad()
        {
            Tag.Core.GameFlow flow = Tag.Core.GameFlow.Instance;
            float scene = flow != null ? flow.BootLoad : -1f;
            if (scene >= 0f)
            {
                if (scene > 1f) scene = 1f;
                ApplyLoadFill(scene, true);
                if (_loadWord != null)
                {
                    int pct = Mathf.RoundToInt(scene * 100f);
                    if (pct > 100) pct = 100;
                    string word = scene >= 1f ? LoadGate.Ready : "Loading";
                    _loadWord.text = word + "  " + pct.ToString() + "%";
                }
                _loadStep = 1;
                return;
            }
            bool live = false;
            TagModeController modes = TagModeController.Instance;
            if (modes != null) live = modes.RoundActive;
            int step = LoadGate.Step(_loadFired, live);
            if (step == _loadStep) return;
            _loadStep = step;
            float fill = LoadGate.Fill(_loadFired, live);
            if (fill > 0f) ApplyLoadFill(fill, true);
            if (_loadWord != null) _loadWord.text = LoadCaption(_loadFired, live);
        }

        void ApplyLoadFill(float fill, bool fromLeft)
        {
            if (_loadFill == null) return;
            if (fill < 0f) fill = 0f;
            if (fill > 1f) fill = 1f;
            DriveLoadBars(0.02f, 0.02f + 0.96f * fill, fromLeft && fill > 0f);
        }

        static string LoadCaption(bool fired, bool roundActive)
        {
            int pct = (int)(LoadGate.Fill(fired, roundActive) * 100f);
            return LoadGate.Caption(fired, roundActive) + "  " + pct.ToString() + "%";
        }

        void TickLoadDash()
        {
            if (_screen != MenuScreenId.Loading || _loadFill == null || _loadStep > 0) return;
            float dash = 0.10f;
            float u = MenuVideo.ReduceMotion ? 0.36f : Mathf.Repeat(Time.unscaledTime * 0.35f, 1f);
            float x = u * (1f - dash);
            Color shimmer = new Color(1f, 0.84f, 0.35f, 0.38f);
            for (int i = 0; i < _loadBar.Length; i++)
            {
                Image bar = _loadBar[i];
                if (bar == null) continue;
                RectTransform rt = bar.rectTransform;
                rt.anchorMin = new Vector2(x, 0.42f);
                rt.anchorMax = new Vector2(x + dash, 0.58f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                bar.color = shimmer;
                bar.enabled = true;
            }
        }

        void DriveLoadBars(float x0, float x1, bool on)
        {
            if (x0 < 0f) x0 = 0f;
            if (x1 > 1f) x1 = 1f;
            if (x1 < x0) x1 = x0;
            for (int i = 0; i < _loadBar.Length; i++)
            {
                Image bar = _loadBar[i];
                if (bar == null) continue;
                RectTransform rt = bar.rectTransform;
                rt.anchorMin = new Vector2(x0, 0.15f);
                rt.anchorMax = new Vector2(x1, 0.85f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                bar.color = MenuTheme.Gold;
                bar.enabled = on;
            }
        }

        void BuildCredits()
        {
            _count = 1;
            _cols = 1;
            if (_header != null) _header.text = "  Credits";
            if (_banner != null) _banner.text = "";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            var creditCard = MenuWidgets.Box(_body, "CreditCard", new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.98f), new Vector2(0.5f, 0.5f));
            Image creditPlate = creditCard.gameObject.AddComponent<Image>();
            MenuArt.Plate(creditPlate, new Color(0.04f, 0.10f, 0.24f, 0.9f), true);
            creditPlate.raycastTarget = false;
            Text credits = MenuWidgets.Words(_body, MenuCatalog.Credits(), UiFit.FloorFont, TextAnchor.UpperLeft, MenuTheme.Cream, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.96f));
            credits.horizontalOverflow = HorizontalWrapMode.Wrap;
            credits.verticalOverflow = VerticalWrapMode.Truncate;
            float body = UiFit.BodyH(UiFit.Current());
            UiFit.RowBox(UiFit.Current(), 480f, out float backX, out float backW);
            float backH = UiFit.RowH(80f);
            float backY = body - 20f - backH;
            if (backY < 120f) backY = 120f;
            AddTile(backX, backY, backW, backH, 0, "Back", "", true);
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
            UiFit.RowBox(UiFit.Current(), 1000f, out float rowX, out float rowW);
            float rowH = 90f;
            float body = UiFit.BodyH(UiFit.Current());
            if (12f + PracticeSession.Rows * 100f > body) rowH = 72f;
            if (!UiFit.IdentityText()) rowH = UiFit.LineH(rowH);
            for (int i = 0; i < PracticeSession.Rows; i++)
                AddTile(rowX, 12f + i * (rowH + 8f), rowW, rowH, i, PracticeSession.RowLabel(i), "", true);
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
                    Open(MenuScreenId.Records);
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
            if (!MenuLobby.Enough(MenuSession.Mode, CouchPlay.Humans))
            {
                MenuAudio.Error();
                if (_banner != null) _banner.text = MenuLobby.Short(MenuSession.Mode);
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
                    MenuMatch.RememberRules();
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
                    MenuMatch.RememberRules();
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
            if (act == MenuDepth.OpenCredits)
            {
                MenuAudio.Confirm();
                Open(MenuScreenId.Credits);
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
            if (_swapOther >= 0)
            {
                if (_swapPick == 0) CommitSwap();
                else CancelSwap();
                return;
            }
            ControlBands(out int action0, out int noteAt, out int stickAt, out int confirmAt, out int barAt);
            if (_focus == 0)
                return;
            int note = _focus - noteAt;
            if (note == 0)
            {
                _capturing = true;
                _captureGrapple = true;
                _captureAction = -1;
                _captureFrame = Time.frameCount;
                _captureUntil = Time.unscaledTime + 5f;
                _notice = "";
                MenuAudio.Confirm();
                PaintControls();
                return;
            }
            if (note > 0 && note < ContextNotes)
                return;
            int stick = _focus - stickAt;
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
            if (_focus >= confirmAt && _focus < barAt)
            {
                GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
                GameSettings.Current = s;
                s.StepConfirm(_focus - confirmAt, 1);
                UnlockLooks();
                MenuAudio.Move();
                PaintControls();
                return;
            }
            if (_focus == barAt)
            {
                if (_barChip == 1)
                {
                    MenuAudio.Back();
                    GoBack();
                    return;
                }
                if (!_resetArmed)
                {
                    _resetArmed = true;
                    MenuAudio.Confirm();
                    PaintControls();
                    return;
                }
                ResetSeatBinds();
                _resetArmed = false;
                _notice = "Space always jumps.";
                MenuAudio.Confirm();
                PaintControls();
                return;
            }
            int action = _focus - action0;
            if (action < 0 || action >= ShownActions())
                return;
            _capturing = true;
            _captureAction = action;
            _captureFrame = Time.frameCount;
            _captureUntil = Time.unscaledTime + 5f;
            _notice = "";
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
            int next = MenuRuleNav.Vertical(_focus, _count, dy, (int)MenuSession.Mode);
            if (next == _focus) return;
            int before = _window;
            _focus = next;
            MenuAudio.Move();
            if (_focus >= 4)
            {
                int prior = _window;
                ClampRuleWindow();
                if (_window != prior || before != _window)
                    PaintRuleRows();
                else
                {
                    RefreshFocus();
                    ShowHow();
                }
            }
            else
            {
                RefreshFocus();
                ShowHow();
            }
        }

        void StepRules(int dir)
        {
            if (_focus <= 3)
            {
                int next = MenuRuleNav.ModeSide(_focus, dir);
                if (next == _focus) return;
                _focus = next;
                MenuAudio.Move();
                if (_focus >= 4)
                {
                    ClampRuleWindow();
                    PaintRuleRows();
                }
                else
                {
                    RefreshFocus();
                    ShowHow();
                }
                return;
            }
            GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
            GameSettings.Current = s;
            string before = RuleBook.Detail(s, _focus);
            if (!RuleBook.Edit(s, _focus, dir)) return;
            bool changed = RuleBook.Detail(s, _focus) != before;
            int landed = MenuRuleNav.AfterEdit(_focus, dir, changed, (int)MenuSession.Mode);
            if (landed != _focus)
            {
                _focus = landed;
                MenuAudio.Move();
                RefreshFocus();
                ShowHow();
                return;
            }
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
            bool armed = OptionApply.Armed >= 0;
            OptionApply.Disarm();
            int before = _window;
            int previous = _focus;
            _focus = next;
            MenuAudio.Move();
            if (_screen == MenuScreenId.Options)
            {
                int span = OptionWindow();
                bool hubBar = (MenuDepth.Page == MenuDepth.Hub || MenuDepth.Page == MenuDepth.Access)
                    && (previous == _count - 1 || _focus == _count - 1);
                if (armed || hubBar || _focus < _window || _focus >= _window + span || before != _window)
                    PaintOptions();
                else
                    RefreshFocus();
            }
            else if (_screen == MenuScreenId.Controls)
            {
                bool armed = _resetArmed;
                _resetArmed = false;
                int span = ControlWindow();
                if (armed || _focus < _window || _focus >= _window + span || previous == _count - 1 || _focus == _count - 1)
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
            bool pad = IsPad(token);
            if (_captureGrapple)
            {
                _capturing = false;
                _captureGrapple = false;
                _captureAction = -1;
                TakeGrapple(token, pad);
                return;
            }
            var action = (PlayAction)_captureAction;
            _capturing = false;
            _captureAction = -1;
            if (!pad && action == PlayAction.Jump)
            {
                if (token == "space" || !ActionBinds.KnownKeyboard(token))
                {
                    _notice = "Space always jumps.";
                    MenuAudio.Back();
                    PaintControls();
                    return;
                }
                if (KeyboardClash(token, action, out PlayAction other))
                {
                    BeginSwap(action, other, false, token);
                    return;
                }
                ActionBinds kb = KeyboardBinds();
                if ((kb.Keyboard[(int)PlayAction.Jump] ?? "") != "space")
                    kb.SetKeyboard(PlayAction.Jump, "space");
                kb.SetJumpAlt(token);
                _notice = "Space always jumps. Added " + ActionBinds.Show(token) + " as a second Jump key.";
                SettingsRuntime.Save();
                MenuAudio.Confirm();
                PaintControls();
                return;
            }
            if (!pad && token == "space")
            {
                _notice = "Space always jumps.";
                MenuAudio.Back();
                PaintControls();
                return;
            }
            if (pad)
            {
                ActionBinds table = PadBinds();
                string previous = table.Gamepad[(int)action] ?? "";
                table.SetGamepad(action, token);
                if ((table.GrapplePad ?? "") == token)
                {
                    table.SetGamepad(action, previous);
                    BeginGrappleSwap(action, true, token, false);
                    return;
                }
                if (table.Conflict(action, out PlayAction other))
                {
                    table.SetGamepad(action, previous);
                    BeginSwap(action, other, true, token);
                    return;
                }
                RememberPad(table);
                _notice = "";
                MenuAudio.Confirm();
                PaintControls();
                return;
            }
            ActionBinds keys = KeyboardBinds();
            if (!string.IsNullOrEmpty(keys.JumpAlt) && token == keys.JumpAlt)
            {
                BeginSwap(action, PlayAction.Jump, false, token);
                return;
            }
            if ((keys.GrappleKey ?? "") == token)
            {
                BeginGrappleSwap(action, false, token, false);
                return;
            }
            string old = keys.Keyboard[(int)action] ?? "";
            keys.SetKeyboard(action, token);
            if (keys.Conflict(action, out PlayAction clash))
            {
                keys.SetKeyboard(action, old);
                BeginSwap(action, clash, false, token);
                return;
            }
            _notice = "";
            SettingsRuntime.Save();
            MenuAudio.Confirm();
            PaintControls();
        }

        void BeginSwap(PlayAction action, PlayAction other, bool pad, string token)
        {
            _swapAction = (int)action;
            _swapOther = (int)other;
            _swapPad = pad;
            _swapToken = token ?? "";
            _swapPick = 0;
            _notice = "";
            MenuAudio.Back();
            PaintControls();
        }

        void CancelSwap()
        {
            ClearSwap();
            _notice = "";
            MenuAudio.Back();
            PaintControls();
        }

        void ClearSwap()
        {
            _swapAction = -1;
            _swapOther = -1;
            _swapGrapple = 0;
            _swapPad = false;
            _swapToken = "";
            _swapPick = 0;
        }

        void CommitSwap()
        {
            if (_swapGrapple != 0)
            {
                CommitGrappleSwap();
                return;
            }
            var action = (PlayAction)_swapAction;
            var other = (PlayAction)_swapOther;
            string token = _swapToken ?? "";
            bool pad = _swapPad;
            ClearSwap();
            if (pad)
            {
                ActionBinds table = PadBinds();
                string previous = table.Gamepad[(int)action] ?? "";
                table.SetGamepad(action, token);
                table.SetGamepad(other, previous);
                RememberPad(table);
                _notice = "";
            }
            else if (action == PlayAction.Jump)
            {
                ActionBinds kb = KeyboardBinds();
                string previousAlt = kb.JumpAlt ?? "";
                if ((kb.Keyboard[(int)PlayAction.Jump] ?? "") != "space")
                    kb.SetKeyboard(PlayAction.Jump, "space");
                kb.SetJumpAlt(token);
                if (previousAlt.Length > 0) kb.SetKeyboard(other, previousAlt);
                else kb.SetKeyboard(other, "");
                _notice = "Space always jumps. Added " + ActionBinds.Show(token) + " as a second Jump key.";
                SettingsRuntime.Save();
            }
            else if (other == PlayAction.Jump)
            {
                ActionBinds kb = KeyboardBinds();
                if ((kb.Keyboard[(int)PlayAction.Jump] ?? "") != "space")
                    kb.SetKeyboard(PlayAction.Jump, "space");
                kb.SetKeyboard(action, token);
                if ((kb.JumpAlt ?? "") == token) kb.SetJumpAlt("");
                _notice = "Space always jumps.";
                SettingsRuntime.Save();
            }
            else
            {
                ActionBinds kb = KeyboardBinds();
                string previous = kb.Keyboard[(int)action] ?? "";
                kb.SetKeyboard(action, token);
                kb.SetKeyboard(other, previous);
                _notice = "";
                SettingsRuntime.Save();
            }
            MenuAudio.Confirm();
            PaintControls();
        }

        static bool KeyboardClash(string token, PlayAction self, out PlayAction other)
        {
            other = self;
            if (string.IsNullOrEmpty(token) || ActionBinds.SharesMove(token)) return false;
            if (ActionBinds.Reserved(token)) return true;
            ActionBinds keys = ActionBinds.Current ?? ActionBinds.Defaults();
            for (int j = 0; j < (int)PlayAction.Count; j++)
            {
                if ((PlayAction)j == self) continue;
                string have = keys.Keyboard[j] ?? "";
                if (have.Length == 0 || have != token) continue;
                if (ActionBinds.SharesMove(have)) continue;
                other = (PlayAction)j;
                return true;
            }
            return false;
        }

        void TakeGrapple(string token, bool pad)
        {
            if (!pad && token == "space")
            {
                _notice = "Space always jumps.";
                MenuAudio.Back();
                PaintControls();
                return;
            }
            ActionBinds keys = KeyboardBinds();
            ActionBinds table = PadBinds();
            if (pad)
            {
                for (int j = 0; j < (int)PlayAction.Count; j++)
                {
                    string have = table.Gamepad[j] ?? "";
                    if (have.Length == 0 || have != token || ActionBinds.SharesMove(have)) continue;
                    BeginGrappleSwap((PlayAction)j, true, token, true);
                    return;
                }
                table.SetGrapplePad(token);
                RememberPad(table);
            }
            else
            {
                if (ActionBinds.Reserved(token))
                {
                    MenuAudio.Back();
                    PaintControls();
                    return;
                }
                if (!string.IsNullOrEmpty(keys.JumpAlt) && token == keys.JumpAlt)
                {
                    BeginGrappleSwap(PlayAction.Jump, false, token, true);
                    return;
                }
                for (int j = 0; j < (int)PlayAction.Count; j++)
                {
                    string have = keys.Keyboard[j] ?? "";
                    if (have.Length == 0 || have != token || ActionBinds.SharesMove(have)) continue;
                    BeginGrappleSwap((PlayAction)j, false, token, true);
                    return;
                }
                keys.SetGrappleKey(token);
                SettingsRuntime.Save();
            }
            _notice = "";
            MenuAudio.Confirm();
            PaintControls();
        }

        /// <summary>
        /// grappleReceives: the new token lands on Grapple and the other row keeps Grapple's old token.
        /// Otherwise the other row takes the token and Grapple keeps that row's old token.
        /// </summary>
        void BeginGrappleSwap(PlayAction other, bool pad, string token, bool grappleReceives)
        {
            _swapAction = (int)other;
            _swapOther = (int)other;
            _swapGrapple = grappleReceives ? 1 : 2;
            _swapPad = pad;
            _swapToken = token ?? "";
            _swapPick = 0;
            _notice = "";
            MenuAudio.Back();
            PaintControls();
        }

        void CommitGrappleSwap()
        {
            var other = (PlayAction)_swapAction;
            string token = _swapToken ?? "";
            bool pad = _swapPad;
            bool grappleReceives = _swapGrapple == 1;
            ClearSwap();
            if (pad)
            {
                ActionBinds table = PadBinds();
                string previous = table.GrapplePad ?? ActionBinds.GrapplePadDefault;
                string theirs = table.Gamepad[(int)other] ?? "";
                if (grappleReceives)
                {
                    table.SetGrapplePad(token);
                    table.SetGamepad(other, previous);
                }
                else
                {
                    table.SetGamepad(other, token);
                    table.SetGrapplePad(theirs.Length > 0 ? theirs : ActionBinds.GrapplePadDefault);
                }
                RememberPad(table);
            }
            else
            {
                ActionBinds kb = KeyboardBinds();
                string previous = kb.GrappleKey ?? ActionBinds.GrappleKeyDefault;
                if (grappleReceives)
                {
                    kb.SetGrappleKey(token);
                    if (other == PlayAction.Jump)
                    {
                        if ((kb.JumpAlt ?? "") == token) kb.SetJumpAlt(previous == "space" ? "" : previous);
                    }
                    else
                        kb.SetKeyboard(other, previous);
                }
                else if (other == PlayAction.Jump)
                {
                    if ((kb.Keyboard[(int)PlayAction.Jump] ?? "") != "space")
                        kb.SetKeyboard(PlayAction.Jump, "space");
                    kb.SetJumpAlt(token);
                    kb.SetGrappleKey(ActionBinds.GrappleKeyDefault);
                }
                else
                {
                    string theirs = kb.Keyboard[(int)other] ?? "";
                    kb.SetKeyboard(other, token);
                    kb.SetGrappleKey(theirs.Length > 0 ? theirs : ActionBinds.GrappleKeyDefault);
                }
                SettingsRuntime.Save();
            }
            _notice = "";
            MenuAudio.Confirm();
            PaintControls();
        }

        string SwapNames()
        {
            if (_swapGrapple != 0)
                return "Grapple and " + ActionBinds.Name((PlayAction)_swapAction);
            return ActionBinds.Name((PlayAction)_swapAction) + " and " + ActionBinds.Name((PlayAction)_swapOther);
        }

        int SwapIndex(int action0, int noteAt, bool low)
        {
            int grapple = noteAt;
            int other = _swapGrapple != 0 ? action0 + _swapAction : action0 + (_swapAction < _swapOther ? _swapAction : _swapOther);
            int hiAction = _swapGrapple != 0 ? other : action0 + (_swapAction > _swapOther ? _swapAction : _swapOther);
            if (_swapGrapple != 0)
            {
                int a = grapple < other ? grapple : other;
                int b = grapple > other ? grapple : other;
                return low ? a : b;
            }
            return low ? other : hiAction;
        }

        ActionBinds KeyboardBinds()
        {
            if (ActionBinds.Current == null)
                ActionBinds.Current = ActionBinds.Defaults();
            return ActionBinds.Current;
        }

        ActionBinds PadBinds()
        {
            int seat = _bindSeat;
            if (seat < 0) seat = 0;
            if (seat > 3) seat = 3;
            return CouchPlay.BindsFor(CouchPlay.DevicePad0 + seat);
        }

        void RememberPad(ActionBinds pad)
        {
            if (pad == null) return;
            ActionBinds owned = LocalProfiles.BindsForSeat(_bindSeat);
            if (owned != null && !ReferenceEquals(owned, pad))
            {
                for (int i = 0; i < (int)PlayAction.Count; i++)
                    owned.SetGamepad((PlayAction)i, pad.Gamepad[i]);
                owned.SetGrapplePad(pad.GrapplePad);
            }
            SettingsRuntime.Save();
        }

        void ResetSeatBinds()
        {
            ActionBinds fresh = ActionBinds.Defaults();
            ActionBinds kb = KeyboardBinds();
            for (int i = 0; i < (int)PlayAction.Count; i++)
                kb.SetKeyboard((PlayAction)i, fresh.Keyboard[i]);
            kb.SetJumpAlt("");
            kb.SetGrappleKey(fresh.GrappleKey);
            ActionBinds pad = PadBinds();
            for (int i = 0; i < (int)PlayAction.Count; i++)
                pad.SetGamepad((PlayAction)i, fresh.Gamepad[i]);
            pad.SetGrapplePad(fresh.GrapplePad);
            RememberPad(pad);
            ClearSwap();
        }

        static bool IsPad(string token)
        {
            return token == "buttonSouth" || token == "buttonEast" || token == "buttonWest"
                || token == "buttonNorth" || token == "leftShoulder" || token == "rightShoulder"
                || token == "leftStickPress" || token == "rightStickPress"
                || token == "leftTrigger" || token == "rightTrigger"
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
                MenuWidgets.Reflow(TileAt(c));
            }
            for (int s = 0; s < 4; s++)
            {
                bool human = CouchPlay.HumanAt(s);
                if (_castJoin[s] != null) _castJoin[s].gameObject.SetActive(!human);
                if (_castView[s] != null) _castView[s].gameObject.SetActive(human);
                if (_castGlyph[s] != null)
                {
                    Transform glyphRt = _castGlyph[s].transform;
                    if (glyphRt.parent != null)
                        glyphRt.parent.gameObject.SetActive(human);
                }
                if (_castWell[s] != null) _castWell[s].enabled = !human;
                int picked = MenuSession.Cursor[s];
                if (picked < 0) picked = 0;
                if (picked > 5) picked = 5;
                RectTransform ring = _swatchRing[s];
                RectTransform chip = _swatch[s * 6 + picked];
                if (ring != null && chip != null) ring.anchoredPosition = chip.anchoredPosition + new Vector2(-5f, 3f);
                if (!human)
                {
                    if (_readyBurst[s] != null) _readyBurst[s].gameObject.SetActive(false);
                    if (_castName[s] != null) _castName[s].text = "";
                    if (_castReady[s] != null) _castReady[s].text = "";
                    if (_castJoin[s] != null)
                    {
                        int family = MenuInput.LastKind == InputDeviceKind.Gamepad
                            ? PadGlyph.Family(MenuInput.LastDevice)
                            : PadGlyph.Keyboard;
                        string line = PadGlyph.Join(family);
                        if (_castJoin[s].text != line) _castJoin[s].text = line;
                    }
                    if (_castPlate[s] != null) _castPlate[s].color = MenuTheme.Seat(s);
                    continue;
                }
                if (_preview != null)
                {
                    _preview.Apply(s, MenuSession.Hier[s], MenuSession.Accent[s], MenuSession.Hat[s], _castView[s]);
                    _preview.SetReady(s, MenuSession.Ready[s]);
                }
                if (_castReady[s] == null) continue;
                string skin = MenuSession.Hier[s] >= 0 && MenuSession.Hier[s] < LocalProfiles.HierNames.Length
                    ? LocalProfiles.HierNames[MenuSession.Hier[s]] : "Tan";
                string trim = MenuSession.Accent[s] >= 0 && MenuSession.Accent[s] < LocalProfiles.HierNames.Length
                    ? LocalProfiles.HierNames[MenuSession.Accent[s]] : skin;
                string ready = MenuSession.Ready[s] ? "READY" : "Not ready";
                string hat = MenuSession.Hat[s] == 0 ? "Hat off" : "Hat on";
                UiFit.CastLines(CouchPlay.Name(s), skin, trim, hat, ready, UiFit.CastInk(UiFit.Current()), out string top, out string bot);
                if (_castName[s] != null && _castName[s].text != top) _castName[s].text = top;
                if (_castReady[s].text != bot) _castReady[s].text = bot;
                _castReady[s].color = MenuTheme.Cream;
                if (_castGlyph[s] != null)
                {
                    int dev = CouchPlay.DeviceOf(s);
                    int family = dev <= 0 ? PadGlyph.Keyboard : PadGlyph.Family(dev);
                    _castGlyph[s].sprite = family == PadGlyph.Keyboard
                        ? MenuIcons.Keys
                        : MenuIcons.ConfirmOf(family, FaceFor(dev, family));
                }
                if (_readyBurst[s] != null)
                    _readyBurst[s].gameObject.SetActive(MenuSession.Ready[s]);
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

        static int JoinSig()
        {
            int n = CouchPlay.Humans;
            for (int s = 0; s < 4; s++)
            {
                n = n * 17 + CouchPlay.DeviceOf(s) + 3;
                n = n * 13 + LocalProfiles.ProfileAt(s);
                if (MenuSession.Ready[s]) n += 1;
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

        void BuildRecords()
        {
            _count = LocalProfiles.Max + 1;
            _cols = 1;
            _window = 0;
            if (_header != null) _header.text = "  Records";
            if (_banner != null) _banner.text = "Matches, wins, tags, and longest time not It";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            PaintRecords();
        }

        void PaintRecords()
        {
            ClearKeepHeader();
            int filled = 0;
            for (int i = 0; i < LocalProfiles.Max; i++)
                if (LocalProfiles.SlotId(i) > 0) filled++;
            _count = filled == 0 ? 2 : filled + 1;
            float recH = UiFit.RowH(120f);
            float recStep = UiFit.RowStep(128f, 120f);
            int win = UiFit.Window(UiFit.Current(), recStep, 8f);
            if (_focus >= _count) _focus = _count - 1;
            if (_focus < 0) _focus = 0;
            if (_focus < _window) _window = _focus;
            if (_focus >= _window + win) _window = _focus - (win - 1);
            int max = _count - win;
            if (max < 0) max = 0;
            if (_window > max) _window = max;
            if (_window < 0) _window = 0;
            UiFit.RowBox(UiFit.Current(), 1100f, out float x, out float w);
            if (filled == 0)
            {
                MenuTile card = AddTile(x, 24f, w, 280f, 0, "No records yet.", "Play a match to set one.", true);
                if (card != null)
                {
                    card.LockColors = true;
                    card.Tint(MenuTheme.Navy);
                    if (card.Label != null)
                    {
                        card.Label.resizeTextForBestFit = false;
                        card.Label.fontSize = UiFit.TextPx(40);
                    }
                    if (card.Detail != null)
                    {
                        card.Detail.resizeTextForBestFit = false;
                        card.Detail.fontSize = UiFit.TextPx(UiFit.FloorFont);
                    }
                    MenuWidgets.SeatLine(card.Label, 280f, 78f, 48f, 190f);
                    MenuWidgets.SeatLine(card.Detail, 280f, 136f, 40f, 190f);
                    MenuWidgets.EmptyMark(card.transform, 36f, 78f, 140f);
                }
                AddTile(x, 320f, w, UiFit.RowH(UiFit.OptRow), 1, "Back", "", true);
                RefreshFocus();
                return;
            }
            int shown = 0;
            for (int i = 0; i < LocalProfiles.Max && shown < filled; i++)
            {
                int id = LocalProfiles.SlotId(i);
                if (id <= 0) continue;
                int index = shown;
                shown++;
                if (index < _window || index >= _window + win) continue;
                int v = index - _window;
                string title = LocalProfiles.NameOf(id);
                string detail = LocalProfiles.CardOf(id);
                string lead = title + "\n";
                if (!string.IsNullOrEmpty(detail) && detail.StartsWith(lead))
                    detail = detail.Substring(lead.Length);
                AddTile(x, 8f + v * recStep, w, recH, index, title, detail, true);
            }
            int back = filled;
            if (back >= _window && back < _window + win)
                AddTile(x, 8f + (back - _window) * recStep, w, recH, back, "Back", "", true);
            RefreshFocus();
        }

        void ActivateRecords()
        {
            if (_focus >= _count - 1)
            {
                MenuAudio.Back();
                GoBack();
                return;
            }
            MenuAudio.Move();
            PaintRecords();
        }

        void OpenName(int seat)
        {
            _naming = seat;
            LocalProfiles.PadClear();
            int id = LocalProfiles.ProfileAt(seat);
            if (id > 0)
            {
                string current = LocalProfiles.NameOf(id);
                if (!string.IsNullOrEmpty(current)) LocalProfiles.Spell(current);
            }
            for (int c = 0; c < 6; c++)
            {
                MenuTile tile = TileAt(c);
                if (tile != null) tile.gameObject.SetActive(false);
            }
            if (_keys != null) _keys.gameObject.SetActive(true);
            PaintKeys();
            if (_banner != null) _banner.text = "Name keys. OK saves. Back leaves the keys.";
            MenuAudio.Move();
        }

        void CloseName()
        {
            _naming = -1;
            if (_keys != null) _keys.gameObject.SetActive(false);
            for (int c = 0; c < 6; c++)
            {
                MenuTile tile = TileAt(c);
                if (tile != null) tile.gameObject.SetActive(true);
            }
            if (_banner != null) _banner.text = "Down from a color opens the name keys";
            RefreshCast();
        }

        void TickName(int seat, MenuEdge edge)
        {
            if (edge.Back)
            {
                MenuAudio.Back();
                CloseName();
                return;
            }
            if (edge.Y > 0 && LocalProfiles.NameRow == 0 && edge.X == 0)
            {
                MenuAudio.Back();
                CloseName();
                return;
            }
            if (edge.X != 0 || edge.Y != 0)
            {
                int dy = 0;
                if (edge.Y < 0) dy = 1;
                else if (edge.Y > 0) dy = -1;
                LocalProfiles.PadMove(edge.X, dy);
                MenuAudio.Move();
                PaintKeys();
            }
            if (!(edge.Confirm || edge.Start)) return;
            char cell = LocalProfiles.KeyAt(LocalProfiles.NameCol, LocalProfiles.NameRow);
            LocalProfiles.PadType();
            if (cell == '\n')
            {
                CommitName(seat);
                return;
            }
            MenuAudio.Move();
            PaintKeys();
        }

        void CommitName(int seat)
        {
            int id = LocalProfiles.ProfileAt(seat);
            bool saved = false;
            if (id > 0 && LocalProfiles.ArmRename(id))
                saved = LocalProfiles.RenameFromPad();
            else
            {
                int created = LocalProfiles.CreateFromPad();
                if (created > 0) saved = LocalProfiles.TrySeat(seat, created);
            }
            if (!saved)
            {
                if (_banner != null) _banner.text = "Type a name, then OK";
                MenuAudio.Error();
                PaintKeys();
                return;
            }
            MenuAudio.Confirm();
            CloseName();
        }

        void PaintKeys()
        {
            int hot = LocalProfiles.NameRow * LocalProfiles.Cols + LocalProfiles.NameCol;
            for (int i = 0; i < _keyPlate.Length; i++)
            {
                if (_keyPlate[i] == null) continue;
                bool on = i == hot;
                _keyPlate[i].color = on ? MenuTheme.Gold : MenuTheme.Panel;
                if (_keyWord[i] != null) _keyWord[i].color = on ? MenuTheme.Ink : MenuTheme.Cream;
            }
            if (_nameWord != null) _nameWord.text = LocalProfiles.PadText();
        }

        static string KeyWord(char cell)
        {
            if (cell == '\b') return "Del";
            if (cell == '\n') return "OK";
            if (cell == ' ') return "Space";
            if (cell == '\0') return "";
            return cell.ToString();
        }

        static string HandLine(GameSettings rules)
        {
            string line = "";
            int n = 0;
            int seats = GameSettings.SeatCount;
            for (int s = 0; s < seats; s++)
            {
                string word = RuleBook.LoadHand(rules, s);
                if (word == "Off") continue;
                if (n > 0) line = line + "  ";
                line = line + "P" + (s + 1).ToString() + " " + word;
                n++;
            }
            if (n == 0) return "none";
            return line;
        }

        static string ConfirmTitle(int seat)
        {
            if (seat == 1) return "P2 confirm";
            if (seat == 2) return "P3 confirm";
            if (seat == 3) return "P4 confirm";
            return "P1 confirm";
        }

        const int ContextNotes = 3;

        static void ControlBands(out int action0, out int noteAt, out int stickAt, out int confirmAt, out int barAt)
        {
            action0 = 1;
            noteAt = action0 + ShownActions();
            stickAt = noteAt + ContextNotes;
            confirmAt = stickAt + MenuStick.Rows;
            barAt = confirmAt + GameSettings.SeatCount;
        }

        /// <summary>Arena 1/2/3 stay in the bind table. The player list hides them outside a dev build.</summary>
        static int ShownActions()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return (int)PlayAction.Count;
#else
            return (int)PlayAction.Arena1;
#endif
        }

        static int ControlCount()
        {
            ControlBands(out _, out _, out _, out _, out int barAt);
            return barAt + 1;
        }

        /// <summary>
        /// Keyboard words are what PlayerInputReader samples on the solo pawn.
        /// Pad words are Xbox names for the same gamepad token. Cling stays the move hold.
        /// </summary>
        static string ActionDetail(PlayAction action, ActionBinds binds)
        {
            if (action == PlayAction.Cling)
                return "Wall climb and wall run need this hold. Wall jump is this hold plus Jump.";
            if (action == PlayAction.Jump && binds != null && !string.IsNullOrEmpty(binds.JumpAlt))
                return "Space always jumps.";
            return "";
        }

        static string PausePlace()
        {
            TagModeController mode = TagModeController.Instance;
            TagModeId id = mode != null ? mode.SelectedMode : MenuSession.Mode;
            int arena = ParkArena.Id;
            if (arena < 0 || arena >= ParkArena.Count) arena = MenuSession.Arena;
            return ParkArena.NameOf(arena) + "  ·  " + MenuCatalog.ModeName(id);
        }

        static string NoteTitle(int note)
        {
            if (note == 0) return "Grapple";
            if (note == 1) return "Zip";
            return "Launch pad";
        }

        static string NoteDetail(int note)
        {
            if (note == 0)
            {
                return ExperimentalGrapple.FireButton
                    + ". Press pulls. Second press within 0.28 s releases. Left hand.";
            }
            if (note == 1)
                return "Hold cling to grab. Jump to drop.";
            return "Walk on. No button.";
        }

        static int FaceFor(int device, int family)
        {
            int over = GameSettings.FaceAuto;
            GameSettings settings = GameSettings.Current;
            if (settings != null)
            {
                for (int seat = 0; seat < GameSettings.SeatCount; seat++)
                {
                    if (!CouchPlay.HumanAt(seat)) continue;
                    if (CouchPlay.DeviceOf(seat) != device) continue;
                    over = settings.ConfirmFace[seat];
                    break;
                }
            }
            return FaceMap.Resolve(family, over);
        }
    }
}
