"""Write only authorized finite follow-up metadata and problemMutantTriage."""
from pathlib import Path
import collections,hashlib,json
BASE=Path(__file__).resolve().parent;ROOT=BASE.parents[5];OWNER=ROOT/'references/Hexalith.FrontComposer'
DIRECTORY='tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/'
ledger_path=DIRECTORY+'mutation-problem-followups.json';manifest_path=DIRECTORY+'mutation-target-manifest.json'
ledger_bytes=(BASE/'prospective-mutation-problem-followups.json').read_bytes();assert len(ledger_bytes)<1048576
ledger=json.loads(ledger_bytes);groups=collections.defaultdict(list)
for row in ledger['mutations']:
 for segment,status in row['observed'].items():
  groups[(segment,status,row['action'])].append(row['id'])
  for allowed in row['allowedRemaining'][segment]:
   if allowed!=status:groups[(segment,allowed,row['action'])].append(row['id'])
triage=[];rows={row['id']:row for row in ledger['mutations']}
for (segment,status,action),identities in sorted(groups.items()):
 identities=sorted(set(identities),key=int)
 original=sum(rows[ident]['observed'][segment]==status for ident in identities)
 transition=len(identities)-original
 rationale={'kill-test-added':'Exact literal failures are bound to preserved adopted assertions. This is historical technical test-added evidence; any fresh remaining problem for these IDs fails the independent identity matcher.',
 'equivalent-accepted':'Source-specific bounded functional/non-actionable dispositions only; see every exact expression and scope in the ledger. Hash distribution/external performance are not globally equivalent;14021 is a detected destructive Timeout, not a semantically equivalent mutation.',
 'deferred-with-owner':'Each real/unresolved source gap has an exact identity and finite assigned Story10-4 follow-up. Deferred does not assert unreachable/equivalent behavior or a fresh native result.'}[action]
 triage.append({'segment':segment,'status':status,'count':len(identities),'action':action,'owner':ledger['owner'],
  'rationale':rationale+' Raw80/60 scores, native segment exits, strict BOTH compatibility/provenance and the independent exact identity/status matcher remain mandatory.',
  'countMeaning':'Maximum original-reviewed identities for this status, including only explicitly listed NoCoverage-to-Survived transitions; not an observed future count.',
  'originalObservedCount':original,'explicitTransitionMaximum':transition,'mutationIds':identities,'identityLedger':ledger_path,
  'identityLedgerSha256':hashlib.sha256(ledger_bytes).hexdigest(),'requiredIndependentCheck':'fc-finite-technical-triage/check-fresh-report.py; unknown IDs/span/replacement/status changes remain blockers',
  'decisionMode':'Automated technical disposition under existing Root human-validation waiver; no personal human review, new census authority or future qualification'})
manifest=json.loads((BASE/'before-mutation-target-manifest.json').read_text());assert manifest['problemMutantTriage']==[]
manifest['problemMutantTriage']=triage
manifest_bytes=(json.dumps(manifest,indent=2)+'\n').replace('\n','\r\n').encode();assert len(manifest_bytes)<1048576
(OWNER/ledger_path).write_bytes(ledger_bytes);(OWNER/manifest_path).write_bytes(manifest_bytes)
(BASE/'after-mutation-target-manifest.json').write_bytes(manifest_bytes)
summary={'manifestRecords':len(triage),'manifestBytes':len(manifest_bytes),'identityLedgerBytes':len(ledger_bytes),'ledgerSha256':hashlib.sha256(ledger_bytes).hexdigest(),
 'originalObservations':sum(len(row['observed']) for row in ledger['mutations']),'maximumCountsBySegmentStatus':{segment:{status:sum(r['count'] for r in triage if r['segment']==segment and r['status']==status) for status in ['Survived','NoCoverage','Timeout']} for segment in ['happy-path','error-handling']},
 'statusTransitionRule':'Only explicit NoCoverage-to-Survived unresolved transitions; kill-test-added entries admit no remaining problem and source-inspected Timeout14021 stays Timeout.'}
(BASE/'manifest-admission-summary.json').write_text(json.dumps(summary,indent=2)+'\n');print(json.dumps(summary))
