using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using FmOK6lC4PapVZikenVa;
using Microsoft.VisualBasic.CompilerServices;
using mjegmktKLOyWiljG1nX;
using sRpiR1CI2v3uIudXaGA;

namespace Kata_Class_Lib_Revit;

[StandardModule]
public sealed class Perform_2D
{
	public static List<View3D> all3DViews;

	public static Transaction tx;

	public static ElementId rebar_category_id;

	private static Perform_2D lSL4UxfbZM0AT58buUw;

	[MethodImpl(MethodImplOptions.NoInlining)]
	static Perform_2D()
	{
		fJUeITteiwtDJsNdQol.D5ctJ5txFm();
		int num = 3;
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
					case 2:
						fJUeITteiwtDJsNdQol.UKytE77xrn();
						num3 = 0;
						if (_003CModule_003E_007Bddb41783_002Da8ee_002D478b_002Da0f2_002D0efdc83d619c_007D.m_22ccadcc7b8d4b17aeb5723ee7edb370.m_3bd95989acde451eaad24cae1098cbcb != 0)
						{
							num3 = 2;
						}
						continue;
					case 4:
						udQmOjCORxRL7YsRZe0.qcbEGyxWLX();
						num3 = 0;
						if (_003CModule_003E_007Bddb41783_002Da8ee_002D478b_002Da0f2_002D0efdc83d619c_007D.m_22ccadcc7b8d4b17aeb5723ee7edb370.m_f0e5853a5e2a42a6a878fc853bd10abd == 0)
						{
							num3 = 5;
						}
						continue;
					case 1:
						return;
					case 5:
						goto end_IL_000e;
					case 0:
						bQmZ2KCKO7T4RTFr4XD.fQZBffWkRQ();
						num3 = 2;
						if (_003CModule_003E_007Bddb41783_002Da8ee_002D478b_002Da0f2_002D0efdc83d619c_007D.m_22ccadcc7b8d4b17aeb5723ee7edb370.m_2d38437f74c2481e85d67e7b90b195ce != 0)
						{
							num3 = 4;
						}
						continue;
					case 3:
						fJUeITteiwtDJsNdQol.RcNt1iGNhH();
						num3 = 2;
						continue;
					}
					switch (num2)
					{
					case 993:
						break;
					default:
						return;
					case 12:
						rebar_category_id = null;
						num3 = 6;
						if (_003CModule_003E_007Bddb41783_002Da8ee_002D478b_002Da0f2_002D0efdc83d619c_007D.m_22ccadcc7b8d4b17aeb5723ee7edb370.m_0bcf85669e4f4422b7a298ed7a6e9d0a == 0)
						{
							num3 = 1;
						}
						continue;
					}
					break;
				}
				continue;
				end_IL_000e:
				break;
			}
			all3DViews = new List<View3D>();
			num = 12;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static ParameterFilterElement get_parameter_filter_element(string name, Document doccu)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void add_filter_rebar_theo_partition(ElementId rebar_category_id, string partition, string name_filter, View vieww, Document doccu)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void tao_filter(string ten_ck, string ten_filter)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static XYZ convert_diem_cad(View view, XYZ diem_goc, diem diem)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static List<XYZ> return_list_xyz_from_curveloop(CurveLoop curve_loop)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static List<XYZ> get_list_point_from_list_curve(List<Curve> list_curve)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Hidden_rebar(View view, string Partition)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Hidden_view_section_doc(View view)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static bool check_rebar_Partition(View view, string Partition)
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Hidden_view_section_ngang(View view, string Partition)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void dieu_chinh_khung_view(View Section_view, XYZ view_Min, XYZ view_Max)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static ViewSection tao_section_dung(XYZ d1, XYZ d2, string ten, int tl, double chieu_sau_trc = 50.0, double chieu_sau_sau = 50.0)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static ViewSection tao_section_nam(XYZ d1, XYZ d2, string ten, int tl, double H, double offset = 50.0)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static object dim_ro_revit_fromcad(View view, XYZ diem_goc, Bien_chung.info_revit_dim_ro info_dim, [Optional][DefaultParameterValue("")] ref string Dimstyle)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static object dim_ro_revit(View view, XYZ[] p, double t, [Optional][DefaultParameterValue("")] ref string Dimstyle)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static object dim_al_revit_Runlength(View view, XYZ p1, XYZ p2, XYZ p3 = null)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static object dim_al_revit(View view, XYZ p1, XYZ p2, double t, [Optional][DefaultParameterValue("")] ref string Dimstyle)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static MultiReferenceAnnotation Create_rebar_mul_Tag(View view, Rebar rebar, XYZ goc, string loai, int leader, XYZ LeaderElbow)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static FamilyInstance Create_Break_Line(View View, XYZ goc1, XYZ goc2)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static Element ve_line_cat_mc(View section_ngang, List<Element> List_Element, Line line)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static TextNote AddText_revit(View view, XYZ goc, string text, double height, short Alignment = 0)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static DetailCurve ve_line_rebar(View view, XYZ d1, XYZ d2, short D)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static int lineweight_rebar(int D)
	{
		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ve_circle(View view, XYZ p, short r)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool uqGiN1fH1iYn6Fv6QIc()
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static Perform_2D U0rI2Nfk9daDVO0pph1()
	{
		return null;
	}
}
