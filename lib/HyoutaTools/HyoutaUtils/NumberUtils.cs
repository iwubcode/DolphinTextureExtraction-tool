using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyoutaUtils {
	public static class NumberUtils {
		public static uint ToUInt24( byte[] file, int location ) {
			byte b1 = file[location];
			byte b2 = file[location + 1];
			byte b3 = file[location + 2];

			return (uint)( b3 << 16 | b2 << 8 | b1 );
		}

		public static byte[] GetBytesForUInt24( uint number ) {
			byte[] b = new byte[3];
			b[0] = (byte)( number & 0xFF );
			b[1] = (byte)( ( number >> 8 ) & 0xFF );
			b[2] = (byte)( ( number >> 16 ) & 0xFF );
			return b;
		}

		/// <summary>
		/// converts a 32-bit int that's actually a byte representation of a float
		/// to an actual float for use in calculations or whatever
		/// </summary>
		public static float UIntToFloat( this uint integer ) {
			byte[] b = BitConverter.GetBytes( integer );
			float f = BitConverter.ToSingle( b, 0 );
			return f;
		}

		public static int Align(this int number, int alignment, long offset = 0) {
			return (int)Align((uint)number, (uint)alignment, (ulong)offset);
		}

		public static uint Align(this uint number, int alignment, ulong offset = 0) {
			return Align(number, (uint)alignment, offset);
		}

		public static uint Align(this uint number, uint alignment, ulong offset = 0) {
			uint diff = (uint)((number - offset) % alignment);
			if (diff == 0) {
				return number;
			} else {
				return (number + (alignment - diff));
			}
		}

		public static long Align(this long number, long alignment, long offset = 0) {
			return (long)Align((ulong)number, (ulong)alignment, (ulong)offset);
		}

		public static ulong Align(this ulong number, long alignment, ulong offset = 0) {
			return Align(number, (ulong)alignment, offset);
		}

		public static ulong Align(this ulong number, ulong alignment, ulong offset = 0) {
			ulong diff = (number - offset) % alignment;
			if (diff == 0) {
				return number;
			} else {
				return (number + (alignment - diff));
			}
		}

		public static sbyte AsSigned( this byte number ) {
			return (sbyte)number;
		}

		public static short AsSigned( this ushort number ) {
			return (short)number;
		}

		public static int AsSigned( this uint number ) {
			return (int)number;
		}

		public static long AsSigned( this ulong number ) {
			return (long)number;
		}
	}
}
