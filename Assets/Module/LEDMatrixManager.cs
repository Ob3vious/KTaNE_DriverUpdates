using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LEDMatrixManager : MonoBehaviour
{
    public LEDMatrixSmall[] AllDisplays;

    public void SetLED(int row, int col, bool state)
    {
        AllDisplays[(row > 7 ? 2 : 0) + (col > 7 ? 1 : 0)].SetLED(row % 8, col % 8, state);
    }
}
