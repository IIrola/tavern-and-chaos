#!/usr/bin/env python3
"""Session closure with real macOS players; accelerated deadline and declared board fixtures."""
import json,pathlib,subprocess,sys,time
root=pathlib.Path(__file__).resolve().parent
case=next((x for x in sys.argv[1:] if x in ('time-cancel','time-win','insolvency')),'time-cancel')
out=root/'Evidence/effects'/case;out.mkdir(parents=True,exist_ok=True)
for p in list(out.glob('*.json'))+list(out.glob('*.png'))+list(out.glob('*.ready')):p.unlink()
player=root/'Builds/macOS/Tavern and Chaos.app/Contents/MacOS/Tavern and Chaos'
port={'time-cancel':7794,'time-win':7795,'insolvency':7796}[case]
processes=[]
for role in ('host','client','watcher','late'):
 flags=[] if role=='host' and '--visual-host' in sys.argv else ['-batchmode','-nographics']
 command=[str(player),*flags,'-session-end-check','-end-case',case,'-end-role',role,'-end-port',str(port),'-evidencePath',str(out),'-logFile',str(out/(role+'.log'))]
 p=subprocess.Popen(command,stdout=subprocess.DEVNULL,stderr=subprocess.STDOUT);processes.append((role,p));print(role,p.pid,flush=True)
 if role=='host':
  until=time.monotonic()+60
  while time.monotonic()<until and not (out/'host.ready').exists() and p.poll() is None:time.sleep(.2)
 time.sleep(.4)
until=time.monotonic()+130
while time.monotonic()<until and any(p.poll() is None for _,p in processes):time.sleep(.3)
results=[]
for role,p in processes:
 if p.poll() is None:p.terminate()
 try:p.wait(timeout=5)
 except subprocess.TimeoutExpired:p.kill();p.wait()
 path=out/(role+'.json');report=json.loads(path.read_text()) if path.exists() else {'passed':False,'failures':['Missing final report']}
 results.append({**report,'exit':p.returncode})
summary={'passed':all(x['passed'] and x['exit']==0 for x in results),'case':case,'scope':'Three connected macOS processes and a fourth denied entrant on loopback. Accelerated authoritative time boundary. Winning-eight setup uses controlled rule history; final shot/pocket and payment are real. Not a human or multi-machine test.','results':results}
(out/'summary.json').write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps({'passed':summary['passed'],'results':[{'role':x.get('role'),'exit':x['exit'],'checks':len(x.get('checks',[])),'failures':x.get('failures',[]),'errors':x.get('errors',[])} for x in results]},indent=2))
sys.exit(0 if summary['passed'] else 1)
