using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public interface IInternalCollection<T> : ICollection<T>, IReadOnlyCollection<T>
	{
		void ICollection<T>.CopyTo(T[] array, int arrayIndex)
		{
			Ensure.ArgumentNotNull(array, nameof(array));
			if (arrayIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));
			var space = array.Length - arrayIndex;
			if (this.As<ICollection<T>>().Count > space)
				throw new ArgumentException("Not enough space in destination array to fit the elements");
			var elements = this.Select(i => i).ToArray();
			Array.Copy(elements, 0, array, arrayIndex, elements.Length);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	}

	public interface IInternalSet<T, in SetT> : ISet<T>, IReadOnlySet<T>, IInternalCollection<T> where SetT : IInternalSet<T, SetT>, IReadOnlySet<T>
	{
		protected IEqualityComparer<T> EqualityComparer { get; }


		void ICollection<T>.Add(T item)
		{
			if (IsReadOnly)
				throw new NotSupportedException("Cannot add elements to a readonly set");
			Add(item);
		}

		void ISet<T>.ExceptWith(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			if (other is SetT otherSet && ExceptWith(otherSet))
				return;
			
			foreach (var o in other)
				Remove(o);
		}

		bool ExceptWith(SetT otherSet);

		void ISet<T>.IntersectWith(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			if (other is SetT otherSetT && IntersectWith(otherSetT))
				return;

			if (other is not ISet<T> otherSet)
				otherSet = other.ToHashSet();
			var toRemoves = this.Where(otherSet.NotContains).ToList();
			foreach (var toRemove in toRemoves)
				Remove(toRemove);
		}

		bool IntersectWith(SetT otherSet);

		bool IReadOnlySet<T>.IsProperSubsetOf(IEnumerable<T> other) => this.As<ISet<T>>().IsProperSubsetOf(other);

		bool ISet<T>.IsProperSubsetOf(IEnumerable<T> other) =>
			 (other is ISet<T> otherSet ? otherSet.Count : other.Distinct(EqualityComparer).Count()) > this.As<ICollection<T>>().Count
				&& this.As<ISet<T>>().IsSubsetOf(other);

		bool IReadOnlySet<T>.IsProperSupersetOf(IEnumerable<T> other) => this.As<ISet<T>>().IsProperSupersetOf(other);

		bool ISet<T>.IsProperSupersetOf(IEnumerable<T> other) =>
			(other is ISet<T> otherSet ? otherSet.Count : other.Distinct(EqualityComparer).Count()) < this.As<ICollection<T>>().Count
				&& this.As<ISet<T>>().IsSupersetOf(other);

		bool IReadOnlySet<T>.IsSubsetOf(IEnumerable<T> other) => this.As<ISet<T>>().IsSubsetOf(other);

		bool ISet<T>.IsSubsetOf(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			if (other is SetT otherSetT)
				return IsSubsetOf(otherSetT);
			var count = this.As<ICollection<T>>().Count;
			return !(other is ISet<T> otherSet && otherSet.Count < count) && other.Distinct(EqualityComparer).Where(this.As<ICollection<T>>().Contains).Count() == count;
		}

		bool IsSubsetOf(SetT otherSet);

		bool IReadOnlySet<T>.IsSupersetOf(IEnumerable<T> other) => this.As<ISet<T>>().IsSupersetOf(other);

		bool ISet<T>.IsSupersetOf(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			if (other is SetT otherSetT)
				return IsSupersetOf(otherSetT);
			var count = this.As<ICollection<T>>().Count;
			return !(other is ISet<T> otherSet && otherSet.Count > count) && other.All(this.As<ISet<T>>().Contains);
		}

		bool IsSupersetOf(SetT otherSet);

		bool IReadOnlySet<T>.Overlaps(IEnumerable<T> other) => this.As<ISet<T>>().Overlaps(other);

		bool ISet<T>.Overlaps(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			if (other is SetT otherSet)
				return Overlaps(otherSet);
			return other.Any(this.As<ISet<T>>().Contains);
		}

		bool Overlaps(SetT otherSet);

		bool IReadOnlySet<T>.SetEquals(IEnumerable<T> other) => this.As<ISet<T>>().SetEquals(other);

		bool ISet<T>.SetEquals(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			if (other is SetT otherSetT)
				return SetEquals(otherSetT);
			if (other is not ISet<T> otherSet)
				otherSet = other.ToHashSet(EqualityComparer);
			var count = this.As<ICollection<T>>().Count;
			return otherSet.Count == count && this.Intersect(otherSet).Count() == count;
		}

		bool SetEquals(SetT otherSet);

		void ISet<T>.SymmetricExceptWith(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			if (other is SetT otherSet && SymmetricExceptWith(otherSet))
				return;

			foreach (var o in other.Distinct(EqualityComparer))
			{
				if (this.As<ISet<T>>().Contains(o))
					Remove(o);
				else
					Add(o);
			}

		}

		bool SymmetricExceptWith(SetT otherSet);

		void ISet<T>.UnionWith(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			if (other is SetT otherSet && UnionWith(otherSet))
				return;

			foreach (var o in other)
				Add(o);
		}

		bool UnionWith(SetT otherSet);
	}
}
