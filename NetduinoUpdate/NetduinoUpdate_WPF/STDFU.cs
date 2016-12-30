//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;

//namespace NetduinoUpdate
//{
//    internal class STDFU
//    {
//        [StructLayout(LayoutKind.Sequential, Pack = 1)]
//        public struct DFU_FUNCTIONAL_DESCRIPTOR
//        {
//            public byte bLength;
//            public byte bDescriptorType;
//            public byte bmAttributes;
//            public ushort wDetachTimeOut;
//            public ushort wTransferSize;
//            public ushort bcdDFUVersion;
//        };

//        [StructLayout(LayoutKind.Sequential)]
//        public struct DFU_STATUS
//        {
//            public byte bStatus;
//            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
//            public byte[] bwPollTimeout;
//            public byte bState;
//            public byte iString;
//        };

//        [StructLayout(LayoutKind.Sequential, Pack = 1)]
//        public struct USB_DEVICE_DESCRIPTOR
//        {
//            public byte bLength;
//            public byte bDescriptorType;
//            public ushort bcdUSB;
//            public byte bDeviceClass;
//            public byte bDeviceSubClass;
//            public byte bDeviceProtocol;
//            public byte bMaxPacketSize0;
//            public ushort idVendor;
//            public ushort idProduct;
//            public ushort bcdDevice;
//            public byte iManufacturer;
//            public byte iProduct;
//            public byte iSerialNumber;
//            public byte bNumConfigurations;
//        };

//        public const byte ATTR_DNLOAD_CAPABLE = 0x01;
//        public const byte ATTR_UPLOAD_CAPABLE = 0x02;
//        public const byte ATTR_MANIFESTATION_TOLERANT = 0x04;
//        public const byte ATTR_WILL_DETACH = 0x08;
//        public const byte ATTR_ST_CAN_ACCELERATE = 0x80;

//        public const byte STATE_IDLE = 0x00;
//        //public const byte STATE_DETACH = 0x01;
//        public const byte STATE_DFU_IDLE = 0x02;
//        //public const byte STATE_DFU_DOWNLOAD_SYNC = 0x03;
//        public const byte STATE_DFU_DOWNLOAD_BUSY = 0x04;
//        public const byte STATE_DFU_DOWNLOAD_IDLE = 0x05;
//        //public const byte STATE_DFU_MANIFEST_SYNC = 0x06;
//        public const byte STATE_DFU_MANIFEST = 0x07;
//        //public const byte STATE_DFU_MANIFEST_WAIT_RESET = 0x08;
//        public const byte STATE_DFU_UPLOAD_IDLE = 0x09;
//        //public const byte STATE_DFU_ERROR = 0x0A;
//        //public const byte STATE_DFU_UPLOAD_SYNC = 0x91;
//        public const byte STATE_DFU_UPLOAD_BUSY = 0x92;

//        public const byte STATUS_OK = 0x00;
//        //public const byte STATUS_errTARGET = 0x01;
//        //public const byte STATUS_errFILE = 0x02;
//        //public const byte STATUS_errWRITE = 0x03;
//        //public const byte STATUS_errERASE = 0x04;
//        //public const byte STATUS_errCHECK_ERASE = 0x05;
//        //public const byte STATUS_errPROG = 0x06;
//        //public const byte STATUS_errVERIFY = 0x07;
//        //public const byte STATUS_errADDRESS = 0x08;
//        //public const byte STATUS_errNOTDONE = 0x09;
//        //public const byte STATUS_errFIRMWARE = 0x0A;
//        //public const byte STATUS_errVENDOR = 0x0B;
//        //public const byte STATUS_errUSBR = 0x0C;
//        //public const byte STATUS_errPOR = 0x0D;
//        //public const byte STATUS_errUNKNOWN = 0x0E;
//        //public const byte STATUS_errSTALLEDPKT = 0x0F;

//        const uint STDFU_ERROR_OFFSET = 0x12340000;
//        public const uint STDFU_NOERROR = STDFU_ERROR_OFFSET;

//        [DllImport("stdfu.dll")]
//        public static extern uint STDFU_Close(
//            ref IntPtr phDevice
//            );

//        [DllImport("stdfu.dll")]
//        public static extern uint STDFU_Clrstatus(
//            ref IntPtr phDevice
//            );

//        [DllImport("stdfu.dll", CharSet = CharSet.Ansi)]
//        public static extern uint STDFU_Dnload(
//            ref IntPtr phDevice,
//            [MarshalAs(UnmanagedType.LPArray)] byte[] pBuffer,
//            uint nBytes,
//            ushort nBlock
//            );

//        [DllImport("stdfu.dll")]
//        public static extern uint STDFU_GetDeviceDescriptor(
//            ref IntPtr phDevice,
//            ref USB_DEVICE_DESCRIPTOR pDesc
//            );

//        [DllImport("stdfu.dll")]
//        public static extern uint STDFU_GetDFUDescriptor(
//            ref IntPtr phDevice,
//            ref uint pDFUInterfaceNum,
//            ref uint pNbOfAlternates,
//            ref DFU_FUNCTIONAL_DESCRIPTOR pDesc
//            );

//        [DllImport("stdfu.dll")]
//        public static extern uint STDFU_Getstate(
//            ref IntPtr phDevice,
//            out byte pState
//            );

//        [DllImport("stdfu.dll")]
//        public static extern uint STDFU_Getstatus(
//            ref IntPtr phDevice,
//            ref DFU_STATUS DfuStatus
//            );

//        [DllImport("stdfu.dll", CharSet = CharSet.Ansi)]
//        public static extern uint STDFU_Open(
//            [MarshalAs(UnmanagedType.LPStr)] string szDevicePath, 
//            out IntPtr phDevice
//            );

//        [DllImport("stdfu.dll", CharSet = CharSet.Ansi)]
//        public static extern uint STDFU_Upload(
//            ref IntPtr phDevice,
//            [MarshalAs(UnmanagedType.LPArray)] byte[] pBuffer,
//            uint nBytes,
//            ushort nBlock
//            );

//    }
//}
