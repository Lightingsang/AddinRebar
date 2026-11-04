import json,sys,os
def load(t): return json.load(open(t,encoding='utf-8-sig'))['entities']
for t in sys.argv[1:]:
    es=load(t)
    outl=[e for e in es if e['layer']=='kata_net thay' and e['type']=='AcDbPolyline' and len(e['pts'])==5]
    if not outl: print(t,'no section'); continue
    o=min(outl,key=lambda e:min(p[1] for p in e['pts']))
    xs=[p[0] for p in o['pts']]; ys=[p[1] for p in o['pts']]
    x0,x1,y0,y1=min(xs),max(xs),min(ys),max(ys)
    bars=[(round(e['at'][0]-x0),round(e['at'][1]-y0)) for e in es if e.get('name')=='kata_block_THEP' and x0<=e['at'][0]<=x1 and y0<=e['at'][1]<=y1]
    bx=sorted(set(b[0] for b in bars)); by=sorted(set(b[1] for b in bars),reverse=True)
    ties=[]
    for e in es:
        if e['layer']!='kata_thep dai' or e['type']!='AcDbPolyline': continue
        px=[p[0] for p in e['pts']]; py=[p[1] for p in e['pts']]
        if not (x0-60<=min(px) and max(px)<=x1+60 and y0-60<=min(py) and max(py)<=y1+60): continue
        k='C' if len(e['pts'])==6 else 'O'
        ties.append(f"{k}[x{round(min(px)-x0)}..{round(max(px)-x0)} y{round(min(py)-y0)}..{round(max(py)-y0)}]")
    print('==',os.path.basename(t)[:-5],len(bars),'bars  xs',bx,'ys',by)
    for y in by: print('    ',' '.join('o' if (x,y) in bars else '.' for x in bx))
    print('    ties',' '.join(ties))
