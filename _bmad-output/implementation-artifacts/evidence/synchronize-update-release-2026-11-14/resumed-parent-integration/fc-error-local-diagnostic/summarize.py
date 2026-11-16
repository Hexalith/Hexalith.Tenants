"""Summarize completed diagnostic reports without changing original report bytes."""
from pathlib import Path
import collections,hashlib,json,re,subprocess
BASE=Path(__file__).resolve().parent
ATTEMPT=BASE/'attempt-01'
ROOT=BASE.parents[5]
record=json.loads((ATTEMPT/'execution.json').read_text())
report_path=ATTEMPT/'original-error-artifacts/reports/source-tools-error-handling.json'
report=json.loads(report_path.read_text())
work=Path(record['workspace']);prefix=str(work)+'/'
relative=lambda name:name.removeprefix(prefix)
sha=lambda path:hashlib.sha256(path.read_bytes()).hexdigest()
counts=collections.Counter();invalid=[];problems=[];files=[];timeouts=[];used_tests=set()
for name,data in report['files'].items():
    rows=data.get('mutants',[]);file_counts=collections.Counter(row['status'] for row in rows)
    files.append({'path':relative(name),'source_sha256':hashlib.sha256(data.get('source','').encode()).hexdigest(),'raw_status_counts':dict(file_counts)})
    for row in rows:
        counts[row['status']]+=1
        used_tests.update(row.get('coveredBy',[]));used_tests.update(row.get('killedBy',[]))
        if row['status']=='CompileError':
            assert re.fullmatch(r'\d+',row['id']) and row.get('statusReason')=='Mutant caused compile errors' and row.get('coveredBy')==[] and row.get('killedBy')==[]
            invalid.append({'path':relative(name),'id':row['id']})
        if row['status'] in ('Survived','NoCoverage','Timeout'):
            value={'path':relative(name),**{key:row[key] for key in ('id','status','mutatorName','replacement','location','static')},'covered_by_count':len(row.get('coveredBy',[])),'killed_by_count':len(row.get('killedBy',[]))}
            problems.append(value)
            if row['status']=='Timeout':timeouts.append(value)
cases=[]
for name,data in report['testFiles'].items():
    for case in data.get('tests',[]): cases.append({'path':relative(name),'source_sha256':hashlib.sha256(data.get('source','').encode()).hexdigest(),**case})
case_ids={row['id'] for row in cases};assert len(cases)==303 and len(case_ids)==303
assert not used_tests-case_ids
bindings={'scope':'Exact original e0 Error diagnostic; case IDs and source spans extracted from untouched original JSON. No human triage or qualification.','source':record['source'],'report_sha256':sha(report_path),'source_files':files,'cases':cases,'all_coverage_and_kill_case_ids_resolve':True}
(ATTEMPT/'source-and-case-bindings.json').write_text(json.dumps(bindings,indent=2)+'\n')
(ATTEMPT/'problem-mutants.json').write_text(json.dumps({'source':record['source'],'report_sha256':sha(report_path),'scope':'Untriaged actual final statuses. Full coveredBy/killedBy arrays remain in original report. No disposition assigned.','mutants':problems},indent=2)+'\n')
(ATTEMPT/'nonexecuted-compiler-candidates.json').write_text(json.dumps({'source':record['source'],'report_sha256':sha(report_path),'count':len(invalid),'candidates':invalid},indent=2)+'\n')
console=(ATTEMPT/'complete-error-segment.log').read_text(errors='replace').splitlines()
resources=[json.loads(line.split(' ',1)[1]) for line in console if line.startswith('mutation-diagnostics ') and '"event": "resources"' in line]
planned=next(int(re.search(r'(\d+)\s+total mutants will be tested',line)[1]) for line in console if 'total mutants will be tested' in line)
assert planned==2278 and counts['Killed']+counts['Survived']+counts['Timeout']==planned
assert counts=={'Ignored':562,'CompileError':717,'Killed':1647,'Survived':630,'NoCoverage':186,'Timeout':1}
assert all(command['exit_code']==0 for command in record['commands'] if command['name']!='strict-both-segment-validator')
assert record['both_segment_validator_exit']==1 and not record.get('blocker')
for command in record['commands']: assert sha(ATTEMPT/command['log'])==command['log_sha256']
original_manifest=json.loads((ATTEMPT/'artifact-manifest.json').read_text())
for name,digest in original_manifest.items():assert sha(ATTEMPT/name)==digest
assert not work.exists() and not work.parent.exists()
owned=[]
for path in Path('/proc').glob('[0-9]*'):
    try:
        args=(path/'cmdline').read_bytes().replace(b'\0',b' ').decode(errors='replace')
        cwd=(path/'cwd').resolve()
        if str(work.parent) in args or str(cwd).startswith(str(work.parent)+'/'):owned.append(path.name)
    except (OSError,ValueError):pass
assert not owned,owned
owner=ROOT/'references/Hexalith.FrontComposer'
assert not subprocess.check_output(['git','status','--porcelain'],cwd=owner,text=True)
owner_head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=owner,text=True).strip()
summary={'scope':'Complete original e0 Error-only local diagnostic. Not a full dual-segment gate, owner acceptance, production/native qualification or qualification of695d/future source.','source':record['source'],'frozen_builds':record['builds'],'existing_error_test_cases':303,'planned_and_completed_executable_mutants':planned,'all_live_mutants_including_no_coverage':planned+counts['NoCoverage'],'raw_status_counts':dict(counts),'exact_nonexecuted_compiler_candidates':len(invalid),'error_segment_exit':record['error_segment_exit'],'tool_reported_error_only_score_percent':66.88,'error_provenance_verify_exit':record['error_provenance_verify_exit'],'strict_both_segment_validator_exit':record['both_segment_validator_exit'],'validator_refusals':['Missing Happy report and exact private-tool provenance','Untriaged Error Survived630','Untriaged Error NoCoverage186','Untriaged Error Timeout1'],'elapsed_segment_seconds':next(row['elapsed_seconds'] for row in record['commands'] if row['name']=='complete-error-segment'),'timeout_mutants':timeouts,'partial_console_count_correction':'Debug Test run for14021 is success was a partial run message; final original JSON marks14021 Timeout. Final630 Survived plus1 Timeout supersedes partial631 success observations.','private_regression_proposal_applied':False,'thresholds_or_cohorts_changed':False,'github_context_supplied':False,'source_and_case_bindings':{'path':'source-and-case-bindings.json','sha256':sha(ATTEMPT/'source-and-case-bindings.json')},'problem_inventory':{'path':'problem-mutants.json','sha256':sha(ATTEMPT/'problem-mutants.json')},'original_report':{'path':str(report_path.relative_to(ATTEMPT)),'sha256':sha(report_path)},'original_html':{'path':'original-error-artifacts/reports/source-tools-error-handling.html','sha256':sha(ATTEMPT/'original-error-artifacts/reports/source-tools-error-handling.html')},'console':{'path':'complete-error-segment.log','sha256':sha(ATTEMPT/'complete-error-segment.log')},'resource_samples':len(resources),'minimum_observed_available_gib':round(min(row['memory_kib']['MemAvailable'] for row in resources)/1024/1024,2),'cleanup':{'private_workspace_exists':False,'wrapper_exists':False,'owned_processes':owned,'final_owned_children_cleaned':record.get('final_owned_children_cleaned',[])},'owner_head_at_handoff':owner_head,'owner_status_porcelain':'','original_log_and_artifact_hashes_verified':True}
(ATTEMPT/'summary.json').write_text(json.dumps(summary,indent=2)+'\n')
final={str(path.relative_to(ATTEMPT)):sha(path) for path in ATTEMPT.rglob('*') if path.is_file() and path.name not in ('final-artifact-manifest.json',)}
(ATTEMPT/'final-artifact-manifest.json').write_text(json.dumps(final,indent=2,sort_keys=True)+'\n')
print(json.dumps({'raw_counts':dict(counts),'cases':len(cases),'executable_completed':planned,'elapsed_seconds':summary['elapsed_segment_seconds'],'timeout_id':timeouts[0]['id'],'owned_processes':len(owned),'owner_head':owner_head,'summary':'attempt-01/summary.json'},indent=2))
