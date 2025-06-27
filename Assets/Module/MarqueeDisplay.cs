using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarqueeDisplay : MonoBehaviour
{
    public TextMesh Text;
    public KMAudio Audio;
    public int RowLength;

    private Coroutine CycleAnimCoroutine;
    private string[] AllTexts;

    private string[] FormatText(string text)
    {
        var textArray = text.Split('|');
        if (textArray.Length == 1)
        {
            if (textArray[0].Length <= RowLength) return new string[] { text, "" };
            return new string[] { text.Substring(0, RowLength), "" };
        }
        else
        {
            var output = new string[2];

            if (textArray[0].Length <= RowLength) output[0] = textArray[0];
            else output[0] = textArray[0].Substring(0, RowLength);

            if (textArray[1].Length <= RowLength) output[1] = textArray[1];
            else output[1] = textArray[1].Substring(0, RowLength);

            return output;
        }
    }

    private void Start()
    {
        Text.text = "";
    }

    public void AssignTexts(string[] newTexts)
    {
        if (CycleAnimCoroutine != null)
            StopCoroutine(CycleAnimCoroutine);
        Text.text = "";

        AllTexts = newTexts;

        CycleAnimCoroutine = StartCoroutine(CycleAnim());
    }

    private IEnumerator CycleAnim(float interval = 2.5f, float offInterval = 0.035f)
    {
        var formattedTexts = AllTexts.Select(x => FormatText(x)).ToArray();
        var isFirst = true;
        while (true)
        {
            for (int i = 0; i < formattedTexts.Length; i++)
            {
                if (isFirst || 
                    formattedTexts[i][0] != formattedTexts[(i + formattedTexts.Length - 1) % formattedTexts.Length][0] || formattedTexts[i][1] != formattedTexts[(i + formattedTexts.Length - 1) % formattedTexts.Length][1])
                {
                    var randomSound = 0;
                    if (formattedTexts[i][0].Length > 0 || formattedTexts[i][1].Length > 0)
                        for (int j = 0; j < RowLength; j++)
                        {
                            if (isFirst)
                            {
                                Audio.PlaySoundAtTransform("blip " + randomSound, transform);

                                if (randomSound != 0)
                                    randomSound = 0;
                                else
                                    randomSound = new[] { 0, 0, 1, 2 }.PickRandom();
                            }
                            Text.text = Enumerable.Repeat(" ", RowLength - j - 1).Join("") + formattedTexts[i][0].Substring(0, Mathf.Min(j + 1, formattedTexts[i][0].Length)) + "\n"
                                + Enumerable.Repeat(" ", RowLength - j - 1).Join("") + formattedTexts[i][1].Substring(0, Mathf.Min(j + 1, formattedTexts[i][1].Length));
                            yield return new WaitForSeconds(offInterval);
                        }
                }

                Text.text = formattedTexts[i][0] + "\n" + formattedTexts[i][1];

                yield return new WaitForSeconds(interval);

                if (formattedTexts[i][0] != formattedTexts[(i + 1) % formattedTexts.Length][0] || formattedTexts[i][1] != formattedTexts[(i + 1) % formattedTexts.Length][1])
                {
                    if (formattedTexts[i][0].Length > 0 || formattedTexts[i][1].Length > 0)
                        for (int j = 0; j < Mathf.Min(Mathf.Max(formattedTexts[i][0].Length, formattedTexts[i][1].Length), RowLength); j++)
                        {
                            Text.text = (j >= formattedTexts[i][0].Length ? "" : formattedTexts[i][0].Substring(j + 1, formattedTexts[i][0].Length - j - 1)) + "\n"
                                + (j >= formattedTexts[i][1].Length ? "" : formattedTexts[i][1].Substring(j + 1, formattedTexts[i][1].Length - j - 1));
                            yield return new WaitForSeconds(offInterval);
                        }
                }

                isFirst = false;
            }
            yield return null;
        }
    }
}
