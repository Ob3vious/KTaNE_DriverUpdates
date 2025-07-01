using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        RegisterTwitchCommand(new TwitchPlaysCommand("commands", "'commands' to list all currently available commands.", new Func<IEnumerator>(() => TwitchListCommands()), 0));

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

        SideButtons[pos].AddInteractionPunch(1f);
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

    public class TwitchPlaysCommand
    {
        public string Name { get; private set; }
        public string HelpMessage { get; private set; }
        public Func<IEnumerator> Enumerator { get; private set; }
        public int ArgCount { get; private set; }

        public TwitchPlaysCommand(string name, string helpMessage, Func<IEnumerator> enumerator, int argCount)
        {
            Name = name;
            HelpMessage = helpMessage;
            Enumerator = enumerator;
            ArgCount = argCount;
        }
    }

    private Dictionary<string, TwitchPlaysCommand> _availableCommands = new Dictionary<string, TwitchPlaysCommand>();
    private List<string> _pickedCommands = new List<string>();

    public void RegisterTwitchCommand(TwitchPlaysCommand command)
    {
        if (_availableCommands.ContainsKey(command.Name))
            return;

        _availableCommands.Add(command.Name, command);
    }

    public void SetTwitchCommandActive(params string[] commands)
    {
        _pickedCommands.Clear();
        foreach (string command in commands)
        {
            //throw new Exception(command + " is not present in the list of registered commands.");
            if (_availableCommands.ContainsKey(command))
                _pickedCommands.Add(command);
        }
    }

    public IEnumerator TwitchListCommands()
    {
        yield return null;
        yield return "sendtochat {0}, commands are: " + _pickedCommands.Select(x => _availableCommands[x].HelpMessage).Join(" ");
    }

    public string[] CurrentCommand;
#pragma warning disable 414
    private string TwitchHelpMessage = "'!{0} commands' to get the commands available in a certain window. Interactions can be chained using semicolons (e.g. '!{0} next;next')";
#pragma warning restore 414
    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();

        string[] commands = command.Split(';');

        foreach (string singleCommand in commands)
        {
            string[] commandSegments = singleCommand.Split(' ').Where(x => x.Length > 0).ToArray();
            if (commandSegments.Length == 0)
            {
                yield return "sendtochaterror {0}, unable to execute empty command.";
                yield break;
            }
            string identifier = commandSegments[0];
            if (!_pickedCommands.Contains(identifier))
            {
                yield return "sendtochaterror {0}, could not find command named '" + identifier + "'.";
                yield break;
            }
            if (_availableCommands[identifier].ArgCount != commandSegments.Length - 1)
            {
                yield return "sendtochaterror {0}, command expected " + _availableCommands[identifier].ArgCount + " arguments, but was given " + (commandSegments.Length - 1) + ".";
                yield break;
            }
            CurrentCommand = commandSegments;

            IEnumerator program = _availableCommands[identifier].Enumerator.Invoke();
            while (program.MoveNext())
            {
                yield return program.Current;
                if (program.Current != null && program.Current is string && ((string)program.Current).Split(' ').First() == "sendtochaterror")
                {
                    yield break;
                }
            }

            //kinda hacky but eh
            yield return "solve";
        }

        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        while (!(_stateMachine.CurrentState is StSolve))
        {
            IEnumerator program = _stateMachine.CurrentState.HandleTwitchPlaysForceSolve();
            while (program.MoveNext())
                yield return program.Current;
        }
    }
}