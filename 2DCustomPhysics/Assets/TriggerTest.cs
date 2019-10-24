using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerTest : MonoBehaviour
{
	private void OnCollisionExit2D(Collision2D collision)
	{
		Debug.LogWarning("Exitted : " + collision.transform.name);
	}
}
