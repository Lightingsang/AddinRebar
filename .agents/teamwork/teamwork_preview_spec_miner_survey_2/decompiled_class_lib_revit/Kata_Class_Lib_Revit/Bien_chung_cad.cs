using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FmOK6lC4PapVZikenVa;
using Microsoft.VisualBasic.CompilerServices;
using mjegmktKLOyWiljG1nX;
using sRpiR1CI2v3uIudXaGA;

namespace Kata_Class_Lib_Revit;

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

	private static Bien_chung_cad z4bwdaq3tAMRnmqaPjx;

	[MethodImpl(MethodImplOptions.NoInlining)]
	static Bien_chung_cad()
	{
		fJUeITteiwtDJsNdQol.D5ctJ5txFm();
		int num = 8;
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
							goto case 5;
						}
						Revit_run = false;
						num3 = 15;
						if (_003CModule_003E_007Bddb41783_002Da8ee_002D478b_002Da0f2_002D0efdc83d619c_007D.m_22ccadcc7b8d4b17aeb5723ee7edb370.m_4e758f16a6ab4f92acb666f2c829b4e7 != 0)
						{
							num3 = 4;
						}
						continue;
					case 5:
						bQmZ2KCKO7T4RTFr4XD.fQZBffWkRQ();
						num3 = 2;
						continue;
					case 1:
						YVtor = new diem(0.0, 1.0);
						num3 = 3;
						if (_003CModule_003E_007Bddb41783_002Da8ee_002D478b_002Da0f2_002D0efdc83d619c_007D.m_22ccadcc7b8d4b17aeb5723ee7edb370.m_4e758f16a6ab4f92acb666f2c829b4e7 != 0)
						{
							num3 = 0;
						}
						continue;
					case 0:
						break;
					case 4:
						acDoc = null;
						num3 = 3;
						if (_003CModule_003E_007Bddb41783_002Da8ee_002D478b_002Da0f2_002D0efdc83d619c_007D.m_22ccadcc7b8d4b17aeb5723ee7edb370.m_78f38f0b03cc49dcb44ace1e5cfe703c == 0)
						{
							num3 = 15;
						}
						continue;
					case 8:
						fJUeITteiwtDJsNdQol.RcNt1iGNhH();
						num3 = 7;
						continue;
					case 3:
						return;
					case 7:
						fJUeITteiwtDJsNdQol.UKytE77xrn();
						num3 = 5;
						continue;
					case 2:
						udQmOjCORxRL7YsRZe0.qcbEGyxWLX();
						num3 = 6;
						continue;
					case 6:
						XVtor = new diem(1.0, 0.0);
						num3 = 1;
						if (_003CModule_003E_007Bddb41783_002Da8ee_002D478b_002Da0f2_002D0efdc83d619c_007D.m_22ccadcc7b8d4b17aeb5723ee7edb370.m_01d3afdc940441458efb20007662fa66 != 0)
						{
							num3 = 2;
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
			ZVtor = new diem(0.0, 0.0, 1.0);
			num = 15;
			if (_003CModule_003E_007Bddb41783_002Da8ee_002D478b_002Da0f2_002D0efdc83d619c_007D.m_22ccadcc7b8d4b17aeb5723ee7edb370.m_1b695d0d6b044c239f8728cc536d008f != 0)
			{
				num = 4;
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
	internal static bool i6K4D8qOIfJFqbtcS4y()
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static Bien_chung_cad UiXNKXqIdwSJVsV92gk()
	{
		return null;
	}
}
