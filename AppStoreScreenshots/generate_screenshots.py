#!/usr/bin/env python3
from PIL import Image, ImageOps
import os, sys, zipfile

ZIP='screenshots_mobile_375x812.zip'
TMP='/tmp/scr_test'
OUT='AppStoreScreenshots'

os.makedirs(TMP, exist_ok=True)
if not os.listdir(TMP):
    if os.path.exists(ZIP):
        with zipfile.ZipFile(ZIP) as z:
            z.extractall(TMP)
        print('Extracted', ZIP, 'to', TMP)
    else:
        print('ZIP not found:', ZIP)
        sys.exit(1)

sources={
 'home':'logged_home.png',
 'medications':'index-elderly-ui.html.png',
 'mood':'index-elderly-ui-v2.html.png',
 'emergency':'signup.png',
 'family':'family_dashboard.png'
}

devices={
 'iPhone_6_7':(1284,2778),
 'iPhone_6_5':(1242,2688),
 'iPhone_5_5':(1242,2208),
 'iPad_12_9':(2048,2732),
 'iPad_11_0':(1668,2388)
}

os.makedirs(OUT, exist_ok=True)

def fit_image(img, target):
    tw,th=target
    iw,ih=img.size
    img_ratio=iw/ih
    tgt_ratio=tw/th
    if img_ratio>tgt_ratio:
        # image wider -> fit height
        new_h=th
        new_w=int(round(new_h*img_ratio))
    else:
        new_w=tw
        new_h=int(round(new_w/img_ratio))
    im2=img.resize((new_w,new_h), Image.LANCZOS)
    left=(new_w-tw)//2
    top=(new_h-th)//2
    return im2.crop((left,top,left+tw,top+th))

wrote=[]
for dev, size in devices.items():
    dev_dir=os.path.join(OUT, dev)
    os.makedirs(dev_dir, exist_ok=True)
    for idx, (role, fname) in enumerate(sources.items(), start=1):
        src_path=os.path.join(TMP, fname)
        if not os.path.exists(src_path):
            alt=fname.replace('-', '_')
            src_path=os.path.join(TMP, alt)
        if not os.path.exists(src_path):
            print('MISSING source for', role, fname)
            continue
        try:
            im=Image.open(src_path).convert('RGB')
        except Exception as e:
            print('ERROR opening', src_path, e)
            continue
        out=fit_image(im, size)
        out_name=f"{idx:02d}_{role}_{dev}.png"
        out_path=os.path.join(dev_dir, out_name)
        out.save(out_path, format='PNG', optimize=True)
        wrote.append(out_path)
        print('WROTE', out_path)

print('\nSummary:')
for dev in devices:
    print(dev, len(os.listdir(os.path.join(OUT,dev))))

print('\nDone.')
