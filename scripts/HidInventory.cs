// Descriptor-only Windows HID discovery. No input/feature/output report APIs.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

public static class X13HidInventory {
    [StructLayout(LayoutKind.Sequential)] struct InterfaceData { public int Size; public Guid ClassGuid; public int Flags; public IntPtr Reserved; }
    [StructLayout(LayoutKind.Sequential)] struct Attributes { public int Size; public ushort VendorID, ProductID, VersionNumber; }
    [StructLayout(LayoutKind.Sequential)] struct Caps {
        public ushort Usage, UsagePage, InputReportByteLength, OutputReportByteLength, FeatureReportByteLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=17)] public ushort[] Reserved;
        public ushort NumberLinkCollectionNodes, NumberInputButtonCaps, NumberInputValueCaps, NumberInputDataIndices;
        public ushort NumberOutputButtonCaps, NumberOutputValueCaps, NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps, NumberFeatureValueCaps, NumberFeatureDataIndices;
    }
    public class Entry {
        public string Path, Product, Manufacturer, Error;
        public ushort VendorId, ProductId, DeviceRevision, UsagePage, Usage, InputBytes, OutputBytes, FeatureBytes;
        public int[] InputReportIds, OutputReportIds, FeatureReportIds;
    }
    [DllImport("hid.dll")] static extern void HidD_GetHidGuid(out Guid guid);
    [DllImport("setupapi.dll", SetLastError=true)] static extern IntPtr SetupDiGetClassDevs(ref Guid guid, IntPtr enumerator, IntPtr hwnd, uint flags);
    [DllImport("setupapi.dll", SetLastError=true)] static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr info, ref Guid guid, uint index, ref InterfaceData data);
    [DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref InterfaceData data, IntPtr detail, uint size, out uint needed, IntPtr info);
    [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DllImport("hid.dll")] static extern bool HidD_GetAttributes(SafeFileHandle file, ref Attributes attributes);
    [DllImport("hid.dll")] static extern bool HidD_GetProductString(SafeFileHandle file, byte[] buffer, uint length);
    [DllImport("hid.dll")] static extern bool HidD_GetManufacturerString(SafeFileHandle file, byte[] buffer, uint length);
    [DllImport("hid.dll")] static extern bool HidD_GetPreparsedData(SafeFileHandle file, out IntPtr data);
    [DllImport("hid.dll")] static extern bool HidD_FreePreparsedData(IntPtr data);
    [DllImport("hid.dll")] static extern int HidP_GetCaps(IntPtr data, out Caps caps);
    [DllImport("hid.dll")] static extern int HidP_GetButtonCaps(int type, IntPtr caps, ref ushort count, IntPtr data);
    [DllImport("hid.dll")] static extern int HidP_GetValueCaps(int type, IntPtr caps, ref ushort count, IntPtr data);
    static int[] ReportIds(IntPtr data, int type, ushort buttons, ushort values) {
        var ids = new SortedSet<int>();
        for (int kind=0; kind<2; kind++) {
            ushort count = kind==0 ? buttons : values;
            if (count==0) continue;
            // HIDP_BUTTON_CAPS and HIDP_VALUE_CAPS are both 72 bytes;
            // ReportID follows the initial USHORT UsagePage at byte offset 2.
            IntPtr buffer=Marshal.AllocHGlobal(72*count);
            try {
                int status=kind==0 ? HidP_GetButtonCaps(type,buffer,ref count,data) : HidP_GetValueCaps(type,buffer,ref count,data);
                if (status != 0x110000) throw new InvalidOperationException("HID parser status " + status);
                for (int i=0; i<count; i++) ids.Add(Marshal.ReadByte(buffer,72*i+2));
            } finally { Marshal.FreeHGlobal(buffer); }
        }
        var result=new int[ids.Count]; ids.CopyTo(result); return result;
    }
    public static Entry[] Enumerate() {
        Guid guid; HidD_GetHidGuid(out guid);
        IntPtr set=SetupDiGetClassDevs(ref guid,IntPtr.Zero,IntPtr.Zero,0x12);
        if (set==new IntPtr(-1)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        var entries=new List<Entry>();
        try {
            for (uint i=0;;i++) {
                var info=new InterfaceData(); info.Size=Marshal.SizeOf(info);
                if (!SetupDiEnumDeviceInterfaces(set,IntPtr.Zero,ref guid,i,ref info)) {
                    int error=Marshal.GetLastWin32Error(); if(error==259) break;
                    throw new System.ComponentModel.Win32Exception(error);
                }
                uint needed; SetupDiGetDeviceInterfaceDetail(set,ref info,IntPtr.Zero,0,out needed,IntPtr.Zero);
                IntPtr detail=Marshal.AllocHGlobal((int)needed);
                try {
                    Marshal.WriteInt32(detail,IntPtr.Size==8 ? 8 : 6);
                    if (!SetupDiGetDeviceInterfaceDetail(set,ref info,detail,needed,out needed,IntPtr.Zero)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                    var e=new Entry(); e.Path=Marshal.PtrToStringUni(IntPtr.Add(detail,4)); entries.Add(e);
                    using (var handle=CreateFile(e.Path,0,3,IntPtr.Zero,3,0,IntPtr.Zero)) {
                        if(handle.IsInvalid) { e.Error="Descriptor handle: " + Marshal.GetLastWin32Error(); continue; }
                        var attr=new Attributes(); attr.Size=Marshal.SizeOf(attr);
                        if(!HidD_GetAttributes(handle,ref attr)) { e.Error="Attributes: " + Marshal.GetLastWin32Error(); continue; }
                        e.VendorId=attr.VendorID; e.ProductId=attr.ProductID; e.DeviceRevision=attr.VersionNumber;
                        var text=new byte[512];
                        if(HidD_GetProductString(handle,text,(uint)text.Length)) e.Product=Encoding.Unicode.GetString(text).TrimEnd('\0');
                        Array.Clear(text,0,text.Length);
                        if(HidD_GetManufacturerString(handle,text,(uint)text.Length)) e.Manufacturer=Encoding.Unicode.GetString(text).TrimEnd('\0');
                        IntPtr preparsed;
                        if(!HidD_GetPreparsedData(handle,out preparsed)) { e.Error="Preparsed data unavailable"; continue; }
                        try {
                            Caps caps; int status=HidP_GetCaps(preparsed,out caps);
                            if(status!=0x110000) { e.Error="Caps: " + status; continue; }
                            e.UsagePage=caps.UsagePage; e.Usage=caps.Usage; e.InputBytes=caps.InputReportByteLength; e.OutputBytes=caps.OutputReportByteLength; e.FeatureBytes=caps.FeatureReportByteLength;
                            e.InputReportIds=ReportIds(preparsed,0,caps.NumberInputButtonCaps,caps.NumberInputValueCaps);
                            e.OutputReportIds=ReportIds(preparsed,1,caps.NumberOutputButtonCaps,caps.NumberOutputValueCaps);
                            e.FeatureReportIds=ReportIds(preparsed,2,caps.NumberFeatureButtonCaps,caps.NumberFeatureValueCaps);
                        } finally { HidD_FreePreparsedData(preparsed); }
                    }
                } finally { Marshal.FreeHGlobal(detail); }
            }
        } finally { SetupDiDestroyDeviceInfoList(set); }
        return entries.ToArray();
    }
}
