namespace DanTup.DaChip8
{
	static class Font
	{
		// Fonts are 4x5. Each line must be a byte wide so is padded with 0x00.
		// Eg:
		//     0:
		//         ####  =  1111  =  F0
		//         #  #  =  1001  =  90
		//         #  #  =  1001  =  90
		//         #  #  =  1001  =  90
		//         ####  =  1111  =  F0

		public const long D0 = 0xF0909090F0;
		public const long D1 = 0x2060202070;
		public const long D2 = 0xF010F080F0;
		public const long D3 = 0xF010F010F0;
		public const long D4 = 0x9090F01010;
		public const long D5 = 0xF080F010F0;
		public const long D6 = 0xF080F090F0;
		public const long D7 = 0xF010204040;
		public const long D8 = 0xF090F090F0;
		public const long D9 = 0xF090F010F0;
		public const long DA = 0xF090F09090;
		public const long DB = 0xE090E090E0;
		public const long DC = 0xF0808080F0;
		public const long DD = 0xE0909090E0;
		public const long DE = 0xF080F080F0;
		public const long DF = 0xF080F08080;

	}
}
