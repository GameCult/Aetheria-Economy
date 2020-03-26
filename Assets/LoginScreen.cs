using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginScreen : MonoBehaviour
{
    public FlatButton LoginButton;
    public FlatButton RegisterButton;
    public TMP_InputField EmailUsername;
    public TMP_InputField Email;
    public TMP_InputField Username;
    public TMP_InputField Password;
    public TextMeshProUGUI Error;

    private bool _registering;
    
    void Start()
    {
        Error.text = "";
        LoginButton.CurrentState = FlatButtonState.Selected;
        CultClient.AddMessageListener<LoginSuccessMessage>(success => SceneManager.LoadScene("Main"));
        CultClient.AddMessageListener<ErrorMessage>(error => Error.text = error.Error);
        RegisterButton.OnClick += _ =>
        {
            if (_registering)
            {
                CultClient.Register(Email.text, Username.text, Password.text);
            }
            else
            {
                _registering = true;
                EmailUsername.gameObject.SetActive(false);
                Email.gameObject.SetActive(true);
                Username.gameObject.SetActive(true);
                LoginButton.CurrentState = FlatButtonState.Unselected;
                RegisterButton.CurrentState = FlatButtonState.Selected;
            }
        };
        LoginButton.OnClick += _ =>
        {
            if (_registering)
            {
                _registering = false;
                EmailUsername.gameObject.SetActive(true);
                Email.gameObject.SetActive(false);
                Username.gameObject.SetActive(false);
                LoginButton.CurrentState = FlatButtonState.Selected;
                RegisterButton.CurrentState = FlatButtonState.Unselected;
            }
            else
            {
                CultClient.Login(EmailUsername.text, Password.text);
            }
        };
    }
}
