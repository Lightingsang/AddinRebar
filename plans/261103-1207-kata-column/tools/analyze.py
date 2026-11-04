import json,glob,os,collections,sys
def load(t): return json.load(open(f'out/{t}.json',encoding='utf-8-sig'))
def sig(e):
    t=e['type']; l=e['layer']
    if t=='AcDbPolyline': return (l,'PL',tuple(tuple(p[:2]) for p in e['pts']))
    if t=='AcDbLine': return (l,'LN',tuple(tuple(p) for p in e['pts']))
    if t=='AcDbBlockReference': return (l,'IN',e['name'],tuple(e['at']),json.dumps(e.get('attrs',{}),ensure_ascii=False))
    if 'Dimension' in t: return (l,'DIM',e.get('value'),e.get('text'),tuple(e.get('textAt',[])))
    if t=='AcDbText': return (l,'TX',e.get('text'),tuple(e.get('at',[])))
    if t=='AcDbLeader': return (l,'LD',tuple(tuple(p) for p in e['pts']))
    return (l,t)
base=collections.Counter(sig(e) for e in load('v00-base')['entities'])
for f in sorted(glob.glob('out/*.json')):
    t=os.path.basename(f)[:-5]
    if t=='v00-base': continue
    d=load(t); cur=collections.Counter(sig(e) for e in d['entities'])
    add=cur-base; rem=base-cur
    print(f'=== {t}: n={d["count"]} +{sum(add.values())} -{sum(rem.values())}')
    if len(sys.argv)>1 and t.startswith(sys.argv[1]):
        for k in list(add)[:60]: print('  +',k)
        for k in list(rem)[:60]: print('  -',k)
    else:
        bylayer=collections.Counter((k[0],k[1]) for k in add.elements())
        bylayer_r=collections.Counter((k[0],k[1]) for k in rem.elements())
        print('   add',dict(bylayer)); print('   rem',dict(bylayer_r))
