using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    private int get_height(AvlNode<TKey, TValue>? node)
    {
        return node?.Height ?? 0;
    }

    private void update_height(AvlNode<TKey, TValue>? node)
    {
        if (node != null)
        {
            node.Height = Math.Max(get_height(node.Left), get_height(node.Right)) + 1;
        }
    }

    private int get_balance_factor(AvlNode<TKey, TValue>? node)
    {
        if (node == null)
        {
            return 0;
        }
        return get_height(node?.Left) - get_height(node?.Right);
    }

    private AvlNode<TKey, TValue>? Balance(AvlNode<TKey, TValue>? node)
    {
        if (node == null)
        {
            return null;
        }
        update_height(node);
        int balance = get_balance_factor(node);
        if (balance > 1)
        {
            if (get_balance_factor(node.Left) < 0)
            {
                var b = node.Left;
                base.RotateLeft(b!);
                update_height(b);
                update_height(node.Left);
            }
            var new_root = node.Left;
            base.RotateRight(node);
            update_height(node);
            if (new_root != null)
            {
                update_height(new_root);
            }
            return new_root;
        } else if (balance < -1)
        {
            if (get_balance_factor(node.Right) > 0)
            {
                var b = node.Right;
                base.RotateRight(b!);
                update_height(b);
                update_height(node.Right);
            }
            var new_root = node.Right;
            base.RotateLeft(node);
            update_height(node);
            if (new_root != null)
            {
                update_height(new_root);
            }
            return new_root;
        }
        return node;
    }
    
    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        var current = newNode;
        int safety_counter = 0;
        while (current != null)
        {
            if (safety_counter++ > 100)
            {
                throw new Exception($"Бесконечный цикл на узле: {current.Key}");
            }
            update_height(current);
            int balance = get_balance_factor(current);
            if (Math.Abs(balance) > 1)
            {
                AvlNode<TKey, TValue>? new_root = Balance(current);
                current = new_root?.Parent;
            } else
            {
                current = current.Parent;
            }
        }
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        var current = parent ?? child;
        int safety_counter = 0;
        while (current != null)
        {
            if (safety_counter++ > 100)
            {
                throw new Exception($"Бесконечный цикл на узле: {current.Key}");
            }
            update_height(current);
            int balance = get_balance_factor(current);
            if (Math.Abs(balance) > 1)
            {
                AvlNode<TKey, TValue>? new_root = Balance(current);
                current = new_root?.Parent;
            } else
            {
                current = current.Parent;
            }
        }
    }

    protected override void RemoveNode(AvlNode<TKey, TValue> node)
    {
        base.RemoveNode(node);
    }
}
