using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EunoHandCard : MonoBehaviour
{
	public EunoCard cardValue {get; private set;}
	public bool isFromClosed;

	[SerializeField]
	private TMP_Text faceText; //set in inspector
	[SerializeField]
	private GameObject redIndicator; //set in inspector
	[SerializeField]
	private GameObject greenIndicator; //set in inspector
	[SerializeField]
	private GameObject blueIndicator; //set in inspector
	[SerializeField]
	private GameObject yellowIndicator; //set in inspector

	public void SetDisplay(EunoCard card)
	{
		cardValue = card;
		faceText.text = card.ToString();
		redIndicator.SetActive((1 << 0 & 0b1111_1111) != 0);
		greenIndicator.SetActive((1 << 1 & 0b1111_1111) != 0);
		blueIndicator.SetActive((1 << 2 & 0b1111_1111) != 0);
		yellowIndicator.SetActive((1 << 3 & 0b1111_1111) != 0);
	}
}
