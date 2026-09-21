# SAP2000 OAPI Cheat Sheet (SAP2000v1)

Chữ ký và mẫu gọi API hay dùng qua `execute_sap2000_code`. Lưu ý: **Units ép sang `kN_m_C` (m, kN, kN·m, kPa)** trong suốt thời gian script chạy.

## 1. Document & File

```csharp
// Lấy tên file đang mở (true = full path)
string path = sapModel.GetModelFilename(true);

// Lấy thư mục chứa model
string folder = sapModel.GetModelFilepath();

// Lưu model (D)
int ret = sapModel.File.Save();
```

## 2. Geometry — Points & Frames

```csharp
// Danh sách điểm
int np = 0; string[] pointNames = null;
int ret = sapModel.PointObj.GetNameList(ref np, ref pointNames);

// Tọa độ điểm (Global CSys)
double x = 0, y = 0, z = 0;
int retC = sapModel.PointObj.GetCoordCartesian("1", ref x, ref y, ref z, "Global");

// Gán liên kết gối: [u1, u2, u3, r1, r2, r3]
bool[] restraints = new[] { true, true, true, false, false, false }; // pinned
int retR = sapModel.PointObj.SetRestraint("1", ref restraints, eItemType.Objects);

// Danh sách frame
int nf = 0; string[] frameNames = null;
int retF = sapModel.FrameObj.GetNameList(ref nf, ref frameNames);

// Vẽ frame mới (m)
string name = "";
int retA = sapModel.FrameObj.AddByCoord(0, 0, 0, 0, 0, 3.0, ref name, "Default", "", "Global");

// Gán tiết diện frame
int retS = sapModel.FrameObj.SetSection("1", "W14X90");
```

## 3. Materials & Sections

```csharp
// Danh sách vật liệu
int nm = 0; string[] matNames = null;
int retM = sapModel.PropMaterial.GetNameList(ref nm, ref matNames);

// Cơ tính đẳng hướng: lưu ý SAP2000 cần tham số thứ 6 double Temp = 0.0
double e = 0, u = 0, a = 0, g = 0;
int retI = sapModel.PropMaterial.GetMPIsotropic("STEEL", ref e, ref u, ref a, ref g, 0.0);
// e tính theo kN/m² -> chia 1000 để ra MPa

// Danh sách tiết diện frame
int ns = 0; string[] secNames = null;
int retSec = sapModel.PropFrame.GetNameList(ref ns, ref secNames);
```

## 4. Loads & Definitions

```csharp
// Load patterns
int npat = 0; string[] patNames = null;
int retP = sapModel.LoadPatterns.GetNameList(ref npat, ref patNames);

// Gán tải phân bố trên frame: Dir 10 = Gravity, Dir 1-3 = Local, Dir 4-6 = Global
// val1, val2 theo kN/m
int retL = sapModel.FrameObj.SetLoadDistributed("1", "DEAD", 1, 10, 0, 1, 10.0, 10.0, "Global", true, true, eItemType.Objects);
```

## 5. Analysis & Results

```csharp
// Khóa model / kết quả
bool isLocked = sapModel.GetModelIsLocked();

// Chạy phân tích (D)
int retRun = sapModel.Analyze.RunAnalysis();

// Phản lực gối
sapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();
sapModel.Results.Setup.SetCaseSelectedForOutput("DEAD", true);
int nRes = 0; string[] pNames = null, loadCases = null, stepTypes = null;
double[] stepNums = null, f1 = null, f2 = null, f3 = null, m1 = null, m2 = null, m3 = null;
int retJ = sapModel.Results.JointReact("1", eItemTypeElm.Element, ref nRes, ref pNames, ref loadCases, ref stepTypes, ref stepNums, ref f1, ref f2, ref f3, ref m1, ref m2, ref m3);
```
