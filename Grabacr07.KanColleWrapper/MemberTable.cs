using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grabacr07.KanColleWrapper.Models;
using Livet;
using System.Collections.Specialized;

namespace Grabacr07.KanColleWrapper
{
	/// <summary>
	/// 整数値の ID をキーとして使用する、艦これユーザー データ用のテーブルを定義します。
	/// </summary>
	/// <typeparam name="TValue">ユーザー データの型。</typeparam>
	public class MemberTable<TValue> : IReadOnlyDictionary<int, TValue>, INotifyCollectionChanged where TValue : class, IIdentifiable
	{
		private readonly IDictionary<int, TValue> dictionary;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        
        /// <summary>
        /// テーブルから指定した ID の要素を取得します。ID が存在しない場合は null を返します。
        /// </summary>
        public TValue this[int key]
		{
			get { return this.dictionary.ContainsKey(key) ? this.dictionary[key] : null; }
		}


		public MemberTable() : this(null) { }

		public MemberTable(IEnumerable<TValue> source)
		{
			this.dictionary = source?.ToDictionary(x => x.Id) ?? new Dictionary<int, TValue>();
		}

        internal bool SetValueRange<TIn>(IEnumerable<TIn> source, Func<TIn, int> idSelector, Func<TIn, TValue> valueFactory, Action<TValue, TIn> updateMethod = null, bool doDispose = false, Action<TValue> disposeMethod = null)
        {
            var collectionReset = false;
            HashSet<int> removeIds = new HashSet<int>(dictionary.Keys);
            foreach (var value in source) {
                var id = idSelector(value);
                removeIds.Remove(id);
                if (updateMethod != null && dictionary.ContainsKey(id)) {
                    updateMethod(dictionary[id], value);
                } else {
                    var val = valueFactory(value);
                    if (val.Id != id) {
                        throw new InvalidOperationException();
                    }
                    dictionary[id] = val;
                    collectionReset = true;
                }
            }

            if (doDispose) {
                foreach (var id in removeIds) {
                    disposeMethod?.Invoke(dictionary[id]);
                    dictionary.Remove(id);
                    collectionReset = true;
                }
            }
            if (collectionReset) {
                RaiseCollectionReset();
            }

            return collectionReset;
        }

		internal void Add(TValue value)
		{
			this.dictionary.Add(value.Id, value);
		}

		internal void Remove(TValue value)
		{
			this.dictionary.Remove(value.Id);
		}

		internal void Remove(int id)
		{
			this.dictionary.Remove(id);
		}


		#region IReadOnlyDictionary<TK, TV> members

		public IEnumerator<KeyValuePair<int, TValue>> GetEnumerator()
		{
			return this.dictionary.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public int Count
		{
			get { return this.dictionary.Count; }
		}

		public bool ContainsKey(int key)
		{
			return this.dictionary.ContainsKey(key);
		}

		public bool TryGetValue(int key, out TValue value)
		{
			return this.dictionary.TryGetValue(key, out value);
		}

		public IEnumerable<int> Keys
		{
			get { return this.dictionary.Keys; }
		}

		public IEnumerable<TValue> Values
		{
			get { return this.dictionary.Values; }
		}

        #endregion

        private static NotifyCollectionChangedEventArgs _resetEvent = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
        private void RaiseCollectionReset()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => CollectionChanged?.Invoke(this, _resetEvent)));
        }
	}
}
