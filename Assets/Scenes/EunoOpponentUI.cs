using UnityEngine;
using TMPro;

public class EunoOpponentUI : MonoBehaviour
{
	public TMP_Text nameText;
	public TMP_Text closedText;
	public TMP_Text openText;
	public EunoPlayer player;

	public void Start()
	{
		nameText = transform.Find("Name").GetComponent<TMP_Text>();
		closedText = transform.Find("Closed").GetComponent<TMP_Text>();
		openText = transform.Find("Open").GetComponent<TMP_Text>();
	}

	public void SetPlayer(EunoPlayer asdf)
	{
		player = asdf;
		nameText.text = player.playerName;
	}

	public void SetClosedCount(int asdf)
	{
		closedText.text = $"Closed: {asdf}";
	}

	public void SetOpenCount(int asdf)
	{
		openText.text = $"Open: {asdf}";
	}
}