using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Runtime.InteropServices;

namespace TeruTeruServer.ManageLogic.Util
{


    public static class MarshalUtil
    {
        public static byte[] Serialize<T>(T data)
        {
            int size = Marshal.SizeOf(data);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        public static T Deserialize<T>(byte[] arr, int startIndex = 0)
        {
            int size = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(arr, startIndex, ptr, size);
            T data = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);
            return data;
        }
    }

}
