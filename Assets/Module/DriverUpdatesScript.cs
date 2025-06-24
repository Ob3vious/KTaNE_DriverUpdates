using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using System.Linq;

public class DriverUpdatesScript : MonoBehaviour
{
    public MarqueeDisplay Marquee;
    public LEDMatrixManager GridRend;

    void Start()
    {
        GetComponent<KMBombModule>().OnActivate += delegate
        {
            Marquee.AssignTexts(new string[] { "THIS IS EXAMPLEEEEEEE|TEXTYY WEXTYyyyyyy", "h", "ababababababababababababababababababababa", "", "wheeee|bobm" });
            GridRend.SetLED(2, 2, true);
            GridRend.SetLED(2, 5, true);
            GridRend.SetLED(4, 2, true);
            GridRend.SetLED(4, 5, true);
            GridRend.SetLED(5, 3, true);
            GridRend.SetLED(5, 4, true);
        };

        DriverStoragePuzzle puzzle = new DriverStoragePuzzle(8, 8);

        //20, 19, 11, 6

        //puzzle = puzzle.FindPuzzle(new List<int>() { 12, 7, 6, 4 });
        //Debug.Log(Enumerable.Range(0, 8).Select(y => Enumerable.Range(0, 8).Select(x => puzzle.Grid[y, x] ? "#" : ".").Join("")).Join(";"));
    }
}
