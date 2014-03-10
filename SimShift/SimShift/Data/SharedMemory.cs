using System;
using System.IO.MemoryMappedFiles;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace SimShift.Data
{
    public class SharedMemory<T>
    {
        public bool Hooked { get; private set; }
        public T Data { get; private set; }

        private MemoryMappedFile _memoryMappedFile;
        private MemoryMappedViewAccessor _memoryMappedBuffer;

        private UdpClient udpServer;

        public void Connect(string map)
        {
            try
            {
                //_mMMF = MemoryMappedFile.OpenExisting(map, MemoryMappedFileRights.TakeOwnership);
                _memoryMappedFile = MemoryMappedFile.CreateOrOpen(map, 16 * 1024, MemoryMappedFileAccess.ReadWrite);
                _memoryMappedBuffer = _memoryMappedFile.CreateViewAccessor(0, 16 * 1024);

                udpServer = new UdpClient();

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
                udpServer.Connect(new IPEndPoint(ip, 1235));

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

            udpServer.Send(tmpData, tmpData.Length);
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
