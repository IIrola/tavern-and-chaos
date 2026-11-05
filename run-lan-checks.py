#!/usr/bin/env python3
"""Runs the actual macOS player as host + five clients and a rejected seventh client."""
import json, pathlib, subprocess, time, sys
ROOT=pathlib.Path(__file__).resolve().parent
PLAYER=ROOT/'Builds/macOS/Tavern and Chaos.app/Contents/MacOS/Tavern and Chaos'
OUT=ROOT/'Evidence/lan-foundation'; OUT.mkdir(parents=True,exist_ok=True)
for report in OUT.glob('*.json'): report.unlink()
ready=OUT/'all-six.ready'
if ready.exists(): ready.unlink()
processes=[]
def start(role,index=0):
    command=[str(PLAYER)]+([] if role=='host' and '--visual-host' in sys.argv else ['-batchmode','-nographics'])+['-lan-check','-lan-role',role,'-lan-index',str(index),'-lan-port','7789','-evidencePath',str(OUT),'-logFile',str(OUT/f'{role}{index}.log')]
    p=subprocess.Popen(command,stdout=subprocess.DEVNULL,stderr=subprocess.STDOUT); processes.append((role+str(index),p)); return p
host=start('host'); time.sleep(1)
for i in range(1,6): start('client',i); time.sleep(.25)
end=time.monotonic()+32
while time.monotonic()<end and not ready.exists() and host.poll() is None: time.sleep(.2)
if ready.exists(): start('overflow',7)
end=time.monotonic()+65
while time.monotonic()<end and any(p.poll() is None for _,p in processes): time.sleep(.5)
results=[]
for name,p in processes:
    if p.poll() is None: p.terminate()
    try: p.wait(timeout=5)
    except subprocess.TimeoutExpired: p.kill(); p.wait()
    path=OUT/f'{name}.json'
    report=json.loads(path.read_text()) if path.exists() else {'passed':False,'failures':['No final report']}
    results.append({'process':name,'exit':p.returncode,'passed':report.get('passed',False),'checks':len(report.get('checks',[])),'failures':report.get('failures',[]),'runtimeErrors':report.get('runtimeErrors',[])})
summary={'passed':len(results)==7 and all(r['passed'] and r['exit']==0 for r in results),'scope':'Actual separate macOS players on one machine, loopback UDP through NGO/UTP; not a Windows or multi-machine LAN certification','results':results}
(OUT/'summary.json').write_text(json.dumps(summary,indent=2)+'\n'); print(json.dumps(summary,indent=2)); sys.exit(0 if summary['passed'] else 1)
