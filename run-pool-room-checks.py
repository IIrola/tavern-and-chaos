#!/usr/bin/env python3
"""Three real macOS players. Controlled layouts isolate rule/physics/seat integration."""
import json, pathlib, subprocess, sys, time
root=pathlib.Path(__file__).resolve().parent
first=1 if '--first-side-1' in sys.argv else 0
out=root/('Evidence/pool-lan/processes-side1' if first else 'Evidence/pool-lan/processes'); out.mkdir(parents=True,exist_ok=True)
for path in list(out.glob('*.json'))+list(out.glob('*.ready'))+list(out.glob('*.png')): path.unlink()
player=root/'Builds/macOS/Tavern and Chaos.app/Contents/MacOS/Tavern and Chaos'
processes=[]
for role in ('host','client','spectator'):
    flags=[] if role=='host' and '--visual-host' in sys.argv else ['-batchmode','-nographics']
    command=[str(player),*flags,'-pool-room-check','-pool-role',role,'-pool-first',str(first),'-pool-port',str(7792+first),'-evidencePath',str(out),'-logFile',str(out/(role+'.log'))]
    p=subprocess.Popen(command,stdout=subprocess.DEVNULL,stderr=subprocess.STDOUT)
    processes.append((role,p)); print(f'{role}: {p.pid}',flush=True)
    if role=='host':
        ready_end=time.monotonic()+60
        while time.monotonic()<ready_end and not (out/'host.ready').exists() and p.poll() is None: time.sleep(.2)
    time.sleep(.5)
end=time.monotonic()+110
while time.monotonic()<end and any(p.poll() is None for _,p in processes): time.sleep(.5)
results=[]
for role,p in processes:
    if p.poll() is None: p.terminate()
    try: p.wait(timeout=5)
    except subprocess.TimeoutExpired: p.kill(); p.wait()
    path=out/(role+'.json'); report=json.loads(path.read_text()) if path.exists() else {'passed':False,'failures':['No final report']}
    results.append({**report,'role':role,'exit':p.returncode})
summary={'passed':all(r['passed'] and r['exit']==0 for r in results),'scope':'Three independent macOS processes on loopback. Physical shots with controlled server arrangements; no mocked shot outcomes. Not a multi-machine or human acceptance test.','results':results}
(out/'summary.json').write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps({'passed':summary['passed'],'results':[{'role':r['role'],'exit':r['exit'],'checks':len(r.get('checks',[])),'failures':r.get('failures',[]),'errors':r.get('errors',[])} for r in results]},indent=2))
sys.exit(0 if summary['passed'] else 1)
