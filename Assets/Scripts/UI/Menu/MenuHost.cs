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
        int _loadStep = -1;
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
        float _travel = 1f;
        bool _enterBack;
        RawImage _arenaShot;
        Text _arenaShotName;
        Text _arenaShotBlurb;
        readonly Image[] _glyphChip = new Image[4];
        readonly Image[] _glyphIcon = new Image[4];
        readonly Text[] _glyphWord = new Text[4];
        Image _promptBar;
        readonly Image[] _paneBar = new Image[4];
        readonly Image[] _paneChip = new Image[16];
        readonly Image[] _paneIcon = new Image[16];
        readonly Text[] _paneWord = new Text[16];
        readonly int[] _seatDevice = { -1, -1, -1, -1 };
        readonly Image[] _castStat = new Image[4];
        readonly Image[] _castNamePlate = new Image[4];
        Text _seatLegend;
        readonly Text[] _castPrev = new Text[4];
        readonly Text[] _castNext = new Text[4];
        readonly Image[] _swatchImage = new Image[24];
        readonly Image[] _swatchLock = new Image[24];
        static readonly string[] PromptLabel = { "Move", "Confirm", "Back", "Hat" };
        float _chrome = UiFit.ChromeTop;
        int _bindRev = -1;
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
        bool _quitAsk;
        int _quitPick;
        int _pauseOwner;
        int _turnSeat = -1;
        float _turnAt;
        float _pauseX, _pauseY, _pauseW, _pauseH;
        RectTransform _quitModal;
        Image _quitNoPlate;
        Image _quitYesPlate;

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
            int bindRev = ActionBinds.Current != null ? ActionBinds.Current.Revision : 0;
            bool seatChanged = false;
            for (int i = 0; i < MenuInput.Count; i++)
            {
                int seat = SeatOf(MenuInput.Edges[i].Device);
                if (seat < 0) continue;
                if (_seatDevice[seat] == MenuInput.Edges[i].Device) continue;
                _seatDevice[seat] = MenuInput.Edges[i].Device;
                seatChanged = true;
            }
            if (_footerKind != MenuInput.LastKind || bindRev != _bindRev || seatChanged)
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
            CouchPlay.Release();
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
            _travel = _enterBack ? -1f : 1f;
            _enterBack = false;
            HideFlyover();
            if (_group != null) _group.alpha = _fade;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ClearBody();
            if (_seatLegend != null) _seatLegend.gameObject.SetActive(id == MenuScreenId.Cast);
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
            RefreshFocus();
            PaintFooter();
            bool title = id == MenuScreenId.Title;
            bool photo = title || id == MenuScreenId.Main;
            if (_vignette != null) _vignette.SetActive(photo);
            if (_pattern != null)
            {
                Color wash = _pattern.color;
                wash.a = photo ? 0f : 0.22f;
                _pattern.color = wash;
            }
            for (int i = 0; i < _ribbons.Length; i++)
            {
                if (_ribbons[i] != null) _ribbons[i].gameObject.SetActive(!title);
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
            headerRt.sizeDelta = new Vector2(0f, 64f);
            headerRt.anchoredPosition = new Vector2(0f, -UiFit.HeaderTop(UiFit.RefH));

            _seatLegend = MenuWidgets.Words(root, "P colour = controller seat", UiFit.FloorFont, TextAnchor.MiddleRight, MenuTheme.Cream, new Vector2(0f, 1f), new Vector2(1f, 1f));
            _seatLegend.raycastTarget = false;
            RectTransform legendRt = _seatLegend.rectTransform;
            legendRt.anchorMin = new Vector2(0f, 1f);
            legendRt.anchorMax = new Vector2(1f, 1f);
            legendRt.pivot = new Vector2(0.5f, 1f);
            legendRt.sizeDelta = new Vector2(0f, 64f);
            legendRt.anchoredPosition = new Vector2(0f, -UiFit.HeaderTop(UiFit.RefH));
            Vector2 legendMin = legendRt.offsetMin;
            Vector2 legendMax = legendRt.offsetMax;
            legendMin.x = UiFit.SafeX;
            legendMax.x = -UiFit.SafeX;
            legendRt.offsetMin = legendMin;
            legendRt.offsetMax = legendMax;
            _seatLegend.gameObject.SetActive(false);

            _body = MenuWidgets.Box(root, "Body", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            _body.offsetMin = new Vector2(UiFit.SafeX, 78f);
            _body.offsetMax = new Vector2(-UiFit.SafeX, -UiFit.ChromeTop);
            PlaceChrome();

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
            ClearPaneBars();
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
                _castNamePlate[i] = null;
                _castPrev[i] = null;
                _castNext[i] = null;
                _readyBurst[i] = null;
                _readyPop[i] = 0f;
                _castGlyph[i] = null;
                _castWell[i] = null;
                _swatchRing[i] = null;
            }
            for (int i = 0; i < _swatch.Length; i++)
            {
                _swatch[i] = null;
                _swatchImage[i] = null;
                _swatchLock[i] = null;
            }
            for (int i = 0; i < _loadRule.Length; i++) _loadRule[i] = null;
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
            if (tile != null)
            {
                if (MenuFlow.Feel(_screen)) tile.PopSelect();
                else tile.PunchIn();
            }
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
            int chosen = ChosenIndex();
            for (int i = 0; i < _tiles.Count; i++)
            {
                MenuTile tile = _tiles[i];
                if (tile == null) continue;
                tile.SetChosen(tile.Index == chosen);
                tile.SetHot(tile.Index == _focus);
            }
        }

        int ChosenIndex()
        {
            if (_screen == MenuScreenId.Rules)
            {
                int mode = (int)MenuSession.Mode;
                if (mode >= 0 && mode <= 3) return mode;
            }
            if (_screen == MenuScreenId.Arena)
            {
                if (MenuSession.RandomArena) return 3;
                if (MenuSession.Arena >= 0 && MenuSession.Arena < ParkArena.Count) return MenuSession.Arena;
            }
            return -1;
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

        bool HasTravelInput()
        {
            if (MenuInput.AnyAdvance()) return true;
            for (int i = 0; i < MenuInput.Count; i++)
            {
                MenuEdge edge = MenuInput.Edges[i];
                if (edge.X != 0 || edge.Y != 0 || edge.Confirm || edge.Back || edge.Start || edge.Join)
                    return true;
            }
            return false;
        }

        bool SkipTravel()
        {
            if (!MenuFlow.Feel(_screen)) return false;
            if (MenuCapture.Running) return false;
            if (!Gated()) return false;
            if (!HasTravelInput()) return false;
            _slide = 0f;
            _fade = 1f;
            _gate = Time.unscaledTime;
            if (_group != null) _group.alpha = 1f;
            return true;
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
            if (Gated() && !SkipTravel()) return;
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
            _enterBack = true;
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
            var bar = MenuWidgets.Place(root, "PromptBar", 0f, 0f, 1920f, 48f);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta = new Vector2(0f, 48f);
            var barImage = bar.gameObject.AddComponent<Image>();
            barImage.color = new Color(0.02f, 0.05f, 0.12f, 0.94f);
            barImage.raycastTarget = false;
            _promptBar = barImage;
            for (int i = 0; i < PromptLabel.Length; i++)
                BuildChip(bar, "Glyph" + i.ToString(), _glyphChip, _glyphIcon, _glyphWord, i);
            PlaceGlyphs();
        }

        static void BuildChip(RectTransform parent, string name, Image[] chips, Image[] icons, Text[] words, int index)
        {
            var chip = MenuWidgets.Place(parent, name, 0f, 6f, 168f, 36f);
            var image = chip.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;
            chips[index] = image;
            var iconRt = MenuWidgets.Place(chip, "Icon", 0f, 4f, 28f, 28f);
            var icon = iconRt.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icons[index] = icon;
            words[index] = MenuWidgets.Words(chip, "", UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Cream, Vector2.zero, Vector2.one);
            words[index].raycastTarget = false;
            words[index].rectTransform.offsetMin = new Vector2(34f, 0f);
            words[index].rectTransform.offsetMax = new Vector2(-2f, 0f);
        }

        void PlaceGlyphs()
        {
            if (_promptBar == null) return;
            RectTransform bar = _promptBar.rectTransform;
            float rw = bar.rect.width;
            if (rw < 8f) UiFit.Ref(UiFit.Current(), out rw, out _);
            const float gap = 28f;
            const float w = 168f;
            const float h = 36f;
            int shown = 0;
            for (int i = 0; i < _glyphChip.Length; i++)
            {
                if (_glyphChip[i] != null && _glyphChip[i].enabled) shown++;
            }
            if (shown < 1) shown = 1;
            float total = w * shown + gap * (shown - 1);
            float x0 = (rw - total) * 0.5f;
            if (x0 < 16f) x0 = 16f;
            int place = 0;
            for (int i = 0; i < _glyphChip.Length; i++)
            {
                if (_glyphChip[i] == null || !_glyphChip[i].enabled) continue;
                RectTransform rt = _glyphChip[i].rectTransform;
                rt.anchoredPosition = new Vector2(x0 + place * (w + gap), -6f);
                rt.sizeDelta = new Vector2(w, h);
                place++;
            }
            PlacePaneBars();
        }

        void PlacePaneBars()
        {
            int humans = MenuSplitPause.Preview > 0 ? MenuSplitPause.Preview : CouchPlay.Humans;
            if (humans < 2) return;
            int split = GameSettings.SplitVertical;
            if (GameSettings.Current != null) split = GameSettings.Current.SplitAxis;
            UiFit.Ref(UiFit.Current(), out float rw, out float rh);
            int panes = CouchPlay.Panes(humans);
            for (int i = 0; i < panes && i < 4; i++)
            {
                if (_paneBar[i] == null) continue;
                CouchPlay.View view = CouchPlay.Pane(i, humans, 1f, 1f, split);
                if (view.Score)
                {
                    _paneBar[i].enabled = false;
                    continue;
                }
                float w = view.W * rw - 16f;
                if (w < 120f) w = 120f;
                float x = view.X * rw + 8f;
                float y = rh - view.Y * rh - 40f;
                if (y < 0f) y = 0f;
                RectTransform rt = _paneBar[i].rectTransform;
                rt.anchoredPosition = new Vector2(x, -y);
                rt.sizeDelta = new Vector2(w, 40f);
                _paneBar[i].enabled = true;
                LayoutPaneChips(i, w);
            }
        }

        void LayoutPaneChips(int seat, float width)
        {
            const float gap = 8f;
            const float h = 32f;
            int shown = 0;
            for (int i = 0; i < 4; i++)
            {
                int at = seat * 4 + i;
                if (_paneIcon[at] != null && _paneIcon[at].enabled) shown++;
            }
            if (shown < 1) shown = 3;
            float w = (width - 12f - gap * (shown - 1)) / shown;
            if (w > 200f) w = 200f;
            if (w < 96f) w = 96f;
            int place = 0;
            for (int i = 0; i < 4; i++)
            {
                int at = seat * 4 + i;
                if (_paneIcon[at] == null || _paneIcon[at].transform.parent == null) continue;
                if (!_paneIcon[at].enabled && (_paneWord[at] == null || !_paneWord[at].enabled)) continue;
                RectTransform chip = _paneIcon[at].transform.parent as RectTransform;
                if (chip == null) continue;
                chip.anchoredPosition = new Vector2(8f + place * (w + gap), -4f);
                chip.sizeDelta = new Vector2(w, h);
                place++;
            }
        }

        void BuildPanePrompts()
        {
            ClearPaneBars();
            int humans = MenuSplitPause.Preview > 0 ? MenuSplitPause.Preview : CouchPlay.Humans;
            if (_screen != MenuScreenId.Pause || humans < 2) return;
            Transform root = _promptBar != null ? _promptBar.transform.parent : transform;
            int panes = CouchPlay.Panes(humans);
            for (int i = 0; i < panes && i < 4; i++)
            {
                var bar = MenuWidgets.Place(root, "PanePrompt" + i.ToString(), 0f, 0f, 400f, 40f);
                var image = bar.gameObject.AddComponent<Image>();
                image.color = new Color(0.02f, 0.05f, 0.12f, 0.94f);
                image.raycastTarget = false;
                _paneBar[i] = image;
                for (int slot = 0; slot < 4; slot++)
                    BuildChip(bar, "Chip" + slot.ToString(), _paneChip, _paneIcon, _paneWord, i * 4 + slot);
            }
        }

        void ClearPaneBars()
        {
            for (int i = 0; i < _paneBar.Length; i++)
            {
                if (_paneBar[i] != null)
                    Destroy(_paneBar[i].gameObject);
                _paneBar[i] = null;
            }
            for (int i = 0; i < _paneIcon.Length; i++)
            {
                _paneChip[i] = null;
                _paneIcon[i] = null;
                _paneWord[i] = null;
            }
        }

        void ApplyScale()
        {
            float s = UiFit.Current();
            if (_scaler == null || s == _uiScale) return;
            _uiScale = s;
            UiFit.Ref(s, out float w, out float h);
            _scaler.referenceResolution = new Vector2(w, h);
            PlaceChrome();
            PlaceGlyphs();
        }

        void PlaceChrome()
        {
            UiFit.Ref(UiFit.Current(), out _, out float rh);
            float top = UiFit.HeaderTop(rh);
            const float band = 64f;
            if (_header != null)
            {
                RectTransform headerRt = _header.rectTransform;
                headerRt.anchoredPosition = new Vector2(0f, -top);
                headerRt.sizeDelta = new Vector2(0f, band);
                Vector2 min = headerRt.offsetMin;
                Vector2 max = headerRt.offsetMax;
                min.x = UiFit.SafeX;
                max.x = -UiFit.SafeX;
                headerRt.offsetMin = min;
                headerRt.offsetMax = max;
            }
            if (_seatLegend != null)
            {
                RectTransform legendRt = _seatLegend.rectTransform;
                legendRt.anchoredPosition = new Vector2(0f, -top);
                legendRt.sizeDelta = new Vector2(0f, band);
                Vector2 legendMin = legendRt.offsetMin;
                Vector2 legendMax = legendRt.offsetMax;
                legendMin.x = UiFit.SafeX;
                legendMax.x = -UiFit.SafeX;
                legendRt.offsetMin = legendMin;
                legendRt.offsetMax = legendMax;
            }
            float chrome = top + band;
            if (chrome < UiFit.ChromeTop) chrome = UiFit.ChromeTop;
            _chrome = chrome;
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
            _footer.text = "";
            _bindRev = ActionBinds.Current != null ? ActionBinds.Current.Revision : 0;
            bool hints = _screen != MenuScreenId.Hidden;
            bool panes = _paneBar[0] != null || _paneBar[1] != null;
            bool keyboard = family == PadGlyph.Keyboard;
            PromptTokens(keyboard, face, true, out string move, out string confirm, out string back, out string extra);
            string[] tokens = { move, confirm, back, extra };
            if (_promptBar != null) _promptBar.enabled = hints && !panes;
            for (int i = 0; i < _glyphWord.Length; i++)
            {
                bool on = hints && !panes && !string.IsNullOrEmpty(tokens[i]);
                if (_glyphWord[i] != null)
                {
                    _glyphWord[i].enabled = on;
                    if (on) _glyphWord[i].text = PromptLabel[i];
                }
                if (_glyphChip[i] != null) _glyphChip[i].enabled = on;
                if (_glyphIcon[i] != null)
                {
                    _glyphIcon[i].enabled = on;
                    if (on) _glyphIcon[i].sprite = MenuIcons.ForToken(family, tokens[i]);
                }
            }
            if (_startGlyph != null && !keyboard)
                _startGlyph.sprite = MenuIcons.ForToken(family, confirm);
            for (int seat = 0; seat < 4; seat++)
                PaintPaneBar(seat);
            LayoutPrompt(keyboard);
            PlaceGlyphs();
        }

        void PaintPaneBar(int seat)
        {
            if (seat < 0 || seat > 3 || _paneBar[seat] == null) return;
            int dev = DeviceFor(seat);
            int family = dev <= 0 ? PadGlyph.Keyboard : PadGlyph.Family(dev);
            int face = FaceFor(dev, family);
            bool keyboard = family == PadGlyph.Keyboard;
            PromptTokens(keyboard, face, false, out string move, out string confirm, out string back, out string extra);
            string[] tokens = { move, confirm, back, extra };
            for (int i = 0; i < 4; i++)
            {
                int at = seat * 4 + i;
                bool on = !string.IsNullOrEmpty(tokens[i]);
                if (_paneWord[at] != null)
                {
                    _paneWord[at].enabled = on;
                    if (on) _paneWord[at].text = PromptLabel[i];
                }
                if (_paneIcon[at] != null)
                {
                    _paneIcon[at].enabled = on;
                    if (on) _paneIcon[at].sprite = MenuIcons.ForToken(family, tokens[i]);
                }
            }
        }

        int DeviceFor(int seat)
        {
            if (seat < 0 || seat > 3) return CouchPlay.DeviceKeyboard;
            int dev = _seatDevice[seat];
            if (dev < 0) dev = CouchPlay.DeviceOf(seat);
            if (dev < 0) dev = CouchPlay.DeviceKeyboard;
            return dev;
        }

        void PromptTokens(bool keyboard, int face, bool screenRules, out string move, out string confirm, out string back, out string extra)
        {
            ActionBinds binds = ActionBinds.Current;
            if (binds == null) binds = ActionBinds.Defaults();
            int moveI = (int)PlayAction.Move;
            int jumpI = (int)PlayAction.Jump;
            int pauseI = (int)PlayAction.Pause;
            if (keyboard)
            {
                move = binds.Keyboard[moveI];
                confirm = binds.Keyboard[jumpI];
                back = binds.Keyboard[pauseI];
            }
            else
            {
                move = binds.Gamepad[moveI];
                confirm = face == FaceMap.East ? "buttonEast" : "buttonSouth";
                back = face == FaceMap.East ? "buttonSouth" : "buttonEast";
            }
            extra = "";
            if (!screenRules) return;
            if (_screen == MenuScreenId.Cast)
                extra = keyboard ? "r" : "buttonNorth";
            if (_screen == MenuScreenId.Title)
            {
                move = "";
                back = "";
            }
            if (_screen == MenuScreenId.Loading)
            {
                move = "";
                confirm = "";
                extra = "";
            }
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

        static void Pull(Text label, float x)
        {
            if (label == null) return;
            RectTransform rt = label.rectTransform;
            Vector2 min = rt.offsetMin;
            if (min.x < x) min.x = x;
            rt.offsetMin = min;
        }

        static void SeatChip(Transform parent, float x, float y, int seat)
        {
            SeatChip(parent, x, y, seat, MenuTheme.Seat(seat), false);
        }

        static void SeatChip(Transform parent, float x, float y, int seat, Color plateColor, bool shape)
        {
            if (parent == null) return;
            if (seat < 0) seat = 0;
            if (seat > 3) seat = 3;
            float w = shape ? 118f : 68f;
            var rt = MenuWidgets.Place(parent, "SeatTag", x, y, w, 36f);
            var plate = rt.gameObject.AddComponent<Image>();
            MenuArt.Plate(plate, plateColor, true);
            plate.raycastTarget = false;
            string mark = shape ? AccessibilityPalette.Glyph(seat) + " " : "";
            Text word = MenuWidgets.Words(rt, mark + "P" + (seat + 1).ToString(), UiFit.FloorFont, TextAnchor.MiddleCenter, MenuTheme.Ink, Vector2.zero, Vector2.one);
            Snug(word);
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
                bool feel = MenuFlow.Feel(_screen) && !MenuVideo.ReduceMotion && !MenuCapture.Running;
                if (feel)
                {
                    MenuFlow.Travel(_slide, _travel, out float off, out float scale);
                    _body.offsetMin = new Vector2(UiFit.SafeX + off, 78f);
                    _body.offsetMax = new Vector2(-UiFit.SafeX + off, -_chrome);
                    _body.localScale = new Vector3(scale, scale, 1f);
                }
                else
                {
                    float slide = MenuVideo.ReduceMotion ? 0f : 160f * _slide;
                    _body.offsetMin = new Vector2(UiFit.SafeX + slide, 78f);
                    _body.offsetMax = new Vector2(-UiFit.SafeX + slide, -_chrome);
                    _body.localScale = Vector3.one;
                }
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
            if (Gated() && !SkipTravel()) return;
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
            if (!Gated() || SkipTravel())
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
                        int next = y * 3 + x;
                        int guard = 0;
                        while (LookTaken(seat, next) && guard < 6)
                        {
                            if (edge.X != 0) x = (x + (edge.X > 0 ? 1 : -1) + 3) % 3;
                            else y = (y + (edge.Y > 0 ? -1 : 1) + 2) % 2;
                            next = y * 3 + x;
                            guard++;
                        }
                        if (!LookTaken(seat, next) && next != cursor)
                        {
                            MenuSession.Cursor[seat] = next;
                            MenuSession.Hier[seat] = next;
                            MenuSession.Ready[seat] = false;
                            MenuAudio.Move();
                        }
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
                        if (picked != null)
                        {
                            if (MenuSession.Ready[seat]) picked.PopSelect();
                            else picked.PunchIn();
                        }
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
            if (Gated() && !SkipTravel()) return;
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
            if (Gated() && !SkipTravel()) return;
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
            int dx;
            int dy;
            bool confirm;
            bool back;
            bool start;
            int ask;
            ReadPause(_pauseOwner, out dx, out dy, out confirm, out back, out start, out ask);
            if (ask >= 0) NoteTurn(ask);
            if (TurnDue())
            {
                GiveTurn();
                return;
            }
            if (start)
            {
                EatPause = true;
                if (MenuFlow.Disconnected) return;
                ResumeMatch();
                return;
            }
            if (_quitAsk)
            {
                if (back)
                {
                    ClearQuitAsk();
                    return;
                }
                if (dx != 0 || dy != 0)
                {
                    _quitPick = (dx > 0 || dy > 0) ? 1 : 0;
                    PaintQuit();
                }
                if (confirm) ConfirmQuit();
                return;
            }
            if (back)
            {
                EatPause = true;
                if (MenuFlow.Disconnected) return;
                ResumeMatch();
                return;
            }
            if (dx != 0 || dy != 0) Move(dx, dy);
            if (confirm) ArmActivate();
        }

        void ReadPause(int owner, out int dx, out int dy, out bool confirm, out bool back, out bool start, out int ask)
        {
            dx = 0;
            dy = 0;
            confirm = false;
            back = false;
            start = false;
            ask = -1;
            for (int i = 0; i < MenuInput.Count; i++)
            {
                MenuEdge edge = MenuInput.Edges[i];
                int seat = SeatOf(edge.Device);
                if (seat == owner)
                {
                    if (edge.X != 0) dx = edge.X;
                    if (edge.Y != 0) dy = edge.Y;
                    if (edge.Confirm) confirm = true;
                    if (edge.Back) back = true;
                    if (edge.Start) start = true;
                }
                else if (edge.Start && seat >= 0 && ask < 0)
                    ask = seat;
            }
        }

        void NoteTurn(int seat)
        {
            if (seat < 0 || seat == _pauseOwner) return;
            if (_turnSeat >= 0) return;
            _turnSeat = seat;
            _turnAt = Time.unscaledTime;
        }

        bool TurnDue()
        {
            if (_turnSeat < 0) return false;
            return Time.unscaledTime >= _turnAt + 2f;
        }

        void GiveTurn()
        {
            int seat = _turnSeat;
            _turnSeat = -1;
            if (seat < 0) return;
            if (GameSettings.Current != null) GameSettings.Current.AccessSeat = seat;
            _pauseOwner = seat;
            ClearBody();
            BuildPause();
            RefreshFocus();
            PaintFooter();
        }

        void ClearQuitAsk()
        {
            _quitAsk = false;
            _quitPick = 0;
            if (_quitModal != null)
            {
                Destroy(_quitModal.gameObject);
                _quitModal = null;
                _quitNoPlate = null;
                _quitYesPlate = null;
            }
            for (int i = 0; i < _tiles.Count; i++)
            {
                MenuTile tile = _tiles[i];
                if (tile == null || tile.Index != 3 || tile.Detail == null) continue;
                tile.Detail.text = "";
            }
            if (_banner != null && _screen == MenuScreenId.Pause)
                _banner.text = Tag.Ui.Hud.MatchHudText.ComicHint;
        }

        void PaintQuit()
        {
            if (_quitNoPlate != null)
                _quitNoPlate.color = _quitPick == 0 ? MenuTheme.Gold : new Color(0.16f, 0.22f, 0.34f, 1f);
            if (_quitYesPlate != null)
                _quitYesPlate.color = _quitPick == 1 ? MenuTheme.Gold : new Color(0.16f, 0.22f, 0.34f, 1f);
        }

        void ConfirmQuit()
        {
            if (_quitPick == 0)
            {
                MenuAudio.Back();
                ClearQuitAsk();
                return;
            }
            EatPause = true;
            MenuAudio.Back();
            _quitAsk = false;
            QuitMatch();
        }

        void ShowQuitModal()
        {
            if (_quitModal != null) return;
            _quitAsk = true;
            _quitPick = 0;
            MenuAudio.Confirm();
            float w = 440f;
            float h = 210f;
            float x = _pauseX + (_pauseW - w) * 0.5f;
            float y = _pauseY + (_pauseH - h) * 0.5f;
            if (x < 12f) x = 12f;
            if (y < 12f) y = 12f;
            var plate = MenuWidgets.Place(_body, "QuitModal", x, y, w, h);
            var image = plate.gameObject.AddComponent<Image>();
            image.color = new Color(0.04f, 0.08f, 0.16f, 0.96f);
            image.raycastTarget = false;
            _quitModal = plate;
            MenuWidgets.Words(plate, "Leave the match?", UiFit.FloorFont, TextAnchor.MiddleCenter, MenuTheme.Cream, new Vector2(0.06f, 0.52f), new Vector2(0.94f, 0.92f));
            float bw = 160f;
            float bh = 64f;
            float gap = 18f;
            float total = bw * 2f + gap;
            float bx = (w - total) * 0.5f;
            var no = MenuWidgets.Place(plate, "QuitNo", bx, 28f, bw, bh);
            _quitNoPlate = no.gameObject.AddComponent<Image>();
            _quitNoPlate.raycastTarget = false;
            MenuWidgets.Words(no, "No", 26, TextAnchor.MiddleCenter, MenuTheme.Ink, Vector2.zero, Vector2.one);
            var yes = MenuWidgets.Place(plate, "QuitYes", bx + bw + gap, 28f, bw, bh);
            _quitYesPlate = yes.gameObject.AddComponent<Image>();
            _quitYesPlate.raycastTarget = false;
            MenuWidgets.Words(yes, "Yes", 26, TextAnchor.MiddleCenter, MenuTheme.Cream, Vector2.zero, Vector2.one);
            PaintQuit();
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
            if (dx != 0)
            {
                ControlBands(out _, out _, out int stickAt, out int confirmAt, out int resetAt);
                int stick = _focus - stickAt;
                if (stick >= 0 && stick < MenuStick.Rows && MenuStick.Nudge(stick, dx))
                {
                    MenuAudio.Move();
                    UnlockLooks();
                    PaintControls();
                }
                else if (_focus >= confirmAt && _focus < resetAt)
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
                    int span = UiFit.Window(UiFit.Current(), 128f, 8f);
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
            float y = 8f;
            AddTile(tileX, y, tileW, 96f, 0, "Play", "Local couch", true); y += 108f;
            AddTile(tileX, y, tileW, 96f, 1, "Practice", "Free run any arena, no tagger", true); y += 108f;
            AddTile(tileX, y, tileW, 96f, 2, "Options", "Sound, picture, access", true); y += 108f;
            AddTile(tileX, y, tileW, 96f, 3, "Controls", "Binds. Space still jumps.", true); y += 108f;
            float gap = 12f;
            float btn = (tileW - gap * 2f) / 3f;
            AddTile(tileX, y, btn, 96f, 4, "Credits", "", true);
            AddTile(tileX + btn + gap, y, btn, 96f, 6, "Records", "Profiles", true);
            AddTile(tileX + (btn + gap) * 2f, y, btn, 96f, 5, "Quit", "", true);
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
                float span = UiFit.BodyW(UiFit.Current());
                float cardW = (span - 16f * 5f) / 4f;
                if (cardW > 428f) cardW = 428f;
                if (cardW < 180f) cardW = 180f;
                float x = 16f + s * (cardW + 16f);
                float cardH = 420f;
                float bodyH = UiFit.BodyH(UiFit.Current());
                if (36f + cardH > bodyH) cardH = bodyH - 48f;
                AddTile(x, 24f, cardW, cardH, s, title, detail, true);
                MenuTile tile = TileAt(s);
                int family = PadGlyph.Generic;
                if (human)
                {
                    int dev = CouchPlay.DeviceOf(s);
                    family = dev <= 0 ? PadGlyph.Keyboard : PadGlyph.Family(dev);
                }
                Sprite mark = MenuIcons.Either;
                if (human) mark = family == PadGlyph.Keyboard ? MenuIcons.Keys : MenuIcons.ConfirmOf(family, FaceFor(CouchPlay.DeviceOf(s), family));
                MenuWidgets.Glyph(tile, mark, MenuTheme.Seat(s));
                if (!human && tile != null && tile.Detail != null)
                    tile.Detail.text = PadGlyph.Join(PadGlyph.Generic);
                if (tile != null)
                {
                    tile.KeepBar = true;
                    tile.BarColor = MenuTheme.Seat(s);
                    if (tile.Bar != null) tile.Bar.color = tile.BarColor;
                    tile.Tint(Color.Lerp(MenuTheme.Ink, MenuTheme.Seat(s), UiSweep.SeatMix));
                }
            }
            _count = 4;
            if (_banner != null)
            {
                int bannerFamily = MenuInput.LastKind == InputDeviceKind.Gamepad
                    ? PadGlyph.Family(MenuInput.LastDevice)
                    : PadGlyph.Keyboard;
                int bannerDevice = MenuInput.LastKind == InputDeviceKind.Gamepad ? MenuInput.LastDevice : CouchPlay.DeviceKeyboard;
                _banner.text = CouchPlay.Humans > 0
                    ? PadGlyph.Continue(bannerFamily, FaceFor(bannerDevice, bannerFamily))
                    : "Anyone can join";
            }
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
                float textH = UiFit.CastNameH + UiFit.CastStatusH;
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
                wellImage.sprite = MenuPreview.WellSprite();
                wellImage.type = Image.Type.Simple;
                wellImage.color = Color.white;
                wellImage.raycastTarget = false;
                _castWell[s] = wellImage;
                well.SetSiblingIndex(viewRt.GetSiblingIndex());
                var prevRt = MenuWidgets.Place(card, "LookPrev", viewX - 8f, 10f + viewH * 0.38f, 36f, 40f);
                _castPrev[s] = MenuWidgets.Words(prevRt, "<", UiFit.FloorFont, TextAnchor.MiddleCenter, MenuTheme.Cream, Vector2.zero, Vector2.one);
                _castPrev[s].raycastTarget = false;
                var nextRt = MenuWidgets.Place(card, "LookNext", viewX + viewSide - 28f, 10f + viewH * 0.38f, 36f, 40f);
                _castNext[s] = MenuWidgets.Words(nextRt, ">", UiFit.FloorFont, TextAnchor.MiddleCenter, MenuTheme.Cream, Vector2.zero, Vector2.one);
                _castNext[s].raycastTarget = false;
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
                    _swatchImage[s * 6 + c] = bitImage;
                    float lockS = swH - 4f;
                    if (lockS < 16f) lockS = 16f;
                    var lockRt = MenuWidgets.Place(bit, "Lock", (chip - 6f - lockS) * 0.5f, 2f, lockS, lockS);
                    var lockImage = lockRt.gameObject.AddComponent<Image>();
                    lockImage.sprite = MenuIcons.Lock;
                    lockImage.preserveAspect = true;
                    lockImage.raycastTarget = false;
                    lockImage.enabled = false;
                    _swatchLock[s * 6 + c] = lockImage;
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
                var nameRt = MenuWidgets.Place(card, "NameLine", 4f, textY, cardW - 8f, UiFit.CastNameH);
                var namePlate = nameRt.gameObject.AddComponent<Image>();
                MenuArt.Plate(namePlate, MenuTheme.Navy, true);
                namePlate.raycastTarget = false;
                _castNamePlate[s] = namePlate;
                _castName[s] = MenuWidgets.Words(nameRt, "", UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Cream, Vector2.zero, Vector2.one);
                Snug(_castName[s]);
                if (_castName[s] != null)
                {
                    _castName[s].rectTransform.offsetMin = new Vector2(12f, 6f);
                    _castName[s].rectTransform.offsetMax = new Vector2(-12f, -6f);
                }
                var statRt = MenuWidgets.Place(card, "StatusLine", 4f, textY + UiFit.CastNameH, cardW - 8f, UiFit.CastStatusH);
                var statPlate = statRt.gameObject.AddComponent<Image>();
                MenuArt.Plate(statPlate, MenuTheme.Navy, true);
                statPlate.raycastTarget = false;
                _castStat[s] = statPlate;
                _castReady[s] = MenuWidgets.Words(statRt, "", UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Cream, Vector2.zero, Vector2.one);
                Snug(_castReady[s]);
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
            if (_banner != null) _banner.text = "Left and right change a rule";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            UiFit.Columns(UiFit.Current(), out float leftX, out float leftW, out _, out _);
            float colW = (leftW - 16f) * 0.5f;
            if (colW > 440f) colW = 440f;
            for (int i = 0; i < 4; i++)
            {
                var id = (TagModeId)i;
                int col = i % 2;
                int row = i / 2;
                AddTile(leftX + col * (colW + 12f), 12f + row * 168f, colW, 152f, i, MenuCatalog.ModeName(id), MenuCatalog.ModeBlurb(id), true);
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
            ClampRuleWindow();
            GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
            UiFit.Columns(UiFit.Current(), out _, out _, out float rightX, out float rightW);
            int win = UiFit.Window(UiFit.Current(), 84f, 12f);
            int shown = 0;
            for (int index = 4; index < RuleBook.Count; index++)
            {
                if (index < 4 + _window || index >= 4 + _window + win) continue;
                AddTile(rightX, 12f + shown * 84f, rightW, 80f, index, RuleBook.Title(index), RuleBook.Detail(s, index), true);
                shown++;
            }
            _count = RuleBook.Count;
            ShowHow();
            RefreshFocus();
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
            _cols = 1;
            _focus = MenuSession.RandomArena ? 3 : MenuSession.Arena;
            if (_focus < 0 || _focus > 4) _focus = 0;
            if (_header != null) _header.text = "  Arena";
            if (_banner != null) _banner.text = "Random picks one of the three parks";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            UiFit.ArenaSplit(UiFit.Current(), out float listW, out float shotX, out float shotW, out float row, out float step);
            float thumbW = listW * 0.28f;
            if (thumbW > 200f) thumbW = 200f;
            if (thumbW < 72f) thumbW = 72f;
            for (int i = 0; i < ParkArena.Count; i++)
            {
                AddTile(16f, 12f + i * step, listW, row, i, ParkArena.NameOf(i), MenuArenaCard.Blurb(i), true);
                MenuTile park = TileAt(i);
                if (park != null)
                    MenuArenaCard.PaintAt(park.transform, i, 12f, 12f, thumbW, row - 24f);
            }
            AddTile(16f, 12f + 3 * step, listW, row, 3, "Random", "One of Mega Park, Pocket Park, or Stack Yard.", true);
            float backW = listW * 0.5f;
            if (backW > 360f) backW = 360f;
            AddTile(16f, 12f + 4 * step, backW, row > 100f ? 100f : row, 4, "Back", "", true);
            float shotH = 12f + 4f * step + row;
            if (shotH > UiFit.BodyH(UiFit.Current()) - 16f) shotH = UiFit.BodyH(UiFit.Current()) - 16f;
            var shot = MenuWidgets.Place(_body, "ArenaShot", shotX, 12f, shotW, shotH);
            var frame = shot.gameObject.AddComponent<Image>();
            MenuArt.Plate(frame, MenuTheme.Navy, true);
            frame.raycastTarget = false;
            float viewH = shotH * 0.68f;
            if (viewH < 120f) viewH = 120f;
            var view = MenuWidgets.Place(shot, "Shot", 16f, 16f, shotW - 32f, viewH);
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
            if (_dim != null) _dim.color = MenuTheme.Veil;
            string name = MenuSession.RandomArena ? "Random" : ParkArena.NameOf(MenuSession.Arena);
            if (_loadPractice) name = PracticeSession.ArenaName();
            if (_header != null) _header.text = "  Loading";
            int fly = MenuSession.Arena;
            if (fly < 0 || fly >= ParkArena.Count) fly = 0;
            ShowFlyover(fly, 0.72f);
            MenuWidgets.Heading(_body, name, 64, TextAnchor.MiddleCenter, MenuTheme.Gold, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.98f));
            GameSettings rules = GameSettings.Current ?? GameSettings.Defaults();
            string it = RuleBook.LoadStart(rules);
            const string itPrefix = "Starting It  ";
            if (it != null && it.StartsWith(itPrefix))
                it = it.Substring(itPrefix.Length);
            string rounds = RuleBook.LoadRounds(rules);
            bool hot = MenuSession.Mode == TagModeId.HotPotato;
            string[] ruleLabel = new string[8];
            string[] ruleValue = new string[8];
            int rows = 0;
            ruleLabel[rows] = "Length";
            ruleValue[rows] = RuleBook.LoadLength(rules);
            rows++;
            ruleLabel[rows] = "Rounds";
            ruleValue[rows] = rounds;
            rows++;
            if (hot || rounds != "1")
            {
                ruleLabel[rows] = "Win target";
                ruleValue[rows] = hot ? "First to " + RuleBook.LoadWin(rules) + " round wins" : RuleBook.LoadWin(rules);
                rows++;
            }
            ruleLabel[rows] = "Starting It";
            ruleValue[rows] = it;
            rows++;
            ruleLabel[rows] = "Handicaps";
            ruleValue[rows] = HandLine(rules);
            rows++;
            ruleLabel[rows] = "Pads";
            ruleValue[rows] = RuleBook.LoadPads(rules);
            rows++;
            ruleLabel[rows] = "Zips";
            ruleValue[rows] = RuleBook.LoadZips(rules);
            rows++;
            ruleLabel[rows] = "Tip";
            ruleValue[rows] = MenuTips.At(_tip);
            rows++;
            _tip++;
            for (int i = 0; i < rows; i++)
            {
                float top = 0.78f - i * 0.07f;
                float bot = top - 0.065f;
                MenuWidgets.Words(_body, ruleLabel[i], UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Mute, new Vector2(0.12f, bot), new Vector2(0.36f, top));
                _loadRule[i] = MenuWidgets.Words(_body, ruleValue[i], UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Cream, new Vector2(0.38f, bot), new Vector2(0.9f, top));
            }
            float bodyH = UiFit.BodyH(UiFit.Current());
            UiFit.RowBox(UiFit.Current(), 1120f, out float barX, out float barW);
            float barY = bodyH - 92f;
            if (barY < 24f) barY = 24f;
            var track = MenuWidgets.Place(_body, "LoadTrack", barX, barY, barW, 28f);
            var trackImage = track.gameObject.AddComponent<Image>();
            MenuArt.Plate(trackImage, new Color(0f, 0f, 0f, 0.45f), true);
            trackImage.raycastTarget = false;
            var fill = MenuWidgets.Place(track, "LoadFill", 4f, 4f, 0f, 28f);
            _loadFill = fill.gameObject.AddComponent<Image>();
            MenuArt.Plate(_loadFill, MenuTheme.Gold, true);
            _loadFill.raycastTarget = false;
            _loadFill.enabled = false;
            _loadWord = MenuWidgets.Words(_body, LoadGate.Waiting, UiFit.FloorFont, TextAnchor.MiddleCenter, MenuTheme.Cream, new Vector2(0.2f, 0.01f), new Vector2(0.8f, 0.08f));
            _loadStep = -1;
            if (_banner != null) _banner.text = "";
        }

        void BuildPause()
        {
            _quitAsk = false;
            _quitPick = 0;
            _quitModal = null;
            _quitNoPlate = null;
            _quitYesPlate = null;
            _turnSeat = -1;
            _count = MenuSplitPause.Items;
            _cols = 1;
            _focus = 0;
            int opener = 0;
            if (GameSettings.Current != null) opener = GameSettings.Current.AccessSeat;
            if (opener < 0) opener = 0;
            if (opener > 3) opener = 3;
            _pauseOwner = opener;
            bool preview = MenuSplitPause.Preview > 0;
            string who = MenuSplitPause.SeatLabel(opener, preview);
            if (_header != null) _header.text = "  Pause";
            if (_banner != null) _banner.text = Tag.Ui.Hud.MatchHudText.ComicHint;
            int n = MenuSplitPause.Fill(_cards);
            int humans = preview ? MenuSplitPause.Preview : CouchPlay.Humans;
            if (humans < 1) humans = 1;
            bool split = humans > 1;
            if (_dim != null) _dim.color = split ? new Color(0f, 0f, 0f, 0f) : MenuTheme.Dim;
            MenuSplitPause.Card home = n > 0 ? _cards[0] : new MenuSplitPause.Card();
            bool have = false;
            for (int c = 0; c < n; c++)
            {
                MenuSplitPause.Card card = _cards[c];
                if (!card.Show) continue;
                if (split && card.Seat != opener)
                {
                    var shade = MenuWidgets.Place(_body, "PaneDim", card.X, card.Y, card.W, card.H);
                    var shadeImage = shade.gameObject.AddComponent<Image>();
                    shadeImage.color = new Color(0.02f, 0.04f, 0.08f, 0.72f);
                    shadeImage.raycastTarget = false;
                    continue;
                }
                home = card;
                have = true;
            }
            if (!have && n > 0) home = _cards[0];
            float bw = 460f;
            if (home.W > 40f && bw > home.W - 28f) bw = home.W - 28f;
            if (bw < 280f) bw = home.W > 120f ? home.W - 24f : 460f;
            float bh = 68f;
            float gap = 10f;
            float stack = MenuSplitPause.Items * bh + (MenuSplitPause.Items - 1) * gap;
            float cardW = bw + 36f;
            float cardH = stack + 28f;
            float cx = home.X + (home.W - cardW) * 0.5f;
            float cy = home.Y + (home.H - cardH) * 0.5f - 8f;
            if (cx < 8f) cx = 8f;
            if (cy < 8f) cy = 8f;
            _pauseX = cx;
            _pauseY = cy;
            _pauseW = cardW;
            _pauseH = cardH;
            Color seat = MenuTheme.Seat(opener);
            float tagW = 196f;
            float tagH = 32f;
            float tagX = cx + cardW - tagW;
            float tagY = cy + cardH + 8f;
            var tag = MenuWidgets.Place(_body, "PauseTag", tagX, tagY, tagW, tagH);
            var tagImage = tag.gameObject.AddComponent<Image>();
            tagImage.color = seat;
            tagImage.raycastTarget = false;
            MenuWidgets.Words(tag, "Paused by " + who, 20, TextAnchor.MiddleCenter, MenuTheme.Ink, Vector2.zero, Vector2.one);
            var plate = MenuWidgets.Place(_body, "PauseCard", cx, cy, cardW, cardH);
            var plateImage = plate.gameObject.AddComponent<Image>();
            Color plateColor = Color.Lerp(new Color(0.05f, 0.12f, 0.32f, 0.94f), seat, 0.42f);
            MenuArt.Plate(plateImage, plateColor, true);
            plateImage.raycastTarget = false;
            var ownerBar = MenuWidgets.Place(plate, "OwnerBar", 0f, 0f, cardW, 8f);
            var ownerImage = ownerBar.gameObject.AddComponent<Image>();
            ownerImage.color = seat;
            ownerImage.raycastTarget = false;
            float bx = 18f;
            float by = 18f;
            for (int i = 0; i < MenuSplitPause.Items; i++)
                AddTile(cx + bx, cy + by + i * (bh + gap), bw, bh, i, MenuSplitPause.Item[i], MenuSplitPause.Blurb[i], true);
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
            string headline = "RESULTS";
            if (mode != null && !string.IsNullOrEmpty(mode.ResultMessage))
                headline = mode.ResultMessage;
            if (_header != null) _header.text = "  " + headline;
            int n = FillRanks();
            float chase = n > 0 ? _rows[0].Chase : 0f;
            string chaseWho = "";
            if (mode != null)
            {
                chase = mode.LongestChase;
                chaseWho = mode.LongestChaseName;
            }
            if (_banner != null)
                _banner.text = MenuCatalog.ModeName(modeId) + "    " + MenuPodium.ChaseLine(chase, chaseWho);
            float span = UiFit.BodyW(UiFit.Current());
            float bodyH = UiFit.BodyH(UiFit.Current());
            float rankH = 168f;
            float btnH = 80f;
            float gap = 8f;
            float top = 8f;
            float maxStageH = bodyH - rankH - btnH - gap * 2f - top;
            if (maxStageH < 180f) maxStageH = 180f;
            float stageW = span - 32f;
            if (stageW > 1680f) stageW = 1680f;
            float fitH = stageW * (9f / 16f);
            float fitW = stageW;
            if (fitH > maxStageH)
            {
                fitH = maxStageH;
                fitW = fitH * (16f / 9f);
            }
            float stageX = (span - fitW) * 0.5f;
            float rankY = top + fitH + gap;
            float btnY = rankY + rankH + gap;
            if (btnY + btnH > bodyH)
            {
                fitH -= btnY + btnH - bodyH;
                if (fitH < 160f) fitH = 160f;
                fitW = fitH * (16f / 9f);
                stageX = (span - fitW) * 0.5f;
                rankY = top + fitH + gap;
                btnY = rankY + rankH + gap;
            }
            var stage = MenuWidgets.Place(_body, "ResultsView", stageX, top, fitW, fitH);
            var frame = stage.gameObject.AddComponent<Image>();
            frame.color = new Color(0f, 0f, 0f, 0f);
            frame.raycastTarget = false;
            var viewRt = MenuWidgets.Place(stage, "View", 0f, 0f, fitW, fitH);
            var view = viewRt.gameObject.AddComponent<RawImage>();
            view.raycastTarget = false;
            if (_preview != null) _preview.ShowPodium(n, _rows, view);
            for (int rank = 0; rank < n; rank++)
            {
                string detail = MenuPodium.Stats(_rows[rank]);
                int seat = _rows[rank].Seat;
                if (seat < 0) seat = rank;
                float rankW = (fitW - 24f) / 4f;
                int col = rank == 0 ? 1 : rank == 1 ? 0 : rank;
                float cardX = stageX + col * (rankW + 8f);
                MenuTile tile = AddTile(cardX, rankY, rankW, rankH, 20 + rank, MenuTheme.Place(rank) + "  " + _rows[rank].Name, detail, false);
                if (tile != null)
                {
                    tile.KeepBar = true;
                    Color look = MenuMannequin.Swatch(MenuMannequin.NameOf(_rows[rank].Hier));
                    tile.BarColor = look;
                    if (tile.Bar != null)
                    {
                        tile.Bar.color = tile.BarColor;
                        RectTransform barRt = tile.Bar.rectTransform;
                        barRt.anchoredPosition = new Vector2(0f, 0f);
                        barRt.sizeDelta = new Vector2(rankW, 10f);
                    }
                    tile.Tint(Color.Lerp(MenuTheme.Ink, look, UiSweep.SeatMix));
                    if (tile.Stroke != null && _rows[rank].Winner)
                        tile.Stroke.color = MenuTheme.Gold;
                    SeatChip(tile.transform, rankW - 168f, UiFit.StripeClear() + 6f, seat, look, true);
                    RectTransform swatch = MenuWidgets.Place(tile.transform, "LookSwatch", rankW - 42f, UiFit.StripeClear() + 12f, 22f, 22f);
                    var swatchImage = swatch.gameObject.AddComponent<Image>();
                    swatchImage.color = look;
                    swatchImage.raycastTarget = false;
                    Pull(tile.Label, 18f);
                    Pull(tile.Detail, 18f);
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
                UiFit.RowBox(UiFit.Current(), 1120f, out float rowX, out float rowW);
                MenuTile tile = AddTile(rowX, 8f + v * 96f, rowW, 88f, index, MenuDepth.Title(index), MenuDepth.Detail(index), true);
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
            _count = ControlCount();
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
            int win = ControlWindow();
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
                ControlBands(out int actions, out int noteAt, out int stickAt, out int confirmAt, out int resetAt);
                int stick = index - stickAt;
                int note = index - noteAt;
                if (index < actions)
                {
                    var action = (PlayAction)index;
                    title = ActionBinds.Name(action);
                    detail = ActionDetail(action, binds);
                    if (_capturing && index == _captureAction) detail = "Press a key or a button";
                }
                else if (note >= 0 && note < ContextNotes)
                {
                    title = NoteTitle(note);
                    detail = NoteDetail(note);
                }
                else if (stick >= 0 && stick < MenuStick.Rows)
                {
                    title = MenuStick.Label(stick);
                    detail = MenuStick.Detail(stick);
                }
                else if (index >= confirmAt && index < resetAt)
                {
                    int seat = index - confirmAt;
                    title = ConfirmTitle(seat);
                    GameSettings settings = GameSettings.Current ?? GameSettings.Defaults();
                    detail = FaceMap.Word(settings.ConfirmFace[seat]);
                }
                else if (index == resetAt)
                {
                    title = "Reset bindings";
                    detail = "Back to the defaults. Jump is Space.";
                }
                else
                {
                    title = "Back";
                    detail = _conflict ?? "";
                }
                UiFit.RowBox(UiFit.Current(), 1680f, out float rowX, out float rowW);
                MenuTile row = AddTile(rowX, 8f + v * 96f, rowW, 88f, index, title, detail, true);
                if (index < actions && row != null)
                {
                    int family = MenuInput.LastKind == InputDeviceKind.Gamepad
                        ? PadGlyph.Family(MenuInput.LastDevice)
                        : PadGlyph.Keyboard;
                    string token = family == PadGlyph.Keyboard ? binds.Keyboard[index] : binds.Gamepad[index];
                    MenuWidgets.Mark(row, MenuIcons.ForToken(family, token), MenuTheme.Gold, 56f);
                }
            }
            PaintControlScroll(win);
            RefreshFocus();
        }

        void PaintControlScroll(int win)
        {
            if (win < 1) win = 1;
            UiFit.RowBox(UiFit.Current(), 1680f, out float rowX, out float rowW);
            float trackH = win * 96f - 16f;
            if (trackH < 120f) trackH = 120f;
            float trackX = rowX + rowW + 8f;
            var track = MenuWidgets.Place(_body, "ScrollTrack", trackX, 12f, 14f, trackH);
            var trackImage = track.gameObject.AddComponent<Image>();
            MenuArt.Plate(trackImage, new Color(0f, 0f, 0f, 0.55f), true);
            trackImage.raycastTarget = false;
            int max = _count - win;
            if (max < 0) max = 0;
            float span = _count <= win ? 1f : win / (float)_count;
            float thumbH = trackH * span;
            if (thumbH < 56f) thumbH = 56f;
            if (thumbH > trackH) thumbH = trackH;
            float travel = trackH - thumbH;
            float t = max <= 0 ? 0f : _window / (float)max;
            var thumb = MenuWidgets.Place(track, "ScrollThumb", 2f, travel * t, 10f, thumbH);
            var thumbImage = thumb.gameObject.AddComponent<Image>();
            MenuArt.Plate(thumbImage, MenuTheme.Gold, true);
            thumbImage.raycastTarget = false;
        }

        int ControlWindow()
        {
            if (8f + (OptWindow + 1) * 96f <= UiFit.BodyH(UiFit.Current()))
                return OptWindow + 1;
            return OptWindow;
        }

        void PaintLoad()
        {
            bool live = false;
            TagModeController modes = TagModeController.Instance;
            if (modes != null) live = modes.RoundActive;
            int step = LoadGate.Step(_loadFired, live);
            if (step == _loadStep) return;
            _loadStep = step;
            float fill = LoadGate.Fill(_loadFired, live);
            if (_loadFill != null)
            {
                float w = 1112f * fill;
                _loadFill.rectTransform.sizeDelta = new Vector2(w, 28f);
                _loadFill.enabled = w > 1f;
            }
            if (_loadWord != null) _loadWord.text = LoadGate.Caption(_loadFired, live);
        }

        void BuildCredits()
        {
            _count = 1;
            _cols = 1;
            if (_header != null) _header.text = "  Credits";
            if (_banner != null) _banner.text = "";
            if (_dim != null) _dim.color = MenuTheme.Veil;
            Text credits = MenuWidgets.Words(_body, MenuCatalog.Credits(), UiFit.FloorFont, TextAnchor.UpperLeft, MenuTheme.Cream, new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.98f));
            credits.verticalOverflow = VerticalWrapMode.Overflow;
            float body = UiFit.BodyH(UiFit.Current());
            UiFit.RowBox(UiFit.Current(), 480f, out float backX, out float backW);
            float backY = body - 100f;
            if (backY < 120f) backY = 120f;
            AddTile(backX, backY, backW, 80f, 0, "Back", "", true);
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
            if (_focus == RuleBook.Arena)
            {
                MenuAudio.Confirm();
                MenuMatch.RememberRules();
                _arenaBack = MenuScreenId.Rules;
                ShowArena();
                return;
            }
            if (_focus >= RuleBook.Back)
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
                    if (!_quitAsk)
                    {
                        ShowQuitModal();
                        break;
                    }
                    ConfirmQuit();
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
            ControlBands(out _, out int noteAt, out int stickAt, out int confirmAt, out int resetAt);
            int note = _focus - noteAt;
            if (note >= 0 && note < ContextNotes)
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
            if (_focus >= confirmAt && _focus < resetAt)
            {
                GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
                GameSettings.Current = s;
                s.StepConfirm(_focus - confirmAt, 1);
                UnlockLooks();
                MenuAudio.Move();
                PaintControls();
                return;
            }
            if (_focus == resetAt)
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
            if (_focus > resetAt)
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
            if (!RuleBook.Edit(s, _focus, dir)) return;
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
                int span = ControlWindow();
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
                    if (_castNamePlate[s] != null) _castNamePlate[s].color = MenuTheme.Navy;
                    if (_castPrev[s] != null) _castPrev[s].gameObject.SetActive(false);
                    if (_castNext[s] != null) _castNext[s].gameObject.SetActive(false);
                    PaintLooks(s, false);
                    continue;
                }
                if (_preview != null)
                {
                    _preview.Apply(s, MenuSession.Hier[s], MenuSession.Accent[s], MenuSession.Hat[s], _castView[s]);
                    _preview.SetReady(s, MenuSession.Ready[s]);
                }
                if (_castReady[s] == null) continue;
                string skin = MenuSession.CardBody(s);
                string trim = MenuSession.Accent[s] >= 0 && MenuSession.Accent[s] < LocalProfiles.HierNames.Length
                    ? LocalProfiles.HierNames[MenuSession.Accent[s]] : skin;
                string ready = MenuSession.Ready[s] ? "READY" : "Not ready";
                string hat = MenuSession.Hat[s] == 0 ? "Hat off" : "Hat on";
                UiFit.CastLines(CouchPlay.Name(s), skin, trim, hat, ready, UiFit.CastInk(UiFit.Current()), out string top, out string bot);
                string tag = "P" + (s + 1).ToString() + "  " + CouchPlay.Name(s);
                string look = skin;
                if (trim.Length > 0 && trim != skin) look = skin + " / " + trim;
                string named = tag + "\n" + look;
                if (_castName[s] != null && _castName[s].text != named) _castName[s].text = named;
                bool isReady = MenuSession.Ready[s];
                string banner = isReady ? "READY" : bot;
                if (_castReady[s].text != banner) _castReady[s].text = banner;
                _castReady[s].color = isReady ? MenuTheme.Ink : MenuTheme.Cream;
                if (_castStat[s] != null) _castStat[s].color = isReady ? MenuTheme.Ready : MenuTheme.Navy;
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
                Color lookTint = MenuPortraits.Tint(picked);
                if (_castPlate[s] != null) _castPlate[s].color = lookTint;
                if (_castNamePlate[s] != null) _castNamePlate[s].color = Color.Lerp(lookTint, MenuTheme.Ink, 0.35f);
                if (_castPrev[s] != null) _castPrev[s].gameObject.SetActive(true);
                if (_castNext[s] != null) _castNext[s].gameObject.SetActive(true);
                PaintLooks(s, true);
            }
        }

        void PaintLooks(int seat, bool human)
        {
            for (int c = 0; c < 6; c++)
            {
                int at = seat * 6 + c;
                bool taken = human && LookTaken(seat, c);
                if (_swatchImage[at] != null)
                    _swatchImage[at].color = taken ? MenuTheme.Off : MenuPortraits.Tint(c);
                if (_swatchLock[at] != null) _swatchLock[at].enabled = taken;
            }
        }

        bool LookTaken(int seat, int look)
        {
            for (int other = 0; other < 4; other++)
            {
                if (other == seat) continue;
                if (!CouchPlay.HumanAt(other)) continue;
                if (MenuSession.Hier[other] == look) return true;
            }
            return false;
        }

        void QuitMatch()
        {
            _holdResults = false;
            HideForMatch();
            GameFlow flow = GameFlow.Instance;
            if (flow != null) flow.QuitToMenu();
            else
            {
                _enterBack = true;
                ShowTitle();
            }
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
            int win = UiFit.Window(UiFit.Current(), 128f, 8f);
            if (_focus < _window) _window = _focus;
            if (_focus >= _window + win) _window = _focus - (win - 1);
            int max = _count - win;
            if (max < 0) max = 0;
            if (_window > max) _window = max;
            if (_window < 0) _window = 0;
            UiFit.RowBox(UiFit.Current(), 1100f, out float x, out float w);
            for (int v = 0; v < win; v++)
            {
                int index = _window + v;
                if (index >= _count) break;
                string title;
                string detail;
                if (index >= LocalProfiles.Max)
                {
                    title = "Back";
                    detail = "";
                }
                else
                {
                    int id = LocalProfiles.SlotId(index);
                    if (id <= 0)
                    {
                        title = "Empty";
                        detail = "";
                    }
                    else
                    {
                        title = LocalProfiles.NameOf(id);
                        detail = LocalProfiles.CardOf(id);
                    }
                }
                AddTile(x, 8f + v * 128f, w, 120f, index, title, detail, true);
            }
            RefreshFocus();
        }

        void ActivateRecords()
        {
            if (_focus >= LocalProfiles.Max)
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

        static void ControlBands(out int actions, out int noteAt, out int stickAt, out int confirmAt, out int resetAt)
        {
            actions = (int)PlayAction.Count;
            noteAt = actions;
            stickAt = actions + ContextNotes;
            confirmAt = stickAt + MenuStick.Rows;
            resetAt = confirmAt + GameSettings.SeatCount;
        }

        static int ControlCount()
        {
            ControlBands(out _, out _, out _, out _, out int resetAt);
            return resetAt + 2;
        }

        /// <summary>
        /// Keyboard words are what PlayerInputReader samples on the solo pawn.
        /// Pad words are the ActionBinds gamepad tokens. Cling stays the move hold.
        /// </summary>
        static string ActionDetail(PlayAction action, ActionBinds binds)
        {
            int i = (int)action;
            string kb = binds.Keyboard[i];
            string pad = ActionBinds.Show(binds.Gamepad[i]);
            if (action == PlayAction.Cling)
            {
                return ActionBinds.Show(kb) + " / " + pad
                    + ". Wall climb and wall run need this hold. Wall jump is this hold plus Jump.";
            }
            string key = ActionBinds.Show(kb);
            if (action == PlayAction.Slide && kb == "leftCtrl")
                key = "Ctrl or C";
            else if (action == PlayAction.AirDash && kb != "leftAlt")
                key = ActionBinds.Show(kb) + " or Alt";
            else if (action == PlayAction.Punch && kb != "e")
                key = ActionBinds.Show(kb) + " or E";
            else if (action == PlayAction.Sprint && kb == "leftShift")
                key = "Shift or Alt";
            return key + "    /    " + pad;
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
                    + ". Click pulls. Second click within 0.28 s releases. Left hand. No pad bind.";
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
