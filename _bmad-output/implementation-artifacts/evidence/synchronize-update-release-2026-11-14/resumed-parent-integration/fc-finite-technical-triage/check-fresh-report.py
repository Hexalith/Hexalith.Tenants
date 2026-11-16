"""Diagnostic single-report wrapper over the persisted owner matcher; never a native gate."""
from pathlib import Path
import argparse
import importlib.util
import json

owner=Path(__file__).resolve().parents[6]/'references/Hexalith.FrontComposer'
spec=importlib.util.spec_from_file_location('owner_finite_triage',owner/'eng/check-mutation-triage.py')
implementation=importlib.util.module_from_spec(spec);spec.loader.exec_module(implementation)
AdmissionError=implementation.AdmissionError
load_metadata=implementation.load_metadata
inspect_report=implementation.inspect_report
digest=implementation.digest
REL=implementation.REL
location=implementation.location

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--owner-root',type=Path,required=True);parser.add_argument('--segment',choices=['happy-path','error-handling'],required=True)
    parser.add_argument('--report',type=Path,required=True);parser.add_argument('--output',type=Path)
    args=parser.parse_args()
    try:
        ledger,manifest,rows,sources=load_metadata(args.owner_root)
        result=inspect_report(json.loads(args.report.read_text()),args.segment,rows,sources)
        result['actualReportSha256']=digest(args.report.read_bytes())
        result['finiteLedgerSha256']=digest((args.owner_root/(REL+'mutation-problem-followups.json')).read_bytes())
        code=0 if result['acceptedByThisFiniteCheck'] else 1
    except (AdmissionError,OSError,KeyError,ValueError) as error:
        result={'scope':__doc__,'acceptedByThisFiniteCheck':False,'error':str(error)};code=2
    text=json.dumps(result,indent=2)+'\n'
    if args.output:args.output.write_text(text)
    print(json.dumps({'acceptedByThisFiniteCheck':result['acceptedByThisFiniteCheck'],'failureCount':len(result.get('failures',[])),'error':result.get('error')}))
    return code

if __name__=='__main__':raise SystemExit(main())
