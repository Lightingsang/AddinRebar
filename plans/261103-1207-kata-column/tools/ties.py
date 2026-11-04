import json,glob,os,sys
def load(t): return json.load(open(t,encoding='utf-8-sig'))['entities']
def sec(es):
    outl=[e for e in es if e['layer']=='kata_net thay' and e['type']=='AcDbPolyline' and len(e['pts'])==5]
    o=min(outl,key=lambda e:min(p[1] for p in e['pts']))
    xs=[p[0] for p in o['pts']]; ys=[p[1] for p in o['pts']]
    return min(xs),max(xs),min(ys),max(ys)
for t in sorted(glob.glob(sys.argv[1])):
    es=load(t); x0,x1,y0,y1=sec(es)
    bx=sorted(set(round(e['at'][0]-x0) for e in es if e.get('name')=='kata_block_THEP' and x0<=e['at'][0]<=x1 and y0<=e['at'][1]<=y1))
    ties=[]
    for e in es:
        if e['layer']!='kata_thep dai' or e['type']!='AcDbPolyline': continue
        xs=[p[0] for p in e['pts']]; ys=[p[1] for p in e['pts']]
        if not (x0-60<=min(xs) and max(xs)<=x1+60 and y0-60<=min(ys) and max(ys)<=y1+60): continue
        k='C' if len(e['pts'])==6 else 'O'
        ties.append(f"{k}[x{round(min(xs)-x0)}..{round(max(xs)-x0)} y{round(min(ys)-y0)}..{round(max(ys)-y0)}]")
    print(os.path.basename(t)[:-5], 'xcols', bx); print('     ', ' '.join(ties))
