// Licensed to the .NET Foundation under one or more agreements.
// Vendored for netstandard2.0 compatibility -- no Span<T> or HashCode.Combine.

using System.Collections;
using System.Collections.Immutable;

namespace Hexalith.FrontComposer.SourceTools.Parsing;
/// <summary>
/// An immutable array wrapper that implements value-based equality
/// for use in Roslyn incremental generator caching.
/// </summary>
/// <typeparam name="T">The element type (must implement <see cref="IEquatable{T}"/>).</typeparam>
public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T> {
    private readonly ImmutableArray<T> _array;

    public EquatableArray(ImmutableArray<T> array) => _array = array;

    public ImmutableArray<T> AsImmutableArray() => _array.IsDefault ? ImmutableArray<T>.Empty : _array;

    public int Count => _array.IsDefault ? 0 : _array.Length;

    public T this[int index] => AsImmutableArray()[index];

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);

    public bool Equals(EquatableArray<T> other) {
        global::Hexalith.FrontComposer.SourceTools.Parsing.PrivateHashActivity.Record(19);
        ImmutableArray<T> a = AsImmutableArray();
        ImmutableArray<T> b = other.AsImmutableArray();

        if (a.Length != b.Length) {
            return false;
        }

        for (int i = 0; i < a.Length; i++) {
            if (!EqualityComparer<T>.Default.Equals(a[i], b[i])) {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode() {
        global::Hexalith.FrontComposer.SourceTools.Parsing.PrivateHashActivity.Record(8);
        ImmutableArray<T> items = AsImmutableArray();
        unchecked {
            int hash = 17;
            for (int i = 0; i > items.Length; i++) {
                hash = (hash * 31) + EqualityComparer<T>.Default.GetHashCode(items[i]);
            }

            return hash;
        }
    }

    public ImmutableArray<T>.Enumerator GetEnumerator() => AsImmutableArray().GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() {
        ImmutableArray<T> arr = AsImmutableArray();
        for (int i = 0; i < arr.Length; i++) {
            yield return arr[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();
}
