using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);


    private void Splay(BstNode<TKey, TValue>? node)
    {
        if (node == null)
        {
            return;
        }
        while (node.Parent != null)
        {
            var parent = node.Parent;
            var grandparent = parent.Parent;
            if (grandparent == null)
            {
                if (node.IsLeftChild)
                {
                    base.RotateRight(parent);
                } else
                {
                    base.RotateLeft(parent);
                }
            } else if (node.IsLeftChild && parent.IsLeftChild)
            {
                base.RotateRight(grandparent);
                base.RotateRight(parent);
            } else if (node.IsRightChild && parent.IsRightChild)
            {
                base.RotateLeft(grandparent);
                base.RotateLeft(parent);
            } else if (node.IsRightChild && parent.IsLeftChild)
            {
                base.RotateLeft(parent);
                base.RotateRight(grandparent);
            } else if (node.IsLeftChild && parent.IsRightChild)
            {
                base.RotateRight(parent);
                base.RotateLeft(grandparent);
            }
        }
        Root = node;
        node.Parent = null;
    }
    
    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }
    
    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
        if (parent != null)
        {
            Splay(parent);
        } else if (child != null)
        {
            Splay(child);
        }
    }

    public override bool ContainsKey(TKey key)
    {
        var node = FindNode(key);
        if (node != null)
        {
            Splay(node);
            return true;
        }
        return false;
    }
    
    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            Splay(node);
            return true;
        }
        if (Root != null)
        {
            var current = Root;
            while (current != null)
            {
                int cmp = Comparer.Compare(key, current.Key);
                if (cmp < 0)
                {
                    if (current.Left == null)
                    {
                        break;
                    }
                    current = current.Left;
                } else if (cmp > 0)
                {
                    if (current.Right == null)
                    {
                        break;
                    }
                    current = current.Right;
                } else
                {
                    break;
                }
            }
            Splay(current);
        }
        value = default;
        return false;
    }
}
