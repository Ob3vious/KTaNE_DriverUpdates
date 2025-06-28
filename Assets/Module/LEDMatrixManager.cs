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

    public void RunAnimation(string name)
    {
        for (int i = 0; i < AllDisplays.Length; i++)
            AllDisplays[i].RunAnimation(name);
    }

    public void ClearAnimation()
    {
        for (int i = 0; i < AllDisplays.Length; i++)
            AllDisplays[i].ClearAnimation();
    }

    public void BlankOut()
    {
        for (int i = 0; i < AllDisplays.Length; i++)
            AllDisplays[i].BlankOut();
    }

    public void MakeActive()
    {
        for (int i = 0; i < AllDisplays.Length; i++)
            AllDisplays[i].MakeActive();
    }
}
