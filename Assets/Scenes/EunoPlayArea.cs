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
	Colourless
}

public enum CardType
{
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
	Zero,
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

	public bool Compare(EunoCard card) => (card.colour == this.colour || card.colour == CardColour.Colourless) || (card.type == this.type);
	public bool CompareColour(EunoCard card) => (card.colour == this.colour ||card.colour == CardColour.Colourless);
	public bool CompareType(EunoCard card) => (card.type == this.type);
}

public enum PlayPile
{
	Closed,
	Open,
	Toggle
}

public class EunoPlayArea : NetworkBehaviour
{
	#region properties/fields
	[SyncVar(hook = nameof(UpdateClosedDisplay))] public EunoCard closedPile;
	[SyncVar(hook = nameof(UpdateOpenDisplay))] public EunoCard openPile;
	[SyncVar(hook = nameof(UpdateToggleDisplay))] public EunoCard togglePile;

	public TMP_Text closedPileText;
	public TMP_Text openPileText;
	public TMP_Text togglePileText;

	public RectTransform selectTransform; /////set in inspector
	public TMP_Text playerClosedCountText;  
	public TMP_Text playerClosedDisplayText;
	public TMP_Text playerOpenCountText;
	public TMP_Text playerOpenDisplayText;

	private bool evenDraw = false;

	private bool isCloseSelected = true;
	public int closedSelectedIndex = 0;
	public int openSelectedIndex = 0;	
	#endregion

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
	public void PlayCard(int pile) //becuase button.onClick doesn't take enums
	{
		EunoPlayer player = NetworkClient.localPlayer.GetComponent<EunoPlayer>();

		//if has no cards in that hand, return
		if (isCloseSelected ? (player.closedHand.Count <= 0) : (player.openHand.Count <= 0)) return;

		if (isCloseSelected)
		{
			PlayCardOnServer(player.gameObject, isCloseSelected, closedSelectedIndex, (PlayPile)pile);
		}
		else 
		{
			PlayCardOnServer(player.gameObject, isCloseSelected, openSelectedIndex, (PlayPile)pile);
		}

		UpdateLocalPlayerDisplay();
	}

	[Command(requiresAuthority = false)]
	public void PlayCardOnServer(GameObject playerGO, bool isFromClosed, int selectedIndex, PlayPile pile)
	{
		EunoPlayer player = playerGO.GetComponent<EunoPlayer>();
		
		EunoCard cardToPlay;
		cardToPlay = isFromClosed ? player.closedHand[selectedIndex] : player.openHand[selectedIndex];
		player.CmdSendChat($"play {cardToPlay} from closed? {isCloseSelected} on {pile}");

		switch (pile)
		{
			case PlayPile.Closed:
				if (isFromClosed && closedPile.Compare(cardToPlay)) closedPile = cardToPlay;
				else return; //Return so card isn't removed from hand
				break;

			case PlayPile.Open:
				if (!isFromClosed && openPile.Compare(cardToPlay)) openPile = cardToPlay;
				else return;
				break;

			case PlayPile.Toggle:
				if (togglePile.Compare(cardToPlay)) togglePile = cardToPlay;
				else return;
				break;

			default:
				Debug.LogWarning($"Invalid pile was played on: {pile}");
				return;
		}

		if (isFromClosed)
		{
			PlayerScrollClosed(-1);
			player.closedHand.RemoveAt(selectedIndex);
		}
		else 
		{
			PlayerScrollOpen(-1);
			player.openHand.RemoveAt(selectedIndex);
		}
	}

	[Server]
	public void ApplyCardEffects(PlayPile pile, EunoCard card)
	{
		if (pile == PlayPile.Closed || pile == PlayPile.Open)
		{
			switch (card.type)
			{
				
				default:
					Debug.LogWarning($"Card {card.type} doesn't exist!");
					break;
			}
		}
	}

	public static EunoCard GetRandomCard()
	{
		int asdf = Random.Range(0,27);

		if (asdf == 26) return new EunoCard(CardColour.Colourless, CardType.PlusFour);
		else if (asdf == 25) return new EunoCard(CardColour.Colourless, CardType.Wild);
		else if (asdf == 24) return new EunoCard((CardColour)Random.Range(0, 4), CardType.PlusTwo);
		else if (asdf == 23) return new EunoCard((CardColour)Random.Range(0, 4), CardType.Zero);
		else return new EunoCard((CardColour)Random.Range(0, 4), (CardType)(asdf/2));
	}

	public void UpdateClosedDisplay(EunoCard oldCard, EunoCard newCard) => closedPileText.text = newCard.ToString();
	public void UpdateOpenDisplay(EunoCard oldCard, EunoCard newCard) => openPileText.text = newCard.ToString();
	public void UpdateToggleDisplay(EunoCard oldCard, EunoCard newCard) => togglePileText.text = newCard.ToString();



	/////////////////////////////////////////////////////////// Temporary player stuff //////////////////////////////////////////////////////////////////////////

	public void PlayerScrollClosed(int deltaIndex)
	{
		EunoPlayer player = NetworkClient.localPlayer.GetComponent<EunoPlayer>();
		closedSelectedIndex = Mathf.Clamp(closedSelectedIndex + deltaIndex, 0, player.closedHand.Count - 1);
		UpdateLocalPlayerDisplay();
		SelectClosed();
	}

	public void PlayerScrollOpen(int deltaIndex)
	{
		EunoPlayer player = NetworkClient.localPlayer.GetComponent<EunoPlayer>();
		openSelectedIndex = Mathf.Clamp(openSelectedIndex + deltaIndex, 0, player.openHand.Count - 1);
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

		playerClosedCountText.text = $"{closedSelectedIndex + 1} of {player.closedHand.Count}";
		playerOpenCountText.text = $"{openSelectedIndex + 1} of {player.openHand.Count}";

		playerClosedDisplayText.text = player.closedHand.Count <= 0 ? "empty" : player.closedHand[closedSelectedIndex].ToString();
		playerOpenDisplayText.text = player.openHand.Count <= 0 ? "empty" : player.openHand[openSelectedIndex].ToString();
	}
}
