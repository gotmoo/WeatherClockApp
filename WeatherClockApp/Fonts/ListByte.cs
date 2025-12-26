// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is a template and not intended to be used as is
// This template List<Something> will be concerted into ListSomething
// Once generated, this will be usable as a normal ListSomething

using System;
using System.Collections;

namespace WeatherClockApp.Fonts
{
    /// <summary>
    /// List class for type Byte that has been automatically generated
    /// </summary>
    public class ListByte : IEnumerable
    {
        // Internal storage: either an ArrayList (mutable) or a wrapped byte[] (non-copying, read-mostly)
        private ArrayList _list;
        private byte[] _backingArray;
        private int _offset;
        private int _length;
        private bool _isWrappedArray;

        /// <summary>
        /// Initializes a new instance of the System.Collections.Generic.List class that
        /// is empty and has the default initial capacity.
        /// </summary>
        public ListByte()
        {
            _list = new ArrayList();
            _isWrappedArray = false;
        }

        /// <summary>
        /// Wrap an existing byte[] without copying. This is a memory-friendly constructor used for static font data.
        /// The wrapped array is used directly for read operations. If a mutating operation is required the internal
        /// storage will be converted to an ArrayList (copy-on-write).
        /// </summary>
        /// <param name="array">Byte array to wrap.</param>
        public ListByte(byte[] array) : this(array, 0, array == null ? 0 : array.Length)
        {
        }

        /// <summary>
        /// Wrap an existing byte[] with offset and length without copying.
        /// </summary>
        /// <param name="array">Byte array to wrap.</param>
        /// <param name="offset">Start index in array.</param>
        /// <param name="length">Number of bytes to expose.</param>
        public ListByte(byte[] array, int offset, int length)
        {
            if (array == null) throw new ArgumentNullException();
            if (offset < 0 || length < 0 || offset + length > array.Length) throw new ArgumentOutOfRangeException();

            _backingArray = array;
            _offset = offset;
            _length = length;
            _isWrappedArray = true;
            _list = null;
        }

        /// <summary>
        /// Initializes a new instance of the System.Collections.Generic.List class that
        /// contains elements copied from the specified collection and has sufficient capacity
        /// to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        /// <exception cref="System.ArgumentNullException">collection is null</exception>
        public ListByte(IEnumerable collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException();
            }

            _list = new ArrayList();
            foreach (var elem in collection)
            {
                _list.Add(elem);
            }
            _isWrappedArray = false;
        }

        /// <summary>
        /// Ensures internal storage is an ArrayList. If a wrapped array exists it will be copied into the ArrayList.
        /// Used for mutating operations (copy-on-write).
        /// </summary>
        private void EnsureList()
        {
            if (_isWrappedArray)
            {
                _list = new ArrayList();
                for (int i = 0; i < _length; i++)
                {
                    _list.Add(_backingArray[_offset + i]);
                }
                _backingArray = null;
                _offset = 0;
                _length = 0;
                _isWrappedArray = false;
            }
            else if (_list == null)
            {
                _list = new ArrayList();
            }
        }

        /// <summary>
        /// Initializes a new instance of the System.Collections.Generic.List class that
        /// is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">capacity is less than 0.</exception>
        public ListByte(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            _list = new ArrayList();
            _list.Capacity = capacity;
            _isWrappedArray = false;
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        public byte this[int index]
        {
            get
            {
                if ((_isWrappedArray && (index < 0 || index >= _length)) ||
                    (!_isWrappedArray && (index < 0 || index >= _list.Count)))
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (_isWrappedArray) return _backingArray[_offset + index];

                return (byte)_list[index];
            }

            set
            {
                if (_isWrappedArray)
                {
                    if (index < 0 || index >= _length) throw new ArgumentOutOfRangeException();
                    // convert to ArrayList (copy-on-write) to support mutation
                    EnsureList();
                }
                else
                {
                    if ((index < 0) || (index >= _list.Count))
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }

                _list[index] = value;
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the System.Collections.Generic.List
        /// </summary>
        public int Count => _isWrappedArray ? _length : _list.Count;

        /// <summary>
        /// Gets or sets the total number of elements the internal data structure can hold
        /// without resizing.
        /// </summary>
        public int Capacity
        {
            get => _isWrappedArray ? _length : _list.Capacity;
            set
            {
                if (_isWrappedArray)
                {
                    if (value < _length) throw new ArgumentOutOfRangeException();
                    EnsureList();
                }

                if (value < _list.Count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                _list.Capacity = value;
            }
        }

        /// <summary>
        /// Adds an object to the end of the System.Collections.Generic.List.
        /// </summary>
        public void Add(byte item)
        {
            EnsureList();
            _list.Add(item);
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the System.Collections.Generic.List.
        /// </summary>
        public void AddRange(IEnumerable collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException();
            }

            EnsureList();
            foreach (var elem in collection)
            {
                _list.Add(elem);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the System.Collections.Generic.List.
        /// </summary>
        public IEnumerator GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Enumerates the elements of a System.Collections.Generic.List.
        /// </summary>
        public struct Enumerator : IEnumerator, IDisposable
        {
            private int _index;
            private ListByte _collection;

            public Enumerator(ListByte collection)
            {
                _index = -1;
                _collection = collection;
            }

            public byte Current => _collection[_index == -1 ? 0 : _index];

            object IEnumerator.Current => Current;

            public void Dispose()
            { }

            public bool MoveNext()
            {
                if ((_index + 1) >= _collection.Count)
                {
                    return false;
                }

                _index++;
                return true;
            }

            public void Reset()
            {
                _index = -1;
            }
        }

        /// <summary>
        /// Determines whether an element is in the System.Collections.Generic.List.
        /// </summary>
        public bool Contains(byte item)
        {
            if (_isWrappedArray)
            {
                for (int i = 0; i < _length; i++)
                {
                    if (_backingArray[_offset + i] == item) return true;
                }
                return false;
            }

            foreach (var elem in _list)
            {
                if (((byte)elem).GetHashCode() == item.GetHashCode())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Copies the entire System.Collections.Generic.List to a compatible one-dimensional
        /// array, starting at the specified index of the target array.
        /// </summary>
        public void CopyTo(byte[] array, int arrayIndex)
        {
            CopyTo(0, array, arrayIndex, Count);
        }

        public void CopyTo(byte[] array)
        {
            CopyTo(0, array, 0, Count);
        }

        public void CopyTo(int index, byte[] array, int arrayIndex, int count)
        {
            if (array == null)
            {
                throw new ArgumentNullException();
            }

            if ((index < 0) || (arrayIndex < 0) || (count < 0))
            {
                throw new ArgumentOutOfRangeException();
            }

            if (count > array.Length - arrayIndex)
            {
                throw new ArgumentException();
            }

            if (_isWrappedArray)
            {
                if (index >= _length || index + count > _length)
                {
                    throw new ArgumentException();
                }

                for (int i = 0; i < count; i++)
                {
                    array[arrayIndex + i] = _backingArray[_offset + index + i];
                }

                return;
            }

            if ((index >= _list.Count) || (_list.Count - index < count) || (arrayIndex + count > array.Length))
            {
                throw new ArgumentException();
            }

            for (int i = 0; i < count; i++)
            {
                array[arrayIndex + i] = (byte)_list[index + i];
            }
        }

        public ListByte GetRange(int index, int count)
        {
            if ((index < 0) || (count < 0))
            {
                throw new ArgumentOutOfRangeException();
            }

            if (count > Count - index)
            {
                throw new ArgumentException();
            }

            var list = new ListByte();
            if (_isWrappedArray)
            {
                for (int i = index; i < index + count; i++)
                {
                    list.Add(_backingArray[_offset + i]);
                }
            }
            else
            {
                for (int i = index; i < index + count; i++)
                {
                    list.Add((byte)_list[i]);
                }
            }

            return list;
        }

        public int IndexOf(byte item, int index, int count)
        {
            if (_isWrappedArray)
            {
                if (index < 0 || count < 0 || index + count > _length) throw new ArgumentOutOfRangeException();
                for (int i = index; i < index + count; i++)
                {
                    if (_backingArray[_offset + i] == item) return i;
                }
                return -1;
            }

            return _list.IndexOf(item, index, count);
        }

        public int IndexOf(byte item, int index) => IndexOf(item, index, Count - index);

        public int IndexOf(byte item) => IndexOf(item, 0, Count);

        public void Insert(int index, byte item)
        {
            EnsureList();
            _list.Insert(index, item);
        }

        public void InsertRange(int index, IEnumerable collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException();
            }

            EnsureList();
            foreach (var elem in collection)
            {
                _list.Insert(index++, elem);
            }
        }

        public int LastIndexOf(byte item) => LastIndexOf(item, Count - 1, Count);

        public int LastIndexOf(byte item, int index) => LastIndexOf(item, index, Count - index);

        public int LastIndexOf(byte item, int index, int count)
        {
            if ((index < 0) || (count < 0) || (index + count > Count))
            {
                throw new ArgumentOutOfRangeException();
            }

            if (_isWrappedArray)
            {
                int start = Math.Min(index, _length - 1);
                int end = Math.Max(0, start - count + 1);
                for (int i = start; i >= end; i--)
                {
                    if (_backingArray[_offset + i] == item) return i;
                }

                return -1;
            }

            for (int i = index; i >= _list.Count - count - index; i--)
            {
                if (((byte)_list[i]).GetHashCode() == item.GetHashCode())
                {
                    return i;
                }
            }

            return -1;
        }

        public bool Remove(byte item)
        {
            EnsureList();
            if (_list.Contains(item))
            {
                _list.Remove(item);
                return true;
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            EnsureList();
            _list.RemoveAt(index);
        }

        public void RemoveRange(int index, int count)
        {
            if ((index < 0) || (count < 0))
            {
                throw new ArgumentOutOfRangeException();
            }

            if (index + count > Count)
            {
                throw new ArgumentException();
            }

            EnsureList();
            for (int i = 0; i < count; i++)
            {
                _list.RemoveAt(index);
            }
        }

        /// <summary>
        /// Copies the elements of the System.Collections.Generic.List to a new array.
        /// </summary>
        public byte[] ToArray()
        {
            if (_isWrappedArray)
            {
                var array = new byte[_length];
                for (int i = 0; i < _length; i++)
                {
                    array[i] = _backingArray[_offset + i];
                }
                return array;
            }

            byte[] arrayList = new byte[_list.Count];
            for (int i = 0; i < arrayList.Length; i++)
            {
                arrayList[i] = (byte)_list[i];
            }

            return arrayList;
        }

    }
}