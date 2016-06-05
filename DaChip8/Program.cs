using System;
using System.Windows.Forms;

namespace DanTup.DaChip8
{
	static class Program
	{
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Screen());
		}
	}
}
