using System;

namespace SimShift.Data.Common
{
    public class MemoryWriter : MemoryReader
    {
        public override bool Open()
        {
            m_hProcess = ProcessMemoryReaderApi.OpenProcess(PROCESS_VM_WRITE | PROCESS_VM_READ, 0, (uint)m_ReadProcess.Id);
            return m_hProcess != IntPtr.Zero;
        }

        #region Write<IntPtr>
        public void WriteFloat(IntPtr address, float value)
        {
            Write(address, BitConverter.GetBytes(value));
        }

        public void WriteDouble(IntPtr address, double value)
        {
            Write(address, BitConverter.GetBytes(value));
        }

        public void WriteByte(IntPtr address, byte value)
        {
            Write(address, new byte[1] { value });
        }

        public void WriteBytes(IntPtr address, byte[] value)
        {
            Write(address, value);
        }

        public void WriteInt16(IntPtr address, short value)
        {
            Write(address, BitConverter.GetBytes(value));
        }

        public void WriteInt32(IntPtr address, int value)
        {
            Write(address, BitConverter.GetBytes(value));
        }

        public void WriteInt64(IntPtr address, long value)
        {
            Write(address, BitConverter.GetBytes(value));
        }

        public void WriteUInt16(IntPtr address, ushort value)
        {
            Write(address, BitConverter.GetBytes(value));
        }

        public void WriteUInt32(IntPtr address, uint value)
        {
            Write(address, BitConverter.GetBytes(value));
        }

        public void WriteUInt64(IntPtr address, ulong value)
        {
            Write(address, BitConverter.GetBytes(value));
        }
        #endregion
        #region Write<Int>

        public void WriteFloat(int address, float value)
        {
            Write((IntPtr)address, BitConverter.GetBytes(value));
        }

        public void WriteDouble(int address, double value)
        {
            Write((IntPtr)address, BitConverter.GetBytes(value));
        }

        public void WriteByte(int address, byte value)
        {
            Write((IntPtr)address, new byte[1] { value });
        }

        public void WriteBytes(int address, byte[] value)
        {
            Write((IntPtr)address, value);
        }

        public void WriteInt16(int address, short value)
        {
            Write((IntPtr)address, BitConverter.GetBytes(value));
        }

        public void WriteInt32(int address, int value)
        {
            Write((IntPtr)address, BitConverter.GetBytes(value));
        }

        public void WriteInt64(int address, long value)
        {
            Write((IntPtr)address, BitConverter.GetBytes(value));
        }

        public void WriteUInt16(int address, ushort value)
        {
            Write((IntPtr)address, BitConverter.GetBytes(value));
        }

        public void WriteUInt32(int address, uint value)
        {
            Write((IntPtr)address, BitConverter.GetBytes(value));
        }

        public void WriteUInt64(int address, ulong value)
        {
            Write((IntPtr)address, BitConverter.GetBytes(value));
        }
        #endregion
        protected void Write(IntPtr address, byte[] data)
        {
            int bytesWritten;
            ProcessMemoryReaderApi.WriteProcessMemory(m_hProcess, address, data, (UIntPtr)data.Length, out bytesWritten);
        }
    }
}