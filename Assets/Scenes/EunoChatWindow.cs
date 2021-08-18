using UnityEngine;
using TMPro;

public class EunoChatWindow : MonoBehaviour
{
	public TMP_Text chatHistory; //set in inspector
	public TMP_InputField chatInput; //set in inspector

	public void UpdateChat(string msg)
	{
		chatHistory.text += msg + "\n";
	}

	public void Clear()
	{
		chatHistory.text = "";
	}
}
