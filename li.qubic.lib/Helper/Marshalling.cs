using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace li.qubic.lib.Helper
{
    public static class Marshalling
    {
        public static byte[] Serialize<T>(T structure, int? fixedSize = null)
            where T : struct
        {
            var size = fixedSize ?? Marshal.SizeOf(typeof(T));
            using (var handle = new ByteArraySafeHandle(Marshal.AllocHGlobal(size), true))
            {
                var array = new byte[size];
                var ptr = handle.DangerousGetHandle();
                Marshal.StructureToPtr(structure, ptr, true);
                Marshal.Copy(ptr, array, 0, size);
                return array;
            }
        }

        public static T Deserialize<T>(byte[] array, int skip = 0, int? fixedSize = null)
            where T : struct
        {
            var size = fixedSize ?? Marshal.SizeOf(typeof(T));
            using (var handle = new ByteArraySafeHandle(Marshal.AllocHGlobal(size), true))
            {
                Marshal.Copy(array, skip, handle.DangerousGetHandle(), size);
                var s = (T)Marshal.PtrToStructure(handle.DangerousGetHandle(), typeof(T));
                return s;
            }
        }

        public class ByteArraySafeHandle : SafeHandle
        {
            public ByteArraySafeHandle(nint ptr, bool ownsHandle) : base(ptr, ownsHandle) { }

            public override bool IsInvalid => handle == nint.Zero;

            protected override bool ReleaseHandle()
            {
                Marshal.FreeHGlobal(handle);
                return true;
            }
        }
    }
}
