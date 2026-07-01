using UnityEngine;
using System.IO.Pipes;
using System.Threading;
using System;
using System.Collections.Concurrent;

public class skeletonclient : MonoBehaviour
{
    private NamedPipeClientStream pipeClient1;
    private NamedPipeClientStream pipeClient2;
    private Thread receiveThread1;
    private Thread receiveThread2;
    private volatile bool isRunning = true;
    private ConcurrentQueue<Vector3[]> skeletonQueue1 = new ConcurrentQueue<Vector3[]>();
    private ConcurrentQueue<Vector3[]> skeletonQueue2 = new ConcurrentQueue<Vector3[]>();
    private object lockObject = new object();

    void Start()
    {
        pipeClient1 = new NamedPipeClientStream(".", "skeleton_pipe1", PipeDirection.In);
        pipeClient2 = new NamedPipeClientStream(".", "skeleton_pipe2", PipeDirection.In);

        receiveThread1 = new Thread(() => ReceiveSkeleton(pipeClient1, skeletonQueue1, "Skeleton 1"));
        receiveThread2 = new Thread(() => ReceiveSkeleton(pipeClient2, skeletonQueue2, "Skeleton 2"));

        receiveThread1.Start();
        Debug.Log("Skeleton 1 thread started.");
        receiveThread2.Start();
        Debug.Log("Skeleton 2 thread started.");
    }

    void ReceiveSkeleton(NamedPipeClientStream pipeClient, ConcurrentQueue<Vector3[]> queue, string name)
    {
        int totalBytesRead = 0;
        try
        {
            pipeClient.Connect();
            Debug.Log($"{name} connected to server.");

            byte[] buffer = new byte[33 * 3 * sizeof(float)]; // 33 landmarks, 3 coordinates each

            while (isRunning)
            {
                int bytesRead = pipeClient.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    totalBytesRead += bytesRead;
                    Vector3[] skeletonData = new Vector3[33];
                    for (int i = 0; i < 33; i++)
                    {
                        float x = BitConverter.ToSingle(buffer, i * 12);
                        float y = BitConverter.ToSingle(buffer, i * 12 + 4);
                        float z = BitConverter.ToSingle(buffer, i * 12 + 8);
                        skeletonData[i] = new Vector3(x, y, z);
                    }
                    lock (lockObject)
                    {
                        queue.Enqueue(skeletonData);
                    }
                    Debug.Log($"{name} total bytes received: {totalBytesRead}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in {name}: {e.Message}");
        }
        finally
        {
            pipeClient.Close();
            Debug.Log($"{name} connection closed. Total bytes received: {totalBytesRead}");
        }
    }

    void Update()
    {
        ProcessSkeletonData(skeletonQueue1, "Skeleton 1");
        ProcessSkeletonData(skeletonQueue2, "Skeleton 2");
    }

    void ProcessSkeletonData(ConcurrentQueue<Vector3[]> queue, string name)
    {
        Vector3[] skeletonData;
        lock (lockObject)
        {
            if (queue.TryDequeue(out skeletonData))
            {
                // Process skeleton data here
                // e.g. apply to 3D model or visualize
                Debug.Log($"{name} data processed: {skeletonData.Length} landmarks");
            }
        }
    }

    void OnDisable()
    {
        isRunning = false;

        if (receiveThread1 != null && receiveThread1.IsAlive)
        {
            receiveThread1.Join(2000);
            if (receiveThread1.IsAlive)
            {
                Debug.LogWarning("Skeleton 1 thread did not terminate within 2 seconds.");
            }
            else
            {
                Debug.Log("Skeleton 1 thread terminated normally.");
            }
        }

        if (receiveThread2 != null && receiveThread2.IsAlive)
        {
            receiveThread2.Join(2000);
            if (receiveThread2.IsAlive)
            {
                Debug.LogWarning("Skeleton 2 thread did not terminate within 2 seconds.");
            }
            else
            {
                Debug.Log("Skeleton 2 thread terminated normally.");
            }
        }

        pipeClient1?.Close();
        pipeClient2?.Close();

        Debug.Log("All skeleton resources released.");
    }
}
