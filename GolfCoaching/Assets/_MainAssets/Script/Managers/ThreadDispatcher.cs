using System;
using System.Collections.Generic;
using UnityEngine;

public class ThreadDispatcher : MonoBehaviourSingleton<ThreadDispatcher>
{
    private Queue<Action> _jobs = new Queue<Action>();

    public void Enqueue(Action action)
    {
        lock (_jobs)
        {
            _jobs.Enqueue(action);
        }
    }

    void Update()
    {
        // 매 프레임 메인 스레드에서 대기 중인 작업 실행
        lock (_jobs)
        {
            while (_jobs.Count > 0)
            {
                var job = _jobs.Dequeue();
                job.Invoke();
            }
        }
    }
}
