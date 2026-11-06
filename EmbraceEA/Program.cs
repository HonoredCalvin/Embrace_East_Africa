using System;
using System.Windows.Forms;

namespace EmbraceEA
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Db.Init();
            Application.Run(new MainForm());
        }
    }
}
