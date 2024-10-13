using System;
using Game.Input;
using Game.SceneFlow;
using JetBrains.Annotations;
using SceneExplorer.ToBeReplaced.Helpers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SceneExplorer.ToBeReplaced.Windows
{
    public abstract class FloatingWindowBase : MonoBehaviour
    {
        private static int _uniqueId = 999;
        private static FloatingWindowBase _resizingWindow;
        private static Vector2 _resizeDragHandle = Vector2.zero;
        private static FloatingWindowBase _movingWindow;
        private static Vector2 _moveDragHandle = Vector2.zero;
        private readonly int _id = ++_uniqueId;

        protected Vector2 _minSize = new Vector2(160, 100);

        private bool _minimize = false;
        private bool _resizable = true;
        private InputManager _inputManager;
        private float _tempWindowHeight;
        private Rect _windowRect = new Rect(200, 150, 160, 100);
        public bool CursorOverUI { get; protected set; }
        public int ChainDepth { get; set; } = 0;
        public Rect Rect => _windowRect;

        static FloatingWindowBase()
        {
            WindowManager = new FloatingWindowsManager();
        }

        public FloatingWindowBase() { }

        protected bool IsRoot { get; set; }
        public int ParentWindowId { get; set; } = -1;

        public virtual string Subtitle { get; set; } = string.Empty;
        protected static UIStyle Style { get; private set; }
        public static FloatingWindowsManager WindowManager { get; protected set; }
        protected abstract string Title { get; }
        public Vector2 Position => _windowRect.position;

        protected bool Resizable
        {
            get => _resizable;
            set => _resizable = value;
        }

        public int Id => _id;
        public bool IsOpen { get; private set; }
        public bool IsMinimized { get; private set; }
        public bool HasFocus => WindowManager.FocusedWindowId == _id;

        /// <summary>
        /// May block removal if long time operation is performed
        /// </summary>
        public virtual bool CanRemove { get; } = true;

        protected virtual void Start()
        {

            _inputManager = GameManager.instance.inputManager;
            WindowManager.RegisterWindow(this, ParentWindowId);
        }

        public virtual void OnDestroy()
        {
            if (ParentWindowId > -1)
            {
                WindowManager.ClosingChild(ParentWindowId, Id);
            }
            WindowManager.DisposeOpenedWindows(Id);
        }

        public void OnGUI()
        {
            if (Style == null)
            {
                Style = UIStyle.Build();
            }

            if (!IsOpen)
                return;

            var originalSkin = GUI.skin;
            GUI.skin = Style.Skin;

            GUIStyle temp = GUI.skin.window;
            GUIStyle temp2 = GUI.skin.box;
            GUI.skin.window = CalculateBackgroundStyle();
            GUI.skin.box = GUI.skin.window;
            _windowRect = GUI.Window(Id, _windowRect, RenderWindowInternal, string.Empty);
            GUI.skin.window = temp;
            GUI.skin.box = temp2;

            GUI.skin = originalSkin;
        }

        public virtual bool InsideUI(Vector2 position)
        {
            return _windowRect.Contains(position);
        }

        private void RenderWindowInternal(int id)
        {
            var mouse = Mouse.current.position;
            float y = Screen.height - mouse.value.y;
            Vector2 cursor = new Vector2(mouse.value.x, y);
            if (Event.current.type == EventType.Repaint)
            {
                CursorOverUI = InsideUI(cursor);
            }

            if (WindowManager.FocusedWindowId == -1 || (Mouse.current.leftButton.wasReleasedThisFrame && CursorOverUI))
            {
                WindowManager.FocusWindow(id);
                OnFocus();
            }

            GUILayout.Space(10);
            try
            {
                if (!IsMinimized)
                {
                    RenderWindowContent();
                }
            }
            catch (Exception e)
            {
                Logging.Info("EXCEPTION: \n" + e);
            }

            GUILayout.Space(10);
            if (!IsMinimized)
            {
                DrawBorder();
            }

            DrawTitle(cursor);
            DrawMinimizeButton(cursor);
            DrawCloseButton(cursor);

            if (_resizable && !IsMinimized)
            {
                DrawResizeHandle(cursor);
            }

            if (Event.current.type == EventType.Repaint && IsMinimized != _minimize)
            {
                bool wasMinimized = IsMinimized;
                IsMinimized = _minimize;
                if (!wasMinimized && _minimize)
                {
                    _tempWindowHeight = _windowRect.height;
                    _windowRect.height = 20f;
                }
                else
                {
                    _windowRect.height = _tempWindowHeight;
                }
            }
        }

        private GUIStyle CalculateBackgroundStyle()
        {
            int depth = ChainDepth;
            if (depth % 5 == 0)
            {
                return UIStyle.Instance.windowDefault;
            }
            if (depth % 5 == 1)
            {
                return UIStyle.Instance.window2;
            }
            if (depth % 5 == 2)
            {
                return UIStyle.Instance.window3;
            }
            if (depth % 5 == 3)
            {
                return UIStyle.Instance.window4;
            }
            if (depth % 5 == 4)
            {
                return UIStyle.Instance.window5;
            }
            return UIStyle.Instance.windowDefault;
        }

        private void DrawCloseButton(Vector3 position)
        {
            var closeRect = new Rect(_windowRect.x + _windowRect.width - 20.0f, _windowRect.y, 20.0f, 20.0f);
            var closeTex = Style.CloseBtnNormalTexture;

            bool drawButton = IsOpen;
            if (!GUIUtility.hasModalWindow && closeRect.Contains(position))
            {
                closeTex = Style.CloseBtnHoverTexture;
                if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    drawButton = false;
                    WindowManager.Unfocus();
                    OnFocusLost();
                    Close();
                }
            }

            if (drawButton)
            {
                GUI.DrawTexture(new Rect(_windowRect.width - 20.0f, 0.0f, 20.0f, 20.0f), closeTex, ScaleMode.StretchToFill);
                GUI.Label(new Rect(_windowRect.width - 17.0f, 0.0f, 20.0f, 20.0f), "✖");
            }
        }

        private void DrawMinimizeButton(Vector3 position)
        {
            var minimizeRect = new Rect(_windowRect.x + 2.0f, _windowRect.y, 16.0f, 8.0f);
            var minimizeTex = Style.MinimizeBtnNormalTexture;

            if (!GUIUtility.hasModalWindow && minimizeRect.Contains(position))
            {
                minimizeTex = Style.MinimizeBtnHoverTexture;

                if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    bool wasMinimized = _minimize;
                    MinimizeToggle();

                    WindowManager.Unfocus();
                    OnFocusLost();
                }
            }

            GUI.DrawTexture(new Rect(4f, 0.0f, 16.0f, 8.0f), minimizeTex, ScaleMode.StretchToFill);
        }

        private void DrawTitle(Vector3 position)
        {
            var moveRect = new Rect(_windowRect.x + 16, _windowRect.y, _windowRect.width - 22f, 20.0f);
            var moveTex = IsMinimized ? Style.TitleMinimizedTexture : Style.TitleNormalTexture;
            if (!GUIUtility.hasModalWindow)
            {
                if (_movingWindow != null)
                {
                    if (_movingWindow == this)
                    {
                        moveTex = Style.TitleHoverTexture;

                        if (Mouse.current.leftButton.isPressed)
                        {
                            var pos = new Vector2(position.x, position.y) + _moveDragHandle;
                            _windowRect.x = pos.x;
                            _windowRect.y = pos.y;
                            if (_windowRect.x < 0.0f)
                            {
                                _windowRect.x = 0.0f;
                            }

                            if (_windowRect.x + _windowRect.width > Screen.width)
                            {
                                _windowRect.x = Screen.width - _windowRect.width;
                            }

                            if (_windowRect.y < 0.0f)
                            {
                                _windowRect.y = 0.0f;
                            }

                            if (_windowRect.y + _windowRect.height > Screen.height)
                            {
                                _windowRect.y = Screen.height - _windowRect.height;
                            }
                        }
                        else
                        {
                            _movingWindow = null;

                            // OnWindowMoved(windowRect.position);
                        }
                    }
                }
                else if (moveRect.Contains(position) && _inputManager.mouseOnScreen)
                {
                    moveTex = Style.TitleHoverTexture;
                    if (Mouse.current.leftButton.isPressed && _resizingWindow == null)
                    {
                        _movingWindow = this;
                        _moveDragHandle = new Vector2(_windowRect.x, _windowRect.y) - new Vector2(position.x, position.y);
                        TryUseEvent();
                    }
                }
            }

            GUI.DrawTexture(new Rect(0.0f, 0.0f, _windowRect.width, 20.0f), moveTex, ScaleMode.StretchToFill);
            GUI.contentColor = Color.white;
            GUI.Label(new Rect(30.0f, 0.0f, _windowRect.width, 20.0f), $"{Title} {(!string.IsNullOrEmpty(Subtitle) ? $"- {Subtitle}" : string.Empty)}", WindowManager.FocusedWindowId == _id ? Style.focusedLabelStyle : Style.defaultLabelStyle);
        }

        private void DrawResizeHandle(Vector3 mouse)
        {
            var resizeRect = new Rect(_windowRect.x + _windowRect.width - 16.0f, _windowRect.y + _windowRect.height - 8.0f, 16.0f, 8.0f);
            var resizeTex = Style.ResizeBtnNormalTexture;

            if (!GUIUtility.hasModalWindow)
            {
                if (_resizingWindow != null)
                {
                    if (_resizingWindow == this)
                    {
                        resizeTex = Style.ResizeBtnHoverTexture;

                        if (Mouse.current.leftButton.isPressed)
                        {
                            var size = new Vector2(mouse.x, mouse.y) + _resizeDragHandle - new Vector2(_windowRect.x, _windowRect.y);

                            if (size.x < _minSize.x)
                            {
                                size.x = _minSize.x;
                            }

                            if (size.y < _minSize.y)
                            {
                                size.y = _minSize.y;
                            }

                            _windowRect.width = size.x;
                            _windowRect.height = size.y;

                            if (_windowRect.x + _windowRect.width >= Screen.width)
                            {
                                _windowRect.width = Screen.width - _windowRect.x;
                            }

                            if (_windowRect.y + _windowRect.height >= Screen.height)
                            {
                                _windowRect.height = Screen.height - _windowRect.y;
                            }
                        }
                        else
                        {
                            _resizingWindow = null;
                        }
                    }
                }
                else if (resizeRect.Contains(mouse))
                {
                    resizeTex = Style.ResizeBtnHoverTexture;
                    if (Mouse.current.leftButton.isPressed)
                    {
                        _resizingWindow = this;
                        _resizeDragHandle = new Vector2(_windowRect.x + _windowRect.width, _windowRect.y + _windowRect.height) - new Vector2(mouse.x, mouse.y);
                        TryUseEvent();
                    }
                }
            }

            GUI.DrawTexture(new Rect(_windowRect.width - 16.0f, _windowRect.height - 8.0f, 16.0f, 8.0f), resizeTex, ScaleMode.StretchToFill);
        }

        private void DrawBorder()
        {
            Texture2D texture = IsRoot ? Style.RootColorTexture :
                (_resizingWindow == this || _movingWindow == this) ? Style.InteractionColorTexture : Style.TitleNormalTexture;
            float width = IsRoot ? 2f : 1f;
            GUI.DrawTexture(new Rect(0.0f, 0.0f, width, _windowRect.height), texture);
            GUI.DrawTexture(new Rect(_windowRect.width - width, 0.0f, width, _windowRect.height), texture);
            GUI.DrawTexture(new Rect(0.0f, _windowRect.height - width, _windowRect.width, width), texture);
        }

        protected void ForceSize(Vector2 size)
        {
            var p = _windowRect;
            p.size = size;
            _windowRect = p;
        }

        protected void ForceSize(uint width, uint height)
        {
            var p = _windowRect;
            p.width = width;
            p.height = height;
            _windowRect = p;
        }

        public void ForcePosition(Vector2 position)
        {
            var p = _windowRect;
            p.x = position.x;
            p.y = position.y;
            _windowRect = p;
        }

        private void InitSkin() { }

        protected abstract void RenderWindowContent();

        private void TryUseEvent()
        {
            if (Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
            {
                Event.current.Use();
            }
        }

        public virtual void OnFocus() { }

        public virtual void OnFocusLost() { }

        public virtual void OnGuiEvent(Event e) { }

        public virtual void RenderLabel() { }

        public virtual void OnOpen() { }

        public void Open()
        {
            IsOpen = true;
            IsMinimized = false;
            _minimize = false;
            OnOpen();
        }

        public virtual void Close()
        {
            IsOpen = false;
            OnFocusLost();
        }

        public void MinimizeToggle()
        {
            _minimize = !IsMinimized;
        }
    }
}
