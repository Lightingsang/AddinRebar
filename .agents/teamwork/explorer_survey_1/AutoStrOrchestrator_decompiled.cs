using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using J71K84QJmVHHhK31Efv4;

namespace ClassLibrary1.AutoStr;

public class AutoStrOrchestrator
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[CompilerGenerated]
	private EventHandler<AutoStrProgress> hTPiA6RMVcb;

	[CompilerGenerated]
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private EventHandler<bool> WhQiApstj2M;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[CompilerGenerated]
	private AutoStrConfig uASiAZ3VcRp;

	[CompilerGenerated]
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private AutoStrPaths bJBiAz6Fmaw;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[CompilerGenerated]
	private AutoStrCheckpoint v85inIiL8DQ;

	private CancellationTokenSource ztuinqnCLmU;

	private bool mfIiniPpLKI;

	private bool QT6inWQZMqB;

	private bool oBxinwCyFey;

	private int FhSinQHhS7c;

	private readonly HashSet<AutoStrStep> HqIinApwXIE;

	private readonly List<AutoStrNghiemThuBuoc.KetQua> wQ6innWqBRM;

	private readonly HashSet<string> L27inLpU7iH;

	private int u0Kin27JM4C;

	private AutoStrAiVongKiem.PhienAi QfqinvwP8uA;

	private AutoStrStepResult B7DinlbJfAM;

	private readonly List<Tuple<AutoStrStep, Func<AutoStrStepResult>, string, AutoStrFloorInfo>> fPiin4WikMi;

	private int xM3inxpk7xy;

	private readonly HashSet<string> MRrinBPqH9U;

	private readonly List<string> WR2ineV7Dg4;

	private bool LhdinXGg81E;

	private List<Tuple<AutoStrStep, Func<AutoStrStepResult>, string, AutoStrFloorInfo, DateTime, AutoStrStepResult>> zBHinbfaKai;

	private bool rQwinONx1wj;

	[CompilerGenerated]
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private string DJjinJhOYXu;

	private static IntPtr dwYin58OLOy;

	internal static AutoStrOrchestrator BG1cvuJ68RyGwANRpMIx;

	public AutoStrConfig Config
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		[CompilerGenerated]
		get
		{
			return null;
		}
	}

	public AutoStrPaths Paths
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		[CompilerGenerated]
		get
		{
			return null;
		}
	}

	public AutoStrCheckpoint Checkpoint
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		[CompilerGenerated]
		get
		{
			return null;
		}
	}

	public string FileNghiemThu
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		[CompilerGenerated]
		get
		{
			return null;
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		[CompilerGenerated]
		set
		{
		}
	}

	public event EventHandler<AutoStrProgress> ProgressChanged
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		[CompilerGenerated]
		add
		{
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		[CompilerGenerated]
		remove
		{
		}
	}

	public event EventHandler<bool> Finished
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		[CompilerGenerated]
		add
		{
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		[CompilerGenerated]
		remove
		{
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static AutoStrOrchestrator()
	{
		G5dBxcQJcPN0MCbH3m8n.OyCQ5AUxbeD();
		int num = 1;
		while (true)
		{
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				case 4:
					return;
				default:
					urc76EASjHxVZoUHkpWY.gmsASMJhCUu(urc76EASjHxVZoUHkpWY.y9wAScPtA5M);
					num2 = 3;
					if (<Module>{9f5b0bd4-f257-4014-a51f-94b51b7efaf6}.m_7f0a9ad703634694b551e1dc5483c251.m_87bebdff099842e789d267b8b5aed7a5 == 0)
					{
						num2 = 2;
					}
					continue;
				case 1:
					eY9PdxAS3RcmxYUq6qIf.gmsASMJhCUu(eY9PdxAS3RcmxYUq6qIf.afaASF2QF6s);
					num2 = 0;
					if (<Module>{9f5b0bd4-f257-4014-a51f-94b51b7efaf6}.m_7f0a9ad703634694b551e1dc5483c251.m_d3d5707bf457453dbfa891eb53bcbdc1 != 0)
					{
						num2 = 0;
					}
					continue;
				case 5:
					op712EASTZerQSi7YJGS.gmsASMJhCUu(op712EASTZerQSi7YJGS.lEFASfGZ2xU);
					num2 = 2;
					if (<Module>{9f5b0bd4-f257-4014-a51f-94b51b7efaf6}.m_7f0a9ad703634694b551e1dc5483c251.m_e49fc6fec18f4258873b68b3458b8f25 != 0)
					{
						num2 = 0;
					}
					continue;
				case 3:
					YSFLdlASmvpJOGtTGyYY.gmsASMJhCUu(YSFLdlASmvpJOGtTGyYY.VGmASNQs7SS);
					num2 = 5;
					continue;
				case 2:
					break;
				}
				break;
			}
			dwYin58OLOy = IntPtr.Zero;
			num = 4;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public AutoStrOrchestrator(AutoStrConfig cfg)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public void StartAsync()
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public void ContinueCheckpoint()
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public void Cancel()
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string XQxiAMqJVeJ(object P_0)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private List<AutoStrFloorInfo> po6iA3JYdM3()
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool TU9iAFqJPil(int P_0)
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool H45iAj8uBWf(int P_0)
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static int QEbiAcix8WW(int P_0)
	{
		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void UJ4iAmAqFkR(CancellationToken P_0)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void COoiANbbZOB(CancellationToken P_0)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void ov9iATI1ao9(CancellationToken P_0)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private string j42iAfsJhqa(bool P_0, int P_1)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void meriAhCwKTd(AutoStrStep P_0, Func<AutoStrStepResult> P_1, string P_2 = "", AutoStrFloorInfo P_3 = null)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void WM7iAggM2gB(AutoStrStep P_0, AutoStrFloorInfo P_1)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void CuXiAa5Ycfm(AutoStrStep P_0, Func<AutoStrStepResult> P_1, string P_2 = "", AutoStrFloorInfo P_3 = null)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private int UVTiAPw7oNX(ref Func<string, bool> P_0)
	{
		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void JdfiAtyKqh3(List<Tuple<AutoStrStep, Func<AutoStrStepResult>, string, AutoStrFloorInfo, DateTime, AutoStrStepResult>> P_0, CancellationToken P_1)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void z5hiAG5vgC9(AutoStrStep P_0, Func<AutoStrStepResult> P_1, string P_2, AutoStrFloorInfo P_3, DateTime P_4)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private AutoStrNghiemThuBuoc.KetQua GlciA8JJnkO(AutoStrStep P_0, AutoStrFloorInfo P_1, AutoStrNghiemThuBuoc.KetQua P_2, int P_3, Func<string, bool> P_4)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string OshiAscXuVG(object P_0)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool f0SiAoM4Bor(AutoStrStep P_0, Func<AutoStrStepResult> P_1, string P_2, ref List<string> P_3)
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool e56iASx9Trv(AutoStrStep P_0, List<string> P_1)
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void a9ZiAy3RjRI()
	{
	}

	[DllImport("user32.dll", EntryPoint = "PostMessageW")]
	private static extern bool oxHiAHmSVxv(IntPtr P_0, uint P_1, IntPtr P_2, IntPtr P_3);

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool NHIiArgQnBX(CancellationToken P_0)
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void On5iAKyfv4o(AutoStrStep P_0, AutoStrStepStatus P_1, string P_2, Exception P_3 = null)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool jSJs44J6skjvJbojkUQs()
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static AutoStrOrchestrator uuWZa8J6omtvCAoWZkk1()
	{
		return null;
	}
}
