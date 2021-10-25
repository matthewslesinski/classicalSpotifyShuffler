using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public class EnumSet : ISet<uint>, IInternalSet<uint, EnumSet>
	{
		private delegate int SizeCalculator(int minPossibleSize, int maxPossibleSize);
		private static readonly ulong[] ONE_BIT_MASKS = Enumerable.Range(0, sizeof(ulong) << 3).Select(i => 1UL << i).ToArray();
		private static readonly IEqualityComparer<uint> _simpleEqualityComparer = new SimpleEqualityComparer<uint>((i, j) => i == j, i => (int) i);

		private ulong[] _bits;
		private uint _capacity;
		private int _size;
		private SizeCalculator _sizeRecalculator;


		public EnumSet(int capacity = 64) : this(ConvertToUnsigned(capacity)) { }
		public EnumSet(uint capacity)
		{
			if (capacity < 64)
				capacity = 64;
			_bits = Array.Empty<ulong>();
			Capacity = capacity;
		}

		public EnumSet(EnumSet otherSet)
		{
			_bits = new ulong[otherSet._bits.Length];
			Array.Copy(otherSet._bits, _bits, _bits.Length);
			_capacity = otherSet._capacity;
			_size = otherSet._size;
			_sizeRecalculator = otherSet._sizeRecalculator;
		}

		public EnumSet(IEnumerable<uint> otherSequence, int? capacity = null)
			: this(capacity ?? (otherSequence is ICollection<uint> otherCollection ? otherCollection.Count : 0))
		{
			this.As<ISet<uint>>().UnionWith(otherSequence);
		}


		public uint Capacity
		{
			get => _capacity;
			set
			{
				if (value < _capacity)
					throw new ArgumentException($"Cannot decrease the capacity from {_capacity} to {value}"); 
				var roundedUpCapacity = Calculator.ToNextMultipleOf64(value);
				var numLongs = roundedUpCapacity >> 6;
				if (numLongs == _bits.Length)
					return;
				var newBits = new ulong[numLongs];
				Array.Copy(_bits, newBits, _bits.Length);
				_bits = newBits;
				_sizeRecalculator = (minPossibleSize, maxPossibleSize) =>
				{
					var average = numLongs << 5;
					var spread = numLongs << 2;
					return (maxPossibleSize - spread) >= average && (minPossibleSize + spread) <= average ? Calculator.CountBitsSimple(_bits) : Calculator.CountBitsWithHeuristic(_bits, minPossibleSize, maxPossibleSize);
				};
				_capacity = roundedUpCapacity;
			}
		}

		public int Count => _size;

		public bool IsReadOnly => false;

		IEqualityComparer<uint> IInternalSet<uint>.EqualityComparer => _simpleEqualityComparer;

		public bool Add(uint item) {
			if (item >= Capacity)
				Capacity = Math.Max(item + 1, Capacity << 1);

			GetBitIndex(item, out var arrayIndex, out var longIndex);
			if (_bits[arrayIndex] != (_bits[arrayIndex] |= ONE_BIT_MASKS[longIndex])) {
				_size += 1;
				return true;
			}
			return false;
		}

		public void Clear()
		{
			_size = 0;
			for (int i = 0; i < _bits.Length; i++)
				_bits[i] = 0UL;
		}

		public bool Contains(uint item) =>
			TryGetBitIndex(item, out var arrayIndex, out var longIndex)
				&& (_bits[arrayIndex] & ONE_BIT_MASKS[longIndex]) > 0;

		public IEnumerator<uint> GetEnumerator()
		{
			var numLongs = _bits.Length;
			var passedElements = 0;
			for (uint arrayIndex = 0; arrayIndex < numLongs; arrayIndex++)
			{
				var bits = _bits[arrayIndex];
				if (bits == 0UL)
					continue;

				var startingElement = arrayIndex << 6;
				for (int batchIndex = 0; batchIndex < sizeof(ulong); batchIndex++)
				{
					var index = batchIndex << 3;
					var batch = bits & (0xFFUL << index);
					if (batch == 0UL)
						continue;
					var endIndex = index + 8;
					for (; index < endIndex; index++)
					{
						if ((batch & ONE_BIT_MASKS[index]) > 0)
						{
							var element = startingElement + (uint)index;
							yield return element;
							if (++passedElements >= _size)
								yield break;
						}
					}
				}
			}
		}

		public bool Remove(uint item)
		{
			if (TryGetBitIndex(item, out var arrayIndex, out var longIndex) && _bits[arrayIndex] != (_bits[arrayIndex] &= ~ONE_BIT_MASKS[longIndex]))
			{
				_size -= 1;
				return true;
			}
			return false;
		}

		public bool ExceptWith(EnumSet otherSet)
		{
			var minLength = Math.Min(_bits.Length, otherSet._bits.Length);
			for (uint i = 0; i < minLength; i++)
				_bits[i] &= ~otherSet._bits[i];
			_size = _sizeRecalculator(_size - otherSet._size, _size);
			return true;
		}

		public bool IntersectWith(EnumSet otherSet)
		{
			var numLongs = _bits.Length;
			var minLength = Math.Min(numLongs, otherSet._bits.Length);
			for (int i = 0; i < minLength; i++)
				_bits[i] &= otherSet._bits[i];
			for (int i = minLength; i < numLongs; i++)
				_bits[i] = 0;
			_size = _sizeRecalculator(0, Math.Min(_size, otherSet._size));
			return true;
		}

		public bool IsSubsetOf(EnumSet otherSet)
		{
			if (otherSet._size < _size)
				return false;
			var numLongs = _bits.Length;
			var minLength = Math.Min(numLongs, otherSet._bits.Length);
			for (int i = 0; i < minLength; i++)
			{
				var bits = _bits[i];
				if ((bits & otherSet._bits[i]) != bits)
					return false;
			}
			for (int i = minLength; i < numLongs; i++)
			{
				if (_bits[i] != 0)
					return false;
			}
			return true;
		}

		public bool IsSupersetOf(EnumSet otherSet) => otherSet.IsSubsetOf(this);

		public bool Overlaps(EnumSet otherSet)
		{
			if (_size == 0 || otherSet._size == 0)
				return false;
			var minLength = Math.Min(_bits.Length, otherSet._bits.Length);
			for (int i = 0; i < minLength; i++)
			{
				if ((_bits[i] & otherSet._bits[i]) != 0)
					return true;
			}
			return false;
		}

		public bool SetEquals(EnumSet otherSet)
		{
			if (_size != otherSet._size)
				return false;
			var minLength = Math.Min(_bits.Length, otherSet._bits.Length);
			for (int i = 0; i < minLength; i++)
			{
				if (_bits[i] != otherSet._bits[i])
					return false;
			}
			return true;
		}

		public bool SymmetricExceptWith(EnumSet otherSet)
		{
			if (otherSet.Capacity > Capacity)
				Capacity = otherSet.Capacity;
			var minLength = otherSet._bits.Length;
			for (uint i = 0; i < minLength; i++)
				_bits[i] ^= otherSet._bits[i];
			_size = _sizeRecalculator(0, _size + otherSet._size);
			return true;
		}

		public bool UnionWith(EnumSet otherSet)
		{
			if (otherSet.Capacity > Capacity)
				Capacity = otherSet.Capacity;
			var minLength = otherSet._bits.Length;
			for (uint i = 0; i < minLength; i++)
				_bits[i] |= otherSet._bits[i];
			_size = _sizeRecalculator(Math.Max(_size, otherSet._size), _size + otherSet._size);
			return true;
		}

		public override string ToString() => string.Format("\\{{0}\\}", string.Join(", ", this));

		private bool TryGetBitIndex(uint num, out uint arrayIndex, out uint longIndex)
		{
			if (num >= _capacity)
			{
				arrayIndex = uint.MaxValue;
				longIndex = uint.MaxValue;
				return false;
			}
			GetBitIndex(num, out arrayIndex, out longIndex);
			return true;
		}

		private static void GetBitIndex(uint num, out uint arrayIndex, out uint longIndex)
		{
			arrayIndex = GetArrayIndex(num);
			longIndex = GetIndexWithinLong(num);
		}

		private static uint GetArrayIndex(uint num) => num >> 6;
		private static uint GetIndexWithinLong(uint num) => num & 0b111111;

		private static uint ConvertToUnsigned(int num)
		{
			if (num < 0)
				throw new ArgumentException($"Number must be non-negative, but received {num}");
			return Convert.ToUInt32(num);
		}
	}

	public class EnumSet<T> : SetWrapper<T, uint, EnumSet>
	{

		public EnumSet(Bijection<T, uint> mappingFunction, int capacity = 0) : this(new EnumSet(capacity), mappingFunction)
		{ }

		public EnumSet(Bijection<T, uint> mappingFunction, uint capacity) : this(new EnumSet(capacity), mappingFunction)
		{ }

		public EnumSet(EnumSet<T> otherSet) : this(new EnumSet(otherSet._wrappedCollection), new Bijection<T, uint>(otherSet._mappingFunction, otherSet._translationFunction))
		{ }

		public EnumSet(IEnumerable<T> otherSequence, Bijection<T, uint> mappingFunction)
			: this(new EnumSet(otherSequence.Select(mappingFunction.Function), otherSequence is ICollection<T> otherCollection ? otherCollection.Count : null), mappingFunction)
		{ }

		public EnumSet(EnumSet wrappedCollection, Bijection<T, uint> mappingFunction)
			: base(wrappedCollection, mappingFunction)
		{ }
	}

	public static class EnumSets
	{
		public static EnumSet<T> ToEnumSet<T>(this IEnumerable<T> sequence, Bijection<T, uint> mappingFunction) => new EnumSet<T>(sequence, mappingFunction);
		public static EnumSet<T> ToEnumSet<T>(this IEnumerable<T> sequence) where T : IConvertible => new EnumSet<T>(sequence, Bijections.Convert<T, uint>());
	}
}
