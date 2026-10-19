using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using FmOK6lC4PapVZikenVa;
using bPdenNCsqhpEJR2KolP;
using juFYQUCAOyYDwCS3w11;
using sRpiR1CI2v3uIudXaGA;

namespace mjegmktKLOyWiljG1nX;

internal class fJUeITteiwtDJsNdQol
{
	private delegate void wgauEopyO7v8766fHt9(object o);

	internal class O3Lf19pn52eD373nOWl : Attribute
	{
		internal class H4f5UUpMQJCrm2sAC3e<mXspp2p62NZcYD8UCjo>
		{
			internal static object DAXr96VSJDtmStFNq4f;

			[MethodImpl(MethodImplOptions.NoInlining)]
			public H4f5UUpMQJCrm2sAC3e()
			{
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			static H4f5UUpMQJCrm2sAC3e()
			{
				D5ctJ5txFm();
				int num = 1;
				while (true)
				{
					int num2 = num;
					do
					{
						int num3 = num2;
						while (true)
						{
							switch (num3)
							{
							default:
								if (num2 == 9)
								{
									return;
								}
								goto end_IL_0012;
							case 0:
								UKytE77xrn();
								num3 = 2;
								continue;
							case 2:
								break;
							case 1:
								RcNt1iGNhH();
								num3 = 0;
								if (1 == 0)
								{
									num3 = 4;
								}
								continue;
							}
							goto end_IL_000e;
							continue;
							end_IL_0012:
							break;
						}
						continue;
						end_IL_000e:
						break;
					}
					while (num2 == 990);
					bQmZ2KCKO7T4RTFr4XD.fQZBffWkRQ();
					num = 2;
					if (true)
					{
						num = 9;
					}
				}
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			internal static bool oWDVCaVo0DcQIxoeBX5()
			{
				return true;
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			internal static object kRp6jxVQgqbG7WydnoA()
			{
				return null;
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public O3Lf19pn52eD373nOWl(object P_0)
		{
		}
	}

	internal class ncHpNEpF8hB4YRDF1ch
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static string sm9phc4nwq(object P_0, object P_1)
		{
			return null;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public ncHpNEpF8hB4YRDF1ch()
		{
		}
	}

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate uint YKb9T7pwgf6BsMpQTo1(IntPtr classthis, IntPtr comp, IntPtr info, uint flags, IntPtr nativeEntry, ref uint nativeSizeOfCode);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate IntPtr baR4qNpS6R180n2nqed();

	internal struct axoD13poAOy2RTNJPvL
	{
		internal bool XNBpQ9cWcG;

		internal byte[] WT0plHug2U;
	}

	internal class IXuFOhpz0Mnl6lRpPtN
	{
		private object sR6CCfI1vk;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public IXuFOhpz0Mnl6lRpPtN(Stream P_0)
		{
			sR6CCfI1vk = new BinaryReader(P_0);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		[SpecialName]
		internal Stream KCFlcDdR6L()
		{
			return ((BinaryReader)sR6CCfI1vk).BaseStream;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal byte[] QFOCGhrbFH(int P_0)
		{
			return ((BinaryReader)sR6CCfI1vk).ReadBytes(P_0);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal int gQwCcVItrd(byte[] P_0, int P_1, int P_2)
		{
			return ((BinaryReader)sR6CCfI1vk).Read(P_0, P_1, P_2);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal int YWICtXkOiC()
		{
			return ((BinaryReader)sR6CCfI1vk).ReadInt32();
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal void dbaCpIGGUj()
		{
			((BinaryReader)sR6CCfI1vk).Close();
		}
	}

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	private delegate IntPtr nX4UPSCTItWAPwhmW2b(IntPtr hModule, string lpName, uint lpType);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate IntPtr xaIapFCbGrA3DrjpRbr(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int qGZj3RCH0YrjnlvXGED(IntPtr hProcess, IntPtr lpBaseAddress, [In][Out] byte[] buffer, uint size, out IntPtr lpNumberOfBytesWritten);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int d2LpgPCkYe5mk6VZFmn(IntPtr lpAddress, int dwSize, int flNewProtect, ref int lpflOldProtect);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate IntPtr Xb3pi1CatfLXCA5kJDE(uint dwDesiredAccess, int bInheritHandle, uint dwProcessId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int E2yxPTCWZkIhebuulMI(IntPtr ptr);

	[Flags]
	private enum Qhc5yeC33EAEE4IDMcp
	{

	}

	private static object L0hpY2Rke2;

	private static object Yf5pLbHD2i;

	private static long CU7p1vJVVx;

	internal static object Tkmp0tBXb9;

	private static object EIKprHdjCb;

	private static bool kbMp7Gxuco;

	private static List<string> nG9pKEBfgc;

	private static IntPtr myrpukTxJl;

	private static object U2vpiMeujs;

	private static int u5mpXnDHeE;

	private static Dictionary<int, int> vgZpsbZLih;

	private static int rI2p9xxOpU;

	private static object ok5pe9P6VR;

	private static bool Dr6pWbnnZU;

	internal static object UC7pfXZD4N;

	internal static object xFRpNbvIbJ;

	private static IntPtr f6apxNJiZK;

	private static int unrpmq9rQL;

	private static int CmWpV9oDlv;

	[O3Lf19pn52eD373nOWl(typeof(O3Lf19pn52eD373nOWl.H4f5UUpMQJCrm2sAC3e<object>[]))]
	private static bool JEepBXDRxl;

	internal static object YL9p8TnZg2;

	private static int RhLpdHcGUb;

	private static object BdYpvWJtWg;

	internal static object oiIp3r0OAK;

	private static bool yRop5oobS6;

	private static long Cf5pDG2bGj;

	private static bool JFkpjZ38np;

	private static bool DihpI3RRbC;

	private static object iZwpgGiqti;

	private static bool p74pqHljgy;

	private static object f14pPR2S7Z;

	private static IntPtr TLppU11Bfh;

	private static object DgKpOwlFrr;

	private static IntPtr TIypExNM5B;

	private static object IBcpRvb6sK;

	private static object KP2pZlT0EB;

	private static object qtxpAF20Zu;

	private static object PxNpJwaTmj;

	private static object dPRp2h3dgx;

	private static List<int> hwXp49PyZA;

	[MethodImpl(MethodImplOptions.NoInlining)]
	static fJUeITteiwtDJsNdQol()
	{
		Dr6pWbnnZU = false;
		oiIp3r0OAK = Type.GetTypeFromHandle(VbhttmCNbvOE14oqZBl.HaoEcZQw87(33554600)).Assembly;
		DgKpOwlFrr = new uint[64]
		{
			3614090360u, 3905402710u, 606105819u, 3250441966u, 4118548399u, 1200080426u, 2821735955u, 4249261313u, 1770035416u, 2336552879u,
			4294925233u, 2304563134u, 1804603682u, 4254626195u, 2792965006u, 1236535329u, 4129170786u, 3225465664u, 643717713u, 3921069994u,
			3593408605u, 38016083u, 3634488961u, 3889429448u, 568446438u, 3275163606u, 4107603335u, 1163531501u, 2850285829u, 4243563512u,
			1735328473u, 2368359562u, 4294588738u, 2272392833u, 1839030562u, 4259657740u, 2763975236u, 1272893353u, 4139469664u, 3200236656u,
			681279174u, 3936430074u, 3572445317u, 76029189u, 3654602809u, 3873151461u, 530742520u, 3299628645u, 4096336452u, 1126891415u,
			2878612391u, 4237533241u, 1700485571u, 2399980690u, 4293915773u, 2240044497u, 1873313359u, 4264355552u, 2734768916u, 1309151649u,
			4149444226u, 3174756917u, 718787259u, 3951481745u
		};
		DihpI3RRbC = false;
		JFkpjZ38np = false;
		xFRpNbvIbJ = null;
		vgZpsbZLih = null;
		L0hpY2Rke2 = new object();
		u5mpXnDHeE = 0;
		ok5pe9P6VR = new object();
		nG9pKEBfgc = null;
		hwXp49PyZA = null;
		KP2pZlT0EB = new byte[0];
		f14pPR2S7Z = new byte[0];
		f6apxNJiZK = IntPtr.Zero;
		TLppU11Bfh = IntPtr.Zero;
		dPRp2h3dgx = new string[0];
		IBcpRvb6sK = new int[0];
		RhLpdHcGUb = 1;
		p74pqHljgy = false;
		iZwpgGiqti = new SortedList();
		unrpmq9rQL = 0;
		Cf5pDG2bGj = 0L;
		UC7pfXZD4N = null;
		YL9p8TnZg2 = null;
		CU7p1vJVVx = 0L;
		CmWpV9oDlv = 0;
		yRop5oobS6 = false;
		kbMp7Gxuco = false;
		rI2p9xxOpU = 0;
		TIypExNM5B = IntPtr.Zero;
		JEepBXDRxl = false;
		Tkmp0tBXb9 = new Hashtable();
		qtxpAF20Zu = null;
		BdYpvWJtWg = null;
		EIKprHdjCb = null;
		U2vpiMeujs = null;
		Yf5pLbHD2i = null;
		PxNpJwaTmj = null;
		myrpukTxJl = IntPtr.Zero;
		try
		{
			RSACryptoServiceProvider.UseMachineKeyStore = true;
		}
		catch
		{
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void t3u9zi0ufA()
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static byte[] Tsat45GH3B(object P_0)
	{
		uint[] array = new uint[16];
		uint num = (uint)((448 - ((Array)P_0).Length * 8 % 512 + 512) % 512);
		if (num == 0)
		{
			num = 512u;
		}
		uint num2 = (uint)(((Array)P_0).Length + num / 8 + 8);
		ulong num3 = (ulong)((Array)P_0).Length * 8uL;
		byte[] array2 = new byte[num2];
		for (int i = 0; i < ((Array)P_0).Length; i++)
		{
			array2[i] = ((byte[])P_0)[i];
		}
		array2[((Array)P_0).Length] |= 128;
		for (int num4 = 8; num4 > 0; num4--)
		{
			array2[num2 - num4] = (byte)((num3 >> (8 - num4) * 8) & 0xFF);
		}
		uint num5 = (uint)(array2.Length * 8) / 32u;
		uint num6 = 1732584193u;
		uint num7 = 4023233417u;
		uint num8 = 2562383102u;
		uint num9 = 271733878u;
		for (uint num10 = 0u; num10 < num5 / 16; num10++)
		{
			uint num11 = num10 << 6;
			for (uint num12 = 0u; num12 < 61; num12 += 4)
			{
				array[num12 >> 2] = (uint)((array2[num11 + (num12 + 3)] << 24) | (array2[num11 + (num12 + 2)] << 16) | (array2[num11 + (num12 + 1)] << 8) | array2[num11 + num12]);
			}
			uint num13 = num6;
			uint num14 = num7;
			uint num15 = num8;
			uint num16 = num9;
			J63tZGhSCW(ref num6, num7, num8, num9, 0u, 7, 1u, array);
			J63tZGhSCW(ref num9, num6, num7, num8, 1u, 12, 2u, array);
			J63tZGhSCW(ref num8, num9, num6, num7, 2u, 17, 3u, array);
			J63tZGhSCW(ref num7, num8, num9, num6, 3u, 22, 4u, array);
			J63tZGhSCW(ref num6, num7, num8, num9, 4u, 7, 5u, array);
			J63tZGhSCW(ref num9, num6, num7, num8, 5u, 12, 6u, array);
			J63tZGhSCW(ref num8, num9, num6, num7, 6u, 17, 7u, array);
			J63tZGhSCW(ref num7, num8, num9, num6, 7u, 22, 8u, array);
			J63tZGhSCW(ref num6, num7, num8, num9, 8u, 7, 9u, array);
			J63tZGhSCW(ref num9, num6, num7, num8, 9u, 12, 10u, array);
			J63tZGhSCW(ref num8, num9, num6, num7, 10u, 17, 11u, array);
			J63tZGhSCW(ref num7, num8, num9, num6, 11u, 22, 12u, array);
			J63tZGhSCW(ref num6, num7, num8, num9, 12u, 7, 13u, array);
			J63tZGhSCW(ref num9, num6, num7, num8, 13u, 12, 14u, array);
			J63tZGhSCW(ref num8, num9, num6, num7, 14u, 17, 15u, array);
			J63tZGhSCW(ref num7, num8, num9, num6, 15u, 22, 16u, array);
			t5FtPBAYwo(ref num6, num7, num8, num9, 1u, 5, 17u, array);
			t5FtPBAYwo(ref num9, num6, num7, num8, 6u, 9, 18u, array);
			t5FtPBAYwo(ref num8, num9, num6, num7, 11u, 14, 19u, array);
			t5FtPBAYwo(ref num7, num8, num9, num6, 0u, 20, 20u, array);
			t5FtPBAYwo(ref num6, num7, num8, num9, 5u, 5, 21u, array);
			t5FtPBAYwo(ref num9, num6, num7, num8, 10u, 9, 22u, array);
			t5FtPBAYwo(ref num8, num9, num6, num7, 15u, 14, 23u, array);
			t5FtPBAYwo(ref num7, num8, num9, num6, 4u, 20, 24u, array);
			t5FtPBAYwo(ref num6, num7, num8, num9, 9u, 5, 25u, array);
			t5FtPBAYwo(ref num9, num6, num7, num8, 14u, 9, 26u, array);
			t5FtPBAYwo(ref num8, num9, num6, num7, 3u, 14, 27u, array);
			t5FtPBAYwo(ref num7, num8, num9, num6, 8u, 20, 28u, array);
			t5FtPBAYwo(ref num6, num7, num8, num9, 13u, 5, 29u, array);
			t5FtPBAYwo(ref num9, num6, num7, num8, 2u, 9, 30u, array);
			t5FtPBAYwo(ref num8, num9, num6, num7, 7u, 14, 31u, array);
			t5FtPBAYwo(ref num7, num8, num9, num6, 12u, 20, 32u, array);
			ejGtxYaktI(ref num6, num7, num8, num9, 5u, 4, 33u, array);
			ejGtxYaktI(ref num9, num6, num7, num8, 8u, 11, 34u, array);
			ejGtxYaktI(ref num8, num9, num6, num7, 11u, 16, 35u, array);
			ejGtxYaktI(ref num7, num8, num9, num6, 14u, 23, 36u, array);
			ejGtxYaktI(ref num6, num7, num8, num9, 1u, 4, 37u, array);
			ejGtxYaktI(ref num9, num6, num7, num8, 4u, 11, 38u, array);
			ejGtxYaktI(ref num8, num9, num6, num7, 7u, 16, 39u, array);
			ejGtxYaktI(ref num7, num8, num9, num6, 10u, 23, 40u, array);
			ejGtxYaktI(ref num6, num7, num8, num9, 13u, 4, 41u, array);
			ejGtxYaktI(ref num9, num6, num7, num8, 0u, 11, 42u, array);
			ejGtxYaktI(ref num8, num9, num6, num7, 3u, 16, 43u, array);
			ejGtxYaktI(ref num7, num8, num9, num6, 6u, 23, 44u, array);
			ejGtxYaktI(ref num6, num7, num8, num9, 9u, 4, 45u, array);
			ejGtxYaktI(ref num9, num6, num7, num8, 12u, 11, 46u, array);
			ejGtxYaktI(ref num8, num9, num6, num7, 15u, 16, 47u, array);
			ejGtxYaktI(ref num7, num8, num9, num6, 2u, 23, 48u, array);
			q9ctUt5epp(ref num6, num7, num8, num9, 0u, 6, 49u, array);
			q9ctUt5epp(ref num9, num6, num7, num8, 7u, 10, 50u, array);
			q9ctUt5epp(ref num8, num9, num6, num7, 14u, 15, 51u, array);
			q9ctUt5epp(ref num7, num8, num9, num6, 5u, 21, 52u, array);
			q9ctUt5epp(ref num6, num7, num8, num9, 12u, 6, 53u, array);
			q9ctUt5epp(ref num9, num6, num7, num8, 3u, 10, 54u, array);
			q9ctUt5epp(ref num8, num9, num6, num7, 10u, 15, 55u, array);
			q9ctUt5epp(ref num7, num8, num9, num6, 1u, 21, 56u, array);
			q9ctUt5epp(ref num6, num7, num8, num9, 8u, 6, 57u, array);
			q9ctUt5epp(ref num9, num6, num7, num8, 15u, 10, 58u, array);
			q9ctUt5epp(ref num8, num9, num6, num7, 6u, 15, 59u, array);
			q9ctUt5epp(ref num7, num8, num9, num6, 13u, 21, 60u, array);
			q9ctUt5epp(ref num6, num7, num8, num9, 4u, 6, 61u, array);
			q9ctUt5epp(ref num9, num6, num7, num8, 11u, 10, 62u, array);
			q9ctUt5epp(ref num8, num9, num6, num7, 2u, 15, 63u, array);
			q9ctUt5epp(ref num7, num8, num9, num6, 9u, 21, 64u, array);
			num6 += num13;
			num7 += num14;
			num8 += num15;
			num9 += num16;
		}
		byte[] array3 = new byte[16];
		Array.Copy(BitConverter.GetBytes(num6), 0, array3, 0, 4);
		Array.Copy(BitConverter.GetBytes(num7), 0, array3, 4, 4);
		Array.Copy(BitConverter.GetBytes(num8), 0, array3, 8, 4);
		Array.Copy(BitConverter.GetBytes(num9), 0, array3, 12, 4);
		return array3;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void J63tZGhSCW(ref uint P_0, uint P_1, uint P_2, uint P_3, uint P_4, ushort P_5, uint P_6, object P_7)
	{
		P_0 = P_1 + z6nt2uCSGN(P_0 + ((P_1 & P_2) | (~P_1 & P_3)) + ((uint[])P_7)[P_4] + ((uint[])DgKpOwlFrr)[P_6 - 1], P_5);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void t5FtPBAYwo(ref uint P_0, uint P_1, uint P_2, uint P_3, uint P_4, ushort P_5, uint P_6, object P_7)
	{
		P_0 = P_1 + z6nt2uCSGN(P_0 + ((P_1 & P_3) | (P_2 & ~P_3)) + ((uint[])P_7)[P_4] + ((uint[])DgKpOwlFrr)[P_6 - 1], P_5);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ejGtxYaktI(ref uint P_0, uint P_1, uint P_2, uint P_3, uint P_4, ushort P_5, uint P_6, object P_7)
	{
		P_0 = P_1 + z6nt2uCSGN(P_0 + (P_1 ^ P_2 ^ P_3) + ((uint[])P_7)[P_4] + ((uint[])DgKpOwlFrr)[P_6 - 1], P_5);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void q9ctUt5epp(ref uint P_0, uint P_1, uint P_2, uint P_3, uint P_4, ushort P_5, uint P_6, object P_7)
	{
		P_0 = P_1 + z6nt2uCSGN(P_0 + (P_2 ^ (P_1 | ~P_3)) + ((uint[])P_7)[P_4] + ((uint[])DgKpOwlFrr)[P_6 - 1], P_5);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static uint z6nt2uCSGN(uint P_0, ushort P_1)
	{
		return (P_0 >> 32 - P_1) | (P_0 << (int)P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool In1tR2DTNX()
	{
		if (!DihpI3RRbC)
		{
			pLutgeLxO3();
			DihpI3RRbC = true;
		}
		return JFkpjZ38np;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal fJUeITteiwtDJsNdQol()
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void M7Xtd9Efjr(byte[] P_0, byte[] P_1, byte[] P_2)
	{
		int num = P_2.Length % 4;
		int num2 = P_2.Length / 4;
		byte[] array = new byte[P_2.Length];
		int num3 = P_0.Length / 4;
		uint num4 = 0u;
		uint num5 = 0u;
		uint num6 = 0u;
		if (num > 0)
		{
			num2++;
		}
		uint num7 = 0u;
		for (int i = 0; i < num2; i++)
		{
			int num8 = i % num3;
			int num9 = i * 4;
			num7 = (uint)(num8 * 4);
			num5 = (uint)((P_0[num7 + 3] << 24) | (P_0[num7 + 2] << 16) | (P_0[num7 + 1] << 8) | P_0[num7]);
			uint num10 = 255u;
			int num11 = 0;
			if (i == num2 - 1 && num > 0)
			{
				num6 = 0u;
				num4 += num5;
				for (int j = 0; j < num; j++)
				{
					if (j > 0)
					{
						num6 <<= 8;
					}
					num6 |= P_2[P_2.Length - (1 + j)];
				}
			}
			else
			{
				num4 += num5;
				num7 = (uint)num9;
				num6 = (uint)((P_2[num7 + 3] << 24) | (P_2[num7 + 2] << 16) | (P_2[num7 + 1] << 8) | P_2[num7]);
			}
			uint num12 = num4;
			num4 = 0u;
			uint num13 = 1400895850u;
			uint num14 = 994329632u;
			uint num15 = 1117048405u;
			uint num16 = 1331402446u;
			uint num17 = num12;
			ulong num18 = num14 * num16;
			if (num18 == 0)
			{
				num18--;
			}
			num13 = (uint)(num13 * num13 % num18);
			num15 ^= num14;
			num18 = num13 * 1026830853;
			if (num18 == 0)
			{
				num18--;
			}
			num14 = (uint)(num14 * num14 % num18);
			if (num16 == 0)
			{
				num16--;
			}
			uint num19 = num13 / num16 + num16;
			num16 = ((num13 + num13) ^ num19) + num13;
			if (num17 == 0)
			{
				num17--;
			}
			num19 = num13 / num17 + num17;
			num17 = num13 - num13 + num19 + num13;
			num17 ^= num17 << 7;
			num17 += num14;
			num17 ^= num17 >> 1;
			num17 += num16;
			num17 ^= num17 << 25;
			num17 += num17;
			num17 = (((num16 << 3) + num16) ^ num16) + num17;
			num4 = num12 + (uint)(double)num17;
			if (i == num2 - 1 && num > 0)
			{
				uint num20 = num4 ^ num6;
				for (int k = 0; k < num; k++)
				{
					if (k > 0)
					{
						num10 <<= 8;
						num11 += 8;
					}
					array[num9 + k] = (byte)((num20 & num10) >> num11);
				}
			}
			else
			{
				uint num21 = num4 ^ num6;
				array[num9] = (byte)(num21 & 0xFF);
				array[num9 + 1] = (byte)((num21 & 0xFF00) >> 8);
				array[num9 + 2] = (byte)((num21 & 0xFF0000) >> 16);
				array[num9 + 3] = (byte)((num21 & 0xFF000000u) >> 24);
			}
		}
		KP2pZlT0EB = array;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static SymmetricAlgorithm DF8tqaPCEj()
	{
		SymmetricAlgorithm symmetricAlgorithm = null;
		if (In1tR2DTNX())
		{
			return new AesCryptoServiceProvider();
		}
		try
		{
			return new RijndaelManaged();
		}
		catch
		{
			try
			{
				return (SymmetricAlgorithm)Activator.CreateInstance("System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Security.Cryptography.AesCryptoServiceProvider").Unwrap();
			}
			catch
			{
				return (SymmetricAlgorithm)Activator.CreateInstance("System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Security.Cryptography.AesCryptoServiceProvider").Unwrap();
			}
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void pLutgeLxO3()
	{
		try
		{
			new MD5CryptoServiceProvider();
		}
		catch
		{
			JFkpjZ38np = true;
			return;
		}
		try
		{
			JFkpjZ38np = CryptoConfig.AllowOnlyFipsAlgorithms;
		}
		catch
		{
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static byte[] tfmtmL1AHF(object P_0)
	{
		if (!In1tR2DTNX())
		{
			return new MD5CryptoServiceProvider().ComputeHash((byte[])P_0);
		}
		return Tsat45GH3B(P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void dFRtDelwPd(object P_0, object P_1, uint P_2, object P_3)
	{
		while (P_2 != 0)
		{
			int num = ((P_2 > (uint)((Array)P_3).Length) ? ((Array)P_3).Length : ((int)P_2));
			((Stream)P_1).Read((byte[])P_3, 0, num);
			bDttfaBsp6(P_0, P_3, 0, num);
			P_2 -= (uint)num;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void bDttfaBsp6(object P_0, object P_1, int P_2, int P_3)
	{
		((HashAlgorithm)P_0).TransformBlock((byte[])P_1, P_2, P_3, (byte[])P_1, P_2);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static uint Hu7t8wEhtf(uint P_0, int P_1, long P_2, object P_3)
	{
		for (int i = 0; i < P_1; i++)
		{
			((BinaryReader)P_3).BaseStream.Position = P_2 + (i * 40 + 8);
			uint num = ((BinaryReader)P_3).ReadUInt32();
			uint num2 = ((BinaryReader)P_3).ReadUInt32();
			((BinaryReader)P_3).ReadUInt32();
			uint num3 = ((BinaryReader)P_3).ReadUInt32();
			if (num2 <= P_0 && P_0 < num2 + num)
			{
				return num3 + P_0 - num2;
			}
		}
		return 0u;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void RcNt1iGNhH()
	{
		int num = 12;
		BinaryReader binaryReader = default;
		string text = default;
		int num21 = default;
		uint num20 = default;
		uint num16 = default;
		int num11 = default;
		long num10 = default;
		int num23 = default;
		HashAlgorithm hashAlgorithm = default;
		uint num14 = default;
		long num17 = default;
		long num25 = default;
		uint num24 = default;
		long num18 = default;
		uint num15 = default;
		bool flag = default;
		string text2 = default;
		byte[] array2 = default;
		byte[] array = default;
		uint num19 = default;
		long num12 = default;
		int num27 = default;
		int num36 = default;
		int num38 = default;
		int num32 = default;
		int num34 = default;
		int num5 = default;
		int num7 = default;
		while (true)
		{
			int num2 = num;
			while (true)
			{
				int num3 = num2;
				while (true)
				{
					switch (num3)
					{
					default:
						if (num2 != 26)
						{
							if (num2 == 1007)
							{
								goto end_IL_000d;
							}
							goto case 19;
						}
						return;
					case 0:
						binaryReader = null;
						num3 = 10;
						continue;
					case 10:
						try
						{
							FileStream fileStream = new FileStream(text, FileMode.Open, FileAccess.Read, FileShare.Read);
							int num8 = 8;
							while (true)
							{
								int num13;
								switch (num8)
								{
								default:
									if (num21 != 51)
									{
										if (num21 == 1031)
										{
											goto IL_00b5;
										}
										goto case 25;
									}
									goto case 16;
								case 36:
									OEDmyPjGovkbQIYxSs9(fileStream, num20);
									num8 = 7;
									continue;
								case 16:
									num16 = DyGAI9j4upKchIwu01p(JI061ZjKUJOr9McdMfU(binaryReader), num11, num10, binaryReader);
									num8 = 3;
									if (PJBdBCIyCfKrfofamI9())
									{
										num8 = 37;
									}
									continue;
								case 8:
									binaryReader = new BinaryReader(fileStream);
									num8 = 32;
									if (PD7vDvInyHFEXTKjKoE() == null)
									{
										num8 = 19;
									}
									continue;
								case 5:
								case 23:
									num23++;
									num8 = 4;
									continue;
								case 32:
									nTYKDBjPZ7RPgDEPuhs(hashAlgorithm, new byte[0], 0, 0);
									num8 = 39;
									continue;
								case 0:
									num14 = (uint)(num17 - num25);
									num8 = 14;
									continue;
								case 28:
								case 30:
									num24 = (uint)bmLrBAjZWk8prH56AGG(num18 - num25, num15);
									num8 = 21;
									continue;
								case 17:
									flag = !RIFYmKjRnrXkjg8Peok(xFRpNbvIbJ, y8shEKj2GEw37b5Wyia(hashAlgorithm), text2, array2);
									num8 = 40;
									if (PD7vDvInyHFEXTKjKoE() != null)
									{
										num8 = 23;
									}
									continue;
								case 35:
									num15 = JI061ZjKUJOr9McdMfU(binaryReader);
									num8 = 11;
									continue;
								case 38:
									JJlg5RjNwa18jHsXna8(hashAlgorithm, fileStream, num15, array);
									num8 = 5;
									if (!PJBdBCIyCfKrfofamI9())
									{
										num8 = 18;
									}
									continue;
								case 9:
									OEDmyPjGovkbQIYxSs9(fileStream, 360L);
									num13 = 51;
									if (!PJBdBCIyCfKrfofamI9())
									{
										num13 = 11;
									}
									goto IL_00b1;
								case 3:
									num17 = num18 + num19;
									num8 = 31;
									if (PD7vDvInyHFEXTKjKoE() != null)
									{
										num8 = 5;
									}
									continue;
								case 14:
									if (num14 < num15)
									{
										num8 = 2;
										continue;
									}
									goto case 5;
								case 10:
									array2 = (byte[])tWgcNIjx31RLswkGukf(binaryReader, (int)num19);
									num8 = 26;
									if (PD7vDvInyHFEXTKjKoE() != null)
									{
										num8 = 13;
									}
									continue;
								case 21:
									JJlg5RjNwa18jHsXna8(hashAlgorithm, fileStream, num24, array);
									num8 = 39;
									if (PD7vDvInyHFEXTKjKoE() == null)
									{
										num8 = 22;
									}
									continue;
								case 7:
								case 27:
								case 42:
									if (num15 == 0)
									{
										num8 = 23;
										continue;
									}
									goto case 34;
								case 29:
									if (num18 > num25)
									{
										num8 = 1;
										if (!PJBdBCIyCfKrfofamI9())
										{
											num8 = 41;
										}
										continue;
									}
									goto case 18;
								case 1:
								case 6:
								case 13:
									if (num25 < num17)
									{
										num8 = 28;
										if (!PJBdBCIyCfKrfofamI9())
										{
											num8 = 6;
										}
										continue;
									}
									goto case 38;
								case 4:
								case 33:
									if (num23 >= num11)
									{
										num8 = 32;
										continue;
									}
									goto case 12;
								case 43:
									JJlg5RjNwa18jHsXna8(hashAlgorithm, fileStream, 152u, array);
									num8 = 15;
									continue;
								case 26:
									JY9lELjU1pP6fSRhcDm(array2);
									num8 = 17;
									continue;
								case 22:
									num15 -= num24;
									num8 = 42;
									continue;
								case 12:
									OEDmyPjGovkbQIYxSs9(fileStream, num10 + num23 * 40 + 16);
									num8 = 35;
									continue;
								case 11:
									num20 = JI061ZjKUJOr9McdMfU(binaryReader);
									num8 = 36;
									continue;
								case 18:
									if (num25 >= num17)
									{
										num13 = 6;
										goto IL_00b1;
									}
									goto case 0;
								case 37:
									OEDmyPjGovkbQIYxSs9(fileStream, num16 + 32);
									num8 = 24;
									continue;
								case 20:
									num23 = 0;
									num13 = 33;
									goto IL_00b1;
								case 39:
									OEDmyPjGovkbQIYxSs9(fileStream, num18);
									num8 = 10;
									continue;
								case 19:
									array = new byte[65536];
									num8 = 9;
									if (PD7vDvInyHFEXTKjKoE() == null)
									{
										num8 = 43;
									}
									continue;
								case 24:
								{
									uint num22 = JI061ZjKUJOr9McdMfU(binaryReader);
									num19 = JI061ZjKUJOr9McdMfU(binaryReader);
									num18 = DyGAI9j4upKchIwu01p(num22, num11, num10, binaryReader);
									num8 = 3;
									if (!PJBdBCIyCfKrfofamI9())
									{
										num8 = 22;
									}
									continue;
								}
								case 31:
									OEDmyPjGovkbQIYxSs9(fileStream, num12);
									num8 = 20;
									if (!PJBdBCIyCfKrfofamI9())
									{
										num8 = 12;
									}
									continue;
								case 25:
									OEDmyPjGovkbQIYxSs9(fileStream, hpk6F2jeTDgihORCcmy(fileStream) + num14);
									num8 = 27;
									if (!PJBdBCIyCfKrfofamI9())
									{
										num8 = 13;
									}
									continue;
								case 41:
									OEDmyPjGovkbQIYxSs9(fileStream, 376L);
									num8 = 31;
									if (PJBdBCIyCfKrfofamI9())
									{
										num8 = 16;
									}
									continue;
								case 34:
									num25 = hpk6F2jeTDgihORCcmy(fileStream);
									num8 = 29;
									continue;
								case 2:
									num15 -= num14;
									num8 = 44;
									if (PD7vDvInyHFEXTKjKoE() == null)
									{
										num8 = 25;
									}
									continue;
								case 15:
								{
									bool flag2 = cCsE6cjsyo2ycQhFUda(binaryReader) != 523;
									int num9 = (flag2 ? 96 : 112);
									OEDmyPjGovkbQIYxSs9(fileStream, 152L);
									KnurOkjYTHWY8MEVvU0(fileStream, array, 0, num9);
									array[64] = 0;
									array[65] = 0;
									array[66] = 0;
									array[67] = 0;
									mk4E6PjXPo3HaiPW3l2(hashAlgorithm, array, 0, num9);
									KnurOkjYTHWY8MEVvU0(fileStream, array, 0, 128);
									array[32] = 0;
									array[33] = 0;
									array[34] = 0;
									array[35] = 0;
									array[36] = 0;
									array[37] = 0;
									array[38] = 0;
									array[39] = 0;
									mk4E6PjXPo3HaiPW3l2(hashAlgorithm, array, 0, 128);
									num10 = hpk6F2jeTDgihORCcmy(fileStream);
									OEDmyPjGovkbQIYxSs9(fileStream, 134L);
									num11 = cCsE6cjsyo2ycQhFUda(binaryReader);
									OEDmyPjGovkbQIYxSs9(fileStream, num10);
									JJlg5RjNwa18jHsXna8(hashAlgorithm, fileStream, (uint)(num11 * 40), array);
									num12 = hpk6F2jeTDgihORCcmy(fileStream);
									if (flag2)
									{
										num13 = 9;
										goto IL_00b1;
									}
									goto case 41;
								}
								case 40:
									break;
									IL_00b1:
									num21 = num13;
									goto IL_00b5;
									IL_00b5:
									num8 = num21;
									continue;
								}
								break;
							}
						}
						catch
						{
							int num26 = 5;
							if (PJBdBCIyCfKrfofamI9())
							{
								num26 = 0;
							}
							while (true)
							{
								switch (num26)
								{
								default:
									if (num27 == 989)
									{
										num26 = num27;
										continue;
									}
									break;
								case 0:
									break;
								case 1:
									goto end_IL_0767;
								}
								flag = true;
								num26 = 1;
								if (!PJBdBCIyCfKrfofamI9())
								{
									num26 = 6;
								}
								continue;
								end_IL_0767:
								break;
							}
						}
						goto case 17;
					case 1:
						try
						{
							hashAlgorithm = (HashAlgorithm)UyrRlIISQ30CEAFxaXD();
							int num35 = 1;
							if (PD7vDvInyHFEXTKjKoE() == null)
							{
								num35 = 1;
							}
							while (true)
							{
								switch (num35)
								{
								default:
									if (num36 == 991)
									{
										num35 = num36;
										continue;
									}
									return;
								case 0:
									return;
								case 2:
									if (!wOBcrDIQOqirEt7HI8P(text))
									{
										num35 = 6;
										if (PD7vDvInyHFEXTKjKoE() == null)
										{
											num35 = 0;
										}
										continue;
									}
									break;
								case 1:
									text2 = (string)s8eFCnIojoIfb5Q2ST3("SHA1");
									num35 = 2;
									if (!PJBdBCIyCfKrfofamI9())
									{
										num35 = 4;
									}
									continue;
								case 3:
									break;
								}
								break;
							}
						}
						catch
						{
							int num37 = 0;
							if (!PJBdBCIyCfKrfofamI9())
							{
								num37 = 2;
							}
							while (true)
							{
								switch (num37)
								{
								case 0:
									return;
								}
								if (num38 == 988)
								{
									num37 = num38;
									continue;
								}
								return;
							}
						}
						goto case 3;
					case 11:
						return;
					case 8:
						text2 = null;
						num3 = 8;
						if (PJBdBCIyCfKrfofamI9())
						{
							num3 = 1;
						}
						continue;
					case 9:
						if (text != null)
						{
							num3 = 15;
							if (PD7vDvInyHFEXTKjKoE() != null)
							{
								num3 = 18;
							}
							continue;
						}
						return;
					case 14:
						throw new Exception((string)KBjIPBjmDTTelyfXgsk(NShuppjgvwn4spvSJUQ(tvqN9tjqxqE1ASfwsSx(lblkXtIFShH2NeUrfi1(typeof(fJUeITteiwtDJsNdQol).TypeHandle).Assembly)), " is tampered."));
					case 15:
						if (ypnLVgIwRXTZvbFMqrq(text) == 0)
						{
							num = 21;
							if (PD7vDvInyHFEXTKjKoE() == null)
							{
								num = 26;
							}
							break;
						}
						hashAlgorithm = null;
						num3 = 8;
						if (!PJBdBCIyCfKrfofamI9())
						{
							num3 = 15;
						}
						continue;
					case 16:
						if (!flag)
						{
							flag = false;
							num3 = 5;
						}
						else
						{
							num3 = 14;
						}
						continue;
					case 4:
						I9qLJEI6kSO1eOng4D5(true);
						num3 = 13;
						if (PD7vDvInyHFEXTKjKoE() == null)
						{
							num3 = 18;
						}
						continue;
					case 7:
						text = (string)gEFQUXIhyyvNqQUukaL(lblkXtIFShH2NeUrfi1(typeof(fJUeITteiwtDJsNdQol).TypeHandle).Assembly);
						num3 = 23;
						if (PD7vDvInyHFEXTKjKoE() == null)
						{
							num3 = 9;
						}
						continue;
					case 19:
						H83Sr2IM0yUJIWxviXF();
						num = 4;
						break;
					case 17:
						num3 = 13;
						continue;
					case 2:
						if (!flag)
						{
							num3 = 24;
							if (PD7vDvInyHFEXTKjKoE() == null)
							{
								num3 = 0;
							}
							continue;
						}
						goto case 16;
					case 6:
						try
						{
							IXuFOhpz0Mnl6lRpPtN xuFOhpz0Mnl6lRpPtN = new IXuFOhpz0Mnl6lRpPtN((Stream)k0KwEIIlNq5ryU7uu0K(oiIp3r0OAK, "icMAooTWWcCcufwJYN.00l2UBbK7dWencibX7"));
							OEDmyPjGovkbQIYxSs9(VY1FHjIzB7diWxuuuVQ(xuFOhpz0Mnl6lRpPtN), 0L);
							byte[] array3 = (byte[])HqYLT1jtwjjc54Udgt5(xuFOhpz0Mnl6lRpPtN, (int)Knu05VjcxiTHh7rOHcm(VY1FHjIzB7diWxuuuVQ(xuFOhpz0Mnl6lRpPtN)));
							byte[] array4 = new byte[32];
							array4[0] = 150;
							array4[0] = 98;
							int num28 = 116 + 116;
							array4[0] = (byte)num28;
							array4[1] = 167;
							num28 = 218 - 72;
							array4[1] = (byte)num28;
							array4[1] = 214;
							array4[1] = 18;
							array4[1] = 156;
							array4[1] = 195;
							num28 = 181 - 60;
							array4[2] = (byte)num28;
							num28 = 131 - 43;
							array4[2] = (byte)num28;
							num28 = 165 - 55;
							array4[2] = (byte)num28;
							array4[2] = 48;
							array4[2] = 53;
							num28 = 183 - 61;
							array4[3] = (byte)num28;
							num28 = 243 - 81;
							array4[3] = (byte)num28;
							array4[3] = 26;
							array4[4] = 144;
							array4[4] = 207;
							num28 = 106 + 122;
							array4[4] = (byte)num28;
							array4[4] = 88;
							array4[4] = 50;
							num28 = 65 + 59;
							array4[5] = (byte)num28;
							num28 = 46 + 63;
							array4[5] = (byte)num28;
							num28 = 148 - 49;
							array4[5] = (byte)num28;
							num28 = 11 + 116;
							array4[5] = (byte)num28;
							num28 = 113 + 15;
							array4[5] = (byte)num28;
							array4[5] = 32;
							num28 = 21 + 3;
							array4[6] = (byte)num28;
							num28 = 41 + 43;
							array4[6] = (byte)num28;
							num28 = 121 + 0;
							array4[6] = (byte)num28;
							array4[6] = 163;
							array4[6] = 99;
							num28 = 170 + 13;
							array4[6] = (byte)num28;
							array4[7] = 121;
							num28 = 68 + 28;
							array4[7] = (byte)num28;
							num28 = 139 + 7;
							array4[7] = (byte)num28;
							num28 = 232 - 77;
							array4[8] = (byte)num28;
							num28 = 170 - 56;
							array4[8] = (byte)num28;
							array4[8] = 86;
							num28 = 125 - 41;
							array4[8] = (byte)num28;
							array4[8] = 152;
							num28 = 149 + 70;
							array4[8] = (byte)num28;
							num28 = 12 + 16;
							array4[9] = (byte)num28;
							array4[9] = 152;
							num28 = 74 + 94;
							array4[9] = (byte)num28;
							array4[10] = 88;
							num28 = 173 - 57;
							array4[10] = (byte)num28;
							num28 = 26 + 99;
							array4[10] = (byte)num28;
							num28 = 196 - 65;
							array4[11] = (byte)num28;
							array4[11] = 161;
							array4[11] = 206;
							array4[11] = 180;
							num28 = 114 + 107;
							array4[11] = (byte)num28;
							array4[12] = 100;
							num28 = 54 + 20;
							array4[12] = (byte)num28;
							array4[12] = 141;
							array4[12] = 162;
							num28 = 245 - 81;
							array4[12] = (byte)num28;
							num28 = 170 - 62;
							array4[12] = (byte)num28;
							num28 = 11 + 114;
							array4[13] = (byte)num28;
							num28 = 69 + 36;
							array4[13] = (byte)num28;
							num28 = 118 - 30;
							array4[13] = (byte)num28;
							array4[14] = 138;
							array4[14] = 81;
							num28 = 235 - 78;
							array4[14] = (byte)num28;
							num28 = 157 - 52;
							array4[14] = (byte)num28;
							array4[14] = 105;
							num28 = 100 + 75;
							array4[14] = (byte)num28;
							array4[15] = 148;
							num28 = 100 + 98;
							array4[15] = (byte)num28;
							num28 = 104 + 93;
							array4[15] = (byte)num28;
							num28 = 21 + 107;
							array4[15] = (byte)num28;
							array4[16] = 96;
							num28 = 165 - 55;
							array4[16] = (byte)num28;
							num28 = 23 + 98;
							array4[16] = (byte)num28;
							array4[16] = 218;
							array4[17] = 227;
							array4[17] = 83;
							array4[17] = 155;
							array4[17] = 219;
							array4[17] = 125;
							num28 = 163 - 95;
							array4[17] = (byte)num28;
							num28 = 58 + 110;
							array4[18] = (byte)num28;
							array4[18] = 156;
							num28 = 38 - 35;
							array4[18] = (byte)num28;
							array4[19] = 156;
							num28 = 62 + 117;
							array4[19] = (byte)num28;
							array4[19] = 86;
							num28 = 210 - 70;
							array4[19] = (byte)num28;
							array4[19] = 100;
							num28 = 98 - 24;
							array4[19] = (byte)num28;
							array4[20] = 124;
							array4[20] = 227;
							num28 = 251 - 83;
							array4[20] = (byte)num28;
							num28 = 115 + 111;
							array4[20] = (byte)num28;
							array4[21] = 90;
							array4[21] = 128;
							array4[21] = 248;
							num28 = 252 - 84;
							array4[22] = (byte)num28;
							array4[22] = 144;
							num28 = 56 + 20;
							array4[22] = (byte)num28;
							num28 = 134 - 44;
							array4[22] = (byte)num28;
							array4[22] = 142;
							num28 = 207 - 69;
							array4[23] = (byte)num28;
							array4[23] = 145;
							array4[23] = 164;
							num28 = 140 - 46;
							array4[24] = (byte)num28;
							num28 = 90 + 110;
							array4[24] = (byte)num28;
							array4[24] = 231;
							num28 = 191 - 63;
							array4[25] = (byte)num28;
							array4[25] = 102;
							array4[25] = 109;
							num28 = 150 + 53;
							array4[25] = (byte)num28;
							array4[26] = 148;
							num28 = 194 - 64;
							array4[26] = (byte)num28;
							num28 = 188 - 62;
							array4[26] = (byte)num28;
							num28 = 52 + 2;
							array4[26] = (byte)num28;
							num28 = 173 + 39;
							array4[26] = (byte)num28;
							num28 = 39 + 110;
							array4[27] = (byte)num28;
							array4[27] = 101;
							array4[27] = 128;
							num28 = 221 - 73;
							array4[27] = (byte)num28;
							num28 = 131 - 43;
							array4[27] = (byte)num28;
							array4[27] = 44;
							array4[28] = 154;
							array4[28] = 167;
							num28 = 154 - 51;
							array4[28] = (byte)num28;
							num28 = 100 + 6;
							array4[28] = (byte)num28;
							num28 = 136 - 85;
							array4[28] = (byte)num28;
							num28 = 89 + 54;
							array4[29] = (byte)num28;
							num28 = 189 - 63;
							array4[29] = (byte)num28;
							array4[29] = 176;
							array4[29] = 113;
							array4[30] = 127;
							num28 = 108 + 0;
							array4[30] = (byte)num28;
							num28 = 241 - 80;
							array4[30] = (byte)num28;
							num28 = 89 + 86;
							array4[30] = (byte)num28;
							num28 = 231 - 77;
							array4[30] = (byte)num28;
							num28 = 66 - 65;
							array4[30] = (byte)num28;
							array4[31] = 159;
							array4[31] = 82;
							array4[31] = 65;
							array4[31] = 16;
							byte[] array5 = array4;
							byte[] array6 = new byte[16]
							{
								93, 0, 0, 0, 0, 0, 0, 0, 0, 0,
								0, 0, 0, 0, 0, 0
							};
							int num29 = 154 - 51;
							array6[0] = (byte)num29;
							array6[0] = 66;
							int num30 = 181 - 60;
							array6[1] = (byte)num30;
							num29 = 113 + 38;
							array6[1] = (byte)num29;
							num29 = 226 - 75;
							array6[1] = (byte)num29;
							num29 = 130 - 43;
							array6[1] = (byte)num29;
							num29 = 142 + 18;
							array6[1] = (byte)num29;
							array6[2] = 121;
							array6[2] = 198;
							num30 = 122 + 34;
							array6[2] = (byte)num30;
							num30 = 27 - 15;
							array6[2] = (byte)num30;
							array6[3] = 111;
							num30 = 233 - 77;
							array6[3] = (byte)num30;
							num29 = 245 - 81;
							array6[3] = (byte)num29;
							num29 = 8 + 65;
							array6[3] = (byte)num29;
							array6[3] = 128;
							array6[3] = 206;
							array6[4] = 103;
							num30 = 217 - 72;
							array6[4] = (byte)num30;
							array6[4] = 145;
							num30 = 152 - 50;
							array6[4] = (byte)num30;
							num30 = 219 - 73;
							array6[4] = (byte)num30;
							array6[4] = 68;
							array6[5] = 96;
							array6[5] = 144;
							array6[5] = 119;
							num30 = 253 - 84;
							array6[5] = (byte)num30;
							num30 = 135 - 45;
							array6[5] = (byte)num30;
							num29 = 71 + 121;
							array6[5] = (byte)num29;
							array6[6] = 126;
							array6[6] = 42;
							array6[6] = 121;
							num29 = 101 + 103;
							array6[6] = (byte)num29;
							num30 = 228 - 76;
							array6[7] = (byte)num30;
							num30 = 222 - 74;
							array6[7] = (byte)num30;
							array6[7] = 87;
							num29 = 241 - 80;
							array6[8] = (byte)num29;
							num30 = 226 - 75;
							array6[8] = (byte)num30;
							array6[8] = 148;
							array6[9] = 178;
							num29 = 223 - 74;
							array6[9] = (byte)num29;
							num29 = 101 - 20;
							array6[9] = (byte)num29;
							array6[10] = 142;
							num29 = 219 - 73;
							array6[10] = (byte)num29;
							num29 = 239 - 79;
							array6[10] = (byte)num29;
							array6[10] = 107;
							array6[10] = 210;
							array6[11] = 134;
							num29 = 228 - 76;
							array6[11] = (byte)num29;
							array6[11] = 88;
							array6[11] = 231;
							array6[12] = 156;
							array6[12] = 156;
							num30 = 26 + 122;
							array6[12] = (byte)num30;
							num30 = 239 - 79;
							array6[12] = (byte)num30;
							num30 = 63 + 17;
							array6[12] = (byte)num30;
							num29 = 120 - 116;
							array6[12] = (byte)num29;
							array6[13] = 177;
							array6[13] = 92;
							num30 = 82 + 89;
							array6[13] = (byte)num30;
							array6[13] = 148;
							num29 = 148 - 82;
							array6[13] = (byte)num29;
							num30 = 204 - 68;
							array6[14] = (byte)num30;
							num30 = 195 - 65;
							array6[14] = (byte)num30;
							array6[14] = 169;
							array6[14] = 75;
							num30 = 120 + 48;
							array6[14] = (byte)num30;
							num29 = 39 + 65;
							array6[15] = (byte)num29;
							num29 = 34 + 8;
							array6[15] = (byte)num29;
							array6[15] = 135;
							array6[15] = 162;
							array6[15] = 69;
							num29 = 154 + 37;
							array6[15] = (byte)num29;
							byte[] array7 = array6;
							object obj3 = JCQ0JBjpP5oYpfjx14b();
							WLiQ4JjC9asWUO4arMv(obj3, CipherMode.CBC);
							ICryptoTransform transform = (ICryptoTransform)dNRhD6jTRkJXksBr95Z(obj3, array5, array7);
							Stream stream = (Stream)VXEMYnjbpokBNwBVmn4();
							CryptoStream cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Write);
							F41fVkjHwpblNYiXEGd(cryptoStream, array3, 0, array3.Length);
							c1Abb8jkVNKnT2Vtkvy(cryptoStream);
							GUAsTjjOwjTXFHxAOOf(xFRpNbvIbJ, dU8f7Pj34LvG0nJOMsi(ovWTXVjaAxgGgcYEJOD(), NpFPS1jWqE4c0KuOgDm(stream)));
							XvM4bYjI0YKP8tKk6Sb(stream);
							XvM4bYjI0YKP8tKk6Sb(cryptoStream);
							EcEIUfjjVxNFKODXnC2(xuFOhpz0Mnl6lRpPtN);
							int num31 = 1;
							if (PD7vDvInyHFEXTKjKoE() == null)
							{
								num31 = 0;
							}
							while (true)
							{
								switch (num31)
								{
								default:
									if (num32 == 988)
									{
										num31 = num32;
										continue;
									}
									break;
								case 0:
									break;
								}
								break;
							}
						}
						catch
						{
							int num33 = 0;
							if (PD7vDvInyHFEXTKjKoE() != null)
							{
								num33 = 4;
							}
							while (true)
							{
								switch (num33)
								{
								default:
									if (num34 == 989)
									{
										num33 = num34;
										continue;
									}
									break;
								case 0:
									break;
								case 1:
									goto end_IL_20d5;
								}
								flag = true;
								num33 = 1;
								if (PD7vDvInyHFEXTKjKoE() != null)
								{
									num33 = 4;
								}
								continue;
								end_IL_20d5:
								break;
							}
						}
						goto case 2;
					case 12:
						if (xFRpNbvIbJ != null)
						{
							num3 = 11;
							if (PD7vDvInyHFEXTKjKoE() != null)
							{
								num3 = 0;
							}
							continue;
						}
						goto case 19;
					case 13:
						try
						{
							int num4;
							if (binaryReader == null)
							{
								num4 = 8;
								if (PD7vDvInyHFEXTKjKoE() == null)
								{
									num4 = 2;
								}
								goto IL_2197;
							}
							goto IL_21bb;
							IL_2197:
							while (true)
							{
								switch (num4)
								{
								default:
									if (num5 == 990)
									{
										goto IL_2193;
									}
									goto end_IL_2197;
								case 1:
									break;
								case 2:
									goto end_IL_2197;
								case 0:
									goto end_IL_2197;
								}
								goto IL_21bb;
								IL_2193:
								num4 = num5;
								continue;
								end_IL_2197:
								break;
							}
							goto end_IL_2164;
							IL_21bb:
							RURkYxjd88OospqRVyp(binaryReader);
							num4 = 0;
							if (!PJBdBCIyCfKrfofamI9())
							{
								num4 = 8;
							}
							goto IL_2197;
							end_IL_2164:;
						}
						catch
						{
							int num6 = 0;
							if (PD7vDvInyHFEXTKjKoE() != null)
							{
								num6 = 4;
							}
							while (true)
							{
								switch (num6)
								{
								default:
									if (num7 == 988)
									{
										num6 = num7;
										continue;
									}
									break;
								case 0:
									break;
								}
								break;
							}
						}
						goto case 16;
					case 3:
						flag = false;
						num3 = 26;
						if (PJBdBCIyCfKrfofamI9())
						{
							num3 = 6;
						}
						continue;
					case 5:
						return;
					case 18:
						xFRpNbvIbJ = new RSACryptoServiceProvider();
						num3 = 7;
						continue;
					}
					goto end_IL_0009;
					continue;
					end_IL_000d:
					break;
				}
				continue;
				end_IL_0009:
				break;
			}
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void j02tVDYfYW(RuntimeTypeHandle P_0)
	{
		try
		{
			Type typeFromHandle = Type.GetTypeFromHandle(P_0);
			if (vgZpsbZLih == null)
			{
				lock (L0hpY2Rke2)
				{
					Dictionary<int, int> dictionary = new Dictionary<int, int>();
					BinaryReader binaryReader = new BinaryReader(Type.GetTypeFromHandle(VbhttmCNbvOE14oqZBl.HaoEcZQw87(33554600)).Assembly.GetManifestResourceStream("vBnaqi3DiyrxoL21uG.40B6y0Ov13HyOnrvDu"));
					binaryReader.BaseStream.Position = 0L;
					byte[] array = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);
					binaryReader.Close();
					if (array.Length != 0)
					{
						int num = array.Length % 4;
						int num2 = array.Length / 4;
						byte[] array2 = new byte[array.Length];
						uint num3 = 0u;
						uint num4 = 0u;
						if (num > 0)
						{
							num2++;
						}
						uint num5 = 0u;
						for (int i = 0; i < num2; i++)
						{
							int num6 = i * 4;
							uint num7 = 255u;
							int num8 = 0;
							if (i == num2 - 1 && num > 0)
							{
								num4 = 0u;
								for (int j = 0; j < num; j++)
								{
									if (j > 0)
									{
										num4 <<= 8;
									}
									num4 |= array[array.Length - (1 + j)];
								}
							}
							else
							{
								num5 = (uint)num6;
								num4 = (uint)((array[num5 + 3] << 24) | (array[num5 + 2] << 16) | (array[num5 + 1] << 8) | array[num5]);
							}
							num3 = num3;
							uint num9 = num3;
							uint num10 = num3;
							uint num11 = 1400895850u;
							uint num12 = 994329632u;
							uint num13 = 1117048405u;
							uint num14 = 1331402446u;
							uint num15 = num10;
							ulong num16 = num12 * num14;
							if (num16 == 0)
							{
								num16--;
							}
							num11 = (uint)(num11 * num11 % num16);
							num13 ^= num12;
							num16 = num11 * 1026830853;
							if (num16 == 0)
							{
								num16--;
							}
							num12 = (uint)(num12 * num12 % num16);
							if (num14 == 0)
							{
								num14--;
							}
							uint num17 = num11 / num14 + num14;
							num14 = ((num11 + num11) ^ num17) + num11;
							if (num15 == 0)
							{
								num15--;
							}
							num17 = num11 / num15 + num15;
							num15 = num11 - num11 + num17 + num11;
							num15 ^= num15 << 7;
							num15 += num12;
							num15 ^= num15 >> 1;
							num15 += num14;
							num15 ^= num15 << 25;
							num15 += num15;
							num15 = (((num14 << 3) + num14) ^ num14) + num15;
							num3 = num9 + (uint)(double)num15;
							if (i == num2 - 1 && num > 0)
							{
								uint num18 = num3 ^ num4;
								for (int k = 0; k < num; k++)
								{
									if (k > 0)
									{
										num7 <<= 8;
										num8 += 8;
									}
									array2[num6 + k] = (byte)((num18 & num7) >> num8);
								}
							}
							else
							{
								uint num19 = num3 ^ num4;
								array2[num6] = (byte)(num19 & 0xFF);
								array2[num6 + 1] = (byte)((num19 & 0xFF00) >> 8);
								array2[num6 + 2] = (byte)((num19 & 0xFF0000) >> 16);
								array2[num6 + 3] = (byte)((num19 & 0xFF000000u) >> 24);
							}
						}
						array = array2;
						array2 = null;
						int num20 = array.Length / 8;
						IXuFOhpz0Mnl6lRpPtN xuFOhpz0Mnl6lRpPtN = new IXuFOhpz0Mnl6lRpPtN(new MemoryStream(array));
						for (int l = 0; l < num20; l++)
						{
							int key = xuFOhpz0Mnl6lRpPtN.YWICtXkOiC();
							int value = xuFOhpz0Mnl6lRpPtN.YWICtXkOiC();
							dictionary.Add(key, value);
						}
						xuFOhpz0Mnl6lRpPtN.dbaCpIGGUj();
					}
					vgZpsbZLih = dictionary;
				}
			}
			FieldInfo[] fields = typeFromHandle.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetField);
			for (int m = 0; m < fields.Length; m++)
			{
				try
				{
					FieldInfo fieldInfo = fields[m];
					int metadataToken = fieldInfo.MetadataToken;
					int num21 = vgZpsbZLih[metadataToken];
					bool flag = (num21 & 0x40000000) > 0;
					num21 &= 0x3FFFFFFF;
					MethodInfo methodInfo = (MethodInfo)Type.GetTypeFromHandle(VbhttmCNbvOE14oqZBl.HaoEcZQw87(33554600)).Module.ResolveMethod(num21, typeFromHandle.GetGenericArguments(), new Type[0]);
					if (methodInfo.IsStatic)
					{
						fieldInfo.SetValue(null, Delegate.CreateDelegate(fieldInfo.FieldType, methodInfo));
						continue;
					}
					ParameterInfo[] parameters = methodInfo.GetParameters();
					int num22 = parameters.Length + 1;
					Type[] array3 = new Type[num22];
					if (methodInfo.DeclaringType.IsValueType)
					{
						array3[0] = methodInfo.DeclaringType.MakeByRefType();
					}
					else
					{
						array3[0] = Type.GetTypeFromHandle(VbhttmCNbvOE14oqZBl.HaoEcZQw87(16777236));
					}
					for (int n = 0; n < parameters.Length; n++)
					{
						array3[n + 1] = parameters[n].ParameterType;
					}
					DynamicMethod dynamicMethod = new DynamicMethod(string.Empty, methodInfo.ReturnType, array3, typeFromHandle, skipVisibility: true);
					ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
					for (int num23 = 0; num23 < num22; num23++)
					{
						switch (num23)
						{
						case 0:
							iLGenerator.Emit(OpCodes.Ldarg_0);
							break;
						case 1:
							iLGenerator.Emit(OpCodes.Ldarg_1);
							break;
						case 2:
							iLGenerator.Emit(OpCodes.Ldarg_2);
							break;
						case 3:
							iLGenerator.Emit(OpCodes.Ldarg_3);
							break;
						default:
							iLGenerator.Emit(OpCodes.Ldarg_S, num23);
							break;
						}
					}
					iLGenerator.Emit(OpCodes.Tailcall);
					iLGenerator.Emit(flag ? OpCodes.Callvirt : OpCodes.Call, methodInfo);
					iLGenerator.Emit(OpCodes.Ret);
					fieldInfo.SetValue(null, dynamicMethod.CreateDelegate(typeFromHandle));
				}
				catch (Exception)
				{
				}
			}
		}
		catch (Exception)
		{
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void UKytE77xrn()
	{
		if (Debugger.IsAttached)
		{
			throw new Exception("Debugger Detected");
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void JTTtBBAZKo(object P_0, int P_1)
	{
		o2UlQhC07i31n34QSqq.dycCzZqxDd(0, new object[2] { P_0, P_1 }, null);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static string E5Vt0LlcI4(int P_0)
	{
		if (((Array)KP2pZlT0EB).Length == 0)
		{
			nG9pKEBfgc = new List<string>();
			hwXp49PyZA = new List<int>();
			JTTtBBAZKo(((Assembly)oiIp3r0OAK).GetManifestResourceStream("BCOUkWcZUC9eWxAkce.UjF4RwtG81oxfpn6I3"), P_0);
		}
		if (u5mpXnDHeE < 75)
		{
			MethodBase method = new StackFrame(1).GetMethod();
			if ((Assembly)oiIp3r0OAK != method.DeclaringType.Assembly)
			{
				bool flag = false;
				string name = method.DeclaringType.Assembly.GetName().Name;
				AssemblyName[] referencedAssemblies = ((Assembly)oiIp3r0OAK).GetReferencedAssemblies();
				foreach (AssemblyName assemblyName in referencedAssemblies)
				{
					if (name == assemblyName.Name)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					throw new Exception();
				}
			}
			u5mpXnDHeE++;
		}
		lock (ok5pe9P6VR)
		{
			int num = BitConverter.ToInt32((byte[])KP2pZlT0EB, P_0);
			if (num < hwXp49PyZA.Count && hwXp49PyZA[num] == P_0)
			{
				return nG9pKEBfgc[num];
			}
			try
			{
				udQmOjCORxRL7YsRZe0.qcbEGyxWLX();
				byte[] array = new byte[num];
				Array.Copy((Array)KP2pZlT0EB, P_0 + 4, array, 0, num);
				string text = Encoding.Unicode.GetString(array, 0, array.Length);
				nG9pKEBfgc.Add(text);
				hwXp49PyZA.Add(P_0);
				Array.Copy(BitConverter.GetBytes(nG9pKEBfgc.Count - 1), 0, (Array)KP2pZlT0EB, P_0, 4);
				return text;
			}
			catch
			{
			}
		}
		return "";
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static string pF4tAvH57q(object P_0)
	{
		"OBv0TYHLj0whyTlEhSBiYd".Trim();
		byte[] array = Convert.FromBase64String((string)P_0);
		return Encoding.Unicode.GetString(array, 0, array.Length);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static uint m8wtvyr1sn(IntPtr P_0, IntPtr P_1, IntPtr P_2, uint P_3, IntPtr P_4, ref uint P_5)
	{
		IntPtr ptr = P_2;
		if (Dr6pWbnnZU)
		{
			ptr = P_1;
		}
		long num = 0L;
		num = ((IntPtr.Size != 4) ? Marshal.ReadInt64(ptr, IntPtr.Size * 2) : Marshal.ReadInt32(ptr, IntPtr.Size * 2));
		object obj = ((Hashtable)Tkmp0tBXb9)[(object)num];
		if (obj != null)
		{
			axoD13poAOy2RTNJPvL axoD13poAOy2RTNJPvL2 = (axoD13poAOy2RTNJPvL)obj;
			IntPtr intPtr = Marshal.AllocCoTaskMem(axoD13poAOy2RTNJPvL2.WT0plHug2U.Length);
			Marshal.Copy(axoD13poAOy2RTNJPvL2.WT0plHug2U, 0, intPtr, axoD13poAOy2RTNJPvL2.WT0plHug2U.Length);
			if (axoD13poAOy2RTNJPvL2.XNBpQ9cWcG)
			{
				P_4 = intPtr;
				P_5 = (uint)axoD13poAOy2RTNJPvL2.WT0plHug2U.Length;
				iThthenarx(P_4, axoD13poAOy2RTNJPvL2.WT0plHug2U.Length, 64, ref rI2p9xxOpU);
				return 0u;
			}
			Marshal.WriteIntPtr(ptr, IntPtr.Size * 2, intPtr);
			Marshal.WriteInt32(ptr, IntPtr.Size * 3, axoD13poAOy2RTNJPvL2.WT0plHug2U.Length);
			uint result = 0u;
			if (P_3 != 216669565 || JEepBXDRxl)
			{
				result = UC7pfXZD4N(P_0, P_1, P_2, P_3, P_4, ref P_5);
				Marshal.WriteIntPtr(ptr, IntPtr.Size * 2, IntPtr.Zero);
			}
			else
			{
				JEepBXDRxl = true;
			}
			return result;
		}
		return UC7pfXZD4N(P_0, P_1, P_2, P_3, P_4, ref P_5);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static int SovtrtpXRg()
	{
		return 5;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void KpjtiFf2dc()
	{
		try
		{
			RSACryptoServiceProvider.UseMachineKeyStore = true;
		}
		catch
		{
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Delegate LGdtLrRvkh(IntPtr P_0, Type P_1)
	{
		return (Delegate)Type.GetTypeFromHandle(VbhttmCNbvOE14oqZBl.HaoEcZQw87(16777417)).GetMethod("GetDelegateForFunctionPointer", new Type[2]
		{
			Type.GetTypeFromHandle(VbhttmCNbvOE14oqZBl.HaoEcZQw87(16777246)),
			Type.GetTypeFromHandle(VbhttmCNbvOE14oqZBl.HaoEcZQw87(16777250))
		}).Invoke(null, new object[2] { P_0, P_1 });
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal unsafe static void D5ctJ5txFm()
	{
		int num = 215;
		byte[] array = default;
		int num16 = default;
		byte[] array5 = default;
		int num15 = default;
		byte[] array3 = default;
		IXuFOhpz0Mnl6lRpPtN xuFOhpz0Mnl6lRpPtN = default;
		int num24 = default;
		int num26 = default;
		byte[] array13 = default;
		byte[] array2 = default;
		int num19 = default;
		axoD13poAOy2RTNJPvL axoD13poAOy2RTNJPvL3 = default;
		byte[] array4 = default;
		int num46 = default;
		uint num5 = default;
		uint num65 = default;
		IntPtr intPtr3 = default;
		IntPtr intPtr4 = default;
		int num14 = default;
		int num21 = default;
		long num18 = default;
		byte[] array8 = default;
		int num17 = default;
		byte[] array7 = default;
		int num23 = default;
		bool xNBpQ9cWcG = default;
		int num50 = default;
		int num29 = default;
		IEnumerator enumerator = default;
		Version version2 = default;
		Version version3 = default;
		Version version = default;
		int num91 = default;
		ProcessModule processModule2 = default;
		int num94 = default;
		byte[] array11 = default;
		Process process = default;
		int num87 = default;
		int num82 = default;
		ProcessModule processModule = default;
		string text = default;
		long num28 = default;
		IntPtr intPtr7 = default;
		int num86 = default;
		int num89 = default;
		IntPtr intPtr8 = default;
		uint num42 = default;
		int num49 = default;
		int num41 = default;
		long value = default;
		IntPtr intPtr9 = default;
		int num64 = default;
		uint num54 = default;
		byte[] array6 = default;
		uint num69 = default;
		string text2 = default;
		byte[] array9 = default;
		int num31 = default;
		ICryptoTransform transform = default;
		byte[] array10 = default;
		byte[] array12 = default;
		uint num25 = default;
		IntPtr intPtr6 = default;
		IntPtr intPtr5 = default;
		int num47 = default;
		byte* ptr = default;
		int num48 = default;
		long num44 = default;
		int num45 = default;
		axoD13poAOy2RTNJPvL axoD13poAOy2RTNJPvL2 = default;
		IntPtr intPtr = default;
		IntPtr intPtr2 = default;
		int num27 = default;
		int num68 = default;
		CryptoStream cryptoStream = default;
		byte[] array14 = default;
		byte[] array18 = default;
		byte[] array21 = default;
		int num66 = default;
		int num51 = default;
		uint num63 = default;
		uint num67 = default;
		int num71 = default;
		int num73 = default;
		int num75 = default;
		int num77 = default;
		int num52 = default;
		int num30 = default;
		int num22 = default;
		int num53 = default;
		int num20 = default;
		int num56 = default;
		int num58 = default;
		int num60 = default;
		int num62 = default;
		int num43 = default;
		byte[] wT0plHug2U = default;
		int num34 = default;
		byte[] array15 = default;
		int num36 = default;
		byte* value2 = default;
		uint nativeSizeOfCode = default;
		int num38 = default;
		MemoryStream memoryStream = default;
		int num40 = default;
		while (true)
		{
			int num2 = num;
			while (true)
			{
				int num3 = num2;
				while (true)
				{
					IntPtr zero;
					switch (num3)
					{
					default:
						if (num2 != 687)
						{
							if (num2 == 1668)
							{
								goto end_IL_000d;
							}
							goto case 173;
						}
						array[num16 + 2] = array5[2];
						num3 = 118;
						continue;
					case 641:
						num15 = 70 + 58;
						num3 = 438;
						continue;
					case 560:
						array3[1] = (byte)num15;
						num3 = 1;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 23;
						}
						continue;
					case 157:
						TU7iRFEBtFw96YP0kQA(xBXNeQEE5ufUE0ju7wc(xuFOhpz0Mnl6lRpPtN), 0L);
						num3 = 151;
						continue;
					case 554:
						array3[25] = 145;
						num3 = 683;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 134;
						}
						continue;
					case 31:
						num24 = num26 * 4;
						num3 = 542;
						continue;
					case 26:
						num15 = 148 - 49;
						num3 = 368;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 34;
						}
						continue;
					case 303:
						array13[7] = 100;
						num3 = 447;
						continue;
					case 471:
						num15 = 62 + 84;
						num3 = 23;
						continue;
					case 106:
						num15 = 162 - 54;
						num3 = 489;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 126;
						}
						continue;
					case 651:
						array3[4] = 123;
						num3 = 167;
						continue;
					case 158:
						array[num16 + 5] = array2[5];
						num3 = 302;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 450;
						}
						continue;
					case 216:
						num19 = 144 - 43;
						num3 = 153;
						continue;
					case 140:
						array3[7] = (byte)num15;
						num3 = 228;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 35;
						}
						continue;
					case 9:
						yRop5oobS6 = false;
						num3 = 632;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 393;
						}
						continue;
					case 114:
						axoD13poAOy2RTNJPvL3.WT0plHug2U = new byte[1] { 42 };
						num = 211;
						break;
					case 136:
						array4[15] = 5;
						num3 = 674;
						continue;
					case 673:
						num15 = 103 + 96;
						num3 = 424;
						continue;
					case 25:
						array3[16] = 91;
						num3 = 501;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 238;
						}
						continue;
					case 115:
						num46 = fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN);
						num3 = 595;
						continue;
					case 340:
						array3[4] = 153;
						num3 = 337;
						continue;
					case 124:
					case 376:
						if (SgdaD5E42usnNCLHEG7() == 4)
						{
							num3 = 75;
							if (uuxvQpECJ1ciupObMkA())
							{
								num3 = 276;
							}
							continue;
						}
						goto case 443;
					case 185:
						array[num16 + 2] = array2[2];
						num3 = 647;
						continue;
					case 133:
						num15 = 160 - 53;
						num3 = 265;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 279;
						}
						continue;
					case 313:
						num5 += num65;
						num3 = 177;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 43;
						}
						continue;
					case 456:
						aHdtFwAZdE(intPtr3, intPtr4, (byte[])vudCNGBceuiJAxRZ40b(fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN)), 4u, out zero);
						num3 = 421;
						continue;
					case 409:
						UC7pfXZD4N = null;
						num3 = 153;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 392;
						}
						continue;
					case 209:
						array[num14] = array5[0];
						num3 = 178;
						continue;
					case 356:
						array3[5] = 84;
						num3 = 645;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 656;
						}
						continue;
					case 491:
						array13[11] = 108;
						num = 24;
						break;
					case 10:
						num21 = 54 + 6;
						num3 = 146;
						continue;
					case 573:
						FCiKtIEH87VdVS5yRIs(new IntPtr(&num18), 0);
						num3 = 49;
						continue;
					case 661:
						dhnayNEaKfZVeU2YOuq(new IntPtr(&num18), 0, IntPtr.Zero);
						num3 = 606;
						continue;
					case 657:
						array3[15] = (byte)num15;
						num3 = 641;
						continue;
					case 569:
						array3[9] = (byte)num15;
						num3 = 196;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 12;
						}
						continue;
					case 159:
					{
						byte[] array20 = new byte[30];
						hQ3U1TBg3I8im4HbnBK(array20, __ldtoken(_003CPrivateImplementationDetails_003E_007B16B6D898_002D2657_002D4B50_002DB8EB_002DDDC3B4FFE416_007D.D5B7247C497788CF0031CEB06E3DF77A45FEF59F1E49633DC7159816D64759B5));
						array8 = array20;
						num3 = 57;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 123;
						}
						continue;
					}
					case 525:
						array3[7] = (byte)num15;
						num3 = 226;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 114;
						}
						continue;
					case 327:
						array13[6] = 46;
						num3 = 303;
						continue;
					case 505:
						array13[2] = 99;
						num3 = 172;
						continue;
					case 407:
						if (num17 > 0)
						{
							num3 = 602;
							if (I8wSSlETnYL0FFJbX98() == null)
							{
								num3 = 602;
							}
							continue;
						}
						goto case 653;
					case 242:
						array3[31] = (byte)num15;
						num3 = 351;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 218;
						}
						continue;
					case 435:
						array[num16 + 3] = array7[3];
						num3 = 436;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 299;
						}
						continue;
					case 201:
						array3[16] = 49;
						num3 = 26;
						continue;
					case 65:
						if (iThthenarx(intPtr4, 4, 4, ref num23) == 0)
						{
							num3 = 308;
							continue;
						}
						goto case 190;
					case 428:
						array5 = (byte[])vudCNGBceuiJAxRZ40b(TIypExNM5B.ToInt32());
						num3 = 535;
						continue;
					case 353:
						xNBpQ9cWcG = true;
						num3 = 115;
						continue;
					case 184:
						array3[21] = (byte)num15;
						num3 = 381;
						continue;
					case 637:
						array4[9] = 89;
						num3 = 488;
						continue;
					case 663:
						array3[17] = (byte)num15;
						num3 = 474;
						continue;
					case 675:
						array3[21] = 97;
						num3 = 673;
						continue;
					case 580:
						num50 = 0;
						num3 = 684;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 436;
						}
						continue;
					case 221:
						num15 = 150 - 50;
						num3 = 490;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 290;
						}
						continue;
					case 236:
						if (num29 > 0)
						{
							num3 = 545;
							continue;
						}
						goto case 166;
					case 110:
						try
						{
							while (true)
							{
								IL_12fc:
								int num90;
								if (!RbgEoLE5KuKXiRdxkVL(enumerator))
								{
									num90 = 2;
									if (uuxvQpECJ1ciupObMkA())
									{
										num90 = 6;
									}
									goto IL_1139;
								}
								goto IL_12d4;
								IL_1139:
								while (true)
								{
									switch (num90)
									{
									case 9:
										goto end_IL_1139;
									case 3:
										Dr6pWbnnZU = true;
										num90 = 9;
										if (I8wSSlETnYL0FFJbX98() != null)
										{
											num90 = 13;
										}
										continue;
									case 1:
										version2 = new Version(4, 0, 30319, 17921);
										num90 = 11;
										if (I8wSSlETnYL0FFJbX98() == null)
										{
											num90 = 4;
										}
										continue;
									case 8:
										goto IL_1234;
									case 2:
										if (!MgfZRJEV5FjntbxPaC8(version3, version2))
										{
											num90 = 15;
											if (uuxvQpECJ1ciupObMkA())
											{
												num90 = 0;
											}
											continue;
										}
										goto case 3;
									case 4:
										if (!UQumgiE113VEq0iGoO7(version3, version))
										{
											num90 = 11;
											continue;
										}
										goto case 2;
									case 12:
										goto IL_12d4;
									case 0:
									case 5:
									case 10:
									case 11:
										goto IL_12fc;
									case 7:
										version = new Version(4, 0, 30319, 17020);
										num90 = 6;
										if (I8wSSlETnYL0FFJbX98() == null)
										{
											num90 = 1;
										}
										continue;
									case 6:
										goto end_IL_1139;
									}
									if (num91 != 20)
									{
										if (num91 == 1000)
										{
											goto IL_1135;
										}
									}
									else if (!CWKCv5Eq4UPkyyCiFtI(NMbsAvEdy3ENFVSckWS(Aj46vUER2Xh1OGvrQPQ(processModule2)), "clrjit.dll"))
									{
										num90 = 3;
										if (uuxvQpECJ1ciupObMkA())
										{
											num90 = 5;
										}
										continue;
									}
									goto IL_1234;
									IL_1234:
									version3 = new Version(It7SadEmuESeuGKbChj(G59GRIEgmGievpDFQON(processModule2)), zbADxOEDxXpmJfBEIgu(G59GRIEgmGievpDFQON(processModule2)), LPuUHZEf2Xo89O4SP3I(G59GRIEgmGievpDFQON(processModule2)), hTZZwCE8fEHsfGelStj(G59GRIEgmGievpDFQON(processModule2)));
									num90 = 14;
									if (uuxvQpECJ1ciupObMkA())
									{
										num90 = 7;
									}
									continue;
									end_IL_1139:
									break;
								}
								break;
								IL_1135:
								num90 = num91;
								goto IL_1139;
								IL_12d4:
								processModule2 = (ProcessModule)Cxls5EE2TSaVpL1WUwJ(enumerator);
								int num92 = 12;
								if (uuxvQpECJ1ciupObMkA())
								{
									num92 = 20;
								}
								num91 = num92;
								goto IL_1135;
							}
						}
						finally
						{
							IDisposable disposable = enumerator as IDisposable;
							int num93 = 1;
							if (I8wSSlETnYL0FFJbX98() != null)
							{
								num93 = 0;
							}
							while (true)
							{
								switch (num93)
								{
								default:
									if (num94 == 990)
									{
										num93 = num94;
										continue;
									}
									goto case 0;
								case 0:
									rmnsm4E7lkuVAYH0HlX(disposable);
									num93 = 2;
									continue;
								case 1:
									if (disposable != null)
									{
										num93 = 1;
										if (uuxvQpECJ1ciupObMkA())
										{
											num93 = 0;
										}
										continue;
									}
									break;
								case 2:
									break;
								}
								break;
							}
						}
						goto case 182;
					case 582:
						array4[15] = (byte)num19;
						num3 = 155;
						continue;
					case 163:
						if (array11.Length != 0)
						{
							num3 = 269;
							continue;
						}
						goto case 174;
					case 601:
						CmWpV9oDlv = evmPENBHbcVC6H3C7bi(CU7p1vJVVx);
						num3 = 633;
						continue;
					case 455:
						num26 = 0;
						num3 = 649;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 363;
						}
						continue;
					case 577:
						array4[9] = (byte)num21;
						num3 = 252;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 24;
						}
						continue;
					case 320:
						array3[14] = (byte)num15;
						num = 567;
						break;
					case 627:
						try
						{
							enumerator = (IEnumerator)Jk84DoEUURplhj96WnG(BQ3m4pEx8YZHx0Eexob(process));
							int num79 = 5;
							if (I8wSSlETnYL0FFJbX98() == null)
							{
								num79 = 0;
							}
							while (true)
							{
								switch (num79)
								{
								default:
									if (num87 == 989)
									{
										goto IL_14df;
									}
									break;
								case 0:
									try
									{
										while (true)
										{
											int num80;
											if (!RbgEoLE5KuKXiRdxkVL(enumerator))
											{
												num80 = 3;
												goto IL_1525;
											}
											goto IL_16a3;
											IL_1525:
											while (true)
											{
												switch (num80)
												{
												default:
													if (num82 != 16)
													{
														if (num82 == 996)
														{
															goto IL_1521;
														}
														goto IL_16a3;
													}
													return;
												case 0:
													if (CWKCv5Eq4UPkyyCiFtI(Aj46vUER2Xh1OGvrQPQ(processModule), text))
													{
														num80 = 2;
														if (uuxvQpECJ1ciupObMkA())
														{
															num80 = 2;
														}
														continue;
													}
													break;
												case 7:
													if (!KuB6QHBXIEOPyPl4IG4(shAwjRBYlE7VDUqNr8d(gXojVCBb5v3rsfrU2bj(typeof(fJUeITteiwtDJsNdQol).TypeHandle).Assembly), null))
													{
														num80 = 3;
														if (uuxvQpECJ1ciupObMkA())
														{
															num80 = 5;
														}
														continue;
													}
													goto case 6;
												case 4:
												case 5:
													break;
												case 1:
												{
													long num83 = num28;
													intPtr7 = CUvyUTEYPF2YBKIDdnt(processModule);
													if (num83 > intPtr7.ToInt64() + BGXTXHBsFJeJyu1Pl99(processModule))
													{
														num80 = 2;
														if (I8wSSlETnYL0FFJbX98() == null)
														{
															num80 = 7;
														}
														continue;
													}
													break;
												}
												case 6:
												{
													P3BUAkEKywrB1yX9eqY();
													int num84 = 1;
													if (I8wSSlETnYL0FFJbX98() == null)
													{
														num84 = 16;
													}
													num82 = num84;
													goto IL_1521;
												}
												case 2:
												{
													long num81 = num28;
													intPtr7 = CUvyUTEYPF2YBKIDdnt(processModule);
													if (num81 >= intPtr7.ToInt64())
													{
														num80 = 2;
														if (I8wSSlETnYL0FFJbX98() == null)
														{
															num80 = 1;
														}
														continue;
													}
													goto case 7;
												}
												case 8:
													goto IL_16a3;
												case 3:
													goto end_IL_15d5;
													IL_1521:
													num80 = num82;
													continue;
												}
												break;
											}
											continue;
											IL_16a3:
											processModule = (ProcessModule)Cxls5EE2TSaVpL1WUwJ(enumerator);
											num80 = 6;
											if (I8wSSlETnYL0FFJbX98() == null)
											{
												num80 = 0;
											}
											goto IL_1525;
											continue;
											end_IL_15d5:
											break;
										}
									}
									finally
									{
										IDisposable disposable = enumerator as IDisposable;
										int num85 = 2;
										if (!uuxvQpECJ1ciupObMkA())
										{
											num85 = 4;
										}
										while (true)
										{
											switch (num85)
											{
											default:
												if (num86 == 990)
												{
													num85 = num86;
													continue;
												}
												break;
											case 2:
												if (disposable != null)
												{
													num85 = 0;
													if (!uuxvQpECJ1ciupObMkA())
													{
														num85 = 8;
													}
													continue;
												}
												goto end_IL_1705;
											case 0:
												break;
											case 1:
												goto end_IL_1705;
											}
											rmnsm4E7lkuVAYH0HlX(disposable);
											num85 = 1;
											if (I8wSSlETnYL0FFJbX98() != null)
											{
												num85 = 5;
											}
											continue;
											end_IL_1705:
											break;
										}
									}
									break;
								case 1:
									break;
								}
								break;
								IL_14df:
								num79 = num87;
							}
						}
						catch
						{
							int num88 = 0;
							if (I8wSSlETnYL0FFJbX98() != null)
							{
								num88 = 4;
							}
							while (true)
							{
								switch (num88)
								{
								default:
									if (num89 == 988)
									{
										num88 = num89;
										continue;
									}
									break;
								case 0:
									break;
								}
								break;
							}
						}
						goto case 664;
					case 509:
						intPtr8 = Y198cyBN94CljEWnwQb(YL9p8TnZg2);
						num3 = 351;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 312;
						}
						continue;
					case 534:
						array3[20] = (byte)num15;
						num3 = 312;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 44;
						}
						continue;
					case 137:
						lbaLKKBqkRocLAuRSLQ(BlvUFfBd6d3r2OAivQH(S7MyTKBemOIkUfsNeSA(YL9p8TnZg2)));
						num3 = 357;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 301;
						}
						continue;
					case 200:
						array13[5] = 116;
						num3 = 327;
						continue;
					case 6:
						array3[2] = 41;
						num3 = 59;
						continue;
					case 511:
						J8Xf5OEbihPVVoNWZDc(new IntPtr(&num18), 0);
						num = 573;
						break;
					case 90:
						array[num14] = array2[0];
						num3 = 539;
						continue;
					case 458:
						num21 = 22 + 81;
						num = 479;
						break;
					case 230:
						num42 = 0u;
						num3 = 326;
						continue;
					case 416:
						array4[5] = (byte)num19;
						num3 = 359;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 10;
						}
						continue;
					case 67:
					case 421:
						iThthenarx(intPtr4, 4, num23, ref num23);
						num3 = 94;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 353;
						}
						continue;
					case 171:
					case 549:
						if (num49 >= num41)
						{
							num3 = 208;
							continue;
						}
						goto case 175;
					case 169:
						if (SgdaD5E42usnNCLHEG7() == 4)
						{
							num3 = 198;
							if (!uuxvQpECJ1ciupObMkA())
							{
								num3 = 528;
							}
							continue;
						}
						goto case 473;
					case 523:
						array3[10] = 166;
						num3 = 558;
						continue;
					case 652:
						array4[9] = 226;
						num3 = 36;
						continue;
					case 492:
						num15 = 123 + 66;
						num3 = 639;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 154;
						}
						continue;
					case 161:
						array3[12] = 96;
						num3 = 546;
						continue;
					case 324:
						if (num29 > 0)
						{
							num = 461;
							break;
						}
						goto case 380;
					case 75:
						num28 = CN8yIfBIFxjCZ2HuoAV(new IntPtr(value));
						num3 = 28;
						continue;
					case 635:
						if (iThthenarx(intPtr9, num64 * 4, 4, ref num23) == 0)
						{
							num3 = 428;
							if (uuxvQpECJ1ciupObMkA())
							{
								num3 = 283;
							}
							continue;
						}
						goto case 591;
					case 388:
						if (num54 == 4109628145u)
						{
							num = 173;
							break;
						}
						goto case 477;
					case 351:
						num28 = 0L;
						num3 = 341;
						continue;
					case 557:
						array4[10] = (byte)num19;
						num3 = 272;
						continue;
					case 593:
						array3[24] = (byte)num15;
						num = 71;
						break;
					case 619:
						num65 = (uint)((array6[num69 + 3] << 24) | (array6[num69 + 2] << 16) | (array6[num69 + 1] << 8) | array6[num69]);
						num3 = 578;
						continue;
					case 551:
						text2 = (string)TFvqHhBajDhL2Odnoid(roj8qlBksDm0T3ssU6j(), array9);
						num3 = 139;
						continue;
					case 270:
						array3[29] = 16;
						num3 = 286;
						continue;
					case 202:
						num31 += 8;
						num3 = 168;
						continue;
					case 187:
						num15 = 63 + 111;
						num3 = 638;
						continue;
					case 605:
						array4[13] = (byte)num19;
						num3 = 430;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 10;
						}
						continue;
					case 173:
						if (upIsmQEeuONn9gvIqmw(gKNwcsEXWkHidi0ONST(CUvyUTEYPF2YBKIDdnt(pwr2RZEsbqGGte5A622(QdOaUHEN5v6v9LTg48u())), "__", 10u), IntPtr.Zero))
						{
							num3 = 493;
							if (uuxvQpECJ1ciupObMkA())
							{
								num3 = 477;
							}
							continue;
						}
						goto case 141;
					case 608:
						array9[4] = 105;
						num3 = 594;
						continue;
					case 272:
						array4[10] = 207;
						num3 = 58;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 109;
						}
						continue;
					case 449:
						array3[19] = (byte)num15;
						num3 = 375;
						continue;
					case 541:
						array5 = null;
						num3 = 227;
						continue;
					case 103:
						array3[27] = (byte)num15;
						num3 = 22;
						continue;
					case 178:
						array[num14 + 1] = array5[1];
						num3 = 213;
						continue;
					case 42:
						array9[1] = 101;
						num3 = 54;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 406;
						}
						continue;
					case 618:
						array13[5] = 106;
						num3 = 547;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 343;
						}
						continue;
					case 44:
						fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN);
						num3 = 314;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 170;
						}
						continue;
					case 231:
						num15 = 147 - 49;
						num3 = 184;
						continue;
					case 678:
						num15 = 177 - 59;
						num3 = 223;
						continue;
					case 583:
						array13[9] = 108;
						num3 = 619;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 30;
						}
						continue;
					case 30:
						text = (string)TFvqHhBajDhL2Odnoid(roj8qlBksDm0T3ssU6j(), array13);
						num3 = 378;
						continue;
					case 644:
						array4[8] = 116;
						num3 = 216;
						continue;
					case 279:
						num65 = 0u;
						num3 = 444;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 275;
						}
						continue;
					case 369:
						P3BUAkEKywrB1yX9eqY();
						num3 = 579;
						continue;
					case 558:
						num15 = 147 - 49;
						num3 = 177;
						continue;
					case 536:
					{
						object obj4 = fSUbccE6gsPRdagQOXW();
						vIxlb6EFZOHTaTmCWFM(obj4, CipherMode.CBC);
						transform = (ICryptoTransform)q9lKaTEhO6mgyr9hh5d(obj4, array6, array10);
						num3 = 331;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 543;
						}
						continue;
					}
					case 214:
						p74pqHljgy = true;
						num3 = 649;
						continue;
					case 592:
						array4[14] = 62;
						num = 610;
						break;
					case 518:
						num15 = 196 - 65;
						num3 = 242;
						continue;
					case 590:
						intPtr8 = IntPtr.Zero;
						num3 = 598;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 509;
						}
						continue;
					case 127:
						array4[15] = (byte)num19;
						num3 = 322;
						continue;
					case 470:
						array3[23] = 99;
						num3 = 506;
						continue;
					case 5:
						array12[num24 + 2] = (byte)((num25 & 0xFF0000) >> 16);
						num3 = 12;
						continue;
					case 448:
						array4[15] = 172;
						num3 = 136;
						continue;
					case 606:
						OxrsHJEWKbstiffRAGw(new IntPtr(&num18), 0, 0);
						num3 = 614;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 221;
						}
						continue;
					case 568:
						array4[7] = (byte)num19;
						num3 = 86;
						continue;
					case 597:
						num19 = 172 - 57;
						num3 = 416;
						continue;
					case 479:
						array4[0] = (byte)num21;
						num3 = 680;
						continue;
					case 139:
						intPtr6 = ijeWUlBOw2HhRGFtNBA((baR4qNpS6R180n2nqed)GEgn3GB3c57weI0muKm(MXxtnniQXO(intPtr5, text2), gXojVCBb5v3rsfrU2bj(typeof(baR4qNpS6R180n2nqed).TypeHandle)));
						num3 = 629;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 206;
						}
						continue;
					case 222:
						array9[3] = 74;
						num3 = 17;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 608;
						}
						continue;
					case 475:
						num15 = 13 + 47;
						num3 = 449;
						continue;
					case 218:
						array3[31] = 157;
						num3 = 56;
						continue;
					case 285:
						array[num16 + 4] = array2[4];
						num3 = 158;
						continue;
					case 262:
						array3[24] = 71;
						num3 = 264;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 540;
						}
						continue;
					case 588:
						array13 = new byte[10];
						num3 = 668;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 327;
						}
						continue;
					case 361:
						num15 = 153 - 51;
						num3 = 566;
						continue;
					case 658:
						array[num16 + 4] = array5[4];
						num3 = 333;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 14;
						}
						continue;
					case 410:
						iThthenarx(new IntPtr(value), SgdaD5E42usnNCLHEG7(), num47, ref num47);
						num3 = 143;
						continue;
					case 490:
						array3[8] = (byte)num15;
						num3 = 92;
						continue;
					case 310:
						array4[12] = 81;
						num3 = 412;
						continue;
					case 240:
						*(long*)(ptr + num48 * 8) ^= 418684539L;
						num3 = 197;
						continue;
					case 8:
						num15 = 31 + 25;
						num = 126;
						break;
					case 205:
						num47 = 0;
						num3 = 15;
						continue;
					case 322:
						array4[15] = 130;
						num3 = 448;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 559;
						}
						continue;
					case 138:
						array4[12] = (byte)num19;
						num3 = 407;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 462;
						}
						continue;
					case 400:
						num15 = 87 + 87;
						num3 = 27;
						continue;
					case 336:
						intPtr7 = eSnslyEuFhKIdwficbS(((object[])QFmg2qEJ0fBF7yBvkU4(oiIp3r0OAK))[0]);
						num3 = 282;
						continue;
					case 350:
						WqI3B3BTLoJD1bwHtWc(Tkmp0tBXb9, num44 + num45, axoD13poAOy2RTNJPvL2);
						num3 = 323;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 376;
						}
						continue;
					case 345:
						num15 = 61 - 29;
						num3 = 570;
						continue;
					case 46:
						array[num16] = array2[0];
						num3 = 624;
						continue;
					case 154:
						array3[30] = (byte)num15;
						num3 = 466;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 183;
						}
						continue;
					case 3:
						array3[25] = (byte)num15;
						num3 = 395;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 298;
						}
						continue;
					case 565:
						array3[6] = (byte)num15;
						num3 = 333;
						continue;
					case 306:
						array4[0] = (byte)num19;
						num3 = 497;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 264;
						}
						continue;
					case 266:
						num19 = 0 + 34;
						num3 = 269;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 7;
						}
						continue;
					case 348:
						num19 = 126 - 42;
						num3 = 321;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 23;
						}
						continue;
					case 258:
						array3[0] = (byte)num15;
						num3 = 572;
						continue;
					case 246:
						num15 = 96 + 88;
						num3 = 103;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 209;
						}
						continue;
					case 177:
						array3[10] = (byte)num15;
						num3 = 387;
						continue;
					case 116:
						array4[7] = (byte)num21;
						num3 = 417;
						continue;
					case 636:
					case 655:
						jMbTtXEI5rcr70arXTH(array, 0, intPtr, array.Length);
						num3 = 9;
						continue;
					case 247:
						num15 = 170 + 8;
						num3 = 258;
						continue;
					case 53:
						array3[16] = (byte)num15;
						num3 = 25;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 554;
						}
						continue;
					case 493:
						array[num14 + 1] = array7[1];
						num3 = 433;
						continue;
					case 93:
						array13[9] = 100;
						num3 = 169;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 584;
						}
						continue;
					case 54:
						num19 = 205 - 68;
						num3 = 256;
						continue;
					case 513:
						intPtr3 = YrBNfkBGVgHMF5SPdV0(56u, 1, (uint)IRq7ugEz0abWSx77c4Y(QdOaUHEN5v6v9LTg48u()));
						num3 = 596;
						continue;
					case 671:
						array12[num24 + 1] = (byte)((num25 & 0xFF00) >> 8);
						num3 = 5;
						continue;
					case 108:
						array9[0] = 103;
						num3 = 42;
						continue;
					case 59:
						num15 = 216 - 72;
						num3 = 271;
						continue;
					case 182:
					case 528:
						xuFOhpz0Mnl6lRpPtN = new IXuFOhpz0Mnl6lRpPtN((Stream)M2TtAqE9T1q0ciPjFnH(oiIp3r0OAK, "6FI1sbHZwsnSkKZ8P3.rOIc7hk5ivbobBotQG"));
						num3 = 157;
						continue;
					case 437:
						if (iThthenarx(intPtr2, 4, 4, ref num23) == 0)
						{
							num3 = 319;
							continue;
						}
						goto case 278;
					case 83:
						array[num16 + 1] = array7[1];
						num3 = 372;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 180;
						}
						continue;
					case 620:
						array[num16 + 6] = array5[6];
						num3 = 77;
						continue;
					case 38:
						num15 = 15 + 52;
						num3 = 97;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 241;
						}
						continue;
					case 488:
						num19 = 165 - 55;
						num3 = 144;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 380;
						}
						continue;
					case 326:
						num17 = 0;
						num3 = 131;
						continue;
					case 614:
						kghyJ3E31k2nZG7WavN(new IntPtr(&num18), 0, 0L);
						num3 = 331;
						continue;
					case 249:
						num15 = 59 + 74;
						num3 = 565;
						continue;
					case 332:
						array3[12] = (byte)num15;
						num3 = 63;
						continue;
					case 532:
						num16 = 30;
						num = 46;
						break;
					case 287:
						array3[8] = 194;
						num3 = 301;
						continue;
					case 539:
						array[num14 + 1] = array2[1];
						num3 = 293;
						continue;
					case 516:
						num15 = 21 + 55;
						num3 = 666;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 21;
						}
						continue;
					case 344:
						num14 = 23;
						num3 = 90;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 220;
						}
						continue;
					case 530:
						num49++;
						num3 = 549;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 448;
						}
						continue;
					case 317:
						array10[3] = array11[1];
						num3 = 616;
						continue;
					case 66:
						num15 = 113 + 74;
						num3 = 91;
						continue;
					case 426:
						array4[8] = (byte)num21;
						num3 = 644;
						continue;
					case 308:
						iThthenarx(intPtr4, 4, 8, ref num23);
						num3 = 190;
						continue;
					case 12:
						array12[num24 + 3] = (byte)((num25 & 0xFF000000u) >> 24);
						num3 = 45;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 108;
						}
						continue;
					case 212:
						num15 = 59 - 45;
						num3 = 534;
						continue;
					case 552:
						iThthenarx(intPtr9, num64 * 4, num23, ref num23);
						num3 = 384;
						continue;
					case 77:
						array[num16 + 7] = array5[7];
						num3 = 440;
						continue;
					case 358:
						array3[3] = 158;
						num3 = 294;
						continue;
					case 217:
						ptr = null;
						num3 = 281;
						continue;
					case 461:
						num27++;
						num3 = 380;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 322;
						}
						continue;
					case 220:
						array4 = new byte[16];
						num3 = 559;
						continue;
					case 22:
						array3[27] = 195;
						num3 = 275;
						continue;
					case 387:
						num15 = 238 - 79;
						num3 = 408;
						continue;
					case 235:
						axoD13poAOy2RTNJPvL2 = default;
						num3 = 499;
						continue;
					case 487:
					case 615:
						if (num68 >= num41)
						{
							num3 = 176;
							continue;
						}
						goto case 96;
					case 293:
						array[num14 + 2] = array2[2];
						num3 = 136;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 0;
						}
						continue;
					case 517:
						array3[14] = (byte)num15;
						num3 = 113;
						continue;
					case 420:
						LVte5LEQ0FlqPncTU94(cryptoStream);
						num3 = 189;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 166;
						}
						continue;
					case 339:
						array4[13] = 162;
						num3 = 69;
						continue;
					case 349:
						array3[30] = (byte)num15;
						num3 = 274;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 38;
						}
						continue;
					case 330:
						Bthy37BD9oaJkCmuxm7(new IntPtr(value), intPtr);
						num3 = 410;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 294;
						}
						continue;
					case 188:
						array3[11] = (byte)num15;
						num3 = 315;
						continue;
					case 526:
						array12 = new byte[array14.Length];
						num3 = 120;
						continue;
					case 74:
						array[num16 + 7] = array2[7];
						num3 = 636;
						continue;
					case 153:
						array4[8] = (byte)num19;
						num3 = 457;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 595;
						}
						continue;
					case 629:
						value = 0L;
						num3 = 169;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 155;
						}
						continue;
					case 418:
						num15 = 174 - 58;
						num3 = 524;
						continue;
					case 85:
						array3[6] = 106;
						num3 = 451;
						continue;
					case 283:
						iThthenarx(intPtr9, num64 * 4, 8, ref num23);
						num3 = 591;
						continue;
					case 522:
						array7 = null;
						num3 = 541;
						continue;
					case 176:
					case 384:
						if (wB6HDQBtSipTQF4yScy(xBXNeQEE5ufUE0ju7wc(xuFOhpz0Mnl6lRpPtN)) < Xs5lnvE0a4IFWrvc1DL(xBXNeQEE5ufUE0ju7wc(xuFOhpz0Mnl6lRpPtN)) - 1)
						{
							num3 = 55;
							continue;
						}
						goto case 82;
					case 383:
						num19 = 116 - 72;
						num3 = 306;
						continue;
					case 45:
					case 600:
						num26++;
						num3 = 422;
						continue;
					case 352:
						array3[2] = 136;
						num3 = 630;
						continue;
					case 640:
						array3[12] = (byte)num15;
						num3 = 254;
						continue;
					case 281:
					case 365:
						num48 = 0;
						num3 = 80;
						continue;
					case 567:
						num15 = 67 + 111;
						num = 657;
						break;
					case 380:
						num69 = 0u;
						num = 455;
						break;
					case 444:
						num42 = 0u;
						num3 = 291;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 324;
						}
						continue;
					case 43:
						num42 = (uint)((array14[num69 + 3] << 24) | (array14[num69 + 2] << 16) | (array14[num69 + 1] << 8) | array14[num69]);
						num3 = 687;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 665;
						}
						continue;
					case 621:
						return;
					case 49:
						o7iMF1EksEUKJT21GtQ(new IntPtr(&num18), 0);
						num3 = 179;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 661;
						}
						continue;
					case 427:
						array3[28] = 148;
						num3 = 533;
						continue;
					case 118:
						array[num16 + 3] = array5[3];
						num3 = 658;
						continue;
					case 27:
						array3[9] = (byte)num15;
						num3 = 84;
						continue;
					case 89:
						num64 = fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN);
						num3 = 635;
						continue;
					case 16:
						array3 = new byte[32];
						num3 = 187;
						continue;
					case 589:
						array[num16 + 7] = array7[7];
						num3 = 532;
						continue;
					case 1:
						num15 = 209 - 69;
						num3 = 47;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 11;
						}
						continue;
					case 256:
						array4[2] = (byte)num19;
						num3 = 199;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 553;
						}
						continue;
					case 610:
						num19 = 193 - 64;
						num3 = 112;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 287;
						}
						continue;
					case 616:
						array10[5] = array11[2];
						num3 = 39;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 35;
						}
						continue;
					case 315:
						num15 = 139 - 21;
						num3 = 683;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 128;
						}
						continue;
					case 515:
						array3[8] = (byte)num15;
						num3 = 221;
						continue;
					case 294:
						array3[3] = 39;
						num = 284;
						break;
					case 403:
						array[num16] = array7[0];
						num3 = 83;
						continue;
					case 447:
						array13[8] = 108;
						num3 = 583;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 486;
						}
						continue;
					case 195:
						array4[11] = (byte)num21;
						num = 186;
						break;
					case 268:
						if (FLQvAIEngja38yT9km8(Vr01UMEyiZb7jIuKFHB(oiIp3r0OAK)) != 0)
						{
							num3 = 229;
							continue;
						}
						goto case 465;
					case 544:
					{
						byte[] array22 = array18;
						array17 = array22;
						if (array22 != null)
						{
							num3 = 556;
							continue;
						}
						goto case 217;
					}
					case 390:
						array3[14] = 133;
						num3 = 639;
						continue;
					case 550:
						num15 = 245 - 81;
						num3 = 68;
						continue;
					case 34:
						array3[17] = (byte)num15;
						num3 = 38;
						continue;
					case 454:
						num17++;
						num3 = 586;
						continue;
					case 280:
						array3[10] = 55;
						num3 = 339;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 145;
						}
						continue;
					case 172:
						array13[3] = 111;
						num3 = 502;
						continue;
					case 143:
						P3BUAkEKywrB1yX9eqY();
						num3 = 621;
						continue;
					case 367:
						array4[4] = (byte)num21;
						num3 = 482;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 111;
						}
						continue;
					case 11:
						array3[1] = (byte)num15;
						num = 564;
						break;
					case 453:
						array4[5] = (byte)num21;
						num3 = 348;
						continue;
					case 561:
						num18 = 0L;
						num = 511;
						break;
					case 587:
						array3[13] = 143;
						num3 = 585;
						continue;
					case 485:
						array4[14] = (byte)num21;
						num3 = 592;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 332;
						}
						continue;
					case 239:
						array3[12] = 27;
						num3 = 161;
						continue;
					case 151:
						array21 = (byte[])yFk2rHEAnajce7OLNyE(xuFOhpz0Mnl6lRpPtN, (int)Xs5lnvE0a4IFWrvc1DL(xBXNeQEE5ufUE0ju7wc(xuFOhpz0Mnl6lRpPtN)));
						num3 = 16;
						continue;
					case 269:
						array10[1] = array11[0];
						num3 = 317;
						continue;
					case 232:
						array12[num24] = (byte)(num25 & 0xFF);
						num3 = 671;
						continue;
					case 298:
						array3[18] = 152;
						num3 = 253;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 337;
						}
						continue;
					case 495:
						array3[22] = 138;
						num3 = 494;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 252;
						}
						continue;
					case 81:
						QXWSFcELdDrXXjJE0nA(array11, 0, array11.Length);
						num3 = 174;
						continue;
					case 61:
						num66 = 0;
						num3 = 575;
						continue;
					case 104:
						array3[13] = (byte)num15;
						num3 = 390;
						continue;
					case 674:
						array10 = array4;
						num3 = 260;
						continue;
					case 69:
						num19 = 136 - 81;
						num3 = 605;
						continue;
					case 368:
						array4[11] = (byte)num19;
						num3 = 571;
						continue;
					case 483:
						num19 = 216 - 72;
						num3 = 368;
						continue;
					case 653:
						num42 |= array14[array14.Length - (1 + num17)];
						num3 = 454;
						continue;
					case 667:
						num21 = 128 - 49;
						num3 = 453;
						continue;
					case 668:
						array13[0] = 99;
						num3 = 80;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 307;
						}
						continue;
					case 109:
						array4[0] = (byte)num21;
						num3 = 383;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 157;
						}
						continue;
					case 654:
						if (array11 != null)
						{
							num3 = 29;
							if (uuxvQpECJ1ciupObMkA())
							{
								num3 = 163;
							}
							continue;
						}
						goto case 174;
					case 440:
						num16 = 18;
						num = 403;
						break;
					case 32:
						array3[7] = 106;
						num3 = 346;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 171;
						}
						continue;
					case 60:
					case 80:
						if (num48 >= num51)
						{
							num3 = 194;
							continue;
						}
						goto case 240;
					case 267:
						num19 = 115 + 85;
						num3 = 519;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 207;
						}
						continue;
					case 244:
						num21 = 243 - 81;
						num3 = 643;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 386;
						}
						continue;
					case 599:
						intPtr7 = eSnslyEuFhKIdwficbS(((object[])QFmg2qEJ0fBF7yBvkU4(oiIp3r0OAK))[0]);
						num3 = 264;
						continue;
					case 394:
						P3BUAkEKywrB1yX9eqY();
						num3 = 373;
						continue;
					case 540:
						array3[25] = 104;
						num3 = 604;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 530;
						}
						continue;
					case 168:
						array12[num24 + num66] = (byte)((num63 & num67) >> num31);
						num3 = 360;
						continue;
					case 295:
						array4[6] = (byte)num19;
						num3 = 244;
						continue;
					case 371:
						array3[22] = 142;
						num3 = 385;
						continue;
					case 445:
						i7JgF2BRptJ1h7a5GGG(YL9p8TnZg2);
						num3 = 137;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 500;
						}
						continue;
					case 39:
						array10[7] = array11[3];
						num3 = 414;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 603;
						}
						continue;
					case 501:
						array3[16] = 154;
						num3 = 201;
						continue;
					case 656:
						array3[7] = (byte)num15;
						num3 = 41;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 132;
						}
						continue;
					case 664:
						num3 = 381;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 432;
						}
						continue;
					case 335:
						num15 = 46 - 45;
						num = 219;
						break;
					case 462:
						num21 = 135 - 45;
						num3 = 504;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 93;
						}
						continue;
					case 291:
						_ = IntPtr.Zero;
						num = 205;
						break;
					case 17:
						fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN);
						num3 = 44;
						continue;
					case 278:
						AjyvxvBpTxth0XtiqWZ(intPtr2, fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN));
						num3 = 147;
						continue;
					case 132:
						array4[6] = 117;
						num3 = 29;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 560;
						}
						continue;
					case 676:
						array13[7] = 116;
						num = 464;
						break;
					case 219:
						array3[15] = (byte)num15;
						num3 = 516;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 261;
						}
						continue;
					case 296:
						num15 = 124 + 23;
						num3 = 188;
						continue;
					case 639:
						array3[14] = 153;
						num3 = 202;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 193;
						}
						continue;
					case 632:
						iThthenarx(new IntPtr(value), SgdaD5E42usnNCLHEG7(), 64, ref num47);
						num3 = 330;
						continue;
					case 535:
						array2 = (byte[])vudCNGBceuiJAxRZ40b(intPtr8.ToInt32());
						num3 = 290;
						continue;
					case 265:
						array3[4] = (byte)num15;
						num3 = 651;
						continue;
					case 98:
					{
						int num78 = fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN);
						xNBpQ9cWcG = false;
						if (num78 >= 1879048192)
						{
							num3 = 353;
							if (!uuxvQpECJ1ciupObMkA())
							{
								num3 = 220;
							}
							continue;
						}
						goto case 115;
					}
					case 392:
						try
						{
							UC7pfXZD4N = (YKb9T7pwgf6BsMpQTo1)GEgn3GB3c57weI0muKm(new IntPtr(num28), gXojVCBb5v3rsfrU2bj(typeof(YKb9T7pwgf6BsMpQTo1).TypeHandle));
							int num70 = 3;
							if (uuxvQpECJ1ciupObMkA())
							{
								num70 = 0;
							}
							while (true)
							{
								switch (num70)
								{
								default:
									if (num71 == 988)
									{
										num70 = num71;
										continue;
									}
									break;
								case 0:
									break;
								}
								break;
							}
						}
						catch
						{
							int num72 = 1;
							if (I8wSSlETnYL0FFJbX98() != null)
							{
								num72 = 1;
							}
							while (true)
							{
								switch (num72)
								{
								default:
									if (num73 == 989)
									{
										goto IL_34b6;
									}
									break;
								case 1:
									break;
								case 0:
									goto end_IL_34ba;
								}
								try
								{
									Delegate obj6 = (Delegate)GEgn3GB3c57weI0muKm(new IntPtr(num28), gXojVCBb5v3rsfrU2bj(typeof(YKb9T7pwgf6BsMpQTo1).TypeHandle));
									int num74 = 3;
									if (I8wSSlETnYL0FFJbX98() == null)
									{
										num74 = 1;
									}
									while (true)
									{
										switch (num74)
										{
										default:
											if (num75 == 989)
											{
												num74 = num75;
												continue;
											}
											break;
										case 1:
											break;
										case 0:
											goto end_IL_351f;
										}
										UC7pfXZD4N = (YKb9T7pwgf6BsMpQTo1)RoTMJEBKODCSjE5CDEl(gXojVCBb5v3rsfrU2bj(typeof(YKb9T7pwgf6BsMpQTo1).TypeHandle), S7MyTKBemOIkUfsNeSA(obj6));
										num74 = 0;
										if (!uuxvQpECJ1ciupObMkA())
										{
											num74 = 0;
										}
										continue;
										end_IL_351f:
										break;
									}
								}
								catch
								{
									int num76 = 0;
									if (!uuxvQpECJ1ciupObMkA())
									{
										num76 = 3;
									}
									while (true)
									{
										switch (num76)
										{
										case 0:
											goto end_IL_357e;
										}
										if (num77 == 988)
										{
											num76 = num77;
											continue;
										}
										goto end_IL_357e;
									}
									end_IL_357e:;
								}
								break;
								IL_34b6:
								num72 = num73;
								continue;
								end_IL_34ba:
								break;
							}
						}
						goto case 291;
					case 208:
						Tkmp0tBXb9 = new Hashtable(fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN) + 1);
						num3 = 203;
						continue;
					case 175:
						intPtr2 = new IntPtr(num44 + fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN) - num50);
						num3 = 437;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 3;
						}
						continue;
					case 333:
						array3[6] = 109;
						num3 = 500;
						continue;
					case 253:
						array3[18] = 199;
						num3 = 475;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 656;
						}
						continue;
					case 547:
						array13[6] = 105;
						num3 = 676;
						continue;
					case 144:
						array4[9] = (byte)num19;
						num3 = 652;
						continue;
					case 476:
						array3[27] = 58;
						num3 = 648;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 149;
						}
						continue;
					case 385:
						array3[23] = 216;
						num3 = 2;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 356;
						}
						continue;
					case 542:
						num69 = (uint)(num52 * 4);
						num = 619;
						break;
					case 300:
						array3[31] = (byte)num15;
						num3 = 518;
						continue;
					case 633:
						array13 = new byte[12];
						num3 = 463;
						continue;
					case 304:
						num15 = 212 - 70;
						num3 = 396;
						continue;
					case 316:
						if (num26 == num27 - 1)
						{
							num3 = 643;
							continue;
						}
						goto case 18;
					case 578:
						num67 = 255u;
						num3 = 47;
						continue;
					case 164:
						array3[4] = (byte)num15;
						num3 = 340;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 587;
						}
						continue;
					case 82:
						w3LXsVBCqhY3dK4VivK(intPtr3);
						num3 = 394;
						continue;
					case 288:
						array3[10] = 122;
						num3 = 523;
						continue;
					case 194:
						array17 = null;
						num3 = 97;
						continue;
					case 570:
						array3[19] = (byte)num15;
						num3 = 418;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 293;
						}
						continue;
					case 604:
						array3[25] = 113;
						num3 = 8;
						continue;
					case 338:
						num21 = 252 - 84;
						num3 = 584;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 426;
						}
						continue;
					case 457:
						num21 = 92 + 123;
						num3 = 577;
						continue;
					case 94:
						num68++;
						num3 = 615;
						continue;
					case 399:
						array3[12] = (byte)num15;
						num3 = 239;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 584;
						}
						continue;
					case 251:
						array3[18] = 126;
						num3 = 298;
						continue;
					case 70:
						lbaLKKBqkRocLAuRSLQ(BlvUFfBd6d3r2OAivQH(S7MyTKBemOIkUfsNeSA(UC7pfXZD4N)));
						num3 = 12;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 445;
						}
						continue;
					case 252:
						array4[9] = 114;
						num3 = 637;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 202;
						}
						continue;
					case 434:
						TU7iRFEBtFw96YP0kQA(xBXNeQEE5ufUE0ju7wc(xuFOhpz0Mnl6lRpPtN), 0L);
						num3 = 73;
						continue;
					case 431:
						num15 = 100 + 116;
						num3 = 53;
						continue;
					case 131:
					case 586:
						if (num17 >= num29)
						{
							num3 = 165;
							continue;
						}
						goto case 407;
					case 20:
						if (SgdaD5E42usnNCLHEG7() != 4)
						{
							num3 = 472;
							continue;
						}
						goto case 159;
					case 237:
					case 609:
						J8Xf5OEbihPVVoNWZDc(intPtr6, 0);
						num3 = 297;
						continue;
					case 241:
						array3[17] = (byte)num15;
						num3 = 204;
						continue;
					case 502:
						array13[4] = 114;
						num3 = 618;
						continue;
					case 73:
						intPtr7 = eSnslyEuFhKIdwficbS(((object[])QFmg2qEJ0fBF7yBvkU4(oiIp3r0OAK))[0]);
						num3 = 507;
						continue;
					case 206:
						array3[14] = (byte)num15;
						num3 = 660;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 339;
						}
						continue;
					case 141:
						P3BUAkEKywrB1yX9eqY();
						num3 = 452;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 115;
						}
						continue;
					case 319:
						iThthenarx(intPtr2, 4, 8, ref num23);
						num3 = 278;
						continue;
					case 631:
						array3[28] = 44;
						num3 = 180;
						continue;
					case 101:
						num19 = 216 - 72;
						num3 = 295;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 371;
						}
						continue;
					case 150:
						i7JgF2BRptJ1h7a5GGG(UC7pfXZD4N);
						num3 = 70;
						continue;
					case 276:
						num14 = 9;
						num3 = 209;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 350;
						}
						continue;
					case 7:
						array4[13] = (byte)num19;
						num3 = 339;
						continue;
					case 99:
						array4[12] = 107;
						num3 = 156;
						continue;
					case 179:
						if (FLQvAIEngja38yT9km8(Vr01UMEyiZb7jIuKFHB(gXojVCBb5v3rsfrU2bj(typeof(fJUeITteiwtDJsNdQol).TypeHandle).Assembly)) > 0)
						{
							num3 = 369;
							continue;
						}
						goto case 72;
					case 563:
						if (!mWXjwhEPYqpk3LO3wgm(yslZ18EZlUTpIUE2NtW("System.Reflection.ReflectionContext", false), null))
						{
							num3 = 528;
							if (!uuxvQpECJ1ciupObMkA())
							{
								num3 = 550;
							}
							continue;
						}
						goto case 347;
					case 617:
						array4[3] = (byte)num19;
						num3 = 98;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 19;
						}
						continue;
					case 521:
						zero = IntPtr.Zero;
						num3 = 121;
						continue;
					case 354:
					{
						MemoryStream memoryStream2 = new MemoryStream();
						cryptoStream = new CryptoStream(memoryStream2, transform, CryptoStreamMode.Write);
						cyuc2xEw4N6ZlejKfHl(cryptoStream, array21, 0, array21.Length);
						i2J8G6ESsURYPkjSkYb(cryptoStream);
						array18 = (byte[])DEhhBlEopghlbcTFFax(memoryStream2);
						QXWSFcELdDrXXjJE0nA(array10, 0, array10.Length);
						LVte5LEQ0FlqPncTU94(memoryStream2);
						num = 420;
						break;
					}
					case 211:
						axoD13poAOy2RTNJPvL3.XNBpQ9cWcG = false;
						num3 = 612;
						continue;
					case 397:
						array3[3] = (byte)num15;
						num3 = 625;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 339;
						}
						continue;
					case 145:
						array3[11] = 123;
						num3 = 3;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 296;
						}
						continue;
					case 360:
						num66++;
						num3 = 407;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 623;
						}
						continue;
					case 199:
						num19 = 186 - 92;
						num3 = 466;
						continue;
					case 553:
						array3[20] = 210;
						num3 = 212;
						continue;
					case 226:
						num15 = 118 - 107;
						num3 = 140;
						continue;
					case 37:
						array6[num30] ^= array10[num30];
						num3 = 626;
						continue;
					case 496:
						num41 = fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN);
						num3 = 486;
						continue;
					case 4:
					case 498:
						if (num30 >= array10.Length)
						{
							num3 = 255;
							continue;
						}
						goto case 37;
					case 408:
						array3[10] = (byte)num15;
						num = 361;
						break;
					case 13:
						num21 = 113 + 84;
						num3 = 48;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 451;
						}
						continue;
					case 284:
						array3[3] = 92;
						num3 = 574;
						continue;
					case 190:
						if (SgdaD5E42usnNCLHEG7() == 4)
						{
							num3 = 530;
							if (I8wSSlETnYL0FFJbX98() == null)
							{
								num3 = 289;
							}
							continue;
						}
						goto case 456;
					case 301:
						num15 = 55 + 28;
						num3 = 144;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 569;
						}
						continue;
					case 524:
						array3[20] = (byte)num15;
						num3 = 238;
						continue;
					case 213:
						array[num14 + 2] = array5[2];
						num3 = 40;
						continue;
					case 382:
						num15 = 9 + 62;
						num3 = 122;
						continue;
					case 2:
						array3[23] = 146;
						num3 = 66;
						continue;
					case 452:
						return;
					case 477:
						if (SgdaD5E42usnNCLHEG7() == 4)
						{
							num3 = 563;
							if (!uuxvQpECJ1ciupObMkA())
							{
								num3 = 622;
							}
							continue;
						}
						goto case 182;
					case 51:
						array18 = array12;
						num = 576;
						break;
					case 84:
						array3[9] = 82;
						num3 = 318;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 387;
						}
						continue;
					case 680:
						num19 = 113 + 34;
						num3 = 439;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 536;
						}
						continue;
					case 312:
						array3[21] = 83;
						num = 675;
						break;
					case 357:
						array8 = null;
						num = 20;
						break;
					case 87:
						array10[13] = array11[6];
						num3 = 544;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 669;
						}
						continue;
					case 96:
						intPtr4 = new IntPtr(Cf5pDG2bGj + fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN) - num50);
						num3 = 337;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 65;
						}
						continue;
					case 64:
						num21 = 83 + 53;
						num3 = 367;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 638;
						}
						continue;
					case 273:
						array3[9] = (byte)num15;
						num3 = 87;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 288;
						}
						continue;
					case 48:
						array4[4] = (byte)num21;
						num3 = 64;
						continue;
					case 203:
						axoD13poAOy2RTNJPvL3 = default;
						num3 = 114;
						continue;
					case 255:
						array14 = array21;
						num3 = 142;
						continue;
					case 430:
						num21 = 115 + 124;
						num = 257;
						break;
					case 63:
						num15 = 91 + 102;
						num3 = 399;
						continue;
					case 625:
						num15 = 162 - 93;
						num3 = 666;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 26;
						}
						continue;
					case 329:
						array3[18] = (byte)num15;
						num3 = 192;
						continue;
					case 121:
						num68 = 0;
						num3 = 487;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 590;
						}
						continue;
					case 507:
						num44 = intPtr7.ToInt64();
						num3 = 482;
						continue;
					case 134:
						num15 = 99 + 64;
						num3 = 3;
						continue;
					case 149:
						array3[27] = 151;
						num3 = 435;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 263;
						}
						continue;
					case 146:
						array4[5] = (byte)num21;
						num3 = 512;
						continue;
					case 210:
						num19 = 163 - 54;
						num3 = 582;
						continue;
					case 364:
						array4[11] = (byte)num21;
						num3 = 611;
						continue;
					case 100:
						array9 = new byte[6];
						num3 = 108;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 553;
						}
						continue;
					case 78:
						num67 <<= 8;
						num3 = 202;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 208;
						}
						continue;
					case 113:
						num15 = 29 + 74;
						num3 = 206;
						continue;
					case 474:
						num15 = 107 + 111;
						num = 329;
						break;
					case 548:
						array3[0] = 117;
						num3 = 544;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 247;
						}
						continue;
					case 545:
						num5 += num65;
						num3 = 226;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 230;
						}
						continue;
					case 243:
						array3[21] = (byte)num15;
						num3 = 245;
						continue;
					case 575:
					case 623:
						if (num66 >= num29)
						{
							num3 = 600;
							continue;
						}
						goto case 405;
					case 152:
						array4[4] = 157;
						num3 = 13;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 165;
						}
						continue;
					case 443:
						num16 = 2;
						num3 = 672;
						continue;
					case 105:
						array3[13] = (byte)num15;
						num3 = 538;
						continue;
					case 35:
						array3[8] = 84;
						num3 = 650;
						continue;
					case 328:
						array3[13] = (byte)num15;
						num3 = 129;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 642;
						}
						continue;
					case 571:
						num21 = 211 - 70;
						num3 = 195;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 664;
						}
						continue;
					case 62:
						num15 = 81 + 9;
						num3 = 640;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 37;
						}
						continue;
					case 234:
						num19 = 191 - 63;
						num3 = 617;
						continue;
					case 643:
						if (num29 > 0)
						{
							num3 = 225;
							continue;
						}
						goto case 18;
					case 307:
						array13[1] = 108;
						num3 = 374;
						continue;
					case 155:
						num19 = 34 + 104;
						num3 = 127;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 3;
						}
						continue;
					case 122:
						array3[19] = (byte)num15;
						num3 = 345;
						continue;
					case 585:
						num15 = 22 + 26;
						num3 = 272;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 328;
						}
						continue;
					case 197:
						num48++;
						num3 = 60;
						continue;
					case 612:
						WqI3B3BTLoJD1bwHtWc(Tkmp0tBXb9, 0L, axoD13poAOy2RTNJPvL3);
						num3 = 413;
						continue;
					case 189:
						Y06cJYElWs7Ae6i6oyV(xuFOhpz0Mnl6lRpPtN);
						num3 = 496;
						continue;
					case 181:
						num21 = 109 + 91;
						num3 = 109;
						continue;
					case 318:
						num15 = 111 - 97;
						num3 = 273;
						continue;
					case 473:
						value = iOEiVqBj3LWVACj1RAI(intPtr6);
						num3 = 609;
						continue;
					case 224:
					case 508:
						if (num22 >= num64)
						{
							num = 552;
							break;
						}
						goto case 170;
					case 634:
						array10[11] = array11[5];
						num3 = 87;
						continue;
					case 598:
						array4[12] = (byte)num19;
						num3 = 310;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 233;
						}
						continue;
					case 464:
						array13[8] = 46;
						num3 = 93;
						continue;
					case 120:
						num53 = array6.Length / 4;
						num3 = 359;
						continue;
					case 225:
						num63 = num5 ^ num42;
						num3 = 236;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 61;
						}
						continue;
					case 396:
						array3[5] = (byte)num15;
						num3 = 356;
						continue;
					case 438:
						array3[15] = (byte)num15;
						num3 = 404;
						continue;
					case 466:
						array4[2] = (byte)num19;
						num3 = 218;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 95;
						}
						continue;
					case 167:
						array3[4] = 115;
						num3 = 262;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 510;
						}
						continue;
					case 500:
						num15 = 15 + 68;
						num3 = 419;
						continue;
					case 311:
						array4[13] = 152;
						num3 = 266;
						continue;
					case 15:
						if (!KuB6QHBXIEOPyPl4IG4(shAwjRBYlE7VDUqNr8d(gXojVCBb5v3rsfrU2bj(typeof(fJUeITteiwtDJsNdQol).TypeHandle).Assembly), null))
						{
							num3 = 72;
							continue;
						}
						goto case 481;
					case 289:
						aHdtFwAZdE(intPtr3, intPtr4, (byte[])vudCNGBceuiJAxRZ40b(fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN)), 4u, out zero);
						num = 67;
						break;
					case 314:
						fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN);
						num3 = 76;
						continue;
					case 119:
						num20 = fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN);
						num3 = 11;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 393;
						}
						continue;
					case 347:
						enumerator = (IEnumerator)Jk84DoEUURplhj96WnG(BQ3m4pEx8YZHx0Eexob(QdOaUHEN5v6v9LTg48u()));
						num3 = 110;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 549;
						}
						continue;
					case 529:
						intPtr7 = eSnslyEuFhKIdwficbS(((object[])QFmg2qEJ0fBF7yBvkU4(gXojVCBb5v3rsfrU2bj(typeof(fJUeITteiwtDJsNdQol).TypeHandle).Assembly))[0]);
						num3 = 156;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 423;
						}
						continue;
					case 362:
						array3[30] = (byte)num15;
						num3 = 309;
						continue;
					case 419:
						array3[6] = (byte)num15;
						num3 = 85;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 462;
						}
						continue;
					case 556:
						if (array17.Length != 0)
						{
							num3 = 520;
							continue;
						}
						goto case 217;
					case 564:
						array3[1] = 130;
						num3 = 352;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 365;
						}
						continue;
					case 465:
						num50 = 7680;
						num3 = 57;
						continue;
					case 489:
						array3[26] = (byte)num15;
						num = 484;
						break;
					case 414:
						array4[11] = (byte)num21;
						num3 = 483;
						continue;
					case 111:
						array4[4] = 135;
						num3 = 102;
						continue;
					case 379:
						array3[22] = (byte)num15;
						num3 = 495;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 372;
						}
						continue;
					case 183:
						array3[30] = 112;
						num3 = 261;
						continue;
					case 442:
						array11 = (byte[])CSrM2UEi8ZtqmrNfZuD(HHoICjErwYiZxvIWgPA(oiIp3r0OAK));
						num3 = 654;
						continue;
					case 432:
						try
						{
							enumerator = (IEnumerator)Jk84DoEUURplhj96WnG(BQ3m4pEx8YZHx0Eexob(process));
							int num55 = 0;
							if (I8wSSlETnYL0FFJbX98() != null)
							{
								num55 = 7;
							}
							while (true)
							{
								switch (num55)
								{
								default:
									if (num56 == 989)
									{
										goto IL_4836;
									}
									break;
								case 0:
									break;
								case 1:
									goto end_IL_483a;
								}
								try
								{
									while (true)
									{
										IL_4900:
										int num57;
										if (!RbgEoLE5KuKXiRdxkVL(enumerator))
										{
											num57 = 0;
											if (I8wSSlETnYL0FFJbX98() == null)
											{
												num57 = 2;
											}
											goto IL_488c;
										}
										goto IL_48c6;
										IL_48c6:
										intPtr7 = CUvyUTEYPF2YBKIDdnt((ProcessModule)Cxls5EE2TSaVpL1WUwJ(enumerator));
										num57 = 4;
										goto IL_488c;
										IL_488c:
										while (true)
										{
											switch (num57)
											{
											default:
												switch (num58)
												{
												case 992:
													break;
												default:
													goto end_IL_4900;
												case 12:
													num50 = 0;
													num57 = 4;
													if (I8wSSlETnYL0FFJbX98() == null)
													{
														num57 = 3;
													}
													continue;
												}
												goto IL_4888;
											case 0:
												break;
											case 1:
												goto IL_4900;
											case 3:
												goto end_IL_4900;
											case 4:
												if (intPtr7.ToInt64() != CU7p1vJVVx)
												{
													goto IL_4900;
												}
												num58 = 12;
												goto IL_4888;
											case 2:
												goto end_IL_4900;
												IL_4888:
												num57 = num58;
												continue;
											}
											break;
										}
										goto IL_48c6;
										continue;
										end_IL_4900:
										break;
									}
								}
								finally
								{
									IDisposable disposable = enumerator as IDisposable;
									int num59 = 7;
									if (uuxvQpECJ1ciupObMkA())
									{
										num59 = 0;
									}
									while (true)
									{
										switch (num59)
										{
										default:
											if (num60 == 990)
											{
												num59 = num60;
												continue;
											}
											break;
										case 0:
											if (disposable != null)
											{
												num59 = 2;
												continue;
											}
											goto end_IL_499a;
										case 2:
											break;
										case 1:
											goto end_IL_499a;
										}
										rmnsm4E7lkuVAYH0HlX(disposable);
										num59 = 1;
										if (I8wSSlETnYL0FFJbX98() == null)
										{
											num59 = 1;
										}
										continue;
										end_IL_499a:
										break;
									}
								}
								break;
								IL_4836:
								num55 = num56;
								continue;
								end_IL_483a:
								break;
							}
						}
						catch
						{
							int num61 = 0;
							if (I8wSSlETnYL0FFJbX98() != null)
							{
								num61 = 0;
							}
							while (true)
							{
								switch (num61)
								{
								default:
									if (num62 == 988)
									{
										num61 = num62;
										continue;
									}
									break;
								case 0:
									break;
								}
								break;
							}
						}
						goto case 409;
					case 355:
						num15 = 85 + 69;
						num3 = 517;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 71;
						}
						continue;
					case 126:
						array3[25] = (byte)num15;
						num3 = 554;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 238;
						}
						continue;
					case 649:
						num54 = 4059231220u;
						num = 561;
						break;
					case 342:
						array4[1] = 124;
						num3 = 267;
						continue;
					case 520:
					case 531:
						ptr = (byte*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref array17[0]);
						num3 = 365;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 190;
						}
						continue;
					case 480:
						num21 = 227 - 75;
						num3 = 485;
						continue;
					case 391:
						num15 = 99 + 92;
						num3 = 656;
						continue;
					case 574:
						num15 = 179 - 59;
						num3 = 397;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 680;
						}
						continue;
					case 68:
						array3[19] = (byte)num15;
						num3 = 382;
						continue;
					case 238:
						array3[20] = 153;
						num3 = 553;
						continue;
					case 395:
						array3[26] = 130;
						num3 = 106;
						continue;
					case 381:
						array3[21] = 94;
						num3 = 366;
						continue;
					case 386:
						array4[6] = (byte)num21;
						num3 = 132;
						continue;
					case 271:
						array3[3] = (byte)num15;
						num3 = 358;
						continue;
					case 245:
						num15 = 245 - 81;
						num3 = 379;
						continue;
					case 341:
						if (SgdaD5E42usnNCLHEG7() == 4)
						{
							num3 = 75;
							continue;
						}
						goto case 135;
					case 52:
						array5 = (byte[])H4i8tHB20J6ggeIGL35(TIypExNM5B.ToInt64());
						num3 = 648;
						continue;
					case 117:
						array3[24] = 126;
						num3 = 50;
						continue;
					case 460:
						num45 = fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN) - num50;
						num3 = 98;
						continue;
					case 579:
						return;
					case 72:
						num3 = 585;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 148;
						}
						continue;
					case 337:
						num15 = 133 - 96;
						num3 = 107;
						continue;
					case 660:
						num15 = 176 - 53;
						num3 = 320;
						continue;
					case 429:
						if (bPhYNdBWHkGVxF4Pwyk(intPtr5, IntPtr.Zero))
						{
							num3 = 588;
							continue;
						}
						goto case 100;
					case 446:
						array13[1] = 115;
						num3 = 505;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 370;
						}
						continue;
					case 156:
						num19 = 136 - 45;
						num3 = 598;
						continue;
					case 325:
						array13[3] = 106;
						num3 = 343;
						continue;
					case 135:
						num28 = iOEiVqBj3LWVACj1RAI(new IntPtr(value));
						num3 = 503;
						continue;
					case 411:
						num27 = array14.Length / 4;
						num3 = 526;
						continue;
					case 24:
						text = (string)TFvqHhBajDhL2Odnoid(roj8qlBksDm0T3ssU6j(), array13);
						num3 = 581;
						continue;
					case 363:
					case 422:
						if (num26 >= num27)
						{
							num3 = 51;
							continue;
						}
						goto case 562;
					case 191:
						if (Vr01UMEyiZb7jIuKFHB(gXojVCBb5v3rsfrU2bj(typeof(fJUeITteiwtDJsNdQol).TypeHandle).Assembly) != null)
						{
							num3 = 179;
							continue;
						}
						goto case 72;
					case 412:
						num19 = 129 - 70;
						num3 = 667;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 138;
						}
						continue;
					case 463:
						array13[0] = 109;
						num3 = 446;
						continue;
					case 112:
						array4[15] = (byte)num19;
						num = 210;
						break;
					case 472:
					{
						byte[] array19 = new byte[40];
						hQ3U1TBg3I8im4HbnBK(array19, __ldtoken(_003CPrivateImplementationDetails_003E_007B16B6D898_002D2657_002D4B50_002DB8EB_002DDDC3B4FFE416_007D._0E448EF5E5E60630BDDB19388CB6378436E3C65D03DD66DA7C6EBFF563BD857A));
						array8 = array19;
						num3 = 678;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 467;
						}
						continue;
					}
					case 584:
						array13[10] = 108;
						num3 = 304;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 491;
						}
						continue;
					case 102:
						array4[4] = 201;
						num3 = 597;
						continue;
					case 576:
						num51 = array18.Length / 8;
						num3 = 544;
						continue;
					case 482:
						num23 = 0;
						num3 = 580;
						continue;
					case 450:
						array[num16 + 6] = array2[6];
						num3 = 74;
						continue;
					case 642:
						array3[16] = (byte)num15;
						num3 = 253;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 431;
						}
						continue;
					case 514:
						num15 = 33 + 25;
						num = 642;
						break;
					case 282:
						unrpmq9rQL = intPtr7.ToInt32();
						num3 = 599;
						continue;
					case 486:
						num20 = fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN);
						num3 = 459;
						continue;
					case 128:
						array3[11] = (byte)num15;
						num3 = 18;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 62;
						}
						continue;
					case 71:
						array3[24] = 86;
						num3 = 471;
						continue;
					case 359:
						num5 = 0u;
						num3 = 279;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 118;
						}
						continue;
					case 19:
						num21 = 126 + 4;
						num3 = 519;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 546;
						}
						continue;
					case 401:
						array3[22] = (byte)num15;
						num3 = 371;
						continue;
					case 36:
						num21 = 95 + 11;
						num3 = 16;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 679;
						}
						continue;
					case 374:
						array13[2] = 114;
						num3 = 325;
						continue;
					case 366:
						num15 = 94 + 78;
						num3 = 243;
						continue;
					case 543:
						QXWSFcELdDrXXjJE0nA(array6, 0, array6.Length);
						num3 = 354;
						continue;
					case 263:
						num15 = 231 - 77;
						num = 125;
						break;
					case 373:
						return;
					case 423:
						CU7p1vJVVx = intPtr7.ToInt64();
						num3 = 277;
						continue;
					case 630:
						array3[2] = 144;
						num3 = 292;
						continue;
					case 645:
						array3[5] = 135;
						num3 = 628;
						continue;
					case 378:
						intPtr5 = fa3typcRVg(text);
						num3 = 368;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 100;
						}
						continue;
					case 41:
						array3[7] = 122;
						num3 = 135;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 32;
						}
						continue;
					case 402:
						intPtr9 = new IntPtr(Cf5pDG2bGj + num43 - num50);
						num = 89;
						break;
					case 377:
						array = array8;
						num3 = 302;
						continue;
					case 611:
						array4[12] = 73;
						num3 = 99;
						continue;
					case 302:
						array2 = null;
						num3 = 522;
						continue;
					case 346:
						num15 = 32 + 92;
						num3 = 525;
						continue;
					case 40:
						array[num14 + 3] = array5[3];
						num3 = 607;
						continue;
					case 607:
						num14 = 16;
						num3 = 79;
						continue;
					case 215:
						if (!p74pqHljgy)
						{
							num3 = 214;
							continue;
						}
						goto case 143;
					case 459:
						if (num20 != 1)
						{
							num49 = 0;
							num3 = 171;
						}
						else
						{
							num3 = 468;
						}
						continue;
					case 207:
						array4[1] = (byte)num19;
						num = 160;
						break;
					case 389:
						num19 = 79 + 112;
						num3 = 557;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 649;
						}
						continue;
					case 290:
						array7 = (byte[])vudCNGBceuiJAxRZ40b(evmPENBHbcVC6H3C7bi(num28));
						num3 = 124;
						continue;
					case 23:
						array3[24] = (byte)num15;
						num3 = 101;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 117;
						}
						continue;
					case 125:
						array3[27] = (byte)num15;
						num = 246;
						break;
					case 666:
						array3[3] = (byte)num15;
						num3 = 133;
						continue;
					case 648:
						array2 = (byte[])H4i8tHB20J6ggeIGL35(intPtr8.ToInt64());
						num3 = 370;
						continue;
					case 223:
						array3[31] = (byte)num15;
						num3 = 662;
						continue;
					case 497:
						array4[1] = 214;
						num3 = 342;
						continue;
					case 417:
						array4[7] = 152;
						num3 = 338;
						continue;
					case 506:
						num15 = 225 - 75;
						num3 = 593;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 248;
						}
						continue;
					case 86:
						num21 = 81 + 98;
						num = 116;
						break;
					case 415:
						array[num16 + 6] = array7[6];
						num3 = 589;
						continue;
					case 28:
					case 503:
						process = (Process)QdOaUHEN5v6v9LTg48u();
						num3 = 627;
						continue;
					case 594:
						array9[5] = 116;
						num3 = 497;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 551;
						}
						continue;
					case 595:
						wT0plHug2U = (byte[])yFk2rHEAnajce7OLNyE(xuFOhpz0Mnl6lRpPtN, num46);
						num3 = 235;
						continue;
					case 91:
						array3[23] = (byte)num15;
						num3 = 470;
						continue;
					case 233:
						array3[2] = (byte)num15;
						num3 = 43;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 6;
						}
						continue;
					case 669:
						array10[15] = array11[7];
						num3 = 579;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 81;
						}
						continue;
					case 622:
						array3[0] = 100;
						num3 = 548;
						continue;
					case 662:
						num15 = 242 - 80;
						num3 = 446;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 537;
						}
						continue;
					case 527:
						array3[31] = (byte)num15;
						num3 = 74;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 259;
						}
						continue;
					case 404:
						array3[15] = 109;
						num3 = 6;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 335;
						}
						continue;
					case 79:
						array[num14] = array7[0];
						num3 = 49;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 493;
						}
						continue;
					case 286:
						array3[29] = 107;
						num3 = 492;
						continue;
					case 494:
						num15 = 61 + 101;
						num3 = 670;
						continue;
					case 257:
						array4[14] = (byte)num21;
						num3 = 480;
						continue;
					case 92:
						array3[8] = 176;
						num3 = 684;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 287;
						}
						continue;
					case 248:
						CFSPCvEj1LCrOyqM8jh();
						num3 = 388;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 69;
						}
						continue;
					case 451:
						array3[6] = 72;
						num3 = 391;
						continue;
					case 566:
						array3[10] = (byte)num15;
						num3 = 280;
						continue;
					case 331:
						jMbTtXEI5rcr70arXTH(new byte[1], 0, IQUwwrEOVJVK14VG5dS(8), 1);
						num3 = 248;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 60;
						}
						continue;
					case 677:
						num15 = 228 - 76;
						num3 = 401;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 33;
						}
						continue;
					case 55:
						num43 = fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN);
						num3 = 402;
						continue;
					case 277:
						if (SgdaD5E42usnNCLHEG7() == 4)
						{
							num3 = 97;
							if (uuxvQpECJ1ciupObMkA())
							{
								num3 = 601;
							}
							continue;
						}
						goto case 633;
					case 18:
						num25 = num5 ^ num42;
						num3 = 232;
						continue;
					case 405:
						if (num66 > 0)
						{
							num3 = 347;
							if (I8wSSlETnYL0FFJbX98() == null)
							{
								num3 = 78;
							}
							continue;
						}
						goto case 168;
					case 441:
						array3[26] = 244;
						num3 = 476;
						continue;
					case 166:
						num69 = (uint)num24;
						num3 = 313;
						continue;
					case 274:
						num15 = 84 + 105;
						num3 = 130;
						continue;
					case 602:
						num42 <<= 8;
						num3 = 274;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 653;
						}
						continue;
					case 97:
						xuFOhpz0Mnl6lRpPtN = new IXuFOhpz0Mnl6lRpPtN(new MemoryStream(array18));
						num3 = 434;
						continue;
					case 264:
						Cf5pDG2bGj = intPtr7.ToInt64();
						num = 521;
						break;
					case 29:
						num19 = 46 + 4;
						num3 = 568;
						continue;
					case 259:
						num15 = 184 - 61;
						num = 300;
						break;
					case 292:
						num15 = 69 + 41;
						num3 = 98;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 233;
						}
						continue;
					case 180:
						array3[29] = 99;
						num = 270;
						break;
					case 76:
						num41 = fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN);
						num3 = 119;
						continue;
					case 148:
						try
						{
							object obj = DcXHnfBUNoFX710Jclr(nUepNVBxqXo8CrVFI77(wWBZBiBP4bOiNe0drAL(hkAeVnBZcZGxBvxP4Go(gXojVCBb5v3rsfrU2bj(typeof(fJUeITteiwtDJsNdQol).TypeHandle).Assembly))).GetField("m_ptr", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), wWBZBiBP4bOiNe0drAL(hkAeVnBZcZGxBvxP4Go(gXojVCBb5v3rsfrU2bj(typeof(fJUeITteiwtDJsNdQol).TypeHandle).Assembly)));
							int num32 = 16;
							if (I8wSSlETnYL0FFJbX98() != null)
							{
								num32 = 5;
							}
							while (true)
							{
								int num33;
								switch (num32)
								{
								default:
									if (num34 != 24)
									{
										if (num34 == 1004)
										{
											goto IL_5837;
										}
										goto case 11;
									}
									goto IL_5a42;
								case 4:
									try
									{
										byte[] array16 = array15;
										fixed (byte[] array17 = array16)
										{
											int num35;
											if (array16 != null)
											{
												num35 = 2;
												if (I8wSSlETnYL0FFJbX98() == null)
												{
													num35 = 2;
												}
												goto IL_58dc;
											}
											goto IL_5939;
											IL_58dc:
											while (true)
											{
												switch (num35)
												{
												default:
													if (num36 != 13)
													{
														if (num36 == 993)
														{
															goto IL_58d8;
														}
														goto case 0;
													}
													goto end_IL_58dc;
												case 5:
													break;
												case 0:
												case 3:
													value2 = (byte*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref array17[0]);
													num35 = 4;
													if (I8wSSlETnYL0FFJbX98() != null)
													{
														num35 = 5;
													}
													continue;
												case 2:
													if (array17.Length != 0)
													{
														num35 = 3;
														continue;
													}
													break;
												case 1:
												case 4:
													{
														YL9p8TnZg2(new IntPtr(value2), new IntPtr(value2), new IntPtr(value2), 216669565u, new IntPtr(value2), ref nativeSizeOfCode);
														num36 = 13;
														goto IL_58d8;
													}
													IL_58d8:
													num35 = num36;
													continue;
												}
												goto IL_5939;
												continue;
												end_IL_58dc:
												break;
											}
											goto end_IL_58ab;
											IL_5939:
											value2 = null;
											num35 = 1;
											if (I8wSSlETnYL0FFJbX98() != null)
											{
												num35 = 0;
											}
											goto IL_58dc;
											end_IL_58ab:;
										}
									}
									finally
									{
										array17 = null;
										int num37 = 0;
										if (I8wSSlETnYL0FFJbX98() != null)
										{
											num37 = 4;
										}
										while (true)
										{
											switch (num37)
											{
											default:
												if (num38 == 988)
												{
													num37 = num38;
													continue;
												}
												break;
											case 0:
												break;
											}
											break;
										}
									}
									break;
								case 11:
									LVte5LEQ0FlqPncTU94(memoryStream);
									num32 = 6;
									continue;
								case 3:
									cyuc2xEw4N6ZlejKfHl(memoryStream, new byte[SgdaD5E42usnNCLHEG7()], 0, SgdaD5E42usnNCLHEG7());
									num32 = 5;
									continue;
								case 16:
									if (obj is IntPtr)
									{
										num32 = 13;
										if (uuxvQpECJ1ciupObMkA())
										{
											num32 = 15;
										}
										continue;
									}
									goto case 0;
								case 8:
									cyuc2xEw4N6ZlejKfHl(memoryStream, vudCNGBceuiJAxRZ40b(TIypExNM5B.ToInt32()), 0, 4);
									num32 = 3;
									if (I8wSSlETnYL0FFJbX98() == null)
									{
										num32 = 13;
									}
									continue;
								case 15:
									TIypExNM5B = (IntPtr)obj;
									num32 = 0;
									if (!uuxvQpECJ1ciupObMkA())
									{
										num32 = 4;
									}
									continue;
								case 7:
								case 13:
									cyuc2xEw4N6ZlejKfHl(memoryStream, new byte[SgdaD5E42usnNCLHEG7()], 0, SgdaD5E42usnNCLHEG7());
									num32 = 3;
									continue;
								case 6:
									nativeSizeOfCode = 0u;
									num32 = 4;
									if (I8wSSlETnYL0FFJbX98() != null)
									{
										num32 = 22;
									}
									continue;
								case 12:
									cyuc2xEw4N6ZlejKfHl(memoryStream, H4i8tHB20J6ggeIGL35(TIypExNM5B.ToInt64()), 0, 8);
									num32 = 15;
									if (uuxvQpECJ1ciupObMkA())
									{
										num32 = 7;
									}
									continue;
								case 2:
									if (SgdaD5E42usnNCLHEG7() == 4)
									{
										num32 = 8;
										if (I8wSSlETnYL0FFJbX98() != null)
										{
											num32 = 10;
										}
										continue;
									}
									goto case 12;
								case 1:
									cyuc2xEw4N6ZlejKfHl(memoryStream, new byte[SgdaD5E42usnNCLHEG7()], 0, SgdaD5E42usnNCLHEG7());
									num32 = 2;
									continue;
								case 10:
									TIypExNM5B = (IntPtr)DcXHnfBUNoFX710Jclr(obj.GetType().GetField("m_pData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), obj);
									num33 = 24;
									goto IL_5833;
								case 9:
									array15 = (byte[])DEhhBlEopghlbcTFFax(memoryStream);
									num32 = 15;
									if (I8wSSlETnYL0FFJbX98() == null)
									{
										num32 = 11;
									}
									continue;
								case 5:
									TU7iRFEBtFw96YP0kQA(memoryStream, 0L);
									num33 = 9;
									goto IL_5833;
								case 0:
									if (CWKCv5Eq4UPkyyCiFtI(obj.GetType().ToString(), "System.Reflection.RuntimeModule"))
									{
										num32 = 10;
										continue;
									}
									goto IL_5a42;
								case 14:
									break;
									IL_5a42:
									memoryStream = new MemoryStream();
									num32 = 0;
									if (I8wSSlETnYL0FFJbX98() == null)
									{
										num32 = 1;
									}
									continue;
									IL_5833:
									num34 = num33;
									goto IL_5837;
									IL_5837:
									num32 = num34;
									continue;
								}
								break;
							}
						}
						catch
						{
							int num39 = 0;
							if (I8wSSlETnYL0FFJbX98() != null)
							{
								num39 = 6;
							}
							while (true)
							{
								switch (num39)
								{
								default:
									if (num40 == 988)
									{
										num39 = num40;
										continue;
									}
									break;
								case 0:
									break;
								}
								break;
							}
						}
						goto case 150;
					case 499:
						axoD13poAOy2RTNJPvL2.WT0plHug2U = wT0plHug2U;
						num3 = 613;
						continue;
					case 198:
						value = CN8yIfBIFxjCZ2HuoAV(intPtr6);
						num3 = 237;
						continue;
					case 58:
						num21 = 41 + 110;
						num3 = 414;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 478;
						}
						continue;
					case 628:
						array3[5] = 183;
						num3 = 249;
						continue;
					case 469:
						array[num16 + 1] = array5[1];
						num = 687;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num = 66;
						}
						break;
					case 192:
						array3[18] = 92;
						num = 251;
						break;
					case 33:
						array3[22] = 100;
						num3 = 665;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 677;
						}
						continue;
					case 170:
						AjyvxvBpTxth0XtiqWZ(new IntPtr(intPtr9.ToInt64() + num22 * 4), fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN));
						num3 = 659;
						continue;
					case 512:
						array4[5] = 157;
						num3 = 667;
						continue;
					case 413:
						xNBpQ9cWcG = false;
						num3 = 334;
						continue;
					case 165:
					case 665:
						num5 = num5;
						num3 = 88;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 106;
						}
						continue;
					case 436:
						if (Vr01UMEyiZb7jIuKFHB(oiIp3r0OAK) != null)
						{
							num3 = 268;
							continue;
						}
						goto case 465;
					case 650:
						num15 = 203 - 67;
						num3 = 483;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 515;
						}
						continue;
					case 321:
						array4[6] = (byte)num19;
						num3 = 101;
						continue;
					case 591:
						num22 = 0;
						num3 = 508;
						continue;
					case 47:
						num31 = 0;
						num3 = 228;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 365;
						}
						continue;
					case 478:
						num15 = 251 - 83;
						num3 = 527;
						continue;
					case 254:
						num15 = 58 + 107;
						num3 = 332;
						continue;
					case 638:
						array3[0] = (byte)num15;
						num3 = 622;
						continue;
					case 481:
						if (((Array)dhtkp8B4lyfT40eW2kv(shAwjRBYlE7VDUqNr8d(gXojVCBb5v3rsfrU2bj(typeof(fJUeITteiwtDJsNdQol).TypeHandle).Assembly))).Length == 2)
						{
							num3 = 191;
							continue;
						}
						goto case 72;
					case 626:
						num30++;
						num3 = 520;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 498;
						}
						continue;
					case 147:
						iThthenarx(intPtr2, 4, num23, ref num23);
						num3 = 24;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 530;
						}
						continue;
					case 186:
						num21 = 75 - 15;
						num3 = 364;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 84;
						}
						continue;
					case 424:
						array3[21] = (byte)num15;
						num3 = 231;
						continue;
					case 343:
						array13[4] = 105;
						num3 = 200;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 46;
						}
						continue;
					case 162:
						array4[0] = 127;
						num3 = 458;
						continue;
					case 297:
						YL9p8TnZg2 = new YKb9T7pwgf6BsMpQTo1(m8wtvyr1sn);
						num3 = 590;
						continue;
					case 142:
						num29 = array14.Length % 4;
						num3 = 411;
						continue;
					case 309:
						num15 = 71 + 68;
						num3 = 684;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 349;
						}
						continue;
					case 596:
						if (SgdaD5E42usnNCLHEG7() == 4)
						{
							num3 = 336;
							continue;
						}
						goto case 599;
					case 370:
						array7 = (byte[])H4i8tHB20J6ggeIGL35(num28);
						num3 = 376;
						continue;
					case 372:
						array[num16 + 2] = array7[2];
						num3 = 435;
						continue;
					case 299:
						array[num16 + 4] = array7[4];
						num = 425;
						break;
					case 562:
						num52 = num26 % num53;
						num3 = 31;
						continue;
					case 227:
						if (SgdaD5E42usnNCLHEG7() == 4)
						{
							num = 428;
							break;
						}
						goto case 52;
					case 425:
						array[num16 + 5] = array7[5];
						num3 = 415;
						if (I8wSSlETnYL0FFJbX98() != null)
						{
							num3 = 355;
						}
						continue;
					case 581:
						intPtr5 = IntPtr.Zero;
						num3 = 429;
						continue;
					case 323:
					case 334:
						if (wB6HDQBtSipTQF4yScy(xBXNeQEE5ufUE0ju7wc(xuFOhpz0Mnl6lRpPtN)) < Xs5lnvE0a4IFWrvc1DL(xBXNeQEE5ufUE0ju7wc(xuFOhpz0Mnl6lRpPtN)) - 1)
						{
							num3 = 460;
							continue;
						}
						goto case 529;
					case 130:
						array3[30] = (byte)num15;
						num3 = 678;
						continue;
					case 468:
						intPtr3 = IntPtr.Zero;
						num3 = 513;
						continue;
					case 260:
						cnhjIOEvwISv4Vp1d0Y(array10);
						num3 = 442;
						continue;
					case 559:
						array4[0] = 135;
						num3 = 162;
						continue;
					case 261:
						num15 = 147 - 49;
						num3 = 183;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 362;
						}
						continue;
					case 57:
					case 229:
						fUYVa2EMX7a0xj66IST(xuFOhpz0Mnl6lRpPtN);
						num3 = 213;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 17;
						}
						continue;
					case 107:
						array3[4] = (byte)num15;
						num3 = 245;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 250;
						}
						continue;
					case 228:
						if (num26 == num27 - 1)
						{
							num3 = 236;
							continue;
						}
						goto case 166;
					case 95:
						array4[3] = 121;
						num3 = 361;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 234;
						}
						continue;
					case 204:
						num15 = 204 - 107;
						num3 = 286;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 663;
						}
						continue;
					case 555:
						array3[13] = 195;
						num3 = 646;
						continue;
					case 160:
						array4[2] = 160;
						num3 = 54;
						continue;
					case 305:
						array[num14 + 3] = array7[3];
						num3 = 344;
						continue;
					case 624:
						array[num16 + 1] = array2[1];
						num3 = 185;
						continue;
					case 50:
						array3[24] = 126;
						num3 = 262;
						continue;
					case 174:
						num30 = 0;
						num3 = 4;
						continue;
					case 670:
						array3[22] = (byte)num15;
						num3 = 33;
						continue;
					case 275:
						array3[27] = 97;
						num3 = 427;
						continue;
					case 603:
						array10[9] = array11[4];
						num3 = 634;
						continue;
					case 679:
						array4[10] = (byte)num21;
						num3 = 330;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 389;
						}
						continue;
					case 21:
						array3[16] = (byte)num15;
						num3 = 514;
						continue;
					case 572:
						num15 = 253 - 84;
						num = 560;
						break;
					case 406:
						array9[2] = 116;
						num3 = 222;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 135;
						}
						continue;
					case 250:
						array3[5] = 81;
						num3 = 304;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 175;
						}
						continue;
					case 647:
						array[num16 + 3] = array2[3];
						num3 = 285;
						continue;
					case 196:
						array3[9] = 91;
						num3 = 400;
						continue;
					case 533:
						array3[28] = 116;
						num3 = 631;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 428;
						}
						continue;
					case 123:
					case 467:
						intPtr = CFabhwBmLdfIO4FrcBu(IntPtr.Zero, (uint)array8.Length, 4096u, 64u);
						num = 377;
						break;
					case 646:
						num15 = 250 - 83;
						num3 = 105;
						continue;
					case 659:
						num22++;
						num3 = 224;
						continue;
					case 672:
						array[num16] = array5[0];
						num3 = 469;
						continue;
					case 0:
						array[num14 + 3] = array2[3];
						num3 = 655;
						continue;
					case 433:
						array[num14 + 2] = array7[2];
						num3 = 305;
						continue;
					case 193:
						array3[14] = 144;
						num3 = 355;
						continue;
					case 129:
						array3[13] = 144;
						num3 = 95;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 555;
						}
						continue;
					case 537:
						array3[31] = (byte)num15;
						num3 = 478;
						continue;
					case 14:
						array[num16 + 5] = array5[5];
						num3 = 620;
						if (!uuxvQpECJ1ciupObMkA())
						{
							num3 = 267;
						}
						continue;
					case 519:
						array4[3] = (byte)num21;
						num3 = 152;
						continue;
					case 504:
						array4[13] = (byte)num21;
						num3 = 398;
						continue;
					case 375:
						array3[19] = 214;
						num3 = 550;
						continue;
					case 56:
						array6 = array3;
						num3 = 220;
						continue;
					case 398:
						array4[13] = 126;
						num3 = 98;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 311;
						}
						continue;
					case 510:
						num15 = 149 - 49;
						num3 = 164;
						continue;
					case 546:
						array3[12] = 205;
						num3 = 461;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 587;
						}
						continue;
					case 538:
						num15 = 57 - 56;
						num3 = 104;
						continue;
					case 439:
						array4[0] = (byte)num19;
						num = 181;
						break;
					case 613:
						axoD13poAOy2RTNJPvL2.XNBpQ9cWcG = xNBpQ9cWcG;
						num3 = 38;
						if (I8wSSlETnYL0FFJbX98() == null)
						{
							num3 = 350;
						}
						continue;
					case 393:
						if (num20 == 4)
						{
							num3 = 536;
							continue;
						}
						goto case 459;
					case 484:
						array3[26] = 143;
						num3 = 84;
						if (uuxvQpECJ1ciupObMkA())
						{
							num3 = 441;
						}
						continue;
					case 88:
					{
						uint num4 = num5;
						uint num6 = num5;
						uint num7 = 1400895850u;
						uint num8 = 994329632u;
						uint num9 = 1117048405u;
						uint num10 = 1331402446u;
						uint num11 = num6;
						ulong num12 = num8 * num10;
						if (num12 == 0)
						{
							num12--;
						}
						num7 = (uint)(num7 * num7 % num12);
						num9 ^= num8;
						num12 = num7 * 1026830853;
						if (num12 == 0)
						{
							num12--;
						}
						num8 = (uint)(num8 * num8 % num12);
						if (num10 == 0)
						{
							num10--;
						}
						uint num13 = num7 / num10 + num10;
						num10 = ((num7 + num7) ^ num13) + num7;
						if (num11 == 0)
						{
							num11--;
						}
						num13 = num7 / num11 + num11;
						num11 = num7 - num7 + num13 + num7;
						num11 ^= num11 << 7;
						num11 += num8;
						num11 ^= num11 >> 1;
						num11 += num10;
						num11 ^= num11 << 25;
						num11 += num11;
						num11 = (((num10 << 3) + num10) ^ num10) + num11;
						num5 = num4 + (uint)(double)num11;
						num3 = 316;
						continue;
					}
					}
					goto end_IL_0009;
					continue;
					end_IL_000d:
					break;
				}
				continue;
				end_IL_0009:
				break;
			}
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object bgmtuUb9KY(object P_0)
	{
		try
		{
			if (File.Exists(((Assembly)P_0).Location))
			{
				return ((Assembly)P_0).Location;
			}
		}
		catch
		{
		}
		try
		{
			if (File.Exists(((Assembly)P_0).GetName().CodeBase.ToString().Replace("file:///", "")))
			{
				return ((Assembly)P_0).GetName().CodeBase.ToString().Replace("file:///", "");
			}
		}
		catch
		{
		}
		try
		{
			if (File.Exists(P_0.GetType().GetProperty("Location").GetValue(P_0, new object[0])
				.ToString()))
			{
				return P_0.GetType().GetProperty("Location").GetValue(P_0, new object[0])
					.ToString();
			}
		}
		catch
		{
		}
		return "";
	}

	[DllImport("kernel32", EntryPoint = "LoadLibrary")]
	public static extern IntPtr fa3typcRVg(string P_0);

	[DllImport("kernel32", CharSet = CharSet.Ansi, EntryPoint = "GetProcAddress")]
	public static extern IntPtr MXxtnniQXO(IntPtr P_0, string P_1);

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static IntPtr FsXtMKs6Or(IntPtr P_0, object P_1, uint P_2)
	{
		if (qtxpAF20Zu == null)
		{
			qtxpAF20Zu = (nX4UPSCTItWAPwhmW2b)Marshal.GetDelegateForFunctionPointer(MXxtnniQXO(gEHrfEJaJ(), "Find ".Trim() + "ResourceA"), Type.GetTypeFromHandle(VbhttmCNbvOE14oqZBl.HaoEcZQw87(33554609)));
		}
		return qtxpAF20Zu(P_0, (string)P_1, P_2);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static IntPtr OUdt6a24Bb(IntPtr P_0, uint P_1, uint P_2, uint P_3)
	{
		if (BdYpvWJtWg == null)
		{
			BdYpvWJtWg = (xaIapFCbGrA3DrjpRbr)Marshal.GetDelegateForFunctionPointer(MXxtnniQXO(gEHrfEJaJ(), "Virtual ".Trim() + "Alloc"), Type.GetTypeFromHandle(VbhttmCNbvOE14oqZBl.HaoEcZQw87(33554610)));
		}
		return BdYpvWJtWg(P_0, P_1, P_2, P_3);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static int aHdtFwAZdE(IntPtr P_0, IntPtr P_1, [In][Out] byte[] P_2, uint P_3, out IntPtr P_4)
	{
		if (EIKprHdjCb == null)
		{
			EIKprHdjCb = (qGZj3RCH0YrjnlvXGED)Marshal.GetDelegateForFunctionPointer(MXxtnniQXO(gEHrfEJaJ(), "Write ".Trim() + "Process ".Trim() + "Memory"), Type.GetTypeFromHandle(VbhttmCNbvOE14oqZBl.HaoEcZQw87(33554611)));
		}
		return EIKprHdjCb(P_0, P_1, P_2, P_3, out P_4);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static int iThthenarx(IntPtr P_0, int P_1, int P_2, ref int P_3)
	{
		if (U2vpiMeujs == null)
		{
			U2vpiMeujs = (d2LpgPCkYe5mk6VZFmn)Marshal.GetDelegateForFunctionPointer(MXxtnniQXO(gEHrfEJaJ(), "Virtual ".Trim() + "Protect"), Type.GetTypeFromHandle(VbhttmCNbvOE14oqZBl.HaoEcZQw87(33554612)));
		}
		return U2vpiMeujs(P_0, P_1, P_2, ref P_3);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static IntPtr o50twqCliK(uint P_0, int P_1, uint P_2)
	{
		if (Yf5pLbHD2i == null)
		{
			Yf5pLbHD2i = (Xb3pi1CatfLXCA5kJDE)Marshal.GetDelegateForFunctionPointer(MXxtnniQXO(gEHrfEJaJ(), "Open ".Trim() + "Process"), Type.GetTypeFromHandle(VbhttmCNbvOE14oqZBl.HaoEcZQw87(33554613)));
		}
		return Yf5pLbHD2i(P_0, P_1, P_2);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static int ihbtSPZgCl(IntPtr P_0)
	{
		if (PxNpJwaTmj == null)
		{
			PxNpJwaTmj = (E2yxPTCWZkIhebuulMI)Marshal.GetDelegateForFunctionPointer(MXxtnniQXO(gEHrfEJaJ(), "Close ".Trim() + "Handle"), Type.GetTypeFromHandle(VbhttmCNbvOE14oqZBl.HaoEcZQw87(33554614)));
		}
		return PxNpJwaTmj(P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[SpecialName]
	private static IntPtr gEHrfEJaJ()
	{
		if (myrpukTxJl == IntPtr.Zero)
		{
			myrpukTxJl = fa3typcRVg("kernel ".Trim() + "32.dll");
		}
		return myrpukTxJl;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static byte[] cjjtoqUEGO(object P_0)
	{
		using FileStream fileStream = new FileStream((string)P_0, FileMode.Open, FileAccess.Read, FileShare.Read);
		int num = 0;
		int num2 = (int)fileStream.Length;
		byte[] array = new byte[num2];
		while (num2 > 0)
		{
			int num3 = fileStream.Read(array, num, num2);
			num += num3;
			num2 -= num3;
		}
		return array;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static Stream AKstQuHdwc()
	{
		return new MemoryStream();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static byte[] de7tlWMh2d(object P_0)
	{
		return ((MemoryStream)P_0).ToArray();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static byte[] dZPtzEcJld(object P_0)
	{
		Stream stream = AKstQuHdwc();
		SymmetricAlgorithm symmetricAlgorithm = DF8tqaPCEj();
		symmetricAlgorithm.Key = new byte[32]
		{
			40, 99, 123, 29, 163, 13, 128, 42, 126, 76,
			203, 50, 200, 117, 154, 15, 28, 200, 0, 74,
			134, 70, 75, 87, 186, 127, 133, 84, 127, 70,
			21, 249
		};
		symmetricAlgorithm.IV = new byte[16]
		{
			172, 111, 19, 39, 108, 209, 46, 155, 184, 179,
			22, 37, 74, 184, 198, 77
		};
		CryptoStream cryptoStream = new CryptoStream(stream, symmetricAlgorithm.CreateDecryptor(), CryptoStreamMode.Write);
		cryptoStream.Write((byte[])P_0, 0, ((Array)P_0).Length);
		cryptoStream.Close();
		byte[] result = de7tlWMh2d(stream);
		udQmOjCORxRL7YsRZe0.qcbEGyxWLX();
		return result;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private byte[] lO5pGUN4C4()
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private byte[] SE3pce9SbT()
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private byte[] XbjptfkC6l()
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private byte[] qVqppYgtZT()
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private byte[] rvtpC6xrA3()
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private byte[] NDgpT49IOs()
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal byte[] weGpb6dkCN()
	{
		_ = "swkWD2pAP3T1EWCLOW1".Length;
		_ = 0;
		return new byte[2] { 1, 2 };
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal byte[] W2MpHPmVBb()
	{
		_ = "6IwIjbwGKcynqxuTJj".Length;
		_ = 0;
		return new byte[2] { 1, 2 };
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal byte[] kpSpk4s727()
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal byte[] VTHpa6uTZj()
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object pOYHc1IDe7Ls9BoSwOx(object P_0)
	{
		return ((IXuFOhpz0Mnl6lRpPtN)P_0).KCFlcDdR6L();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void GF2aS5IfuEysLFIvUe4(object P_0, long P_1)
	{
		((Stream)P_0).Position = P_1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static long Yn1mD1I8Ei5d773BxNn(object P_0)
	{
		return ((Stream)P_0).Length;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object SxexMkI10BQJAbbSmfk(object P_0, int P_1)
	{
		return ((IXuFOhpz0Mnl6lRpPtN)P_0).QFOCGhrbFH(P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void iymByNIVIxFKbhXLrND(object P_0)
	{
		((IXuFOhpz0Mnl6lRpPtN)P_0).dbaCpIGGUj();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void mybyBaI5wooi2MjbEO9(object P_0)
	{
		Array.Reverse((Array)P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object jZbdagI7yPpQUwVCLgp(object P_0)
	{
		return ((Assembly)P_0).GetName();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object V0qjGlI9jrQZGc6X682(object P_0)
	{
		return ((AssemblyName)P_0).GetPublicKeyToken();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object TWBWXOIEbCcNAnPTEQc()
	{
		return DF8tqaPCEj();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void drkHj7IB5Dxkr5Ap78E(object P_0, CipherMode P_1)
	{
		((SymmetricAlgorithm)P_0).Mode = P_1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object WE1u28I0JudZfro7P1V(object P_0, object P_1, object P_2)
	{
		return ((SymmetricAlgorithm)P_0).CreateDecryptor((byte[])P_1, (byte[])P_2);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object svsbKBIAjWLS69eINqs()
	{
		return AKstQuHdwc();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void VjH2Y0IvKc1H7w8r7Cs(object P_0, object P_1, int P_2, int P_3)
	{
		((Stream)P_0).Write((byte[])P_1, P_2, P_3);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void xWguIZIrNgVnf9YrCFU(object P_0)
	{
		((CryptoStream)P_0).FlushFinalBlock();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object BjLkiqIiJ0WimO5R3cy(object P_0)
	{
		return de7tlWMh2d(P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void Bf4D8TIL9xM6G7AMKhf(object P_0)
	{
		((Stream)P_0).Close();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object IjAK95IJGKqxU4AkKtB(object P_0)
	{
		return ((Assembly)P_0).EntryPoint;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool QyF8kiIuS0hvmMcr7ru(object P_0, object P_1)
	{
		return (MethodInfo)P_0 == (MethodInfo)P_1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool Q0u26tIgxPYYNpe2Ntq()
	{
		return null == null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object qxXXh8ImpvWrWFHoo70()
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void H83Sr2IM0yUJIWxviXF()
	{
		udQmOjCORxRL7YsRZe0.qcbEGyxWLX();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void I9qLJEI6kSO1eOng4D5(bool P_0)
	{
		RSACryptoServiceProvider.UseMachineKeyStore = P_0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static Type lblkXtIFShH2NeUrfi1(RuntimeTypeHandle P_0)
	{
		return Type.GetTypeFromHandle(P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object gEFQUXIhyyvNqQUukaL(object P_0)
	{
		return ((Assembly)P_0).Location;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int ypnLVgIwRXTZvbFMqrq(object P_0)
	{
		return ((string)P_0).Length;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object UyrRlIISQ30CEAFxaXD()
	{
		return SHA1.Create();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object s8eFCnIojoIfb5Q2ST3(object P_0)
	{
		return CryptoConfig.MapNameToOID((string)P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool wOBcrDIQOqirEt7HI8P(object P_0)
	{
		return File.Exists((string)P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object k0KwEIIlNq5ryU7uu0K(object P_0, object P_1)
	{
		return ((Assembly)P_0).GetManifestResourceStream((string)P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object VY1FHjIzB7diWxuuuVQ(object P_0)
	{
		return ((IXuFOhpz0Mnl6lRpPtN)P_0).KCFlcDdR6L();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void OEDmyPjGovkbQIYxSs9(object P_0, long P_1)
	{
		((Stream)P_0).Position = P_1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static long Knu05VjcxiTHh7rOHcm(object P_0)
	{
		return ((Stream)P_0).Length;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object HqYLT1jtwjjc54Udgt5(object P_0, int P_1)
	{
		return ((IXuFOhpz0Mnl6lRpPtN)P_0).QFOCGhrbFH(P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object JCQ0JBjpP5oYpfjx14b()
	{
		return DF8tqaPCEj();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void WLiQ4JjC9asWUO4arMv(object P_0, CipherMode P_1)
	{
		((SymmetricAlgorithm)P_0).Mode = P_1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object dNRhD6jTRkJXksBr95Z(object P_0, object P_1, object P_2)
	{
		return ((SymmetricAlgorithm)P_0).CreateDecryptor((byte[])P_1, (byte[])P_2);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object VXEMYnjbpokBNwBVmn4()
	{
		return AKstQuHdwc();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void F41fVkjHwpblNYiXEGd(object P_0, object P_1, int P_2, int P_3)
	{
		((Stream)P_0).Write((byte[])P_1, P_2, P_3);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void c1Abb8jkVNKnT2Vtkvy(object P_0)
	{
		((CryptoStream)P_0).FlushFinalBlock();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object ovWTXVjaAxgGgcYEJOD()
	{
		return Encoding.UTF8;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object NpFPS1jWqE4c0KuOgDm(object P_0)
	{
		return de7tlWMh2d(P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object dU8f7Pj34LvG0nJOMsi(object P_0, object P_1)
	{
		return ((Encoding)P_0).GetString((byte[])P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void GUAsTjjOwjTXFHxAOOf(object P_0, object P_1)
	{
		((AsymmetricAlgorithm)P_0).FromXmlString((string)P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void XvM4bYjI0YKP8tKk6Sb(object P_0)
	{
		((Stream)P_0).Close();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void EcEIUfjjVxNFKODXnC2(object P_0)
	{
		((IXuFOhpz0Mnl6lRpPtN)P_0).dbaCpIGGUj();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void JJlg5RjNwa18jHsXna8(object P_0, object P_1, uint P_2, object P_3)
	{
		dFRtDelwPd(P_0, P_1, P_2, P_3);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static ushort cCsE6cjsyo2ycQhFUda(object P_0)
	{
		return ((BinaryReader)P_0).ReadUInt16();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int KnurOkjYTHWY8MEVvU0(object P_0, object P_1, int P_2, int P_3)
	{
		return ((Stream)P_0).Read((byte[])P_1, P_2, P_3);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void mk4E6PjXPo3HaiPW3l2(object P_0, object P_1, int P_2, int P_3)
	{
		bDttfaBsp6(P_0, P_1, P_2, P_3);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static long hpk6F2jeTDgihORCcmy(object P_0)
	{
		return ((Stream)P_0).Position;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static uint JI061ZjKUJOr9McdMfU(object P_0)
	{
		return ((BinaryReader)P_0).ReadUInt32();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static uint DyGAI9j4upKchIwu01p(uint P_0, int P_1, long P_2, object P_3)
	{
		return Hu7t8wEhtf(P_0, P_1, P_2, P_3);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static long bmLrBAjZWk8prH56AGG(long P_0, long P_1)
	{
		return Math.Min(P_0, P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object nTYKDBjPZ7RPgDEPuhs(object P_0, object P_1, int P_2, int P_3)
	{
		return ((HashAlgorithm)P_0).TransformFinalBlock((byte[])P_1, P_2, P_3);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object tWgcNIjx31RLswkGukf(object P_0, int P_1)
	{
		return ((BinaryReader)P_0).ReadBytes(P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void JY9lELjU1pP6fSRhcDm(object P_0)
	{
		Array.Reverse((Array)P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object y8shEKj2GEw37b5Wyia(object P_0)
	{
		return ((HashAlgorithm)P_0).Hash;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool RIFYmKjRnrXkjg8Peok(object P_0, object P_1, object P_2, object P_3)
	{
		return ((RSACryptoServiceProvider)P_0).VerifyHash((byte[])P_1, (string)P_2, (byte[])P_3);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void RURkYxjd88OospqRVyp(object P_0)
	{
		((BinaryReader)P_0).Close();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object tvqN9tjqxqE1ASfwsSx(object P_0)
	{
		return ((Assembly)P_0).GetName();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object NShuppjgvwn4spvSJUQ(object P_0)
	{
		return ((AssemblyName)P_0).Name;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object KBjIPBjmDTTelyfXgsk(object P_0, object P_1)
	{
		return (string)P_0 + (string)P_1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool PJBdBCIyCfKrfofamI9()
	{
		return null == null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object PD7vDvInyHFEXTKjKoE()
	{
		return null;
	}

	static int ubRgmdEpProKwIRFUvo()
	{
		return 1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static IntPtr J8Xf5OEbihPVVoNWZDc(IntPtr P_0, int P_1)
	{
		return Marshal.ReadIntPtr(P_0, P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int FCiKtIEH87VdVS5yRIs(IntPtr P_0, int P_1)
	{
		return Marshal.ReadInt32(P_0, P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static long o7iMF1EksEUKJT21GtQ(IntPtr P_0, int P_1)
	{
		return Marshal.ReadInt64(P_0, P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void dhnayNEaKfZVeU2YOuq(IntPtr P_0, int P_1, IntPtr P_2)
	{
		Marshal.WriteIntPtr(P_0, P_1, P_2);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void OxrsHJEWKbstiffRAGw(IntPtr P_0, int P_1, int P_2)
	{
		Marshal.WriteInt32(P_0, P_1, P_2);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void kghyJ3E31k2nZG7WavN(IntPtr P_0, int P_1, long P_2)
	{
		Marshal.WriteInt64(P_0, P_1, P_2);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static IntPtr IQUwwrEOVJVK14VG5dS(int P_0)
	{
		return Marshal.AllocCoTaskMem(P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void jMbTtXEI5rcr70arXTH(object P_0, int P_1, IntPtr P_2, int P_3)
	{
		Marshal.Copy((byte[])P_0, P_1, P_2, P_3);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void CFSPCvEj1LCrOyqM8jh()
	{
		KpjtiFf2dc();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object QdOaUHEN5v6v9LTg48u()
	{
		return Process.GetCurrentProcess();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object pwr2RZEsbqGGte5A622(object P_0)
	{
		return ((Process)P_0).MainModule;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static IntPtr CUvyUTEYPF2YBKIDdnt(object P_0)
	{
		return ((ProcessModule)P_0).BaseAddress;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static IntPtr gKNwcsEXWkHidi0ONST(IntPtr P_0, object P_1, uint P_2)
	{
		return FsXtMKs6Or(P_0, P_1, P_2);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool upIsmQEeuONn9gvIqmw(IntPtr P_0, IntPtr P_1)
	{
		return P_0 != P_1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void P3BUAkEKywrB1yX9eqY()
	{
		udQmOjCORxRL7YsRZe0.qcbEGyxWLX();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int SgdaD5E42usnNCLHEG7()
	{
		return IntPtr.Size;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static Type yslZ18EZlUTpIUE2NtW(object P_0, bool P_1)
	{
		return Type.GetType((string)P_0, P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool mWXjwhEPYqpk3LO3wgm(Type P_0, Type P_1)
	{
		return P_0 != P_1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object BQ3m4pEx8YZHx0Eexob(object P_0)
	{
		return ((Process)P_0).Modules;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object Jk84DoEUURplhj96WnG(object P_0)
	{
		return ((ReadOnlyCollectionBase)P_0).GetEnumerator();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object Cxls5EE2TSaVpL1WUwJ(object P_0)
	{
		return ((IEnumerator)P_0).Current;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object Aj46vUER2Xh1OGvrQPQ(object P_0)
	{
		return ((ProcessModule)P_0).ModuleName;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object NMbsAvEdy3ENFVSckWS(object P_0)
	{
		return ((string)P_0).ToLower();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool CWKCv5Eq4UPkyyCiFtI(object P_0, object P_1)
	{
		return (string)P_0 == (string)P_1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object G59GRIEgmGievpDFQON(object P_0)
	{
		return ((ProcessModule)P_0).FileVersionInfo;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int It7SadEmuESeuGKbChj(object P_0)
	{
		return ((FileVersionInfo)P_0).ProductMajorPart;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int zbADxOEDxXpmJfBEIgu(object P_0)
	{
		return ((FileVersionInfo)P_0).ProductMinorPart;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int LPuUHZEf2Xo89O4SP3I(object P_0)
	{
		return ((FileVersionInfo)P_0).ProductBuildPart;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int hTZZwCE8fEHsfGelStj(object P_0)
	{
		return ((FileVersionInfo)P_0).ProductPrivatePart;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool UQumgiE113VEq0iGoO7(object P_0, object P_1)
	{
		return (Version)P_0 >= (Version)P_1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool MgfZRJEV5FjntbxPaC8(object P_0, object P_1)
	{
		return (Version)P_0 < (Version)P_1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool RbgEoLE5KuKXiRdxkVL(object P_0)
	{
		return ((IEnumerator)P_0).MoveNext();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void rmnsm4E7lkuVAYH0HlX(object P_0)
	{
		((IDisposable)P_0).Dispose();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object M2TtAqE9T1q0ciPjFnH(object P_0, object P_1)
	{
		return ((Assembly)P_0).GetManifestResourceStream((string)P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object xBXNeQEE5ufUE0ju7wc(object P_0)
	{
		return ((IXuFOhpz0Mnl6lRpPtN)P_0).KCFlcDdR6L();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void TU7iRFEBtFw96YP0kQA(object P_0, long P_1)
	{
		((Stream)P_0).Position = P_1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static long Xs5lnvE0a4IFWrvc1DL(object P_0)
	{
		return ((Stream)P_0).Length;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object yFk2rHEAnajce7OLNyE(object P_0, int P_1)
	{
		return ((IXuFOhpz0Mnl6lRpPtN)P_0).QFOCGhrbFH(P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void cnhjIOEvwISv4Vp1d0Y(object P_0)
	{
		Array.Reverse((Array)P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object HHoICjErwYiZxvIWgPA(object P_0)
	{
		return ((Assembly)P_0).GetName();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object CSrM2UEi8ZtqmrNfZuD(object P_0)
	{
		return ((AssemblyName)P_0).GetPublicKeyToken();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void QXWSFcELdDrXXjJE0nA(object P_0, int P_1, int P_2)
	{
		Array.Clear((Array)P_0, P_1, P_2);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object QFmg2qEJ0fBF7yBvkU4(object P_0)
	{
		return ((Assembly)P_0).GetModules();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static IntPtr eSnslyEuFhKIdwficbS(object P_0)
	{
		return Marshal.GetHINSTANCE((Module)P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object Vr01UMEyiZb7jIuKFHB(object P_0)
	{
		return ((Assembly)P_0).Location;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int FLQvAIEngja38yT9km8(object P_0)
	{
		return ((string)P_0).Length;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int fUYVa2EMX7a0xj66IST(object P_0)
	{
		return ((IXuFOhpz0Mnl6lRpPtN)P_0).YWICtXkOiC();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object fSUbccE6gsPRdagQOXW()
	{
		return DF8tqaPCEj();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void vIxlb6EFZOHTaTmCWFM(object P_0, CipherMode P_1)
	{
		((SymmetricAlgorithm)P_0).Mode = P_1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object q9lKaTEhO6mgyr9hh5d(object P_0, object P_1, object P_2)
	{
		return ((SymmetricAlgorithm)P_0).CreateDecryptor((byte[])P_1, (byte[])P_2);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void cyuc2xEw4N6ZlejKfHl(object P_0, object P_1, int P_2, int P_3)
	{
		((Stream)P_0).Write((byte[])P_1, P_2, P_3);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void i2J8G6ESsURYPkjSkYb(object P_0)
	{
		((CryptoStream)P_0).FlushFinalBlock();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object DEhhBlEopghlbcTFFax(object P_0)
	{
		return ((MemoryStream)P_0).ToArray();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void LVte5LEQ0FlqPncTU94(object P_0)
	{
		((Stream)P_0).Close();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void Y06cJYElWs7Ae6i6oyV(object P_0)
	{
		((IXuFOhpz0Mnl6lRpPtN)P_0).dbaCpIGGUj();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int IRq7ugEz0abWSx77c4Y(object P_0)
	{
		return ((Process)P_0).Id;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static IntPtr YrBNfkBGVgHMF5SPdV0(uint P_0, int P_1, uint P_2)
	{
		return o50twqCliK(P_0, P_1, P_2);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object vudCNGBceuiJAxRZ40b(int P_0)
	{
		return BitConverter.GetBytes(P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static long wB6HDQBtSipTQF4yScy(object P_0)
	{
		return ((Stream)P_0).Position;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void AjyvxvBpTxth0XtiqWZ(IntPtr P_0, int P_1)
	{
		Marshal.WriteInt32(P_0, P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int w3LXsVBCqhY3dK4VivK(IntPtr P_0)
	{
		return ihbtSPZgCl(P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void WqI3B3BTLoJD1bwHtWc(object P_0, object P_1, object P_2)
	{
		((Hashtable)P_0).Add(P_1, P_2);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static Type gXojVCBb5v3rsfrU2bj(RuntimeTypeHandle P_0)
	{
		return Type.GetTypeFromHandle(P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int evmPENBHbcVC6H3C7bi(long P_0)
	{
		return Convert.ToInt32(P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object roj8qlBksDm0T3ssU6j()
	{
		return Encoding.UTF8;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object TFvqHhBajDhL2Odnoid(object P_0, object P_1)
	{
		return ((Encoding)P_0).GetString((byte[])P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool bPhYNdBWHkGVxF4Pwyk(IntPtr P_0, IntPtr P_1)
	{
		return P_0 == P_1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object GEgn3GB3c57weI0muKm(IntPtr P_0, Type P_1)
	{
		return LGdtLrRvkh(P_0, P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static IntPtr ijeWUlBOw2HhRGFtNBA(object P_0)
	{
		return P_0();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int CN8yIfBIFxjCZ2HuoAV(IntPtr P_0)
	{
		return Marshal.ReadInt32(P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static long iOEiVqBj3LWVACj1RAI(IntPtr P_0)
	{
		return Marshal.ReadInt64(P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static IntPtr Y198cyBN94CljEWnwQb(object P_0)
	{
		return Marshal.GetFunctionPointerForDelegate((Delegate)P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int BGXTXHBsFJeJyu1Pl99(object P_0)
	{
		return ((ProcessModule)P_0).ModuleMemorySize;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object shAwjRBYlE7VDUqNr8d(object P_0)
	{
		return ((Assembly)P_0).EntryPoint;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool KuB6QHBXIEOPyPl4IG4(object P_0, object P_1)
	{
		return (MethodInfo)P_0 != (MethodInfo)P_1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object S7MyTKBemOIkUfsNeSA(object P_0)
	{
		return ((Delegate)P_0).Method;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object RoTMJEBKODCSjE5CDEl(Type P_0, object P_1)
	{
		return Delegate.CreateDelegate(P_0, (MethodInfo)P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object dhtkp8B4lyfT40eW2kv(object P_0)
	{
		return ((MethodBase)P_0).GetParameters();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object hkAeVnBZcZGxBvxP4Go(object P_0)
	{
		return ((Assembly)P_0).ManifestModule;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static ModuleHandle wWBZBiBP4bOiNe0drAL(object P_0)
	{
		return ((Module)P_0).ModuleHandle;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static Type nUepNVBxqXo8CrVFI77(object P_0)
	{
		return P_0.GetType();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object DcXHnfBUNoFX710Jclr(object P_0, object P_1)
	{
		return ((FieldInfo)P_0).GetValue(P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object H4i8tHB20J6ggeIGL35(long P_0)
	{
		return BitConverter.GetBytes(P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void i7JgF2BRptJ1h7a5GGG(object P_0)
	{
		RuntimeHelpers.PrepareDelegate((Delegate)P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static RuntimeMethodHandle BlvUFfBd6d3r2OAivQH(object P_0)
	{
		return ((MethodBase)P_0).MethodHandle;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void lbaLKKBqkRocLAuRSLQ(RuntimeMethodHandle P_0)
	{
		RuntimeHelpers.PrepareMethod(P_0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void hQ3U1TBg3I8im4HbnBK(object P_0, RuntimeFieldHandle P_1)
	{
		RuntimeHelpers.InitializeArray((Array)P_0, P_1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static IntPtr CFabhwBmLdfIO4FrcBu(IntPtr P_0, uint P_1, uint P_2, uint P_3)
	{
		return OUdt6a24Bb(P_0, P_1, P_2, P_3);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void Bthy37BD9oaJkCmuxm7(IntPtr P_0, IntPtr P_1)
	{
		Marshal.WriteIntPtr(P_0, P_1);
	}

	internal static bool uuxvQpECJ1ciupObMkA()
	{
		return null == null;
	}

	internal static object I8wSSlETnYL0FFJbX98()
	{
		return null;
	}
}
