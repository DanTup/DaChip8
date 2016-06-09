using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DanTup.DaChip8
{
	public partial class Screen : Form
	{
		readonly Chip8 chip8;
		readonly Bitmap screen;
		readonly string ROM = "../../../ROMs/Chip-8 Pack/Chip-8 Games/Breakout (Brix hack) [David Winter, 1997].ch8";

		// For timing..
		readonly Stopwatch stopWatch = Stopwatch.StartNew();
		readonly TimeSpan targetElapsedTime60Hz = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
		readonly TimeSpan targetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 1000);
		TimeSpan lastTime;

		public Screen()
		{
			InitializeComponent();

			screen = new Bitmap(64, 32);
			pbScreen.Image = screen;

			chip8 = new Chip8(screen);
			chip8.LoadProgram(File.ReadAllBytes(ROM));

			KeyDown += SetKeyDown;
			KeyUp += SetKeyUp;
		}

		protected override void OnLoad(EventArgs e)
		{
			StartGameLoop();
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

		void StartGameLoop()
		{
			Task.Run(GameLoop);
		}

		Task GameLoop()
		{
			while (true)
			{
				var currentTime = stopWatch.Elapsed;
				var elapsedTime = currentTime - lastTime;

				while (elapsedTime >= targetElapsedTime60Hz)
				{
					this.Invoke((Action)Tick60Hz);
					elapsedTime -= targetElapsedTime60Hz;
					lastTime += targetElapsedTime60Hz;
				}

				this.Invoke((Action)Tick);

				Thread.Sleep(targetElapsedTime);
			}
		}

		void Tick() => chip8.Tick();
		void Tick60Hz()
		{
			chip8.Tick60Hz();
			pbScreen.Refresh();
		}
	}
}
