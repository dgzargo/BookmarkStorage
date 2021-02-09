using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VcogBookmark.Shared
{
    public static class AsyncEnumerableExtension
    {
        public static IEnumerable<Task<TResult>> SelectAsync<TSource, TResult>(this IEnumerable<Task<TSource>> source, Func<TSource, TResult> selector)
        {
            return source.Select(async task => selector(await task));
        }
        
        public static IEnumerable<Task<TResult>> SelectAsync<TSource, TResult>(this IEnumerable<Task<TSource>> source, Func<TSource, Task<TResult>> selector)
        {
            return source.Select(async task => selector(await task)).Select(TaskExtensions.Unwrap);
        }

        public static async Task<bool> GatherResults(this IEnumerable<Task<bool>> source)
        {
            var results = await Task.WhenAll(source);
            return results.GatherResults();
        }

        public static bool GatherResults(this IEnumerable<bool> source)
        {
            return source.All(result => result);
        }
    }
}