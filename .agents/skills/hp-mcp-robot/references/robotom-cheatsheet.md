# RobotOM API Cheatsheet

Hướng dẫn tra cứu các hàm và thuộc tính thông dụng trong thư viện `RobotOM.dll`:

## 1. Nút (Nodes)

```csharp
// Đếm số lượng nút
int count = structure.Nodes.GetAll().Count;

// Tìm hoặc tạo số hiệu nút trống
int freeNode = structure.Nodes.FreeNumber;

// Tạo nút mới tại tọa độ (x, y, z) tính bằng mét
structure.Nodes.Create(freeNode, 0.0, 0.0, 3.0);

// Đọc tọa độ nút
var node = structure.Nodes.Get(freeNode);
double x = node.X;
double y = node.Y;
double z = node.Z;
```

## 2. Thanh (Bars)

```csharp
// Đếm số lượng thanh
int barCount = structure.Bars.GetAll().Count;

// Tạo thanh mới nối giữa 2 nút
int freeBar = structure.Bars.FreeNumber;
structure.Bars.Create(freeBar, startNodeId, endNodeId);

// Gán nhãn tiết diện
structure.Bars.Get(freeBar).SetLabel(IRobotLabelType.I_LT_BAR_SECTION, "HEA 200");
```

## 3. Liên kết gối (Supports)

```csharp
// Gán liên kết nút (Pinned / Fixed)
var node = structure.Nodes.Get(nodeId);
node.SetLabel(IRobotLabelType.I_LT_SUPPORT, "Fixed");
```

## 4. Tải trọng (Loads)

```csharp
// Lấy trường hợp tải trọng số 1
var loadCase = (IRobotSimpleCase)structure.Cases.Get(1);

// Tạo bản ghi tải phân bố đều trên thanh
var record = (IRobotLoadRecord2)loadCase.Records.Create(IRobotLoadRecordType.I_LRT_BAR_UNIFORM);
record.SetValue((short)IRobotBarUniformLoadValueName.I_BULVN_PZ, -15.0); // -15 kN/m
record.Objects.AddOne(barNumber);
```

## 5. Chạy phân tích (Calculations)

```csharp
// Kích hoạt giải kết cấu FEA
robot.Project.CalcEngine.Calculate();
```

## 6. Lấy kết quả (Results)

```csharp
// Đọc phản lực nút cho trường hợp tải 1
var node = structure.Nodes.Get(nodeId);
var reactionParams = robot.CmpntFactory.Create(IRobotComponentType.I_CT_NODE_REACTION_DATA);
var reactions = node.GetReaction(1, reactionParams);
double fz = reactions.FZ; // kN

// Đọc nội lực thanh
var bar = structure.Bars.Get(barId);
var forceParams = robot.CmpntFactory.Create(IRobotComponentType.I_CT_BAR_FORCE_DATA);
var forces = bar.GetForce(1, 0.5, forceParams); // tại giữa nhịp x = 0.5
double my = forces.MY; // kN.m
```
