using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        _states = new List<State> { new StFetch(this), new StList(this), new StPick(this), new StAllocate(this), new StInstall(this), new StSolve(this) };
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

            if (StateMachine.Module.PotentialSolution.Sum(x => x.CellCount - x.Value - 1) < 4)
            {
                StateMachine.Module.Log("Failed to find a solution. Retrying...");
                StateMachine.Module.Marquee.AssignTexts(new string[] { "Connection Lost" });
                StateMachine.Module.GridRend.RunAnimation("x");
                continue;
            }

            break;
        }
        //bends add 1pt each to complexity
        while (true);

        StateMachine.Module.ComponentsSelected = new List<bool> { true, true, false, false };
        StateMachine.Module.ComponentsSelectionImmutable = new List<bool> { true, true, false, false };
        StateMachine.Module.ComponentNames = new List<string> { "Program", "Libraries", "Installer Patch", "Faster Loading" };

        StateMachine.Module.Marquee.AssignTexts(new string[] { "Updates Gathered|Press Confirm" });
        StateMachine.Module.GridRend.RunAnimation("tick");

        List<int> shapeOrder = new List<int>();
        while (shapeOrder.Count < StateMachine.Module.ComponentSizes.Count)
            shapeOrder.Add(Enumerable.Range(0, StateMachine.Module.ComponentSizes.Count)
                .First(x => !shapeOrder.Contains(x) && StateMachine.Module.ComponentSizes[shapeOrder.Count] == StateMachine.Module.PotentialSolution[x].Value));

        StateMachine.Module.PotentialSolution = shapeOrder.Select(x => StateMachine.Module.PotentialSolution[x]).ToList();
        shapeOrder = Enumerable.Range(0, shapeOrder.Count).ToList();

        StateMachine.Module.Log("Found a grid and solution with sizes [{0}]: {1}.", StateMachine.Module.ComponentSizes.Join(", "),
             Enumerable.Range(0, 8).Select(y => Enumerable.Range(0, 8).Select(x =>
             StateMachine.Module.Puzzle.Grid[y, x] ? "#" :
             (shapeOrder.Any(z => StateMachine.Module.PotentialSolution[z].Cell(x, y)) ? (shapeOrder.First(z => StateMachine.Module.PotentialSolution[z].Cell(x, y)) + 1).ToString() : "-")).Join("")).Join(";"));

        //thread stuff is done here

        _ready = true;
    }

    private void GenerateUpdateLogs()
    {
        StateMachine.Module.UpdateLogList = UpdateLog.GenerateLogs();

        for (int i = 0; i < StateMachine.Module.UpdateLogList.Count; i++)
            StateMachine.Module.Log("Generated update {0}/{1}: {2}. The score for this update is {3}.", i + 1, StateMachine.Module.UpdateLogList.Count, StateMachine.Module.UpdateLogList[i], StateMachine.Module.UpdateLogList[i].EvaluateScore());

        StateMachine.Module.Log("The total score comes out to be {0}.", UpdateLog.EvaluateTotalScore(StateMachine.Module.UpdateLogList));
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
        StateMachine.Module.GridRend.ClearAnimation();

        if (_index >= StateMachine.Module.ComponentsSelected.Count)
        {
            StateMachine.Module.Marquee.AssignTexts(new string[] { "Selected " + StateMachine.Module.ComponentsSelected.Count(x => x) + "/" + StateMachine.Module.ComponentsSelected.Count + "|Press Confirm" });

            return;
        }

        StateMachine.Module.Marquee.AssignTexts(new string[] {
            StateMachine.Module.ComponentNames[_index] + " (" + StateMachine.Module.ComponentSizes[_index] + "kB)|"
            + (StateMachine.Module.ComponentsSelected[_index] ? "Selected" : "Not Selected") +  (StateMachine.Module.ComponentsSelectionImmutable[_index] ? "*" : "") });

        if (StateMachine.Module.ComponentsSelected[_index])
            StateMachine.Module.GridRend.RunAnimation("tick");
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
                    StateMachine.Module.Log("Attempting to allocate: {0}.", Enumerable.Range(0, StateMachine.Module.ComponentsSelected.Count).Where(x => StateMachine.Module.ComponentsSelected[x])
                        .Select(x => StateMachine.Module.ComponentNames[x] + " (" + StateMachine.Module.ComponentSizes[x] + "kB)").Join(", "));
                    StateMachine.SwitchState<StAllocate>();
                    break;
                }

                if (StateMachine.Module.ComponentsSelectionImmutable[_index])
                    break;

                //invert the value
                StateMachine.Module.ComponentsSelected[_index] ^= true;
                StateMachine.Module.Log("{0} the installation of \"{1}\".", StateMachine.Module.ComponentsSelected[_index] ? "Enabled" : "Disabled", StateMachine.Module.ComponentNames[_index]);
                ShowSelectedComponents();
                break;
            default:
                break;
        }
        base.OnPress(button);
    }
}

public class StAllocate : UpdaterStateMachine.State
{
    private List<DriverStoragePuzzle.Shape> _shapes = new List<DriverStoragePuzzle.Shape> { null, null, null, null };
    private int _index = 0;
    private bool _selectedRegion = false;
    private int _cursor = -1;
    private int _selectedCell = -1;
    private List<int> _candidates = new List<int>();

    public StAllocate(UpdaterStateMachine stateMachine) : base(stateMachine)
    {
    }

    private void EvaluateCandidates()
    {
        _candidates.Clear();
        _cursor = -1;

        if (_shapes[_index].Value >= StateMachine.Module.ComponentSizes[_index])
        {
            ForceDeselect();
            return;
        }

        int width = StateMachine.Module.Puzzle.Width;
        int height = StateMachine.Module.Puzzle.Height;

        if (_selectedCell < 0)
        {
            for (int i = 0; i < width * height; i++)
            {
                int x = i % width;
                int y = i / width;

                if (StateMachine.Module.Puzzle.Grid[y, x] || _shapes.Any(z => z != null && z.Cell(x, y)))
                    continue;

                if (_shapes[_index].CellCount == 0)
                {
                    _candidates.Add(i);
                    continue;
                }

                bool hasUp = y > 0;
                bool hasDown = y < height - 1;
                bool hasLeft = x > 0;
                bool hasRight = x < width - 1;

                if (hasUp && _shapes[_index].Cell(x, y - 1) ||
                    hasDown && _shapes[_index].Cell(x, y + 1) ||
                    hasLeft && _shapes[_index].Cell(x - 1, y) ||
                    hasRight && _shapes[_index].Cell(x + 1, y))
                    continue;

                if ((hasUp && hasLeft && _shapes[_index].Cell(x - 1, y - 1)) ||
                    (hasUp && hasRight && _shapes[_index].Cell(x + 1, y - 1)) ||
                    (hasDown && hasLeft && _shapes[_index].Cell(x - 1, y + 1)) ||
                    (hasDown && hasRight && _shapes[_index].Cell(x + 1, y + 1)))
                    _candidates.Add(i);

                continue;
            }

            if (_candidates.Count > 0)
                _cursor = _candidates.First();
            else
                ForceDeselect();

            return;
        }

        {
            int x = _selectedCell % width;
            int y = _selectedCell / width;

            bool hasUp = y > 0;
            bool hasDown = y < height - 1;
            bool hasLeft = x > 0;
            bool hasRight = x < width - 1;

            for (int i = -1; i < 2; i += 2)
            {
                if ((i < 0 && !hasUp) || (i > 0 && !hasDown))
                    continue;

                for (int j = -1; j < 2; j += 2)
                {
                    if ((j < 0 && !hasLeft) || (j > 0 && !hasRight))
                        continue;

                    int maxH = width;
                    for (int k = 0; maxH > 0; k++)
                    {
                        int newY = y + i * k;

                        if (newY < 0 || newY >= height)
                            break;
                        for (int l = 0; l < maxH; l++)
                        {
                            int newX = x + j * l;

                            if (newX < 0 || newX >= width ||
                                StateMachine.Module.Puzzle.Grid[newY, newX] || _shapes.Any(z => z != null && z.Cell(newX, newY)) ||
                                (newX - 1 >= 0 && _shapes[_index].Cell(newX - 1, newY)) ||
                                (newX + 1 < width && _shapes[_index].Cell(newX + 1, newY)) ||
                                (newY - 1 >= 0 && _shapes[_index].Cell(newX, newY - 1)) ||
                                (newY + 1 < height && _shapes[_index].Cell(newX, newY + 1)))
                            {
                                maxH = l;
                                break;
                            }

                            _candidates.Add(newY * width + newX);
                        }
                    }
                }
            }

            _candidates = _candidates.Distinct().OrderBy(z => z).ToList();

            if (_candidates.Count > 0)
                _cursor = _candidates.First();
            else
                ForceDeselect();
        }
    }

    private void ForceDeselect()
    {
        _selectedRegion = false;
        _selectedCell = -1;
        _cursor = -1;
    }

    private void RenderShape()
    {
        if (_shapes[_index] == null)
            _shapes[_index] = new DriverStoragePuzzle.Shape(StateMachine.Module.Puzzle.Width, StateMachine.Module.Puzzle.Height);

        LEDMatrixSmall.FrameBit[][] frames = new LEDMatrixSmall.FrameBit[2][];
        DriverStoragePuzzle puzzle = StateMachine.Module.Puzzle;
        //LEDMatrixSmall.LEDAnim anim = new LEDMatrixSmall.LEDAnim()

        if (!_selectedRegion)
        {
            frames[0] = Enumerable.Range(0, puzzle.Width * puzzle.Height).Select(x => new LEDMatrixSmall.FrameBit(x / puzzle.Width, x % puzzle.Width,
                puzzle.Grid[x / puzzle.Width, x % puzzle.Width] || _shapes.Any(z => z != null && z.Cell(x % puzzle.Width, x / puzzle.Width))))
                .Where(x => x.State).ToArray();
            List<LEDMatrixSmall.FrameBit> pixelList = new List<LEDMatrixSmall.FrameBit>();
            for (int i = 0; i < puzzle.Height; i++)
                for (int j = 0; j < puzzle.Width; j++)
                    if (_shapes[_index].Cell(j, i))
                        pixelList.Add(new LEDMatrixSmall.FrameBit(i, j, false));

            frames[1] = pixelList.ToArray();

            frames[0] = frames[0].Concat(frames[1].Select(x => new LEDMatrixSmall.FrameBit(x.Row, x.Column, true))).ToArray();

            StateMachine.Module.GridRend.AllDisplays.First().RunAnimation(new LEDMatrixSmall.LEDAnim("gridRender", 0.5f, frames));

            return;
        }

        frames[0] = Enumerable.Range(0, puzzle.Width * puzzle.Height)
            .Select(x => new LEDMatrixSmall.FrameBit(x / puzzle.Width, x % puzzle.Width,
            puzzle.Grid[x / puzzle.Width, x % puzzle.Width] || _shapes[_index].Cell(x % puzzle.Width, x / puzzle.Width) || _shapes.Any(z => z != null && z.Cell(x % puzzle.Width, x / puzzle.Width))))
            .Where(x => x.State).ToArray();

        List<LEDMatrixSmall.FrameBit> newPixels = new List<LEDMatrixSmall.FrameBit>();
        if (_selectedCell < 0)
            newPixels.Add(new LEDMatrixSmall.FrameBit(_cursor / puzzle.Width, _cursor % puzzle.Width, false));
        else
        {
            int minX = Math.Min(_cursor % puzzle.Width, _selectedCell % puzzle.Width);
            int maxX = Math.Max(_cursor % puzzle.Width, _selectedCell % puzzle.Width);
            int minY = Math.Min(_cursor / puzzle.Width, _selectedCell / puzzle.Width);
            int maxY = Math.Max(_cursor / puzzle.Width, _selectedCell / puzzle.Width);

            for (int i = minY; i <= maxY; i++)
                for (int j = minX; j <= maxX; j++)
                    newPixels.Add(new LEDMatrixSmall.FrameBit(i, j, false));
        }

        frames[1] = newPixels.ToArray();

        frames[0] = frames[0].Concat(frames[1].Select(x => new LEDMatrixSmall.FrameBit(x.Row, x.Column, true))).ToArray();

        StateMachine.Module.GridRend.AllDisplays.First().RunAnimation(new LEDMatrixSmall.LEDAnim("gridRender", _selectedCell < 0 ? 0.25f : 0.5f, frames));
    }

    private void UpdateText()
    {
        if (!_selectedRegion || _selectedCell < 0)
        {
            StateMachine.Module.Marquee.AssignTexts(new string[] { StateMachine.Module.ComponentNames[_index] + ":|" + _shapes[_index].Value + "/" + StateMachine.Module.ComponentSizes[_index] });
            return;
        }

        int width = StateMachine.Module.Puzzle.Width;
        int extraValue = (Math.Abs(_cursor % width - _selectedCell % width) + 1) * (Math.Abs(_cursor / width - _selectedCell / width) + 1) - 1;
        StateMachine.Module.Marquee.AssignTexts(new string[] { StateMachine.Module.ComponentNames[_index] + ":|" + (_shapes[_index].Value + extraValue) + "/" + StateMachine.Module.ComponentSizes[_index] + " (+" + extraValue + ")" });
    }

    public override void OnStart()
    {
        List<bool> components = StateMachine.Module.ComponentsSelected;
        _shapes = Enumerable.Range(0, components.Count).Select(x => !components[x] ? null : _shapes[x]).ToList();

        _index = Enumerable.Range(0, components.Count).Last(x => x <= _index && components[x]);
        ForceDeselect();
        RenderShape();
        UpdateText();
        base.OnStart();
    }

    public override void OnEnd()
    {
        StateMachine.Module.GridRend.ClearAnimation();
        base.OnEnd();
    }

    public override void OnPress(int button)
    {
        switch (button)
        {
            case 0:
                if (_selectedRegion)
                {
                    int curIndex = _candidates.IndexOf(_cursor) - 1;
                    if (curIndex < 0)
                        curIndex += _candidates.Count;
                    _cursor = _candidates[curIndex];

                    RenderShape();
                    if (_selectedCell >= 0)
                        UpdateText();
                    break;
                }

                { //fuck scopes
                    List<bool> selection = StateMachine.Module.ComponentsSelected;
                    if (selection.Take(_index).Any(x => x))
                        _index = Enumerable.Range(0, selection.Count).Take(_index).Last(x => selection[x]);

                    RenderShape();
                    UpdateText();
                }
                break;
            case 1:
                if (_selectedRegion)
                {
                    int curIndex = _candidates.IndexOf(_cursor) + 1;
                    if (curIndex >= _candidates.Count)
                        curIndex -= _candidates.Count;
                    _cursor = _candidates[curIndex];

                    RenderShape();
                    if (_selectedCell >= 0)
                        UpdateText();
                    break;
                }

                { //fuck scopes
                    List<bool> selection = StateMachine.Module.ComponentsSelected;
                    if (selection.Skip(_index + 1).Any(x => x))
                        _index = Enumerable.Range(0, selection.Count).Skip(_index + 1).First(x => selection[x]);

                    RenderShape();
                    UpdateText();
                    break;
                }
            case 2:
                if (_selectedRegion)
                {
                    if (_selectedCell >= 0)
                    {
                        int cursorMaybe = _selectedCell;
                        _selectedCell = -1;
                        EvaluateCandidates();
                        if (_candidates.Contains(cursorMaybe))
                            _cursor = cursorMaybe;
                        RenderShape();
                        UpdateText();
                        break;
                    }

                    _selectedRegion = false;
                    RenderShape();
                    break;
                }

                if (_shapes[_index].CellCount > 0)
                {
                    _shapes[_index] = new DriverStoragePuzzle.Shape(StateMachine.Module.Puzzle.Width, StateMachine.Module.Puzzle.Height);
                    RenderShape();
                    UpdateText();
                    break;
                }

                StateMachine.SwitchState<StPick>();
                break;
            case 3:

                if (_shapes[_index] == null)
                    _shapes[_index] = new DriverStoragePuzzle.Shape(StateMachine.Module.Puzzle.Width, StateMachine.Module.Puzzle.Height);

                if (_selectedRegion)
                {
                    if (_selectedCell < 0)
                    {
                        _selectedCell = _cursor;
                        EvaluateCandidates();
                        RenderShape();
                        UpdateText();
                        break;
                    }

                    int width = StateMachine.Module.Puzzle.Width;
                    int x1 = _selectedCell % width;
                    int x2 = _cursor % width;
                    int y1 = _selectedCell / width;
                    int y2 = _cursor / width;

                    _shapes[_index].RegisterRect(Math.Min(y1, y2) * width + Math.Min(x1, x2), Math.Max(y1, y2) * width + Math.Max(x1, x2));
                    _selectedCell = -1;
                    EvaluateCandidates();
                    RenderShape();
                    UpdateText();
                    break;
                }

                if (Enumerable.Range(0, StateMachine.Module.ComponentSizes.Count).All(x => !StateMachine.Module.ComponentsSelected[x] || (_shapes[x] != null && StateMachine.Module.ComponentSizes[x] == _shapes[x].Value)))
                {
                    List<int> shapeOrder = Enumerable.Range(0, _shapes.Count).Where(x => _shapes[x] != null).ToList();
                    StateMachine.Module.Log("The memory allocation has been solved: {0}.",
                        Enumerable.Range(0, 8).Select(y => Enumerable.Range(0, 8).Select(x =>
                        StateMachine.Module.Puzzle.Grid[y, x] ? "#" :
                        (shapeOrder.Any(z => _shapes[z].Cell(x, y)) ? (shapeOrder.First(z => _shapes[z].Cell(x, y)) + 1).ToString() : "-")).Join("")).Join(";"));

                    StateMachine.SwitchState<StInstall>();
                    break;
                }

                _selectedRegion = true;
                _selectedCell = -1;
                EvaluateCandidates();
                RenderShape();
                UpdateText();
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
        int timeMult = StateMachine.Module.ComponentsSelected[3] ? 15 : 120;
        float timeTotal = UnityEngine.Random.Range(1f, 2f) * timeMult;
        Queue<float> percentageIncreaseMoments = new Queue<float>(Enumerable.Range(0, 99).Select(_ => UnityEngine.Random.Range(0, timeTotal)).OrderBy(x => x));

        StateMachine.Module.GridRend.RunAnimation("throbber");
        StateMachine.Module.Log("Starting installation. Estimated time: {0} seconds.", timeTotal);
        if (_doesPass == Pass.ForcedPass)
            StateMachine.Module.Log("The update is guaranteed to succeed.");

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

        if (_doesPass.EqualsAny(Pass.Correct, Pass.ForcedPass))
        {
            StateMachine.Module.Log("The updates have been applied successfully. Solving module.");
            StateMachine.SwitchState<StSolve>();
            yield break;
        }

        if (_doesPass == Pass.Unpressed)
            StateMachine.Module.Log("Did not press confirm. The update will fail.");
        StateMachine.Module.Log("Update failed.");
        FailInstall();
    }

    private void ShowPercentage()
    {
        StateMachine.Module.Marquee.AssignTexts(new string[] { "Updating...|" + _percentage + "% complete" }, true);
    }

    private void FailInstall()
    {
        StateMachine.Module.GridRend.RunAnimation("x");
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

                StateMachine.Module.Log("Update failed by choice.");
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
                        if (Math.Abs(_percentage - UpdateLog.EvaluateTotalScore(StateMachine.Module.UpdateLogList)) > 2)
                        {
                            StateMachine.Module.Log("Pressed confirm at the wrong time. The update will fail.");
                            _doesPass = Pass.Failed;
                            break;
                        }

                        StateMachine.Module.Log("Pressed confirm at the right time.");
                        _doesPass = Pass.Correct;
                        break;
                    case Pass.Correct:
                        StateMachine.Module.Log("Pressed confirm more than once. The update will fail.");
                        _doesPass = Pass.Failed;
                        break;
                    case Pass.Failed:
                    case Pass.ForcedPass:
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

public class StSolve : UpdaterStateMachine.State
{
    public StSolve(UpdaterStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void OnStart()
    {
        StateMachine.Module.GridRend.RunAnimation("tick");
        StateMachine.Module.Marquee.AssignTexts(new string[] { "Updates Complete!" });
        StateMachine.Module.GetComponent<KMBombModule>().HandlePass();
        base.OnStart();
    }

    public override void OnEnd()
    {
        throw new InvalidOperationException("Cannot 'unsolve' module");
    }
}