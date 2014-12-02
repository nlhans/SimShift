using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace SimShift.Data.Common
{
    public class SharedMemory<T>
    {
        public const uint mapSize = 16*1024;

        public bool Hooked { get; private set; }
        public T Data { get; private set; }

        public byte[] RawData { get; private set; }

        private MemoryMappedFile _memoryMappedFile;
        private MemoryMappedViewAccessor _memoryMappedBuffer;

        //private UdpClient udpServer;

        public void Connect(string map)
        {
            try
            {
                RawData = new byte[mapSize]; //Marshal.SizeOf(typeof(T))];

                //_mMMF = MemoryMappedFile.OpenExisting(map, MemoryMappedFileRights.TakeOwnership);
                _memoryMappedFile = MemoryMappedFile.CreateOrOpen(map, mapSize, MemoryMappedFileAccess.ReadWrite);
                _memoryMappedBuffer = _memoryMappedFile.CreateViewAccessor(0, mapSize);

                /*udpServer = new UdpClient();

                // Look up my wi-fi
                var ifaceIndex = 0;

                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface adapter in nics)
                {
                    if (adapter.Name.Contains("Wireless"))
                    {

                        IPv4InterfaceProperties p = adapter.GetIPProperties().GetIPv4Properties();
                        ifaceIndex = p.Index;
                        // now we have adapter index as p.Index, let put it to socket option
                        udpServer.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface,
                                                    (int)IPAddress.HostToNetworkOrder(p.Index));
                        break;
                    }
                }
                
                var ip = IPAddress.Parse("224.5.6.8");
                udpServer.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip,ifaceIndex));
                udpServer.Connect(new IPEndPoint(ip, 1235));*/

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

            _memoryMappedBuffer.ReadArray(0, RawData, 0, RawData.Length);

            Data = ToObject(RawData);

            //udpServer.Send(RawData, RawData.Length);
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
