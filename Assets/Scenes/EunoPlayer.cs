using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class EunoPlayer : NetworkBehaviour
{
	[SyncVar] public string playerName;
	public SyncList<EunoCard> closedHand = new SyncList<EunoCard>(); //interest management for security and bandwidth
	public SyncList<EunoCard> openHand = new SyncList<EunoCard>();

	private EunoOpponentUI opponentUI; //will be null on local player
	[SerializeField] private GameObject opponentUIPrefab; //set in inspector

	public EunoChatWindow chatWindow; //move to delegate or action to tidy up?
	public EunoPlayArea playArea; //set by the play area when it spawns locally

	public void Start()
	{		
		chatWindow = GameObject.Find("Canvas/Chat Panel").GetComponent<EunoChatWindow>();
		playArea = GameObject.Find("Canvas/Play Area").GetComponent<EunoPlayArea>();

		closedHand.Callback += OnClosedHandUpdated;
		openHand.Callback += OnOpenHandUpdated;

		if (isLocalPlayer)
		{
			chatWindow.Clear();
			CmdSendChat($"<color=red>{playerName} has joined the game</color>");
		}
		else
		{
			//create opponent UI
			GameObject opponentGO = Instantiate(opponentUIPrefab);
			opponentGO.name = $"Opponent {playerName}";
			opponentGO.transform.SetParent(GameObject.Find("Canvas/Opponent Panel").transform, false);	
			opponentUI = opponentGO.GetComponent<EunoOpponentUI>();
			opponentUI.SetPlayer(this);
			opponentUI.UpdateDisplay();
		}
	}

	public void Update()
	{
		if (!isLocalPlayer) return;
		
		if (Input.GetKeyUp(KeyCode.Return) && chatWindow.chatInput.isFocused)
		{
			if (chatWindow.chatInput.text.Trim() != "")
			{
				if (chatWindow.chatInput.text.StartsWith("/")) CmdDoCommand(chatWindow.chatInput.text);
				else CmdSendChat($"<b><color=#000660ff>{playerName}</color></b>: {chatWindow.chatInput.text}");
			}
			chatWindow.chatInput.text = "";
		}
	}

	void OnClosedHandUpdated(SyncList<EunoCard>.Operation op, int index, EunoCard oldCard, EunoCard newCard)
	{
		if (isLocalPlayer) playArea.UpdateLocalPlayerDisplay();
		else opponentUI.UpdateDisplay();
	}

	void OnOpenHandUpdated(SyncList<EunoCard>.Operation op, int index, EunoCard oldCard, EunoCard newCard)
	{
		if (isLocalPlayer) playArea.UpdateLocalPlayerDisplay();
		else opponentUI.UpdateDisplay();
	}

	public void OnDestroy()
	{
		if (isLocalPlayer)
		{
			chatWindow.Clear();
			chatWindow.UpdateChat($"<color=red><b>You have left the game</b></color>"); //client player is destroyed on disconnect
		}
		else
		{
			Destroy(opponentUI.gameObject);
		}
	}



	///<summary> Send chat message to everyone </summary>
	[Command]
	public void CmdSendChat(string msg)
	{
		RpcUpdateChat(msg);
	}

	///<summary> Recieve chat message </summary>
	[ClientRpc]
	public void RpcUpdateChat(string msg)
	{
		chatWindow.UpdateChat(msg);
	}

	///<summary> Recieve targeted chat message </summary>
	[TargetRpc]
	public void TgtUpdateChat(NetworkConnection target, string msg)
	{
		chatWindow.UpdateChat(msg);
	}

	///<summary> Append targeted chat message </summary>
	[TargetRpc]
	public void TgtAppendChat(NetworkConnection target, string msg)
	{
		chatWindow.AppendChat(msg);
	}

	///<summary> Does command on server </summary>
	[Command]
	public void CmdDoCommand(string cmd)
	{
		string[] args = cmd.Split(' ');

		TgtAppendChat(connectionToClient, "<color=grey><i>");
		switch (args[0])
		{
			case "/whisper":
			case "/w":
				NetworkConnectionToClient conn = null;
				foreach (NetworkConnectionToClient clientConnection in NetworkServer.connections.Values)
				{
					if (clientConnection.identity.GetComponent<EunoPlayer>().playerName == args[1])
					{
						conn = clientConnection;
						break;
					}
				}

				if (conn == null)
				{
					TgtUpdateChat(connectionToClient, $"Player <b>\"{args[1]}\"</b> doesn't exist!");
					break;
				}
				else
				{
					args[1] = playerName;
					TgtUpdateChat(connectionToClient, $"<b>{args[1]}</b> whispers: " + string.Join(" ", args, 2, args.Length - 2));
					TgtUpdateChat(conn, $"<color=grey><i><b>{args[1]}</b> whispers: " + string.Join(" ", args, 2, args.Length-2) + "</color></i>");
					break;
				}

			case "/players":
			case "/connections":
			case "/conns":
				StringBuilder sb = new StringBuilder();
				foreach (KeyValuePair<int, NetworkConnectionToClient> entry in NetworkServer.connections)
				{
					sb.AppendLine($"<b>{entry.Value.identity.GetComponent<EunoPlayer>().playerName}</b> - connID:{entry.Key}");
				}
				TgtUpdateChat(connectionToClient, sb.ToString().TrimEnd('\n'));
				break;


			case "/conn":
			case "/me":
			case "/player":
				TgtUpdateChat(connectionToClient, $"Player:<b>{playerName}</b>, connID:{connectionToClient.connectionId}, netID:{netId}\nconnected:{connectionToClient.address}\nping:{NetworkTime.rtt}, stdDev:{NetworkTime.rttStandardDeviation}");
				break;

			case "/turn": //key not present
				TgtUpdateChat(connectionToClient, $"turnIndex: {playArea.turnIndex}, turn of:<b>{NetworkServer.connections.ElementAt(playArea.turnIndex).Key}</b>, you:{NetworkClient.connection.connectionId}");
				break;

			case "/help":
				TgtUpdateChat(connectionToClient,
								"<b>/players</b> - Lists players.\n" +
								"<b>/whisper [target] [message]</b> - Sends a secret message to [target]. AKA <b>/w</b>\n" +
								"<b>/turn</b> - Turn"
								);
				break;

			default:
				TgtDoCommand(connectionToClient, cmd);
				break;
		}
		TgtAppendChat(connectionToClient, "</color></i>");
	}

	///<summary> Does command on client </summary>
	[TargetRpc]
	public void TgtDoCommand(NetworkConnection target, string cmd) //implement local commands that do more than just update chat here
	{
		string[] args = cmd.Split(' ');

		chatWindow.AppendChat("<color=grey><i>");
		switch (args[0])
		{
			case "/throw":
				CmdSendChat($"<color=red><b>{playerName}</b> is throwing!</color>");
				UnityEngine.Diagnostics.Utils.ForceCrash(UnityEngine.Diagnostics.ForcedCrashCategory.FatalError);
				break;

			case "/selected":
				CmdSendChat($"Closed: index:{playArea.closedSelectedIndex}, count{closedHand.Count}\nOpen: index:{ playArea.openSelectedIndex}, count{ openHand.Count}");
				break;

			default:
				chatWindow.UpdateChat($"Command </i>{args[0]}<i> is not valid. Try using /help.");
				break;
		}
		chatWindow.AppendChat("</color></i>");
	}
}