using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace ForagAR.Library.Editor
{
    /// <summary>
    /// One-click setup of the mushroom library UI in the open scene:
    /// a canvas whose bottom half holds a filter bar and a scrolling grid of mushroom cards, and an
    /// tabbed information section that replaces the library once a mushroom is placed or selected.
    /// A handle with an arrow button on top of the panel minimizes it to show the AR view full screen.
    /// Running it again rebuilds the library from scratch.
    /// </summary>
    static class LibraryUIBuilder
    {
        const string k_CanvasName = "ForagAR UI";
        const string k_CardPrefabPath = MushroomCatalogSync.PrefabFolder + "/UI/MushroomCard.prefab";
        const int k_ThumbnailLayer = 31;
        const string k_ThumbnailLayerName = "Thumbnail";

        // Layout in reference units (1080 x 1920 canvas, matched on width).
        const float k_PanelPadding = 32f;
        const float k_HandleHeight = 80f;
        static readonly Vector2 k_MinimizeButtonSize = new Vector2(180f, 60f);
        const float k_FilterBarHeight = 120f;
        const float k_TabBarHeight = 88f;
        const float k_TabSpacing = 12f;
        const int k_Columns = 3;
        static readonly Vector2 k_CardSize = new Vector2(300f, 360f);
        const float k_CardSpacing = 30f;

        static readonly Color k_PanelColor = new Color32(0x1F, 0x24, 0x1D, 0xFF);
        static readonly Color k_SurfaceColor = new Color32(0x2D, 0x34, 0x2A, 0xFF);
        static readonly Color k_DividerColor = new Color32(0x45, 0x50, 0x40, 0xFF);
        static readonly Color k_TextColor = new Color32(0xEE, 0xEA, 0xDE, 0xFF);
        static readonly Color k_MutedTextColor = new Color32(0xA8, 0xAD, 0x9C, 0xFF);

        [MenuItem("ForagAR/Build Library UI")]
        static void Build()
        {
            EnsureThumbnailLayer();
            var catalog = MushroomCatalogSync.Sync(MushroomCatalogSync.GetOrCreateCatalog());
            var cardPrefab = BuildCardPrefab();

            var existing = GameObject.Find(k_CanvasName);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing);

            var canvas = CreateCanvas();
            var panel = CreatePanel(canvas.transform);
            var minimizeButton = CreateHandle(panel, out var minimizeLabel);

            // Below the handle, the panel shows either the library or a mushroom's information section.
            var libraryContent = CreateGroup("Library Content", panel);
            CreateFilterBar(libraryContent);
            var content = CreateScrollView(libraryContent);
            var info = CreateInfoSection(panel);

            var thumbnailRenderer = Undo.AddComponent<MushroomThumbnailRenderer>(panel.gameObject);
            var thumbnailSettings = new SerializedObject(thumbnailRenderer);
            thumbnailSettings.FindProperty("m_Layer").intValue = k_ThumbnailLayer;
            thumbnailSettings.ApplyModifiedPropertiesWithoutUndo();

            var spawner = Object.FindAnyObjectByType<ObjectSpawner>(FindObjectsInactive.Include);
            if (spawner == null)
                Debug.LogWarning("No ObjectSpawner found in the scene. The library will show mushrooms but cannot place them.");

            var view = Undo.AddComponent<MushroomLibraryView>(panel.gameObject);
            var placement = Undo.AddComponent<MushroomPlacementController>(panel.gameObject);
            var placementSettings = new SerializedObject(placement);
            placementSettings.FindProperty("m_Library").objectReferenceValue = view;
            placementSettings.FindProperty("m_LibraryPanel").objectReferenceValue = panel;
            placementSettings.ApplyModifiedPropertiesWithoutUndo();

            var viewSettings = new SerializedObject(view);
            viewSettings.FindProperty("m_PlacementController").objectReferenceValue = placement;
            viewSettings.FindProperty("m_Catalog").objectReferenceValue = catalog;
            viewSettings.FindProperty("m_CardPrefab").objectReferenceValue = cardPrefab;
            viewSettings.FindProperty("m_CardContainer").objectReferenceValue = content;
            viewSettings.FindProperty("m_ThumbnailRenderer").objectReferenceValue = thumbnailRenderer;
            viewSettings.FindProperty("m_ObjectSpawner").objectReferenceValue = spawner;
            viewSettings.ApplyModifiedPropertiesWithoutUndo();

            var infoView = Undo.AddComponent<MushroomInfoView>(panel.gameObject);
            var infoSettings = new SerializedObject(infoView);
            infoSettings.FindProperty("m_PlacementController").objectReferenceValue = placement;
            infoSettings.FindProperty("m_LibraryContent").objectReferenceValue = libraryContent.gameObject;
            infoSettings.FindProperty("m_InfoContent").objectReferenceValue = info.root.gameObject;
            infoSettings.FindProperty("m_Title").objectReferenceValue = info.title;
            infoSettings.FindProperty("m_ScientificName").objectReferenceValue = info.scientificName;
            infoSettings.FindProperty("m_Body").objectReferenceValue = info.body;
            infoSettings.FindProperty("m_BodyScroll").objectReferenceValue = info.bodyScroll;
            infoSettings.FindProperty("m_BackButton").objectReferenceValue = info.backButton;
            var tabButtons = infoSettings.FindProperty("m_TabButtons");
            tabButtons.arraySize = info.tabButtons.Length;
            for (var i = 0; i < info.tabButtons.Length; i++)
                tabButtons.GetArrayElementAtIndex(i).objectReferenceValue = info.tabButtons[i];
            infoSettings.ApplyModifiedPropertiesWithoutUndo();
            info.root.gameObject.SetActive(false);

            var minimizer = Undo.AddComponent<LibraryPanelMinimizer>(panel.gameObject);
            var minimizerSettings = new SerializedObject(minimizer);
            minimizerSettings.FindProperty("m_Panel").objectReferenceValue = panel;
            minimizerSettings.FindProperty("m_ToggleButton").objectReferenceValue = minimizeButton;
            minimizerSettings.FindProperty("m_ToggleLabel").objectReferenceValue = minimizeLabel;
            minimizerSettings.FindProperty("m_MinimizedHeight").floatValue = k_HandleHeight;
            minimizerSettings.ApplyModifiedPropertiesWithoutUndo();

            if (Object.FindAnyObjectByType<EventSystem>() == null)
                Debug.LogWarning("No active EventSystem in the scene. The library UI will not receive touches until one is added.");

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Selection.activeGameObject = panel.gameObject;
            Debug.Log($"Built mushroom library with {catalog.entries.Count} mushroom(s). Save the scene to keep it.");
        }

        static void EnsureThumbnailLayer()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layer = tagManager.FindProperty("layers").GetArrayElementAtIndex(k_ThumbnailLayer);
            if (layer.stringValue == k_ThumbnailLayerName)
                return;

            if (!string.IsNullOrEmpty(layer.stringValue))
            {
                Debug.LogWarning($"Layer {k_ThumbnailLayer} is already named '{layer.stringValue}'. Thumbnails will render on it anyway.");
                return;
            }

            layer.stringValue = k_ThumbnailLayerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        static MushroomCard BuildCardPrefab()
        {
            var folder = Path.GetDirectoryName(k_CardPrefabPath)?.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(MushroomCatalogSync.PrefabFolder, Path.GetFileName(folder));

            var root = new GameObject("MushroomCard", typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");
            ((RectTransform)root.transform).sizeDelta = k_CardSize;
            var background = root.AddComponent<Image>();
            background.sprite = BuiltinSprite("UI/Skin/UISprite.psd");
            background.type = Image.Type.Sliced;
            background.color = k_SurfaceColor;

            var thumbnail = CreateUIObject("Thumbnail", root.transform).gameObject.AddComponent<RawImage>();
            thumbnail.raycastTarget = false;
            SetAnchors(thumbnail.rectTransform, new Vector2(0f, 0.2f), Vector2.one, new Vector2(16f, 0f), new Vector2(-16f, -16f));

            var label = CreateText("Name", root.transform, "Mushroom", 32f, k_TextColor);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            SetAnchors(label.rectTransform, Vector2.zero, new Vector2(1f, 0.2f), new Vector2(12f, 8f), new Vector2(-12f, 0f));

            var card = root.AddComponent<MushroomCard>();
            var cardSettings = new SerializedObject(card);
            cardSettings.FindProperty("m_Thumbnail").objectReferenceValue = thumbnail;
            cardSettings.FindProperty("m_Label").objectReferenceValue = label;
            cardSettings.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, k_CardPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<MushroomCard>();
        }

        static Canvas CreateCanvas()
        {
            var go = new GameObject(k_CanvasName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Build Library UI");
            go.layer = LayerMask.NameToLayer("UI");

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        static RectTransform CreatePanel(Transform parent)
        {
            // The AR camera keeps rendering full screen; this panel covers the bottom half.
            var panel = CreateUIObject("Library Panel", parent);
            SetAnchors(panel, Vector2.zero, new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);

            var image = panel.gameObject.AddComponent<Image>();
            image.color = k_PanelColor;
            return panel;
        }

        /// <summary>
        /// The strip along the top of the panel with the arrow button that minimizes and expands it.
        /// It is the only part left on screen while the panel is minimized.
        /// </summary>
        static Button CreateHandle(RectTransform panel, out TMP_Text label)
        {
            var handle = CreateUIObject("Handle", panel);
            handle.anchorMin = new Vector2(0f, 1f);
            handle.anchorMax = new Vector2(1f, 1f);
            handle.pivot = new Vector2(0.5f, 1f);
            handle.offsetMin = new Vector2(0f, -k_HandleHeight);
            handle.offsetMax = Vector2.zero;

            var button = TMP_DefaultControls.CreateButton(ControlResources());
            button.name = "Minimize Button";
            button.transform.SetParent(handle, false);
            var buttonRect = (RectTransform)button.transform;
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = k_MinimizeButtonSize;
            buttonRect.anchoredPosition = Vector2.zero;
            button.GetComponent<Image>().color = k_SurfaceColor;

            // LibraryPanelMinimizer swaps this between ▼ and ▲.
            label = button.GetComponentInChildren<TMP_Text>();
            label.text = "▼";
            label.fontSize = 36f;
            label.color = k_TextColor;
            return button.GetComponent<Button>();
        }

        static void CreateFilterBar(RectTransform panel)
        {
            var bar = CreateUIObject("Filter Bar", panel);
            SetTopStrip(bar, k_PanelPadding, k_FilterBarHeight);

            // Placeholder only: drawn but not wired to any filtering yet.
            var resources = ControlResources();

            var search = TMP_DefaultControls.CreateInputField(resources);
            search.name = "Search Field";
            search.transform.SetParent(bar, false);
            SetAnchors((RectTransform)search.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-(k_FilterBarHeight + 20f), 0f));
            search.GetComponent<Image>().color = k_SurfaceColor;

            var input = search.GetComponent<TMP_InputField>();
            input.pointSize = 40f;
            if (input.placeholder is TMP_Text placeholder)
            {
                placeholder.text = "Search mushrooms…";
                placeholder.color = k_MutedTextColor;
                placeholder.fontStyle = FontStyles.Normal;
                placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            }

            if (input.textComponent != null)
            {
                input.textComponent.color = k_TextColor;
                input.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            }

            // Inset the text from the rounded edge of the field.
            if (input.textViewport != null)
                SetAnchors(input.textViewport, Vector2.zero, Vector2.one, new Vector2(28f, 0f), new Vector2(-28f, 0f));

            var filterButton = TMP_DefaultControls.CreateButton(resources);
            filterButton.name = "Filter Button";
            filterButton.transform.SetParent(bar, false);
            var buttonRect = (RectTransform)filterButton.transform;
            buttonRect.anchorMin = new Vector2(1f, 0f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 0.5f);
            buttonRect.sizeDelta = new Vector2(k_FilterBarHeight, 0f);
            buttonRect.anchoredPosition = Vector2.zero;
            filterButton.GetComponent<Image>().color = k_SurfaceColor;

            var buttonLabel = filterButton.GetComponentInChildren<TMP_Text>();
            buttonLabel.text = "Filter";
            buttonLabel.fontSize = 30f;
            buttonLabel.color = k_TextColor;

            var divider = CreateUIObject("Divider", panel);
            SetTopStrip(divider, k_PanelPadding * 1.5f + k_FilterBarHeight, 2f);
            divider.gameObject.AddComponent<Image>().color = k_DividerColor;
        }

        static RectTransform CreateScrollView(RectTransform panel)
        {
            var top = k_PanelPadding * 2f + k_FilterBarHeight;

            var scrollView = CreateUIObject("Library Scroll View", panel);
            SetAnchors(scrollView, Vector2.zero, Vector2.one, new Vector2(k_PanelPadding, 0f), new Vector2(-k_PanelPadding, -top));
            var scrollRect = scrollView.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 30f;

            var viewport = CreateUIObject("Viewport", scrollView);
            SetAnchors(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            // Transparent graphic so swipes on empty space between cards still scroll.
            viewport.gameObject.AddComponent<Image>().color = Color.clear;

            var content = CreateUIObject("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = k_CardSize;
            grid.spacing = new Vector2(k_CardSpacing, k_CardSpacing);
            grid.padding = new RectOffset(0, 0, 0, (int)k_PanelPadding);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = k_Columns;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return content;
        }

        struct InfoSection
        {
            public RectTransform root;
            public TMP_Text title;
            public TMP_Text scientificName;
            public TMP_Text body;
            public ScrollRect bodyScroll;
            public Button backButton;
            public Button[] tabButtons;
        }

        static InfoSection CreateInfoSection(RectTransform panel)
        {
            const float backButtonHeight = 100f;
            const float titleHeight = 72f;
            const float scientificNameHeight = 48f;

            var section = new InfoSection { root = CreateGroup("Info Content", panel) };

            var backButton = TMP_DefaultControls.CreateButton(ControlResources());
            backButton.name = "Back To Library Button";
            backButton.transform.SetParent(section.root, false);
            var backRect = (RectTransform)backButton.transform;
            backRect.anchorMin = new Vector2(0f, 1f);
            backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0f, 1f);
            backRect.sizeDelta = new Vector2(300f, backButtonHeight);
            backRect.anchoredPosition = new Vector2(k_PanelPadding, -k_PanelPadding);
            backButton.GetComponent<Image>().color = k_SurfaceColor;
            var backLabel = backButton.GetComponentInChildren<TMP_Text>();
            backLabel.text = "‹ Library";
            backLabel.fontSize = 36f;
            backLabel.color = k_TextColor;
            section.backButton = backButton.GetComponent<Button>();

            var top = k_PanelPadding + backButtonHeight + 24f;
            var title = CreateText("Title", section.root, "Mushroom", 56f, k_TextColor);
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            SetTopStrip(title.rectTransform, top, titleHeight);
            section.title = title;

            top += titleHeight;
            var scientificName = CreateText("Scientific Name", section.root, "Scientific name", 36f, k_MutedTextColor);
            scientificName.fontStyle = FontStyles.Italic;
            scientificName.alignment = TextAlignmentOptions.MidlineLeft;
            SetTopStrip(scientificName.rectTransform, top, scientificNameHeight);
            section.scientificName = scientificName;

            top += scientificNameHeight + 16f;
            section.tabButtons = CreateTabBar(section.root, top);

            top += k_TabBarHeight + 12f;
            var divider = CreateUIObject("Divider", section.root);
            SetTopStrip(divider, top, 2f);
            divider.gameObject.AddComponent<Image>().color = k_DividerColor;

            top += 2f + 24f;
            var scrollView = CreateUIObject("Info Scroll View", section.root);
            SetAnchors(scrollView, Vector2.zero, Vector2.one, new Vector2(k_PanelPadding, k_PanelPadding), new Vector2(-k_PanelPadding, -top));
            var scrollRect = scrollView.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 30f;

            var viewport = CreateUIObject("Viewport", scrollView);
            SetAnchors(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = Color.clear;

            var body = CreateText("Tab Text", viewport, "Description", 38f, k_TextColor);
            body.alignment = TextAlignmentOptions.TopLeft;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 10f;
            var bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.anchoredPosition = Vector2.zero;
            bodyRect.sizeDelta = Vector2.zero;
            body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            section.body = body;

            scrollRect.viewport = viewport;
            scrollRect.content = bodyRect;
            section.bodyScroll = scrollRect;

            return section;
        }

        /// <summary>
        /// A row of equally wide tab buttons, one per <see cref="MushroomInfoTab"/> in order.
        /// <see cref="MushroomInfoView"/> colors the selected one and swaps the text below.
        /// </summary>
        static Button[] CreateTabBar(RectTransform parent, float top)
        {
            var bar = CreateUIObject("Tab Bar", parent);
            SetTopStrip(bar, top, k_TabBarHeight);

            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = k_TabSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var tabs = (MushroomInfoTab[])System.Enum.GetValues(typeof(MushroomInfoTab));
            var buttons = new Button[tabs.Length];
            for (var i = 0; i < tabs.Length; i++)
            {
                var tabName = MushroomInfoView.GetTabName(tabs[i]);
                var button = TMP_DefaultControls.CreateButton(ControlResources());
                button.name = tabName + " Tab";
                button.transform.SetParent(bar, false);
                button.GetComponent<Image>().color = k_SurfaceColor;

                // Same width for every tab, whatever the length of its name.
                var layoutElement = button.AddComponent<LayoutElement>();
                layoutElement.minWidth = 0f;
                layoutElement.preferredWidth = 0f;
                layoutElement.flexibleWidth = 1f;

                var label = button.GetComponentInChildren<TMP_Text>();
                label.text = tabName;
                label.color = k_MutedTextColor;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                // Shrinks long names such as "Look-Alikes" to fit narrow screens.
                label.enableAutoSizing = true;
                label.fontSizeMin = 20f;
                label.fontSizeMax = 32f;
                SetAnchors(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));

                buttons[i] = button.GetComponent<Button>();
            }

            return buttons;
        }

        /// <summary>
        /// An empty object filling the panel below its handle, used to show or hide one view of the panel at a time.
        /// </summary>
        static RectTransform CreateGroup(string name, RectTransform panel)
        {
            var group = CreateUIObject(name, panel);
            SetAnchors(group, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -k_HandleHeight));
            return group;
        }

        static TMP_DefaultControls.Resources ControlResources() => new TMP_DefaultControls.Resources
        {
            standard = BuiltinSprite("UI/Skin/UISprite.psd"),
            inputField = BuiltinSprite("UI/Skin/InputFieldBackground.psd"),
        };

        static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color)
        {
            var label = CreateUIObject(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        /// <summary>
        /// Stretches horizontally inside the panel padding, <paramref name="top"/> units below the panel's top edge.
        /// </summary>
        static void SetTopStrip(RectTransform rect, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(k_PanelPadding, -top - height);
            rect.offsetMax = new Vector2(-k_PanelPadding, -top);
        }

        static Sprite BuiltinSprite(string path) => AssetDatabase.GetBuiltinExtraResource<Sprite>(path);
    }
}
