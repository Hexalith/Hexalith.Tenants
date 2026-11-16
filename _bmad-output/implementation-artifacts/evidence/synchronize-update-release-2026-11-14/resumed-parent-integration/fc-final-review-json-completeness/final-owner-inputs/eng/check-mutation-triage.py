"""Finite source/identity/status admission only; native scores/BOTH/private provenance remain mandatory."""
from pathlib import Path
import argparse,hashlib,json,re,sys

REL='tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/'
STATUSES={'Killed','Survived','NoCoverage','Timeout','CompileError','Ignored'}
PROBLEM={'Survived','NoCoverage','Timeout','CompileError'}
ROLE='Story10-4 SourceTools quality/release-automation role'

class AdmissionError(ValueError):pass

def require(condition,message):
    if not condition:raise AdmissionError(message)

def digest(data):return hashlib.sha256(data).hexdigest()

def canonical(text):return text.replace('\r\n','\n')

def offset(source,line,column):
    lines=source.splitlines(keepends=True)
    require(type(line) is int and type(column) is int and 1<=line<=len(lines) and column>=1,'Invalid mutation source span')
    width=(column-1)*2
    raw=lines[line-1].encode('utf-16-le')
    require(width<=len(raw),'Mutation column escapes source line')
    return sum(map(len,lines[:line-1]))+len(raw[:width].decode('utf-16-le'))

def extract(source,span):return source[offset(source,*span[:2]):offset(source,*span[2:])]

def location(span):return {'start':{'line':span[0],'column':span[1]},'end':{'line':span[2],'column':span[3]}}

def identity(row,sources):
    return {'id':row['id'],'path':row['path'],'sourceSha256':sources[row['path']]['sha256'],'location':location(row['span']),
            'original':row['original'],'replacement':row['replacement'],'mutator':row['mutator']}

def load_metadata(owner):
    ledger_path=owner/(REL+'mutation-problem-followups.json');manifest_path=owner/(REL+'mutation-target-manifest.json')
    require(ledger_path.stat().st_size<1048576 and manifest_path.stat().st_size<1048576,'Owner metadata exceeds existing1MiB limit')
    data=ledger_path.read_bytes();ledger=json.loads(data);manifest=json.loads(manifest_path.read_text())
    require(ledger['schemaVersion']=='finite-source-tools-technical-followups.v1','Unknown finite ledger schema')
    require(ledger['owner']==ROLE,'Unassigned finite follow-up role')
    require(ledger['originalIdentityCount']==828 and ledger['supplementalIdentityCount']==1 and len(ledger['mutations'])==829,'Finite original/supplemental identity inventory drift')
    sources={}
    for path,expected in ledger['sourceFiles'].items():
        raw=(owner/path).read_bytes();require(digest(raw)==expected['sha256'],'Changed current production source: '+path)
        require(digest(canonical(raw.decode('utf-8-sig')).encode())==expected['canonicalLfSha256'],'Canonical source fingerprint drift')
        sources[path]=raw.decode('utf-8-sig')
    rows={}
    for row in ledger['mutations']:
        ident=row['id'];require(ident not in rows,'Duplicate reviewed mutation identity')
        require(row['path'] in sources,'Unknown reviewed source path')
        require(extract(sources[row['path']],row['span'])==row['original'],'Changed reviewed exact source span: '+ident)
        require(digest(json.dumps(identity(row,ledger['sourceFiles']),sort_keys=True,separators=(',',':')).encode())==row['identitySha256'],'Reviewed mutation identity digest differs: '+ident)
        require(row['action'] in {'kill-test-added','equivalent-accepted','deferred-with-owner'},'Unsupported/blocking disposition')
        if 'supplementalReview' in row:
            require(ident=='12812' and row['observed']=={} and row['action']=='deferred-with-owner','Unreviewed supplemental identity')
            require(row['supplementalReview']['literalResult']=='survived-current94-case-control' and row['supplementalReview']['nativeOutcomeClaimed'] is False,'Fabricated supplemental native outcome')
            require(row['allowedRemaining']=={segment:['Survived','NoCoverage'] for segment in ['happy-path','error-handling']},'Unreviewed supplemental execution status')
        else:require(set(row['observed'])==set(row['allowedRemaining']),'Segment transition scope differs')
        if row['action']=='kill-test-added':require(not any(row['allowedRemaining'].values()),'Test-added metadata cannot admit a remaining problem')
        for segment,status in row['observed'].items():
            require(segment in {'happy-path','error-handling'} and status in {'Survived','NoCoverage','Timeout'},'Forged original observation')
            allowed=row['allowedRemaining'][segment]
            require(set(allowed)<=({status,'Survived'} if status=='NoCoverage' else {status}),'Unreviewed execution-status transition')
        if row['followup']:
            require(ledger['followups'][row['followup']]['owner']==ROLE and ident in ledger['followups'][row['followup']]['ids'],'Missing exact assigned follow-up')
        if row['action']=='deferred-with-owner':require(row['followup'] is not None,'Deferred gap has no finite follow-up')
        rows[ident]=row
    require(ledger['hosted12812Uncertainty']['hostedFinalJsonStatus'] is None and not ledger['hosted12812Uncertainty']['admittedAsProblem'],'Hosted12812 uncertainty fabricated')
    catalog_ref=ledger['evidenceCatalog']
    catalog_path=owner/catalog_ref['path']
    require(catalog_path.stat().st_size<1048576 and digest(catalog_path.read_bytes())==catalog_ref['sha256'],'Changed/missing finite control evidence catalog')
    catalog=json.loads(catalog_path.read_text())
    require(catalog['schemaVersion']=='finite-regression-control-evidence.v1','Unknown evidence catalog schema')
    require(set(catalog['cases'])=={ident for ident,row in rows.items() if row['action']=='kill-test-added'},'Control evidence identity inventory differs')
    for ident,entry in catalog['cases'].items():
        require(rows[ident]['control']['catalogId']==ident and entry['identitySha256']==rows[ident]['identitySha256'],'Control evidence mutation identity differs')
        require(entry['executed']['commands'] and entry['executed']['testSources'] and entry['executed']['resultLocator'],'Control evidence is not replayable/resolvable')
        require(re.fullmatch(r'[0-9a-f]{64}',entry['executed']['resultSha256']) is not None,'Control result fingerprint missing')
        for preserved in entry['currentPreservation']:
            require(digest((owner/preserved['ownerFile']).read_bytes())==preserved['currentFileSha256'],'Current preserved assertion source fingerprint changed')
    for record in manifest['problemMutantTriage']:
        require(record['identityLedger']==REL+'mutation-problem-followups.json' and record['identityLedgerSha256']==digest(data),'Stale/changed ledger-manifest binding')
        ids=record['mutationIds'];require(type(record['count']) is int and record['count']==len(ids)==len(set(ids)),'Aggregate count is not its finite identity maximum')
        require(record['owner']==ROLE and bool(record['rationale']),'Missing real role/rationale')
        original=0;transition=0;supplemental=0
        for ident in ids:
            require(ident in rows,'Unknown identity in aggregate maximum')
            row=rows[ident];require(row['action']==record['action'],'Aggregate action mismatch')
            require(record['segment'] in row['allowedRemaining'],'Aggregate segment mismatch')
            status=row['observed'].get(record['segment'])
            if 'supplementalReview' in row:
                require(record['status'] in row['allowedRemaining'][record['segment']],'Unreviewed supplemental aggregate status')
                supplemental+=1
            elif status==record['status']:original+=1
            else:
                require(status=='NoCoverage' and record['status']=='Survived' and 'Survived' in row['allowedRemaining'][record['segment']],'Aggregate includes an unreviewed transition')
                transition+=1
        require(original==record['originalObservedCount'] and transition==record['explicitTransitionMaximum'] and supplemental==record['supplementalReviewedMaximum'],'Aggregate original/transition/supplemental maxima differ')
    expected_groups = {}
    for row in rows.values():
        for segment,statuses in row['allowedRemaining'].items():
            status=row['observed'].get(segment)
            if status is not None:expected_groups.setdefault((segment,status,row['action']),set()).add(row['id'])
            for allowed in statuses:
                if allowed != status:
                    expected_groups.setdefault((segment,allowed,row['action']),set()).add(row['id'])
    actual_groups = {}
    for record in manifest['problemMutantTriage']:
        key = (record['segment'],record['status'],record['action'])
        require(key not in actual_groups,'Duplicate aggregate disposition group')
        actual_groups[key] = set(record['mutationIds'])
    require(actual_groups == expected_groups,'Aggregate maxima do not exactly cover the finite reviewed identities/transitions')
    return ledger,manifest,rows,sources

def report_path(value):
    value=value.replace('\\','/');marker='src/Hexalith.FrontComposer.SourceTools/'
    require(marker in value,'Problem source path is outside SourceTools')
    relative=marker+value.split(marker,1)[1]
    require('..' not in Path(relative).parts,'Problem source path traverses repository')
    return relative

def qualified_invalid_candidate(mutant):
    # This classification is conditional on the unchanged strict validator's actual
    # private-runtime qualification. It is not standalone compiler/tool admission.
    return (mutant.get('status')=='CompileError' and mutant.get('statusReason')=='Mutant caused compile errors'
            and re.fullmatch(r'[0-9]+',str(mutant.get('id',''))) is not None
            and mutant.get('coveredBy')==[] and mutant.get('killedBy')==[])

def inspect_report(report,segment,rows,sources):
    require(segment in {'happy-path','error-handling'},'Unknown segment')
    failures=[];reviewed=[];invalid=0;seen=set()
    for file_path,file in report['files'].items():
        path=None;matches_current_source=None
        for mutant in file['mutants']:
            ident=str(mutant.get('id',''));status=mutant.get('status')
            require(ident not in seen,'Duplicate actual mutation ID: '+ident);seen.add(ident)
            if status not in STATUSES:
                failures.append({'id':ident,'reason':'Unknown/unhandled actual status'});continue
            if ident not in rows and qualified_invalid_candidate(mutant):invalid+=1;continue
            is_problem=status in PROBLEM
            if ident not in rows:
                if is_problem:failures.append({'id':ident,'reason':'Unknown/unreviewed fresh problem identity'})
                continue
            row=rows[ident]
            if path is None:
                path=report_path(file_path)
                matches_current_source=path in sources and canonical(file['source'])==canonical(sources[path])
            try:
                require(path==row['path'],'Changed source path')
                require(mutant['location']==location(row['span']),'Changed source span')
                require(mutant['replacement']==row['replacement'],'Changed replacement')
                require(mutant.get('mutatorName')==row['mutator'],'Changed mutator identity')
                require(matches_current_source,'Actual report source differs from current production')
                # Exact source spans were validated by load_metadata; matching whole source preserves them.
                require(status!='Ignored','Reviewed live identity cannot become an unapproved exclusion/Ignored status')
                require(status!='CompileError','Reviewed live identity cannot be resolved by an unapproved compiler-candidate transition')
                if is_problem:require(status in row['allowedRemaining'].get(segment,[]),'Remaining problem/status is not admitted; tested kill requires actual fresh resolution')
            except (AdmissionError,KeyError) as error:
                failures.append({'id':ident,'reason':str(error)});continue
            if is_problem:reviewed.append({'id':ident,'status':status,'action':row['action'],'identitySha256':row['identitySha256']})
    expected={ident for ident,row in rows.items() if segment in row['allowedRemaining']}
    for ident in sorted(expected-seen,key=int):failures.append({'id':ident,'reason':'Expected reviewed identity missing from report'})
    return {'scope':__doc__,'segment':segment,'expectedReviewedIdentities':len(expected),'seenReviewedIdentities':len(expected & seen),'remainingProblemsAdmitted':reviewed,'conditionalInvalidCompilerCandidates':invalid,'failures':failures,
            'acceptedByThisFiniteCheck':not failures,'nativeScoresBothCohortsPrivateProvenanceStillRequired':True}

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.parse_args()
    owner=Path(__file__).resolve().parents[1]
    output=owner/'artifacts/mutation/mutation-triage-admission.json'
    result={'scope':__doc__,'acceptedByThisFiniteCheck':False,'segments':[],
            'nativeScoresBothCohortsPrivateProvenanceStillRequired':True}
    try:
        ledger,manifest,rows,sources=load_metadata(owner)
        result['finiteLedgerSha256']=digest((owner/(REL+'mutation-problem-followups.json')).read_bytes())
        for segment in ['happy-path','error-handling']:
            try:
                directory=owner/'artifacts/mutation'/segment
                candidates=sorted(directory.glob('**/reports/source-tools-'+segment+'.json'))
                require(len(candidates)==1,'Missing or ambiguous exact JSON report for '+segment)
                report=candidates[0]
                require(report.is_file() and not report.is_symlink(),'Report is not an unlinked regular JSON file')
                require(report.resolve().is_relative_to(directory.resolve()),'Report escapes its fixed segment directory')
                checked=inspect_report(json.loads(report.read_text()),segment,rows,sources)
                checked['actualReportSha256']=digest(report.read_bytes())
                checked['report']=report.relative_to(owner).as_posix()
            except (AdmissionError,OSError,KeyError,ValueError,TypeError) as error:
                checked={'segment':segment,'acceptedByThisFiniteCheck':False,'failures':[], 'error':str(error)}
            result['segments'].append(checked)
        result['acceptedByThisFiniteCheck']=all(segment['acceptedByThisFiniteCheck'] for segment in result['segments'])
        code=0 if result['acceptedByThisFiniteCheck'] else 1
    except (AdmissionError,OSError,KeyError,ValueError,TypeError) as error:
        result['error']=str(error);code=2
    output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({'acceptedByThisFiniteCheck':result['acceptedByThisFiniteCheck'],
                     'failureCount':sum(len(segment.get('failures',[])) for segment in result['segments']),
                     'error':result.get('error')}))
    return code

if __name__=='__main__':raise SystemExit(main())
