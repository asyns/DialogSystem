using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
{
    private const string DIALOG_NODE = " Dialog Node";
    private const string QUEST_NODE = " Quest Node";
    private const string EXIT_NODE = " Exit Node";
    private const string PROMPT_NODE = " Prompt Node";
    private const string BRANCH_NODE = " Branch Node";
    private const string GOTOGRAPH_NODE = " Go to Graph Node";
    private const string CHOICE_NODE = " Choice Node";
    private DialogGraphView _graphView;
    private EditorWindow _window;

    public void Init(DialogGraphView graphView, EditorWindow window)
    {
        _graphView = graphView;
        _window = window;
    }

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        var tree = new List<SearchTreeEntry>
        {
            new SearchTreeGroupEntry(new GUIContent("Create Elements"), 0),
            new SearchTreeEntry(new GUIContent(DIALOG_NODE))
            {
                userData = new DialogNode(), level=1
            },
            new SearchTreeEntry(new GUIContent(QUEST_NODE))
            {
                userData = new StartQuestNode(), level=1
            },
            new SearchTreeEntry(new GUIContent(EXIT_NODE))
            {
                userData = new ExitNode(), level=1
            },
            new SearchTreeEntry(new GUIContent(PROMPT_NODE))
            {
                userData = new PromptTextNode(), level=1
            },
            new SearchTreeEntry(new GUIContent(BRANCH_NODE))
            {
                userData = new BranchNode(), level=1
            },
            new SearchTreeEntry(new GUIContent(GOTOGRAPH_NODE))
            {
                userData = new GoToGraphNode(), level=1
            },
            new SearchTreeEntry(new GUIContent(CHOICE_NODE))
            {
                userData = new ChoiceNode(), level=1
            }
        };

        return tree;
    }

    public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
    {
        var worldMousePos = _window.rootVisualElement.ChangeCoordinatesTo(_window.rootVisualElement.parent, context.screenMousePosition - _window.position.position);
        var localMousePos = _graphView.contentViewContainer.WorldToLocal(worldMousePos);
        
        _graphView.MakeNewNode(SearchTreeEntry.userData, localMousePos);
        return true;
    }
}
