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
            GridRend.SetLED(5, 6, true);
            GridRend.SetLED(5, 5, true);
            GridRend.SetLED(6, 6, true);
            GridRend.SetLED(6, 5, true);
            GridRend.SetLED(5, 9, true);
            GridRend.SetLED(5, 10, true);
            GridRend.SetLED(6, 9, true);
            GridRend.SetLED(6, 10, true);
            GridRend.SetLED(11, 7, true);
            GridRend.SetLED(11, 6, true);
            GridRend.SetLED(11, 5, true);
            GridRend.SetLED(10, 4, true);
            GridRend.SetLED(11, 8, true);
            GridRend.SetLED(11, 9, true);
            GridRend.SetLED(11, 10, true);
            GridRend.SetLED(10, 11, true);
        };

        DriverStoragePuzzle puzzle = new DriverStoragePuzzle(8, 8);

        //20, 19, 11, 6
        List<int> values = new List<int> { 8, 6, 3, 1 };
        List<int> additions = new List<int> { 0, 1, 2, 3 }.Shuffle();
        for (int i = 0; i < values.Count; i++)
        {
            values[i] += additions[i];
        }
        puzzle = puzzle.FindPuzzle(values);
        Debug.Log(Enumerable.Range(0, 8).Select(y => Enumerable.Range(0, 8).Select(x => puzzle.Grid[y, x] ? "#" : ".").Join("")).Join(";"));
    }
}
