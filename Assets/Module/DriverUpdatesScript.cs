using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Newtonsoft.Json.Linq;

public class DriverUpdatesScript : MonoBehaviour
{
    public KMAudio Audio;
    public MarqueeDisplay Marquee;
    public LEDMatrixManager GridRend;
    public KMSelectable[] SideButtons;

    private DriverStoragePuzzle _puzzle = null;
    private Coroutine[] SideButtonAnimCoroutines;
    private float SideButtonInitPosition;

    void Start()
    {
        Marquee.AssignTexts(new string[] { "Booting Up..." });
        GetComponent<KMBombModule>().OnActivate += delegate
        {
            GridRend.SetLED(2, 2, true);
            GridRend.SetLED(2, 5, true);
            GridRend.SetLED(4, 2, true);
            GridRend.SetLED(4, 5, true);
            GridRend.SetLED(5, 3, true);
            GridRend.SetLED(5, 4, true);
            StartCoroutine(FetchPuzzle());
        };

        SideButtonAnimCoroutines = new Coroutine[SideButtons.Length];
        SideButtonInitPosition = SideButtons[0].transform.localPosition.y;
        for (int i = 0; i < SideButtons.Length; i++)
        {
            int x = i;
            SideButtons[x].OnInteract += delegate { SideButtonPress(x); return false; };
            SideButtons[x].OnInteractEnded += delegate { SideButtonRelease(x); };
        }
    }



    private Thread _thread = null;
    private static bool _isUsingThreads = false;
    private IEnumerator FetchPuzzle()
    {


        //Wait an extra frame so TwitchPlaysActive can be set in TestHarness
        //yield return null;
        List<DriverStoragePuzzle.Shape> pieces = null;
        do
        {
            yield return new WaitForSecondsRealtime(Rnd.Range(1f, 4f));

            DriverStoragePuzzle puzzle = new DriverStoragePuzzle(8, 8);

            List<int> values = new List<int> { 10, 6, 3, 1 };
            List<int> additions = new List<int> { 0, 1, 2, 3 }.Shuffle();
            for (int i = 0; i < values.Count; i++)
            {
                values[i] += additions[i];
            }

            //threads needed here
            Marquee.AssignTexts(new string[] { "Connecting..." });
            yield return new WaitWhile(() => _isUsingThreads);
            _isUsingThreads = true;
            bool generated = false;

            List<DriverStoragePuzzle> coverage = puzzle.GenerateCoverage();
            _thread = new Thread(() =>
            {
                _puzzle = puzzle.FindPuzzle(values, coverage, out pieces);
                generated = true;
            });
            _thread.Start();

            Marquee.AssignTexts(new string[] { "Fetching Updates..." });
            yield return new WaitWhile(() => !generated);
            _isUsingThreads = false;
            _thread = null;

            if (pieces.Sum(x => x.CellCount - x.Value - 1) <= 4)
            {
                Debug.Log("fail:" + pieces.Join(","));
                Marquee.AssignTexts(new string[] { "Connection Lost" });
            }
        }
        //bends add 1pt each to complexity
        while (pieces.Sum(x => x.CellCount - x.Value - 1) < 4);

        Marquee.AssignTexts(new string[] { "Updates Gathered|Press Confirm" });

        Debug.Log(Enumerable.Range(0, 8).Select(y => Enumerable.Range(0, 8).Select(x => _puzzle.Grid[y, x] ? "#" : ".").Join("")).Join(";"));
        Debug.Log(pieces.Join(","));

        //thread stuff is done here
    }

    void OnDestroy()
    {
        if (_thread != null)
        {
            _thread.Interrupt();
            _isUsingThreads = false;
        }
    }

    private void SideButtonPress(int pos)
    {
        Audio.PlaySoundAtTransform("press", SideButtons[pos].transform);
        if (SideButtonAnimCoroutines[pos] != null)
            StopCoroutine(SideButtonAnimCoroutines[pos]);
        SideButtonAnimCoroutines[pos] = StartCoroutine(SideButtonAnim(pos, false));
    }

    private void SideButtonRelease(int pos)
    {
        Audio.PlaySoundAtTransform("release", SideButtons[pos].transform);
        if (SideButtonAnimCoroutines[pos] != null)
            StopCoroutine(SideButtonAnimCoroutines[pos]);
        SideButtonAnimCoroutines[pos] = StartCoroutine(SideButtonAnim(pos, true));
    }

    private IEnumerator SideButtonAnim(int pos, bool isUp, float duration = 0.05f, float depression = 0.002f)
    {
        SideButtons[pos].transform.localPosition = new Vector3(SideButtons[pos].transform.localPosition.x, isUp ? SideButtonInitPosition - depression : SideButtonInitPosition, SideButtons[pos].transform.localPosition.z);

        float timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            SideButtons[pos].transform.localPosition = new Vector3(SideButtons[pos].transform.localPosition.x, Mathf.Lerp(isUp ? SideButtonInitPosition - depression : SideButtonInitPosition,
                isUp ? SideButtonInitPosition : SideButtonInitPosition - depression, timer / duration), SideButtons[pos].transform.localPosition.z);
        }

        SideButtons[pos].transform.localPosition = new Vector3(SideButtons[pos].transform.localPosition.x, isUp ? SideButtonInitPosition : SideButtonInitPosition - depression, SideButtons[pos].transform.localPosition.z);
    }
}