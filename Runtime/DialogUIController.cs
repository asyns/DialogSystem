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
    private bool isSpecialCharacter = false; 
    private bool skipWaitTime = false;

    private const float TIME_BETWEEN_LETTERS = 0.02f;
    private const float PAUSE_DURATION = .6f;
    private const float AWAY_DISTANCE = 6.0f;

    //position changes
    RectTransform dialogPanelRT;
    RectTransform titleTextRT;
    VerticalLayoutGroup choicePanelLayoutGroup;
    CanvasGroup choiceCanvasGroup; 
    private DialogNodeData displayedNode; //to avoid displaying the same node multiple times at once


    void Awake()
    {
        dialogPanelRT = dialogPanel.transform as RectTransform;
        titleTextRT = titleText.transform as RectTransform;
        titleLocalizeStringEvent = titleText.GetComponent<LocalizeStringEvent>();
        choicePanelLayoutGroup = choicePanel.GetComponent<VerticalLayoutGroup>();
        choiceCanvasGroup = choicePanel.GetComponent<CanvasGroup>();

        inputFieldPanel.SetActive(false);
        dialogPanel.SetActive(false);
        choicePanel.SetActive(false);
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
        if(displayedNode == nodeData) 
        {
            return;
        }

        displayedNode = nodeData;

        //hide prompt box
        inputFieldPanel.SetActive(false);

        //show dialog panel and choice
        dialogPanel.SetActive(true);
        choicePanel.SetActive(true);

        ClearChoices(); // remove old choices

        string localizedText = LocalizationSettings.StringDatabase.GetLocalizedString(DIALOG_TABLE, nodeData.DialogText);
        string text = FormatText(localizedText);

        StartCoroutine(DisplayText(text, () => 
        {
            //On text finished appearing
            DisplayChoices(choices);
        }));
    }

    private IEnumerator DisplayText(string text, Action callback)
    {
        paragraphText.text = "";

        skipWaitTime = false;

        foreach(char c in text)
        {
            if(skipWaitTime)
            {
                break;
            }
            yield return ProcessCharacter(c);
        }

        if(skipWaitTime)
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
        skipWaitTime = true;
    }

    private IEnumerator ProcessCharacter(char c)
    {
        if(isSpecialCharacter)
        {
            isSpecialCharacter = false;
            yield return HandleSpecialCharacter(c);
        }
        else if(c.Equals(SPECIAL_COMMAND))
        {
            isSpecialCharacter = true;
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
        dialogPanel.SetActive(false);//hide ui
        choicePanel.SetActive(false);
        inputFieldPanel.SetActive(true); //show prompt box
    }

    public void ConfirmTextPrompt()
    {
        _dialogController.ConfirmTextPrompt(inputText.text);
    }

    private void DisplayChoices(List<NodeLinkData> choices)
    {
        choiceCanvasGroup.alpha = 0;
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
        
        StartCoroutine(LetChoicesAppear());
    }

    private void CloseDialogButton()
    {
        DialogController.CloseDialog();
        CloseDialogUI();
    }

    private void CloseDialogUI()
    {
        StopAllCoroutines();

        displayedNode = null;

        inputFieldPanel.SetActive(false);
        dialogPanel.SetActive(false);
        choicePanel.SetActive(false);
    }

    private IEnumerator LetChoicesAppear()
    {
        while(choiceCanvasGroup.alpha < 1)
        {
            choiceCanvasGroup.alpha += Time.deltaTime;
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
        // LayoutRebuilder.MarkLayoutForRebuild(choicePanel.transform as RectTransform);
    }


    private void ClearChoices()
    {
        foreach (Transform choice in choicePanel.transform)
        {
            Destroy(choice.gameObject);
        }
    }
}
