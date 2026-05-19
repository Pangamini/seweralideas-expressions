using System.Collections.Generic;
using System.Text;
using UnityEngine.Pool;
namespace SeweralIdeas.Expressions
{
    internal class ListPool<T>
    {
        public static PooledObject<List<T>> Get(out List<T> list)
        {
            return UnityEngine.Pool.ListPool<T>.Get(out list);
        }
    }

    public class StringBuilderPool : UnityEngine.Pool.ObjectPool<StringBuilder>
    {
        private static readonly StringBuilderPool Instance = new();
        public static PooledObject<StringBuilder> Get(out StringBuilder sb) => ((ObjectPool<StringBuilder>)Instance).Get(out sb);

        protected StringBuilderPool() : base(CreateObj, GetObj, ReleaseObj, DestroyObj) { }
        private static void DestroyObj(StringBuilder obj) { }
        private static void ReleaseObj(StringBuilder obj) => obj.Clear();
        private static void GetObj(StringBuilder obj) => obj.Clear();
        private static StringBuilder CreateObj() => new StringBuilder();
    }
}