using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace DanTup.DaChip8
{
	public partial class Screen : Form
	{
		readonly Chip8 chip8;
		readonly Bitmap screen;
		readonly string ROM = "../../../ROMs/Chip-8 Pack/Chip-8 Games/Pong (1 player).ch8";

		// For timing..
		readonly Stopwatch stopWatch = Stopwatch.StartNew();
		readonly TimeSpan targetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
		TimeSpan lastTime;

		// Currently held keys


		public Screen()
		{
			InitializeComponent();

			screen = new Bitmap(64, 32, PixelFormat.Format1bppIndexed);
			chip8 = new Chip8(screen);
			chip8.LoadProgram(File.ReadAllBytes(ROM));

			Application.Idle += IdleTick;
			KeyDown += SetKeyDown;
			KeyUp += SetKeyUp;
		}

		Dictionary<Keys, byte> keyMapping = new Dictionary<Keys, byte>
		{
			{ Keys.D1, 0x1 },
			{ Keys.D2, 0x2 },
			{ Keys.D3, 0x3 },
			{ Keys.D4, 0xC },
			{ Keys.Q, 0x4 },
			{ Keys.W, 0x5 },
			{ Keys.E, 0x6 },
			{ Keys.R, 0xD },
			{ Keys.A, 0x7 },
			{ Keys.S, 0x8 },
			{ Keys.D, 0x9 },
			{ Keys.F, 0xE },
			{ Keys.Z, 0xA },
			{ Keys.X, 0x0 },
			{ Keys.C, 0xB },
			{ Keys.V, 0xF },
		};

		void SetKeyDown(object sender, KeyEventArgs e)
		{
			if (keyMapping.ContainsKey(e.KeyCode))
				chip8.KeyDown(keyMapping[e.KeyCode]);
		}

		void SetKeyUp(object sender, KeyEventArgs e)
		{
			if (keyMapping.ContainsKey(e.KeyCode))
				chip8.KeyUp(keyMapping[e.KeyCode]);
		}

		void IdleTick(object sender, EventArgs e)
		{
			var currentTime = stopWatch.Elapsed;
			var elapsedTime = currentTime - lastTime;

			while (elapsedTime >= targetElapsedTime)
			{
				Tick60Hz();
				elapsedTime -= targetElapsedTime;
				lastTime += targetElapsedTime;
			}
			Tick();

			Invalidate();
		}

		void Tick() => chip8.Tick();
		void Tick60Hz() => chip8.Tick60Hz();
	}
}
