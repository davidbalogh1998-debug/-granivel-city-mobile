#!/usr/bin/env python3
"""Granivel City — Budapest offline data pack builder.

Builds an offline cache matching BudapestWorldStreamer's file layout.
No proprietary GTA/Rockstar assets are used.

Providers:
- OpenStreetMap geometry via Overpass API (ODbL; attribution required)
- Mapzen/AWS Terrarium elevation tiles

This script is deliberately resumable: existing files are skipped.
"""

from __future__ import annotations

import argparse
import concurrent.futures as cf
import json
import math
import pathlib
import random
import sys
import time
import urllib.parse
import urllib.request

BBOX = {"south":47.33,"north":47.62,"west":18.83,"east":19.36}
DEFAULT_ZOOM=14
USER_AGENT="GranivelCity-BudapestRP-DataBuilder/0.3"
TERRAIN_URL="https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{z}/{x}/{y}.png"
OVERPASS_ENDPOINTS=["https://overpass-api.de/api/interpreter","https://overpass.kumi.systems/api/interpreter"]

def tile_xy(lat,lon,z):
    lat_rad=math.radians(lat); n=2.0**z
    return int((lon+180.0)/360.0*n), int((1.0-math.asinh(math.tan(lat_rad))/math.pi)/2.0*n)

def tile_bounds(x,y,z):
    n=2.0**z; west=x/n*360.0-180.0; east=(x+1)/n*360.0-180.0
    north=math.degrees(math.atan(math.sinh(math.pi*(1-2*y/n))))
    south=math.degrees(math.atan(math.sinh(math.pi*(1-2*(y+1)/n))))
    return south,west,north,east

def iter_tiles(z):
    x0,y0=tile_xy(BBOX['north'],BBOX['west'],z); x1,y1=tile_xy(BBOX['south'],BBOX['east'],z)
    for y in range(y0,y1+1):
        for x in range(x0,x1+1): yield x,y

def http_get(url,timeout=60,retries=5):
    last=None
    for attempt in range(retries):
        try:
            req=urllib.request.Request(url,headers={'User-Agent':USER_AGENT})
            with urllib.request.urlopen(req,timeout=timeout) as r:return r.read()
        except Exception as exc:
            last=exc
            if attempt+1<retries:time.sleep(min(30,(2**attempt)+random.random()))
    raise RuntimeError(f'GET failed after {retries} attempts: {url}: {last}')

def http_post_form(url,fields,timeout=120,retries=5):
    body=urllib.parse.urlencode(fields).encode('utf-8');last=None
    for attempt in range(retries):
        try:
            req=urllib.request.Request(url,data=body,headers={'User-Agent':USER_AGENT,'Content-Type':'application/x-www-form-urlencoded'},method='POST')
            with urllib.request.urlopen(req,timeout=timeout) as r:return r.read()
        except Exception as exc:
            last=exc
            if attempt+1<retries:time.sleep(min(45,(2**attempt)*2+random.random()))
    raise RuntimeError(f'POST failed after {retries} attempts: {url}: {last}')

def download_terrain(root,z,workers):
    out=root/'terrain'/str(z);out.mkdir(parents=True,exist_ok=True);tiles=list(iter_tiles(z))
    def one(item):
        x,y=item;path=out/f'{x}_{y}.png'
        if path.exists() and path.stat().st_size>100:return 'skip',path
        data=http_get(TERRAIN_URL.format(z=z,x=x,y=y),timeout=45);tmp=path.with_suffix('.part');tmp.write_bytes(data);tmp.replace(path);return 'ok',path
    ok=skip=0
    with cf.ThreadPoolExecutor(max_workers=max(1,workers)) as pool:
        for i,(status,path) in enumerate(pool.map(one,tiles),start=1):
            if status=='ok':ok+=1
            else:skip+=1
            if i%25==0 or i==len(tiles):print(f'[terrain] {i}/{len(tiles)} downloaded={ok} skipped={skip}')
    return ok,skip

def build_overpass_query(s,w,n,e):
    bbox=f'{s:.7f},{w:.7f},{n:.7f},{e:.7f}'
    return ('[out:xml][timeout:90];('+f'way[highway]({bbox});'+f'way[building]({bbox});'+f'way[natural=water]({bbox});'+f'way[waterway]({bbox});'+f'way[leisure=park]({bbox});'+f'way[landuse=grass]({bbox});'+f'way[landuse=forest]({bbox});'+f'node[tourism]({bbox});'+f'node[historic]({bbox});'+f'node[amenity]({bbox});'+');out geom;')

def download_osm(root,z,delay):
    out=root/'osm'/str(z);out.mkdir(parents=True,exist_ok=True);tiles=list(iter_tiles(z));ok=skip=failed=0
    for i,(x,y) in enumerate(tiles,start=1):
        path=out/f'{x}_{y}.xml'
        if path.exists() and path.stat().st_size>150:skip+=1;continue
        s,w,n,e=tile_bounds(x,y,z);query=build_overpass_query(s,w,n,e);success=False;error_text=''
        for endpoint in OVERPASS_ENDPOINTS:
            try:
                data=http_post_form(endpoint,{'data':query},timeout=150,retries=3)
                if b'<osm' not in data:raise RuntimeError('response is not OSM XML')
                tmp=path.with_suffix('.part');tmp.write_bytes(data);tmp.replace(path);success=True;break
            except Exception as exc:error_text=str(exc)
        if success:ok+=1
        else:failed+=1;print(f'[osm] FAILED {x}/{y}: {error_text}',file=sys.stderr)
        if i%10==0 or i==len(tiles):print(f'[osm] {i}/{len(tiles)} downloaded={ok} skipped={skip} failed={failed}')
        time.sleep(max(1.0,delay))
    return ok,skip,failed

def write_manifest(root,z):
    manifest={'product':'Granivel City — Budapest RP','schema':1,'zoom':z,'bounds':BBOX,'tile_count':len(list(iter_tiles(z))),'providers':{'map_geometry':'OpenStreetMap contributors / ODbL','elevation':'Mapzen Terrain Tiles / AWS public dataset'},'attribution':['© OpenStreetMap contributors','Mapzen/AWS elevation tiles'],'layout':{'terrain':f'terrain/{z}/<x>_<y>.png','osm':f'osm/{z}/<x>_<y>.xml'}}
    (root/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--output',default='../Assets/StreamingAssets/BudapestData');ap.add_argument('--zoom',type=int,default=DEFAULT_ZOOM);ap.add_argument('--terrain',action='store_true');ap.add_argument('--osm',action='store_true');ap.add_argument('--all',action='store_true');ap.add_argument('--workers',type=int,default=8);ap.add_argument('--osm-delay',type=float,default=2.0);args=ap.parse_args()
    root=pathlib.Path(args.output).expanduser().resolve();root.mkdir(parents=True,exist_ok=True);write_manifest(root,args.zoom)
    do_terrain=args.terrain or args.all;do_osm=args.osm or args.all
    if not do_terrain and not do_osm:ap.error('choose --terrain, --osm or --all')
    print(f'Output: {root}');print(f'Budapest extent: {BBOX}');print(f'Zoom {args.zoom}: {len(list(iter_tiles(args.zoom)))} tiles')
    if do_terrain:download_terrain(root,args.zoom,args.workers)
    if do_osm:download_osm(root,args.zoom,args.osm_delay)
    print('Done. Keep the OpenStreetMap attribution in the game credits.');return 0

if __name__=='__main__':raise SystemExit(main())
