// Documented Windows user32 settings; affects this user's system, not mouse firmware.
using System;
using System.Runtime.InteropServices;
public static class X13WindowsMouseSettings {
    [DllImport("user32.dll",EntryPoint="SystemParametersInfoW",SetLastError=true)] static extern bool SpiGet(uint action,uint param,[In,Out]int[] value,uint flags);
    [DllImport("user32.dll",EntryPoint="SystemParametersInfoW",SetLastError=true)] static extern bool SpiPtr(uint action,uint param,IntPtr value,uint flags);
    [DllImport("user32.dll")] static extern uint GetDoubleClickTime();
    [DllImport("user32.dll",SetLastError=true)] static extern bool SetDoubleClickTime(uint ms);
    public class Settings {public int PointerSpeed,Threshold1,Threshold2,Acceleration,ScrollLines,DoubleClickMs;public bool PageScroll,EnhancePointerPrecision;}
    static int[] Get(uint action,int count){var data=new int[count];Check(SpiGet(action,0,data,0));return data;}
    static void Check(bool ok){if(!ok)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}
    public static Settings Read(){var mouse=Get(3,3);int lines=Get(0x68,1)[0];return new Settings{PointerSpeed=Get(0x70,1)[0],Threshold1=mouse[0],Threshold2=mouse[1],Acceleration=mouse[2],EnhancePointerPrecision=mouse[2]!=0,ScrollLines=lines<0?3:lines,PageScroll=lines==-1,DoubleClickMs=(int)GetDoubleClickTime()};}
    public static Settings Set(string field,int value){
        var old=Read();
        switch(field){
        case "pointer":if(value<1||value>20)throw new ArgumentOutOfRangeException("value");Check(SpiPtr(0x71,0,new IntPtr(value),3));break;
        case "precision":if(value!=0&&value!=1)throw new ArgumentOutOfRangeException("value");Check(SpiGet(4,0,new int[]{old.Threshold1,old.Threshold2,value},3));break;
        case "scroll":if(value<0||value>100)throw new ArgumentOutOfRangeException("value");Check(SpiPtr(0x69,(uint)value,IntPtr.Zero,3));break;
        case "page-scroll":if(value!=0&&value!=1)throw new ArgumentOutOfRangeException("value");Check(SpiPtr(0x69,value==1?UInt32.MaxValue:(uint)old.ScrollLines,IntPtr.Zero,3));break;
        case "double-click":if(value<100||value>900)throw new ArgumentOutOfRangeException("value");Check(SetDoubleClickTime((uint)value));break;
        default:throw new ArgumentException("Unsupported Windows mouse setting");
        }
        var actual=Read();
        bool same=field=="pointer"?actual.PointerSpeed==value:field=="precision"?actual.EnhancePointerPrecision==(value==1):field=="scroll"?!actual.PageScroll&&actual.ScrollLines==value:field=="page-scroll"?actual.PageScroll==(value==1):actual.DoubleClickMs==value;
        if(!same)throw new InvalidOperationException("Windows setting read-back mismatch");return actual;
    }
}
