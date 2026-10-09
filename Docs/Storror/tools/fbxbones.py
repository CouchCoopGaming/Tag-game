import struct, zlib, sys, json
data=open(sys.argv[1],'rb').read()
ver=struct.unpack_from('<I',data,23)[0]; big=ver>=7500
def rd_prop(o):
    t=chr(data[o]); o+=1
    if t=='Y': return struct.unpack_from('<h',data,o)[0],o+2
    if t=='C': return data[o],o+1
    if t=='I': return struct.unpack_from('<i',data,o)[0],o+4
    if t=='F': return struct.unpack_from('<f',data,o)[0],o+4
    if t=='D': return struct.unpack_from('<d',data,o)[0],o+8
    if t=='L': return struct.unpack_from('<q',data,o)[0],o+8
    if t in 'SR':
        n=struct.unpack_from('<I',data,o)[0]; s=data[o+4:o+4+n]; return (s.decode('utf8','replace') if t=='S' else s),o+4+n
    if t in 'fdlib':
        n,enc,cl=struct.unpack_from('<III',data,o); o+=12; raw=data[o:o+cl]; o+=cl
        if enc: raw=zlib.decompress(raw)
        fmt={'f':'f','d':'d','l':'q','i':'i','b':'?'}[t]
        return list(struct.unpack('<%d%s'%(n,fmt),raw)) if t in 'di' and n<64 else ('arr',t,n),o
    raise ValueError(t)
def rd_node(o):
    if big: end,np_,pl=struct.unpack_from('<QQQ',data,o); o+=24
    else: end,np_,pl=struct.unpack_from('<III',data,o); o+=12
    nl=data[o]; name=data[o+1:o+1+nl].decode(); o+=1+nl
    if end==0: return None,o
    props=[]
    for _ in range(np_):
        p,o=rd_prop(o); props.append(p)
    kids=[]
    while o<end:
        k,o=rd_node(o)
        if k is None: break
        kids.append(k)
    return (name,props,kids),end
o=27; top=[]
while o<len(data)-200:
    n,o=rd_node(o)
    if n is None: break
    top.append(n)
objs={t[0]:t for t in top}
models={}
for n in objs['Objects'][2]:
    if n[0]=='Model':
        uid,name,typ=n[1][0],n[1][1].split('\x00')[0],n[1][2]
        tr=[0,0,0]; rot=[0,0,0]; sc=[1,1,1]
        for k in n[2]:
            if k[0]=='Properties70':
                for p in k[2]:
                    if p[1][0]=='Lcl Translation': tr=p[1][4:7]
                    if p[1][0]=='Lcl Rotation': rot=p[1][4:7]
                    if p[1][0]=='Lcl Scaling': sc=p[1][4:7]
        models[uid]=dict(name=name,type=typ,t=tr,r=rot,s=sc)
par={}
for c in objs['Connections'][2]:
    if c[1][0]=='OO' and c[1][1] in models: par[c[1][1]]=c[1][2]
gs=objs.get('GlobalSettings')
if gs:
    for k in gs[2]:
        if k[0]=='Properties70':
            for p in k[2]:
                if p[1][0] in('UpAxis','UpAxisSign','FrontAxis','CoordAxis','UnitScaleFactor','OriginalUnitScaleFactor'): print(p[1][0],p[1][4])
for u,m in models.items():
    m['parent']=models.get(par.get(u),{}).get('name')
    print(m['name'],m['type'],m['parent'],[round(x,4) for x in m['t']],[round(x,2) for x in m['r']],[round(x,3) for x in m['s']])
