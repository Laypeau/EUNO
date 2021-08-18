using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

public enum CardColour
{
	Red,
	Green,
	Blue,
	Yellow,
	Uncoloured
}

public enum CardType
{
	Zero,
	One,
	Two,
	Three,
	Four,
	Five,
	Six,
	Seven,
	Eight,
	Nine,
	Skip,
	Reverse,
	Wild,
	PlusTwo,
	PlusFour
}

public struct EunoCard
{
	public EunoCard(CardColour colour, CardType type)
	{
		this.colour = colour;
		this.type = type;
	}

	public CardColour colour;
	public CardType type;

	public override string ToString() => $"{colour} {type}";
}

public enum EunoPile
{
	closed,
	open,
	toggle
}

public class EunoPlayArea : NetworkBehaviour
{
	[SyncVar] public EunoCard closedPile;
	[SyncVar] public EunoCard openPile;
	[SyncVar] public EunoCard togglePile;

	[SyncVar] public TMP_Text closedPileText;
	[SyncVar] public TMP_Text openPileText;
	[SyncVar] public TMP_Text togglePileText;

	public RectTransform selectTransform; /////set in inspector
	public TMP_Text playerClosedCountText;  
	public TMP_Text playerClosedDisplayText;
	public TMP_Text playerOpenCountText;
	public TMP_Text playerOpenDisplayText;

	private bool evenDraw = false;

	private bool isCloseSelected = true;

	[Server]
	public override void OnStartServer()
	{
		closedPile = GetRandomCard();
		openPile = GetRandomCard();
		togglePile = GetRandomCard();

		closedPileText.text = closedPile.ToString();
		openPileText.text = openPile.ToString();
		togglePileText.text = togglePile.ToString();
	}

	public void Start()
	{
		transform.SetParent(GameObject.Find("Canvas").transform, false);
		gameObject.name = "Play Area"; //set name so EunoPlayer can use GameObject.Find()  -- EunoPlayer is created after this     !!!!!use events to get rid of this!!!!!
	}

	//called by UI button
	public void DrawCard()
	{
		EunoPlayer player = NetworkClient.localPlayer.GetComponent<EunoPlayer>();

		if (evenDraw) 
		{
			player.AddClosedHandCard(GetRandomCard());
		}
		else
		{
			player.AddOpenHandCard(GetRandomCard());
		}

		evenDraw = !evenDraw;
	}
	
	//pass local player info to server
	public void PlayCard(int pile) //becuas button.onClick doesn't give enums
	{
		EunoPlayer player = NetworkClient.localPlayer.GetComponent<EunoPlayer>();

		if (isCloseSelected ? (player.closedCount <= 0) : (player.openCount <= 0)) return; // hmmmmmmm negative cards
	
		if (isCloseSelected)
		{
			PlayCardOnServer(player.gameObject, isCloseSelected, player.closedSelectedIndex, pile);
			player.closedSelectedIndex = Mathf.Clamp(player.closedSelectedIndex - 1, 0, player.closedCount - 1);
		}
		else 
		{
			PlayCardOnServer(player.gameObject, isCloseSelected, player.openSelectedIndex, pile);
			player.openSelectedIndex = Mathf.Clamp(player.openSelectedIndex - 1, 0, player.openCount - 1);
		}

		UpdateLocalPlayerDisplay();
	}

	[Command(requiresAuthority = false)]
	public void PlayCardOnServer(GameObject playerGO, bool isFromClosed, int selectedIndex, int pile)
	{
		EunoPlayer player = playerGO.GetComponent<EunoPlayer>();

		if (pile == 0)
		{
			closedPileText.text = isFromClosed ? player.closedHand[selectedIndex].ToString() : player.openHand[selectedIndex].ToString(); 
		}
		else if (pile == 1)
		{
			openPileText.text = isFromClosed ? player.closedHand[selectedIndex].ToString() : player.openHand[selectedIndex].ToString();
		}
		else if (pile == 2)
		{
			togglePileText.text = isFromClosed ? player.closedHand[selectedIndex].ToString() : player.openHand[selectedIndex].ToString();
		}

		if (isFromClosed) player.closedHand.RemoveAt(selectedIndex);
		else player.openHand.RemoveAt(selectedIndex);
	}

	public static EunoCard GetRandomCard() => new EunoCard((CardColour)Random.Range(0,4), (CardType)Random.Range(0, 15));




	/////////////////////////////////////////////////////////// Temporary player stuff //////////////////////////////////////////////////////////////////////////

	public void PlayerScrollClosed(int deltaIndex)
	{
		EunoPlayer player = NetworkClient.localPlayer.GetComponent<EunoPlayer>();
		player.closedSelectedIndex = Mathf.Clamp(player.closedSelectedIndex + deltaIndex, 0, player.closedCount - 1);
		UpdateLocalPlayerDisplay();
		SelectClosed();
	}

	public void PlayerScrollOpen(int deltaIndex)
	{
		EunoPlayer player = NetworkClient.localPlayer.GetComponent<EunoPlayer>();
		player.openSelectedIndex = Mathf.Clamp(player.openSelectedIndex + deltaIndex, 0, player.openCount - 1);
		UpdateLocalPlayerDisplay();
		SelectOpen();
	}

	public void SelectClosed()
	{
		isCloseSelected = true;
		selectTransform.localPosition = new Vector3(selectTransform.localPosition.x, -320, selectTransform.localPosition.z);
	}

	public void SelectOpen()
	{
		isCloseSelected = false;
		selectTransform.localPosition = new Vector3(selectTransform.localPosition.x, -380, selectTransform.localPosition.z);
	}

	public void UpdateLocalPlayerDisplay() //////////////////////////////////////////////////////convert to event called by syncvar hook!!!!!!!!!!!!!!!    also separate everything out
	{
		EunoPlayer player = NetworkClient.localPlayer.GetComponent<EunoPlayer>();

		playerClosedCountText.text = $"{player.closedSelectedIndex + 1} of {player.closedCount}";
		playerOpenCountText.text = $"{player.openSelectedIndex + 1} of {player.openCount}";

		playerClosedDisplayText.text = player.closedCount <= 0 ? "empty" : player.closedHand[player.closedSelectedIndex].ToString();
		playerOpenDisplayText.text = player.openCount <= 0 ? "empty" : player.openHand[player.openSelectedIndex].ToString() ?? "empty";
	}
}
