using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System.Linq;
using System.Collections;

[System.Flags]
public enum CardColour
{
	Red = 0b0000_0001,
	Green = 0b000_00010,
	Blue = 0b0000_0100,
	Yellow = 0b000_01000,
	Rainbow = 0b0000_1111,
	Uncoloured = 0
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

	public bool CanBeJumpedWith(EunoCard card) => CompareColour(card) && CompareType(card);
	public bool CanBePlayedWith(EunoCard card) => CompareColour(card) || CompareType(card);
	public bool CompareColour(EunoCard card) => (card.colour & this.colour) != 0;
	public bool CompareType(EunoCard card) => card.type == this.type;
}

public enum PlayPile
{
	Closed,
	Open,
	Toggle,
}

public class EunoPlayArea : NetworkBehaviour
{
	#region properties/fields
	[SyncVar(hook = nameof(UpdateClosedPileDisplay))] public EunoCard closedPile;
	[SyncVar(hook = nameof(UpdateOpenPileDisplay))] public EunoCard openPile;
	[SyncVar(hook = nameof(UpdateTogglePileDisplay))] public EunoCard togglePile;
	[SyncVar] public int turnIndex;

	public TMP_Text closedPileText;
	public TMP_Text openPileText;
	public TMP_Text togglePileText;

	public RectTransform selectTransform; /////set in inspector
	public TMP_Text playerClosedCountText;  
	public TMP_Text playerClosedDisplayText;
	public TMP_Text playerOpenCountText;
	public TMP_Text playerOpenDisplayText;

	private bool drawClosed = false;
	private bool isReversed = false;

	private bool isFromClosed = true;
	public int closedSelectedIndex = 0;
	public int openSelectedIndex = 0;

	public bool isSelecting;
	public PlayPile selectedPile;
	public EunoPlayer selectedPlayer;

	//for 4s
	public EunoCard lastClosedCardEffect;
	public EunoCard lastOpenCardEffect;

	public bool keepWaiting = true;
	#endregion

	public void OnGUI()
	{
		GUILayout.BeginArea(new Rect(20, 200, 100, 500));
		GUILayout.Label($"<color=red>Turn: {turnIndex}</color>");
		if (GUILayout.Button("turn +1")) ChangeTurn(1);
		if (GUILayout.Button("turn -1")) ChangeTurn(-1);
		if (GUILayout.Button("turn +3")) ChangeTurn(3);
		if (GUILayout.Button("turn -3")) ChangeTurn(-3);
		GUILayout.EndArea();
	}

	[Server]
	public override void OnStartServer()
	{
		closedPile = GetRandomCard();
		openPile = GetRandomCard();
		togglePile = GetRandomCard();

		///////////////////////////////////////////////////////////////////////////why are these here? syncvar hooks don't work??
		closedPileText.text = closedPile.ToString();
		openPileText.text = openPile.ToString();
		togglePileText.text = togglePile.ToString();
	}

	public void Start()
	{
		transform.SetParent(GameObject.Find("Canvas").transform, false);
		gameObject.name = "Play Area"; //set name so EunoPlayer can use GameObject.Find()  -- EunoPlayer is created after this     !!!!!use events to get rid of this!!!!!
	}

	[Command(requiresAuthority = false)]
	public void ChangeTurn(int deltaPlaces)
	{
		int direction = (isReversed ? -1 : 1);
		int numPlayers = NetworkServer.connections.Count;
		int sign = (int)Mathf.Sign(deltaPlaces) * direction;

		turnIndex = ((turnIndex + (deltaPlaces * direction)) % numPlayers + numPlayers) % numPlayers;
	}

	[Command(requiresAuthority = false)]
	public void SetTurn(NetworkConnectionToClient sender = null)
	{
		for (int i = 0; i < NetworkServer.connections.Count; i++) //something something, dictionaries being fundamentally unordered ¯\_(ツ)_/¯
		{
			if (NetworkServer.connections.ElementAt(i).Key == sender.connectionId)
			{
				turnIndex = i;
				break;
			}
		}
	}

	[Server]
	public bool IsTurnOf(NetworkConnectionToClient asd) //idk how connectionId works on different devices, so dont do that
	{
		return asd.connectionId == NetworkServer.connections.ElementAt(turnIndex).Value.connectionId;
	}

	public void OnDrawButtonPressed()
	{
		DrawCard();
	}

	//called by UI button
	[Command(requiresAuthority = false)]
	public void DrawCard(NetworkConnectionToClient sender = null)
	{
		EunoPlayer player = sender.identity.GetComponent<EunoPlayer>();

		if (drawClosed)	player.closedHand.Add(GetRandomCard());
		else player.openHand.Add(GetRandomCard());

		drawClosed = !drawClosed;
	}
	
	//ui deck is clicked
	public void OnClickDeck(int pile) //because button.onClick doesn't take enums
	{
		EunoPlayer player = NetworkClient.localPlayer.GetComponent<EunoPlayer>();
		PlayCardOnServer(player, isFromClosed, isFromClosed ? closedSelectedIndex : openSelectedIndex, (PlayPile)pile);
	}

	[Command(requiresAuthority = false)]
	public void PlayCardOnServer(EunoPlayer player, bool isFromClosed, int selectedIndex, PlayPile pile, NetworkConnectionToClient sender = null) //idk how passing a NetworkBehaviour through command works, but apparently it does
	{
		//check for invalid input
			 //index out of bounds or hand count is 0
		if (selectedIndex < 0|| selectedIndex + 1 > (isFromClosed ? player.closedHand.Count : player.openHand.Count)|| (isFromClosed ? player.closedHand.Count : player.openHand.Count) == 0) return;

		EunoCard cardToPlay = isFromClosed ? player.closedHand[selectedIndex] : player.openHand[selectedIndex];

		switch (pile)
		{
			case PlayPile.Closed:
				if (!isFromClosed) break; //card is not from correct hand, cant play
				else if (closedPile.CanBeJumpedWith(cardToPlay)) //if can jump in, set turn, play card
				{
					SetTurn(sender);
					StartCoroutine(ApplyCardEffects(player, pile, cardToPlay, sender));
					RemoveCardFromHand();
				}
				else if (closedPile.CanBePlayedWith(cardToPlay) && IsTurnOf(sender)) //if can play and is turn, play
				{
					StartCoroutine(ApplyCardEffects(player, pile, cardToPlay, sender));
					RemoveCardFromHand();
				}
				break;

			case PlayPile.Open:
				if (isFromClosed) break;
				else if (openPile.CanBeJumpedWith(cardToPlay))
				{
					SetTurn(sender);
					StartCoroutine(ApplyCardEffects(player, pile, cardToPlay, sender));
					RemoveCardFromHand();
				}
				else if (openPile.CanBePlayedWith(cardToPlay) && IsTurnOf(sender))
				{
					StartCoroutine(ApplyCardEffects(player, pile, cardToPlay, sender));
					RemoveCardFromHand();
				}
				return;

			case PlayPile.Toggle:
				if (cardToPlay.type == CardType.PlusFour) break;
				else if (togglePile.CanBePlayedWith(cardToPlay) && IsTurnOf(sender))
				{
					StartCoroutine(ApplyCardEffects(player, pile, cardToPlay, sender));
					RemoveCardFromHand();
				}
				break;

			default:
				Debug.LogError($"Invalid pile was played on: {pile}");
				return;
		}

		void RemoveCardFromHand()
		{
			if (isFromClosed) player.closedHand.RemoveAt(selectedIndex); //checks for invalid index on synclist callback
			else player.openHand.RemoveAt(selectedIndex);

			Debug.Log($"played {cardToPlay} on {pile} pile from {(isFromClosed ? "closed" : "open")}\nindex {selectedIndex} of count {(isFromClosed ? player.closedHand.Count : player.openHand.Count) + 1} of player {player.playerName}");
		}
	}

	[Server]
	public IEnumerator ApplyCardEffects(EunoPlayer player, PlayPile pilePlayed, EunoCard card, NetworkConnectionToClient sender) //Pass NetworkConnectionToClient??? It's gonna be cast to NetworkConnection, its base class, so just use EunoPlayer.connectionToClient? 
	{
		if (pilePlayed == PlayPile.Closed || pilePlayed == PlayPile.Open)
		{
			#region
			switch (card.type)
			{	
				case CardType.One:
					ChangeDeckValue();
					ChangeTurn(-1);
					break;

				case CardType.Two: //select deck
					yield return new WaitWhile(() => keepWaiting == true);
					keepWaiting = true;
					
					ChangeDeckValue();
					ChangeTurn(1);
					break;

				case CardType.Three: //select player
					ChangeDeckValue();
					ChangeTurn(1);
					break;

				case CardType.Four: //TEST
					StartCoroutine(ApplyCardEffects(player, pilePlayed, pilePlayed == PlayPile.Closed ? lastClosedCardEffect : lastOpenCardEffect, sender));
					if (pilePlayed == PlayPile.Closed) closedPile = new EunoCard(card.colour, closedPile.type);
					else openPile = new EunoCard(card.colour, openPile.type);
					break;

				case CardType.Five: //oh no
					ChangeTurn(1);
					break;

				case CardType.Six:
					ChangeDeckValue();
					ChangeTurn(1);
					break;

				case CardType.Seven: //select player
					ChangeDeckValue();
					ChangeTurn(1);
					break;

				case CardType.Eight:
					ChangeDeckValue();
					ChangeTurn(1);
					break;

				case CardType.Nine:
					ChangeDeckValue();
					ChangeTurn(1);
					break;

				case CardType.Skip: //TEST
					ChangeDeckValue();
					ChangeTurn(2);
					break;

				case CardType.Reverse:
					isReversed = !isReversed;
					ChangeDeckValue();
					ChangeTurn(1);
					break;

				case CardType.Zero: //oh no
					ChangeDeckValue();
					ChangeTurn(1);
					break;

				case CardType.Wild: //select colour
					ChangeDeckValue();
					ChangeTurn(1);
					break;

				case CardType.PlusTwo: //+2 buffer
					ChangeDeckValue();
					ChangeTurn(1);
					break;

				case CardType.PlusFour: //select colour, +4 buffer
					ChangeDeckValue();
					ChangeTurn(1);
					break;
				
				default:
					Debug.LogError($"Invalid card type!!! {card.type} doesn't exist!");
					break;
			}
			#endregion
		}
		else if (pilePlayed == PlayPile.Toggle)
		{
			togglePile = card;
			ChangeTurn(1);
		}
	
		yield return null;

		void ChangeDeckValue()
		{
			if (pilePlayed == PlayPile.Closed)
			{
				closedPile = card;
				lastClosedCardEffect = card;
			}
			else
			{
				openPile = card;
				lastOpenCardEffect = card;
			}
		}
	}

	[TargetRpc]
	public void TargetSelectPileInstruction(NetworkConnection target)
	{

	}

	[Command]
	public void SelectIle(PlayPile pile, NetworkConnectionToClient sender = null)
	{

	}

	public static EunoCard GetRandomCard()
	{
		int asdf = Random.Range(0,27);

		if (asdf == 26) return new EunoCard(CardColour.Rainbow, CardType.PlusFour);
		else if (asdf == 25) return new EunoCard(CardColour.Rainbow, CardType.Wild);
		else if (asdf == 24) return new EunoCard(RandomColour(), CardType.PlusTwo);
		else if (asdf == 23) return new EunoCard(RandomColour(), CardType.Zero);
		else return new EunoCard(RandomColour(), (CardType)(asdf/2));

		CardColour RandomColour() => (CardColour)1;// (CardColour)(1 << Random.Range(0, 4));
	}

	//syncvar hooks
	public void UpdateClosedPileDisplay(EunoCard oldCard, EunoCard newCard) => closedPileText.text = newCard.ToString();
	public void UpdateOpenPileDisplay(EunoCard oldCard, EunoCard newCard) => openPileText.text = newCard.ToString();
	public void UpdateTogglePileDisplay(EunoCard oldCard, EunoCard newCard) => togglePileText.text = newCard.ToString();

	/////////////////////////////////////////////////////////// Temporary local player display stuff //////////////////////////////////////////////////////////////////////////
	public void PlayerHandScrollClosed(int deltaIndex)
	{
		EunoPlayer player = NetworkClient.localPlayer.GetComponent<EunoPlayer>();
		closedSelectedIndex = Mathf.Clamp(closedSelectedIndex + deltaIndex, 0, player.closedHand.Count - 1);
		UpdateLocalPlayerDisplay();
		SelectClosed();
	}

	public void PlayerHandScrollOpen(int deltaIndex)
	{
		EunoPlayer player = NetworkClient.localPlayer.GetComponent<EunoPlayer>();
		openSelectedIndex = Mathf.Clamp(openSelectedIndex + deltaIndex, 0, player.openHand.Count - 1);
		UpdateLocalPlayerDisplay();
		SelectOpen();
	}

	public void SelectClosed()
	{
		isFromClosed = true;
		selectTransform.localPosition = new Vector3(selectTransform.localPosition.x, -320, selectTransform.localPosition.z);
	}

	public void SelectOpen()
	{
		isFromClosed = false;
		selectTransform.localPosition = new Vector3(selectTransform.localPosition.x, -380, selectTransform.localPosition.z);
	}

	public void UpdateLocalPlayerDisplay() /////////////////convert to event called by syncvar hook!!!!!!!!!!!!!!!    also separate everything out.... but then again this is supposed to be temporary
	{
		EunoPlayer player = NetworkClient.localPlayer.GetComponent<EunoPlayer>();

		closedSelectedIndex = Mathf.Clamp(closedSelectedIndex, 0, player.closedHand.Count - 1);
		openSelectedIndex = Mathf.Clamp(openSelectedIndex, 0, player.openHand.Count - 1);

		playerClosedCountText.text = $"{closedSelectedIndex + 1} of {player.closedHand.Count}";
		playerOpenCountText.text = $"{openSelectedIndex + 1} of {player.openHand.Count}";

		playerClosedDisplayText.text = player.closedHand.Count <= 0 ? "empty" : player.closedHand[closedSelectedIndex].ToString();
		playerOpenDisplayText.text = player.openHand.Count <= 0 ? "empty" : player.openHand[openSelectedIndex].ToString();
	}
}

public class WaitForSelection : CustomYieldInstruction
{	
	public override bool keepWaiting {get{return _keepWaiting;}}
	public bool _keepWaiting = true;

	public WaitForSelection()
	{
		Debug.Log("WAIT FOR SELECTION");
	}
}