using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LEDMatrixSmall : MonoBehaviour
{
    public MeshRenderer LEDTemplate;
    public float MaxPos;
    public Material OffMat;
    public Material OnMat;
    public Color Colour;

    private List<List<MeshRenderer>> LEDRends = new List<List<MeshRenderer>>();

    private void Start()
    {
        Setup();
    }

    private void Setup()
    {
        for (int i = 0; i < 8; i++)
        {
            LEDRends.Add(new List<MeshRenderer>());
            for (int j = 0; j < 8; j++)
            {
                var ledTemp = Instantiate(LEDTemplate, LEDTemplate.transform.parent);
                ledTemp.transform.localPosition = new Vector3(Mathf.Lerp(-MaxPos, MaxPos, j / 7f), ledTemp.transform.localPosition.y, Mathf.Lerp(MaxPos, -MaxPos, i / 7f));
                LEDRends[i].Add(ledTemp);
            }
        }

        LEDTemplate.gameObject.SetActive(false);
    }

    public void SetLED(int row, int col, bool state)
    {
        LEDRends[row][col].material = state ? OnMat : OffMat;
        if (state) LEDRends[row][col].material.color = Colour;
    }
}
