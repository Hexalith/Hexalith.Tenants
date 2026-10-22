from collections import Counter
from pathlib import Path
import json,re,subprocess
BASELINE='7cfa1c38847f7f4340b1cc11a9137f28430bd6c6'
SELECTED={341,573,581,607,620}
LEDGER='_bmad-output/implementation-artifacts/deferred-work.md'
INDEX='docs/maintenance-backlog.md'
PREFIX='pre-partial-recovery-'
MUTABLE={'status','disposition','verification-date','verification','verification-files','evidence-kind','dependencies','next-action'}
SUMMARY='**DW-341/DW-573/DW-581/DW-607/DW-620 closure (2026-10-22):**'
def require(value,message):
 if not value: raise ValueError(message)
def original(path):return subprocess.check_output(['git','show',BASELINE+':'+path],text=True)
def records(text):
 result={}
 for b in re.split(r'(?=^### DW-)',text,flags=re.M)[1:]:
  n=int(re.match(r'### DW-(\d+)',b)[1]);require(n not in result,'duplicate');result[n]=b.rstrip()
 return result
def field(block,key):
 values=re.findall(r'^'+re.escape(key)+r': (.*)$',block,re.M);require(len(values)==1,'field: '+key);return values[0]
def row_ids(row):return {int(n) for n in re.findall(r'DW-(\d+)',row.split('|')[1])}
def remove_summary(text):
 lines=text.splitlines(keepends=True);positions=[i for i,l in enumerate(lines) if l.startswith(SUMMARY)];require(len(positions)==1,'summary');i=positions[0];require(lines[i+1]=='\n','paragraph');return ''.join(lines[:i]+lines[i+2:])
def reconstruct(block):
 result=[]
 for line in block.splitlines():
  key=line.partition(':')[0]
  if key in MUTABLE:continue
  if key.startswith(PREFIX) and key[len(PREFIX):] in MUTABLE:line=line[len(PREFIX):]
  result.append(line)
 return '\n'.join(result)
before=records(original(LEDGER));text=Path(LEDGER).read_text();after=records(text)
require(before.keys()==after.keys() and len(after)==657,'inventory')
require({n for n in before if before[n]!=after[n]}==SELECTED,'changed IDs')
for n in SELECTED:
 require(field(before[n],'status')=='open','already closed')
 require(field(after[n],'status')=='done 2026-10-22','status')
 require(field(after[n],'disposition')=='done','disposition')
 for key in MUTABLE:field(after[n],key)
 require(reconstruct(after[n])==before[n],'history')
require(remove_summary(text).partition('### DW-')[0]==original(LEDGER).partition('### DW-')[0],'preamble')
index=Path(INDEX).read_text();old_rows=[l for l in original(INDEX).splitlines() if l.startswith('| DW-')]
removed=[r for r in old_rows if row_ids(r)&SELECTED]
require(len(removed)==5 and all(row_ids(r)<=SELECTED for r in removed),'shared row')
rows=[l for l in index.splitlines() if l.startswith('| DW-')]
require(rows==[r for r in old_rows if r not in removed],'unrelated rows')
open_ids={n for n,b in after.items() if field(b,'status')=='open'}
require(Counter(n for r in rows for n in row_ids(r))==Counter({n:1 for n in open_ids}),'open coverage')
counts=Counter('done' if n not in open_ids else field(b,'disposition') for n,b in after.items())
require(counts==dict(done=500,actionable=86,blocked=15,**{'awaiting-decision':23,'accepted-limitation':7,'uncertain':26}),'counts')
expected=''.join(l for l in original(INDEX).splitlines(keepends=True) if l.rstrip('\n') not in removed)
for a,b in [('| done | 495 |','| done | 500 |'),('| actionable | 91 |','| actionable | 86 |'),('**Remaining: 162 open records across 152 index rows.**','**Remaining: 157 open records across 147 index rows.**')]:expected=expected.replace(a,b,1)
require(remove_summary(index)==expected,'index narrative')
require(not subprocess.check_output(['git','diff',BASELINE,'--raw','--','references/'],text=True),'pointers')
for heading in ['### DW-1: duplicate','### DW-01: alias','### DW-1 duplicate']:
 try:records(heading+'\nstatus: open\n### DW-1: original\nstatus: open\n')
 except ValueError:pass
 else:raise ValueError('duplicate control accepted')
for mutation in [after[341]+'\nowner: duplicate',after[341].replace('priority: P2','priority: P1'),after[341].replace('owner: unassigned\n',''),after[341].replace('owner: unassigned','unexpected: value\nowner: unassigned')]:require(reconstruct(mutation)!=before[341],'immutable control accepted')
print(json.dumps(dict(baseline=BASELINE,selected=sorted(SELECTED),records=len(after),done=counts['done'],open=len(open_ids),index_rows=len(rows),dispositions=dict(counts),result='pass'),indent=2))
