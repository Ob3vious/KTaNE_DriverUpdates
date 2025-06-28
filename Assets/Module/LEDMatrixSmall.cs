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

    private Coroutine LEDAnimCoroutine;
    private bool ModuleIsActive;

    public class LEDAnim
    {
        public string Name;
        public float Interval;
        public FrameBit[][] Frames;

        public LEDAnim(string name, float interval, FrameBit[][] frames)
        {
            Name = name;
            Interval = interval;
            Frames = frames;
        }
    }

    public struct FrameBit
    {
        public int Row;
        public int Column;
        public bool State;

        public FrameBit(int x, int y, bool status)
        {
            Row = x;
            Column = y;
            State = status;
        }
    }

    private static readonly LEDAnim[] AllAnims = new LEDAnim[]
    {
        new LEDAnim("wifi", 0.25f, new FrameBit[][]
        {
            new FrameBit[]
            {
                new FrameBit(6, 3, true),
                new FrameBit(6, 4, true),
                new FrameBit(7, 3, true),
                new FrameBit(7, 4, true)
            },
            new FrameBit[]
            {
                new FrameBit(5, 2, true),
                new FrameBit(5, 5, true),
                new FrameBit(4, 3, true),
                new FrameBit(4, 4, true),
            },
            new FrameBit[]
            {
                new FrameBit(3, 1, true),
                new FrameBit(3, 6, true),
                new FrameBit(2, 2, true),
                new FrameBit(2, 3, true),
                new FrameBit(2, 4, true),
                new FrameBit(2, 5, true),
            },
            new FrameBit[]
            {
                new FrameBit(1, 0, true),
                new FrameBit(1, 7, true),
                new FrameBit(0, 1, true),
                new FrameBit(0, 2, true),
                new FrameBit(0, 3, true),
                new FrameBit(0, 4, true),
                new FrameBit(0, 5, true),
                new FrameBit(0, 6, true),
            }
        }),
        new LEDAnim("download", 0.1f, new FrameBit[][]
        {
            new FrameBit[]
            {
                new FrameBit(7, 1, true),
                new FrameBit(7, 2, true),
                new FrameBit(7, 3, true),
                new FrameBit(7, 4, true),
                new FrameBit(7, 5, true),
                new FrameBit(7, 6, true)
            },
            new FrameBit[]
            {
                new FrameBit(0, 3, true),
                new FrameBit(0, 4, true)
            },
            new FrameBit[]
            {
                new FrameBit(1, 3, true),
                new FrameBit(1, 4, true)
            },
            new FrameBit[]
            {
                new FrameBit(2, 3, true),
                new FrameBit(2, 4, true)
            },
            new FrameBit[]
            {
                new FrameBit(3, 1, true),
                new FrameBit(3, 2, true),
                new FrameBit(3, 3, true),
                new FrameBit(3, 4, true),
                new FrameBit(3, 5, true),
                new FrameBit(3, 6, true)
            },
            new FrameBit[]
            {
                new FrameBit(4, 2, true),
                new FrameBit(4, 3, true),
                new FrameBit(4, 4, true),
                new FrameBit(4, 5, true)
            },
            new FrameBit[]
            {
                new FrameBit(5, 3, true),
                new FrameBit(5, 4, true)
            },
            new FrameBit[]
            {
                new FrameBit(0, 3, false),
                new FrameBit(0, 4, false)
            },
            new FrameBit[]
            {
                new FrameBit(1, 3, false),
                new FrameBit(1, 4, false)
            },
            new FrameBit[]
            {
                new FrameBit(2, 3, false),
                new FrameBit(2, 4, false)
            },
            new FrameBit[]
            {
                new FrameBit(3, 1, false),
                new FrameBit(3, 2, false),
                new FrameBit(3, 3, false),
                new FrameBit(3, 4, false),
                new FrameBit(3, 5, false),
                new FrameBit(3, 6, false)
            },
            new FrameBit[]
            {
                new FrameBit(4, 2, false),
                new FrameBit(4, 3, false),
                new FrameBit(4, 4, false),
                new FrameBit(4, 5, false)
            },
            new FrameBit[]
            {
                new FrameBit(5, 3, false),
                new FrameBit(5, 4, false)
            }
        }),
        new LEDAnim("x", -1, new FrameBit[][]
        {
            new FrameBit[]
            {
                new FrameBit(0, 0, true),
                new FrameBit(0, 1, true),
                new FrameBit(0, 6, true),
                new FrameBit(0, 7, true),

                new FrameBit(1, 0, true),
                new FrameBit(1, 1, true),
                new FrameBit(1, 2, true),
                new FrameBit(1, 5, true),
                new FrameBit(1, 6, true),
                new FrameBit(1, 7, true),

                new FrameBit(2, 1, true),
                new FrameBit(2, 2, true),
                new FrameBit(2, 3, true),
                new FrameBit(2, 4, true),
                new FrameBit(2, 5, true),
                new FrameBit(2, 6, true),

                new FrameBit(3, 2, true),
                new FrameBit(3, 3, true),
                new FrameBit(3, 4, true),
                new FrameBit(3, 5, true),

                new FrameBit(4, 2, true),
                new FrameBit(4, 3, true),
                new FrameBit(4, 4, true),
                new FrameBit(4, 5, true),

                new FrameBit(5, 1, true),
                new FrameBit(5, 2, true),
                new FrameBit(5, 3, true),
                new FrameBit(5, 4, true),
                new FrameBit(5, 5, true),
                new FrameBit(5, 6, true),

                new FrameBit(6, 0, true),
                new FrameBit(6, 1, true),
                new FrameBit(6, 2, true),
                new FrameBit(6, 5, true),
                new FrameBit(6, 6, true),
                new FrameBit(6, 7, true),

                new FrameBit(7, 0, true),
                new FrameBit(7, 1, true),
                new FrameBit(7, 6, true),
                new FrameBit(7, 7, true)
            }
        }),
        new LEDAnim("tick", -1, new FrameBit[][]
        {
            new FrameBit[]
            {
                new FrameBit(0, 6, true),
                new FrameBit(0, 7, true),

                new FrameBit(1, 5, true),
                new FrameBit(1, 6, true),
                new FrameBit(1, 7, true),

                new FrameBit(2, 5, true),
                new FrameBit(2, 6, true),

                new FrameBit(3, 4, true),
                new FrameBit(3, 5, true),

                new FrameBit(4, 0, true),
                new FrameBit(4, 1, true),
                new FrameBit(4, 4, true),
                new FrameBit(4, 5, true),

                new FrameBit(5, 0, true),
                new FrameBit(5, 1, true),
                new FrameBit(5, 2, true),
                new FrameBit(5, 3, true),
                new FrameBit(5, 4, true),

                new FrameBit(6, 1, true),
                new FrameBit(6, 2, true),
                new FrameBit(6, 3, true),
                new FrameBit(6, 4, true),

                new FrameBit(7, 2, true),
                new FrameBit(7, 3, true)
            }
        }),
        new LEDAnim("box empty", -1, new FrameBit[][]
        {
            new FrameBit[]
            {
                new FrameBit(0, 0, true),
                new FrameBit(0, 1, true),
                new FrameBit(0, 2, true),
                new FrameBit(0, 3, true),
                new FrameBit(0, 4, true),
                new FrameBit(0, 5, true),
                new FrameBit(0, 6, true),
                new FrameBit(0, 7, true),

                new FrameBit(1, 0, true),
                new FrameBit(1, 7, true),
                new FrameBit(2, 0, true),
                new FrameBit(2, 7, true),
                new FrameBit(3, 0, true),
                new FrameBit(3, 7, true),
                new FrameBit(4, 0, true),
                new FrameBit(4, 7, true),
                new FrameBit(5, 0, true),
                new FrameBit(5, 7, true),
                new FrameBit(6, 0, true),
                new FrameBit(6, 7, true),

                new FrameBit(7, 0, true),
                new FrameBit(7, 1, true),
                new FrameBit(7, 2, true),
                new FrameBit(7, 3, true),
                new FrameBit(7, 4, true),
                new FrameBit(7, 5, true),
                new FrameBit(7, 6, true),
                new FrameBit(7, 7, true)
            }
        }),
        new LEDAnim("box filled", -1, new FrameBit[][]
        {
            new FrameBit[]
            {
                new FrameBit(0, 0, true),
                new FrameBit(0, 1, true),
                new FrameBit(0, 2, true),
                new FrameBit(0, 3, true),
                new FrameBit(0, 4, true),
                new FrameBit(0, 5, true),
                new FrameBit(0, 6, true),
                new FrameBit(0, 7, true),

                new FrameBit(1, 0, true),
                new FrameBit(1, 7, true),
                new FrameBit(2, 0, true),
                new FrameBit(2, 7, true),
                new FrameBit(3, 0, true),
                new FrameBit(3, 7, true),
                new FrameBit(4, 0, true),
                new FrameBit(4, 7, true),
                new FrameBit(5, 0, true),
                new FrameBit(5, 7, true),
                new FrameBit(6, 0, true),
                new FrameBit(6, 7, true),

                new FrameBit(7, 0, true),
                new FrameBit(7, 1, true),
                new FrameBit(7, 2, true),
                new FrameBit(7, 3, true),
                new FrameBit(7, 4, true),
                new FrameBit(7, 5, true),
                new FrameBit(7, 6, true),
                new FrameBit(7, 7, true),

                new FrameBit(2, 3, true),
                new FrameBit(2, 4, true),
                new FrameBit(3, 2, true),
                new FrameBit(3, 3, true),
                new FrameBit(3, 4, true),
                new FrameBit(3, 5, true),
                new FrameBit(4, 2, true),
                new FrameBit(4, 3, true),
                new FrameBit(4, 4, true),
                new FrameBit(4, 5, true),
                new FrameBit(5, 3, true),
                new FrameBit(5, 4, true)
            }
        }),
        new LEDAnim("throbber", 0.05f, new FrameBit[][]
        {
            new FrameBit[]
            {
                new FrameBit(0, 3, true),
                new FrameBit(0, 4, true),
                new FrameBit(1, 3, true),
                new FrameBit(1, 4, true),

                new FrameBit(1, 5, true),
                new FrameBit(1, 6, true),
                new FrameBit(2, 5, true),
                new FrameBit(2, 6, true),

                new FrameBit(3, 6, true),
                new FrameBit(3, 7, true),
                new FrameBit(4, 6, true),
                new FrameBit(4, 7, true),

                new FrameBit(5, 5, true),
                new FrameBit(5, 6, true),
                new FrameBit(6, 5, true),
                new FrameBit(6, 6, true)
            },
            new FrameBit[]
            {
                new FrameBit(0, 3, false),
                new FrameBit(1, 3, false),

                new FrameBit(6, 4, true),
                new FrameBit(7, 4, true)
            },
            new FrameBit[]
            {
                new FrameBit(0, 4, false),
                new FrameBit(1, 4, false),

                new FrameBit(6, 3, true),
                new FrameBit(7, 3, true)
            },
            new FrameBit[]
            {
                new FrameBit(1, 5, false),
                new FrameBit(1, 6, false),
                new FrameBit(2, 5, false),

                new FrameBit(5, 2, true),
                new FrameBit(6, 1, true),
                new FrameBit(6, 2, true)
            },
            new FrameBit[]
            {
                new FrameBit(2, 6, false),

                new FrameBit(5, 1, true)
            },
            new FrameBit[]
            {
                new FrameBit(3, 6, false),
                new FrameBit(3, 7, false),

                new FrameBit(4, 0, true),
                new FrameBit(4, 1, true)
            },
            new FrameBit[]
            {
                new FrameBit(4, 6, false),
                new FrameBit(4, 7, false),

                new FrameBit(3, 0, true),
                new FrameBit(3, 1, true)
            },
            new FrameBit[]
            {
                new FrameBit(5, 5, false),
                new FrameBit(5, 6, false),
                new FrameBit(6, 6, false),

                new FrameBit(1, 1, true),
                new FrameBit(2, 1, true),
                new FrameBit(2, 2, true)
            },
            new FrameBit[]
            {
                new FrameBit(6, 5, false),

                new FrameBit(1, 2, true)
            },
            new FrameBit[]
            {
                new FrameBit(6, 4, false),
                new FrameBit(7, 4, false),

                new FrameBit(0, 3, true),
                new FrameBit(1, 3, true)
            },
            new FrameBit[]
            {
                new FrameBit(6, 3, false),
                new FrameBit(7, 3, false),

                new FrameBit(0, 4, true),
                new FrameBit(1, 4, true)
            },
            new FrameBit[]
            {
                new FrameBit(5, 2, false),
                new FrameBit(6, 1, false),
                new FrameBit(6, 2, false),

                new FrameBit(1, 5, true),
                new FrameBit(1, 6, true),
                new FrameBit(2, 5, true)
            },
            new FrameBit[]
            {
                new FrameBit(5, 1, false),

                new FrameBit(2, 6, true)
            },
            new FrameBit[]
            {
                new FrameBit(4, 0, false),
                new FrameBit(4, 1, false),

                new FrameBit(3, 6, true),
                new FrameBit(3, 7, true)
            },
            new FrameBit[]
            {
                new FrameBit(3, 0, false),
                new FrameBit(3, 1, false),

                new FrameBit(4, 6, true),
                new FrameBit(4, 7, true)
            },
            new FrameBit[]
            {
                new FrameBit(1, 1, false),
                new FrameBit(2, 1, false),
                new FrameBit(2, 2, false),

                new FrameBit(5, 5, true),
                new FrameBit(5, 6, true),
                new FrameBit(6, 6, true)
            }
        }),
        new LEDAnim("box filled", -1, new FrameBit[][]
        {
            new FrameBit[]
            {
                new FrameBit(2, 2, true),
                new FrameBit(2, 5, true),
                new FrameBit(4, 2, true),
                new FrameBit(4, 5, true),
                new FrameBit(5, 3, true),
                new FrameBit(5, 4, true),
            }
        })
    };

    private LEDAnim FindAnimByName(string name)
    {
        foreach (var anim in AllAnims)
            if (anim.Name == name.ToLowerInvariant())
                return anim;

        Debug.LogWarning("Failed to find animation \"" + name + "\"!");
        return null;
    }

    private void Start()
    {
        Setup();
    }

    public void MakeActive()
    {
        ModuleIsActive = true;
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

        BlankOut();
    }

    public void SetLED(int row, int col, bool state)
    {
        LEDRends[row][col].material = state ? OnMat : OffMat;
        if (state) LEDRends[row][col].material.color = Colour;
    }

    public void RunAnimation(string name)
    {
        var anim = FindAnimByName(name);
        if (anim != null)
        {
            if (LEDAnimCoroutine != null)
                StopCoroutine(LEDAnimCoroutine);
            LEDAnimCoroutine = StartCoroutine(DisplayAnimation(anim));
        }
        else
            BlankOut();
    }

    public void RunAnimation(LEDAnim anim)
    {
        if (LEDAnimCoroutine != null)
            StopCoroutine(LEDAnimCoroutine);
        LEDAnimCoroutine = StartCoroutine(DisplayAnimation(anim));
    }

    private IEnumerator DisplayAnimation(LEDAnim anim)
    {
        yield return new WaitUntil(() => ModuleIsActive);
        if (anim.Interval == -1)
        {
            BlankOut();
            foreach (var frame in anim.Frames[0])
                SetLED(frame.Row, frame.Column, frame.State);
            yield break;
        }
        else
        {
            while (true)
            {
                BlankOut();
                for (int i = 0; i < anim.Frames.Length; i++)
                {
                    foreach (var frame in anim.Frames[i])
                        SetLED(frame.Row, frame.Column, frame.State);

                    yield return new WaitForSeconds(anim.Interval);
                }
            }
        }
    }

    public void BlankOut()
    {
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                SetLED(i, j, false);
    }
}
