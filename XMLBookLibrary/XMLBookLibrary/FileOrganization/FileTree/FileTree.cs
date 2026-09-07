using System.Diagnostics;
using XMLBookLibrary.Configs;
using XMLBookLibrary.Models.Constants;
using XMLBookLibrary.Models.InternalModels;

namespace XMLBookLibrary.XmlDataManagement.FileOrganization.FileTree;

internal class FileTree
{
    private FileTree? Left { get; set; }
    private FileTree? Right { get; set; }
    private FileTree? Parent { get; set; }
    private FileTree? Child { get; set; }
    private int Index { get; set; }
    private char Symbol { get; set; }
    internal string FilePath { get; private set; }

    public FileTree()
    {
        
    }

    public FileTree GetInitial()
    {
        if (Left is null)
        {
            return this;
        }

        return Left.GetInitial();
    }
    public FileTree(int  index, char symbol, string fileName)
    {
        Index = index;
        Symbol = symbol;
        FilePath = fileName;
    }

    private FileTree? InitialAdd(List<char> from, string file)
    {
        char targetSymbol = from[Index];

        if (targetSymbol == Symbol)
        {
            if (from.Count < Index + 2) return this;
            Child ??= new FileTree()
            {
                Parent = this,
                Index = Index + 1,
                Symbol = from[Index + 1],
                FilePath = file
            };
            return Child.InitialAdd(from, file);
        }

        if (targetSymbol > Symbol)
        {
            Right ??= new FileTree()
            {
                Parent = Parent,
                Left = this,
                Index = Index,
                Symbol = from[Index],
                FilePath = file
            };

            return Right.InitialAdd(from, file);
        }

        if (Left is null)
        {
            Left = new FileTree()
            {
                Right = this,
                Parent = Parent,
                Index = Index,
                Symbol = from[Index],
                FilePath = file
            };

            if (Parent != null)
            {
                Parent!.Child = Left;
            }

            return Left.InitialAdd(from, file);
        }

        var tNode = new FileTree()
        {
            Parent = Parent,
            Left = Left,
            Right = this,
            Index = Index,
            Symbol = from[Index],
            FilePath = file
        };

        if (Parent != null)
        {
            Parent.Child = tNode;
        }

        Left.Right = tNode;
        Left = tNode;
        return Left.InitialAdd(from, file);
    }

    private void SetLinks(FileTree tree, List<char> to, string file)
    {
        while (true)
        {
            if (tree!.Index + 1 <= to.Count
                && tree.Symbol >= to[tree.Index])
            {
                break;
            }

            tree.FilePath = file;

            if (tree.Right is null)
            {
                if (tree.Parent is null)
                {
                    break;
                }

                tree = tree.Parent;
                continue;
            }

            tree = tree.Right;
        }
    }

    internal bool Add(FileDataContainer data)
    {
        var node = InitialAdd(data.From, data.File);

        if (node is null)
        {
            return false;
        }

        SetLinks(node, data.To, data.File);
        return true;
    }

    internal FileTree Add(string title)
    {
        var currentNode = this;
        FileTree? result = null;

        while (true)
        {
            if (title.Length < currentNode.Index + 2)
            {
                return result ?? currentNode;
            }

            if (title[currentNode.Index] > currentNode.Symbol)
            {
                currentNode.Right ??= new FileTree()
                {
                    Parent = currentNode.Parent,
                    Left = currentNode,
                    Index = currentNode.Index,
                    Symbol = title[currentNode.Index],
                    FilePath = currentNode.FilePath,
                };

                result = currentNode;
                currentNode = currentNode.Right;
                continue;
            }

            if (title[currentNode.Index] < currentNode.Symbol)
            {
                var node = new FileTree()
                {
                    Left = currentNode.Left,
                    Right = currentNode,
                    Parent = currentNode.Parent,
                    Index = currentNode.Index,
                    Symbol = title[currentNode.Index],
                    FilePath = currentNode.FilePath,
                };

                currentNode.Parent?.Child = node;
                result = currentNode;
                currentNode.Left?.Right = node;
                currentNode.Left = node;
                currentNode = node;
                continue;
            }

            currentNode.Child ??= new FileTree()
            {
                Parent = currentNode,
                Index = currentNode.Index + 1,
                Symbol = title[currentNode.Index + 1],
                FilePath = currentNode.FilePath,
            };

            currentNode = currentNode.Child;
        }
    }

    internal void Update(FileDataContainer data)
    {
        var node = InitialAdd(data.From, data.File);

        if (node is null)
        {
            return;
        }

        node.FilePath = data.File;
        SetLinks(node, data.To, data.File);
    }

    internal string GetFile(string title)
    {
        var currentNode = this;
        bool isCatched = false;

        while (true)
        {
            if (isCatched
                || title.Length < currentNode!.Index + 2)
            {
                if (currentNode!.Child is null)
                {
                    return currentNode.FilePath;
                }

                currentNode = currentNode.Child;
                continue;
            }

            if (title[currentNode.Index] > currentNode.Symbol)
            {
                if (currentNode.Right is null)
                {
                    if (currentNode.Child != null)
                    {
                        currentNode = currentNode.Child;
                        isCatched = true;
                        continue;
                    }

                    return currentNode.FilePath;
                }
            }

            if (title[currentNode.Index] < currentNode.Symbol
                && currentNode.Child != null)
            {
                return currentNode.FilePath;
            }

            currentNode = currentNode.Child;
            isCatched = true;
        }
    }

    internal List<FileDataContainer> GetData()
    {
        var result = new List<FileDataContainer>();
        List<char> word = new List<char>();
        var currentNode = this;
        bool isInitial = true;

        while (true)
        {
            word.Add(currentNode.Symbol);
            if (isInitial && currentNode.Child != null)
            {
                currentNode = currentNode.Child;
                continue;
            }

            if (isInitial)
            {
                result.Add(new FileDataContainer
                {
                    From = word,
                    File = currentNode.FilePath,
                });
                isInitial = false;
                word = new List<char>(word.ToArray());
                result.Last().To = word;
            }
            else
            {
                var lastFile = result.Last();

                if (lastFile.File != currentNode.FilePath)
                {
                    word = new List<char>(word.ToArray());
                    isInitial = true;
                    continue;
                }
            }

            while (currentNode.Right is null)
            {
                if (currentNode.Parent is null)
                {
                    break;
                }

                word.RemoveAt(word.Count - 1);
                currentNode = currentNode.Parent;
            }

            if (currentNode.Parent is null)
            {
                break;
            }

            word.RemoveAt(word.Count - 1);
            currentNode = currentNode.Right;
        }

        return result;
    }

    public void SetFilePath(string path)
    {
        FilePath = path;
        var currentNode = this;
        while (currentNode.Child != null)
        {
            currentNode = currentNode.Child;
            currentNode.FilePath = path;
        }
    }

    private FileTree UpdateNodeLink(string newPath)
    {
        var currentNode = this;
        var checkpointNode = this;

        while (true)
        {
            currentNode.FilePath = newPath;

            if (currentNode.Right is null)
            {
                if (checkpointNode.Child is null)
                {
                    return checkpointNode;
                }

                checkpointNode = checkpointNode.Child;
                currentNode = checkpointNode;
                continue;
            }

            currentNode = currentNode.Right;

            currentNode.Child?.UpdateNodeLink(newPath);
        }
    }

    public FileTree Split(string newFilePath)
    {
        var currentNode = this;
        while (currentNode.Parent != null
               && currentNode.Parent.FilePath == FilePath)
        {
            currentNode = currentNode.Parent;
        }

        int leftOffset = 0;
        var tNode = currentNode;

        while (tNode.Left != null
               && tNode.Left.FilePath == FilePath)
        {
            tNode = tNode.Left;
            ++leftOffset;
        }

        int rightOffset = 0;
        tNode = currentNode;

        while (tNode.Right != null
               && tNode.Right.FilePath == FilePath)
        {
            tNode = tNode.Right;
            ++rightOffset;
        }

        var step = (leftOffset + rightOffset + 1) / 2;

        if (leftOffset > step)
        {
            step -= leftOffset;
            leftOffset = 0;
        }
        else
        {
            leftOffset -= step;
            step = 0;
        }

        if (step > 0)
        {
            rightOffset -= step - 1;
        }

        tNode = currentNode;
        FileTree? result = null;
        for (int i = 0; i < leftOffset; ++i)
        {
            tNode = tNode.Left;
            tNode!.FilePath = newFilePath;

            result = tNode.Child?.UpdateNodeLink(newFilePath);
        }

        if (step > 0)
        {
            currentNode.FilePath = newFilePath;
            currentNode.Child?.UpdateNodeLink(newFilePath);
            result ??= currentNode;
        }

        tNode = currentNode;

        for (int i = 0; i < rightOffset; ++i)
        {
            tNode = tNode.Right;
            tNode!.FilePath = newFilePath;

            result ??= tNode.Child?.UpdateNodeLink(newFilePath);
        }

        return result!;
    }

    public List<char> GetName()
    {
        var currentNode = this;
        var result = new List<char>();
        do
        {
            result.Insert(0, currentNode.Symbol);
        } while (currentNode.Parent != null);
        
        return result;
    }
}