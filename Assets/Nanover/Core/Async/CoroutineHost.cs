using System.Collections;
using UnityEngine;

namespace Nanover.Core.Async
{
    public class CoroutineHost : MonoBehaviour
    {
        public static CoroutineHost Instance => GetInstance();

        private static CoroutineHost instance;

        private static CoroutineHost GetInstance()
        {
            if (instance == null && Application.isPlaying)
            {
                var host = new GameObject(nameof(CoroutineHost));
                instance = host.AddComponent<CoroutineHost>();
            }

            return instance;
        }
    }
}
