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

    private Coroutine[] SideButtonAnimCoroutines;
    private float SideButtonInitPosition;
    public bool IsActive { get; private set; }

    private UpdaterStateMachine _stateMachine;

    public DriverStoragePuzzle Puzzle = null;
    public List<DriverStoragePuzzle.Shape> PotentialSolution = null;
    public List<UpdateLog> UpdateLogList = null;
    public List<int> ComponentSizes = null;
    public List<bool> ComponentsSelected = null;
    public List<bool> ComponentsSelectionImmutable = null;
    public List<string> ComponentNames = null;

    private static int _moduleIdCounter = 1;
    private int _moduleID = 0;

    void Start()
    {
        _moduleID = _moduleIdCounter++;
        IsActive = false;

        _stateMachine = new UpdaterStateMachine(this);
        _stateMachine.SwitchState<StFetch>();

        
        GetComponent<KMBombModule>().OnActivate += delegate
        {
            //StartCoroutine(FetchPuzzle());
            GridRend.MakeActive();

            IsActive = true;
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





    void OnDestroy()
    {
        new UpdaterStateMachine(this).CurrentState.OnEnd();
    }

    public void Log(string text, params object[] args)
    {
        Debug.LogFormat("[Driver Updates #{0}] {1}", _moduleID, string.Format(text, args));
    }

    private void SideButtonPress(int pos)
    {
        Audio.PlaySoundAtTransform("press", SideButtons[pos].transform);
        if (SideButtonAnimCoroutines[pos] != null)
            StopCoroutine(SideButtonAnimCoroutines[pos]);
        SideButtonAnimCoroutines[pos] = StartCoroutine(SideButtonAnim(pos, false));

        _stateMachine.CurrentState.OnPress(pos);
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