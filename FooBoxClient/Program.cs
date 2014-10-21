using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FooBoxClient
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
          //  if (Properties.Settings.Default.ID == 0)
         //   {
            FormStart frm = null;

            if (Properties.Settings.Default.UserID == 0)
            {
                Properties.Settings.Default.Reset();
                frm = new FormStart(FormWindowState.Normal);
            }
            else { 
                frm = new FormStart(FormWindowState.Minimized);
            }
            
            Application.Run(frm);
               // Application.Run(frm);
        /*    }
            else
            {
                Application.Run(new FormSysTray());
            }*/
           
        }
    }
}
