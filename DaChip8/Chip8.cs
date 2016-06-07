using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace DanTup.DaChip8
{
	class Chip8
	{
		Bitmap screen; // 64x32
		bool[,] buffer;

		// Registers
		byte[] V = new byte[16];
		// Timers
		byte Delay, Sound;
		// Address/Program Counters
		ushort I, PC = 0x200;
		// Stack
		byte SP;
		ushort[] Stack = new ushort[16];

		// Memory & ROM
		byte[] RAM = new byte[0x1000];

		// OpCodes
		Dictionary<byte, Action<OpCodeData>> opCodes;
		Dictionary<byte, Action<OpCodeData>> opCodesMisc;

		Random rnd = new Random();

		// Keys that are currently pressed.
		HashSet<byte> pressedKeys = new HashSet<byte>();

		public Chip8(Bitmap screen)
		{
			this.screen = screen;
			this.buffer = new bool[screen.Width, screen.Height];

			WriteFont();

			opCodes = new Dictionary<byte, Action<OpCodeData>>
			{
				{ 0x0, ClearOrReturn },
				{ 0x1, Jump },
				{ 0x2, CallSubroutine },
				{ 0x3, SkipIfXEqual },
				{ 0x4, SkipIfXNotEqual },
				{ 0x5, SkipIfXEqualY },
				{ 0x6, SetX },
				{ 0x7, AddX },
				{ 0x8, Arithmetic },
				{ 0x9, SkipIfXNotEqualY },
				{ 0xA, SetI },
				{ 0xB, JumpWithOffset },
				{ 0xC, Rnd },
				{ 0xD, DrawSprite },
				{ 0xE, SkipOnKey },
				{ 0xF, Misc },
			};

			opCodesMisc = new Dictionary<byte, Action<OpCodeData>>
			{
				{ 0x07, SetXToDelay },
				{ 0x0A, WaitForKey },
				{ 0x15, SetDelay },
				{ 0x18, SetSound },
				{ 0x1E, AddXToI },
				{ 0x29, SetIForChar },
				{ 0x33, BinaryCodedDecimal },
				{ 0x55, SaveX },
				{ 0x65, LoadX },
			};
		}

		void WriteFont()
		{
			var offset = 0x0;
			WriteFont(5 * offset++, Font.D0);
			WriteFont(5 * offset++, Font.D1);
			WriteFont(5 * offset++, Font.D2);
			WriteFont(5 * offset++, Font.D3);
			WriteFont(5 * offset++, Font.D4);
			WriteFont(5 * offset++, Font.D5);
			WriteFont(5 * offset++, Font.D6);
			WriteFont(5 * offset++, Font.D7);
			WriteFont(5 * offset++, Font.D8);
			WriteFont(5 * offset++, Font.D9);
			WriteFont(5 * offset++, Font.DA);
			WriteFont(5 * offset++, Font.DB);
			WriteFont(5 * offset++, Font.DC);
			WriteFont(5 * offset++, Font.DD);
			WriteFont(5 * offset++, Font.DE);
			WriteFont(5 * offset++, Font.DF);
		}

		void WriteFont(int address, long fontData)
		{
			// Fonts are 4-bits wide, but we need to write them a byte wide, so pad each "line".
			WriteFont(address + 0, (fontData & 0xF0000) >> 3);
			WriteFont(address + 1, (fontData & 0x0F000) >> 2);
			WriteFont(address + 2, (fontData & 0x00F00) >> 1);
			WriteFont(address + 3, (fontData & 0x000F0) >> 0);
			WriteFont(address + 4, (fontData & 0x0000F) << 1);
		}

		public void LoadProgram(byte[] data)
		{
			Array.Copy(data, 0, RAM, 0x200, data.Length);
		}

		public void Tick()
		{
			// Read the two bytes of OpCode (big endian).
			var opCode = (ushort)(RAM[PC++] << 8 | RAM[PC++]);

			Debug.WriteLine(PC.ToString("X4") + ": " + opCode.ToString("X4"));

			// Split data into the possible formats the instruction might need.
			// https://en.wikipedia.org/wiki/CHIP-8#Opcode_table
			var op = new OpCodeData()
			{
				OpCode = opCode,
				NNN = (ushort)(opCode & 0x0FFF),
				NN = (byte)(opCode & 0x00FF),
				N = (byte)(opCode & 0x000F),
				X = (byte)((opCode & 0x0F00) >> 8),
				Y = (byte)((opCode & 0x00F0) >> 4),
			};

			// Loop up the OpCode using the first nibble and execute.
			opCodes[(byte)(opCode >> 12)](op);
		}

		public void Tick60Hz()
		{
			Debug.WriteLine("60Hz tick");
			if (Delay > 0)
				Delay--;
			if (Sound > 0)
				Sound--;

			UpdateScreen();
		}

		void UpdateScreen()
		{
			using (var g = Graphics.FromImage(screen))
				g.Clear(Color.Black);

			for (var x = 0; x < screen.Width; x++)
			{
				for (var y = 0; y < screen.Height; y++)
				{
					if (buffer[x, y])
						screen.SetPixel(x, y, Color.White);
				}
			}
		}

		// Misc has its own dictionary because it's full of random stuff.
		void Misc(OpCodeData data) => opCodesMisc[data.NN](data);

		public void KeyDown(byte key) => pressedKeys.Add(key);
		public void KeyUp(byte key) => pressedKeys.Remove(key);

		// http://devernay.free.fr/hacks/chip8/C8TECH10.HTM#3.1

		/// <summary>
		/// Handles 0x0... which either clears the screen or returns from a subroutine.
		/// </summary>
		void ClearOrReturn(OpCodeData data)
		{
			if (data.NN == 0xE0)
			{
				using (var g = Graphics.FromImage(screen))
					g.Clear(Color.Black);
			}
			else if (data.NN == 0xEE)
				PC = Pop();
		}

		/// <summary>
		/// Jumps to location nnn (not a subroutine, so old PC is not pushed to the stack).
		/// </summary>
		void Jump(OpCodeData data) => PC = data.NNN;

		/// <summary>
		/// Jumps to location nnn + v[0] (not a subroutine, so old PC is not pushed to the stack).
		/// </summary>
		void JumpWithOffset(OpCodeData data) => PC = (ushort)(data.NNN + V[0]);

		/// <summary>
		/// Jumps to subroutine nnn (unlike Jump, this pushes the previous PC to the stack to allow return).
		/// </summary>
		void CallSubroutine(OpCodeData data)
		{
			Push(PC);
			PC = data.NNN;
		}

		/// <summary>
		/// Skips the next instruction (two bytes) if V[x] == nn.
		/// </summary>
		void SkipIfXEqual(OpCodeData data)
		{
			if (V[data.X] == data.NN)
				PC += 2;
		}

		/// <summary>
		/// Skips the next instruction (two bytes) if V[x] != nn.
		/// </summary>
		void SkipIfXNotEqual(OpCodeData data)
		{
			if (V[data.X] != data.NN)
				PC += 2;
		}

		/// <summary>
		/// Skips the next instruction (two bytes) if V[x] == V[y].
		/// </summary>
		void SkipIfXEqualY(OpCodeData data)
		{
			if (V[data.X] == V[data.Y])
				PC += 2;
		}

		/// <summary>
		/// Skips the next instruction (two bytes) if V[x] != V[y].
		/// </summary>
		void SkipIfXNotEqualY(OpCodeData data)
		{
			if (V[data.X] != V[data.Y])
				PC += 2;
		}

		/// <summary>
		/// Sets V[x] == nn.
		/// </summary>
		void SetX(OpCodeData data)
		{
			V[data.X] = data.NN;
		}

		/// <summary>
		/// Adds nn to V[x].
		/// </summary>
		void AddX(OpCodeData data)
		{
			V[data.X] += data.NN; // TODO: Do we need to handle overflow?
		}

		/// <summary>
		/// Sets V[x] to V[y].
		/// </summary>
		void Arithmetic(OpCodeData data)
		{
			switch (data.N)
			{
				case 0x0:
					V[data.X] = V[data.Y];
					break;
				case 0x1:
					V[data.X] |= V[data.Y];
					break;
				case 0x2:
					V[data.X] &= V[data.Y];
					break;
				case 0x3:
					V[data.X] ^= V[data.Y];
					break;
				case 0x4:
					V[0xF] = (byte)(V[data.X] + V[data.Y] > 0xFF ? 1 : 0); // Set flag if we overflowed.
					V[data.X] += V[data.Y];
					break;
				case 0x5:
					V[0xF] = (byte)(V[data.X] > V[data.Y] ? 1 : 0); // Set flag if we underflowed.
					V[data.X] -= V[data.Y];
					break;
				case 0x6:
					V[0xF] = (byte)((V[data.X] & 0x1) != 0 ? 1 : 0); // Set flag if we shifted a 1 off the end.
					V[data.X] /= 2; // Shift right.
					break;
				case 0x7: // Note: This is Y-X, 5 was X-Y.
					V[0xF] = (byte)(V[data.Y] > V[data.X] ? 1 : 0); // Set flag if we underflowed.
					V[data.Y] -= V[data.X];
					break;
				case 0xE:
					V[0xF] = (byte)((V[data.X] & 0xF) != 0 ? 1 : 0); // Set flag if we shifted a 1 off the end.
					V[data.X] *= 2; // Shift left.
					break;
			}
		}

		/// <summary>
		/// Sets the I register.
		/// </summary>
		void SetI(OpCodeData data) => I = data.NNN;

		/// <summary>
		/// ANDs a random number with nn and stores in V[x].
		/// </summary>
		void Rnd(OpCodeData data) => V[data.X] = (byte)(rnd.Next(0, 256) & data.NN);

		/// <summary>
		/// Draws an n-byte sprite from register I at V[x], V[y]. Sets V[0xF] if it collides.
		/// </summary>
		void DrawSprite(OpCodeData data)
		{
			for (var i = 0; i < data.N; i++)
			{
				var spriteLine = RAM[I + i]; // A line of the sprite to render

				for (var bit = 0; bit < 8; bit++)
				{
					var x = (data.X + bit) % screen.Width;
					var y = (data.Y + i) % screen.Height;

					var spriteBit = ((spriteLine >> bit) & 1);
					var oldBit = buffer[x, y] ? 1 : 0;

					// New bit is XOR of existing and new.
					var newBit = oldBit ^ spriteBit;

					buffer[data.X + bit, data.Y + i] = newBit != 0;

					// If we wiped out a pixel, set flag for collission.
					if (oldBit != 0 && newBit == 0)
						V[0xF] = 1;
				}
			}
		}

		/// <summary>
		/// Skips th enext instruction based on the key at V[x] being pressed/not pressed.
		/// </summary>
		void SkipOnKey(OpCodeData data)
		{
			if (
				(data.NN == 0x9E && pressedKeys.Contains(V[data.X])) // 9E = IfKeyPressed
				|| (data.NN == 0xA1 && !pressedKeys.Contains(V[data.X])) // A1 = IfKeyNotPressed
			)
				PC += 2;
		}

		/// <summary>
		/// Waits for a key to be pressed by looping at the current instruction.
		/// </summary>
		void WaitForKey(OpCodeData data)
		{
			// If we have a key pressed, store it and more on.
			if (pressedKeys.Count != 0)
				V[data.X] = pressedKeys.GetEnumerator().Current;
			else
				// Otherwise, wind the PC back so we will keep executing this instruction.
				PC -= 2;
		}

		/// <summary>
		/// Sets V[x] to equal the Delay register.
		/// </summary>
		void SetXToDelay(OpCodeData data) => V[data.X] = Delay;

		/// <summary>
		/// Sets the delay register to V[x].
		/// </summary>
		void SetDelay(OpCodeData data) => Delay = V[data.X];

		/// <summary>
		/// Sets the sound register to V[x].
		/// </summary>
		void SetSound(OpCodeData data) => Sound = V[data.X];

		/// <summary>
		/// Adds V[x] to register I.
		/// </summary>
		void AddXToI(OpCodeData data) => I += V[data.X];

		/// <summary>
		/// Sets I to the correct location of the font sprite V[x].
		/// Each font sprite is 5 bytes long.
		/// </summary>
		void SetIForChar(OpCodeData data)
		{
			I = (ushort)(V[data.X] * 5); // 0 is at 0x0, 1 is at 0x5, ...
		}

		/// <summary>
		/// Takes the decimal representation of V[x] and puts each character into memory locations
		/// starting at I (with a maximum of 3).
		/// </summary>
		void BinaryCodedDecimal(OpCodeData data)
		{
			RAM[I + 0] = (byte)((V[data.X] / 100) % 10);
			RAM[I + 1] = (byte)((V[data.X] / 10) % 10);
			RAM[I + 2] = (byte)(V[data.X] % 10);
		}

		/// <summary>
		/// Saves all registers to the address in register I.
		/// </summary>
		void SaveX(OpCodeData data)
		{
			for (var i = 0; i < 16; i++)
				RAM[I + i] = V[i];
		}

		/// <summary>
		/// Loads all registers from the address in register I.
		/// </summary>
		void LoadX(OpCodeData data)
		{
			for (var i = 0; i < 16; i++)
				V[i] = RAM[I + i];
		}

		/// <summary>
		/// Pushes a 16-bit value onto the stack, incrementing the SP.
		/// </summary>
		void Push(ushort value) => Stack[SP++] = value;

		/// <summary>
		/// Retrieves a 16-bit value from the stack, decrementing the SP.
		/// </summary>
		ushort Pop() => Stack[--SP];
	}
}
