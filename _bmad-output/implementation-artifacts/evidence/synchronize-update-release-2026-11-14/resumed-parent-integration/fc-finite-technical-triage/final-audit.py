"""Final source/count/evidence/cleanup audit; no native rerun or release qualification."""
from pathlib import Path
import collections,difflib,hashlib,importlib.util,json,os,subprocess,time
BASE=Path(__file__).resolve().parent;ROOT=BASE.parents[5];OWNER=ROOT/'references/Hexalith.FrontComposer'
REL='tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/'
sha=lambda path:hashlib.sha256(path.read_bytes()).hexdigest()
before=json.loads((BASE/'before-input-hashes.json').read_text())
existing_changes=['.github/workflows/mutation-property-nightly.yml','eng/prepare-mutation-artifacts.ps1','tests/eng/test_mutation_artifact_publication.py',REL+'mutation-target-manifest.json']
new_files=['eng/check-mutation-triage.py','tests/eng/test_mutation_triage.py',REL+'mutation-problem-followups.json']
changed=[path for path,value in before['files'].items() if not (OWNER/path).is_file() or sha(OWNER/path)!=value]
assert set(changed)==set(existing_changes),changed
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=OWNER,text=True).strip()==before['ownerHead']
current={path:sha(OWNER/path) for path in existing_changes+new_files}
for path in new_files:assert (OWNER/path).is_file() and (OWNER/path).stat().st_size<1048576
original_manifest=json.loads((BASE/'before-mutation-target-manifest.json').read_text())
manifest=json.loads((OWNER/(REL+'mutation-target-manifest.json')).read_text())
comparison=json.loads(json.dumps(manifest));comparison['problemMutantTriage']=[];assert comparison==original_manifest
ledger=json.loads((OWNER/(REL+'mutation-problem-followups.json')).read_text())
assert len(ledger['mutations'])==829 and sum(len(row['observed']) for row in ledger['mutations'])==1498
assert len(manifest['problemMutantTriage'])==14 and len(ledger['followups'])==214
counts=collections.Counter(row['action'] for row in ledger['mutations']);assert counts=={'kill-test-added':95,'equivalent-accepted':329,'deferred-with-owner':405}
assert collections.Counter(row['kind'] for row in ledger['mutations'])['non-actionable-for-bounded-equal-value-hash-congruence']==318
spec=importlib.util.spec_from_file_location('final_owner_checker',OWNER/'eng/check-mutation-triage.py');module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
module.load_metadata(OWNER)
assert all(record['identityLedgerSha256']==current[REL+'mutation-problem-followups.json'] for record in manifest['problemMutantTriage'])
# Native/runtime/config/target/test-assertion inputs are untouched; syntax inspection source bindings remain current.
inspection=json.loads((BASE/'method-inventories.json').read_text())
for item in inspection:
 if item['kind']=='current':assert sha(Path(item['path']))==item['sha256']
receipts=[]
for name in ['method-inspection-verification.json','contract-checks-01/verification.json','native-count-contract-01/verification.json','original-report-refusals-01/verification.json','contract-checks-02-supplemental/verification.json','owner-guard-checks-01/verification.json','final-metadata-bindings-01/verification.json']:
 row=json.loads((BASE/name).read_text());assert not Path(row['scratch']).exists();receipts.append(row)
owned=[]
for proc in Path('/proc').iterdir():
 if not proc.name.isdigit() or int(proc.name)==os.getpid():continue
 try:
  env=(proc/'environ').read_bytes().split(b'\0')
  if any(('TMPDIR='+row['scratch']).encode() in env for row in receipts):owned.append(int(proc.name))
 except OSError:pass
assert not owned,owned
# The only publication-helper delta after separately sealed FCL-P2 is the authorized admission-audit allowlist entry.
fcl=BASE.parent/'fc-idempotent-publication-fcl-p2'
fcl_guard=(fcl/'attempt-02-corrected/inputs/eng/prepare-mutation-artifacts.ps1').read_text()
assert (OWNER/'eng/prepare-mutation-artifacts.ps1').read_text().replace('"mutation-triage-admission.json", ','')==fcl_guard
for path in ['eng/validate-stryker-reports.ps1','eng/mutation-artifact-policy.ps1']:
 assert sha(OWNER/path)==json.loads((fcl/'before-hashes.json').read_text())[path]
# Original/source-attributed input packets remain immutable; preserve source bindings of all controls.
proof=json.loads((BASE/'verified-identities-and-controls.json').read_text())
assert len(proof['mutations'])==828 and len(proof['preservedAssertionKillCandidates'])==95
for path,binding in ledger['sourceFiles'].items():assert sha(OWNER/path)==binding['sha256']
source_groups=json.loads((BASE/'cause-dispositions.json').read_text());assert source_groups['totalFiniteGroups']==237
owner_checks=json.loads((BASE/'owner-guard-checks-01/verification.json').read_text());assert all(row['exitCode']==0 for row in owner_checks['commands'])
assert 'Ran 13 tests' in (BASE/'owner-guard-checks-01/owner-triage-contracts.log').read_text()
assert 'Ran 10 tests' in (BASE/'owner-guard-checks-01/publication-contracts.log').read_text()
cli_controls=list((BASE/'owner-guard-checks-01/triage-controls').rglob('process.json'));assert len(cli_controls)==7
pub=[]
for path in (BASE/'owner-guard-checks-01/publication-controls').rglob('commands.json'):pub.extend(json.loads(path.read_text()))
assert len(pub)==22 and sum(row['exitCode']!=0 for row in pub)==17
for path in existing_changes+new_files:
 target=BASE/'final-owner-inputs'/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((OWNER/path).read_bytes())
summary={'scope':__doc__,'ownerBase':before['ownerHead'],'authorizedOwnerFiles':current,
 'originalEvidence':{'uniqueIdentities':828,'observations':1498,'errorProblems':817,'happyProblems':681,'tracedCauses':225,'additionalHappyCauseGroups':11,'productionFilesIndependentlyVerified':23},
 'supplemental12812':{'additionalIdentities':1,'originalObservationCountChange':0,'actualCurrentLiteral':'baseline94/94 and literal94/94 pass; finite survivor only','hostedFinalJsonStatus':None,'allowedFreshProblemStates':['Survived','NoCoverage'],'exactIdentityOnly':True},
 'dispositions':dict(counts),'boundedAcceptedBreakdown':{'individualHashArithmeticCongruenceOnly':318,'boundedSourceOutputEquivalence':10,'detectedDestructiveTimeout14021':1},
 'finiteAssignedFollowups':214,'totalCauseGroups':237,'originalSealedCensus':{'detected':92,'survived':150,'compileRefusedNotKills':10},'additionalAdoptedHashControlKills':3,
 'verification':{'ownerTriageTestMethods':13,'finiteAllowedIdentityStatusCases':1546,'actualOwnerCliProcesses':7,'publicationTestMethods':10,'actualPublicationOrValidatorProcesses':22,'actualPublicationRefusals':17,'actionlintExit':0,'actualExistingCountPolicyChecks':6,'invalidActionAndMissingOwnerRefused':True,'originalReportsFreshAdmissionRefused':{'happy-path':95,'error-handling':95}},
 'persistence':{'ownerChecker':'eng/check-mutation-triage.py','mandatoryAggregateStep':'Validate finite mutation identities and statuses','ordering':'After unchanged strict BOTH validator; separate raw segment-success requirement remains','missingOrAmbiguousReportsRefuse':True,'audit':'artifacts/mutation/mutation-triage-admission.json','auditExplicitlyScannedAndUploadAllowed':True,'manifestRecords':14,'manifestBytes':(OWNER/(REL+'mutation-target-manifest.json')).stat().st_size,'followupLedgerBytes':(OWNER/(REL+'mutation-problem-followups.json')).stat().st_size,'allOwnerFilesBelowOneMiB':True},
 'preservation':{'preexistingOwnerInputsByteIdentical':len(before['files'])-len(existing_changes),'allCurrentCsharpAssertionsStillBound':True,'onlyManifestProblemMutantTriageChanged':True,'sharedPolicyAndStrictValidatorUnchanged':True,'fclP2AuthorizedDeltaPreserved':True,'rawThresholdsCohortsToolProvenanceProtectionsUnchanged':True},
 'cleanup':{'allScratchRootsGone':[row['scratch'] for row in receipts],'processesRetainingOwnedTMPDIR':owned,'allOwnedCommandsWaited':True},
 'limits':['No personal human review, new census authority, future native status or passing score is asserted.','318 arithmetic dispositions preserve only conditional equal-value/hash congruence; concrete distribution/external/performance and selected collection-quality effects remain explicitly bounded/open. Constant-array13541 and96 unproved field drops are deferred.','Private compile refusals are not kills; original observations are unchanged. Current native complete scores80/60, both segment exits and exact private provenance/BOTH validation remain mandatory.','Owner CLI positives/refusals use finite synthetic fixtures; original complete reports are retained/refused rather than requalified. No full mutation rerun, Git/remote or governance-authority edit occurred.'],
 'finishedUtc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime())}
(BASE/'summary.json').write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps({'records':829,'originalObservations':1498,'killTestAdded':95,'boundedAccepted':329,'deferred':405,'followups':214,'ownerTests':13,'publicationTests':10,'protectedInputFiles':summary['preservation']['preexistingOwnerInputsByteIdentical'],'allOwnedScratchGone':True}))
