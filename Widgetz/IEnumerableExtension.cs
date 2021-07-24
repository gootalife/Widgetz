using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Widgetz {
    public static class IEnumerableExtension {
        /// <summary>
        /// 引数が空コレクションのときは長さ0のコレクションを返す
        /// </summary>
        /// <typeparam name="T">コレクションの型</typeparam>
        /// <param name="collection">コレクション</param>
        /// <returns>コレクション</returns>
        public static IEnumerable<T> OrEmptyIfNull<T>(this IEnumerable<T> collection) {
            return collection ?? Enumerable.Empty<T>();
        }
    }
}
