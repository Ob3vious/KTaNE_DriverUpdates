using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class DriverUpdatesScript : MonoBehaviour
{
	public MarqueeDisplay Marquee;

	void Start () {
		GetComponent<KMBombModule>().OnActivate += delegate { Marquee.AssignTexts(new string[] { "THIS IS EXAMPLEEEEEEE|TEXTYY WEXTYyyyyyy", "h", "ababababababababababababababababababababa", "", "wheeee|bobm" }); };
	}
}
