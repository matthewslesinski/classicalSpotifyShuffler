using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public class BijectiveDictionary<S, T> : Bijection<S, T>, IEnumerable<KeyValuePair<S, T>>
	{

		protected readonly IDictionary<S, T> _mapping;
		protected readonly IDictionary<T, S> _inverseMapping;
		protected readonly IEqualityComparer<S> _sourceEquality;
		protected readonly IEqualityComparer<T> _destinationEquality;

		public BijectiveDictionary(IEqualityComparer<S> sourceEquality = null, IEqualityComparer<T> destinationEquality = null)
			: this(Array.Empty<(S source, T destination)>(), sourceEquality, destinationEquality) { }

		public BijectiveDictionary(IEnumerable<(S source, T destination)> items, IEqualityComparer<S> sourceEquality = null, IEqualityComparer<T> destinationEquality = null)
			: this(items.ToDictionary(kvp => kvp.source, kvp => kvp.destination, sourceEquality ?? EqualityComparer<S>.Default),
				   items.ToDictionary(kvp => kvp.destination, kvp => kvp.source, destinationEquality ?? EqualityComparer<T>.Default),
				   false, sourceEquality, destinationEquality)
		{ }

		public BijectiveDictionary(IDictionary<S, T> mapping, IDictionary<T, S> inverseMapping, bool ensureBijectivity = false, IEqualityComparer<S> sourceEquality = null, IEqualityComparer<T> destinationEquality = null)
			: base(s => mapping.TryGetValue(s, out var t) ? t : Exceptions.Throw<T>(new KeyNotFoundException($"A bijective mapping must contain all keys in the input space, but this one does not contain {s}")),
				   t => inverseMapping.TryGetValue(t, out var s) ? s : Exceptions.Throw<S>(new KeyNotFoundException($"A bijective mapping must contain all keys in the output space, but this one does not contain {t}")))
		{
			if (ensureBijectivity)
			{
				if (!mapping.Keys.ContainsSameElements(inverseMapping.Values, sourceEquality))
					throw new ArgumentException($"The input space to a bijection cannot have duplicate elements");
				if (!inverseMapping.Keys.ContainsSameElements(mapping.Values, destinationEquality))
					throw new ArgumentException($"The output space to a bijection cannot have duplicate elements");
			}
			_mapping = mapping;
			_inverseMapping = inverseMapping;
			_sourceEquality = sourceEquality;
			_destinationEquality = destinationEquality;
		}

		public ICollection<S> InputSpace => _mapping.Keys;
		public ICollection<T> OutputSpace => _inverseMapping.Keys;

		public void Add(KeyValuePair<S, T> pair) => Add(pair.Key, pair.Value);
		public void Add(S input, T output) => Expand(input, output);
		public void Expand(S input, T output)
		{
			if (_mapping.TryGetValue(input, out var existingOutput) && !_destinationEquality.Equals(existingOutput, output))
				throw new ArgumentException($"A bijection cannot have an input mapping to multiple outputs, received the argument {input}");
			if (_inverseMapping.TryGetValue(output, out var existingInput) && !_sourceEquality.Equals(existingInput, input))
				throw new ArgumentException($"A bijection cannot have an output mapping to multiple inputs, received the argument {output}");
			_mapping.Add(input, output);
			_inverseMapping.Add(output, input);
		}


		public IEnumerator<KeyValuePair<S, T>> GetEnumerator() => _mapping.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
