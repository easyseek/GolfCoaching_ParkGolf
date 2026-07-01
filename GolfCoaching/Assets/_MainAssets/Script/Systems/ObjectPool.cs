using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : Component
{
    private readonly Queue<T> _objects = new Queue<T>();
    private readonly T _prefab;
    private readonly Transform _parent;

    public ObjectPool(T prefab, Transform parent, int initialSize = 10)
    {
        _prefab = prefab;
        _parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            T obj = GameObject.Instantiate(_prefab, _parent);

            obj.gameObject.SetActive(false);

            _objects.Enqueue(obj);
        }
    }

    public T Get()
    {
        if (_objects.Count > 0)
        {
            T obj = _objects.Dequeue();

            obj.gameObject.SetActive(true);

            return obj;
        }

        return GameObject.Instantiate(_prefab, _parent);
    }

    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);

        _objects.Enqueue(obj);
    }

    public void ReturnAll(List<T> activeList)
    {
        foreach (var obj in activeList)
        {
            Return(obj);
        }

        activeList.Clear();
    }
}
