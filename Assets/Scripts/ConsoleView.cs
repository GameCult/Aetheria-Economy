/// <summary>
/// Marshals events and data between ConsoleController and uGUI.
/// </summary>
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Collections;
using System.Linq;
using TMPro;
using UniRx;

public class ConsoleView : MonoBehaviour {
	bool _didShow = false;

	public GameObject ViewContainer; //Container for console view, should be a child of this GameObject
	public TextMeshProUGUI LogTextArea;
    public ScrollRect Scroll;

    private string _inputString = "";
    private string[] _log;
    public bool AcceptingInput { get; private set; }

    public ConsoleView()
    {
        AcceptingInput = false;
    }

    public bool Visible { get; private set; }

    void Start()
	{
        //_console = new ConsoleController();
        Application.logMessageReceived += (condition, trace, type) => ConsoleController.Instance.AppendLogLine("<color=" + (type == LogType.Error ? "red" : type == LogType.Warning ? "yellow" : "white") + ">" + condition + "</color>");

        SetVisibility(false);

		ConsoleController.Instance.VisibilityChanged += OnVisibilityChanged;
        ConsoleController.Instance.LogChanged += OnLogChanged;
        
		UpdateLogStr(ConsoleController.Instance.Log);
		
		// var sizeFitter = GetComponentInChildren<ContentSizeFitter>().on
	}
	
	~ConsoleView() {
        ConsoleController.Instance.VisibilityChanged -= OnVisibilityChanged;
        ConsoleController.Instance.LogChanged -= OnLogChanged;
	}
	
	void Update() {
		//Toggle visibility when tilde key pressed
		if (Input.GetKeyUp("`")) {
			ToggleVisibility();
		}

	    if (Visible)
	    {
	        string s = Input.inputString;

	        if (s.Length > 0)
	        {
	            if (s.Contains("\b"))
	            {
                    if(_inputString.Length>0)
	                    _inputString = _inputString.Substring(0, _inputString.Length - 1);
	            }
                else
                    _inputString += s;
                DisplayLog();
	        }

            if (Input.GetKeyDown(KeyCode.Return))
	        {
	            if (AcceptingInput && _inputString.Length > 0)
	            {
	                if(_inputString.StartsWith("!") || _inputString.StartsWith("/"))
                        ConsoleController.Instance.RunCommandString(_inputString.Substring(1));
                    else ConsoleController.Instance.RunCommandString($"say \"{_inputString}\"");
	            }
	            AcceptingInput = !AcceptingInput;
	            _inputString = "";
                DisplayLog();
	        }
	    }
	}

    /// <summary>
    /// Example of a console command. Can be on any behavior but always has lowercase name and string array parameter
    /// </summary>
    /// <param name="args"></param>
    void echo(string[] args)
    {
        if(args.Length>0)
            Debug.Log(args.Aggregate((a,b) => a + " " + b));
    }

	void ToggleVisibility() {
		SetVisibility(!ViewContainer.activeSelf);
	}
	
	void SetVisibility(bool visible) {
		ViewContainer.SetActive(visible);
	    Visible = visible;
		if(visible)
			Cursor.lockState = CursorLockMode.None;
	}

    void OnVisibilityChanged(bool visible) {
		SetVisibility(visible);
	}
	
	void OnLogChanged(string[] newLog) {
		UpdateLogStr(newLog);
	}
	
	void UpdateLogStr(string[] newLog)
	{
	    _log = newLog;
	    DisplayLog();
	}

    void DisplayLog()
    {
        if (_log == null)
        {
            LogTextArea.text = "";
        }
        else
        {
            LogTextArea.text = string.Join("\n", _log);
            if (AcceptingInput)
                LogTextArea.text += "\n\n> " + _inputString;
        }

        Observable.NextFrame().Subscribe(_ => Scroll.verticalNormalizedPosition = 0);
    }

}
