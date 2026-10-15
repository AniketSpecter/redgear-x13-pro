using System;
using System.Drawing;
using System.Windows.Forms;
public static class X13WindowsMousePanel {
    [STAThread] public static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new Panel());}
    sealed class Panel:Form {
        readonly NumericUpDown pointer=new NumericUpDown(),scroll=new NumericUpDown(),doubleClick=new NumericUpDown();
        readonly CheckBox precision=new CheckBox(),page=new CheckBox();readonly Label status=new Label();
        X13WindowsMouseSettings.Settings original;
        public Panel(){
            Text="X13 Pro - Windows mouse settings";ClientSize=new Size(520,380);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;
            Controls.Add(new Label{Text="These settings affect Windows for this user, including other mice.",Bounds=new Rectangle(20,15,480,35)});
            Row("Pointer speed (1-20)",pointer,1,20,60);Row("Scroll lines (0-100)",scroll,0,100,105);Row("Double-click interval (100-900 ms)",doubleClick,100,900,150);
            precision.Text="Enhance pointer precision";precision.SetBounds(20,205,450,25);Controls.Add(precision);
            page.Text="Scroll one screen at a time";page.SetBounds(20,240,450,25);Controls.Add(page);
            status.SetBounds(20,275,480,40);Controls.Add(status);
            var apply=new Button{Text="Apply",Bounds=new Rectangle(390,330,100,30)};apply.Click+=(s,e)=>Apply();Controls.Add(apply);
            var reload=new Button{Text="Reload",Bounds=new Rectangle(275,330,100,30)};reload.Click+=(s,e)=>{LoadValues();status.Text="Read current Windows settings.";};Controls.Add(reload);LoadValues();
        }
        void Row(string title,NumericUpDown field,int min,int max,int y){Controls.Add(new Label{Text=title,Bounds=new Rectangle(20,y,340,30)});field.Minimum=min;field.Maximum=max;field.SetBounds(370,y,120,30);Controls.Add(field);}
        void LoadValues(){original=X13WindowsMouseSettings.Read();pointer.Value=original.PointerSpeed;scroll.Value=Math.Min(100,original.ScrollLines);doubleClick.Value=Math.Min(900,Math.Max(100,original.DoubleClickMs));precision.Checked=original.EnhancePointerPrecision;page.Checked=original.PageScroll;}
        void Apply(){try{
            if((int)pointer.Value!=original.PointerSpeed)X13WindowsMouseSettings.Set("pointer",(int)pointer.Value);
            if(precision.Checked!=original.EnhancePointerPrecision)X13WindowsMouseSettings.Set("precision",precision.Checked?1:0);
            if(page.Checked){if(!original.PageScroll)X13WindowsMouseSettings.Set("page-scroll",1);}
            else if(original.PageScroll||(int)scroll.Value!=original.ScrollLines)X13WindowsMouseSettings.Set("scroll",(int)scroll.Value);
            if((int)doubleClick.Value!=original.DoubleClickMs)X13WindowsMouseSettings.Set("double-click",(int)doubleClick.Value);
            LoadValues();status.Text="Applied and verified Windows settings.";
        }catch(Exception e){status.Text=e.Message;}}
    }
}
