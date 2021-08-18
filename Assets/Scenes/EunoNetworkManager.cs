using UnityEngine;
using Mirror;
using UnityEngine.UI;
using TMPro;

public class EunoNetworkManager : NetworkManager
{
	[HideInInspector] public EunoPlayArea playArea;
	[SerializeField] private GameObject playAreaPrefab; //Set in inspector

	private RectTransform disconnectButton;
	private RectTransform connectPanel;
	private RectTransform gamePanel;

	public struct CreatePlayerMessage : NetworkMessage
	{
		public string name;
	}

	//register networkMessages, spawn playArea
	public override void OnStartServer()
	{
		NetworkServer.RegisterHandler<CreatePlayerMessage>(OnCreatePlayer);

		GameObject asdf = Instantiate(playAreaPrefab);
		NetworkServer.Spawn(asdf);
		playArea = asdf.GetComponent<EunoPlayArea>();
	}

	public override void Start()
	{
		disconnectButton = GameObject.Find("Canvas/Disconnect Button").GetComponent<RectTransform>();
		disconnectButton.anchoredPosition = new Vector3(20000, 0, 0); //deactivate
	
		connectPanel = GameObject.Find("Canvas/Connect Panel").GetComponent<RectTransform>(); //deactivating messes with assignment on player, so just move it out of sight
		connectPanel.anchoredPosition = new Vector3(-200, -100, 0); //activate

		//gamePanel = GameObject.Find("Canvas/Game Panel").GetComponent<RectTransform>(); //move everything out of sight as well because why not
		//gamePanel.anchoredPosition = new Vector3(20000, 0, 0); //deactivate
	}

	//toggle game UI panels
	public override void OnStartClient()
	{
		// activate, deactivate, activate
		disconnectButton.anchoredPosition = new Vector3(0, 0, 0);
		connectPanel.GetComponent<RectTransform>().anchoredPosition = new Vector3(20000, 0, 0);
		//gamePanel.anchoredPosition = new Vector3(-350, -110, 0);
	}
	
	//toggle game UI panels
	public override void OnStopClient()
	{
		//deactivate, activate, deactivate
		disconnectButton.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(20000, 0, 0);
		connectPanel.GetComponent<RectTransform>().anchoredPosition = new Vector3(-200, -100, 0);
		//gamePanel.anchoredPosition = new Vector3(20000, 0, 0);
	}

	//left game message for all connected clients
	public override void OnServerDisconnect(NetworkConnection conn)
	{
		//throw an error if host, I guess
		EunoPlayer player = conn.identity.GetComponent<EunoPlayer>();
		NetworkClient.localPlayer.GetComponent<EunoPlayer>().RpcUpdateChat($"<color=red>{player.playerName} has left the game</color>");

		base.OnServerDisconnect(conn);
	}

	//disable AutoCreatePlayer and do it here
	public override void OnClientConnect(NetworkConnection conn)
	{
		base.OnClientConnect(conn);

		//create a player with the username set on this client
		conn.Send(new CreatePlayerMessage {name = connectPanel.transform.Find("InputField Username").GetComponent<TMP_InputField>().text});
	}

	//disable AutoCreatePlayer and do it OnClientConnect
	private void OnCreatePlayer(NetworkConnection conn, CreatePlayerMessage networkMessage)
	{
		GameObject asdf = Instantiate(playerPrefab);
		asdf.name = $"Player {networkMessage.name} [connId={conn.connectionId}]";
		EunoPlayer localPlayer = asdf.GetComponent<EunoPlayer>();
		localPlayer.playerName = networkMessage.name;

		NetworkServer.AddPlayerForConnection(conn, asdf);
	}

	//called by UI
	public void SetAddress(string input)
	{
		networkAddress = input;
	}

	//called by UI
	public void Disconnect()
	{
		if (NetworkServer.active && NetworkClient.isConnected) StopHost();
		else if (NetworkClient.isConnected) StopClient();
		else if (NetworkServer.active) StopServer();
	}

	//called by UI
	public void QuitApplication()
	{
		Application.Quit();
	}
}
