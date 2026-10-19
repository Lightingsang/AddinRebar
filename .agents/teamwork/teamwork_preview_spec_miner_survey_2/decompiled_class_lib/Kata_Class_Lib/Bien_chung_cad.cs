using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CBsM2MKTBsK0aC8V6VA;
using EiU4UbbIbWynyPARofm;
using MLjYl4KoROKr3ywt4st;
using Microsoft.VisualBasic.CompilerServices;

namespace Kata_Class_Lib;

[StandardModule]
public sealed class Bien_chung_cad
{
	public static diem XVtor;

	public static diem YVtor;

	public static diem ZVtor;

	public static bool Revit_run;

	public static object acDoc;

	public static object acDocEd;

	public static object acCurDb;

	public static object acadapp;

	public static object AcadMo;

	public static object AcadUt;

	public static object docObj;

	public static object ExcelApp;

	public static object Excelsheet;

	public const int SW_RESTORE = 9;

	public const int SW_SHOW = 5;

	internal static Bien_chung_cad gT486JDgfTXObyB8MHX;

	[MethodImpl(MethodImplOptions.NoInlining)]
	static Bien_chung_cad()
	{
		GZrJ5abseOG8VZcsys1.HgLbp0rL1w();
		int num = 4;
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
						if (num2 != 15)
						{
							if (num2 == 996)
							{
								goto end_IL_0012;
							}
							goto case 0;
						}
						XVtor = new diem(1.0, 0.0);
						num3 = 2;
						continue;
					case 7:
						return;
					case 3:
						GZrJ5abseOG8VZcsys1.lnxbScZUKm();
						num = 6;
						break;
					case 2:
						YVtor = new diem(0.0, 1.0);
						num3 = 0;
						if (_003CModule_003E_007B22ae4a70_002Dbd11_002D4241_002D81b0_002Da7bc2a6b96e9_007D.m_5b3255da57444b62ab95860a927b1a91.m_38dbbe32da8e4577a5b0e14d4a1fc7b4 != 0)
						{
							num3 = 2;
						}
						continue;
					case 4:
						GZrJ5abseOG8VZcsys1.B63beoOBW3();
						num3 = 3;
						continue;
					case 1:
						iOE6yFKN5o5rhPqoX50.lWRnjE5FLH();
						num = 1;
						if (_003CModule_003E_007B22ae4a70_002Dbd11_002D4241_002D81b0_002Da7bc2a6b96e9_007D.m_5b3255da57444b62ab95860a927b1a91.m_ccf2fe33c7c4417db1fc15c57563fb7b == 0)
						{
							num = 15;
						}
						break;
					case 5:
						acDoc = null;
						num3 = 7;
						continue;
					case 6:
						wW9DU8KIcd4TUwrXDpt.fDfJk2ecXg();
						num3 = 15;
						if (_003CModule_003E_007B22ae4a70_002Dbd11_002D4241_002D81b0_002Da7bc2a6b96e9_007D.m_5b3255da57444b62ab95860a927b1a91.m_ffeffc0f2f1f4c3c8f3c5e0c00083456 == 0)
						{
							num3 = 1;
						}
						continue;
					case 0:
						ZVtor = new diem(0.0, 0.0, 1.0);
						num3 = 8;
						if (_003CModule_003E_007B22ae4a70_002Dbd11_002D4241_002D81b0_002Da7bc2a6b96e9_007D.m_5b3255da57444b62ab95860a927b1a91.m_6ccf05993e964ad9b404086161a60d78 != 0)
						{
							num3 = 4;
						}
						continue;
					case 8:
						Revit_run = false;
						num3 = 0;
						if (_003CModule_003E_007B22ae4a70_002Dbd11_002D4241_002D81b0_002Da7bc2a6b96e9_007D.m_5b3255da57444b62ab95860a927b1a91.m_d9000c20e98749e58f7ec5520421bdd8 == 0)
						{
							num3 = 5;
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
		}
	}

	[DllImport("user32.dll")]
	public static extern bool SetForegroundWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	public static extern IntPtr SetFocus(IntPtr hWnd);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

	[DllImport("user32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
	public static extern bool IsIconic(IntPtr hwnd);

	[DllImport("user32.dll")]
	public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	[DllImport("oleaut32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
	public static extern int GetActiveObject(ref Guid rclsid, IntPtr pvReserved, ref IntPtr ppunk);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static object GetObject_core(string progID)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void FocusWindow(string strWindowCaption, string strClassName)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool XoaWiXDw2vTMVV8ZN4m()
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static Bien_chung_cad YlbG65DuNTff3xJqLOC()
	{
		return null;
	}
}
