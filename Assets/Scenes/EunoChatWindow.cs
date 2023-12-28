using UnityEngine;
using TMPro;

public class EunoChatWindow : MonoBehaviour
{
	public TMP_Text chatHistory; //set in inspector
	public TMP_InputField chatInput; //set in inspector

	///<summary> Adds a chat message on a new line </summary>
	public void UpdateChat(string msg)
	{
		chatHistory.text += msg + "\n";
	}

	///<summary> Adds a chat message without the \n </summary>
	public void AppendChat(string msg)
	{
		chatHistory.text += msg;
	}

	///<summary> Clears chat </summary>
	public void Clear()
	{
		chatHistory.text = "";
	}
}