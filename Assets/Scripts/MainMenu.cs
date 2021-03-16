using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Unity.Mathematics.math;
using Random = UnityEngine.Random;

public class MainMenu : MonoBehaviour
{
    public SectorGenerationSettings Settings;
    public ConfirmationDialog Dialog;
    public bool InGame;
    public Prototype PanelPrototype;
    public float FadeTime = .5f;
    public float FadeDistance = 512;
    public float FadeAlphaExponent = 2;
    public float FadePositionExponent = 2;

    private (PropertiesPanel panel, CanvasGroup group) _currentMenu, _nextMenu;
    private bool _fadeFromRight;
    private float _fadeLerp;
    private bool _fading;
    private Vector3 _panelPosition;
    //private Task<DatabaseCache> _databaseLoad;
    
    void Start()
    {
        // Start loading the database in the background 'cause it takes a few seconds
        //_databaseLoad = Task.Run(() => ActionGameManager.Database);
        
        RegisterResolver.Register();
        _panelPosition = PanelPrototype.transform.position;
        
        var panel1 = PanelPrototype.Instantiate<PropertiesPanel>();
        _currentMenu = (panel1, panel1.GetComponent<CanvasGroup>());
        
        var panel2 = PanelPrototype.Instantiate<PropertiesPanel>();
        _nextMenu = (panel2, panel2.GetComponent<CanvasGroup>());

        _currentMenu.panel.gameObject.SetActive(false);
        //_saveDirectory = ActionGameManager.GameDataDirectory.CreateSubdirectory("Saves");
        
        ShowMain();
        Fade(true);
    }

    private void Update()
    {
        if (_fading)
        {
            _fadeLerp += Time.deltaTime / FadeTime;

            _currentMenu.panel.transform.position = 
                _panelPosition + (_fadeFromRight ? Vector3.left : Vector3.right) * (FadeDistance * pow(_fadeLerp, FadePositionExponent));
            _nextMenu.panel.transform.position = 
                _panelPosition + (_fadeFromRight ? Vector3.right : Vector3.left) * (FadeDistance * pow(1-_fadeLerp, FadePositionExponent));
            _currentMenu.group.alpha = pow(1 - _fadeLerp, FadeAlphaExponent);
            _nextMenu.group.alpha = pow(_fadeLerp, FadeAlphaExponent);
            
            if (_fadeLerp > 1)
            {
                _fading = false;
                _currentMenu.panel.gameObject.SetActive(false);
                var temp = _currentMenu;
                _currentMenu = _nextMenu;
                _nextMenu = temp;
            }
        }
    }

    private string TitleSubtitle(string title, string subtitle) => $"{title}\n<smallcaps><size=50%>{subtitle}";

    private void Fade(bool fromRight)
    {
        _nextMenu.panel.gameObject.SetActive(true);
        _nextMenu.group.alpha = 0;
        _fading = true;
        _fadeLerp = 0;
        _fadeFromRight = fromRight;
    }

    private void ShowMain()
    {
        _nextMenu.panel.Clear();
        _nextMenu.panel.Title.text = TitleSubtitle("aetheria", "terminus");
        if (!InGame)
        {
            if(ActionGameManager.PlayerSettings.CurrentRun != null)
                _nextMenu.panel.AddButton("Continue",
                    () =>
                    {
                        ActionGameManager.CurrentSector = new Sector(ActionGameManager.Database, ActionGameManager.PlayerSettings.CurrentRun);
                        SceneManager.LoadScene("ARPG");
                    });
            else
                _nextMenu.panel.AddButton("Continue", null);
        }
        _nextMenu.panel.AddButton("New Game",
            () =>
            {
                var generatorState = "Loading Database Contents";
                Action<string> setState = s => generatorState = s;

                Fade(true);
                _nextMenu.panel.gameObject.SetActive(false);
                Dialog.Clear();
                Dialog.Title.text = "Generating Galaxy";
                Dialog.AddProperty(() => generatorState);
                Dialog.Show();
                Settings.NoisePosition = Random.value * 100;
                
                var task = Task.Run(() => new Sector(Settings, ActionGameManager.Database, 0, setState));
                task.ContinueWith(task => Observable.NextFrame().Subscribe(_ =>
                {
                    ActionGameManager.PlayerSettings.CurrentRun = null;
                    ActionGameManager.CurrentSector = task.Result;
                    SceneManager.LoadScene("ARPG");
                }));
            });
        _nextMenu.panel.AddButton("Settings",
            () =>
            {
                ShowSettings();
                Fade(true);
            });
        _nextMenu.panel.AddButton("Quit", Application.Quit);
    }

    private void ShowSettings()
    {
        _nextMenu.panel.Clear();
        _nextMenu.panel.Title.text = "settings";
        _nextMenu.panel.AddButton("Gameplay",
            () =>
            {
                ShowGameplaySettings();
                Fade(true);
            });
        _nextMenu.panel.AddButton("Graphics",
            () =>
            {
                ShowGraphicsSettings();
                Fade(true);
            });
        _nextMenu.panel.AddButton("Input",
            () =>
            {
                ShowInputSettings();
                Fade(true);
            });
        _nextMenu.panel.AddButton("Audio",
            () =>
            {
                ShowAudioSettings();
                Fade(true);
            });
        _nextMenu.panel.AddButton("Back",
            () =>
            {
                ShowMain();
                Fade(false);
            });
    }
    
    private void ShowGameplaySettings()
    {
        _nextMenu.panel.Clear();
        _nextMenu.panel.Title.text = TitleSubtitle("gameplay", "settings");
        _nextMenu.panel.AddField("Name", 
            () => ActionGameManager.PlayerSettings.Name,
            s => ActionGameManager.PlayerSettings.Name = s);
        _nextMenu.panel.AddButton("Back",
            () =>
            {
                ActionGameManager.SavePlayerSettings();
                ShowSettings();
                Fade(false);
            });
    }

    private void ShowGraphicsSettings()
    {
        _nextMenu.panel.Clear();
        _nextMenu.panel.Title.text = TitleSubtitle("graphics", "settings");
        _nextMenu.panel.AddButton("Back",
            () =>
            {
                ShowSettings();
                Fade(false);
            });
    }

    private void ShowInputSettings()
    {
        _nextMenu.panel.Clear();
        _nextMenu.panel.Title.text = TitleSubtitle("input", "settings");
        _nextMenu.panel.AddButton("Back",
            () =>
            {
                ShowSettings();
                Fade(false);
            });
    }

    private void ShowAudioSettings()
    {
        _nextMenu.panel.Clear();
        _nextMenu.panel.Title.text = TitleSubtitle("audio", "settings");
        _nextMenu.panel.AddButton("Back",
            () =>
            {
                ShowSettings();
                Fade(false);
            });
    }
}
