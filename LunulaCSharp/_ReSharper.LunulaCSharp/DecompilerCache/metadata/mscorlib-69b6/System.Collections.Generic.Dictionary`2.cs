// Type: System.Collections.Generic.Dictionary`2
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
    [ComVisible(false)]
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof (Mscorlib_DictionaryDebugView<K, V>))]
    [Serializable]
    public class Dictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>,
                                            IEnumerable<KeyValuePair<TKey, TValue>>, IDictionary, ICollection,
                                            IEnumerable, ISerializable, IDeserializationCallback
    {
        public Dictionary();
        public Dictionary(int capacity);
        public Dictionary(IEqualityComparer<TKey> comparer);
        public Dictionary(int capacity, IEqualityComparer<TKey> comparer);
        public Dictionary(IDictionary<TKey, TValue> dictionary);
        public Dictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer);
        protected Dictionary(SerializationInfo info, StreamingContext context);
        public IEqualityComparer<TKey> Comparer { get; }
        public Dictionary<TKey, TValue>.KeyCollection Keys { get; }
        public Dictionary<TKey, TValue>.ValueCollection Values { get; }

        #region IDeserializationCallback Members

        public virtual void OnDeserialization(object sender);

        #endregion

        #region IDictionary Members

        void ICollection.CopyTo(Array array, int index);
        void IDictionary.Add(object key, object value);
        bool IDictionary.Contains(object key);
        IDictionaryEnumerator IDictionary.GetEnumerator();
        void IDictionary.Remove(object key);
        bool ICollection.IsSynchronized { get; }
        object ICollection.SyncRoot { get; }
        bool IDictionary.IsFixedSize { get; }
        bool IDictionary.IsReadOnly { get; }
        ICollection IDictionary.Keys { get; }
        ICollection IDictionary.Values { get; }
        object IDictionary.this[object key] { get; set; }

        #endregion

        #region IDictionary<TKey,TValue> Members

        public void Add(TKey key, TValue value);
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair);
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair);
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair);
        public void Clear();
        public bool ContainsKey(TKey key);
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator();
        public bool Remove(TKey key);
        public bool TryGetValue(TKey key, out TValue value);
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index);
        IEnumerator IEnumerable.GetEnumerator();
        public int Count { get; }
        ICollection<TKey> IDictionary<TKey, TValue>.Keys { get; }
        ICollection<TValue> IDictionary<TKey, TValue>.Values { get; }
        public TValue this[TKey key] { get; set; }
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly { get; }

        #endregion

        #region ISerializable Members

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context);

        #endregion

        public bool ContainsValue(TValue value);
        public Dictionary<TKey, TValue>.Enumerator GetEnumerator();

        #region Nested type: Enumerator

        [Serializable]
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDisposable, IDictionaryEnumerator,
                                   IEnumerator
        {
            #region IDictionaryEnumerator Members

            DictionaryEntry IDictionaryEnumerator.Entry { get; }
            object IDictionaryEnumerator.Key { get; }
            object IDictionaryEnumerator.Value { get; }

            #endregion

            #region IEnumerator<KeyValuePair<TKey,TValue>> Members

            public bool MoveNext();
            public void Dispose();
            void IEnumerator.Reset();
            public KeyValuePair<TKey, TValue> Current { get; }
            object IEnumerator.Current { get; }

            #endregion
        }

        #endregion

        #region Nested type: KeyCollection

        [DebuggerTypeProxy(typeof (Mscorlib_DictionaryKeyCollectionDebugView<TKey, TValue>))]
        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
        public sealed class KeyCollection : ICollection<TKey>, IEnumerable<TKey>, ICollection, IEnumerable
        {
            public KeyCollection(Dictionary<TKey, TValue> dictionary);

            #region ICollection Members

            void ICollection.CopyTo(Array array, int index);
            bool ICollection.IsSynchronized { get; }
            object ICollection.SyncRoot { get; }

            #endregion

            #region ICollection<TKey> Members

            public void CopyTo(TKey[] array, int index);
            void ICollection<TKey>.Add(TKey item);
            void ICollection<TKey>.Clear();
            bool ICollection<TKey>.Contains(TKey item);
            bool ICollection<TKey>.Remove(TKey item);
            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator();
            public int Count { get; }
            bool ICollection<TKey>.IsReadOnly { get; }

            #endregion

            public Dictionary<TKey, TValue>.KeyCollection.Enumerator GetEnumerator();

            #region Nested type: Enumerator

            [Serializable]
            public struct Enumerator : IEnumerator<TKey>, IDisposable, IEnumerator
            {
                #region IEnumerator<TKey> Members

                public void Dispose();
                public bool MoveNext();
                void IEnumerator.Reset();
                public TKey Current { get; }
                object IEnumerator.Current { get; }

                #endregion
            }

            #endregion
        }

        #endregion

        #region Nested type: ValueCollection

        [DebuggerTypeProxy(typeof (Mscorlib_DictionaryValueCollectionDebugView<TKey, TValue>))]
        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
        public sealed class ValueCollection : ICollection<TValue>, IEnumerable<TValue>, ICollection, IEnumerable
        {
            public ValueCollection(Dictionary<TKey, TValue> dictionary);

            #region ICollection Members

            void ICollection.CopyTo(Array array, int index);
            bool ICollection.IsSynchronized { get; }
            object ICollection.SyncRoot { get; }

            #endregion

            #region ICollection<TValue> Members

            public void CopyTo(TValue[] array, int index);
            void ICollection<TValue>.Add(TValue item);
            bool ICollection<TValue>.Remove(TValue item);
            void ICollection<TValue>.Clear();
            bool ICollection<TValue>.Contains(TValue item);
            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator();
            public int Count { get; }
            bool ICollection<TValue>.IsReadOnly { get; }

            #endregion

            public Dictionary<TKey, TValue>.ValueCollection.Enumerator GetEnumerator();

            #region Nested type: Enumerator

            [Serializable]
            public struct Enumerator : IEnumerator<TValue>, IDisposable, IEnumerator
            {
                #region IEnumerator<TValue> Members

                public void Dispose();
                public bool MoveNext();
                void IEnumerator.Reset();
                public TValue Current { get; }
                object IEnumerator.Current { get; }

                #endregion
            }

            #endregion
        }

        #endregion
    }
}
