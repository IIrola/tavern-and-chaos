#!/usr/bin/env python3
"""Real Unity players on loopback. Position, depleted wallets and accelerated deadline are declared fixtures."""
import argparse
import json
import pathlib
import subprocess
import sys
import time

parser = argparse.ArgumentParser()
parser.add_argument('--visual-host', action='store_true')
args = parser.parse_args()
root = pathlib.Path(__file__).resolve().parent
out = root / 'Evidence/effects/actors'
out.mkdir(parents=True, exist_ok=True)
for path in [*out.glob('*.json'), *out.glob('*.png'), *out.glob('*.ready')]:
    path.unlink()
player = root / 'Builds/macOS/Tavern and Chaos.app/Contents/MacOS/Tavern and Chaos'
port = 7801
processes = []
for role in ['host', 'client']:
    flags = [] if role == 'host' and args.visual_host else ['-batchmode', '-nographics']
    command = [str(player), *flags, '-cocktail-effects-check', '-effects-role', role,
               '-effects-port', str(port), '-evidencePath', str(out), '-logFile', str(out / (role + '.log'))]
    process = subprocess.Popen(command, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
    processes.append((role, process))
    print(role, process.pid, flush=True)
    if role == 'host':
        end = time.monotonic() + 60
        while time.monotonic() < end and not (out / 'host.ready').exists() and process.poll() is None:
            time.sleep(.2)
    time.sleep(.4)
end = time.monotonic() + 100
while time.monotonic() < end and any(p.poll() is None for _, p in processes):
    time.sleep(.3)
results = []
for role, process in processes:
    if process.poll() is None:
        process.terminate()
    try:
        process.wait(timeout=5)
    except subprocess.TimeoutExpired:
        process.kill()
        process.wait()
    path = out / (role + '.json')
    report = json.loads(path.read_text()) if path.exists() else {'passed': False, 'failures': ['Missing report']}
    results.append({**report, 'role': role, 'exit': process.returncode})
summary = {'passed': all(r['passed'] and r['exit'] == 0 for r in results), 'case': 'actor-effects',
           'scope': 'Two independent macOS players on loopback. Real capsule/ragdoll physics, effect timers, seats and effect snapshots. Recipe application and actor/camera positioning are explicit fixtures. This does not test the cocktail preparation or serving UI.', 'results': results}
(out / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n')
print(json.dumps({'passed': summary['passed'], 'results': [
    {'role': r['role'], 'exit': r['exit'], 'checks': len(r.get('checks', [])),
     'failures': r.get('failures', []), 'errors': r.get('errors', [])} for r in results]}, indent=2))
sys.exit(0 if summary['passed'] else 1)
