using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro; 
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
 
public class DialogUIController : MonoBehaviour
{
    public static event EventHandler OnDialogUIInstantiated = delegate {};
    public static event EventHandler OnChoicePressed = delegate {};
    private DialogController _dialogController;
    public DialogController DialogController
    {
        get => _dialogController;
        set
        {
            _dialogController = value;
            if(_dialogController != null)
            {
                _dialogController.OnStartDialog += OnStartDialog;
                _dialogController.OnNodeEnabled += OnNodeEnabled;
            }
            else
            {
                _dialogController.OnStartDialog -= OnStartDialog;
                _dialogController.OnNodeEnabled -= OnNodeEnabled;
            }
        }
    }

    [Header("GameObject References")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private GameObject inputFieldPanel;
    [SerializeField] private TextMeshProUGUI inputText;
    [SerializeField] private TextMeshProUGUI titleText;
    private LocalizeStringEvent titleLocalizeStringEvent;
    [SerializeField] private TextMeshProUGUI paragraphText;
    [SerializeField] private GameObject inventoryButton;

    [Header("Prefabs")]
    [SerializeField] private GameObject choiceButtonPrefab;

    private const string INTERACTABLES_TABLE = "Interactables";
    private const string DIALOG_ELEMENTS_TABLE = "DialogElements"; 
    private const string DIALOG_TABLE = "Dialogs"; 
    private const string CLOSE_KEY = "<Close>"; 
    private const string CONTINUE_KEY = "<Continue>";  
    private const string DEFAULT_KEY = "Out";

    private const char SPECIAL_COMMAND = '\\';
    private const char PAUSE_COMMAND = 'p';
    private bool _isSpecialCharacter = false; 
    private bool _skipWaitTime = false;

    private const float TIME_BETWEEN_LETTERS = 0.02f;
    private const float PAUSE_DURATION = .6f;

    //position changes
    private RectTransform _dialogPanelRT;
    private RectTransform _titleTextRT;
    private RectTransform _choicePanelRT;
    private VerticalLayoutGroup _choicePanelLayoutGroup;
    private CanvasGroup _choiceCanvasGroup; 
    private DialogNodeData _displayedNode; //to avoid displaying the same node multiple times at once
    private Dictionary<DialogPosition, DialogElementPosition> dialogPositionSettings = new Dictionary<DialogPosition, DialogElementPosition>();

    private class DialogElementPosition
    {
        public Vector2 textAnchor;
        public Vector2 choicesAnchor;
        public Vector2 choicesPivot;
        public TextAnchor choicePanelAlignment;
        public Vector2 titleAnchor;
        public Vector2 titlePivot;
        public TextAlignmentOptions titleAlignment;
        public TextAlignmentOptions textAlignment;
    }
    
    void Awake()
    {
        _dialogPanelRT = dialogPanel.transform as RectTransform;
        _titleTextRT = titleText.transform as RectTransform;
        _choicePanelRT = choicePanel.transform as RectTransform;
        
        titleLocalizeStringEvent = titleText.GetComponent<LocalizeStringEvent>();
        _choicePanelLayoutGroup = choicePanel.GetComponent<VerticalLayoutGroup>();
        _choiceCanvasGroup = choicePanel.GetComponent<CanvasGroup>();

        inputFieldPanel.SetActive(false);
        dialogPanel.SetActive(false);
        choicePanel.SetActive(false);

        InitElementPositions();
    }

    void Start()
    {
        OnDialogUIInstantiated(this, null);
    }

    void OnDestroy()
    {
        if(_dialogController)
        {
            _dialogController.OnStartDialog -= OnStartDialog;
            _dialogController.OnNodeEnabled -= OnNodeEnabled;
        }
        DialogController.OnEndDialog -= OnEndDialog;
    }

    private void InitElementPositions()
    {
        dialogPositionSettings = new Dictionary<DialogPosition, DialogElementPosition>()
        {
            [DialogPosition.Top] = new DialogElementPosition()
            {
                titleAnchor = new Vector2(0, 0),
                titlePivot = new Vector2(1, 0),
                textAnchor = new Vector2(0.5f, 1),
                choicesAnchor = new Vector2(1, 0),
                choicesPivot = new Vector2(1, 1),
                choicePanelAlignment = TextAnchor.UpperRight,
                titleAlignment = TextAlignmentOptions.BottomRight,
                textAlignment = TextAlignmentOptions.MidlineLeft,
            },
            [DialogPosition.Right] = new DialogElementPosition()
            {
                titleAnchor = new Vector2(1, 1),
                titlePivot = new Vector2(1, 0),
                textAnchor = new Vector2(1, 0.5f),
                choicesAnchor = new Vector2(1, 0),
                choicesPivot = new Vector2(1, 1),
                choicePanelAlignment = TextAnchor.UpperRight,
                titleAlignment = TextAlignmentOptions.BottomRight,
                textAlignment = TextAlignmentOptions.MidlineRight,
            },
            [DialogPosition.Bottom] = new DialogElementPosition()
            {
                titleAnchor = new Vector2(0, 0),
                titlePivot = new Vector2(1, 0),
                textAnchor = new Vector2(0.5f, 0),
                choicesAnchor = new Vector2(1, 1),
                choicesPivot = new Vector2(1, 0),
                choicePanelAlignment = TextAnchor.LowerRight,
                titleAlignment = TextAlignmentOptions.BottomRight,
                textAlignment = TextAlignmentOptions.MidlineLeft,
            },
            [DialogPosition.Left] = new DialogElementPosition()
            {
                titleAnchor = new Vector2(0, 1),
                titlePivot = new Vector2(0, 0),
                textAnchor = new Vector2(0, 0.5f),
                choicesAnchor = new Vector2(0, 0),
                choicesPivot = new Vector2(0, 1),
                choicePanelAlignment = TextAnchor.UpperLeft,
                titleAlignment = TextAlignmentOptions.BottomLeft,
                textAlignment = TextAlignmentOptions.MidlineLeft,
            },
            [DialogPosition.Center] = new DialogElementPosition()
            {
                titleAnchor = new Vector2(0, 0),
                titlePivot = new Vector2(1, 0),
                textAnchor = new Vector2(0.5f, 0.5f),
                choicesAnchor = new Vector2(1, 0),
                choicesPivot = new Vector2(1, 1),
                choicePanelAlignment = TextAnchor.UpperRight,
                titleAlignment = TextAlignmentOptions.BottomRight,
                textAlignment = TextAlignmentOptions.Midline,
            }
        };
    }

    private void OnStartDialog(object sender, DialogEvent e)
    {
        if(_dialogController == null)
        {
            Debug.LogError("No dialogController ref in DialogUIController");
            return;
        }
        ClearCurrentDialog();

        string dialogableName = string.Empty;

        InitDialog(dialogableName, false);
    }

    private void ClearCurrentDialog()
    {
        StopAllCoroutines();
        titleText.text = string.Empty;
        paragraphText.text = string.Empty;
    }

    private void OnNodeEnabled(object sender, DialogEvent e)
    {
        if(e.node is DialogNodeData dialogNodeData)
        {
            DisplayNode(dialogNodeData, e.choices);
        }
    }

    public void InitDialog(string npcName, bool canTrade)
    {
        bool hasNPC = !string.IsNullOrEmpty(npcName);
        inventoryButton.SetActive(hasNPC && canTrade);
        if(hasNPC)
        {
            titleLocalizeStringEvent.StringReference.SetReference(INTERACTABLES_TABLE, npcName);
            titleText.text = npcName;
        }
        else
        {
            titleText.text = "";
        }

        DialogController.OnEndDialog += OnEndDialog;
    }

    private void OnEndDialog(object sender, DialogEvent e)
    {
        DialogController.OnEndDialog -= OnEndDialog;
        CloseDialogUI();
    }

    private void OnPlayerTooFarFromTalkable(object sender, EventArgs e)
    {
        CloseDialogButton();
    }

    private void DisplayNode(DialogNodeData nodeData, List<NodeLinkData> choices)
    {
        if(_displayedNode == nodeData) 
        {
            return;
        }

        _displayedNode = nodeData;

        //hide prompt box
        inputFieldPanel.SetActive(false);

        //show dialog panel and choice
        dialogPanel.SetActive(true);
        choicePanel.SetActive(true);
        
        SetDynamicPosition();

        ClearChoices(); // remove old choices

        string localizedText = LocalizationSettings.StringDatabase.GetLocalizedString(DIALOG_TABLE, nodeData.DialogText);
        string text = FormatText(localizedText);

        StartCoroutine(DisplayText(text, () => 
        {
            //On text finished appearing
            DisplayChoices(choices);
        }));
    }

    private void SetDynamicPosition()
    {
        DialogPosition position = _dialogController.DialogGraph.Position;
        DialogElementPosition elementPosition = dialogPositionSettings[position];
        SetAnchorAndPivot(_titleTextRT, elementPosition.titleAnchor, elementPosition.titlePivot);
        SetAnchorAndPivot(_dialogPanelRT, elementPosition.textAnchor, elementPosition.textAnchor);
        SetAnchorAndPivot(_choicePanelRT, elementPosition.choicesAnchor, elementPosition.choicesPivot);
        _choicePanelLayoutGroup.childAlignment = elementPosition.choicePanelAlignment;
        titleText.alignment = elementPosition.titleAlignment;
        paragraphText.alignment = elementPosition.textAlignment;
    }

    private void SetAnchorAndPivot(RectTransform rt, Vector2 anchor, Vector2 pivot)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
    }

    private IEnumerator DisplayText(string text, Action callback)
    {
        paragraphText.text = "";

        _skipWaitTime = false;

        foreach(char c in text)
        {
            if(_skipWaitTime)
            {
                break;
            }
            yield return ProcessCharacter(c);
        }

        if(_skipWaitTime)
        {
            paragraphText.text = RemoveCommands(text);
        }

        callback?.Invoke();
    }

    private string RemoveCommands(string text)
    {
        string ret = "";
        for(int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if(c.Equals(SPECIAL_COMMAND))
            {
                i++;
            }
            else
            {
                ret += c;
            }
        }
        return ret;
    }

    private void OnSpacebarPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        _skipWaitTime = true;
    }

    private IEnumerator ProcessCharacter(char c)
    {
        if(_isSpecialCharacter)
        {
            _isSpecialCharacter = false;
            yield return HandleSpecialCharacter(c);
        }
        else if(c.Equals(SPECIAL_COMMAND))
        {
            _isSpecialCharacter = true;
        }
        else
        {
            paragraphText.text += c;
            yield return new WaitForSeconds(TIME_BETWEEN_LETTERS);
        }
    }

    private IEnumerator HandleSpecialCharacter(char c)
    {
        switch (c)
        {
            case PAUSE_COMMAND:
                yield return new WaitForSeconds(PAUSE_DURATION);
                break;
            
            // Add other cases here as needed
        }
    }

    private string FormatText(string dialogText)
    {
        // return dialogText.Replace("<PlayerName>", playerCharacter.name);
        return dialogText;
    }

    internal void ShowTextPrompt()
    {
        dialogPanel.SetActive(false); //hide ui
        choicePanel.SetActive(false);
        inputFieldPanel.SetActive(true); //show prompt box
    }

    public void ConfirmTextPrompt()
    {
        _dialogController.ConfirmTextPrompt(inputText.text);
    }

    private void DisplayChoices(List<NodeLinkData> choices)
    {
        _choiceCanvasGroup.alpha = 0;
        //No next node -> Display Close Button
        if (choices.Count == 0)
        {
            MakeChoiceButton(CLOSE_KEY, () => CloseDialogButton(), DIALOG_ELEMENTS_TABLE);
        }
        //Only one possibility as next node -> Display Continue Button (or the node's text if it has any)
        else if(choices.Count == 1)
        {
            NodeLinkData choice = choices[0];
            string text = CONTINUE_KEY;
            if(!string.Empty.Equals(choice.PortName) && !choice.PortName.Equals(DEFAULT_KEY)) //if no text is set on choice port, portName will be "Choice 0" which is replaced by <Continue>
            {
                text = choice.PortName; //otherwise it's the port name that's the text for that choice
            }
            MakeChoiceButton(text, () => HandleChoice(choice), DIALOG_ELEMENTS_TABLE);
        }
        else
        {
            //Display all possible choices
            foreach (NodeLinkData choice in choices)
            {
                MakeChoiceButton(choice.PortName, () => HandleChoice(choice), DIALOG_TABLE);
            }
        }

        // LayoutRebuilder.ForceRebuildLayoutImmediate(choicePanel.transform as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(choicePanel.transform as RectTransform);
        
        StartCoroutine(ShowChoices());
    }

    private void CloseDialogButton()
    {
        DialogController.StopConversation();
        CloseDialogUI();
    }

    private void CloseDialogUI()
    {
        StopAllCoroutines();

        _displayedNode = null;

        inputFieldPanel.SetActive(false);
        dialogPanel.SetActive(false);
        choicePanel.SetActive(false);
    }

    private IEnumerator ShowChoices()
    {
        while(_choiceCanvasGroup.alpha < 1)
        {
            _choiceCanvasGroup.alpha += Time.deltaTime;
            yield return null;
        }
        LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
    }

    private void HandleChoice(NodeLinkData choice)
    {
        _dialogController.MoveToNode(choice.TargetNodeGuid); //display next node
        OnChoicePressed(this, null);
    }

    private void MakeChoiceButton(string choiceStr, Action listener, string localizationTable)
    {
        GameObject choiceButton = Instantiate(choiceButtonPrefab, choicePanel.transform);

        choiceButton.GetComponentInChildren<TextMeshProUGUI>().text = choiceStr;
        LocalizeStringEvent localizeStringEvent = choiceButton.GetComponentInChildren<LocalizeStringEvent>();
        localizeStringEvent.StringReference.SetReference(localizationTable, choiceStr);

        choiceButton.GetComponent<Button>().onClick.AddListener(() => { listener(); });

        LayoutRebuilder.ForceRebuildLayoutImmediate(choiceButton.transform as RectTransform);
    }


    private void ClearChoices()
    {
        foreach (Transform choice in choicePanel.transform)
        {
            Destroy(choice.gameObject);
        }
    }
}
