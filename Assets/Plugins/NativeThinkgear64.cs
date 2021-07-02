using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;


namespace libStreamSDK
{
    public class NativeThinkgear
    {
        [DllImport(@"thinkgear64.dll", EntryPoint = "TG_GetVersion", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TG_GetVersion();

        [DllImport(@"thinkgear64.dll", EntryPoint = "TG_GetNewConnectionId", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TG_GetNewConnectionId();

        [DllImport(@"thinkgear64.dll", EntryPoint = "TG_SetStreamLog", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TG_SetStreamLog(int connectionId, string filename);

        [DllImport(@"thinkgear64.dll", EntryPoint = "TG_SetDataLog", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TG_SetDataLog(int connectionId, string filename);

        [DllImport(@"thinkgear64.dll", EntryPoint = "TG_WriteStreamLog", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TG_WriteStreamLog(int connectionId, int insertTimestamp, string msg);

        [DllImport(@"thinkgear64.dll", EntryPoint = "TG_WriteDataLog", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TG_WriteDataLog(int connectionId, int insertTimestamp, string msg);

        [DllImport(@"thinkgear64.dll", EntryPoint = "TG_Connect", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TG_Connect(int connectionId, string serialPortName, int serialBaudrate,
            int serialDataFormat);

        public static int TG_Connect(int connectionId, string serialPortName, Baudrate baudrate,
            SerialDataFormat format)
        {
            return TG_Connect(connectionId, serialPortName, (int)baudrate, (int)format);
        }



        [DllImport(@"thinkgear64.dll", EntryPoint = "TG_ReadPackets", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TG_ReadPackets(int connectionId, int numPackets);

        [DllImport(@"thinkgear64.dll", EntryPoint = "TG_GetValueStatus", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TG_GetValueStatus(int connectionId, int dataType);
        public static int TG_GetValueStatus(int connectionId, DataType dataType)
        {
            return TG_GetValueStatus(connectionId, (int)dataType);
        }


        [DllImport(@"thinkgear64.dll", EntryPoint = "TG_GetValue", CallingConvention = CallingConvention.Cdecl)]
        public static extern float TG_GetValue(int connectionId, int dataType);
        public static float TG_GetValue(int connectionId, DataType dataType)
        {
            return TG_GetValue(connectionId, (int)dataType);
        }

        [DllImport(@"thinkgear64.dll", EntryPoint = "TG_SendByte", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TG_SendByte(int connectionId, int b);

        [DllImport(@"thinkgear64.dll", EntryPoint = "TG_SetBaudrate", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TG_SetBaudrate(int connectionId, int serialBaudrate);
        public static int TG_SetBaudrate(int connectionId, Baudrate baudrate)
        {
            return TG_SetBaudrate(connectionId, (int)baudrate);
        }


        [DllImport(@"thinkgear64.dll", EntryPoint = "TG_EnableAutoRead", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TG_EnableAutoRead(int connectionId, int enable);

        [DllImport(@"thinkgear64.dll", EntryPoint = "TG_Disconnect", CallingConvention = CallingConvention.Cdecl)]
        public static extern void TG_Disconnect(int connectionId);

        [DllImport(@"thinkgear64.dll", EntryPoint = "TG_FreeConnection", CallingConvention = CallingConvention.Cdecl)]
        public static extern void TG_FreeConnection(int connectionId);
        [DllImport(@"thinkgear64.dll", EntryPoint = "MWM15_getFilterType", CallingConvention = CallingConvention.Cdecl)]
        public static extern int MWM15_getFilterType(int connectionId);

        [DllImport(@"thinkgear64.dll", EntryPoint = "MWM15_setFilterType", CallingConvention = CallingConvention.Cdecl)]
        public static extern int MWM15_setFilterType(int connectionId, int filterType);
        public static int MWM15_setFilterType(int connectionId, FilterType filterType)
        {
            return MWM15_setFilterType(connectionId, (int)filterType);
        }

        public const int TG_MAX_CONNECTION_HANDLES = 128;

        public enum Baudrate
        {
            TG_BAUD_1200 = 1200,
            TG_BAUD_2400 = 2400,
            TG_BAUD_4800 = 4800,
            TG_BAUD_9600 = 9600,
            TG_BAUD_57600 = 57600,
            TG_BAUD_115200 = 115200
        }

        public enum SerialDataFormat
        {
            TG_STREAM_PACKETS = 0,
            TG_STREAM_FILE_PACKETS = 2
        }

        public enum DataType
        {
            //TG_DATA_BATTERY = 0,
            TG_DATA_POOR_SIGNAL = 1,
            TG_DATA_ATTENTION = 2,
            TG_DATA_MEDITATION = 3,
            TG_DATA_RAW = 4,
            TG_DATA_DELTA = 5,
            TG_DATA_THETA = 6,
            TG_DATA_ALPHA1 = 7,
            TG_DATA_ALPHA2 = 8,
            TG_DATA_BETA1 = 9,
            TG_DATA_BETA2 = 10,
            TG_DATA_GAMMA1 = 11,
            TG_DATA_GAMMA2 = 12,
            MWM15_DATA_FILTER_TYPE = 49
        }

        public enum FilterType
        {
            MWM15_FILTER_TYPE_50HZ = 4,
            MWM15_FILTER_TYPE_60HZ = 5
        }


    }
}
