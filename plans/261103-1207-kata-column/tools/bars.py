import json,math,sys
def load(p): return json.load(open(p,encoding='utf-8-sig'))['entities']
def length(pts):
    L=0
    for i in range(len(pts)-1):
        (x0,y0,b),(x1,y1,_)=pts[i],pts[i+1]
        c=math.hypot(x1-x0,y1-y0)
        if abs(b)>1e-9:
            th=4*math.atan(abs(b)); r=c/(2*math.sin(th/2)); L+=r*th
        else: L+=c
    return L
def elevation_bars(es):
    out=[]
    for e in es:
        if e['layer']=='kata_thep chu' and e['type']=='AcDbPolyline':
            pts=[tuple(p) for p in e['pts']]
            ded=[pts[0]]+[p for i,p in enumerate(pts[1:]) if (p[0],p[1])!=(pts[i][0],pts[i][1])]
            if max(p[0] for p in ded) < 2500: out.append(ded)
    return out
if __name__=='__main__':
    for p in sys.argv[1:]:
        es=load(p); bars=elevation_bars(es)
        print('==',p.split('/')[-1], len(bars))
        for b in sorted(bars,key=lambda d:(d[0][0]>500,min(q[1] for q in d))):
            L=length(b); n=11700/L
            print(f"   x{round(b[-1][0]):5} y {round(min(q[1] for q in b)):6}->{round(max(q[1] for q in b)):6} L={L:7.0f} 11700/L={n:5.2f}")
