"""Read-only progress extraction for the diagnostic console; never a final score."""
from pathlib import Path
import json,re
base=Path(__file__).resolve().parent/'attempt-01'
receipt=json.loads((base/'execution.json').read_text())
log=base/'complete-error-segment.log'
lines=log.read_text(errors='replace').splitlines() if log.exists() else []
completed={}
for line in lines:
    found=re.search(r'Test run for (\d+): (.*?) is (success|failed)\s*$',line)
    if found:completed[found[1]]=found[3]
resources=[json.loads(line.split(' ',1)[1]) for line in lines[-160:] if line.startswith('mutation-diagnostics ') and '"event": "resources"' in line]
result={'source':receipt['source'],'last_completed_phase':receipt['commands'][-1]['name'] if receipt['commands'] else None,'console_bytes':log.stat().st_size if log.exists() else 0,'completed_ids':len(completed),'observed_success':sum(status=='success' for status in completed.values()),'observed_failed':sum(status=='failed' for status in completed.values()),'latest':next((line for line in reversed(lines) if 'Test run for' in line),None),'segment_exit':receipt.get('error_segment_exit'),'validator_exit':receipt.get('both_segment_validator_exit'),'blocker':receipt.get('blocker')}
if resources:result['available_gib']=round(resources[-1]['memory_kib']['MemAvailable']/1024/1024,1)
print(json.dumps(result))
