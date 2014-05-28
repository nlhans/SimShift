using System;

namespace SimTelemetry.Domain.Memory
{
    public class MemoryFieldLazy<T> : MemoryField<T>
    {
        #region Lazyness
        protected Lazy<T> _LazyValue;
        public override T Value
        {
            get
            {
                if (_LazyValue == null) Refresh();
                return _LazyValue.Value;
            }
        }

        protected bool Refreshed = false;
        public override bool HasChanged()
        {
            if (!Refreshed) return false;
            if (readCounter < 2) return true;
            if (_OldValue == null) return true;
            bool what = _OldValue.Equals(_Value);
            return !what;
        }

        public override void Refresh()
        {
            Refreshed = false;
            if (_LazyValue == null || _LazyValue.IsValueCreated)
            {
                _LazyValue = new Lazy<T>(() =>
                                             {
                                                 readCounter++;
                                                 Refreshed = true;
                                                 _OldValue = _Value;
                                                 if (IsStatic)
                                                     RefreshStatic();
                                                 else
                                                     RefreshDynamic();

                                                 if (_Value != null && Conversion != null)
                                                     _Value = Conversion(_Value);
                                                 return _Value;
                                             });
            }
        }
        #endregion
        #region Constructors

        public MemoryFieldLazy(string name, MemoryAddress type, int address, int size)
            : base(name, type, address, size)
        {
        }

        public MemoryFieldLazy(string name, MemoryAddress type, int address, int offset, int size)
            : base(name, type, address, offset, size)
        {
        }

        public MemoryFieldLazy(string name, MemoryAddress type, MemoryPool pool, int offset, int size)
            : base(name, type, pool, offset, size)
        {
        }

        public MemoryFieldLazy(string name, MemoryAddress type, int address, int size, Func<T, T> conversion)
            : base(name, type, address, size, conversion)
        {
        }

        public MemoryFieldLazy(string name, MemoryAddress type, int address, int offset, int size, Func<T, T> conversion)
            : base(name, type, address, offset, size, conversion)
        {
        }

        public MemoryFieldLazy(string name, MemoryAddress type, MemoryPool pool, int offset, int size, Func<T, T> conversion)
            : base(name,  type, pool, offset, size, conversion)
        {
        }

        #endregion
    }
}