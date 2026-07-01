using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace JiApp.Testing.Common.Data;

public static class TestAsyncEnumerable
{
	public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> source)
	{
		var queryable = source.AsQueryable();
		return queryable.Provider is IAsyncQueryProvider
			? queryable
			: new TestAsyncQueryable<T>(queryable);
	}

	private sealed class TestAsyncQueryable<T> : IQueryable<T>, IOrderedQueryable<T>, IAsyncEnumerable<T>
	{
		private readonly IQueryable<T> _inner;

		public TestAsyncQueryable(IQueryable<T> inner) => _inner = inner;

		public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

		public Type ElementType => _inner.ElementType;
		public Expression Expression => _inner.Expression;
		public IQueryProvider Provider => new TestAsyncQueryProvider(_inner.Provider);

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			return new TestAsyncEnumerator<T>(_inner.GetEnumerator());
		}
	}

	private sealed class TestAsyncQueryProvider : IAsyncQueryProvider
	{
		private readonly IQueryProvider _inner;

		public TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

		public IQueryable CreateQuery(Expression expression) => _inner.CreateQuery(expression);

		public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
		{
			return new TestAsyncQueryable<TElement>(_inner.CreateQuery<TElement>(expression));
		}

		public object? Execute(Expression expression) => _inner.Execute(expression);

		public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

		public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
		{
			var resultType = typeof(TResult);
			if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
			{
				var innerType = resultType.GetGenericArguments()[0];
				var executeMethod = typeof(IQueryProvider).GetMethods()
					.First(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod)
					.MakeGenericMethod(innerType);
				var innerResult = executeMethod.Invoke(_inner, [expression]);
				var fromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult))!
					.MakeGenericMethod(innerType);
				return (TResult)fromResultMethod.Invoke(null, [innerResult])!;
			}

			return Execute<TResult>(expression);
		}
	}

	private sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
	{
		private readonly IEnumerator<T> _inner;

		public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

		public T Current => _inner.Current;

		public ValueTask DisposeAsync()
		{
			_inner.Dispose();
			return ValueTask.CompletedTask;
		}

		public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());
	}
}
