using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        var node = new  RbNode<TKey, TValue>(key, value);
        node.Color = RbColor.Red;
        return node;
    }

    private static RbColor GetColor(RbNode<TKey, TValue>? node)
    {
        return node?.Color ?? RbColor.Black;
    }

    private static void SetColor(RbNode<TKey, TValue>? node, RbColor color)
    {
        if (node != null)
        {
            node?.Color = color;
        }
    }

    private static bool IsRed(RbNode<TKey, TValue>? node)
    {
        return GetColor(node) == RbColor.Red;
    }
    
    private static bool IsBlack(RbNode<TKey, TValue>? node)
    {
        return GetColor(node) == RbColor.Black;
    }

    private void FixInsert(RbNode<TKey, TValue>? node)
    {
        while (IsRed(node?.Parent) && node != Root)
        {
           if (node?.Parent == node?.Parent?.Parent?.Left)
            {
                var y = node?.Parent?.Parent?.Right;
                if (IsRed(y))
                {
                    SetColor(node?.Parent, RbColor.Black);
                    SetColor(y, RbColor.Black);
                    SetColor(node?.Parent?.Parent, RbColor.Red);
                    node = node?.Parent?.Parent;
                } else
                {
                    if (node == node?.Parent?.Right)
                    {
                        node = node?.Parent;
                        base.RotateLeft(node!);
                    }
                    SetColor(node?.Parent, RbColor.Black);
                    SetColor(node?.Parent?.Parent, RbColor.Red);
                    base.RotateRight(node?.Parent?.Parent!);
                }
            } else if (node?.Parent == node?.Parent?.Parent?.Right)
            {
                var y = node?.Parent?.Parent?.Left;
                if (IsRed(y))
                {
                    SetColor(node?.Parent, RbColor.Black);
                    SetColor(y, RbColor.Black);
                    SetColor(node?.Parent?.Parent, RbColor.Red);
                    node = node?.Parent?.Parent;
                } else
                {
                    if (node == node?.Parent?.Left)
                    {
                        node = node?.Parent;
                        base.RotateRight(node!);
                    }
                    SetColor(node?.Parent, RbColor.Black);
                    SetColor(node?.Parent?.Parent, RbColor.Red);
                    base.RotateLeft(node?.Parent?.Parent!);
                }
            }
        }
        SetColor(Root, RbColor.Black);
    }

    private void FixDelete(RbNode<TKey, TValue>? node, RbNode<TKey, TValue>? parent)
    {
        while (IsBlack(node) && node != Root) 
        {
            if (node == parent?.Left)
            {
                var sibling = parent?.Right;
                if (IsRed(sibling))
                {
                    SetColor(sibling, RbColor.Black);
                    SetColor(parent, RbColor.Red);
                    base.RotateLeft(parent!);
                    sibling = parent?.Right;
                }
                if (IsBlack(sibling?.Left) && IsBlack(sibling?.Right))
                {
                    SetColor(sibling, RbColor.Red);
                    node = parent;
                    parent = node?.Parent;
                } else if (IsBlack(sibling?.Right))
                {
                    SetColor(sibling?.Left, RbColor.Black);
                    SetColor(sibling, RbColor.Red);
                    base.RotateRight(sibling!);
                    sibling = parent?.Right;
                } else
                {
                    SetColor(sibling, GetColor(parent));
                    SetColor(parent, RbColor.Black);
                    SetColor(sibling?.Right, RbColor.Black);
                    base.RotateLeft(parent!);
                    node = Root;
                }
            } else if (node == parent?.Right)
            {
                var sibling = parent?.Left;
                if (IsRed(sibling))
                {
                    SetColor(sibling, RbColor.Black);
                    SetColor(parent, RbColor.Red);
                    base.RotateRight(parent!);
                    sibling = parent?.Left;
                }
                if (IsBlack(sibling?.Left) && IsBlack(sibling?.Right))
                {
                    SetColor(sibling, RbColor.Red);
                    node = parent;
                    parent = node?.Parent;
                } else if (IsBlack(sibling?.Left))
                {
                    SetColor(sibling?.Right, RbColor.Black);
                    SetColor(sibling, RbColor.Red);
                    base.RotateLeft(sibling!);
                    sibling = parent?.Left;
                } else
                {
                    SetColor(sibling, GetColor(parent));
                    SetColor(parent, RbColor.Black);
                    SetColor(sibling?.Left, RbColor.Black);
                    base.RotateRight(parent!);
                    node = Root;
                }
            }
        }
        SetColor(node, RbColor.Black);
    }

    protected override void RemoveNode(RbNode<TKey, TValue> node)
    {
        RbNode<TKey, TValue>? replacement;
        RbNode<TKey, TValue>? child;
        RbNode<TKey, TValue>? parent;
        RbColor original_color = node.Color;
        if (node.Left == null)
        {
            replacement = node.Right;
            parent = node.Parent;
            child = node.Right;
            Transplant(node, node.Right);
        } else if (node.Right == null)
        {
            replacement = node.Left;
            parent = node.Parent;
            child = node.Left;
            Transplant(node, node.Left);
        } else
        {
            replacement = node.Right;
            while (replacement.Left != null)
            {
                replacement = replacement.Left;
            }
            original_color = replacement.Color;
            child = replacement.Right;
            parent = replacement.Parent;
            if (replacement.Parent == node)
            {
                parent = replacement;
                if (child != null)
                {
                    child.Parent = replacement;
                }
            } else
            {
                Transplant(replacement, replacement.Right);
                replacement.Right = node.Right;
                replacement.Right.Parent = replacement;
                parent = replacement.Parent;
            }
            Transplant(node, replacement);
            replacement.Left = node.Left;
            replacement.Left.Parent = replacement;
            replacement.Color = node.Color;
        }
        if (original_color == RbColor.Black)
        {
            FixDelete(child, parent);
        }
        OnNodeRemoved(parent, child);
    }

    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        FixInsert(newNode);
    }
    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
        
    }
}