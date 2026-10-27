"""Dependency-free starter meshes; Pillow is only used for the contact sheet.
Run: python Tools/generate_starter_models.py
Coordinates: meters, Y up, bottom-centered origin. Faces are flat shaded.
"""
import json, math, struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/MathRunner/Art/Models'
PALETTE = dict(red='#EF5350', teal='#20B7AC', yellow='#FFD14A', orange='#FF8B36',
    white='#FFF4DF', dark='#263B50', glass='#73D9EE', blue='#438BE3', grey='#929CA9',
    skin='#EFB387', hair='#70412D', green='#60BE66', leaf='#389D62', pink='#F476A6',
    gold='#FFC536', wood='#BA7850', black='#27313C')

class Model:
    def __init__(self, name, budget):
        self.name, self.budget, self.parts = name, budget, []
    def mesh(self, vertices, faces, color):
        self.parts.append((vertices, faces, color))
    def box(self, p, s, c):
        x,y,z=p; a,b,d=[v/2 for v in s]
        v=[(x+i*a,y+j*b,z+k*d) for i,j,k in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
        self.mesh(v,[(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(3,7,6,2),(0,1,5,4)],c)
    def ellipsoid(self,p,s,c,n=10,rings=5):
        v=[]
        for j in range(rings+1):
            t=math.pi*j/rings
            for i in range(n):
                a=2*math.pi*i/n
                v.append((p[0]+s[0]*math.sin(t)*math.cos(a),p[1]+s[1]*math.cos(t),p[2]+s[2]*math.sin(t)*math.sin(a)))
        f=[]
        for j in range(rings):
            for i in range(n):
                a=j*n+i;b=j*n+(i+1)%n
                if j==0: f.append((a,b,b+n))
                elif j==rings-1: f.append((a,b,a+n))
                else: f.append((a,b,b+n,a+n))
        self.mesh(v,f,c)
    def cylinder(self,p,r,h,c,n=10,top=None,axis='y'):
        rt=r if top is None else top
        v=[]
        for height,rad in [(-h/2,r),(h/2,rt)]:
            for i in range(n):
                a=2*math.pi*i/n
                q=(rad*math.cos(a),height,rad*math.sin(a))
                if axis=='z': q=(q[0],-q[2],q[1])
                v.append(tuple(p[k]+q[k] for k in range(3)))
        f=[tuple(range(n)),tuple(range(2*n-1,n-1,-1))]
        f += [(i,i+n,(i+1)%n+n,(i+1)%n) for i in range(n)]
        self.mesh(v,f,c)
    def beam(self,a,b,r,c):
        # Low-poly rectangular limb aligned along any vector.
        d=[b[i]-a[i] for i in range(3)]; length=math.sqrt(sum(x*x for x in d)); u=[x/length for x in d]
        ref=(0,0,1) if abs(u[2])<.9 else (1,0,0)
        v=[u[1]*ref[2]-u[2]*ref[1],u[2]*ref[0]-u[0]*ref[2],u[0]*ref[1]-u[1]*ref[0]]
        norm=math.sqrt(sum(x*x for x in v));v=[x/norm*r for x in v]
        w=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]
        pts=[tuple(p[k]+i*v[k]+j*w[k] for k in range(3)) for p in (a,b) for i,j in [(-1,-1),(1,-1),(1,1),(-1,1)]]
        self.mesh(pts,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],c)

models=[]
def new(name,budget):
    m=Model(name,budget);models.append(m);return m

def runner(name,shirt,pants):
    m=new(name,15000)
    for x in [-.14,.14]:
        m.box((x,.09,.065),(.23,.18,.38),'white')
        m.box((x,.035,.065),(.24,.07,.39),'dark')
        m.beam((x,.18,0),(x,.79,0),.105,pants)
    m.box((0,.81,0),(.48,.16,.27),pants)
    m.box((0,1.06,0),(.50,.43,.31),shirt)
    m.cylinder((0,1.32,0),.085,.14,'skin')
    m.ellipsoid((0,1.535,0),(.245,.265,.215),'skin')
    m.ellipsoid((0,1.67,-.025),(.249,.13,.213),'hair')
    for sign in [-1,1]:
        m.beam((sign*.23,1.23,0),(sign*.47,.96,0),.09,shirt)
        m.beam((sign*.47,.96,0),(sign*.64,.76,0),.075,shirt)
        m.ellipsoid((sign*.66,.74,0),(.085,.1,.08),'skin',8,4)
        m.ellipsoid((sign*.29,1.53,-.09),(.11,.18,.11),'hair',8,4)
        m.box((sign*.255,1.63,-.07),(.09,.05,.12),'pink')
        m.ellipsoid((sign*.085,1.56,.198),(.028,.037,.017),'dark',8,4)
    m.box((0,1.435,.209),(.085,.018,.015),'white')
    return m
runner('01_runner_kid','grey','blue');runner('02_runner_skin_teal','teal','dark');runner('03_runner_skin_red','red','blue')

def building(name,color,floors,kind='shop'):
    m=new(name,1500 if 'skyline' in name else 5000)
    height=floors*1.7+1
    m.box((0,.15,0),(6,.3,4),'white')
    m.box((0,height/2+.3,0),(5.8,height,3.8),color)
    m.box((0,height+.35,0),(6,.22,4),'white')
    m.box((0,.45,1.96),(5.8,.2,.12),'white')
    for floor in range(floors):
        y=1.5+floor*1.7
        for x in [-1.85,0,1.85]:
            m.box((x,y,1.94),(1.12,1.2,.1),'white')
            m.box((x,y,2.005),(.91,.99,.04),'glass')
            m.box((x,y,2.04),(.05,1,.05),'white')
            if kind=='apartment':
                m.box((x,y-.53,2.12),(1.35,.12,.5),'white')
                m.box((x,y-.22,2.36),(1.35,.08,.07),'dark')
                for dx in [-.6,0,.6]: m.box((x+dx,y-.36,2.36),(.055,.3,.055),'dark')
    if kind=='shop':
        for i in range(10): m.box((-2.475+i*.55,2.23,2.22),(.55,.18,.95),color if i%2==0 else 'white')
        m.box((0,1.1,2.08),(1.05,1.8,.1),'dark')
        m.box((0,1.18,2.15),(.85,1.5,.05),'glass')
        m.box((.29,1.05,2.2),(.08,.1,.04),'gold')
    if kind in ('apartment','corner'):
        m.box((-1,height+.7,0),(1.5,.6,1),'grey')
        m.cylinder((1.5,height+.65,-.6),.3,.5,'dark')
    return m
building('04_shop_red','red',3);building('05_shop_teal','teal',3);building('06_shop_yellow','yellow',3)
building('07_apartment_tall','teal',6,'apartment');building('08_apartment_wide','orange',4,'apartment')
m=building('09_corner_building','pink',3,'corner')
for y in [1.5,3.2,4.9]:
    for z in [-1.1,1.1]: m.box((2.94,y,z),(.05,1.1,1),'glass')
building('10_skyline_a','blue',7,'skyline');building('11_skyline_b','teal',9,'skyline')
m=new('12_street_lamp',800);m.cylinder((0,.12,0),.22,.24,'dark');m.cylinder((0,1.8,0),.07,3.4,'dark');m.box((.35,3.5,0),(.8,.12,.15),'dark');m.box((.7,3.41,0),(.45,.12,.32),'yellow')
m=new('13_tree_round',1500);m.cylinder((0,1,0),.15,2,'wood');m.ellipsoid((0,2.35,0),(1.1,1.15,1),'green',10,6);m.ellipsoid((.55,2.1,.1),(.65,.7,.65),'leaf',8,5)
m=new('14_bush',800);m.ellipsoid((0,.45,0),(.65,.45,.55),'green');m.ellipsoid((.45,.32,0),(.42,.32,.4),'leaf')
def bench(name,budget,back=True):
    m=new(name,budget)
    for x in [-.65,.65]:
        for z in [-.2,.2]: m.box((x,.25,z),(.1,.5,.1),'dark')
    for z in [-.24,-.08,.08,.24]:m.box((0,.53,z),(1.7,.12,.13),'wood')
    if back:
        for x in [-.65,.65]:m.box((x,.85,-.25),(.08,.8,.08),'dark')
        for y in [.82,1.05]:m.box((0,y,-.25),(1.7,.16,.1),'wood')
    return m
bench('15_bench',800)
m=new('16_trash_bin',500);m.cylinder((0,.48,0),.32,.96,'teal');m.cylinder((0,.97,0),.35,.1,'dark');m.box((0,.8,.32),(.3,.12,.03),'dark')
m=new('17_mailbox',500);m.box((0,.45,0),(.14,.9,.14),'dark');m.box((0,1.04,0),(.55,.5,.4),'blue');m.box((0,1.13,.208),(.4,.055,.02),'dark');m.box((.31,1.22,0),(.08,.22,.14),'red')
m=new('18_bus_shelter',1500)
for x in [-1.35,1.35]:
    for z in [-.45,.45]:m.box((x,1.1,z),(.08,2.2,.08),'dark')
m.box((0,2.2,0),(3,.18,1.3),'teal');m.box((0,1.25,-.46),(2.65,1.8,.04),'glass');m.box((0,.5,-.15),(2,.15,.45),'wood')
for x in [-.8,.8]:m.box((x,.25,-.15),(.12,.5,.3),'dark')
m=new('19_flower_planter',600);m.box((0,.2,0),(1,.4,.6),'white');m.box((0,.42,0),(.86,.08,.46),'wood')
for x,z in [(-.3,0),(0,.1),(.3,-.1)]:
    m.cylinder((x,.6,z),.025,.35,'leaf',6);m.ellipsoid((x,.79,z),(.13,.12,.13),'pink',6,3)
def vehicle(name,bus=False):
    m=new(name,2500 if bus else 2000);length=5.8 if bus else 3.1
    m.box((0,.65,0),(1.65,.8,length),'yellow' if bus else 'red')
    m.box((0,1.3,-.15),(1.5,.7,length-.6 if bus else 1.7),'teal' if bus else 'red')
    m.box((0,1.4,length/2-.27 if bus else .72),(1.3,.45,.06),'glass')
    for side in [-1,1]:
        for z in ([-2,-1,0,1,2] if bus else [-.55,.25]):m.box((side*.76,1.4,z),(.05,.45,.6),'glass')
        for z in [-length*.32,length*.32]:
            m.ellipsoid((side*.81,.36,z),(.16,.36,.36),'dark',10,5)
        m.box((side*.55,.7,length/2+.02),(.3,.17,.05),'white')
        m.box((side*.55,.7,-length/2-.02),(.3,.17,.05),'red')
    return m
vehicle('20_parked_car');vehicle('21_parked_bus',True)
m=new('22_hydrant',400);m.cylinder((0,.35,0),.16,.7,'red',8);m.cylinder((0,.06,0),.25,.12,'dark',8);m.ellipsoid((0,.72,0),(.19,.16,.19),'red',8,4);m.box((0,.48,0),(.6,.15,.18),'red');m.cylinder((0,.86,0),.07,.08,'gold',6)
m=new('23_striped_barrier',600)
for x in [-.7,.7]:m.box((x,.37,0),(.12,.74,.15),'dark');m.box((x,.05,0),(.42,.1,.6),'dark')
for i in range(8):m.box((-.875+i*.25,.8,0),(.25,.48,.16),'orange' if i%2==0 else 'white')
m=new('24_cone_cluster',500)
for x,z in [(-.42,-.22),(.42,-.22),(0,.3)]:
    m.box((x,.04,z),(.48,.08,.48),'dark');m.cylinder((x,.19,z),.19,.22,'orange',8,top=.12);m.cylinder((x,.36,z),.12,.12,'white',8,top=.08);m.cylinder((x,.5,z),.08,.16,'orange',8,top=.025)
m=new('25_crate_stack',800)
for x,y in [(-.4,.4),(.4,.4),(0,1.2)]:
    m.box((x,y,0),(.76,.76,.76),'wood')
    for dx in [-.31,.31]:m.box((x+dx,y,.39),(.09,.76,.06),'gold')
    for dy in [-.31,.31]:m.box((x,y+dy,.39),(.76,.09,.06),'gold')
    m.beam((x-.28,y-.28,.43),(x+.28,y+.28,.43),.045,'gold')
m=new('26_low_fence',600)
for x in [-.8,.8]:m.box((x,.4,0),(.12,.8,.18),'teal')
for y in [.3,.64]:m.box((0,y,0),(1.8,.16,.16),'white')
m=new('27_overhead_sign',800)
for x in [-.95,.95]:m.box((x,1.15,0),(.13,2.3,.22),'dark');m.box((x,.05,0),(.35,.1,.5),'dark')
m.box((0,1.86,0),(1.95,.7,.2),'orange')
for x in [-.7,-.35,0,.35,.7]:m.box((x,1.86,.11),(.15,.55,.02),'white')
m=new('28_star_coin',300);m.cylinder((0,.45,0),.45,.13,'gold',16,axis='z');m.cylinder((0,.45,.075),.36,.025,'yellow',16,axis='z')
star=[(0,.45,.1)]+[(math.sin(i*math.pi/5)*(.28 if i%2==0 else .13),.45+math.cos(i*math.pi/5)*(.28 if i%2==0 else .13),.1) for i in range(10)]
m.mesh(star,[(0,i+1,(i+1)%10+1) for i in range(10)],'white')
m=new('29_slow_clock',800);m.cylinder((0,.48,0),.43,.12,'teal',16,axis='z');m.cylinder((0,.48,.075),.36,.025,'white',16,axis='z');m.beam((0,.48,.1),(0,.74,.1),.022,'dark');m.beam((0,.48,.1),(.18,.4,.1),.022,'dark');m.box((0,.96,0),(.2,.1,.13),'gold')
m=new('30_magnet',800)
for x in [-.25,.25]:m.box((x,.46,0),(.2,.55,.2),'red');m.box((x,.8,0),(.2,.15,.2),'white')
m.box((0,.16,0),(.7,.2,.2),'red')
m=new('31_shield_bubble',500);m.ellipsoid((0,.85,0),(.85,.85,.85),'glass',12,8)
m=new('32_extra_life_heart',500)
outline=[(0,.08),(-.43,.48),(-.43,.69),(-.29,.83),(-.13,.83),(0,.69),(.13,.83),(.29,.83),(.43,.69),(.43,.48)]
v=[(0,.49,z) for z in [-.15,.15]]+[(x,y,z) for z in [-.1,.1] for x,y in outline]
f=[]
for i in range(10):
    j=(i+1)%10;f.extend([(0,2+j,2+i),(1,12+i,12+j),(2+i,2+j,12+j,12+i)])
m.mesh(v,f,'pink')

# Basic geometry from the Unity-built list; text, effects, UI remain Unity work.
m=new('road_segment',100);m.box((0,.05,0),(6,.1,12),'dark')
m=new('sidewalk_segment',100);m.box((0,.12,0),(2,.24,12),'grey');m.box((-.93,.18,0),(.14,.36,12),'white')
m=new('lane_marking',100);m.box((0,.005,0),(.12,.01,1.2),'white')
m=new('answer_board',100);m.box((0,1,0),(1.6,1,.12),'teal');m.box((0,1,.07),(1.44,.84,.03),'glass')
m=new('ground_fill',100);m.box((0,.025,0),(12,.05,12),'green')

def triangles(faces):
    return [(f[0],f[i],f[i+1]) for f in faces for i in range(1,len(f)-1)]

def export(m):
    # Normalize bottom center based on true geometry, including asymmetric props.
    pts=[p for v,_,_ in m.parts for p in v]
    lo=[min(p[i] for p in pts) for i in range(3)];hi=[max(p[i] for p in pts) for i in range(3)]
    offset=[(lo[0]+hi[0])/2,lo[1],(lo[2]+hi[2])/2]
    parts=[([tuple(p[k]-offset[k] for k in range(3)) for p in v],triangles(f),c) for v,f,c in m.parts]
    count=sum(len(f) for _,f,_ in parts)
    assert count<=m.budget,(m.name,count,m.budget)
    obj=[f'# {m.name}; meters, Y up; {count} triangles', 'mtllib palette.mtl'];base=1
    binary=bytearray();views=[];accessors=[];prims=[]
    colors=list(PALETTE)
    def buffer(data,target):
        while len(binary)%4:binary.append(0)
        idx=len(views);views.append(dict(buffer=0,byteOffset=len(binary),byteLength=len(data),target=target));binary.extend(data);return idx
    for idx,(verts,faces,c) in enumerate(parts):
        obj.extend([f'o part_{idx}',f'usemtl {c}','s off'])
        obj.extend('v %.6f %.6f %.6f'%p for p in verts)
        obj.extend('f '+' '.join(str(base+i) for i in f) for f in faces);base+=len(verts)
        # Explode triangles for reliable flat normals in GLB.
        vv=[];nn=[]
        for f in faces:
            a,b,d=[verts[i] for i in f];u=[b[i]-a[i] for i in range(3)];w=[d[i]-a[i] for i in range(3)]
            n=[u[1]*w[2]-u[2]*w[1],u[2]*w[0]-u[0]*w[2],u[0]*w[1]-u[1]*w[0]];length=math.sqrt(sum(x*x for x in n)) or 1
            for p in (a,b,d):vv.extend(p);nn.extend(x/length for x in n)
        av=len(accessors);view=buffer(struct.pack('<%sf'%len(vv),*vv),34962)
        accessors.append(dict(bufferView=view,componentType=5126,count=len(vv)//3,type='VEC3',min=[min(vv[k::3]) for k in range(3)],max=[max(vv[k::3]) for k in range(3)]))
        view=buffer(struct.pack('<%sf'%len(nn),*nn),34962);accessors.append(dict(bufferView=view,componentType=5126,count=len(nn)//3,type='VEC3'))
        prims.append(dict(attributes={'POSITION':av,'NORMAL':av+1},material=colors.index(c)))
    (OUT/(m.name+'.obj')).write_text('\n'.join(obj)+'\n')
    materials=[]
    for c in colors:
        h=PALETTE[c].lstrip('#');rgb=[int(h[i:i+2],16)/255 for i in (0,2,4)]
        alpha=.24 if m.name=='31_shield_bubble' and c=='glass' else 1
        materials.append(dict(name=c,pbrMetallicRoughness=dict(baseColorFactor=rgb+[alpha],metallicFactor=.35 if c=='gold' else 0,roughnessFactor=.8),**({'alphaMode':'BLEND','doubleSided':True} if alpha<1 else {})))
    gltf=dict(asset={'version':'2.0','generator':'Math Runner starter mesh generator'},scene=0,scenes=[{'nodes':[0]}],nodes=[{'mesh':0,'name':m.name}],meshes=[{'primitives':prims}],materials=materials,buffers=[{'byteLength':len(binary)}],bufferViews=views,accessors=accessors)
    js=json.dumps(gltf,separators=(',',':')).encode();js+=b' '*((-len(js))%4);binary+=b'\0'*((-len(binary))%4)
    glb=struct.pack('<III',0x46546C67,2,12+8+len(js)+8+len(binary))+struct.pack('<II',len(js),0x4E4F534A)+js+struct.pack('<II',len(binary),0x004E4942)+binary
    (OUT/(m.name+'.glb')).write_bytes(glb)
    return dict(name=m.name,triangles=count,budget=m.budget,size_m=[round(hi[i]-lo[i],3) for i in range(3)])

def preview():
    from PIL import Image,ImageDraw,ImageFont
    width=1400;cellw=280;cellh=260
    img=Image.new('RGB',(width,80+math.ceil(len(models)/5)*cellh),'#EAF0F4');draw=ImageDraw.Draw(img)
    font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',14);title=ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',24)
    draw.text((24,18),'MATH RUNNER / STARTER MODEL SET',fill='#263B50',font=title)
    draw.text((24,50),'37 low-poly assets | flat colors | meters | Y up | bottom-centered pivots',fill='#526778',font=font)
    for idx,m in enumerate(models):
        ox=(idx%5)*cellw;oy=80+(idx//5)*cellh
        draw.rounded_rectangle((ox+6,oy+6,ox+cellw-6,oy+cellh-6),12,fill='white')
        projected=[]
        for verts,faces,c in m.parts:
            for f in triangles(faces):
                pts=[verts[i] for i in f]
                screen=[(.866*p[0]-.5*p[2],.25*p[0]+.433*p[2]-.866*p[1],.433*p[0]+.75*p[2]+.5*p[1]) for p in pts]
                depth=sum(.433*p[0]+.75*p[2]+.5*p[1] for p in pts)/3
                a,b,d=pts;u=[b[i]-a[i] for i in range(3)];v=[d[i]-a[i] for i in range(3)]
                n=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]];length=math.sqrt(sum(x*x for x in n)) or 1
                light=.68+.32*max(0,sum(n[i]*[-.3,.8,.52][i] for i in range(3))/length)
                h=PALETTE[c].lstrip('#');rgb=tuple(int(int(h[i:i+2],16)*light) for i in (0,2,4))
                projected.append((depth,screen,rgb))
        allpts=[p for _,ps,_ in projected for p in ps];xs=[p[0] for p in allpts];ys=[p[1] for p in allpts]
        scale=min(220/(max(xs)-min(xs)),190/(max(ys)-min(ys)))
        cx=(min(xs)+max(xs))/2;cy=(min(ys)+max(ys))/2
        # Depth buffer avoids painter-order artifacts on windows and roof details.
        pixels=img.load();depths={}
        for _,ps,c in projected:
            a,b,d=[(140+(x-cx)*scale,110+(y-cy)*scale,z) for x,y,z in ps]
            det=(b[1]-d[1])*(a[0]-d[0])+(d[0]-b[0])*(a[1]-d[1])
            if abs(det)<1e-8:continue
            for yy in range(max(10,int(min(a[1],b[1],d[1]))),min(215,int(max(a[1],b[1],d[1]))+1)):
                for xx in range(max(10,int(min(a[0],b[0],d[0]))),min(270,int(max(a[0],b[0],d[0]))+1)):
                    u=((b[1]-d[1])*(xx+.5-d[0])+(d[0]-b[0])*(yy+.5-d[1]))/det
                    v=((d[1]-a[1])*(xx+.5-d[0])+(a[0]-d[0])*(yy+.5-d[1]))/det
                    w=1-u-v
                    if min(u,v,w)<-1e-7:continue
                    z=u*a[2]+v*b[2]+w*d[2];key=(xx,yy)
                    if z>depths.get(key,-1e9):
                        depths[key]=z;pixels[ox+xx,oy+yy]=c
        draw.text((ox+16,oy+222),m.name.replace('_',' '),font=font,fill='#263B50')
    img.save(ROOT/'Tools/starter-models-preview.png')

if __name__=='__main__':
    OUT.mkdir(parents=True,exist_ok=True)
    mtl=[]
    for name,h in PALETTE.items():
        rgb=[int(h[i:i+2],16)/255 for i in (1,3,5)]
        mtl.extend([f'newmtl {name}','Kd '+' '.join(str(v) for v in rgb),'Ka 0 0 0','Ks 0 0 0','Ns 1',''])
    (OUT/'palette.mtl').write_text('\n'.join(mtl))
    report=[export(m) for m in models]
    (OUT/'catalog.json').write_text(json.dumps(report,indent=2)+'\n')
    preview()
    print(f'Generated {len(report)} models, {sum(m["triangles"] for m in report)} triangles total; every mesh within its budget.')
