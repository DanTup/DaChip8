using System.Drawing;

namespace DanTup.DaChip8
{
	class Chip8
	{
		Bitmap screen; // 64x32

		// Registers
		byte V0, V1, V2, V3, V4, V5, V6, V7, V8, V9, VA, VB, VC, VD, VE, VF;
		byte SP, Delay, Sound;
		ushort I, PC;
		ushort[] Stack = new ushort[16];
		byte[] RAM = new byte[0x1000];

		public Chip8(Bitmap screen)
		{
			this.screen = screen;
		}

		public void Tick()
		{

		}
	}
}
