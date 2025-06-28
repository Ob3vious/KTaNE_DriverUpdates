using System;
using System.Collections.Generic;
using System.Linq;

public class UpdateLog
{
    //single notes
    public class UpdateNote
    {
        public enum ChangeType
        {
            PositiveAddition,
            NegativeAddition,
            PositiveRemoval,
            NegativeRemoval,
            Gibberish
        }

        public ChangeType NoteChangeType { get; private set; }
        public string Description { get; private set; }

        public static string[] PositiveLabels = new string[] { "Compatibility", "Feature", "Fix", "Improvement", "Performance" };
        public static string[] NegativeLabels = new string[] { "Bug", "Error", "Exploit", "Issue", "Jank" };

        public UpdateNote(ChangeType type)
        {
            NoteChangeType = type;
            switch (type)
            {
                case ChangeType.PositiveAddition:
                    Description = "Added " + PositiveLabels.PickRandom();
                    break;
                case ChangeType.NegativeAddition:
                    Description = "Added " + NegativeLabels.PickRandom();
                    break;
                case ChangeType.PositiveRemoval:
                    Description = "Removed " + PositiveLabels.PickRandom();
                    break;
                case ChangeType.NegativeRemoval:
                    Description = "Removed " + NegativeLabels.PickRandom();
                    break;
                case ChangeType.Gibberish:
                    Description = Enumerable.Range(0, UnityEngine.Random.Range(5, 15)).Select(x => "bcdfghjklmnpqrstvwxz".PickRandom()).Join("");
                    break;
                default:
                    break;
            }
        }

        public int Apply(int input)
        {
            switch (NoteChangeType)
            {
                case ChangeType.PositiveAddition:
                    return input + 2;
                case ChangeType.NegativeAddition:
                    return input - 3;
                case ChangeType.PositiveRemoval:
                    return input - 1;
                case ChangeType.NegativeRemoval:
                    return input + 3;
                case ChangeType.Gibberish:
                    return input * -2;
                default:
                    return input;
            }
        }

        public override string ToString()
        {
            string text = Description;
            switch (NoteChangeType)
            {
                case ChangeType.PositiveAddition:
                    text += " (+pos)";
                    break;
                case ChangeType.NegativeAddition:
                    text += " (+neg)";
                    break;
                case ChangeType.PositiveRemoval:
                    text += " (-pos)";
                    break;
                case ChangeType.NegativeRemoval:
                    text += " (-neg)";
                    break;
                case ChangeType.Gibberish:
                    text += " (???)";
                    break;
                default:
                    text += " (undef.)";
                    break;
            }
            return text;
        }
    }

    public List<UpdateNote> Notes { get; private set; }
    public int VersionNumber { get; private set; }
    public string VersionName { get; private set; }

    public UpdateLog(int versionNumberImportant, string versionName, int noteCount)
    {
        VersionNumber = versionNumberImportant;
        VersionName = versionName;

        Notes = new List<UpdateNote>();
        Array changeNoteTypes = Enum.GetValues(typeof(UpdateNote.ChangeType));
        for (int i = 0; i < noteCount; i++)
            Notes.Add(new UpdateNote((UpdateNote.ChangeType)changeNoteTypes.GetValue(UnityEngine.Random.Range(0, changeNoteTypes.Length))));
    }

    public int EvaluateScore()
    {
        int score = VersionNumber;
        foreach (UpdateNote note in Notes)
            score = note.Apply(score);

        return score;
    }

    public static int EvaluateTotalScore(List<UpdateLog> logs)
    {
        int totalScore = 0;
        foreach (UpdateLog log in logs)
        {
            int score = log.EvaluateScore();
            switch (Math.Sign(score))
            {
                case 1:
                    totalScore = Math.Abs(totalScore - score);
                    break;
                case -1:
                    totalScore = Math.Abs(totalScore * 2 + score);
                    break;
                case 0:
                    //this is equivalent to halving with commercial rounding for integers
                    totalScore = (totalScore + 1) / 2;
                    break;
                default:
                    throw new NotImplementedException("How is an integer's sign not 1, 0 or -1?");

            }
        }

        return totalScore % 101;
    }

    public static List<int> Versions = null;
    public static List<UpdateLog> GenerateLogs()
    {
        if (Versions == null)
        {
            Versions = new List<int> { 1, 0, 0 };
            int versionBumps = UnityEngine.Random.Range(10, 20);
            for (int i = 0; i < versionBumps; i++)
                BumpVersionRandom();
        }



        int updateCount = UnityEngine.Random.Range(4, 6);
        int noteCount = UnityEngine.Random.Range(15, 19);
        List<int> updatesPerLog = Enumerable.Repeat(1, updateCount).ToList();
        int totalAssigned = updateCount;
        while (totalAssigned++ < noteCount)
            updatesPerLog[Enumerable.Range(0, updatesPerLog.Count).Where(x => updatesPerLog[x] < 5).PickRandom()]++;

        List<UpdateLog> logs = new List<UpdateLog>();
        for (int i = 0; i < updateCount; i++)
        {
            BumpVersionRandom();
            logs.Add(new UpdateLog(Versions.Last(), "v" + Versions.Join("."), updatesPerLog[i]));
        }

        return logs;
    }

    public static void BumpVersionRandom()
    {
        int pointer = Versions.Count - 1;
        int pow = 1;
        while (pointer > 0 && UnityEngine.Random.Range(0, (int)Math.Pow(10, pow)) == 0)
        {
            Versions[pointer] = 0;
            pointer--;
            pow++;
        }
        Versions[pointer]++;
    }

    public override string ToString()
    {
        return VersionName + ": " + Notes.Join(", ");
    }
}