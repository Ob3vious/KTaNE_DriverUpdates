using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public class UpdaterStateMachine
{
    public class State
    {
        public UpdaterStateMachine StateMachine { get; private set; }

        public State(UpdaterStateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }

        public virtual void OnStart()
        {

        }
        public virtual void OnEnd()
        {

        }
        public virtual void OnPress(int button)
        {

        }
    }

    private List<State> _states;
    public State CurrentState { get; private set; }
    public DriverUpdatesScript Module { get; private set; }

    public UpdaterStateMachine(DriverUpdatesScript module)
    {
        CurrentState = null;
        Module = module;
        _states = new List<State> { new StFetch(this), new StList(this), new StPick(this), /*StAllocate here*/ new StInstall(this) };
    }

    public void SwitchState<T>() where T : State
    {
        State newState = _states.FirstOrDefault(x => x is T);
        if (newState == null)
            throw new Exception("Missing a state: " + typeof(T));

        if (CurrentState != null)
            CurrentState.OnEnd();
        CurrentState = newState;
        newState.OnStart();
    }

}

public class StFetch : UpdaterStateMachine.State
{
    private Thread _thread = null;
    private static bool _isUsingThreads = false;
    private Coroutine _fetchingRoutine = null;
    private bool _ready = false;

    public StFetch(UpdaterStateMachine stateMachine) : base(stateMachine)
    {
    }

    private IEnumerator FetchPuzzle()
    {
        yield return new WaitWhile(() => !StateMachine.Module.IsActive);
        yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0f, 3f));
        StateMachine.Module.PotentialSolution = null;
        do
        {
            yield return new WaitForSecondsRealtime(1f);
            DriverStoragePuzzle puzzle = new DriverStoragePuzzle(8, 8);

            StateMachine.Module.ComponentSizes = new List<int> { 10, 6, 3, 1 };
            List<int> additions = new List<int> { 0, 1, 2, 3 }.Shuffle();
            for (int i = 0; i < StateMachine.Module.ComponentSizes.Count; i++)
                StateMachine.Module.ComponentSizes[i] += additions[i];

            //threads needed here
            StateMachine.Module.Marquee.AssignTexts(new string[] { "Connecting..." });
            StateMachine.Module.GridRend.RunAnimation("wifi");
            yield return new WaitWhile(() => _isUsingThreads);
            _isUsingThreads = true;
            bool generated = false;

            List<DriverStoragePuzzle> coverage = puzzle.GenerateCoverage();
            _thread = new Thread(() =>
            {
                StateMachine.Module.Puzzle = puzzle.FindPuzzle(StateMachine.Module.ComponentSizes, coverage, out StateMachine.Module.PotentialSolution);
                generated = true;
            });
            _thread.Start();

            StateMachine.Module.Marquee.AssignTexts(new string[] { "Fetching Updates..." });
            StateMachine.Module.GridRend.RunAnimation("download");
            yield return new WaitWhile(() => !generated);
            _isUsingThreads = false;
            _thread = null;

            if (StateMachine.Module.PotentialSolution.Sum(x => x.CellCount - x.Value - 1) <= 4)
            {
                Debug.Log("fail:" + StateMachine.Module.PotentialSolution.Join(","));
                StateMachine.Module.Marquee.AssignTexts(new string[] { "Connection Lost" });
                StateMachine.Module.GridRend.RunAnimation("x");
            }
        }
        //bends add 1pt each to complexity
        while (StateMachine.Module.PotentialSolution.Sum(x => x.CellCount - x.Value - 1) < 4);

        StateMachine.Module.ComponentsSelected = new List<bool> { true, true, false, false };
        StateMachine.Module.ComponentsSelectionImmutable = new List<bool> { true, true, false, false };

        StateMachine.Module.Marquee.AssignTexts(new string[] { "Updates Gathered|Press Confirm" });
        StateMachine.Module.GridRend.RunAnimation("tick");

        Debug.Log(Enumerable.Range(0, 8).Select(y => Enumerable.Range(0, 8).Select(x => StateMachine.Module.Puzzle.Grid[y, x] ? "#" : ".").Join("")).Join(";"));
        Debug.Log(StateMachine.Module.PotentialSolution.Join(","));

        //thread stuff is done here

        _ready = true;
    }

    private void GenerateUpdateLogs()
    {
        StateMachine.Module.UpdateLogList = UpdateLog.GenerateLogs();
        Debug.Log(StateMachine.Module.UpdateLogList.Join("; "));
        Debug.Log(StateMachine.Module.UpdateLogList.Select(x => x.EvaluateScore()).Join(", "));
        Debug.Log(UpdateLog.EvaluateTotalScore(StateMachine.Module.UpdateLogList));
    }

    public override void OnStart()
    {
        StateMachine.Module.Marquee.AssignTexts(new string[] { "Booting Up..." });
        StateMachine.Module.GridRend.RunAnimation("throbber");
        GenerateUpdateLogs();
        _fetchingRoutine = StateMachine.Module.StartCoroutine(FetchPuzzle());
        base.OnStart();
    }

    public override void OnEnd()
    {
        if (_fetchingRoutine != null)
        {
            StateMachine.Module.StopCoroutine(_fetchingRoutine);
            _fetchingRoutine = null;
        }
        if (_thread != null)
        {
            _thread.Interrupt();
            _isUsingThreads = false;
        }
        base.OnEnd();
    }

    public override void OnPress(int button)
    {
        if (_ready && button == 3)
            StateMachine.SwitchState<StList>();
        base.OnPress(button);
    }
}

public class StList : UpdaterStateMachine.State
{
    private int _index = 0;

    public StList(UpdaterStateMachine stateMachine) : base(stateMachine)
    {
    }

    private void ShowCurrentUpdate()
    {
        //sorry
        UpdateLog log = StateMachine.Module.UpdateLogList[_index];
        StateMachine.Module.Marquee.AssignTexts(new string[] { log.VersionName + " (Update " + (_index + 1) + "/" + StateMachine.Module.UpdateLogList.Count + ")|" + log.Notes.Count + " Notes" }.Concat(
            Enumerable.Range(0, log.Notes.Count).Select(x => log.Notes[x].Description + "|" + (x + 1) + "/" + log.Notes.Count)
            ).ToArray());
    }

    public override void OnStart()
    {
        _index = 0;
        ShowCurrentUpdate();
        base.OnStart();
    }

    public override void OnPress(int button)
    {
        switch (button)
        {
            case 0:
                if (_index > 0)
                    _index--;
                ShowCurrentUpdate();
                break;
            case 1:
                if (_index < StateMachine.Module.UpdateLogList.Count - 1)
                    _index++;
                ShowCurrentUpdate();
                break;
            case 3:
                StateMachine.SwitchState<StPick>();
                break;
            default:
                break;
        }
        base.OnPress(button);
    }
}

public class StPick : UpdaterStateMachine.State
{
    private int _index = 0;

    public StPick(UpdaterStateMachine stateMachine) : base(stateMachine)
    {
    }

    private void ShowSelectedComponents()
    {
        if (_index >= StateMachine.Module.ComponentsSelected.Count)
        {
            StateMachine.Module.Marquee.AssignTexts(new string[] { "Selected " + StateMachine.Module.ComponentsSelected.Count(x => x) + "/" + StateMachine.Module.ComponentsSelected.Count + "|Press Confirm" });
            return;
        }

        StateMachine.Module.Marquee.AssignTexts(new string[] { 
            new string[] { "Program", "Libraries", "Installer Patch", "Fast Loading" }[_index] + " (" + StateMachine.Module.ComponentSizes[_index] + "kB)|" 
            + (StateMachine.Module.ComponentsSelected[_index] ? "Selected" : "Not Selected") +  (StateMachine.Module.ComponentsSelectionImmutable[_index] ? "*" : "") });
    }

    public override void OnStart()
    {
        ShowSelectedComponents();
        base.OnStart();
    }

    public override void OnPress(int button)
    {
        switch (button)
        {
            case 0:
                if (_index > 0)
                {
                    _index--;
                    ShowSelectedComponents();
                }
                break;
            case 1:
                //intentional to let it past the list's range
                if (_index < StateMachine.Module.ComponentsSelected.Count)
                {
                    _index++;
                    ShowSelectedComponents();
                }
                break;
            case 2:
                StateMachine.SwitchState<StList>();
                break;
            case 3:
                if (_index >= StateMachine.Module.ComponentsSelected.Count)
                {
                    StateMachine.SwitchState<StInstall>();
                    break;
                }

                if (StateMachine.Module.ComponentsSelectionImmutable[_index])
                    break;

                //invert the value
                StateMachine.Module.ComponentsSelected[_index] ^= true;
                ShowSelectedComponents();
                break;
            default:
                break;
        }
        base.OnPress(button);
    }
}

public class StInstall : UpdaterStateMachine.State
{
    private int _percentage = 0;
    private Coroutine _loadingRoutine = null;
    private enum Pass
    {
        Unpressed,
        Correct,
        Failed,
        ForcedPass,
        AwaitingRetry
    }
    private Pass _doesPass = Pass.Unpressed;

    public StInstall(UpdaterStateMachine stateMachine) : base(stateMachine)
    {
    }

    private IEnumerator InstallUpdates()
    {
        int timeMult = StateMachine.Module.ComponentsSelected[3] ? 10 : 120;
        float timeTotal = UnityEngine.Random.Range(1f, 2f) * timeMult;
        Queue<float> percentageIncreaseMoments = new Queue<float>(Enumerable.Range(0, 99).Select(_ => UnityEngine.Random.Range(0, timeTotal)).OrderBy(x => x));

        float timer = 0;
        while (timer < timeTotal)
        {
            while (percentageIncreaseMoments.Count > 0 && timer > percentageIncreaseMoments.Peek())
            {
                percentageIncreaseMoments.Dequeue();
                _percentage++;
                ShowPercentage();
            }

            yield return null;
            timer += Time.deltaTime;
        }

        Debug.Log(_doesPass);
        if (_doesPass.EqualsAny(Pass.Correct, Pass.ForcedPass))
        {
            //switch state to win here
            yield break;
        }

        FailInstall();
    }

    private void ShowPercentage()
    {
        StateMachine.Module.Marquee.AssignTexts(new string[] { "Updating...|" + _percentage + "% complete" }, true);
    }

    private void FailInstall()
    {
        _percentage = 0;
        if (_loadingRoutine != null)
        {
            StateMachine.Module.StopCoroutine(_loadingRoutine);
            _loadingRoutine = null;
        }
        StateMachine.Module.Marquee.AssignTexts(new string[] { "Update Failed|Press Confirm" });
        _doesPass = Pass.AwaitingRetry;
    }

    public override void OnStart()
    {
        _doesPass = StateMachine.Module.ComponentsSelected[2] ? Pass.ForcedPass : Pass.Unpressed;
        _percentage = 0;
        ShowPercentage();
        _loadingRoutine = StateMachine.Module.StartCoroutine(InstallUpdates());
        base.OnStart();
    }

    public override void OnEnd()
    {
        if (_loadingRoutine != null)
        {
            StateMachine.Module.StopCoroutine(_loadingRoutine);
            _loadingRoutine = null;
        }
        base.OnEnd();
    }

    public override void OnPress(int button)
    {
        switch (button)
        {
            case 2:
                if (_doesPass == Pass.AwaitingRetry)
                {
                    StateMachine.SwitchState<StPick>();
                    break;
                }

                FailInstall();
                break;
            case 3:
                switch (_doesPass)
                {
                    case Pass.AwaitingRetry:
                        _loadingRoutine = StateMachine.Module.StartCoroutine(InstallUpdates());
                        _doesPass = Pass.Unpressed;
                        break;
                    case Pass.Unpressed:
                        if (Math.Abs(_percentage - UpdateLog.EvaluateTotalScore(StateMachine.Module.UpdateLogList)) > 5)
                        {
                            _doesPass = Pass.Failed;
                            break;
                        }

                        _doesPass = Pass.Correct;
                        break;
                    case Pass.Correct:
                        _doesPass = Pass.Failed;
                        break;
                    case Pass.Failed: case Pass.ForcedPass:
                        break;
                    default:
                        break;
                }
                break;
            default:
                break;
        }
        base.OnPress(button);
    }
}