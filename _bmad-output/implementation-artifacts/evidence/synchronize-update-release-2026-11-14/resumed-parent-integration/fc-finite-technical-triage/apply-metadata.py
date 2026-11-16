"""Write only authorized finite follow-up metadata and problemMutantTriage."""
from pathlib import Path
import collections,hashlib,json
BASE=Path(__file__).resolve().parent;ROOT=BASE.parents[5];OWNER=ROOT/'references/Hexalith.FrontComposer'
DIRECTORY='tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/'
ledger_path=DIRECTORY+'mutation-problem-followups.json';manifest_path=DIRECTORY+'mutation-target-manifest.json'
ledger_bytes=(BASE/'prospective-mutation-problem-followups.json').read_bytes();assert len(ledger_bytes)<1048576
ledger=json.loads(ledger_bytes);groups=collections.defaultdict(list)
for row in ledger['mutations']:
 for segment,statuses in row['allowedRemaining'].items():
  status=row['observed'].get(segment)
  if status is not None:groups[(segment,status,row['action'])].append(row['id'])
  for allowed in statuses:
   if allowed!=status:groups[(segment,allowed,row['action'])].append(row['id'])
triage=[];rows={row['id']:row for row in ledger['mutations']}
for (segment,status,action),identities in sorted(groups.items()):
 identities=sorted(set(identities),key=int)
 original=sum(rows[ident]['observed'].get(segment)==status for ident in identities)
 supplemental=sum('supplementalReview' in rows[ident] for ident in identities)
 transition=len(identities)-original-supplemental
 rationale={'kill-test-added':'Exact literal failures are bound to preserved adopted assertions. This is historical technical test-added evidence; any fresh remaining problem for these IDs fails the independent identity matcher.',
 'equivalent-accepted':'Source-specific bounded functional/non-actionable dispositions only; see every exact expression and scope in the ledger. Hash distribution/external performance are not globally equivalent;14021 is a detected destructive Timeout, not a semantically equivalent mutation.',
 'deferred-with-owner':'Each real/unresolved source gap has an exact identity and finite assigned Story10-4 follow-up. Deferred does not assert unreachable/equivalent behavior or a fresh native result.'}[action]
 triage.append({'segment':segment,'status':status,'count':len(identities),'action':action,'owner':ledger['owner'],
  'rationale':rationale+' Raw80/60 scores, native segment exits, strict BOTH compatibility/provenance and the independent exact identity/status matcher remain mandatory.',
  'countMeaning':'Maximum original-reviewed identities for this status, plus only explicit NoCoverage-to-Survived transitions and separately reviewed supplemental identities; not an observed future count.',
  'originalObservedCount':original,'explicitTransitionMaximum':transition,'supplementalReviewedMaximum':supplemental,'mutationIds':identities,'identityLedger':ledger_path,
  'identityLedgerSha256':hashlib.sha256(ledger_bytes).hexdigest(),'requiredIndependentCheck':'eng/check-mutation-triage.py, mandatory after strict BOTH aggregate validation; unknown IDs/span/replacement/status changes remain blockers',
  'decisionMode':'Automated technical disposition under existing Root human-validation waiver; no personal human review, new census authority or future qualification'})
manifest=json.loads((BASE/'before-mutation-target-manifest.json').read_text());assert manifest['problemMutantTriage']==[]
manifest['problemMutantTriage']=triage
manifest_bytes=(json.dumps(manifest,indent=2)+'\n').replace('\n','\r\n').encode();assert len(manifest_bytes)<1048576
(OWNER/ledger_path).write_bytes(ledger_bytes);(OWNER/manifest_path).write_bytes(manifest_bytes)
(BASE/'after-mutation-target-manifest.json').write_bytes(manifest_bytes)
summary={'manifestRecords':len(triage),'manifestBytes':len(manifest_bytes),'identityLedgerBytes':len(ledger_bytes),'ledgerSha256':hashlib.sha256(ledger_bytes).hexdigest(),
 'originalObservations':sum(len(row['observed']) for row in ledger['mutations']),'maximumCountsBySegmentStatus':{segment:{status:sum(r['count'] for r in triage if r['segment']==segment and r['status']==status) for status in ['Survived','NoCoverage','Timeout']} for segment in ['happy-path','error-handling']},
 'statusTransitionRule':'Original observations permit only explicit NoCoverage-to-Survived unresolved transitions; supplemental12812 permits only its exact Survived/NoCoverage identity. Tested kills admit no remaining problem;14021 remains Timeout.'}
(BASE/'manifest-admission-summary.json').write_text(json.dumps(summary,indent=2)+'\n');print(json.dumps(summary))
