using UnityEngine;
using TMPro;

public class EunoOpponentUI : MonoBehaviour
{
	public EunoPlayer player;
	public TMP_Text nameText;
	public TMP_Text closedText;
	public TMP_Text openText;

	public void Awake()
	{
		nameText = transform.GetChild(0).Find("Name").GetComponent<TMP_Text>();
		closedText = transform.GetChild(0).Find("Closed").GetComponent<TMP_Text>();
		openText = transform.GetChild(0).Find("Open").GetComponent<TMP_Text>();
	}


	public void SetPlayer(EunoPlayer player)
	{
		this.player = player;
		nameText.text = this.player.playerName;
	}

	public void UpdateDisplay() //called by synclist hook
	{
		closedText.text = $"Closed: {player.closedHand.Count}";
		openText.text = $"Open: {player.openHand.Count}";
	}

	//for selecting player on 3s, 7s, etc
	public void OnClickOpponent()
	{
		player.playArea.selectedPlayer = player;
	}
}

