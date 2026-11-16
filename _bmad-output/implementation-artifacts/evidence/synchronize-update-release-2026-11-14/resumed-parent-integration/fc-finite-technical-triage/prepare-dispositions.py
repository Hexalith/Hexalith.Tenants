"""Finite automated technical dispositions authorized by the existing Root waiver; never a score/authority receipt."""
from pathlib import Path
import collections,hashlib,json,re,xml.etree.ElementTree as ET
BASE=Path(__file__).resolve().parent;EVIDENCE=BASE.parent;ROOT=BASE.parents[5];OWNER=ROOT/'references/Hexalith.FrontComposer'
REL='tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/'
ROLE='Story10-4 SourceTools quality/release-automation role'
sha=lambda b:hashlib.sha256(b).hexdigest()
v=json.loads((BASE/'verified-identities-and-controls.json').read_text())
groups=json.loads((EVIDENCE/'fc-error-followthrough-analysis/cause-groups.json').read_text())['groups'];group_index={g['id']:g for g in groups}
hashaudit=json.loads((EVIDENCE/'fc-error-followthrough-analysis/hash-consumer-audit.json').read_text());hash_members={m['member']:m for m in hashaudit['hash_members']}
assert len(v['mutations'])==828 and len(v['preservedAssertionKillCandidates'])==95
entries=[];details=[];followups={};historical=[]

def compact_location(location):return [location['start']['line'],location['start']['column'],location['end']['line'],location['end']['column']]
def identity(row):
 return {'id':row['id'],'path':row['path'],'sourceSha256':v['sourceFiles'][row['path']]['sha256'],'location':row['location'],'original':row['original'],'replacement':row['replacement'],'mutator':row['mutator']}
def followup(key,kind,scope,check,ident):
 if key not in followups:followups[key]={'owner':ROLE,'kind':kind,'scope':scope,'remainingCheck':check,'ids':[],'state':'open-technical-follow-up; no invented personal approval'}
 followups[key]['ids'].append(ident)

for row in sorted(v['mutations'],key=lambda r:int(r['id'])):
 ident=row['id'];initial=row['initialReview'] or {};control=v['literalControls'].get(ident)
 old_private=[]
 for original in initial.get('private_controls',[]):
  path=EVIDENCE/original['trx'];assert path.is_file() and sha(path.read_bytes())==original['sha256']
  doc=ET.parse(path);failed=[node.attrib['testName'] for node in doc.iter() if node.tag.rsplit('}',1)[-1]=='UnitTestResult' and node.attrib.get('outcome')=='Failed']
  assert all(test in failed for test in original['failed_tests'])
  old_private.append({'trx':original['trx'],'sha256':original['sha256'],'failedTests':failed,'boundary':'Historical literal failure only; current preserved assertion must be separately established before kill-test-added'})
 if old_private:historical.append({'id':ident,'controls':old_private})
 is_hash=initial.get('proposed_disposition')=='unresolved-hash-consumer-and-distribution-review'
 ticket=None
 if control and control['supportsKillTestAdded']:
  action='kill-test-added';kind='tested-expression-with-preserved-current-assertion'
  rationale=f"Literal {ident} changes the exact recorded span. Its source-bound control failed, and the current class/helper bytes or original Shouldly assertions remain preserved. This records the added regression, not a fresh native Killed status."
  if control['kind']=='finite-internal-field-participation-literal':
   rationale+=' The selected internal collection check allows collisions; it refuses complete sampled field-participation collapse, not unequal-hash/numeric uniqueness.'
 elif is_hash and row['mutator']=='Arithmetic mutation':
  action='equivalent-accepted';kind='non-actionable-for-bounded-equal-value-hash-congruence'
  member=hash_members[row['member']]
  assert 'unchecked' in member['hash_source']
  assert len(row['original'])==len(row['replacement'])
  differences=[(i,a,b) for i,(a,b) in enumerate(zip(row['original'],row['replacement'])) if a!=b]
  assert len(differences)==1 and differences[0][1:] in [('+','-'),('*','/')],ident
  position,before_operator,after_operator=differences[0]
  if before_operator=='*':
   assert re.match(r'\s*31\b',row['original'][position+1:]),ident
   formula='Division uses fixed nonzero31; equal left operands (including a fixed seed or accumulated subexpression) still yield equal results. Numeric values and collision distribution change.'
  else:formula='Unchecked subtraction replaces unchecked addition of the same field/nested hash; equal operands still produce equal results. Numeric values and collision distribution change.'
  rationale=f"{row['member']}: {row['context']} Exact {row['original']} -> {row['replacement']}. {formula} Accepted only for stable equal-value/hash congruence under the existing nested-hash laws; external/performance/collection-quality equivalence is not asserted."
  ticket='HASH-'+row['member'].replace('.GetHashCode','').replace('.','-')
  followup(ticket,'bounded-hash-distribution-unknown',row['member'],
   'Assess these exact arithmetic variants against the selected112-field internal collection regression and real/implicit comparer consumers. Preserve legal-value and equal-copy laws; investigate observed bucket collapse if any. No exact numeric hash, all-unequal uniqueness, global performance budget or current native outcome is prescribed.',ident)
 elif initial.get('proposed_disposition')=='bounded-equivalence-candidate-for-parent-review':
  action='equivalent-accepted';kind='bounded-source-output-equivalence'
  rationale=initial['evidence_and_rationale']+' Automated acceptance is limited to that exact unchanged source/caller/value boundary; no global allocation/timing/external-provider equivalence is claimed.'
 elif ident=='14021':
  action='equivalent-accepted';kind='non-actionable-detected-destructive-nonprogress'
  rationale='The removed body contains parts.Pop, the sole progress step in while(parts.Count > 0). With a nonempty stack the literal loops forever. Both complete original JSONs record Timeout. The policy action denotes a detected/non-actionable mutation outcome, not semantic equivalence or an unhandled product failure; retain the raw Timeout and unchanged deadline.'
 else:
  action='deferred-with-owner';kind='finite-unresolved-source-contract-or-coverage-gap'
  ticket='FOLLOWUP-'+row['cause']
  if is_hash:
   if ident=='13541':
    effect='Reversing the array-loop bound skips all item contributions and returns a constant array hash. Functional congruence alone does not resolve the concrete collection-quality/performance risk.'
    check='Use legal default/empty and distinct nonempty public array-wrapper values plus actual generic collections/nested DTO consumers; retain equality/hash congruence and assess field/sequence participation without requiring all unequal hashes to differ. The current112-field scalar test does not establish this array case.'
   else:
    effect=f"The exact contribution {row['original']} is replaced by0. This can discard {row['member']} field participation while preserving equal-copy congruence; collision-sensitive behavior remains unresolved."
    check='Exercise the exact removed field contribution through legal independently constructed DTO values and real generic collection consumers. Use the selected internal participation test where applicable; record an actual literal/native result before claiming a kill. No exact numeric hashes or global unequal-hash uniqueness.'
  elif initial.get('proposed_disposition')=='unresolved-public-default-and-caller-boundary':
   effect='This public constructor/default-overload value can reach external or generic model comparison consumers; no matching preserved failing assertion is established for this exact remaining variant.'
   check='Compare the documented omitted/default overload with an explicit equivalent value using the real public generic comparer/consumer, including generated behavior when reachable. Establish the baseline and exact literal before deciding non-actionability or adding a regression.'
  else:
   effect=initial.get('contract_effect') or 'The exact public getter/transform literal can affect emitted role, badge, label or diagnostic metadata; reachability and observable effect require a finite baseline/control.'
   if 'GetHashCode' in row['member']:check='Investigate the exact equality/hash boundary with legal consumer inputs, retaining congruence versus distribution distinctions and no numeric/uniqueness pin.'
   elif 'Equals' in row['member'] or row['member'].endswith('op_Equality') or row['member'].endswith('op_Inequality'):
    check='Use independently constructed equal/unequal public models and actual EqualityComparer/HashSet operations. Establish every relevant identity field and default/null boundary; do not infer private hash distribution from equality alone.'
   elif any(s in row['member'] for s in ['Parse','Validate','Format','Append']):
    check='Reach this exact branch using its supported valid source or legitimate malformed-editing/provider boundary. Assert actual IR/generated output and diagnostic identity/reason/location, then repair and restore the damaged source on the same driver while preserving a healthy sibling. Unreachable/unsupported inputs must be documented rather than invented.'
   else:
    check='Trace this exact literal through its current caller/getter and assert the existing observable IR/generated output/diagnostic contract. Include an adverse-to-valid-to-adverse recovery where supported. Establish baseline/literal evidence; leave unsupported/unreachable cases explicit.'
  result=control['result'] if control else 'no current-assertion-bound literal census result'
  rationale=f"{row['member']}: {effect} Existing finite control: {result}. Source inspection does not establish equivalence or a current kill for this expression."
  followup(ticket,kind,row['member'],check,ident)
 allowed={}
 for segment,observation in row['observations'].items():
  original_status=observation['status']
  if action=='kill-test-added':statuses=[]
  elif original_status=='NoCoverage':statuses=['NoCoverage','Survived']
  else:statuses=[original_status]
  allowed[segment]=statuses
 record={'id':ident,'path':row['path'],'span':compact_location(row['location']),'original':row['original'],'replacement':row['replacement'],
  'mutator':row['mutator'],'member':row['member'],'cause':row['cause'],'observed':{segment:obs['status'] for segment,obs in row['observations'].items()},
  'allowedRemaining':allowed,'action':action,'kind':kind,'rationale':rationale,'followup':ticket,'identitySha256':sha(json.dumps(identity(row),sort_keys=True,separators=(',',':')).encode())}
 if control:record['control']={'result':control['result'],'evidenceId':ident,'currentAssertionBound':control['supportsKillTestAdded']}
 entries.append(record)
 details.append({'id':ident,'identity':identity(row),'observations':row['observations'],'context':row['context'],'disposition':record,'control':control,'historicalControls':old_private,'sourceCallers':initial.get('direct_source_callers',[]),'originalCaseExamples':initial.get('covered_case_examples',[])})

# Separately authorized supplemental12812: actual current94-case survivor, not an invented original/future native status.
parent_path=EVIDENCE/'fc-12812-current-control-attempt3/verification.json'
parent=json.loads(parent_path.read_text());literal=parent['literal'];assert literal['id']=='12812'
assert parent['originalLocalStatus']=='Killed' and parent['ownerCsharpOverlayUnchanged'] and parent['wrapperGone']
for path,value in parent['overlay'].items():assert sha((OWNER/path).read_bytes())==value
for key,directory in [('baseline','baseline-results'),('literal12812','literal12812-results')]:
 proof_path=parent_path.parent/directory/'tests.trx'
 assert proof_path.is_file() and sha(proof_path.read_bytes())==parent[key]['sha256']
 assert parent[key]['exit']==0 and parent[key]['counters'][0]['total']=='94' and parent[key]['counters'][0]['failed']=='0'
assert v['sourceFiles'][literal['path']]['sha256']==literal['sourceSha256']
source=(OWNER/literal['path']).read_text()
source_lines=source.splitlines(keepends=True);start,end=literal['location']['start'],literal['location']['end']
a=sum(map(len,source_lines[:start['line']-1]))+start['column']-1;b=sum(map(len,source_lines[:end['line']-1]))+end['column']-1
assert source[a:b]==literal['original']
raw_happy=json.loads((EVIDENCE/'fc-happy-local-diagnostic/attempt-03/post-validator-artifacts/happy-path/reports/source-tools-happy-path.json').read_text())
raw_mutant=next(m for file in raw_happy['files'].values() for m in file['mutants'] if str(m['id'])=='12812')
assert raw_mutant['status']=='Killed' and raw_mutant['replacement']==literal['replacement'] and raw_mutant['location']==literal['location']
extra={'id':'12812','path':literal['path'],'location':literal['location'],'original':literal['original'],'replacement':literal['replacement'],'mutator':raw_mutant['mutatorName']}
extra_identity=identity(extra)
ticket='FOLLOWUP-12812-override-chain'
followup(ticket,'supplemental-current-literal-survivor','CommandParser.IsDerivableProperty',
 'Establish actual supported override/shadow/base-property-chain cases reaching the removed OverriddenProperty choice, including matching DerivedFrom metadata. Compare baseline IR/generated assignment and repair/reversal behavior. The historical killedBy fixture declares no inheritance beyond object and marks all relevant fields DerivedFrom, so neither that attribution nor94new-case survival proves a broad override-chain defect or equivalence. Preserve localKilled/hostedrawSuccess uncertainty; fresh native outcomes remain to be measured.', '12812')
extra_record={'id':'12812','path':literal['path'],'span':compact_location(literal['location']),'original':literal['original'],'replacement':literal['replacement'],'mutator':raw_mutant['mutatorName'],
 'member':'CommandParser.IsDerivableProperty','cause':'SUPPLEMENTAL-12812','observed':{},
 'allowedRemaining':{segment:['Survived','NoCoverage'] for segment in ['happy-path','error-handling']},'action':'deferred-with-owner','kind':'supplemental-current-literal-survivor',
 'rationale':'Exact current baseline94/94 and literal94/94 pass with normal analyzers; this finite expression survives the new tests. Original complete local Happy was Killed and hosted only has a raw-success observation. This is an authorized source-bound deferral, not a native outcome, broad override-chain defect or equivalence claim.',
 'followup':ticket,'identitySha256':sha(json.dumps(extra_identity,sort_keys=True,separators=(',',':')).encode()),
 'supplementalReview':{'evidence':'fc-12812-current-control-attempt3/verification.json','sha256':sha(parent_path.read_bytes()),'literalResult':'survived-current94-case-control','nativeOutcomeClaimed':False}}
entries.append(extra_record);entries.sort(key=lambda row:int(row['id']))
details.append({'id':'12812','identity':extra_identity,'observations':{},'disposition':extra_record,'supplementalControl':parent,'originalLocalStatus':'Killed','hostedFinalJsonStatus':None})
v['hosted12812Uncertainty']['remainingCheck']='Original hosted final JSON status remains unknown. Only the exact separately reviewed supplemental12812 may be admitted for a fresh Survived/NoCoverage outcome; this does not assign either outcome.'
v['hosted12812Uncertainty']['freshSupplementalAdmission']='Exact identity only, deferred-with-owner; original problem observation counts remain1498.'

# The per-ID owner ledger is finite/source-bound; existing aggregate counts are not an identity verifier.
ledger={'schemaVersion':'finite-source-tools-technical-followups.v1','scope':'Automated source/evidence dispositions under the existing Root human-validation waiver; not personal human review, a new census authority, future native outcomes or a passing score.',
 'owner':ROLE,'sourceBase':'e0eef14ddbbcd03823863a4f417a4b07f6f5bafc','currentSourceBase':'b34be9245443d21b72a6076cf205bbbc8a8c521f',
 'originalIdentityCount':828,'supplementalIdentityCount':1,
 'sourceFiles':v['sourceFiles'],'originalReports':v['originalReportHashes'],
 'admissionRule':'Only exact known identity/source/span/replacement with an allowedRemaining status is admitted. Killed is resolved, not fabricated. Tested kill entries admit no fresh remaining problem. NoCoverage may become Survived only where explicitly listed. Unknown/unreviewed fresh problem IDs remain blockers;12812 has an explicitly separate finite supplemental review, never a forged original/native observation.',
 'requiredAcceptanceChecks':['Unchanged complete native segment exits/scores80/60 and strict BOTH private compatibility/provenance validation remain mandatory.','Existing strict validator retains aggregate counts. The mandatory owner eng/check-mutation-triage.py step additionally verifies both exact per-ID/status reports in the source-tools-mutation aggregate; unknown identities remain blockers.'],
 'hashBounds':'Arithmetic acceptance concerns equal-value/hash congruence only, under stable nested-hash/immutability preconditions. Actual numeric values, external/performance behavior and bucket distribution are not asserted equivalent. Field removal/constant-array risk is separately deferred unless that exact expression has an adopted preserved failing control. The112-field internal regression allows collisions; it is not a general public uniqueness guarantee.',
 'hosted12812Uncertainty':v['hosted12812Uncertainty'],'followups':followups,'mutations':entries}
# Full substantive details live outside the owner1MiB file limit.
(BASE/'per-id-technical-dispositions.json').write_text(json.dumps({'scope':__doc__,'mutations':details,'followups':followups,'historicalControlBindings':historical},indent=2)+'\n')
# Keep the owner ledger compact and reviewable; each complete record retains its own rationale.
# One complete mutation/follow-up object per line keeps finite identity review practical
# and preserves the existing1MiB owner-file ceiling without dropping evidence.
fields=[]
for key,value in ledger.items():
 if key=='mutations':
  encoded='[\n'+',\n'.join('  '+json.dumps(row,ensure_ascii=False,separators=(',',':')) for row in value)+'\n ]'
 elif key=='followups':
  encoded='{\n'+',\n'.join('  '+json.dumps(k)+':'+json.dumps(row,ensure_ascii=False,separators=(',',':')) for k,row in value.items())+'\n }'
 else:encoded=json.dumps(value,ensure_ascii=False,separators=(',',':'))
 fields.append(' '+json.dumps(key)+':'+encoded)
data=('{\n'+',\n'.join(fields)+'\n}\n').replace('\n','\r\n').encode()
(BASE/'prospective-mutation-problem-followups.json').write_bytes(data)
counts=collections.Counter(row['action'] for row in entries)
(BASE/'disposition-counts.json').write_text(json.dumps({'uniqueIdentities':len(entries),'originalObservations':sum(len(r['observed']) for r in entries),'actions':dict(counts),'hashArithmeticBounded':sum(r['kind']=='non-actionable-for-bounded-equal-value-hash-congruence' for r in entries),'followupTickets':len(followups),'ownerLedgerBytes':len(data),'belowOneMiB':len(data)<1048576},indent=2)+'\n')
print(json.dumps({'actions':dict(counts),'followupTickets':len(followups),'ownerLedgerBytes':len(data),'belowOneMiB':len(data)<1048576}))
