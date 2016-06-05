using System;
using System.Collections.Generic;
using System.Drawing;

namespace DanTup.DaChip8
{
	class Chip8
	{
		Bitmap screen; // 64x32

		// Registers
		byte[] V = new byte[16];
		// Timers
		byte Delay, Sound;
		// Address/Program Counters
		ushort I, PC;
		// Stack
		byte SP;
		ushort[] Stack = new ushort[16];

		// Memory & ROM
		byte[] RAM = new byte[0x1000];
		byte[] Program;

		// OpCodes
		Dictionary<byte, Action<OpCodeData>> opCodes;

		public Chip8(Bitmap screen)
		{
			this.screen = screen;

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
		}

		public void LoadProgram(byte[] data)
		{
			Program = data;
		}

		public void Tick()
		{
			// Read the two bytes of OpCode (big endian).
			var opCode = (ushort)(Program[PC++] << 8 | Program[PC++]);

			// Split data into the possible formats the instruction might need.
			// https://en.wikipedia.org/wiki/CHIP-8#Opcode_table
			var op = new OpCodeData()
			{
				OpCode = opCode,
				NNN = (ushort)(opCode & 0x0FFFF),
				X = (byte)(opCode & 0x0F00 >> 8),
				Y = (byte)(opCode & 0x00F0 >> 4),
				N = (byte)(opCode & 0x000F)
			};

			// Loop up the OpCode using the first nibble and execute.
			opCodes[(byte)(opCode >> 12)](op);
		}

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

		void SetI(OpCodeData data) { }

		void JumpWithOffset(OpCodeData data) { }
		void Rnd(OpCodeData data) { }
		void DrawSprite(OpCodeData data) { }
		void SkipOnKey(OpCodeData data) { }
		void Misc(OpCodeData data) { }

		void Push(ushort value) => Stack[SP++] = value;
		ushort Pop() => Stack[SP--];
	}
}
