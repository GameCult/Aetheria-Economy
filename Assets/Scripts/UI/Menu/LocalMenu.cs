using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ink.Runtime;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class LocalMenu : MonoBehaviour
{
    public ObservablePointerClickTrigger ContinueTrigger;
    public TextMeshProUGUI Output;
    public RectTransform ChoiceParent;
    public ChoicePrefab ChoicePrefab;
    
    private Story _currentStory;
    private List<GameObject> _choiceInstances = new List<GameObject>();
    
    private void OnEnable()
    {
        _currentStory = ActionGameManager.Instance.GetStories.First();
        Continue();
    }

    void Start()
    {
        ContinueTrigger.OnPointerClickAsObservable().Subscribe(pointerEvent =>
        {
            if (_currentStory == null) return;
            
            if(_currentStory.canContinue) Continue();
        });
    }

    void Continue()
    {
        Output.text = _currentStory.Continue();

        foreach(var instance in _choiceInstances)
            Destroy(instance);
        _choiceInstances.Clear();
        
        foreach (var choice in _currentStory.currentChoices)
        {
            var choiceInstance = Instantiate(ChoicePrefab, ChoiceParent);
            choiceInstance.Label.text = choice.text;
            choiceInstance.Button.onClick.AddListener(() =>
            {
                _currentStory.ChoosePath(choice.targetPath);
                Continue();
            });
            _choiceInstances.Add(choiceInstance.gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
