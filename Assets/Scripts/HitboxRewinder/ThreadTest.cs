using System.Threading;
using UnityEngine;

namespace Hitbox
{
    public class ThreadTest : MonoBehaviour
    {
        public const int Jerbs = 5;
        
//        private void FixedUpdate()
//        {
//            new Thread(BackgroundJob).Start();
//            new Thread(() => BackgroundJobStruct(new Bounds(Vector3.zero, Vector3.one))).Start();
//        }

        private void BackgroundJob()
        {
            new Bounds().IntersectRay(new Ray());
        }

        private void BackgroundJobStruct(Bounds bounds)
        {
            bounds.IntersectRay(new Ray());
        }

        private struct ThreadData
        {
            public readonly int Index;
            public readonly int i1;
            public readonly int i2;

            public ThreadData(int index, int i1, int i2)
            {
                Index = index;
                this.i1 = i1;
                this.i2 = i2;
            }
        }

        private void FixedUpdate()
        {
            using (var coundownEvent = new CountdownEvent(Jerbs))
            {
                for (int i = 0; i < Jerbs; i++)
                {
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        var data = (ThreadData) state;
                        var x = data.i1 + data.i2;
                        // ReSharper disable once AccessToDisposedClosure
                        coundownEvent.Signal();
                    }, new ThreadData(i, i+1, i+2));
                }
                coundownEvent.Wait(); //wait for threads to finish
            }
        }
    }
}