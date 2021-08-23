using UnityEngine;
using Mirror;

public class EunoPlayer : NetworkBehaviour
{
	[SyncVar] public string playerName;
	public SyncList<EunoCard> closedHand = new SyncList<EunoCard>(); //interest management -- players only observe their own hands, hand size is syncvar, reduces bandwidth requirements
	public SyncList<EunoCard> openHand = new SyncList<EunoCard>();

	private EunoOpponentUI opponentUI; //will be null on local player
	[SerializeField] private GameObject opponentUIPrefab; //set in inspector

	private EunoChatWindow chatWindow; //move to delegate or action to tidy up?
	public EunoPlayArea playArea; //set by the play area when it spawns locally

	public void Start()
	{
		chatWindow = GameObject.Find("Canvas/Chat Panel").GetComponent<EunoChatWindow>();
		chatWindow.Clear();
		playArea = GameObject.Find("Canvas/Play Area").GetComponent<EunoPlayArea>();

		closedHand.Callback += OnClosedHandUpdated;
		openHand.Callback += OnOpenHandUpdated;

		if (isLocalPlayer)
		{
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
		}
	}

	public void Update()
	{
		if (!isLocalPlayer) return;
		
		if (Input.GetKeyUp(KeyCode.Return) && chatWindow.chatInput.isFocused)
		{
			if (chatWindow.chatInput.text.Trim() != "") CmdSendChat($"<color=#0000a0ff>{playerName}:</color> {chatWindow.chatInput.text}");
			chatWindow.chatInput.text = "";
		}
	}

	public void OnDestroy()
	{
		if (isLocalPlayer)
		{
			chatWindow.Clear();
			chatWindow.UpdateChat($"<color=red>You have left the game</color>"); //client player is destroyed on disconnect
		}
		else
		{
			Destroy(opponentUI.gameObject);
		}
	}

	[Command(requiresAuthority = false)]
	public void AddClosedHandCard(EunoCard card)
	{
		closedHand.Add(card);
	}

	[Command(requiresAuthority = false)]
	public void AddOpenHandCard(EunoCard card)
	{
		openHand.Add(card);
	}

	void OnClosedHandUpdated(SyncList<EunoCard>.Operation op, int index, EunoCard oldCard, EunoCard newCard)
	{
		if (isLocalPlayer)
		{
			playArea.UpdateLocalPlayerDisplay();
		}
		else
		{
			opponentUI.SetClosedCount(closedHand.Count);
		}
	}

	void OnOpenHandUpdated(SyncList<EunoCard>.Operation op, int index, EunoCard oldCard, EunoCard newCard)
	{
		if (isLocalPlayer)
		{
			playArea.UpdateLocalPlayerDisplay();
		}
		else
		{
			opponentUI.SetOpenCount(openHand.Count);
		}
	}

	///<summary> Send a chat message to server instance </summary>
	[Command]
	public void CmdSendChat(string msg)
	{
		RpcUpdateChat(msg);
	}

	///<summary> Update chat with message on all clients </summary>
	[ClientRpc]
	public void RpcUpdateChat(string msg)
	{
		chatWindow.UpdateChat(msg);
	}
}