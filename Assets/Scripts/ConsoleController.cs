/// <summary>
/// Handles parsing and execution of console commands, as well as collecting log output.
/// </summary>
using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class ConsoleController
{
	public static Component MessageReceiver;
    private static ConsoleController instance;
    private static Regex permittedCharacters = new Regex("[^a-zA-Z0-9 -]");

    private ConsoleController()
    {
    }

    public static ConsoleController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new ConsoleController();
            }
            return instance;
        }
    }

    #region Event declarations
    // Used to communicate with ConsoleView
    public delegate void LogChangedHandler(string[] log);
	public event LogChangedHandler LogChanged;
	
	public delegate void VisibilityChangedHandler(bool visible);
	public event VisibilityChangedHandler VisibilityChanged;
	#endregion

	/// <summary>
	/// How many log lines should be retained?
	/// Note that strings submitted to appendLogLine with embedded newlines will be counted as a single line.
	/// </summary>
	const int ScrollbackSize = 100;

	Queue<string> _scrollback = new Queue<string>(ScrollbackSize);
	List<string> _commandHistory = new List<string>();

	public string[] Log { get; private set; } //Copy of scrollback as an array for easier use by ConsoleView
	
	public void AppendLogLine(string line) {
		//Debug.Log(line);
		
		if (_scrollback.Count >= ConsoleController.ScrollbackSize) {
			_scrollback.Dequeue();
		}
		_scrollback.Enqueue(line);
		
		Log = _scrollback.ToArray();
		if (LogChanged != null) {
			LogChanged(Log);
		}
	}
	
	public void RunCommandString(string commandString) {
		AppendLogLine("$ " + commandString);
		
		string[] commandSplit = ParseArguments(commandString);
		string[] args = {"",""};
		if (commandSplit.Length < 1) {
			AppendLogLine(string.Format("Unable to process command '{0}'", commandString));
			return;
			
		}
		if (commandSplit.Length >= 2) {
			int numArgs = commandSplit.Length - 1;
			args = new string[numArgs];
			Array.Copy(commandSplit, 1, args, 0, numArgs);
		}
		MessageReceiver.SendMessage(commandSplit[0].ToLower(), args, SendMessageOptions.DontRequireReceiver);
		_commandHistory.Add(commandString);
	}
	
	static string[] ParseArguments(string commandString)
	{
		LinkedList<char> parmChars = new LinkedList<char>(commandString.ToCharArray());
		bool inQuote = false;
		var node = parmChars.First;
		while (node != null)
		{
			var next = node.Next;
			if (node.Value == '"') {
				inQuote = !inQuote;
				parmChars.Remove(node);
			}
			if (!inQuote && node.Value == ' ') {
				node.Value = '\n';
			}
			node = next;
		}
		char[] parmCharsArr = new char[parmChars.Count];
		parmChars.CopyTo(parmCharsArr, 0);
		var args = new string(parmCharsArr).Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
		for (var i = 0; i < args.Length; i++)
		{
			args[i] = permittedCharacters.Replace(args[i], "");
		}
		return args;
	}
}
