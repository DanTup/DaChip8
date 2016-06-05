using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace DanTup.DaChip8
{
	public partial class Screen : Form
	{
		Chip8 chip8;
		Bitmap screen;

		public Screen()
		{
			InitializeComponent();

			screen = new Bitmap(64, 32, PixelFormat.Format1bppIndexed);
			chip8 = new Chip8(screen);
		}
	}
}
