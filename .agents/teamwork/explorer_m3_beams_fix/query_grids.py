import json, sys
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

def main():
    with RevitIPC() as ipc:
        code = """
        var grids = new FilteredElementCollector(doc).OfClass(typeof(Grid)).Cast<Grid>()
            .Select(g => {
                var crv = g.Curve;
                var p0 = crv.GetEndPoint(0);
                var p1 = crv.GetEndPoint(1);
                return new {
                    name = g.Name,
                    x0 = Math.Round(p0.X * 304.8, 2),
                    y0 = Math.Round(p0.Y * 304.8, 2),
                    x1 = Math.Round(p1.X * 304.8, 2),
                    y1 = Math.Round(p1.Y * 304.8, 2)
                };
            }).ToList();
        return grids;
        """
        res = ipc.execute(code, transaction='none', label='Query Grids')
        grids = res['result']['value']
        
        x_grids = []
        y_grids = []
        for g in grids:
            # If x0 == x1, vertical grid -> constant X
            if abs(g['x0'] - g['x1']) < 5.0:
                x_val = (g['x0'] + g['x1']) / 2.0
                x_grids.append((g['name'], round(x_val, 2)))
            else:
                y_val = (g['y0'] + g['y1']) / 2.0
                y_grids.append((g['name'], round(y_val, 2)))

        x_grids.sort(key=lambda x: x[1])
        y_grids.sort(key=lambda y: y[1])

        print(f"X Grids ({len(x_grids)}):")
        for g in x_grids:
            print(f"  Grid {g[0]:<5}: X = {g[1]:10.2f} mm")

        print(f"\nY Grids ({len(y_grids)}):")
        for g in y_grids:
            print(f"  Grid {g[0]:<5}: Y = {g[1]:10.2f} mm")

        with open('grids.json', 'w', encoding='utf-8') as f:
            json.dump({'x_grids': x_grids, 'y_grids': y_grids}, f, indent=2)

if __name__ == '__main__':
    main()
