using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace LocalAssistant.Core
{
    public sealed class AssistantUiRefs
    {
        public Text HealthBanner;
        public Text AvatarStateText;
        public Text StageStatusText;
        public Text StagePlaceholderText;
        public Text SubtitleText;
        public Text TaskSummaryText;
        public Text TaskContentText;
        public GameObject SettingsPanel;
        public Text SettingsSummaryText;
        public Text SettingsStatusText;
        public Toggle SpeakRepliesToggle;
        public Toggle TranscriptPreviewToggle;
        public Toggle MiniAssistantToggle;
        public Toggle ReminderSpeechToggle;
        public Button ReloadSettingsButton;
        public Button SaveSettingsButton;
        public Text ChatLogText;
        public Text ReminderText;
        public InputField ChatInput;
        public InputField QuickAddInput;
        public Button SendButton;
        public Button MicButton;
        public Button QuickAddButton;
        public Button RefreshButton;
        public Button TodayTab;
        public Button WeekTab;
        public Button InboxTab;
        public Button CompletedTab;
        public Button SettingsTab;
    }

    public static class UiFactory
    {
        private static readonly Color BackdropColor = new(0.14f, 0.08f, 0.05f, 1f);
        private static readonly Color PanelColor = new(0.18f, 0.11f, 0.07f, 0.98f);
        private static readonly Color PanelSoftColor = new(0.24f, 0.15f, 0.10f, 0.96f);
        private static readonly Color CardColor = new(0.29f, 0.17f, 0.11f, 0.96f);
        private static readonly Color CardStrongColor = new(0.37f, 0.20f, 0.11f, 0.97f);
        private static readonly Color AccentColor = new(0.98f, 0.39f, 0.03f, 1f);
        private static readonly Color AccentSoftColor = new(1f, 0.60f, 0.24f, 0.92f);
        private static readonly Color BorderColor = new(0.47f, 0.25f, 0.13f, 0.78f);
        private static readonly Color TextPrimary = new(0.98f, 0.96f, 0.93f, 1f);
        private static readonly Color TextSecondary = new(0.86f, 0.79f, 0.72f, 1f);
        private static readonly Color TextMuted = new(0.68f, 0.63f, 0.59f, 1f);

        public static AssistantUiRefs Build(Transform parent)
        {
            EnsureEventSystem();
            var font = LoadFont();
            var chromeSprite = LoadBuiltinSprite("UI/Skin/UISprite.psd", "UISprite.psd", "UI/Skin/Background.psd", "Background.psd");
            var inputSprite = LoadBuiltinSprite("UI/Skin/InputFieldBackground.psd", "InputFieldBackground.psd", "UI/Skin/Background.psd", "Background.psd");
            var knobSprite = LoadBuiltinSprite("UI/Skin/Knob.psd", "Knob.psd", "UI/Skin/UISprite.psd", "UISprite.psd");
            var checkmarkSprite = LoadBuiltinSprite("UI/Skin/Checkmark.psd", "Checkmark.psd", "UI/Skin/UISprite.psd", "UISprite.psd");
            var refs = new AssistantUiRefs();

            var canvasRoot = new GameObject("AssistantCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasRoot.transform.SetParent(parent, false);
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            BuildBackdrop(canvasRoot.transform, chromeSprite, font);
            BuildTopBar(canvasRoot.transform, chromeSprite, inputSprite, font, refs);
            BuildSidebar(canvasRoot.transform, chromeSprite, font, refs);
            BuildStagePanel(canvasRoot.transform, chromeSprite, font, refs);
            BuildTaskSheet(canvasRoot.transform, chromeSprite, inputSprite, knobSprite, checkmarkSprite, font, refs);
            BuildChatPanel(canvasRoot.transform, chromeSprite, inputSprite, font, refs);

            return refs;
        }

        private static void BuildBackdrop(Transform parent, Sprite sprite, Font font)
        {
            var backdrop = CreateSurface(parent, "Backdrop", BackdropColor, sprite, Color.clear, false);
            Stretch(backdrop.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var leftGlow = CreateSurface(parent, "LeftGlow", new Color(0.74f, 0.31f, 0.09f, 0.14f), sprite, Color.clear, false);
            Stretch(leftGlow.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-120f, -260f), new Vector2(300f, 260f));

            var rightGlow = CreateSurface(parent, "RightGlow", new Color(0.98f, 0.43f, 0.04f, 0.10f), sprite, Color.clear, false);
            Stretch(rightGlow.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-300f, -300f), new Vector2(140f, 300f));

            var centerMark = CreateText(parent, "CenterBackdropMark", font, 18, TextAnchor.MiddleCenter);
            centerMark.text = "LOCAL ASSISTANT";
            centerMark.color = new Color(0.42f, 0.28f, 0.21f, 0.75f);
            Stretch(centerMark.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-180f, -30f), new Vector2(180f, 30f));
        }

        private static void BuildTopBar(Transform parent, Sprite sprite, Sprite inputSprite, Font font, AssistantUiRefs refs)
        {
            var topBar = CreateSurface(parent, "TopBar", PanelColor, sprite, BorderColor);
            Stretch(topBar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -82f), new Vector2(0f, 0f));

            var brandMark = CreateSurface(topBar.transform, "BrandMark", AccentColor, sprite, Color.clear, false);
            Stretch(brandMark.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, -18f), new Vector2(52f, 18f));

            var brandMarkText = CreateText(brandMark.transform, "BrandMarkText", font, 18, TextAnchor.MiddleCenter);
            brandMarkText.text = "AI";
            brandMarkText.fontStyle = FontStyle.Bold;
            brandMarkText.color = new Color(0.20f, 0.09f, 0.03f, 1f);
            Stretch(brandMarkText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var brandTitle = CreateText(topBar.transform, "BrandTitle", font, 24, TextAnchor.MiddleLeft);
            brandTitle.text = "Tro ly Ao";
            brandTitle.fontStyle = FontStyle.Bold;
            brandTitle.color = AccentColor;
            Stretch(brandTitle.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(64f, 0f), new Vector2(210f, 0f));

            refs.TodayTab = CreateButton(topBar.transform, font, "TodayButton", "Trang chu", new Vector2(210f, 18f), new Vector2(320f, -18f), sprite, anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0f, 1f), fillColor: PanelColor);
            refs.WeekTab = CreateButton(topBar.transform, font, "WeekButton", "Lich trinh", new Vector2(324f, 18f), new Vector2(466f, -18f), sprite, anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0f, 1f), fillColor: PanelColor);
            refs.SettingsTab = CreateButton(topBar.transform, font, "SettingsButton", "Cai dat", new Vector2(470f, 18f), new Vector2(582f, -18f), sprite, anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0f, 1f), fillColor: PanelColor);

            var searchShell = CreateSurface(topBar.transform, "SearchShell", CardColor, sprite, BorderColor, false);
            Stretch(searchShell.rectTransform, new Vector2(0.58f, 0.5f), new Vector2(0.86f, 0.5f), new Vector2(0f, -22f), new Vector2(0f, 22f));

            var searchInput = CreateInputField(searchShell.transform, font, "TopSearchInput", "Tim kiem tinh nang...", inputSprite);
            Stretch(searchInput.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(14f, 6f), new Vector2(-14f, -6f));
            searchInput.interactable = false;

            var profileCard = CreateSurface(topBar.transform, "ProfileCard", PanelSoftColor, sprite, BorderColor, false);
            Stretch(profileCard.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-230f, -24f), new Vector2(-18f, 24f));

            var profileTitle = CreateText(profileCard.transform, "ProfileTitle", font, 16, TextAnchor.UpperRight);
            profileTitle.text = "Minh Anh";
            profileTitle.fontStyle = FontStyle.Bold;
            Stretch(profileTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0.78f, 1f), new Vector2(12f, -8f), new Vector2(0f, -2f));

            var profileSubtitle = CreateText(profileCard.transform, "ProfileSubtitle", font, 12, TextAnchor.LowerRight);
            profileSubtitle.text = "Premium Member";
            profileSubtitle.color = AccentSoftColor;
            Stretch(profileSubtitle.rectTransform, new Vector2(0f, 0f), new Vector2(0.78f, 0f), new Vector2(12f, 2f), new Vector2(0f, 8f));

            var profileAvatar = CreateSurface(profileCard.transform, "ProfileAvatar", new Color(0.95f, 0.83f, 0.74f, 1f), sprite, AccentColor, false);
            Stretch(profileAvatar.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-44f, -18f), new Vector2(-8f, 18f));

            var profileAvatarText = CreateText(profileAvatar.transform, "ProfileAvatarText", font, 18, TextAnchor.MiddleCenter);
            profileAvatarText.text = "U";
            profileAvatarText.fontStyle = FontStyle.Bold;
            profileAvatarText.color = new Color(0.30f, 0.15f, 0.09f, 1f);
            Stretch(profileAvatarText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void BuildSidebar(Transform parent, Sprite sprite, Font font, AssistantUiRefs refs)
        {
            var sidebar = CreateSurface(parent, "Sidebar", PanelColor, sprite, BorderColor);
            Stretch(sidebar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(320f, -82f));

            var systemLabel = CreateText(sidebar.transform, "SystemLabel", font, 14, TextAnchor.UpperLeft);
            systemLabel.text = "HE THONG";
            systemLabel.fontStyle = FontStyle.Bold;
            systemLabel.color = new Color(0.73f, 0.76f, 0.82f, 1f);
            Stretch(systemLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -22f), new Vector2(-20f, -2f));

            refs.HealthBanner = CreateText(sidebar.transform, "HealthBanner", font, 17, TextAnchor.UpperLeft);
            refs.HealthBanner.fontStyle = FontStyle.Bold;
            Stretch(refs.HealthBanner.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -54f), new Vector2(-84f, -18f));

            refs.RefreshButton = CreateButton(sidebar.transform, font, "RefreshButton", "Lam moi", new Vector2(-116f, -58f), new Vector2(-20f, -22f), sprite, anchorMin: new Vector2(1f, 1f), anchorMax: new Vector2(1f, 1f), fillColor: CardStrongColor);

            var stateButton = CreateStaticMenuButton(sidebar.transform, font, sprite, "SidebarState", "Trang thai", new Vector2(20f, -146f), new Vector2(-20f, -90f), true);
            refs.AvatarStateText = CreateText(stateButton.transform, "AvatarStateText", font, 12, TextAnchor.LowerRight);
            refs.AvatarStateText.color = new Color(0.33f, 0.16f, 0.06f, 0.92f);
            Stretch(refs.AvatarStateText.rectTransform, new Vector2(0.48f, 0f), new Vector2(1f, 1f), new Vector2(0f, 6f), new Vector2(-16f, -6f));

            CreateStaticMenuButton(sidebar.transform, font, sprite, "SidebarCloset", "Tu do", new Vector2(20f, -206f), new Vector2(-20f, -154f), false);
            CreateStaticMenuButton(sidebar.transform, font, sprite, "SidebarDevices", "Thiet bi", new Vector2(20f, -266f), new Vector2(-20f, -214f), false);
            CreateStaticMenuButton(sidebar.transform, font, sprite, "SidebarAlerts", "Thong bao", new Vector2(20f, -326f), new Vector2(-20f, -274f), false);
        }

        private static void BuildStagePanel(Transform parent, Sprite sprite, Font font, AssistantUiRefs refs)
        {
            var stagePanel = CreateSurface(parent, "StagePanel", PanelColor, sprite, BorderColor);
            Stretch(stagePanel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(320f, 502f), new Vector2(-368f, -82f));

            var stageStatusCard = CreateSurface(stagePanel.transform, "StageStatusCard", CardColor, sprite, BorderColor);
            Stretch(stageStatusCard.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 160f));

            refs.StageStatusText = CreateText(stageStatusCard.transform, "StageStatusText", font, 15, TextAnchor.UpperLeft);
            refs.StageStatusText.color = TextSecondary;
            Stretch(refs.StageStatusText.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 18f), new Vector2(-20f, -18f));

            var portraitCard = CreateSurface(stagePanel.transform, "PortraitCard", new Color(0.90f, 0.91f, 0.86f, 1f), sprite, BorderColor);
            Stretch(portraitCard.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), Vector2.zero);

            var portraitShade = CreateSurface(portraitCard.transform, "PortraitShade", new Color(0.05f, 0.05f, 0.06f, 0.12f), sprite, Color.clear, false);
            Stretch(portraitShade.rectTransform, new Vector2(0.12f, 0.06f), new Vector2(0.88f, 0.98f), Vector2.zero, Vector2.zero);

            var portraitCore = CreateSurface(portraitCard.transform, "PortraitCore", new Color(0.14f, 0.19f, 0.24f, 0.95f), sprite, Color.clear, false);
            Stretch(portraitCore.rectTransform, new Vector2(0.30f, 0.12f), new Vector2(0.70f, 0.90f), Vector2.zero, Vector2.zero);

            var portraitFace = CreateSurface(portraitCore.transform, "PortraitFace", new Color(0.93f, 0.84f, 0.77f, 1f), sprite, Color.clear, false);
            Stretch(portraitFace.rectTransform, new Vector2(0.24f, 0.20f), new Vector2(0.76f, 0.76f), Vector2.zero, Vector2.zero);

            var portraitEyes = CreateText(portraitFace.transform, "PortraitEyes", font, 40, TextAnchor.MiddleCenter);
            portraitEyes.text = "o   o";
            portraitEyes.color = new Color(0.18f, 0.16f, 0.16f, 0.62f);
            Stretch(portraitEyes.rectTransform, new Vector2(0.2f, 0.48f), new Vector2(0.8f, 0.70f), Vector2.zero, Vector2.zero);

            refs.StagePlaceholderText = CreateText(portraitCard.transform, "StagePlaceholderText", font, 22, TextAnchor.UpperCenter);
            refs.StagePlaceholderText.color = new Color(0.24f, 0.16f, 0.11f, 0.78f);
            Stretch(refs.StagePlaceholderText.rectTransform, new Vector2(0.18f, 0f), new Vector2(0.82f, 0.28f), new Vector2(0f, 8f), new Vector2(0f, -8f));

            var subtitleCard = CreateSurface(parent, "SubtitleCard", new Color(0.26f, 0.15f, 0.10f, 0.98f), sprite, new Color(0.79f, 0.38f, 0.05f, 0.55f));
            Stretch(subtitleCard.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-280f, 422f), new Vector2(280f, 482f));

            refs.SubtitleText = CreateText(subtitleCard.transform, "SubtitleText", font, 17, TextAnchor.MiddleCenter);
            refs.SubtitleText.color = new Color(1f, 0.92f, 0.82f, 1f);
            Stretch(refs.SubtitleText.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 10f), new Vector2(-18f, -10f));
            refs.SubtitleText.gameObject.SetActive(false);
        }

        private static void BuildTaskSheet(Transform parent, Sprite chromeSprite, Sprite inputSprite, Sprite knobSprite, Sprite checkmarkSprite, Font font, AssistantUiRefs refs)
        {
            var sheet = CreateSurface(parent, "TaskSheet", PanelColor, chromeSprite, BorderColor);
            Stretch(sheet.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(320f, 0f), new Vector2(-368f, 500f));

            var handle = CreateSurface(sheet.transform, "TaskSheetHandle", new Color(0.86f, 0.54f, 0.26f, 0.35f), chromeSprite, Color.clear, false);
            Stretch(handle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-36f, -10f), new Vector2(36f, -4f));

            var header = CreateSurface(sheet.transform, "TaskSheetHeader", new Color(0.23f, 0.13f, 0.08f, 1f), chromeSprite, BorderColor, false);
            Stretch(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, -72f));

            var headerTitle = CreateText(header.transform, "TaskSheetHeaderTitle", font, 18, TextAnchor.MiddleLeft);
            headerTitle.text = "LICH TRINH & CONG VIEC";
            headerTitle.fontStyle = FontStyle.Bold;
            headerTitle.color = TextPrimary;
            Stretch(headerTitle.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(24f, 0f), new Vector2(260f, 0f));

            var monthLabel = CreateText(header.transform, "TaskSheetMonthLabel", font, 16, TextAnchor.MiddleCenter);
            monthLabel.text = $"Thang {System.DateTime.Now.Month}, {System.DateTime.Now.Year}";
            monthLabel.fontStyle = FontStyle.Bold;
            Stretch(monthLabel.rectTransform, new Vector2(0.34f, 0f), new Vector2(0.66f, 1f), Vector2.zero, Vector2.zero);

            refs.InboxTab = CreateButton(header.transform, font, "InboxButton", "Inbox", new Vector2(-214f, 18f), new Vector2(-126f, -18f), chromeSprite, anchorMin: new Vector2(1f, 0f), anchorMax: new Vector2(1f, 1f), fillColor: CardStrongColor);
            refs.CompletedTab = CreateButton(header.transform, font, "DoneButton", "Da xong", new Vector2(-118f, 18f), new Vector2(-18f, -18f), chromeSprite, anchorMin: new Vector2(1f, 0f), anchorMax: new Vector2(1f, 1f), fillColor: CardStrongColor);

            var leftColumn = CreateSurface(sheet.transform, "TaskSheetLeftColumn", new Color(0.21f, 0.12f, 0.08f, 0.98f), chromeSprite, BorderColor, false);
            Stretch(leftColumn.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(278f, -72f));

            refs.QuickAddButton = CreateButton(leftColumn.transform, font, "QuickAddButton", "TAO MOI", new Vector2(20f, -116f), new Vector2(-20f, -70f), chromeSprite, fillColor: AccentColor);
            refs.QuickAddInput = CreateInputField(leftColumn.transform, font, "QuickAddInput", "Them cong viec nhanh...", inputSprite);
            Stretch(refs.QuickAddInput.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -164f), new Vector2(-20f, -122f));

            var summaryTitle = CreateText(leftColumn.transform, "SummaryTitle", font, 15, TextAnchor.UpperLeft);
            summaryTitle.text = "LICH CUA TOI";
            summaryTitle.fontStyle = FontStyle.Bold;
            summaryTitle.color = TextMuted;
            Stretch(summaryTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -206f), new Vector2(-20f, -182f));

            refs.TaskSummaryText = CreateText(leftColumn.transform, "TaskSummaryText", font, 15, TextAnchor.UpperLeft);
            refs.TaskSummaryText.color = TextSecondary;
            Stretch(refs.TaskSummaryText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -232f), new Vector2(-20f, -270f));

            var taskListTitle = CreateText(leftColumn.transform, "TaskListTitle", font, 15, TextAnchor.UpperLeft);
            taskListTitle.text = "VIEC CAN LAM";
            taskListTitle.fontStyle = FontStyle.Bold;
            taskListTitle.color = TextMuted;
            Stretch(taskListTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -334f), new Vector2(-20f, -310f));

            var taskListCard = CreateSurface(leftColumn.transform, "TaskListCard", CardColor, chromeSprite, BorderColor, false);
            Stretch(taskListCard.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(20f, 20f), new Vector2(-20f, -350f));

            refs.TaskContentText = CreateText(taskListCard.transform, "TaskContentText", font, 16, TextAnchor.UpperLeft);
            refs.TaskContentText.color = TextPrimary;
            Stretch(refs.TaskContentText.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 14f), new Vector2(-16f, -14f));

            var calendar = CreateSurface(sheet.transform, "TaskSheetCalendar", new Color(0.20f, 0.11f, 0.07f, 0.98f), chromeSprite, BorderColor, false);
            Stretch(calendar.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(278f, 0f), new Vector2(0f, -72f));
            BuildCalendarGrid(calendar.transform, chromeSprite, font);

            var settingsPanel = CreateSurface(sheet.transform, "SettingsPanel", new Color(0.20f, 0.11f, 0.07f, 0.98f), chromeSprite, BorderColor);
            Stretch(settingsPanel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -72f));
            settingsPanel.gameObject.SetActive(false);
            refs.SettingsPanel = settingsPanel.gameObject;

            refs.SettingsSummaryText = CreateText(settingsPanel.transform, "SettingsSummaryText", font, 16, TextAnchor.UpperLeft);
            refs.SettingsSummaryText.color = TextPrimary;
            Stretch(refs.SettingsSummaryText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -24f), new Vector2(-24f, -88f));

            refs.SpeakRepliesToggle = CreateToggle(settingsPanel.transform, font, "SpeakRepliesToggle", "Speak voice replies", new Vector2(24f, -120f), new Vector2(-24f, -84f), chromeSprite, knobSprite, checkmarkSprite);
            refs.TranscriptPreviewToggle = CreateToggle(settingsPanel.transform, font, "TranscriptPreviewToggle", "Show transcript preview", new Vector2(24f, -160f), new Vector2(-24f, -124f), chromeSprite, knobSprite, checkmarkSprite);
            refs.MiniAssistantToggle = CreateToggle(settingsPanel.transform, font, "MiniAssistantToggle", "Enable mini assistant mode", new Vector2(24f, -200f), new Vector2(-24f, -164f), chromeSprite, knobSprite, checkmarkSprite);
            refs.ReminderSpeechToggle = CreateToggle(settingsPanel.transform, font, "ReminderSpeechToggle", "Read reminders aloud", new Vector2(24f, -240f), new Vector2(-24f, -204f), chromeSprite, knobSprite, checkmarkSprite);

            refs.SettingsStatusText = CreateText(settingsPanel.transform, "SettingsStatusText", font, 15, TextAnchor.MiddleLeft);
            refs.SettingsStatusText.color = TextSecondary;
            Stretch(refs.SettingsStatusText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 20f), new Vector2(-230f, 64f));

            refs.ReloadSettingsButton = CreateButton(settingsPanel.transform, font, "ReloadButton", "Reload", new Vector2(-218f, 20f), new Vector2(-116f, 64f), chromeSprite, anchorMin: new Vector2(1f, 0f), anchorMax: new Vector2(1f, 0f), fillColor: CardStrongColor);
            refs.SaveSettingsButton = CreateButton(settingsPanel.transform, font, "SaveButton", "Save", new Vector2(-108f, 20f), new Vector2(-24f, 64f), chromeSprite, anchorMin: new Vector2(1f, 0f), anchorMax: new Vector2(1f, 0f), fillColor: AccentColor);
        }

        private static void BuildChatPanel(Transform parent, Sprite chromeSprite, Sprite inputSprite, Font font, AssistantUiRefs refs)
        {
            var chatPanel = CreateSurface(parent, "ChatPanel", PanelColor, chromeSprite, BorderColor);
            Stretch(chatPanel.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-368f, 0f), new Vector2(0f, -82f));

            var chatHeader = CreateSurface(chatPanel.transform, "ChatHeader", new Color(0.22f, 0.13f, 0.08f, 1f), chromeSprite, BorderColor, false);
            Stretch(chatHeader.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, -92f));

            var chatTitle = CreateText(chatHeader.transform, "ChatTitle", font, 18, TextAnchor.UpperLeft);
            chatTitle.text = "Tro chuyen AI";
            chatTitle.fontStyle = FontStyle.Bold;
            chatTitle.color = TextPrimary;
            Stretch(chatTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(22f, -18f), new Vector2(-22f, -4f));

            var chatStatus = CreateText(chatHeader.transform, "ChatStatus", font, 14, TextAnchor.LowerLeft);
            chatStatus.text = "Truc tuyen";
            chatStatus.color = AccentSoftColor;
            Stretch(chatStatus.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(22f, 10f), new Vector2(-22f, 28f));

            var chatLogCard = CreateSurface(chatPanel.transform, "ChatLogCard", new Color(0.18f, 0.11f, 0.08f, 0.98f), chromeSprite, BorderColor, false);
            Stretch(chatLogCard.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 92f), new Vector2(-18f, -104f));

            refs.ChatLogText = CreateText(chatLogCard.transform, "ChatLogText", font, 18, TextAnchor.UpperLeft);
            refs.ChatLogText.color = TextPrimary;
            Stretch(refs.ChatLogText.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 18f), new Vector2(-18f, -18f));

            var chatInputCard = CreateSurface(chatPanel.transform, "ChatInputCard", CardColor, chromeSprite, BorderColor, false);
            Stretch(chatInputCard.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(18f, 18f), new Vector2(-18f, 78f));

            refs.ChatInput = CreateInputField(chatInputCard.transform, font, "ChatInput", "Nhap tin nhan...", inputSprite);
            Stretch(refs.ChatInput.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(12f, 8f), new Vector2(-126f, -8f));

            refs.SendButton = CreateButton(chatInputCard.transform, font, "SendButton", ">", new Vector2(-56f, 8f), new Vector2(-12f, -8f), chromeSprite, anchorMin: new Vector2(1f, 0f), anchorMax: new Vector2(1f, 1f), fillColor: AccentColor);
            refs.MicButton = CreateButton(chatInputCard.transform, font, "MicButton", "Mic", new Vector2(-114f, 8f), new Vector2(-62f, -8f), chromeSprite, anchorMin: new Vector2(1f, 0f), anchorMax: new Vector2(1f, 1f), fillColor: CardStrongColor);

            var reminderCard = CreateSurface(parent, "ReminderCard", new Color(0.28f, 0.16f, 0.09f, 0.98f), chromeSprite, BorderColor, false);
            Stretch(reminderCard.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(300f, 158f));

            var reminderTitle = CreateText(reminderCard.transform, "ReminderTitle", font, 15, TextAnchor.UpperLeft);
            reminderTitle.text = "Goi y hom nay";
            reminderTitle.fontStyle = FontStyle.Bold;
            reminderTitle.color = TextPrimary;
            Stretch(reminderTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -18f), new Vector2(-18f, -2f));

            refs.ReminderText = CreateText(reminderCard.transform, "ReminderText", font, 15, TextAnchor.UpperLeft);
            refs.ReminderText.color = TextSecondary;
            Stretch(refs.ReminderText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 18f), new Vector2(-18f, -42f));
            refs.ReminderText.gameObject.SetActive(false);
        }

        private static GameObject CreateStaticMenuButton(
            Transform parent,
            Font font,
            Sprite sprite,
            string name,
            string label,
            Vector2 offsetMin,
            Vector2 offsetMax,
            bool isActive)
        {
            var button = CreateSurface(parent, name, isActive ? AccentColor : PanelColor, sprite, isActive ? new Color(1f, 0.59f, 0.24f, 0.50f) : Color.clear, false);
            Stretch(button.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), offsetMin, offsetMax);

            var labelText = CreateText(button.transform, name + "Label", font, 16, TextAnchor.MiddleLeft);
            labelText.text = label;
            labelText.fontStyle = FontStyle.Bold;
            labelText.color = isActive ? new Color(1f, 0.97f, 0.92f, 1f) : TextPrimary;
            Stretch(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, 0f));

            return button.gameObject;
        }

        private static void BuildCalendarGrid(Transform parent, Sprite sprite, Font font)
        {
            var weekdayRow = CreateSurface(parent, "CalendarWeekdays", new Color(0.23f, 0.13f, 0.09f, 0.90f), sprite, Color.clear, false);
            Stretch(weekdayRow.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -42f), new Vector2(0f, 0f));

            var dayNames = new[] { "T2", "T3", "T4", "T5", "T6", "T7", "CN" };
            for (var i = 0; i < dayNames.Length; i++)
            {
                var label = CreateText(weekdayRow.transform, "Weekday" + i, font, 14, TextAnchor.MiddleCenter);
                label.text = dayNames[i];
                label.fontStyle = FontStyle.Bold;
                label.color = TextSecondary;
                Stretch(label.rectTransform, new Vector2(i / 7f, 0f), new Vector2((i + 1f) / 7f, 1f), Vector2.zero, Vector2.zero);
            }

            var month = System.DateTime.Now.Month;
            var year = System.DateTime.Now.Year;
            var firstDay = new System.DateTime(year, month, 1);
            var leading = ((int)firstDay.DayOfWeek + 6) % 7;
            var daysInMonth = System.DateTime.DaysInMonth(year, month);
            var today = System.DateTime.Now.Day;

            const int rows = 5;
            const int cols = 7;
            var day = 1;

            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < cols; col++)
                {
                    var cellIndex = row * cols + col;
                    var cell = CreateSurface(parent, $"CalendarCell{cellIndex}", new Color(0.18f, 0.10f, 0.07f, 0.90f), sprite, new Color(0.32f, 0.18f, 0.11f, 0.45f), false);
                    var offsetTop = -42f - row * 82f;
                    var offsetBottom = -124f - row * 82f;
                    Stretch(cell.rectTransform, new Vector2(col / 7f, 1f), new Vector2((col + 1f) / 7f, 1f), new Vector2(0f, offsetBottom), new Vector2(0f, offsetTop));

                    var number = CreateText(cell.transform, "DayLabel", font, 15, TextAnchor.UpperLeft);
                    number.color = TextPrimary;
                    Stretch(number.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -10f), new Vector2(-10f, -4f));

                    if (cellIndex < leading || day > daysInMonth)
                    {
                        number.text = string.Empty;
                        cell.color = new Color(0.17f, 0.10f, 0.07f, 0.55f);
                        continue;
                    }

                    number.text = day.ToString();
                    if (day == today)
                    {
                        number.color = AccentColor;
                        var dot = CreateSurface(cell.transform, "TodayDot", AccentColor, sprite, Color.clear, false);
                        Stretch(dot.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -16f), new Vector2(-10f, -8f));
                    }

                    if (day % 4 == 0)
                    {
                        var tag = CreateSurface(cell.transform, "TaskTag", day == today ? AccentColor : CardStrongColor, sprite, Color.clear, false);
                        Stretch(tag.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(8f, 10f), new Vector2(-8f, 34f));

                        var tagText = CreateText(tag.transform, "TaskTagText", font, 11, TextAnchor.MiddleCenter);
                        tagText.text = day == today ? "Hom nay" : "Task";
                        tagText.fontStyle = FontStyle.Bold;
                        Stretch(tagText.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 0f), new Vector2(-4f, 0f));
                    }

                    day++;
                }
            }
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

        private static Font LoadFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Sprite LoadBuiltinSprite(params string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                if (string.IsNullOrEmpty(candidate))
                {
                    continue;
                }

                var sprite = Resources.GetBuiltinResource<Sprite>(candidate);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static Image CreateSurface(Transform parent, string name, Color color, Sprite sprite, Color outlineColor, bool addShadow = true)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = color;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }

            if (outlineColor.a > 0f)
            {
                var outline = go.AddComponent<Outline>();
                outline.effectColor = outlineColor;
                outline.effectDistance = new Vector2(1f, -1f);
            }

            if (addShadow)
            {
                var shadow = go.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
                shadow.effectDistance = new Vector2(0f, -6f);
            }

            return image;
        }

        private static Text CreateText(Transform parent, string name, Font font, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            text.lineSpacing = 1.08f;
            text.color = TextPrimary;
            text.raycastTarget = false;

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.16f);
            shadow.effectDistance = new Vector2(0f, -1f);
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            Font font,
            string name,
            string label,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Sprite sprite,
            Vector2? anchorMin = null,
            Vector2? anchorMax = null,
            Color? fillColor = null)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = fillColor ?? CardStrongColor;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.69f, 0.39f, 0.18f, 0.45f);
            outline.effectDistance = new Vector2(1f, -1f);

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
            shadow.effectDistance = new Vector2(0f, -4f);

            var rect = go.GetComponent<RectTransform>();
            Stretch(rect, anchorMin ?? new Vector2(0f, 1f), anchorMax ?? new Vector2(0f, 1f), offsetMin, offsetMax);

            var text = CreateText(go.transform, "Label", font, 17, TextAnchor.MiddleCenter);
            text.text = label;
            text.fontStyle = FontStyle.Bold;
            text.color = TextPrimary;
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 4f), new Vector2(-10f, -4f));

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
            colors.pressedColor = new Color(1f, 0.88f, 0.76f, 0.92f);
            colors.selectedColor = new Color(1f, 0.92f, 0.82f, 0.95f);
            colors.disabledColor = new Color(0.52f, 0.56f, 0.66f, 0.65f);
            button.colors = colors;
            return button;
        }

        private static InputField CreateInputField(Transform parent, Font font, string name, string placeholder, Sprite sprite)
        {
            var go = new GameObject(name, typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.24f, 0.15f, 0.10f, 0.98f);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.70f, 0.39f, 0.18f, 0.35f);
            outline.effectDistance = new Vector2(1f, -1f);

            var text = CreateText(go.transform, "Text", font, 17, TextAnchor.MiddleLeft);
            text.color = TextPrimary;
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 8f), new Vector2(-14f, -8f));

            var placeholderText = CreateText(go.transform, "Placeholder", font, 17, TextAnchor.MiddleLeft);
            placeholderText.color = TextMuted;
            placeholderText.text = placeholder;
            Stretch(placeholderText.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 8f), new Vector2(-14f, -8f));

            var input = go.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholderText;
            input.lineType = InputField.LineType.MultiLineNewline;
            input.targetGraphic = image;
            input.selectionColor = new Color(1f, 0.58f, 0.18f, 0.25f);
            input.caretColor = TextPrimary;
            return input;
        }

        private static Toggle CreateToggle(
            Transform parent,
            Font font,
            string name,
            string label,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Sprite chromeSprite,
            Sprite knobSprite,
            Sprite checkmarkSprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            Stretch(rect, new Vector2(0f, 1f), new Vector2(1f, 1f), offsetMin, offsetMax);

            var background = CreateSurface(go.transform, "Background", new Color(0.24f, 0.15f, 0.10f, 1f), chromeSprite, new Color(0.70f, 0.39f, 0.18f, 0.35f), false);
            Stretch(background.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, -12f), new Vector2(24f, 12f));

            var checkmark = CreateSurface(background.transform, "Checkmark", AccentColor, checkmarkSprite ?? knobSprite ?? chromeSprite, Color.clear, false);
            Stretch(checkmark.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));

            var toggleLabel = CreateText(go.transform, "Label", font, 17, TextAnchor.MiddleLeft);
            toggleLabel.color = TextPrimary;
            toggleLabel.text = label;
            Stretch(toggleLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(36f, 0f), Vector2.zero);

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = checkmark;
            toggle.isOn = false;
            return toggle;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
