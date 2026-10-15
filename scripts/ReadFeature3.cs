using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public static class X13ReadFeature3 {
    [StructLayout(LayoutKind.Sequential)] struct Attributes { public int Size; public ushort Vid, Pid, Revision; }
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern SafeFileHandle CreateFile(string path,uint access,uint share,IntPtr security,uint disposition,uint flags,IntPtr template);
    [DllImport("hid.dll",SetLastError=true)] static extern bool HidD_GetAttributes(SafeFileHandle handle,ref Attributes attributes);
    [DllImport("hid.dll",SetLastError=true)] static extern bool HidD_GetFeature(SafeFileHandle handle,byte[] buffer,int size);
    public static byte[] Read(string path) {
        // Narrowly scope the only request implemented here. No SetFeature API.
        string lower=path.ToLowerInvariant();
        if(!lower.Contains("vid_1b80&pid_b412&mi_01&col03")) throw new ArgumentException("Unexpected HID collection");
        using(var handle=CreateFile(path,0,3,IntPtr.Zero,3,0,IntPtr.Zero)) {
            if(handle.IsInvalid) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            var attr=new Attributes(); attr.Size=Marshal.SizeOf(attr);
            if(!HidD_GetAttributes(handle,ref attr)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            if(attr.Vid!=0x1b80 || attr.Pid!=0xb412) throw new InvalidOperationException("Identity changed");
            byte[] data=new byte[40]; data[0]=3;
            if(!HidD_GetFeature(handle,data,data.Length)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            if(data[0]!=3) throw new InvalidOperationException("Unexpected report ID");
            return data;
        }
    }
}
