using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class DriverStoragePuzzle
{
    public class Shape
    {
        private int _width, _height;
        private int[] _cellValues;

        public int Value { get; private set; }
        public int CellCount { get; private set; }

        public Shape(int width, int height)
        {
            _cellValues = new int[width * height];
            Value = 0;
            CellCount = 0;
            _width = width;
            _height = height;
            Ambitions = new Queue<int>();
        }

        public Shape(int width, int height, bool[,] grid) : this(width, height)
        {
            for (int i = 0; i < width * height; i++)
                if (grid[i / width, i % width])
                    _cellValues[i] = -1;
        }

        public Shape(Shape old)
        {
            _cellValues = (int[])old._cellValues.Clone();
            _width = old._width;
            _height = old._height;
            Value = old.Value;
            CellCount = old.CellCount;
            Ambitions = new Queue<int>(old.Ambitions);
        }

        public void RegisterCell(int cell, bool block)
        {


            if (block)
            {
                _cellValues[cell] = -1;
                return;
            }

            _cellValues[cell] = 1;

            if (CellCount == 0)
            {
                CellCount++;
                Value++;
                return;
            }

            int x = cell % _width;
            int y = cell / _width;

            int cornerMod = 0;

            for (int i = -1; i < 2; i += 2)
                for (int j = -1; j < 2; j += 2)
                {
                    bool canVerti = !((i < 0 && y == 0) || (i > 0 && y == _height - 1));
                    bool canHoriz = !((j < 0 && x == 0) || (j > 0 && x == _width - 1));


                    bool outer = canVerti && canHoriz && _cellValues[i * _width + j + cell] > 0;
                    bool horiz = canHoriz && _cellValues[j + cell] > 0;
                    bool verti = canVerti && _cellValues[i * _width + cell] > 0;

                    cornerMod += ((horiz == verti) ? !horiz : outer) ? 1 : -1;
                }

            Value += 1 - cornerMod / 2;
            CellCount++;
        }

        public void RegisterRect(int topleft, int bottomright)
        {
            int x1 = topleft % _width;
            int y1 = topleft / _width;
            int x2 = bottomright % _width;
            int y2 = bottomright / _width;

            for (int i = y1; i <= y2; i++)
                for (int j = x1; j <= x2; j++)
                    _cellValues[i * _width + j] = 1;

            for (int i = y1; i <= y2; i++)
            {
                if (x1 > 0)
                    _cellValues[i * _width + x1 - 1] = -1;
                if (x2 < _width - 1)
                    _cellValues[i * _width + x2 + 1] = -1;
            }

            for (int i = x1; i <= x2; i++)
            {
                if (y1 > 0)
                    _cellValues[(y1 - 1) * _width + i] = -1;
                if (y2 < _height - 1)
                    _cellValues[(y2 + 1) * _width + i] = -1;
            }

            int newCells = (x2 - x1 + 1) * (y2 - y1 + 1);

            if (newCells == 1)
            {
                Ambitions.Enqueue(topleft);
                //double register if this is the first cell because it has to bridge to be meaningful
                if (CellCount == 0)
                    Ambitions.Enqueue(topleft);
            }

            CellCount += newCells;
            Value += newCells - 1;
        }

        public List<Shape> IterateRect(int baseCorner, bool onlyPositive)
        {
            List<Shape> result = new List<Shape>();

            int x = baseCorner % _width;
            int y = baseCorner / _width;

            if (onlyPositive)
            {
                x--;
                y--;
            }

            for (int i = -1; i < 2; i += 2)
                for (int j = -1; j < 2; j += 2)
                {
                    if (onlyPositive && (j <= 0 || i <= 0))
                        continue;

                    if (y + i < 0 || y + i >= _height || x + j < 0 || x + j >= _width)
                        continue;

                    int maxH = _width;

                    // I know this is a horrible way to use a for loop, deal with it
                    for (int k = 0; maxH > 0; k++)
                    {
                        int newY = y + i * (k + 1);

                        if (newY < 0 || newY >= _height)
                            break;
                        for (int l = 0; l < maxH; l++)
                        {
                            int newX = x + j * (l + 1);
                            if (newX < 0 || newX >= _width || _cellValues[newY * _width + newX] != 0)
                            {
                                maxH = l;
                                break;
                            }

                            Shape clone = new Shape(this);
                            clone.RegisterRect(Math.Min(newY, y + i) * _width + Math.Min(newX, x + j), Math.Max(newY, y + i) * _width + Math.Max(newX, x + j));

                            result.Add(clone);
                        }
                    }

                    if (y + i < 0 || y + i >= _height || x + j < 0 || x + j >= _width)
                        continue;

                    if (_cellValues[(y + i) * _width + x + j] == 0)
                        RegisterCell((y + i) * _width + x + j, true);
                }

            return result;
        }

        //stuff that needs to be handled in order to make the single cell rectangles worth their while; causes a shape to die of uselessness otherwise
        public Queue<int> Ambitions { get; private set; }

        public List<Shape> IterateAllRect()
        {
            List<Shape> result = new List<Shape>();

            if (Ambitions.Count > 0)
            {
                result = IterateRect(Ambitions.Dequeue(), CellCount == 0);
                return result;
            }

            for (int i = 0; i < _width * _height; i++)
                if ((CellCount == 0 && _cellValues[i] >= 0) || _cellValues[i] > 0)
                    result.AddRange(IterateRect(i, CellCount == 0));

            return result;
        }

        public List<Shape> IterateShape(bool[,] grid)
        {
            List<Shape> result = new List<Shape>();

            for (int i = 0; i < _width * _height; i++)
            {
                if (_cellValues[i] != 0)
                    continue;

                int x = i % _width;
                int y = i / _width;

                if (!grid[y, x] && (CellCount == 0 ||
                    (y > 0 && _cellValues[i - _width] == 1) ||
                    (y < _height - 1 && _cellValues[i + _width] == 1) ||
                    (x > 0 && _cellValues[i - 1] == 1) ||
                    (x < _width - 1 && _cellValues[i + 1] == 1)))
                {
                    Shape clone = new Shape(this);
                    clone.RegisterCell(i, false);
                    RegisterCell(i, true);
                    result.Add(clone);
                }
            }

            return result;
        }

        public bool Overlap(Shape other)
        {
            for (int i = 0; i < _cellValues.Length; i++)
                if (_cellValues[i] > 0 && other._cellValues[i] > 0)
                    return true;

            return false;
        }

        public override string ToString()
        {
            return _cellValues.Select(x => x > 0 ? '1' : (x < 0 ? '-' : '0')).Join("") + ";" + Value;
        }
    }


    public int Width { get; private set; }
    public int Height { get; private set; }

    //use true for occupied
    public bool[,] Grid;

    public DriverStoragePuzzle(int width, int height)
    {
        Width = width;
        Height = height;

        Grid = new bool[height, width];
        for (int i = 0; i < Height; i++)
            for (int j = 0; j < Width; j++)
                Grid[i, j] = false;
    }

    public DriverStoragePuzzle(DriverStoragePuzzle old)
    {
        Width = old.Width;
        Height = old.Height;

        Grid = new bool[Height, Width];
        for (int i = 0; i < Height; i++)
            for (int j = 0; j < Width; j++)
                Grid[i, j] = old.Grid[i, j];
    }

    public DriverStoragePuzzle FindPuzzle(List<int> sizes, List<DriverStoragePuzzle> coverage, out List<Shape> pieces)
    {
        DriverStoragePuzzle lastSuccess = null;

        pieces = null;

        while (coverage.Count > 0)
        {
            DriverStoragePuzzle trial = coverage[coverage.Count / 2];
            List<Shape> newPieces = trial.SearchSolutionRect(sizes);
            if (newPieces != null)
            {
                pieces = newPieces;
                lastSuccess = trial;
                coverage = coverage.Skip(coverage.Count / 2 + 1).ToList();
            }
            else
            {
                coverage = coverage.Take(coverage.Count / 2).ToList();
            }
        }

        return lastSuccess;
    }

    public List<DriverStoragePuzzle> GenerateCoverage()
    {
        List<DriverStoragePuzzle> list = new List<DriverStoragePuzzle> { this };

        while (list.Last() != null)
            list.Add(list.Last().GenerateCoverageSingle());

        list.RemoveAt(list.Count - 1);

        return list;
    }

    private DriverStoragePuzzle GenerateCoverageSingle()
    {
        DriverStoragePuzzle newPuzzle = new DriverStoragePuzzle(this);

        List<int> candidates = new List<int>();
        for (int i = 0; i < Width * Height; i++)
        {
            int x = i % Width;
            int y = i / Width;
            if (Grid[y, x])
                continue;
            DriverStoragePuzzle copy1 = new DriverStoragePuzzle(newPuzzle);
            copy1.Grid[y, x] = true;
            DriverStoragePuzzle copy2 = new DriverStoragePuzzle(copy1);
            if (copy1.TryFloodFill() && copy2.TryCountRegions() < 4)
                candidates.Add(i);
        }

        if (candidates.Count == 0)
            return null;

        int pos = candidates.PickRandom();
        int newX = pos % Width;
        int newY = pos / Width;
        newPuzzle.Grid[newY, newX] = true;

        return newPuzzle;
    }

    public List<Shape> SearchSolutionRect(List<int> sizes)
    {
        int meandering = 5;
        int biggest = sizes.Max();

        int availableSpace = Enumerable.Range(0, Width * Height).Count(x => !Grid[x / Width, x % Width]);
        Queue<Shape> evalQueue = new Queue<Shape>();
        evalQueue.Enqueue(new Shape(Width, Height, Grid));
        List<List<Shape>> shapesByValue = new List<List<Shape>>();

        while (evalQueue.Count > 0)
        {
            Shape shape = evalQueue.Dequeue();
            List<Shape> shapeList = shape.IterateAllRect();

            foreach (Shape element in shapeList)
            {
                if (element.CellCount - element.Value > meandering)
                    return null;

                if (element.Value > biggest)
                    continue;

                evalQueue.Enqueue(element);

                if (element.Ambitions.Count > 0)
                    continue;

                while (element.Value >= shapesByValue.Count)
                    shapesByValue.Add(new List<Shape>());

                shapesByValue[element.Value].Add(element);

                if (!sizes.Contains(element.Value) ||
                    sizes.Any(x => x >= shapesByValue.Count ||
                    shapesByValue[x].Count == 0))
                    continue;

                bool cut = false;
                List<int> cutSizes = new List<int>();
                sizes.ForEach(x =>
                {
                    if (cut || x != element.Value)
                        cutSizes.Add(x);
                    else
                        cut = true;
                });

                List<int> iterators = new List<int> { -1 };
                while (iterators.Count > 0)
                {
                    iterators[iterators.Count - 1]++;

                    if (iterators.Last() >= shapesByValue[cutSizes[iterators.Count - 1]].Count)
                    {
                        iterators.RemoveAt(iterators.Count - 1);
                        continue;
                    }

                    Shape newShape = shapesByValue[cutSizes[iterators.Count - 1]][iterators.Last()];

                    if (element.Overlap(newShape))
                        continue;

                    int cellCount = element.CellCount;
                    bool isGood = true;
                    for (int i = 0; i < iterators.Count - 1; i++)
                    {
                        Shape currentShape = shapesByValue[cutSizes[i]][iterators[i]];
                        cellCount += currentShape.CellCount;
                        if (currentShape.Overlap(newShape))
                        {
                            isGood = false;
                            break;
                        }
                    }
                    cellCount += newShape.CellCount;
                    if (!isGood || availableSpace - cellCount < cutSizes.Skip(iterators.Count).Sum())
                        continue;


                    if (iterators.Count == cutSizes.Count)
                    {
                        List<Shape> entries = new List<Shape> { element };
                        for (int i = 0; i < cutSizes.Count; i++)
                            entries.Add(shapesByValue[cutSizes[i]][iterators[i]]);
                        return entries;
                    }

                    iterators.Add(-1);
                }
            }
        }
        return null;
    }

    public bool SearchSolutionNew(List<int> sizes)
    {
        int availableSpace = Enumerable.Range(0, Width * Height).Count(x => !Grid[x / Width, x % Width]);
        Queue<Shape> evalQueue = new Queue<Shape>();
        evalQueue.Enqueue(new Shape(Width, Height));
        List<List<Shape>> shapesByValue = new List<List<Shape>>();

        while (evalQueue.Count > 0)
        {
            Shape shape = evalQueue.Dequeue();
            List<Shape> shapeList = shape.IterateShape(Grid);

            foreach (Shape element in shapeList)
            {
                //int meander = 5;
                //if (element.CellCount - element.Value > meander || element.CellCount > sizes.Max() + meander)
                //    continue;

                while (element.Value >= shapesByValue.Count)
                    shapesByValue.Add(new List<Shape>());
                shapesByValue[element.Value].Add(element);


                evalQueue.Enqueue(element);

                if (element.CellCount + sizes.Sum() - sizes.Max() > availableSpace)
                    return false;

                if (!sizes.Contains(element.Value) ||
                    sizes.Any(x => x >= shapesByValue.Count || shapesByValue[x].Count == 0))
                    continue;

                bool cut = false;
                List<int> cutSizes = new List<int>();
                sizes.ForEach(x =>
                {
                    if (cut || x != element.Value)
                        cutSizes.Add(x);
                    else
                        cut = true;
                });

                int cellSum = element.CellCount;
                List<int> iterators = new List<int> { -1 };
                while (iterators.Count > 0)
                {
                    if (iterators.Last() != -1)
                        cellSum -= shapesByValue[cutSizes[iterators.Count - 1]][iterators.Last()].CellCount;
                    iterators[iterators.Count - 1]++;

                    if (iterators.Last() >= shapesByValue[cutSizes[iterators.Count - 1]].Count)
                    {
                        iterators.RemoveAt(iterators.Count - 1);
                        continue;
                    }

                    Shape newShape = shapesByValue[cutSizes[iterators.Count - 1]][iterators.Last()];
                    cellSum += newShape.CellCount;

                    if (cellSum + cutSizes.Sum() > availableSpace)
                        continue;

                    if (element.Overlap(newShape))
                        continue;

                    bool isGood = true;
                    for (int i = 0; i < iterators.Count - 1; i++)
                        if (shapesByValue[cutSizes[i]][iterators[i]].Overlap(newShape))
                        {
                            isGood = false;
                            break;
                        }
                    if (!isGood)
                        continue;

                    if (iterators.Count == cutSizes.Count)
                    {
                        List<Shape> entries = new List<Shape> { element };
                        for (int i = 0; i < cutSizes.Count; i++)
                            entries.Add(shapesByValue[cutSizes[i]][iterators[i]]);
                        return true;
                    }

                    iterators.Add(-1);
                }
            }
        }

        return false;
    }

    public int CountFittingMethods(List<int> sizes)
    {
        int availableSpace = Enumerable.Range(0, Width * Height).Count(x => !Grid[x / Width, x % Width]);
        int count = 0;
        Stack<Stack<int>> shapes = new Stack<Stack<int>>();
        shapes.Push(new Stack<int>());

        while (shapes.Count > 0)
        {
            Stack<int> newShape = shapes.Pop();
            newShape = FindNextFitting(newShape, sizes[shapes.Count], availableSpace - shapes.Sum(x => x.Count(y => y >= 0)));
            if (newShape == null)
                continue;
            shapes.Push(newShape);

            if (shapes.Count < sizes.Count)
            {
                List<int> vetoes = new List<int>();
                foreach (Stack<int> shape in shapes)
                    vetoes.AddRange(shape.Where(x => x >= 0).Select(x => -1 - x));
                shapes.Push(new Stack<int>(vetoes));
                continue;
            }
            return 1;
        }

        return count;
    }


    public Stack<int> FindNextFitting(Stack<int> shape, int size, int maxCells)
    {
        while (IterateNextFitting(shape, size, maxCells))
            if (GetSize(shape) == size)
                return shape;

        return null;
    }

    //returns whether there is a next step to execute, the stack passed into the method will be mutated accordingly if there is
    public bool IterateNextFitting(Stack<int> shape, int size, int maxCells)
    {
        //-1 - value for vetoing spaces

        int cellCount = shape.Count(x => x >= 0);

        if (cellCount < maxCells && GetSize(shape) != size)
            for (int i = 0; i < Width * Height; i++)
            {
                if (shape.Contains(i) || shape.Contains(-1 - i))
                    continue;

                int x = i % Width;
                int y = i / Width;

                if (!Grid[y, x] && (cellCount == 0 ||
                    (y > 0 && shape.Contains(i - Width)) ||
                    (y < Height - 1 && shape.Contains(i + Width)) ||
                    (x > 0 && shape.Contains(i - 1)) ||
                    (x < Width - 1 && shape.Contains(i + 1))))
                {
                    shape.Push(i);
                    return true;
                }
            }

        //only reaches here if adding directly is impossible or meaningless
        int lastPop = 0;
        while (true)
        {
            if (shape.Count == 0)
                return false;

            lastPop = shape.Pop();
            if (lastPop >= 0)
            {
                shape.Push(-1 - lastPop);
                return true;
            }
        }
    }

    public int GetSize(Stack<int> shape)
    {
        //normalise to rectangles
        int cornerCount = -4;

        int cellCount = shape.Count(x => x >= 0);

        //size 0 shouldn't get reported as size 2 due to missing corners
        if (cellCount == 0)
            return 0;

        bool[] shapeParts = new bool[Width * Height];
        foreach (int cell in shape)
            if (cell >= 0)
                shapeParts[cell] = true;

        for (int i = -1; i < Height; i++)
            for (int j = -1; j < Width; j++)
            {
                bool tl = i >= 0 && j >= 0 && shapeParts[i * Width + j];
                bool tr = i >= 0 && j < Width - 1 && shapeParts[i * Width + j + 1];
                bool bl = i < Height - 1 && j >= 0 && shapeParts[(i + 1) * Width + j];
                bool br = i < Height - 1 && j < Width - 1 && shapeParts[(i + 1) * Width + j + 1];

                if (tl != tr || tl != bl || tl != br)
                    cornerCount += (tl == br ? 1 : 0) + (tr == bl ? 1 : 0);
            }

        return cellCount - cornerCount / 2;
    }



    //note: this mutates the grid and fills it (partially)
    private bool TryFloodFill()
    {
        Queue<int> floodfillable = new Queue<int>();
        for (int i = 0; i < Width * Height; i++)
        {
            int x = i % Width;
            int y = i / Width;
            if (!Grid[y, x])
            {
                floodfillable.Enqueue(i);
                break;
            }
        }

        while (floodfillable.Count > 0)
        {
            int pos = floodfillable.Dequeue();
            int x = pos % Width;
            int y = pos / Width;

            Grid[y, x] = true;
            if (y > 0 && !Grid[y - 1, x])
                floodfillable.Enqueue(pos - Width);
            if (y < Height - 1 && !Grid[y + 1, x])
                floodfillable.Enqueue(pos + Width);
            if (x > 0 && !Grid[y, x - 1])
                floodfillable.Enqueue(pos - 1);
            if (x < Width - 1 && !Grid[y, x + 1])
                floodfillable.Enqueue(pos + 1);
        }

        for (int i = 0; i < Width * Height; i++)
        {
            int x = i % Width;
            int y = i / Width;
            if (!Grid[y, x])
                return false;
        }
        return true;
    }

    //note: this mutates the grid and clears it
    private int TryCountRegions()
    {
        int regions = 0;
        int iter = 0;

        while (true)
        {
            Queue<int> floodfillable = new Queue<int>();
            for (; iter < Width * Height; iter++)
            {
                int x = iter % Width;
                int y = iter / Width;
                if (Grid[y, x])
                {
                    regions++;
                    floodfillable.Enqueue(iter);
                    break;
                }
            }

            if (floodfillable.Count == 0)
                return regions;

            while (floodfillable.Count > 0)
            {
                int pos = floodfillable.Dequeue();
                int x = pos % Width;
                int y = pos / Width;

                Grid[y, x] = false;
                if (y > 0 && Grid[y - 1, x])
                    floodfillable.Enqueue(pos - Width);
                if (y < Height - 1 && Grid[y + 1, x])
                    floodfillable.Enqueue(pos + Width);
                if (x > 0 && Grid[y, x - 1])
                    floodfillable.Enqueue(pos - 1);
                if (x < Width - 1 && Grid[y, x + 1])
                    floodfillable.Enqueue(pos + 1);
            }
        }
    }
}
