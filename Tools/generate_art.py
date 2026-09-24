from PIL import Image, ImageDraw
from pathlib import Path

root=Path(__file__).resolve().parents[1]/'Assets'/'Umbra'/'Resources'/'Umbra'
root.mkdir(parents=True,exist_ok=True)
def ranger(step=0):
    im=Image.new('RGBA',(40,52));d=ImageDraw.Draw(im)
    outline='#202d25';cloak='#344d36';green='#567049';light='#87965b';gold='#d6af63';skin='#dca574';brown='#543c2c'
    def rect(box,c):d.rectangle(box,fill=c)
    # Readable silhouette, hood, cloak, bracers, quiver and bow.
    rect((26,18,30,36),outline);rect((27,19,29,34),brown)
    for x in (26,29,31):rect((x,9,x,23),'#b19e70');rect((x-1,9,x+1,12),'#e1d5a0')
    d.polygon([(11,23),(25,23),(30,43),(7,43)],fill=outline)
    d.polygon([(12,24),(24,24),(27,40),(9,40)],fill=cloak)
    rect((11,29,14,39),green);rect((23,27,25,39),'#263f32')
    rect((13,39+step,17,48+step),outline);rect((21,39-step,25,48-step),outline)
    rect((13,40+step,17,45+step),brown);rect((21,40-step,25,45-step),brown)
    rect((11,47+step,17,49+step),outline);rect((21,47-step,27,49-step),outline)
    rect((12,25,25,36),green);rect((15,25,22,34),light)
    rect((12,35,26,38),brown);rect((18,35,21,38),gold);rect((19,36,20,37),brown)
    rect((7,27,11,35),outline);rect((8,28,11,33),green);rect((8,34,11,37),skin)
    rect((26,27,30,35),outline);rect((26,28,29,32),green);rect((27,33,30,36),skin)
    d.ellipse((10,7,28,26),fill=outline);d.ellipse((12,8,27,24),fill=green)
    rect((13,15,25,23),skin);rect((14,23,24,25),'#bd815c')
    rect((12,13,27,16),brown);rect((13,16,15,20),brown)
    rect((16,17,18,19),outline);rect((23,17,25,19),outline)
    rect((17,17,17,17),'#fff0ca');rect((24,17,24,17),'#fff0ca')
    rect((20,22,22,22),'#805142')
    d.polygon([(8,13),(13,5),(23,3),(29,13)],fill=outline)
    d.polygon([(11,12),(15,6),(22,5),(26,12)],fill=green)
    rect((8,12,29,14),outline);rect((10,12,28,12),gold)
    d.line((23,9,28,2),fill='#e0c983',width=2);rect((27,2,29,5),'#f0dca4')
    d.line([(32,22),(36,26),(37,33),(35,40),(32,43)],fill=outline,width=3)
    d.line([(32,22),(35,27),(36,33),(34,40),(32,43)],fill=gold,width=1)
    d.line((32,22,32,43),fill='#e8d5a2',width=1)
    return im
for name,s in [('ranger_idle',0),('ranger_step1',1),('ranger_step2',-1)]:ranger(s).save(root/(name+'.png'))
im=Image.new('RGBA',(32,34));d=ImageDraw.Draw(im)
d.ellipse((5,14,27,31),fill='#293e24');d.ellipse((6,14,26,28),fill='#9ba746');d.ellipse((8,14,24,25),fill='#d9cc65')
d.rectangle((8,29,12,32),fill='#41502a');d.rectangle((21,29,25,32),fill='#41502a')
d.rectangle((11,20,13,23),fill='#283b2d');d.rectangle((21,20,23,23),fill='#283b2d')
d.point((12,20),fill='#fff4b7');d.point((22,20),fill='#fff4b7')
d.rectangle((16,25,19,25),fill='#7c6034');d.rectangle((8,24,11,25),fill='#d9985b');d.rectangle((23,24,25,25),fill='#d9985b')
d.line((16,16,17,7),fill='#4e5d2c',width=2)
d.polygon([(17,10),(9,10),(5,4),(12,3),(17,7)],fill='#3f6536');d.line((7,5,16,9),fill='#a5b961')
d.polygon([(17,7),(21,2),(29,3),(25,9),(18,10)],fill='#6c8c3d');d.line((18,8,26,4),fill='#c0c975')
im.save(root/'sprout.png')
im=Image.new('RGBA',(34,36));d=ImageDraw.Draw(im)
d.rectangle((11,18,24,31),fill='#394333');d.rectangle((12,20,23,30),fill='#d5c88f')
d.rectangle((10,31,14,33),fill='#564b32');d.rectangle((21,31,25,33),fill='#564b32')
d.rectangle((14,23,16,25),fill='#29352b');d.rectangle((21,23,23,25),fill='#29352b')
d.rectangle((17,28,20,28),fill='#8c684b')
d.polygon([(2,19),(4,11),(11,5),(22,4),(29,10),(32,19),(27,22),(8,22)],fill='#383b2b')
d.polygon([(4,18),(6,11),(12,7),(22,6),(28,11),(30,18),(25,20),(9,20)],fill='#b66535')
d.polygon([(6,13),(12,7),(22,6),(27,10),(27,13)],fill='#d48c43')
for box in [(9,10,13,13),(20,8,23,11),(22,16,27,18),(6,17,10,19),(15,16,17,18)]:d.rectangle(box,fill='#e9ce86')
im.save(root/'mushroom.png')
print('Generated five original pixel sprites.')
import random, math
random.seed(24816)
im=Image.new('RGB',(512,512));px=im.load()
for y in range(512):
    for x in range(512):
        wave=math.sin(x*.033)*math.cos(y*.041)*7+math.sin((x+y)*.012)*5
        noise=random.uniform(-9,9)+wave
        px[x,y]=(int(59+noise),int(72+noise),int(37+noise*.65))
d=ImageDraw.Draw(im)
for _ in range(8500):
    x=random.randrange(512);y=random.randrange(512);c=random.choice(['#4f582d','#646039','#737043','#3f4e2d','#68633a','#536131'])
    d.line((x,y,x+random.randrange(-2,3),y-random.randrange(1,5)),fill=c)
for _ in range(700):
    x=random.randrange(512);y=random.randrange(512)
    d.rectangle((x,y,x+1,y+1),fill=random.choice(['#9b8045','#ac8f4e','#79673b']))
im.save(root/'ground.png')
