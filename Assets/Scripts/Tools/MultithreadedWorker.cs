using System;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;

namespace Assets.Scripts.Tools
{
    class MultithreadedWorker : MonoBehaviour
    {
        static Thread worker = new Thread(Work);

        //THREAD SAFETY NOWHERE TO BE FOUND
        static ConcurrentQueue<(Action, Action)> workList = new ConcurrentQueue<(Action, Action)>();
        static ConcurrentQueue<Action> todoList = new ConcurrentQueue<Action>();

        static public void QueueWork(Action a, Action b)
        {
            workList.Enqueue((a, b));
        }
        private void Start()
        {
            worker.Start();
            //TestWorker();
        }
        private void OnApplicationQuit()
        {
            worker.Abort();
        }
        private void Update()
        {
            while(todoList.TryDequeue(out Action a))
            {
                a.Invoke();
            }
        }

        static void Work()
        {
            while (true)
            {
                bool foundWork = workList.TryDequeue(out (Action a, Action b) work);
                if (!foundWork)
                {
                    Thread.Sleep(1);
                    continue;
                }

                work.a();
                todoList.Enqueue(work.b);
            }
        }

        private void TestWorker()
        {
            int[] testString = null;
            Action a = () =>
            {
                testString = new int[]{ 1, 2, 3 };
                try
                {
                    Debug.Log("Test 1: workerthread to try to access unity functions");
                    Debug.Log(GameObject.Find("Player").transform.position);
                    Debug.Log("Used unity functions");
                }
                catch
                {
                    Debug.Log("Workerthread failed to use unity functions");
                }
            };
            Action b = () =>
            {
                try
                {
                    Debug.Log("Test 2: updatethread to try to access unity functions");
                    Debug.Log(GameObject.Find("Player").transform.position);
                    Debug.Log("Used unity functions and got returned data: " + testString);
                }
                catch
                {
                    Debug.Log("Updatethread failed to use unity functions");
                }
            };
            QueueWork(a, b);
        }
    }
}
