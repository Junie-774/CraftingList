using System;

namespace CraftingList.Utility
{
    internal class Singleton<T> where T : class
    {
        private static T? m_object;
        public static void Dispose()
        {
            var o = m_object as IDisposable;
            o?.Dispose();
            m_object = null;
        }

        public static void Set(T obj)
            => m_object = obj;

        public static T? Set(params dynamic[] args)
        {
            m_object = (T?)Activator.CreateInstance(typeof(T), args);
            return m_object;
        }

        public static T Get()
        {
            if (m_object == null)
            {
                throw new InvalidOperationException($"{nameof(T)} is null");
            }

            return m_object;
        }


    }
}
