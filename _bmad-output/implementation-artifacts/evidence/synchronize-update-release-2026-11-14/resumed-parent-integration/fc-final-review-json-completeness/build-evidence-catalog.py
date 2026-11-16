"""Expose retained executed control identities and current preservation separately; publication stays pending."""
from pathlib import Path
import hashlib,json,re
BASE=Path(__file__).resolve().parent;E=BASE.parent;ROOT=BASE.parents[5];OWNER=ROOT/'references/Hexalith.FrontComposer';REL='tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
proof=json.loads((E/'fc-finite-technical-triage/verified-identities-and-controls.json').read_text());census=json.loads((E/'fc-current-regression-literal-census/verification.json').read_text());hp=E/'fc-hash-field-participation-proposal/attempt-02/verification.json';hashproof=json.loads(hp.read_text())
ledger_path=OWNER/(REL+'mutation-problem-followups.json');ledger=json.loads(ledger_path.read_text());cases={}
for row in ledger['mutations']:
 if row['action']!='kill-test-added':continue
 ident=row['id'];control=proof['literalControls'][ident];assert control['supportsKillTestAdded']
 is_hash=control['kind']=='finite-internal-field-participation-literal';packet='fc-hash-field-participation-proposal' if is_hash else 'fc-current-regression-literal-census'
 record=hashproof if is_hash else census;record_file=hp if is_hash else E/packet/'verification.json'
 commands=[c for c in record['commands'] if c['name'] in [ident+'-build',ident+'-tests']]
 assert len(commands)==2 and commands[0].get('exitCode',commands[0].get('exit_code'))==0 and commands[1].get('exitCode',commands[1].get('exit_code'))==2
 executed=[]
 if is_hash:
  for name,value in hashproof['private_test_sha256'].items():
   source=E/packet/name;assert sha(source)==value
   executed.append({'file':name,'sha256':value,'retainedLocator':packet+'/'+name})
 else:
  original=next(c for c in census['controls'] if c['id']==ident)
  for cls in original['classFilters']:
   relative=REL+cls+'.cs';source=E/packet/'retained-executed-overlay'/relative;assert sha(source)==census['overlay'][relative]
   executed.append({'file':relative,'sha256':census['overlay'][relative],'retainedLocator':packet+'/retained-executed-overlay/'+relative})
 current=[]
 for assertion in control['currentAssertionProofs']:
  if not assertion['preserved']:continue
  cls=assertion['method'].split('.')[0];path=REL+cls+'.cs'
  current.append({'method':assertion['method'],'testCase':assertion['test'],'ownerFile':path,'currentFileSha256':sha(OWNER/path),
   'originalInspectionMethodTokenSha256':assertion['originalMethodSha256'],'preservationInspectionMethodTokenSha256':assertion['currentMethodSha256'],
   'inspectionRetainedLocator':'fc-finite-technical-triage/verified-identities-and-controls.json#/literalControls/'+ident,
   'inspectionRecordSha256':sha(E/'fc-finite-technical-triage/verified-identities-and-controls.json'),
   'boundary':'Executed test source fingerprints above are original; owner fingerprints/stronger current assertions are later, not retroactively claimed as executed by the original control.'})
 cases[ident]={'identitySha256':row['identitySha256'],'production':{'path':row['path'],'span':row['span'],'original':row['original'],'replacement':row['replacement'],'sourceSha256':ledger['sourceFiles'][row['path']]['sha256']},
  'executed':{'sourceBase':record.get('sourceBase',record.get('source_base_commit')),'testSources':executed,'commands':commands,
   'recordLocator':str(record_file.relative_to(E)),'recordSha256':sha(record_file),'resultLocator':control['trx'],'resultSha256':control['trxSha256'],'result':'literal test failure; not a native Stryker status'},
  'currentPreservation':current,'replay':{'workingDirectory':'A disposable owner copy with the declared frozen inputs and the original executed test-source overlay restored and hash-checked.',
   'steps':['Verify the exact production SHA/span and replace only the recorded expression.','Restore and hash-check executed testSources using retained locators below the workspace evidence root.','Run the recorded build/test commands; replace only expired scratch/result paths with private current paths, preserving every semantic option/filter.','Keep baseline/literal evidence distinct from later owner assertions and any native qualification.']}}
assert len(cases)==95
catalog={'schemaVersion':'finite-regression-control-evidence.v1','scope':__doc__,'rootEvidencePublication':{'state':'pending; no published commit/URL claimed','workspaceRelativeRoot':'_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/resumed-parent-integration'},'cases':cases}
data=(json.dumps(catalog,indent=1)+'\n').replace('\n','\r\n').encode();assert len(data)<1048576
(OWNER/(REL+'mutation-regression-evidence.json')).write_bytes(data)
ledger['evidenceCatalog']={'path':REL+'mutation-regression-evidence.json','sha256':hashlib.sha256(data).hexdigest(),'publication':'Root retained evidence publication pending; no fabricated published commit'}
for row in ledger['mutations']:
 if row['action']=='kill-test-added':row['control']={'result':'detected','catalogId':row['id']}
# Retain one complete object per line within the existing1MiB bound.
fields=[]
for key,value in ledger.items():
 if key=='mutations':encoded='[\n'+',\n'.join('  '+json.dumps(item,ensure_ascii=False,separators=(',',':')) for item in value)+'\n ]'
 elif key=='followups':encoded='{\n'+',\n'.join('  '+json.dumps(k)+':'+json.dumps(item,ensure_ascii=False,separators=(',',':')) for k,item in value.items())+'\n }'
 else:encoded=json.dumps(value,ensure_ascii=False,separators=(',',':'))
 fields.append(' '+json.dumps(key)+':'+encoded)
ledger_bytes=('{\n'+',\n'.join(fields)+'\n}\n').replace('\n','\r\n').encode();assert len(ledger_bytes)<1048576
ledger_path.write_bytes(ledger_bytes)
manifest_path=OWNER/(REL+'mutation-target-manifest.json');manifest=json.loads(manifest_path.read_text())
for entry in manifest['problemMutantTriage']:entry['identityLedgerSha256']=hashlib.sha256(ledger_bytes).hexdigest()
manifest_path.write_bytes((json.dumps(manifest,indent=2)+'\n').replace('\n','\r\n').encode())
(BASE/'evidence-catalog-bindings.json').write_text(json.dumps({'cases':95,'catalogBytes':len(data),'ledgerBytes':len(ledger_bytes),'catalogSha256':hashlib.sha256(data).hexdigest(),'ledgerSha256':hashlib.sha256(ledger_bytes).hexdigest(),'rootEvidencePublication':'pending'},indent=2)+'\n')
print(json.dumps({'cases':95,'catalogBytes':len(data),'ledgerBytes':len(ledger_bytes)}))
