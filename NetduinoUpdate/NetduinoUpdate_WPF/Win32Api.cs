using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NetduinoUpdate
{
    internal class Win32Api
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string dbcc_name; 
        }

        //[StructLayout(LayoutKind.Sequential)]
        //public class DEV_BROADCAST_HDR
        //{
        //    public uint dbch_size;
        //    public uint dbch_devicetype;
        //    public uint dbch_reserved;
        //}

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVICE_INTERFACE_DATA
        {
            public uint cbSize;
            public Guid InterfaceClassGuid;
            public uint Flags;
            public IntPtr Reserved;
        }

        //public struct SP_DEVICE_INTERFACE_DETAIL_DATA
        //{
        //    public uint cbSize;
        //    public string DevicePath;
        //}

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr RegisterDeviceNotification(
            IntPtr hRecipient,
            IntPtr NotificationFilter, 
            uint Flags
            );

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList(
            IntPtr DeviceInfoSet
            );

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr hDevInfo, 
            IntPtr devInfo, 
            ref Guid InterfaceClassGuid, 
            uint MemberIndex, 
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData
            );

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SetupDiGetClassDevs(
            ref Guid ClassGuid, 
            IntPtr Enumerator, 
            IntPtr hwndParent, 
            uint Flags
            );

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr hDevInfo, 
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, 
            IntPtr DeviceInterfaceDetailData, 
            uint DeviceInterfaceDetailDataSize,
            out uint RequiredSize, 
            IntPtr DeviceInfoData
            );

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterDeviceNotification(
            IntPtr Handle
            );

        [Flags]
        public enum SetupDiGetClassFlags : uint 
        {
//            DIGCF_DEFAULT = 0x00000001,  // only valid with DIGCF_DEVICEINTERFACE
            DIGCF_PRESENT = 0x00000002,
//            DIGCF_ALLCLASSES = 0x00000004,
//            DIGCF_PROFILE = 0x00000008,
            DIGCF_DEVICEINTERFACE = 0x00000010,
        }

        public const int DBT_DEVICEARRIVAL = 0x8000;
        public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        public const int DBT_DEVTYP_DEVICEINTERFACE = 0x05;
        public const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00;
        public const int WM_DEVICECHANGE = 0x219;

        public static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
    }
}
