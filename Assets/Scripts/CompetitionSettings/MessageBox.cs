using UnityEngine;
using System.Collections;
using TMPro;

public class MessageBox : MonoBehaviour
{
	public static MessageBox instance;
	public TMP_Text messageText;

	void Awake()
	{
		instance = this;
		ResetMessage();
	}

	public void AddMessage(string newMessage)
	{
		messageText.text += newMessage + "\n";
		AdjustHeightToFitContent();
	}

	void AdjustHeightToFitContent()
	{
		RectTransform rectTransform = messageText.GetComponent<RectTransform>();

		float preferredHeight = messageText.preferredHeight;
		rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, preferredHeight);
	}

	public void ResetMessage()
	{
		messageText.text = "";
	}
}
