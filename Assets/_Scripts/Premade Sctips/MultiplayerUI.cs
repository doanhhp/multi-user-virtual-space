using UnityEngine;
using UnityEngine.UIElements;

public class MultiplayerUI : MonoBehaviour
{
    [SerializeField] private UIDocument m_uiDocument;
    
    public static bool IsUIActive { get; private set; } = false;

    private VisualElement m_boardControls;
    private TextField m_urlInputField;
    private Label m_indicatorLabel;
    private VisualElement m_videoControlsRow;
    private Button m_playPauseBtn;
    private Slider m_videoSlider;
    private Slider m_volumeSlider;
    private bool m_isDraggingSlider = false;
    private bool m_isDraggingVolumeSlider = false;

    private VisualElement m_npcChatContainer;
    private TextField m_npcInputField;

    private void Awake()
    {
        var root = m_uiDocument.rootVisualElement;

        root.style.width = Length.Percent(100);
        root.style.height = Length.Percent(100);
        root.style.justifyContent = Justify.Center;
        root.style.alignItems = Align.Center;

        var oldHostBtn = root.Q<VisualElement>("ButtonHost");
        if (oldHostBtn != null) oldHostBtn.style.display = DisplayStyle.None;

        var oldClientBtn = root.Q<VisualElement>("ButtonClient");
        if (oldClientBtn != null) oldClientBtn.style.display = DisplayStyle.None;

        var oldDisconnectBtn = root.Q<VisualElement>("ButtonDisconnect");
        if (oldDisconnectBtn != null) oldDisconnectBtn.style.display = DisplayStyle.None;

        SetupBoardUI(root);
        SetupNpcUI(root); 
    }

    private void Start()
    {
        CloseAllUI();
        SetActiveState(true);
    }

    private void Update()
    {
        if (m_boardControls != null && m_boardControls.style.display == DisplayStyle.Flex)
        {
            var board = FindFirstObjectByType<NetworkedMediaBoard>();
            if (board != null)
            {
                int current = board.GetCurrentIndex() + 1;
                int total = board.GetTotalMediaCount();
                if (total == 0) current = 0; // Show 0 / 0 if empty
                m_indicatorLabel.text = $"{current} / {total}";

                if (board.IsVideoActive())
                {
                    m_videoControlsRow.style.display = DisplayStyle.Flex;
                    m_playPauseBtn.text = board.IsVideoPaused() ? "Play" : "Pause";
                    
                    if (!m_isDraggingSlider)
                    {
                        m_videoSlider.SetValueWithoutNotify(board.GetVideoProgress());
                    }
                    if (!m_isDraggingVolumeSlider)
                    {
                        m_volumeSlider.SetValueWithoutNotify(board.GetVolume());
                    }
                }
                else
                {
                    m_videoControlsRow.style.display = DisplayStyle.None;
                }
            }
        }
    }

    private void SetupBoardUI(VisualElement root)
    {
        m_boardControls = new VisualElement();
        StyleContainer(m_boardControls, bottom: 40, left: 40);
        m_boardControls.style.flexDirection = FlexDirection.Column;
        m_boardControls.style.alignItems = Align.FlexStart;

        var row1 = new VisualElement();
        row1.style.flexDirection = FlexDirection.Row;
        row1.style.alignItems = Align.Center;
        
        m_urlInputField = new TextField();
        StyleInputField(m_urlInputField, "Paste Image URL:", 500);
        
        Button addBtn = new Button { text = "Add" };
        StyleAccentButton(addBtn, Color.clear); 
        addBtn.clicked += () => 
        {
            if (string.IsNullOrEmpty(m_urlInputField.value)) return;
            var board = FindFirstObjectByType<NetworkedMediaBoard>();
            if (board != null) board.RequestAddMedia(m_urlInputField.value);
            m_urlInputField.value = "";
        };

        Button prevBtn = new Button { text = "<" };
        StyleAccentButton(prevBtn, Color.clear); 
        prevBtn.clicked += () => 
        {
            var board = FindFirstObjectByType<NetworkedMediaBoard>();
            if (board != null) board.RequestPrevSlide();
        };

        Button nextBtn = new Button { text = ">" };
        StyleAccentButton(nextBtn, Color.clear); 
        nextBtn.clicked += () => 
        {
            var board = FindFirstObjectByType<NetworkedMediaBoard>();
            if (board != null) board.RequestNextSlide();
        };

        m_indicatorLabel = new Label("0 / 0");
        m_indicatorLabel.style.color = Color.white;
        m_indicatorLabel.style.fontSize = 20;
        m_indicatorLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        m_indicatorLabel.style.marginLeft = 15;
        m_indicatorLabel.style.marginRight = 15;

        Button delBtn = new Button { text = "Delete" };
        StyleAccentButton(delBtn, Color.clear); 
        delBtn.clicked += () => 
        {
            var board = FindFirstObjectByType<NetworkedMediaBoard>();
            if (board != null) board.RequestDeleteCurrent();
        };

        Button closeBtn = new Button { text = "X" };
        StyleAccentButton(closeBtn, Color.clear); 
        closeBtn.clicked += CloseAllUI;

        row1.Add(m_urlInputField);
        row1.Add(addBtn);
        row1.Add(prevBtn);
        row1.Add(m_indicatorLabel);
        row1.Add(nextBtn);
        row1.Add(delBtn);
        row1.Add(closeBtn);

        m_videoControlsRow = new VisualElement();
        m_videoControlsRow.style.flexDirection = FlexDirection.Row;
        m_videoControlsRow.style.alignItems = Align.Center;
        m_videoControlsRow.style.marginTop = 10;
        m_videoControlsRow.style.display = DisplayStyle.None;

        m_playPauseBtn = new Button { text = "Pause" };
        StyleAccentButton(m_playPauseBtn, Color.clear);
        m_playPauseBtn.clicked += () => 
        {
            var board = FindFirstObjectByType<NetworkedMediaBoard>();
            if (board != null) board.RequestTogglePause();
        };

        m_videoSlider = new Slider(0f, 1f);
        m_videoSlider.style.width = 400;
        m_videoSlider.style.marginLeft = 15;
        m_videoSlider.RegisterCallback<GeometryChangedEvent>(e => {
            var dragger = m_videoSlider.Q("unity-dragger");
            if (dragger != null)
            {
                dragger.style.width = 24;
                dragger.style.height = 24;
                dragger.style.marginTop = -12;
                dragger.style.borderTopLeftRadius = 12;
                dragger.style.borderTopRightRadius = 12;
                dragger.style.borderBottomLeftRadius = 12;
                dragger.style.borderBottomRightRadius = 12;
            }
        });
        
        m_videoSlider.RegisterCallback<PointerCaptureEvent>(e => m_isDraggingSlider = true);
        m_videoSlider.RegisterCallback<PointerCaptureOutEvent>(e => {
            m_isDraggingSlider = false;
            var board = FindFirstObjectByType<NetworkedMediaBoard>();
            if (board != null) board.RequestSeekToPercent(m_videoSlider.value);
        });
        // Removed PointerLeaveEvent to fix the slider rubber-banding when the mouse leaves the track during a drag

        // Volume Slider Setup
        m_volumeSlider = new Slider(0f, 1f);
        m_volumeSlider.style.width = 100;
        m_volumeSlider.style.marginLeft = 15;
        m_volumeSlider.value = 1f; // Default volume
        m_volumeSlider.RegisterCallback<GeometryChangedEvent>(e => {
            var dragger = m_volumeSlider.Q("unity-dragger");
            if (dragger != null)
            {
                dragger.style.width = 24;
                dragger.style.height = 24;
                dragger.style.marginTop = -12;
                dragger.style.borderTopLeftRadius = 12;
                dragger.style.borderTopRightRadius = 12;
                dragger.style.borderBottomLeftRadius = 12;
                dragger.style.borderBottomRightRadius = 12;
            }
        });

        var volLabel = new Label("Vol");
        volLabel.style.color = Color.white;
        volLabel.style.marginLeft = 15;
        volLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        m_volumeSlider.RegisterCallback<PointerCaptureEvent>(e => m_isDraggingVolumeSlider = true);
        m_volumeSlider.RegisterCallback<PointerCaptureOutEvent>(e => {
            m_isDraggingVolumeSlider = false;
            var board = FindFirstObjectByType<NetworkedMediaBoard>();
            if (board != null) board.RequestSetVolume(m_volumeSlider.value);
        });

        m_videoControlsRow.Add(m_playPauseBtn);
        m_videoControlsRow.Add(m_videoSlider);
        m_videoControlsRow.Add(volLabel);
        m_videoControlsRow.Add(m_volumeSlider);

        m_boardControls.Add(row1);
        m_boardControls.Add(m_videoControlsRow);

        root.Add(m_boardControls);
        m_boardControls.style.display = DisplayStyle.None;
    }

    private void SetupNpcUI(VisualElement root)
    {
        m_npcChatContainer = new VisualElement();
        StyleContainer(m_npcChatContainer, bottom: 120, left: 40);
        
        m_npcInputField = new TextField();
        StyleInputField(m_npcInputField, "Ask AI Assistant:", 500);
        
        Button sendBtn = new Button { text = "Send to AI" };
        StyleAccentButton(sendBtn, new Color(0.2f, 0.7f, 0.3f)); 
        sendBtn.clicked += () => 
        {
            if (string.IsNullOrEmpty(m_npcInputField.value)) return;
            var npc = FindFirstObjectByType<NpcBrain>();
            if (npc != null) npc.AskQuestion(m_npcInputField.value);
            m_npcInputField.value = ""; 
            CloseAllUI(); 
        };

        Button closeBtn = new Button { text = "X" };
        StyleAccentButton(closeBtn, new Color(0.8f, 0.2f, 0.2f)); 
        closeBtn.clicked += CloseAllUI;

        m_npcChatContainer.Add(m_npcInputField);
        m_npcChatContainer.Add(sendBtn);
        m_npcChatContainer.Add(closeBtn);
        root.Add(m_npcChatContainer);
        m_npcChatContainer.style.display = DisplayStyle.None;
    }

    private void StyleContainer(VisualElement el, int bottom, int left)
    {
        el.style.position = Position.Absolute;
        el.style.bottom = bottom;
        el.style.left = left;
        el.style.flexDirection = FlexDirection.Row;
        el.style.alignItems = Align.Center;
        el.style.backgroundColor = new Color(0f, 0f, 0f, 0f); // fully transparent container
        el.style.paddingTop = 15; el.style.paddingBottom = 15;
        el.style.paddingLeft = 20; el.style.paddingRight = 20;
    }

    private void StyleInputField(TextField field, string placeholder, int width)
    {
        field.label = placeholder;
        field.style.width = width;
        field.style.fontSize = 20; 
        field.style.color = Color.white; 
        field.style.unityFontStyleAndWeight = FontStyle.Bold;
        
        var labelEl = field.Q<Label>();
        if (labelEl != null)
        {
            labelEl.style.width = 170;
            labelEl.style.minWidth = 170;
        }

        var innerInput = field.Q("unity-text-input");
        if (innerInput != null)
        {
            innerInput.style.color = Color.white; 
            innerInput.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            innerInput.style.fontSize = 20;
            innerInput.style.paddingTop = 10; innerInput.style.paddingBottom = 10;
            innerInput.style.borderTopWidth = 2; innerInput.style.borderBottomWidth = 2;
            innerInput.style.borderLeftWidth = 2; innerInput.style.borderRightWidth = 2;
            innerInput.style.borderTopColor = Color.white; innerInput.style.borderBottomColor = Color.white;
            innerInput.style.borderLeftColor = Color.white; innerInput.style.borderRightColor = Color.white;
        }
    }

    private void StyleAccentButton(Button btn, Color accentColor)
    {
        btn.style.backgroundColor = new Color(0.61f, 0.55f, 0.52f, 0.8f); // Taupe minimal background
        btn.style.color = Color.white;
        btn.style.fontSize = 18;
        btn.style.unityFontStyleAndWeight = FontStyle.Bold;
        
        // Minimal white border
        btn.style.borderTopWidth = 2; btn.style.borderBottomWidth = 2;
        btn.style.borderLeftWidth = 2; btn.style.borderRightWidth = 2;
        btn.style.borderTopColor = new Color(1f, 1f, 1f, 0.8f);
        btn.style.borderBottomColor = new Color(1f, 1f, 1f, 0.8f);
        btn.style.borderLeftColor = new Color(1f, 1f, 1f, 0.8f);
        btn.style.borderRightColor = new Color(1f, 1f, 1f, 0.8f);

        btn.style.paddingTop = 12; btn.style.paddingBottom = 12;
        btn.style.paddingLeft = 20; btn.style.paddingRight = 20;
        btn.style.marginLeft = 15;
    }

    public void OpenClassroomUI() { m_boardControls.style.display = DisplayStyle.Flex; SetActiveState(true); }
    public void OpenNpcChatUI() { m_npcChatContainer.style.display = DisplayStyle.Flex; SetActiveState(true); }
    
    public void CloseAllUI() 
    { 
        m_boardControls.style.display = DisplayStyle.None; 
        m_npcChatContainer.style.display = DisplayStyle.None;
        SetActiveState(false); 
    }

    private void SetActiveState(bool isActive)
    {
        IsUIActive = isActive; 
        UnityEngine.Cursor.lockState = isActive ? CursorLockMode.None : CursorLockMode.Locked;
        UnityEngine.Cursor.visible = isActive;
    }

    public void DisableButtons() => SetActiveState(false);
    public void EnableButtons() { CloseAllUI(); SetActiveState(true); }
}