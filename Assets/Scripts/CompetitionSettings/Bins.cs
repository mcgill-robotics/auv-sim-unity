using UnityEngine;
using System.Collections;

public class Bins : MonoBehaviour
{
	private bool isFirst;

	void Start()
	{
		isFirst = true;
		this.enabled = false;
	}

	public void OnCollisionEnter(Collision col)
	{
		if (col.gameObject.name == "Dropping Sphere" && isFirst)
		{
			isFirst = false;
			if (PointsManager.instance.color == "none" || PointsManager.instance.color == gameObject.name)
			{
				PointsManager.instance.AddPoint(BinsManager.instance.pointsAvailableCorrect, "Bins");
				MessageBox.instance.AddMessage(string.Format("Bins correct side +{0}pts", BinsManager.instance.pointsAvailableCorrect));
			}
			else
			{
				PointsManager.instance.AddPoint(BinsManager.instance.pointsAvailableWrong, "Bins");
				MessageBox.instance.AddMessage(string.Format("Bins any side +{0}pts", BinsManager.instance.pointsAvailableWrong));
			}
			BinsManager.instance.StopAllBinsScripts();
		}
	}

	public void StartScript()
	{
		this.enabled = true;
	}

	public void StopScript()
	{
		this.enabled = false;
	}
}