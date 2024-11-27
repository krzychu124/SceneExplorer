using System;
using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEngine;

namespace SceneExplorer.ToBeReplaced.Helpers
{
    public class UIStyle
    {
        private static UIStyle _instance;

        public static UIStyle Instance
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                if (_instance == null)
                {
                    _instance = new UIStyle();
                }

                return _instance;
            }
        }

        public GUISkin Skin => _skin;

        public Texture2D BgTexture => _bgTexture;
        public Texture2D ResizeBtnNormalTexture => _resizeBtnNormalTexture;
        public Texture2D ResizeBtnHoverTexture => _resizeBtnHoverTexture;
        public Texture2D CloseBtnNormalTexture => _closeBtnNormalTexture;
        public Texture2D CloseBtnHoverTexture => _closeBtnHoverTexture;
        public Texture2D MinimizeBtnNormalTexture => _minimizeBtnNormalTexture;
        public Texture2D MinimizeBtnHoverTexture => _minimizeBtnHoverTexture;
        public Texture2D TitleNormalTexture => _titleNormalTexture;
        public Texture2D TitleMinimizedTexture => _titleMinimizedTexture;
        public Texture2D TitleHoverTexture => _titleHoverTexture;
        public Texture2D CommonHoverTexture => _commonHoverTexture;
        public Texture2D RootColorTexture => _rootColorTexture;
        public Texture2D InteractionColorTexture => _activeColorTexture;


        private readonly GUISkin _skin;
        private readonly Texture2D _bgTexture;
        private readonly Texture2D _bgTexture2;
        private readonly Texture2D _bgTexture3;
        private readonly Texture2D _bgTexture4;
        private readonly Texture2D _bgTexture5;
        private readonly Texture2D _resizeBtnNormalTexture;
        private readonly Texture2D _resizeBtnHoverTexture;
        private readonly Texture2D _closeBtnNormalTexture;
        private readonly Texture2D _closeBtnHoverTexture;
        private readonly Texture2D _minimizeBtnNormalTexture;
        private readonly Texture2D _minimizeBtnHoverTexture;
        private readonly Texture2D _titleNormalTexture;
        private readonly Texture2D _titleMinimizedTexture;
        private readonly Texture2D _titleHoverTexture;
        private readonly Texture2D _commonHoverTexture;
        private readonly Texture2D _rootColorTexture;
        private readonly Texture2D _activeColorTexture;

        public readonly GUIStyle windowDefault;
        public readonly GUIStyle window2;
        public readonly GUIStyle window3;
        public readonly GUIStyle window4;
        public readonly GUIStyle window5;
        public readonly GUIStyle focusedLabelStyle;
        public readonly GUIStyle defaultLabelStyle;
        public readonly GUIStyle reducedPaddingLabelStyle;
        public readonly GUIStyle reducedPaddingPrivateLabelStyle;
        public readonly GUIStyle paginationLabelStyle;
        public readonly GUIStyle reducedPaddingHighlightedLabelStyle;
        public readonly GUIStyle focusedReducedPaddingLabelStyle;
        public readonly GUIStyle focusedReducedPaddingHighlightedLabelStyle;
        public readonly GUIStyle enumValueStyle;
        public readonly GUIStyle numericValueStyle;
        public readonly GUIStyle booleanValueStyle;
        public readonly GUIStyle entityValueStyle;
        public readonly GUIStyle iconButton;
        public readonly GUIStyle line;
        public readonly GUIStyle collapsibleContentStyle;
        public readonly GUIStyle managedLabelStyle;
        public readonly GUIStyle managedHighlightedLabelStyle;
        public readonly GUIStyle unManagedLabelStyle;
        public readonly GUIStyle unManagedHighlightedLabelStyle;
        public readonly GUIStyle bufferHighlightedLabelStyle;
        public readonly GUIStyle bufferLabelStyle;
        public readonly GUIStyle sharedLabelStyle;
        public readonly GUIStyle sharedHighlightedLabelStyle;
        public readonly GUIStyle unknownLabelStyle;
        public readonly GUIStyle unknownHighlightedLabelStyle;

        public readonly GUILayoutOption[] textInputLayoutOptions = new[] { GUILayout.MinWidth(60), GUILayout.MaxWidth(60) };

        public UIStyle()
        {
            _bgTexture = new Texture2D(1, 1);
            _bgTexture.SetPixel(0, 0, new Color32(26, 29, 34, 255));
            _bgTexture.Apply();

            _bgTexture2 = new Texture2D(1, 1);
            _bgTexture2.SetPixel(0, 0, new Color32(29, 32, 37, 255));
            _bgTexture2.Apply();

            _bgTexture3 = new Texture2D(1, 1);
            _bgTexture3.SetPixel(0, 0, new Color32(31, 34, 39, 255));
            _bgTexture3.Apply();

            _bgTexture4 = new Texture2D(1, 1);
            _bgTexture4.SetPixel(0, 0, new Color32(34, 37, 42, 255));
            _bgTexture4.Apply();

            _bgTexture5 = new Texture2D(1, 1);
            _bgTexture5.SetPixel(0, 0, new Color32(37, 40, 45, 255));
            _bgTexture5.Apply();

            _resizeBtnNormalTexture = new Texture2D(1, 1);
            _resizeBtnNormalTexture.SetPixel(0, 0, Color.white);
            _resizeBtnNormalTexture.Apply();

            _resizeBtnHoverTexture = new Texture2D(1, 1);
            _resizeBtnHoverTexture.SetPixel(0, 0, new Color(0.24f, 0.36f, 0.78f));
            _resizeBtnHoverTexture.Apply();

            _closeBtnNormalTexture = new Texture2D(1, 1);
            _closeBtnNormalTexture.SetPixel(0, 0, new Color(0.7f, 0.26f, 0.2f));
            _closeBtnNormalTexture.Apply();

            _closeBtnHoverTexture = new Texture2D(1, 1);
            _closeBtnHoverTexture.SetPixel(0, 0, Color.white);
            _closeBtnHoverTexture.Apply();

            _minimizeBtnNormalTexture = new Texture2D(1, 1);
            _minimizeBtnNormalTexture.SetPixel(0, 0, new Color(0.9f, 0.81f, 0.02f));
            _minimizeBtnNormalTexture.Apply();

            _minimizeBtnHoverTexture = new Texture2D(1, 1);
            _minimizeBtnHoverTexture.SetPixel(0, 0, Color.white);
            _minimizeBtnHoverTexture.Apply();

            _titleNormalTexture = new Texture2D(1, 1);
            _titleNormalTexture.SetPixel(0, 0, new Color(0.06f, 0.07f, 0.1f, 0.82f));
            _titleNormalTexture.Apply();

            _titleMinimizedTexture = new Texture2D(1, 1);
            _titleMinimizedTexture.SetPixel(0, 0, new Color(0.18f, 0.18f, 0.18f));
            _titleMinimizedTexture.Apply();

            _titleHoverTexture = new Texture2D(1, 1);
            _titleHoverTexture.SetPixel(0, 0, new Color(0.23f, 0.24f, 0.27f, 0.86f));
            _titleHoverTexture.Apply();

            _commonHoverTexture = new Texture2D(1, 1);
            _commonHoverTexture.SetPixel(0, 0, new Color(0.3f, 0.31f, 0.34f, 0.86f));
            _commonHoverTexture.Apply();

            _rootColorTexture = new Texture2D(1, 1);
            _rootColorTexture.SetPixel(0, 0, new Color(0f, 0.49f, 0.58f));
            _rootColorTexture.Apply();

            _activeColorTexture = new Texture2D(1, 1);
            _activeColorTexture.SetPixel(0, 0, new Color(0f, 1f, 0.31f));
            _activeColorTexture.Apply();

            _skin = ScriptableObject.CreateInstance<GUISkin>();
            _skin.box = new GUIStyle(GUI.skin.box) { normal = { background = _bgTexture }, onNormal = { background = _bgTexture }, hover = { background = _titleHoverTexture }, alignment = TextAnchor.MiddleLeft };
            _skin.button = new GUIStyle(GUI.skin.button);
            _skin.horizontalScrollbar = new GUIStyle(GUI.skin.horizontalScrollbar)
            { normal = { background = _bgTexture }, active = { background = _bgTexture }, onNormal = { background = _bgTexture }, hover = { background = _bgTexture }, onHover = { background = _bgTexture } };
            _skin.horizontalScrollbarLeftButton = new GUIStyle(GUI.skin.horizontalScrollbarLeftButton);
            _skin.horizontalScrollbarRightButton = new GUIStyle(GUI.skin.horizontalScrollbarRightButton);
            _skin.horizontalScrollbarThumb = new GUIStyle(GUI.skin.horizontalScrollbarThumb)
            {
                normal = { background = _titleHoverTexture },
                active = { background = _commonHoverTexture },
                onActive = { background = _commonHoverTexture },
                onNormal = { background = _commonHoverTexture },
                hover = { background = _commonHoverTexture },
                onHover = { background = _commonHoverTexture }
            };
            _skin.horizontalSlider = new GUIStyle(GUI.skin.horizontalSlider)
            { normal = { background = _bgTexture }, active = { background = _bgTexture }, onNormal = { background = _bgTexture }, hover = { background = _bgTexture }, onHover = { background = _bgTexture } };
            _skin.horizontalSliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb)
            { normal = { background = _bgTexture }, active = { background = _bgTexture }, onNormal = { background = _bgTexture }, hover = { background = _bgTexture }, onHover = { background = _bgTexture } };
            _skin.label = new GUIStyle(GUI.skin.label) { margin = new RectOffset(0, 0, 1, 1), padding = new RectOffset(0, 0, 1, 1), wordWrap = false };
            _skin.scrollView = new GUIStyle(GUI.skin.scrollView);
            _skin.textArea = new GUIStyle(GUI.skin.textArea);
            _skin.textField = new GUIStyle(GUI.skin.textField);
            _skin.toggle = new GUIStyle(GUI.skin.toggle);
            _skin.verticalScrollbar = new GUIStyle(GUI.skin.verticalScrollbar)
            { normal = { background = _bgTexture }, active = { background = _bgTexture }, onNormal = { background = _bgTexture }, hover = { background = _bgTexture }, onHover = { background = _bgTexture } };
            _skin.verticalScrollbarDownButton = new GUIStyle(GUI.skin.verticalScrollbarDownButton);
            _skin.verticalScrollbarThumb = new GUIStyle(GUI.skin.verticalScrollbarThumb)
            {
                normal = { background = _titleHoverTexture },
                active = { background = _commonHoverTexture },
                onActive = { background = _commonHoverTexture },
                onNormal = { background = _commonHoverTexture },
                hover = { background = _commonHoverTexture },
                onHover = { background = _commonHoverTexture }
            };
            _skin.verticalScrollbarUpButton = new GUIStyle(GUI.skin.verticalScrollbarUpButton);
            _skin.verticalSlider = new GUIStyle(GUI.skin.verticalSlider)
            { normal = { background = _bgTexture }, active = { background = _bgTexture }, onNormal = { background = _bgTexture }, hover = { background = _bgTexture }, onHover = { background = _bgTexture } };
            _skin.verticalSliderThumb = new GUIStyle(GUI.skin.verticalSliderThumb)
            { normal = { background = _bgTexture }, active = { background = _bgTexture }, onNormal = { background = _bgTexture }, hover = { background = _bgTexture }, onHover = { background = _bgTexture } };
            _skin.window = new GUIStyle(GUI.skin.window);
            _skin.window.normal.background = _bgTexture;
            _skin.window.onNormal.background = _bgTexture;
            _skin.window.onFocused.background = _bgTexture;
            _skin.font = GUI.skin.font;
            windowDefault = new GUIStyle(_skin.window);
            window2 = new GUIStyle(_skin.window) { normal = { background = _bgTexture2 }, onNormal = { background = _bgTexture2 }, onFocused = { background = _bgTexture2 } };
            window3 = new GUIStyle(_skin.window) { normal = { background = _bgTexture3 }, onNormal = { background = _bgTexture3 }, onFocused = { background = _bgTexture3 } };
            window4 = new GUIStyle(_skin.window) { normal = { background = _bgTexture4 }, onNormal = { background = _bgTexture4 }, onFocused = { background = _bgTexture4 } };
            window5 = new GUIStyle(_skin.window) { normal = { background = _bgTexture5 }, onNormal = { background = _bgTexture5 }, onFocused = { background = _bgTexture5 } };

            _skin.settings.cursorColor = GUI.skin.settings.cursorColor;
            _skin.settings.cursorFlashSpeed = GUI.skin.settings.cursorFlashSpeed;
            _skin.settings.doubleClickSelectsWord = GUI.skin.settings.doubleClickSelectsWord;
            _skin.settings.selectionColor = GUI.skin.settings.selectionColor;
            _skin.settings.tripleClickSelectsLine = GUI.skin.settings.tripleClickSelectsLine;

            focusedLabelStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = new Color(0.52f, 0.91f, 0.24f) } };
            focusedLabelStyle.wordWrap = false;
            defaultLabelStyle = new GUIStyle(GUI.skin.label);
            reducedPaddingLabelStyle = new GUIStyle(GUI.skin.label) { margin = new RectOffset(0, 0, 1, 1), padding = new RectOffset(1, 1, 1, 1) };
            reducedPaddingLabelStyle.wordWrap = false;
            reducedPaddingPrivateLabelStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = reducedPaddingLabelStyle.normal.textColor * new Color(1, 1, 1, 0.6f) }, margin = new RectOffset(0, 0, 1, 1), padding = new RectOffset(1, 1, 1, 1) };
            reducedPaddingPrivateLabelStyle.wordWrap = false;
            paginationLabelStyle = new GUIStyle(reducedPaddingLabelStyle) { margin = new RectOffset(3, 3, 3, 1), padding = new RectOffset(1, 1, 1, 1), fontSize = 12, fontStyle = FontStyle.Bold };
            reducedPaddingHighlightedLabelStyle = new GUIStyle(reducedPaddingLabelStyle) { fontStyle = FontStyle.Bold };
            enumValueStyle = new GUIStyle(reducedPaddingLabelStyle) { normal = { textColor = new Color(0f, 0.59f, 0.83f) }, fontStyle = FontStyle.BoldAndItalic, wordWrap = true };
            entityValueStyle = new GUIStyle(reducedPaddingLabelStyle) { normal = { textColor = new Color(0f, 0.76f, 0.87f) }, fontStyle = FontStyle.Bold, wordWrap = true };
            numericValueStyle = new GUIStyle(reducedPaddingLabelStyle) { normal = { textColor = new Color(0f, 0.8f, 0.71f) }, fontStyle = FontStyle.Bold, wordWrap = true };
            booleanValueStyle = new GUIStyle(reducedPaddingLabelStyle) { normal = { textColor = new Color32(16, 167, 178, 255) }, fontStyle = FontStyle.Bold, wordWrap = true };
            focusedReducedPaddingLabelStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = new Color(0.21f, 0.75f, 0f) }, fontStyle = FontStyle.Bold, wordWrap = false, margin = new RectOffset(0, 0, 1, 1), padding = new RectOffset(1, 1, 1, 1) };
            focusedReducedPaddingHighlightedLabelStyle = new GUIStyle(focusedReducedPaddingLabelStyle) { fontStyle = FontStyle.Bold };

            managedLabelStyle = new GUIStyle(reducedPaddingLabelStyle) { normal = { textColor = new Color32(255, 232, 115, 255) } };
            managedHighlightedLabelStyle = new GUIStyle(focusedReducedPaddingHighlightedLabelStyle) { normal = { textColor = new Color32(255, 232, 115, 255) } };
            unManagedLabelStyle = new GUIStyle(reducedPaddingLabelStyle) { normal = { textColor = new Color32(255, 245, 191, 255) } };
            unManagedHighlightedLabelStyle = new GUIStyle(focusedReducedPaddingHighlightedLabelStyle) { normal = { textColor = new Color32(255, 245, 191, 255) } };
            bufferLabelStyle = new GUIStyle(reducedPaddingLabelStyle) { normal = { textColor = new Color32(242, 203, 0, 255) } };
            bufferHighlightedLabelStyle = new GUIStyle(focusedReducedPaddingHighlightedLabelStyle) { normal = { textColor = new Color32(242, 203, 0, 255) } };
            sharedLabelStyle = new GUIStyle(reducedPaddingLabelStyle) { normal = { textColor = new Color32(120, 252, 255, 255) } };
            sharedHighlightedLabelStyle = new GUIStyle(focusedReducedPaddingHighlightedLabelStyle) { normal = { textColor = new Color32(120, 252, 255, 255) } };
            unknownLabelStyle = new GUIStyle(reducedPaddingLabelStyle) { normal = { textColor = new Color32(255, 89, 98, 255) } };
            unknownHighlightedLabelStyle = new GUIStyle(focusedReducedPaddingHighlightedLabelStyle) { normal = { textColor = new Color32(255, 89, 98, 255) } };

            iconButton = new GUIStyle(GUI.skin.button)
            {
                margin = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(4, 4, 2, 2),
                normal = { background = _titleHoverTexture },
                active = { background = _titleHoverTexture },
                onNormal = { background = _titleHoverTexture },
                hover = { background = _commonHoverTexture },
                onHover = { background = _commonHoverTexture }
            };
            collapsibleContentStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _bgTexture3 },
                active = { background = _bgTexture3 },
                onNormal = { background = _bgTexture3 },
                hover = { background = _bgTexture3 },
                onHover = { background = _bgTexture3 },
                margin = new RectOffset(),
                padding = new RectOffset(),
                alignment = TextAnchor.MiddleLeft
            };
            line = new GUIStyle(_skin.box)
            {
                border = new RectOffset(0, 0, 1, 1),
                margin = new RectOffset(0, 0, 1, 1),
                padding = new RectOffset(0, 0, 1, 1),
                normal = { background = _commonHoverTexture },
                onNormal = { background = _commonHoverTexture }
            };
        }

        public GUIStyle CalculateTextStyle(Type t)
        {
            if (t == null)
            {
                return reducedPaddingLabelStyle;
            }

            if (t.IsEnum || t == typeof(EntityArchetype))
            {
                return enumValueStyle;
            }
            if (t == typeof(Entity))
            {
                return entityValueStyle;
            }
            if (t == typeof(bool))
            {
                return booleanValueStyle;
            }

            return t.IsNumericType() ? numericValueStyle : reducedPaddingLabelStyle;
        }

        public GUIStyle CalculateLabelStyle(bool isPublic)
        {
            return !isPublic ? reducedPaddingPrivateLabelStyle : reducedPaddingLabelStyle;
        }
    }
}
