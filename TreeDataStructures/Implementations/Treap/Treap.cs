using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Разрезает дерево с корнем <paramref name="root"/> на два поддерева:
    /// Left: все ключи <= <paramref name="key"/>
    /// Right: все ключи > <paramref name="key"/>
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null)
        {
            return (null, null);
        }
        int cmp = Comparer.Compare(key, root.Key);
        if (cmp < 0)
        {
            var (leftsub, rightsub) = Split(root.Left, key);
            root.Left = rightsub;
            if (rightsub != null)
            {
                rightsub.Parent = root;
            }
            return (leftsub, root);
        } else
        {
            var (leftsub, rightsub) = Split(root.Right, key);
            root.Right = leftsub;
            if (leftsub != null)
            {
                leftsub.Parent = root;
            }
            return (root, rightsub);
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Важное условие: все ключи в <paramref name="left"/> должны быть меньше ключей в <paramref name="right"/>.
    /// Слияние происходит на основе Priority (куча).
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left == null)
        {
            return right;
        }
        if (right == null)
        {
            return left;
        }
        if (left.Priority > right.Priority)
        {
            var new_right = Merge(left.Right, right);
            left.Right = new_right;
            if (new_right != null)
            {
                new_right.Parent = left;
            }
            return left;
        } else
        {
            var new_left = Merge(left, right.Left);
            right.Left = new_left;
            if (new_left != null)
            {
                new_left.Parent = right;
            }
            return right;
        }
    }
    

    public override void Add(TKey key, TValue value)
    {
        var existing = FindNode(key);
        if (existing != null)
        {
            existing.Value = value;
            return;
        }
        var (left, right) = Split(Root, key);
        var new_node = CreateNode(key, value);
        var new_tree = Merge(Merge(left, new_node), right);
        Root = new_tree;
        if (Root != null)
        {
            Root.Parent = null;
        }
        Count++;
        OnNodeAdded(new_node);
    }

    public override bool Remove(TKey key)
    {
        var node = FindNode(key);
        if (node == null)
        {
            return false;
        }
        var joined_children = Merge(node.Left, node.Right);
        Transplant(node, joined_children);
        Count--;
        OnNodeRemoved(node.Parent, joined_children);
        return true;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new TreapNode<TKey, TValue>(key, value);
    }
    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode)
    {

    }
    
    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child)
    {

    }
    
}