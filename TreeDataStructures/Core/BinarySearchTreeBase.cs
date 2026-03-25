using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys
    {
        get
        {
            var keys = new List<TKey>();
            var iterator = new TreeIterator(Root, TraversalStrategy.InOrder);
            while (iterator.MoveNext())
            {
                keys.Add(iterator.Current.Key);
            } 
            return keys;
        }
    }
    public ICollection<TValue> Values
    {
        get
        {
            var values = new List<TValue>();
            var iterator = new TreeIterator(Root, TraversalStrategy.InOrder);
            while (iterator.MoveNext())
            {
                values.Add(iterator.Current.Value);
            }
            return values;
        }
    }
    
    
    public virtual void Add(TKey key, TValue value)
    {
        TNode newnode = CreateNode(key, value);
        if (Root == null)
        {
            Root = newnode;
            Count++;
            OnNodeAdded(newnode);
            return;
        }
        TNode? current = Root;
        TNode? parent = null;
        int cmp = 0;
        while (current != null)
        {
            parent = current;
            cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0)
            {
                current.Value = value;
                return;
            }
            current = cmp < 0 ? current.Left : current.Right;
        }
        newnode.Parent = parent;
        if (cmp < 0)
        {
            parent!.Left = newnode;
        } else
        {
            parent!.Right = newnode;
        }
        Count++;
        OnNodeAdded(newnode);
    }

    
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }
    
    
    protected virtual void RemoveNode(TNode node)
    {
        TNode? parent = node.Parent;
        TNode? child = null;
        if (node.Left == null)
        {
            child = node.Right;
            Transplant(node, node.Right);
        } else if (node.Right == null)
        {
            child = node.Left;
            Transplant(node, node.Left);
        } else
        {
            TNode successor = node.Right;
            while (successor.Left != null)
            {
                successor = successor.Left;
            }
            if (successor.Parent != node)
            {
                Transplant(successor, successor.Right);
                successor.Right = node.Right;
                successor.Right.Parent = successor;
            }
            Transplant(node, successor);
            successor.Left = node.Left;
            successor.Left.Parent = successor;
            child = successor;
        }
        OnNodeRemoved(parent, child);
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }
    
    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }
    
    #endregion
    
    
    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);
    
    
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x)
    {
        if (x == null || x.Right == null)
        {
            return;
        } 
        TNode right = x.Right;
        x.Right = right.Left;
        if (right.Left != null)
        {
            right.Left.Parent = x;
        }
        Transplant(x, right);
        right.Left = x;
        x.Parent = right;
    }

    protected void RotateRight(TNode y)
    {
        if (y == null || y.Left == null)
        {
            return;
        }
        TNode left = y.Left;
        y.Left = left.Right;
        if (left.Right != null)
        {
            left.Right.Parent = y;
        }
        Transplant(y, left);
        left.Right = y;
        y.Parent = left;
    }
    
    protected void RotateBigLeft(TNode x)
    {
        RotateRight(x.Right!);
        RotateLeft(x);
    }
    
    protected void RotateBigRight(TNode y)
    {
        RotateLeft(y.Left!);
        RotateRight(y);
    }
    
    protected void RotateDoubleLeft(TNode x)
    {
        RotateLeft(x);
        RotateLeft(x);
    }   
    protected void RotateDoubleRight(TNode y)
    {
        RotateRight(y);
        RotateRight(y);
    }
    
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion
    
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrder()
    {
        return new TreeIterator(Root, TraversalStrategy.InOrder);
    }
    
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrder()
    {
        return new TreeIterator(Root, TraversalStrategy.PreOrder);
    }
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrder()
    {
        return new TreeIterator(Root, TraversalStrategy.PostOrder);
    }
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrderReverse()
    {
        return new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    }
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrderReverse()
    {
        return new TreeIterator(Root, TraversalStrategy.PreOrderReverse);
    }
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrderReverse()
    {
        return new TreeIterator(Root, TraversalStrategy.PostOrderReverse);
    }
    
    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private struct TreeIterator : 
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        // probably add something here
        private readonly TNode? _root;
        private readonly TraversalStrategy _strategy; // or make it template parameter?
        private Stack<(TNode? Node, bool Visited)> _stack;
        private TNode? _currentNode;

        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            _stack = new Stack<(TNode?, bool)>();
            _currentNode = null;
            Reset();
        }

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        
        public TreeEntry<TKey, TValue> Current {
            get
            {
                if (_currentNode == null)
                {
                    throw new InvalidOperationException();
                }
                return new TreeEntry<TKey, TValue>(_currentNode.Key, _currentNode.Value, Height(_currentNode));
            }
        }
        object IEnumerator.Current => Current;
        
        
        public bool MoveNext()
        {
            while (_stack.Count > 0)
            {
                var (node, visited) = _stack.Pop();
                if (node == null)
                {
                    continue;
                }

                switch (_strategy)
                {
                    case TraversalStrategy.PreOrder:
                        if (!visited)
                        {
                            _stack.Push((node.Right!, false));
                            _stack.Push((node.Left!, false));
                            _currentNode = node;
                            return true;
                        }
                        break;
                    case TraversalStrategy.InOrder:
                        if (!visited)
                        {
                            _stack.Push((node.Right!, false));
                            _stack.Push((node, true));
                            _stack.Push((node.Left!, false));
                        } else
                        {
                            _currentNode = node;
                            return true;
                        }
                        break;
                    case TraversalStrategy.PostOrder:
                        if (!visited)
                        {
                            _stack.Push((node, true));
                            _stack.Push((node.Right!, false));
                            _stack.Push((node.Left!, false));
                        } else
                        {
                            _currentNode = node;
                            return true;
                        }
                        break;
                    case TraversalStrategy.PreOrderReverse:
                        if (!visited)
                        {
                            _stack.Push((node, true));
                            _stack.Push((node.Left!, false));
                            _stack.Push((node.Right!, false));
                        } else
                        {
                            _currentNode = node;
                            return true;
                        }
                        break;
                    case TraversalStrategy.InOrderReverse:
                        if (!visited)
                        {
                            _stack.Push((node.Left!, false));
                            _stack.Push((node, true));
                            _stack.Push((node.Right!, false));
                        } else
                        {
                            _currentNode = node;
                            return true;
                        }
                        break;
                    case TraversalStrategy.PostOrderReverse:
                        if (!visited)
                        {
                            _stack.Push((node.Left!, false));
                            _stack.Push((node.Right!, false));
                            _currentNode = node;
                            return true;
                        }
                        break;
                }
            }
            _currentNode = null;
            return false;
        }
        
        public void Reset()
        {
            _stack.Clear();
            _currentNode = null;
            if (_root == null)
            {
              return;  
            }
            switch (_strategy)
            {
                case TraversalStrategy.PreOrder:
                case TraversalStrategy.InOrder:
                case TraversalStrategy.PostOrder:
                case TraversalStrategy.PreOrderReverse:
                case TraversalStrategy.InOrderReverse:
                case TraversalStrategy.PostOrderReverse:
                    _stack.Push((_root, false));
                    break;
            }
        }

        
        public void Dispose()
        {
            // TODO release managed resources here
        }

        public int Height(TNode node)
        {
            return 0;
        }
    }

    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return new EnumeratorWrapper(Root);
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private class EnumeratorWrapper : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private TreeIterator _iterator;

        public EnumeratorWrapper(TNode? root)
        {
            _iterator = new TreeIterator(root, TraversalStrategy.InOrder);
        }
        public KeyValuePair<TKey, TValue> Current
        {
            get
            {
                var entry = _iterator.Current;
                return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
            }
        }
        object IEnumerator.Current
        {
            get
            {
                return this.Current;
            }
        }
        public bool MoveNext()
        {
            return _iterator.MoveNext();
        }

        public void Reset()
        {
            _iterator.Reset();
        }

        public void Dispose()
        {
            _iterator.Dispose();
        }
    }


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }
        if (arrayIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }
        if (array.Length - arrayIndex < Count)
        {
            throw new ArgumentException("Insufficient Space");
        }
        int i = arrayIndex;
        var iterator = new TreeIterator(Root, TraversalStrategy.InOrder);
        while (iterator.MoveNext())
        {
            var entry = iterator.Current;
            array[i++] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}