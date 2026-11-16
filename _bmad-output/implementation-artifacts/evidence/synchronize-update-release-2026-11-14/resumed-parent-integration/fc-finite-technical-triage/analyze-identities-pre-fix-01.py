"""Independently bind every original mutation/control to unchanged production and current assertions."""
from pathlib import Path
import collections,hashlib,json,re,xml.etree.ElementTree as ET
BASE=Path(__file__).resolve().parent;EVIDENCE=BASE.parent;ROOT=BASE.parents[5];OWNER=ROOT/'references/Hexalith.FrontComposer'
sha=lambda data:hashlib.sha256(data).hexdigest()
load=lambda relative:json.loads((EVIDENCE/relative).read_text())
error=load('fc-error-followthrough-analysis/per-id-ledger.json');happy=load('fc-happy-local-diagnostic/complete-local-problem-inventory.json')
report_paths={'error-handling':EVIDENCE/'fc-error-local-diagnostic/retained-original-reports/source-tools-error-handling.json','happy-path':EVIDENCE/'fc-happy-local-diagnostic/attempt-03/post-validator-artifacts/happy-path/reports/source-tools-happy-path.json'}
expected={'error-handling':error['report_sha256'],'happy-path':happy['reportSha256']}
reports={};report_indices={};sources={};members=load('fc-error-followthrough-analysis/source-index.json')['members']
for segment,path in report_paths.items():
 assert sha(path.read_bytes())==expected[segment]
 doc=json.loads(path.read_text());reports[segment]=doc;index={}
 for full_path,file in doc['files'].items():
  marker='src/Hexalith.FrontComposer.SourceTools/'
  if marker not in full_path:continue
  relative=marker+full_path.split(marker,1)[1]
  for mutant in file['mutants']:
   assert str(mutant['id']) not in index
   index[str(mutant['id'])]=(relative,file,mutant)
 report_indices[segment]=index

def normalize(text):return text.replace('\r\n','\n')
def offset(source,position):
 lines=source.splitlines(keepends=True)
 prefix=lines[position['line']-1].encode('utf-16-le')[:(position['column']-1)*2].decode('utf-16-le')
 return sum(map(len,lines[:position['line']-1]))+len(prefix)
def span(source,location):return source[offset(source,location['start']):offset(source,location['end'])]
def variant(source,location,replacement):
 source=normalize(source)
 return source[:offset(source,location['start'])]+normalize(replacement)+source[offset(source,location['end']):]

rows={}
for segment,inventory,status_key in [('error-handling',error['mutants'],'observed_status'),('happy-path',happy['mutants'],'localOriginalStatus')]:
 for entry in inventory:
  ident=entry['id'];path,file,mutation=report_indices[segment][ident]
  assert path==entry['path'] and mutation['status']==entry[status_key]
  assert mutation['location']==entry['location'] and mutation['replacement']==entry['replacement']
  assert mutation['mutatorName']==entry['mutator']
  actual=(OWNER/path).read_bytes();source=actual.decode('utf-8-sig');original=span(source,mutation['location'])
  assert normalize(file['source'])==normalize(source),(ident,path,'report/current source drift')
  assert normalize(span(file['source'],mutation['location']))==normalize(original)
  if 'source_sha256' in entry:assert sha(actual)==entry['source_sha256']
  if 'original' in entry:assert normalize(original)==normalize(entry['original'])
  sources[path]={'sha256':sha(actual),'canonicalLfSha256':sha(normalize(source).encode())}
  if ident not in rows:
   enclosing=[m for m in members if m['path']==path and m['startLine']<=mutation['location']['start']['line'] and m['endLine']>=mutation['location']['end']['line']]
   member=min(enclosing,key=lambda m:m['endLine']-m['startLine']) if enclosing else None
   row={'id':ident,'path':path,'location':mutation['location'],'original':original,'replacement':mutation['replacement'],'mutator':mutation['mutatorName'],
        'member':entry.get('member') or (member['name'] if member else 'public getter at exact source span'),
        'cause':entry.get('cause_group') or 'H'+ident,'observations':{},'context':source.splitlines()[mutation['location']['start']['line']-1].strip(),
        'initialReview':entry if segment=='error-handling' else None}
   rows[ident]=row
  row=rows[ident]
  assert row['path']==path and row['location']==mutation['location'] and row['replacement']==mutation['replacement'] and row['original']==original
  row['observations'][segment]={'status':mutation['status'],'reportSha256':expected[segment],'coverageCount':len(mutation.get('coveredBy') or [])}
assert len(rows)==828
# Every traced hash/equality method and scalar participation remains exact current source.
hashes=load('fc-error-followthrough-analysis/hash-consumer-audit.json')
for member in hashes['hash_members']:
 current=normalize((OWNER/member['path']).read_text())
 assert normalize(member['hash_source']) in current
 assert all(normalize(text) in current for text in member['equality_source'])
 assert not member['hashed_input_names_not_found_in_equality_text']

inventory=json.loads((BASE/'method-inventories.json').read_text());by_kind={}
for item in inventory:
 by_kind.setdefault(item['kind'],{})[Path(item['path']).name]=item
 if item['kind']=='current':assert sha(Path(item['path']).read_bytes())==item['sha256']
census=load('fc-current-regression-literal-census/verification.json')
for path,expected_sha in census['overlay'].items():
 original=EVIDENCE/'fc-current-regression-literal-census/retained-executed-overlay'/path
 assert sha(original.read_bytes())==expected_sha

def subsequence(before,after):
 iterator=iter(after)
 return all(any(value==candidate for candidate in iterator) for value in before)
def assertion_proof(test_name,original_kind='original'):
 prefix=test_name.split('(',1)[0];class_name,method_name=prefix.split('.')[-2:];file=class_name+'.cs';key=class_name+'.'+method_name
 originals=by_kind.get(original_kind,{})
 if file not in originals or file not in by_kind['current']:return {'test':test_name,'preserved':False,'reason':'No exact recorded/current class pair'}
 old=next((m for m in originals[file]['methods'] if m['key']==key),None);current=next((m for m in by_kind['current'][file]['methods'] if m['key']==key),None)
 if not old or not current:return {'test':test_name,'preserved':False,'reason':'Method identity missing'}
 before=collections.Counter(sha(json.dumps(a['tokens']).encode()) for a in old['assertions']);after=collections.Counter(sha(json.dumps(a['tokens']).encode()) for a in current['assertions'])
 retained=bool(before) and all(after[k]>=count for k,count in before.items())
 subset=subsequence(old['tokens'],current['tokens'])
 return {'test':test_name,'method':key,'preserved':retained,'oldAssertionCount':sum(before.values()),'allOriginalMethodTokensRetainedInOrder':subset,
         'originalMethodSha256':sha(json.dumps(old['tokens']).encode()),'currentMethodSha256':sha(json.dumps(current['tokens']).encode()),
         'currentClassSha256':by_kind['current'][file]['sha256']}
controls={}
for control in census['controls']:
 ident=control['id'];row=rows[ident];data=(OWNER/row['path']).read_bytes()
 assert sha(data)==control['sourceSha256']
 assert sha(variant(data.decode('utf-8-sig'),row['location'],row['replacement']).encode())==control['variantSha256']
 assert row['observations']['happy-path']['status']==control['originalStatus']
 checked={'kind':'sealed77-case-nonhash-literal','result':control['result'],'sourceSha256':control['sourceSha256'],'variantSha256':control['variantSha256']}
 if control.get('nativeTrxSha256'):
  path=EVIDENCE/'fc-current-regression-literal-census/results'/ident/'tests.xml';assert sha(path.read_bytes())==control['nativeTrxSha256']
  doc=ET.parse(path);failures=[item.attrib['testName'] for item in doc.iter() if item.tag.rsplit('}',1)[-1]=='UnitTestResult' and item.attrib.get('outcome')=='Failed']
  assert failures==control['failedTests']
  checked['trx']=str(path.relative_to(EVIDENCE));checked['trxSha256']=control['nativeTrxSha256'];checked['currentAssertionProofs']=[assertion_proof(test) for test in failures]
  checked['supportsKillTestAdded']=control['result']=='detected' and bool(failures) and any(p['preserved'] for p in checked['currentAssertionProofs'])
 else:checked['supportsKillTestAdded']=False
 controls[ident]=checked
# New bounded internal hash-collection assertion: only the three actually failed expressions qualify.
adoption=load('fc-hash-field-participation-proposal/owner-adoption.json')
for file,value in adoption['newOwnerFiles'].items():assert sha(OWNER/'tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation'/file)==value
hash_verification=load('fc-hash-field-participation-proposal/attempt-02/verification.json')
hash_summary=load('fc-hash-field-participation-proposal/summary.json')
for ident in ['13180','13382','13419','13178','13179']:
 row=rows[ident];proof=hash_verification['variants'][ident]
 assert row['path']==proof['path'] and row['location']==proof['original_location'] and row['replacement']==proof['replacement']
 assert sha(variant((OWNER/row['path']).read_text(),row['location'],row['replacement']).encode())==proof['literal_source_sha256']
 path=EVIDENCE/'fc-hash-field-participation-proposal/attempt-02'/(ident+'-results/tests.trx')
 expected_trx=next(x['nativeResultSha256'] for x in hash_summary['attempt02'] if x['literal']==ident)
 assert sha(path.read_bytes())==expected_trx
 doc=ET.parse(path);failures=[item.attrib['testName'] for item in doc.iter() if item.tag.rsplit('}',1)[-1]=='UnitTestResult' and item.attrib.get('outcome')=='Failed']
 current_proofs=[assertion_proof(test,'hash-private') for test in failures]
 checked={'kind':'finite-internal-field-participation-literal','result':'detected' if failures else 'survived','trx':str(path.relative_to(EVIDENCE)),'trxSha256':expected_trx,
          'sourceSha256':sources[row['path']]['sha256'],'variantSha256':proof['literal_source_sha256'],'currentAssertionProofs':current_proofs,
          'supportsKillTestAdded':bool(failures) and any(p['preserved'] for p in current_proofs),'boundary':'64-value collection sampling allows collisions, not numeric hash pin/all-unequal uniqueness; no native outcome inferred'}
 controls[ident]=checked
# Separately observed hosted12812 was Killed in the only complete available local JSON.
p,f,m=report_indices['happy-path']['12812'];assert m['status']=='Killed'
hosted_uncertainty={'id':'12812','path':p,'location':m['location'],'original':span(f['source'],m['location']),'replacement':m['replacement'],'localCompleteStatus':'Killed',
 'hostedObservation':'raw is-success console event; hosted550survivors differs from local549; final hosted JSON unavailable','hostedFinalJsonStatus':None,'admittedAsProblem':False,
 'remainingCheck':'Obtain an actual final hosted/fresh report, then independently inspect exact mutation identity/status; a fresh remaining problem12812 is not admitted by this ledger.'}
result={'scope':__doc__,'sourceFiles':sources,'originalReportHashes':expected,'mutations':list(rows.values()),'literalControls':controls,'hosted12812Uncertainty':hosted_uncertainty,
        'controlCounts':dict(collections.Counter(c['result'] for c in census['controls'])),'preservedAssertionKillCandidates':[ident for ident,c in controls.items() if c['supportsKillTestAdded']],
        'detectedButAssertionNotProved':[ident for ident,c in controls.items() if c['result']=='detected' and not c['supportsKillTestAdded']]}
(BASE/'verified-identities-and-controls.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({'identities':len(rows),'observations':sum(len(r['observations']) for r in rows.values()),'currentProductionFiles':len(sources),'supportsKillTestAdded':len(result['preservedAssertionKillCandidates']),'detectedButAssertionNotProved':result['detectedButAssertionNotProved']}))
