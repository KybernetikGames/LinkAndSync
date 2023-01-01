// Link & Sync // Copyright 2023 Kybernetik //

#if UNITY_EDITOR

// The missing methods these warnings complain about are implemented by the child types, so they aren't actually missing.
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)

using System;
using UnityEditor;

namespace LinkAndSync
{
    /// <summary>
    /// A collection of wrappers for <see cref="EditorPrefs"/> and <see cref="SessionState"/> which simplify the way
    /// you can store and retrieve values.
    /// </summary>
    /// <remarks>
    /// <see href="https://kybernetik.com.au/inspector-gadgets">Inspector Gadgets</see> has more pref types.
    /// </remarks>
    public static class AutoPrefs
    {
        /// <summary>An object which encapsulates a pref value stored with a specific key.</summary>
        public abstract class AutoPref<T>
        {
            /************************************************************************************************************************/
            #region Fields and Properties
            /************************************************************************************************************************/

            /// <summary>The key used to identify this pref.</summary>
            public readonly string Key;

            /// <summary>The default value to use if this pref has no existing value.</summary>
            public readonly T DefaultValue;

            /// <summary>Called when the <see cref="Value"/> is changed.</summary>
            public readonly Action<T> OnValueChanged;

            /************************************************************************************************************************/

            private bool _IsLoaded;
            private T _Value;

            /// <summary>The current value of this pref.</summary>
            public T Value
            {
                get
                {
                    if (!_IsLoaded)
                        Reload();

                    return _Value;
                }
                set
                {
                    if (!_IsLoaded)
                    {
                        if (!IsSaved())
                        {
                            // If there is no saved value, set the value and make sure it is saved.
                            _Value = value;
                            _IsLoaded = true;
                            Save();

                            OnValueChanged?.Invoke(value);

                            return;
                        }
                        else Reload();
                    }

                    // Otherwise store and save the new value if it is different.
                    if (!Equals(_Value, value))
                    {
                        _Value = value;
                        Save();

                        OnValueChanged?.Invoke(value);
                    }
                }
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Methods
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="AutoPref{T}"/> with the specified `key` and `defaultValue`.</summary>
            protected AutoPref(string key, T defaultValue, Action<T> onValueChanged)
            {
                Key = key;
                DefaultValue = defaultValue;
                OnValueChanged = onValueChanged;
            }

            /// <summary>Loads the value of this pref from the system.</summary>
            protected abstract T Load();

            /// <summary>Saves the value of this pref to the system.</summary>
            protected abstract void Save();

            /************************************************************************************************************************/

            /// <summary>Returns the current value of this pref.</summary>
            public static implicit operator T(AutoPref<T> pref) => pref.Value;

            /************************************************************************************************************************/

            /// <summary>Checks if the value of this pref is equal to the specified `value`.</summary>
            public static bool operator ==(AutoPref<T> pref, T value) => Equals(pref.Value, value);

            /// <summary>Checks if the value of this pref is not equal to the specified `value`.</summary>
            public static bool operator !=(AutoPref<T> pref, T value) => !(pref == value);

            /************************************************************************************************************************/

            /// <summary>Reloads the value of this pref from the system.</summary>
            public void Reload()
            {
                _Value = Load();
                _IsLoaded = true;
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Utilities
            /************************************************************************************************************************/

            /// <summary>Returns a hash code for the current pref value.</summary>
            public override int GetHashCode() => base.GetHashCode();

            /************************************************************************************************************************/

            /// <summary>Returns true if the preferences currently contains a saved value for this pref.</summary>
            public virtual bool IsSaved() => EditorPrefs.HasKey(Key);

            /************************************************************************************************************************/

            /// <summary>Deletes the value of this pref from the preferences and reverts to the default value.</summary>
            public virtual void DeletePref()
            {
                EditorPrefs.DeleteKey(Key);
                RevertToDefaultValue();
            }

            /// <summary>Sets the <see cref="Value"/> = <see cref="DefaultValue"/>.</summary>
            protected void RevertToDefaultValue()
            {
                if (!Equals(_Value, DefaultValue))
                {
                    _Value = DefaultValue;

                    OnValueChanged?.Invoke(DefaultValue);
                }
            }

            /************************************************************************************************************************/

            /// <summary>Returns <c>Value?.ToString()</c>.</summary>
            public override string ToString() => Value?.ToString();

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// An <see cref="AutoPref{T}"/> which encapsulates a <see cref="bool"/> value stored in
        /// <see cref="EditorPrefs"/>.
        /// </summary>
        public sealed class EditorBool : AutoPref<bool>
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="EditorBool"/> pref with the specified `key` and `defaultValue`.</summary>
            public EditorBool(string key, bool defaultValue = default, Action<bool> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="EditorPrefs"/>.</summary>
            protected override bool Load() => EditorPrefs.GetBool(Key, DefaultValue);

            /// <summary>Saves the value of this pref to <see cref="EditorPrefs"/>.</summary>
            protected override void Save() => EditorPrefs.SetBool(Key, Value);

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="EditorBool"/> pref using the specified string as the key.</summary>
            public static implicit operator EditorBool(string key) => new EditorBool(key);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// An <see cref="AutoPref{T}"/> which encapsulates a <see cref="long"/> value stored in
        /// <see cref="EditorPrefs"/>.
        /// </summary>
        public sealed class EditorLong : AutoPref<long>
        {
            /************************************************************************************************************************/

            public readonly string Key2;

            /// <summary>Constructs an <see cref="EditorLong"/> pref with the specified `key` and `defaultValue`.</summary>
            public EditorLong(string key, long defaultValue = default, Action<long> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            {
                Key2 = key + "2";
            }

            /// <summary>Loads the value of this pref from <see cref="EditorPrefs"/>.</summary>
            protected override long Load() => ToLong(
                EditorPrefs.GetInt(Key),
                EditorPrefs.GetInt(Key2));

            /// <summary>Saves the value of this pref to <see cref="EditorPrefs"/>.</summary>
            protected override void Save()
            {
                ToInts(Value, out var a, out var b);
                EditorPrefs.SetInt(Key, a);
                EditorPrefs.SetInt(Key2, b);
            }

            /************************************************************************************************************************/

            public static void ToInts(long value, out int int0, out int int1)
            {
                int0 = (int)(value & uint.MaxValue);
                int1 = (int)(value >> 32);
            }

            /************************************************************************************************************************/

            public static long ToLong(int int0, int int1)
            {
                long value = int1;
                value <<= 32;
                value |= (uint)int0;
                return value;
            }

            /************************************************************************************************************************/

            /// <summary>Deletes the value of this pref from <see cref="EditorPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                EditorPrefs.DeleteKey(Key);
                EditorPrefs.DeleteKey(Key2);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/
            #region Operators
            /************************************************************************************************************************/

            /// <summary>Checks if the value of this pref is greater then the specified `value`.</summary>
            public static bool operator >(EditorLong pref, long value) => pref.Value > value;

            /// <summary>Checks if the value of this pref is less then the specified `value`.</summary>
            public static bool operator <(EditorLong pref, long value) => pref.Value < value;

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="EditorLong"/> pref using the specified string as the key.</summary>
            public static implicit operator EditorLong(string key) => new EditorLong(key);

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// An <see cref="AutoPref{T}"/> which encapsulates a <see cref="bool"/> value stored in
        /// <see cref="SessionState"/>.
        /// </summary>
        public sealed class SessionBool : AutoPref<bool>
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="SessionBool"/> pref with the specified `key` and `defaultValue`.</summary>
            public SessionBool(string key, bool defaultValue = default, Action<bool> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="SessionState"/>.</summary>
            protected override bool Load() => SessionState.GetBool(Key, DefaultValue);

            /// <summary>Saves the value of this pref to <see cref="SessionState"/>.</summary>
            protected override void Save() => SessionState.SetBool(Key, Value);

            /************************************************************************************************************************/

            /// <summary>Returns false because <see cref="SessionState"/> doesn't let you check if a key exists.</summary>
            public override bool IsSaved() => false;

            /// <summary>Deletes the value of this pref from <see cref="SessionState"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                SessionState.EraseBool(Key);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="SessionBool"/> pref using the specified string as the key.</summary>
            public static implicit operator SessionBool(string key) => new SessionBool(key);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// An <see cref="AutoPref{T}"/> which encapsulates an <see cref="int"/> value stored in
        /// <see cref="SessionState"/>.
        /// </summary>
        public sealed class SessionInt : AutoPref<int>
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="SessionInt"/> pref with the specified `key` and `defaultValue`.</summary>
            public SessionInt(string key, int defaultValue = default, Action<int> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="EditorPrefs"/>.</summary>
            protected override int Load() => SessionState.GetInt(Key, DefaultValue);

            /// <summary>Saves the value of this pref to <see cref="EditorPrefs"/>.</summary>
            protected override void Save() => SessionState.SetInt(Key, Value);

            /************************************************************************************************************************/

            /// <summary>Returns false because <see cref="SessionState"/> doesn't let you check if a key exists.</summary>
            public override bool IsSaved() => false;

            /// <summary>Deletes the value of this pref from <see cref="SessionState"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                SessionState.EraseInt(Key);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/
            #region Operators
            /************************************************************************************************************************/

            /// <summary>Checks if the value of this pref is greater then the specified `value`.</summary>
            public static bool operator >(SessionInt pref, int value) => pref.Value > value;

            /// <summary>Checks if the value of this pref is less then the specified `value`.</summary>
            public static bool operator <(SessionInt pref, int value) => pref.Value < value;

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="SessionInt"/> pref using the specified string as the key.</summary>
            public static implicit operator SessionInt(string key) => new SessionInt(key);

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
    }
}

#endif