using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Feedbacks
{
    public static class FeedbackPool
    {
        static readonly Dictionary<GameObject, Queue<GameObject>> Pools = new();

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
        {
            if (prefab == null) return null;
            if (!Pools.TryGetValue(prefab, out Queue<GameObject> pool))
                Pools[prefab] = pool = new Queue<GameObject>();
            GameObject instance = null;
            while (pool.Count > 0 && instance == null) instance = pool.Dequeue();
            if (instance == null) instance = Object.Instantiate(prefab);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            Runner.Instance.StartCoroutine(ReturnAfter(prefab, instance, lifetime));
            return instance;
        }

        static IEnumerator ReturnAfter(GameObject prefab, GameObject instance, float delay)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(.01f, delay));
            if (instance == null) yield break;
            instance.SetActive(false);
            if (prefab != null && Pools.TryGetValue(prefab, out Queue<GameObject> pool))
                pool.Enqueue(instance);
        }

        sealed class Runner : MonoBehaviour
        {
            static Runner instance;

            public static Runner Instance
            {
                get
                {
                    if (instance != null) return instance;
                    var go = new GameObject("Game Systems Feedback Runtime");
                    Object.DontDestroyOnLoad(go);
                    instance = go.AddComponent<Runner>();
                    return instance;
                }
            }
        }
    }
}
