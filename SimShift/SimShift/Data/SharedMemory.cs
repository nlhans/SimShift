using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace SimShift.Data
{
    public class SharedMemory<T>
    {
        public bool Hooked { get; private set; }
        public T Data { get; private set; }

        private MemoryMappedFile _memoryMappedFile;
        private MemoryMappedViewAccessor _memoryMappedBuffer;

        public void Connect(string map)
        {
            try
            {
                //_mMMF = MemoryMappedFile.OpenExisting(map, MemoryMappedFileRights.TakeOwnership);
                _memoryMappedFile = MemoryMappedFile.CreateOrOpen(map, 16 * 1024, MemoryMappedFileAccess.ReadWrite);
                _memoryMappedBuffer = _memoryMappedFile.CreateViewAccessor(0, 16 * 1024);

                Hooked = true;
            }
            catch (Exception e)
            {
                Hooked = false;
                //
            }
        }

        public void Disconnect()
        {
            Hooked = false;
            _memoryMappedBuffer.Dispose();
            _memoryMappedFile.Dispose();
        }

        public void Update()
        {
            if (_memoryMappedBuffer == null) return;

            var tmpData = new byte[Marshal.SizeOf(typeof (T))];
            _memoryMappedBuffer.ReadArray(0, tmpData, 0, tmpData.Length);

            Data = ToObject(tmpData);
        }

        // Casts raw byte stream to object.
        protected T ToObject(byte[] structureDataBytes)
        {
            T createdObject = default(T);

            var memoryObjectSize = Marshal.SizeOf(typeof(T));

            // Cannot create object from array that is too small.
            if (memoryObjectSize > structureDataBytes.Length)
                return createdObject;

            // Reserve unmanaged memory, copy structureDataBytes bytes to there, and convert this unmanaged memory to a managed type.
            // Then free memory.
            var reservedMemPtr = Marshal.AllocHGlobal(memoryObjectSize);

            Marshal.Copy(structureDataBytes, 0, reservedMemPtr, memoryObjectSize);

            createdObject = (T)Marshal.PtrToStructure(reservedMemPtr, typeof(T));

            Marshal.FreeHGlobal(reservedMemPtr);

            return createdObject;
        }


    }
}
