// Narrow acceptance test: one DPI/rate byte, immediate read-back, then restore.
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public static class X13SettingTrial {
    [StructLayout(LayoutKind.Sequential)] struct Attributes { public int Size; public ushort Vid, Pid, Revision; }
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern SafeFileHandle CreateFile(string path,uint access,uint share,IntPtr security,uint disposition,uint flags,IntPtr template);
    [DllImport("hid.dll",SetLastError=true)] static extern bool HidD_GetAttributes(SafeFileHandle handle,ref Attributes attributes);
    [DllImport("hid.dll",SetLastError=true)] static extern bool HidD_GetFeature(SafeFileHandle handle,byte[] buffer,int size);
    [DllImport("hid.dll",SetLastError=true)] static extern bool HidD_SetFeature(SafeFileHandle handle,byte[] buffer,int size);
    public class Result {
        public string Feature, OriginalHex, TargetHex, ReadBackHex, RestoredHex;
        public bool Executed, SetAccepted, ReadBackMatched, RestoreAccepted, RestoreMatched;
        public string Failure;
    }
    static byte[] Read(SafeFileHandle h) {
        var b=new byte[40]; b[0]=3;
        if(!HidD_GetFeature(h,b,40)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        if(b[0]!=3 || b[1]>6 || b[2]>3) throw new InvalidOperationException("Unrecognized settings frame");
        return b;
    }
    static string Hex(byte[] b) { return BitConverter.ToString(b).Replace("-","").ToLowerInvariant(); }
    static bool Same(byte[] a,byte[] b) { return Hex(a)==Hex(b); }
    public static Result SetStaticColor(string path,string color) {
        if(!System.Text.RegularExpressions.Regex.IsMatch(color,"^#[0-9a-fA-F]{6}$")) throw new ArgumentException("Expected #rrggbb");
        if(!path.ToLowerInvariant().Contains("vid_1b80&pid_b412&mi_01&col03")) throw new ArgumentException("Unexpected HID collection");
        using(var h=CreateFile(path,0,3,IntPtr.Zero,3,0,IntPtr.Zero)) {
            if(h.IsInvalid) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            var attr=new Attributes(); attr.Size=Marshal.SizeOf(attr);
            if(!HidD_GetAttributes(h,ref attr) || attr.Vid!=0x1b80 || attr.Pid!=0xb412) throw new InvalidOperationException("Identity changed");
            var original=Read(h); var target=(byte[])original.Clone(); target[3]=(byte)(original[3]&0x0e);
            // Exact-device visible trials confirmed R,B,G, not the vendor UI's R,G,B.
            target[5]=Convert.ToByte(color.Substring(1,2),16);
            target[6]=Convert.ToByte(color.Substring(5,2),16);
            target[7]=Convert.ToByte(color.Substring(3,2),16);
            var result=new Result {Feature="static-color",OriginalHex=Hex(original),TargetHex=Hex(target),Executed=true};
            result.SetAccepted=Same(original,target) || HidD_SetFeature(h,target,40);
            if(!result.SetAccepted) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            System.Threading.Thread.Sleep(200); var reply=Read(h); result.ReadBackHex=Hex(reply); result.ReadBackMatched=Same(reply,target);
            if(!result.ReadBackMatched) result.Failure="Static colour read-back mismatch";
            return result;
        }
    }
    public static Result[] DiagnoseColors(string path,bool execute) {
        if(!path.ToLowerInvariant().Contains("vid_1b80&pid_b412&mi_01&col03")) throw new ArgumentException("Unexpected HID collection");
        using(var h=CreateFile(path,0,3,IntPtr.Zero,3,0,IntPtr.Zero)) {
            if(h.IsInvalid) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            var attr=new Attributes(); attr.Size=Marshal.SizeOf(attr);
            if(!HidD_GetAttributes(h,ref attr) || attr.Vid!=0x1b80 || attr.Pid!=0xb412) throw new InvalidOperationException("Identity changed");
            var original=Read(h); var results=new System.Collections.Generic.List<Result>();
            string failure=null;
            try {
                for(int channel=0;channel<3;channel++) {
                    var target=(byte[])original.Clone(); target[3]=(byte)(original[3]&0x0e);
                    target[5]=0; target[6]=0; target[7]=0; target[5+channel]=255;
                    var row=new Result {Feature="lighting-native-byte-"+(5+channel),OriginalHex=Hex(original),TargetHex=Hex(target),Executed=execute};
                    results.Add(row);
                    if(!execute) continue;
                    Console.WriteLine("Lighting diagnostic "+(channel+1)+"/3: native byte "+(5+channel)+" only; observe for eight seconds.");
                    row.SetAccepted=HidD_SetFeature(h,target,40);
                    if(!row.SetAccepted) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                    System.Threading.Thread.Sleep(150);
                    var reply=Read(h); row.ReadBackHex=Hex(reply); row.ReadBackMatched=Same(reply,target);
                    if(!row.ReadBackMatched) throw new InvalidOperationException("Diagnostic read-back mismatch");
                    System.Threading.Thread.Sleep(8000);
                }
            } catch(Exception e) { failure=e.Message; }
            finally {
                if(execute) {
                    bool accepted=HidD_SetFeature(h,original,40); bool matched=false; string hex=null;
                    try { System.Threading.Thread.Sleep(200); var restored=Read(h); hex=Hex(restored); matched=Same(original,restored); }
                    catch(Exception e) { failure=(failure??"")+"; restoration read: "+e.Message; }
                    foreach(var row in results) { row.RestoreAccepted=accepted; row.RestoreMatched=matched; row.RestoredHex=hex; row.Failure=failure; }
                    Console.WriteLine("Original complete lighting report restored; read-back matched: "+matched);
                }
            }
            return results.ToArray();
        }
    }
    public static Result Set(string path,string feature,int value) {
        if(!path.ToLowerInvariant().Contains("vid_1b80&pid_b412&mi_01&col03")) throw new ArgumentException("Unexpected HID collection");
        int offset; int code;
        if(feature=="dpi") { offset=1; code=Array.IndexOf(new int[]{400,800,1200,2400,3200,4000,4800},value); }
        else if(feature=="polling") { offset=2; code=Array.IndexOf(new int[]{125,250,500,1000},value); }
        else throw new ArgumentException("Unsupported setting");
        if(code<0) throw new ArgumentException("Unsupported setting value");
        using(var h=CreateFile(path,0,3,IntPtr.Zero,3,0,IntPtr.Zero)) {
            if(h.IsInvalid) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            var attr=new Attributes(); attr.Size=Marshal.SizeOf(attr);
            if(!HidD_GetAttributes(h,ref attr) || attr.Vid!=0x1b80 || attr.Pid!=0xb412) throw new InvalidOperationException("Identity changed");
            var original=Read(h); var target=(byte[])original.Clone(); target[offset]=(byte)code;
            var result=new Result {Feature=feature,OriginalHex=Hex(original),TargetHex=Hex(target),Executed=true};
            result.SetAccepted=Same(original,target) || HidD_SetFeature(h,target,40);
            if(!result.SetAccepted) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            System.Threading.Thread.Sleep(200);
            var reply=Read(h); result.ReadBackHex=Hex(reply); result.ReadBackMatched=Same(reply,target);
            if(!result.ReadBackMatched) result.Failure="Read-back mismatch; inspect mouse settings before retrying";
            return result;
        }
    }
    public static Result Run(string path,string feature,bool execute) {
        if(!path.ToLowerInvariant().Contains("vid_1b80&pid_b412&mi_01&col03")) throw new ArgumentException("Unexpected HID collection");
        int offset, originalCode, targetCode;
        if(feature=="dpi") { offset=1; originalCode=5; targetCode=3; }
        else if(feature=="polling") { offset=2; originalCode=3; targetCode=2; }
        else if(feature=="lighting") { offset=3; originalCode=0x31; targetCode=0; }
        else throw new ArgumentException("Only DPI 4000->2400->4000 or polling 1000->500->1000 trials exist");
        using(var h=CreateFile(path,0,3,IntPtr.Zero,3,0,IntPtr.Zero)) {
            if(h.IsInvalid) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            var attr=new Attributes(); attr.Size=Marshal.SizeOf(attr);
            if(!HidD_GetAttributes(h,ref attr) || attr.Vid!=0x1b80 || attr.Pid!=0xb412) throw new InvalidOperationException("Device identity changed");
            var original=Read(h);
            if(original[offset]!=originalCode) throw new InvalidOperationException("Original setting changed; trial refused");
            var target=(byte[])original.Clone(); target[offset]=(byte)targetCode;
            if(feature=="lighting") { target[3]=(byte)(original[3]&0x0e); target[5]=0; target[6]=255; target[7]=0; }
            var result=new Result {Feature=feature,OriginalHex=Hex(original),TargetHex=Hex(target),Executed=execute};
            if(!execute) return result;
            // Caller must obtain explicit first-write approval before execute=true.
            try {
                result.SetAccepted=HidD_SetFeature(h,target,40);
                if(!result.SetAccepted) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                System.Threading.Thread.Sleep(200);
                var reply=Read(h); result.ReadBackHex=Hex(reply); result.ReadBackMatched=Same(reply,target);
                if(!result.ReadBackMatched) throw new InvalidOperationException("Read-back differed from requested settings");
                System.Threading.Thread.Sleep(feature=="lighting" ? 5000 : 1000);
            } catch(Exception e) { result.Failure=e.Message; }
            finally {
                // Restore the approved, freshly read original frame even if read-back failed.
                result.RestoreAccepted=HidD_SetFeature(h,original,40);
                try { System.Threading.Thread.Sleep(200); var restored=Read(h); result.RestoredHex=Hex(restored); result.RestoreMatched=Same(original,restored); }
                catch(Exception e) { result.Failure=(result.Failure??"")+"; restoration read: "+e.Message; }
            }
            return result;
        }
    }
}
