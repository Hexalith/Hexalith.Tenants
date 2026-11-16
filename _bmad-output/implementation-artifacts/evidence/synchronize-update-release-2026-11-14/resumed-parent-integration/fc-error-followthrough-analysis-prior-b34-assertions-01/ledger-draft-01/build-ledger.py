"""Draft source-specific per-ID review ledger; never an accepted triage manifest."""
from pathlib import Path
import collections,hashlib,json,re,xml.etree.ElementTree as ET
BASE=Path(__file__).resolve().parent;PREVIOUS=BASE.parent/'fc-error-local-diagnostic/attempt-01'
index=json.loads((BASE/'source-index.json').read_text());report_path=PREVIOUS/'original-error-artifacts/reports/source-tools-error-handling.json';report=json.loads(report_path.read_text())
original={m['id']:(path,data,m) for path,data in report['files'].items() for m in data.get('mutants',[]) if m['status'] in ('Survived','NoCoverage','Timeout')}
cases={case['id']:case['name'] for data in report['testFiles'].values() for case in data.get('tests',[])}
previous=BASE.parent/'fc-partial-mutation-analysis'
controls={}
for directory in (previous,BASE/'behavior-controls-01',BASE/'behavior-controls-02'):
 if not directory.exists():continue
 for path in directory.rglob('*.trx'):
  match=re.match(r'(\d+)',path.relative_to(directory).parts[0])
  if not match:continue
  failures=[node.attrib.get('testName') for node in ET.parse(path).getroot().iter() if node.tag.endswith('UnitTestResult') and node.attrib.get('outcome')=='Failed']
  if failures:controls.setdefault(match[1],[]).append({'trx':str(path.relative_to(BASE.parent)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'failed_tests':failures})
# These proofs are exact at e0 and state the narrow domain; they are proposals, not approvals.
proofs={
 '14724':('duplicate boolean pattern','Both alternatives are the same constant: nameof(TypeCategory.Boolean) is "Boolean". A or A and A and A recognize identical strings, including null/fallback behavior.'),
 '14727':('duplicate collection pattern','Both alternatives are the same constant: nameof(TypeCategory.Collection) is "Collection". A or A and A and A recognize identical strings.'),
 '14651':('nonempty GroupBy group','Enumerable.GroupBy yields nonempty groups. First and FirstOrDefault return the identical first BadgeMappingEntry for every group reaching this call; null entries fail in the unchanged key selector before it.'),
 '15157':('substring algebra','For nonempty namespace without a dot, LastIndexOf is -1 and Substring(lastDot+1) is Substring(0), the original whole namespace. The empty/null guard is unchanged.'),
 '14347':('humanizer fallback redundancy','Humanize returns null only for null input, empty only for empty input; those fallback values equal the input. For all other input the original already selects humanized. This depends on exact e0 Humanize behavior.'),
 '14693':('humanizer whitespace fallback redundancy','Humanize preserves an all-whitespace input exactly and cannot turn a non-whitespace input into whitespace. Thus its whitespace fallback equals humanized at e0.'),
 '15060':('humanizer whitespace fallback redundancy','The base-name scalar has the same whitespace-preserving Humanize property: the selected fallback and humanized value coincide whenever the original guard is true.'),
 '13885':('private build-drift decision unobserved','Only private ValidateContractVersion consumes CompareContractVersion. Exact and BuildDrift both have ShouldReportDiagnostic=false and immediately return; the changed private Decision is never otherwise read.'),
 '13811':('monotone attribute detection','Removing break only rechecks remaining attributes; hasProjectionAttribute is monotone false-to-true and is never reset. Attribute inspection is side-effect-free for normal Roslyn symbols.'),
 '13689':('matched-attribute first iteration','The sole production caller is ForAttributeWithMetadataName for the exact marker name. context.Attributes contains matching attributes, so the first iteration sets markerAttr and breaks before the changed loop bound matters.'),
 '13690':('matched-attribute first iteration','The same matched-attribute first-iteration break means the increment/decrement expression is never reached for the generator-produced context.'),
 '14010':('empty namespace append','For null/global namespaces the unchanged loops execute zero iterations. Removing the early return appends the same empty text; it adds an allocation/work, whose timing impact is not qualified.'),
}
contracts={
 'AttributeParser':'Projection/command parse IR, diagnostic identity/message/severity/location, annotation normalization and supported property/static-assignment behavior.',
 'CommandParser':'Command admission, policy/target/default-value/destructive metadata, diagnostic content and inherited-property handling.',
 'ProjectionTemplateMarkerParser':'Template/projection admission, component context, qualified manifest identity, role and packed-version diagnostics.',
 'SourceTypeNameFormatter':'Compilable qualified source syntax, nullable/array/pointer spelling, extern-alias collection and termination.',
 'FieldTypeMapper':'Public admitted .NET/C# aliases, collection recognition and IR category spelling.',
 'CamelCaseHumanizer':'Visible labels and termination/bounds of acronym/digit/ASCII transitions.',
 'McpManifestTransform':'Protocol identifiers, visible labels, JSON schema field types and badge mapping selection.',
 'RazorModelTransform':'Visible labels, role/fallback diagnostics, status/date selection, ordering, display hints and state filtering.',
 'CommandFormTransform':'Visible enum option labels, truncation and null/empty fallback.',
 'CommandRendererTransform':'Renderer DTO generation and documented invalid-argument boundaries.',
 'RegistrationModelTransform':'Registration bounded-context fallback derived from namespace.',
 'SchemaFingerprintTransform':'Canonical negotiated schema family/metadata/ordering/nullability and structural SHA fingerprints; distinct from Object.GetHashCode.',
}
proposals={
 'AttributeParser':'Use valid source plus malformed editing/metadata inputs; assert final diagnostic identity/reason/location and emitted behavior, then repair source. Existing13-case private proposal covers a finite subset only.',
 'CommandParser':'Add both-cohort source fixtures for malformed/valid policy names, nested scope fallback, inherited/shadowed setters, destructive confirmation copy and allowed numeric DefaultValue conversions; assert compiler diagnostics and generated registration/form behavior.',
 'ProjectionTemplateMarkerParser':'Run the real generator on valid and invalid component/projection/context shapes, global/nested namespaces, duplicate/malformed markers, and exact/major/minor/build versions; assert manifest entries plus full diagnostic reasons.',
 'SourceTypeNameFormatter':'Compile emitted command/source syntax using extern-only and multiple aliases, escaped keywords, nested nullable arrays, generic arity and function pointers. Bound intentional nontermination in an isolated child; do not raise global timeouts.',
 'FieldTypeMapper':'Private ErrorFollowthroughBehaviorTests admits aliases/collections and rejects prefix lookalikes through public methods; remaining cases need source/metadata-to-renderer validation.',
 'CamelCaseHumanizer':'Private public-label boundary cases cover A/Z/z, short acronyms, final uppercase and digit transitions; preserve exact visible labels without asserting counters.',
 'McpManifestTransform':'Use parsed commands/projections with multiword names, all supported field types, duplicate badge entries and blank/escaped protocol parts; assert public descriptors and emitted manifest.',
 'RazorModelTransform':'Use multiple badged/unbadged enums, dates, duplicate priorities, exactly15/16 columns, CSV empties, non-default roles, explicit/plural labels and typed formats; assert emitted UI/diagnostics, not private helper values.',
 'CommandFormTransform':'Exercise humanized enum names at the truncation boundary through generated option labels; establish null/empty domain before classifying guard mutations.',
 'CommandRendererTransform':'Exercise the public invalid-argument seam if supported, otherwise review caller non-null guarantees and decide whether a focused contract test is appropriate.',
 'RegistrationModelTransform':'Exercise public registration context fallback on global/simple/nested namespaces; separately bound malformed manually constructed namespaces.',
 'SchemaFingerprintTransform':'Private complete Markdown/skill metadata and nullable-field material tests exercise public negotiated payloads/fingerprints; keep culture/EOL/order controls and cross-package negotiation tests.',
}
rows=[];groups={}
for mutant in index['mutants']:
 ident=mutant['id'];path,data,raw=original[ident];member=mutant['member'];typename=member.split('.')[0]
 member_row=next(row for row in index['members'] if row['path']==mutant['path'] and row['name']==member and row['startLine']==mutant['memberStartLine'])
 symbol=member_row['symbol']
 callers=[{'path':call['path'],'line':call['line'],'member':call['caller'],'expression':call['expression']} for call in index['calls'] if symbol and call['target']==symbol]
 bounds=['Current report is original e0 Error-only evidence, not695d or a complete dual-segment qualification.','No accepted owner/action/approval is assigned. A deferred-with-owner entry needs a real owner and follow-up agreed by parent.']
 if member.endswith('.GetHashCode'):
  cause='hash-array-loop-collision' if ident=='13541' else 'hash-pure-numeric-variation'
  effect='Object hash integer and collision distribution change. The mutation does not alter Equals or schema SHA material.'
  decision='unresolved-hash-consumer-and-distribution-review'
  rationale=('Changing i<items.Length to i>items.Length skips all elements and returns seed17: all arrays collide. Equal hashes for equal values still holds, but distribution/performance does not.' if ident=='13541' else 'Pure unchecked addition/subtraction or division by nonzero31, or replacing an existing hash contribution with constant0, remains a congruent function of values already compared by Equals. This is only a conditional hash-law proof, not numeric/output/performance equivalence.')
  bounds+=['Resolved production-source hash calls are nested hash methods; incremental-provider generic default comparers remain an implicit platform consumer.','Public external consumers, collision distribution/performance, legal input/null invariants, and model equality correctness are not exhaustively qualified.','Do not add exact-hash assertions or demand all unequal objects have unequal hashes solely to kill these mutants.']
  proposal='Audit equal-value/hash congruence, immutability and collection lookup semantics, then measure/inspect collision-sensitive real consumers. Parent must choose bounded equivalence or an owned performance/API follow-up; none is preselected.'
 elif ident in proofs:
  cause,rationale=proofs[ident];effect='No changed observable result under the exact proof domain; timing/external/future-source bounds remain explicit.';decision='bounded-equivalence-candidate-for-parent-review';proposal='Independently review the cited source/call graph and proof domain against the complete original report before any accepted-equivalent record.'
  bounds+=['Proof is source-specific at e0; it is not a human acceptance and does not generalize after caller/helper contracts change.']
 elif ident=='14021':
  cause='namespace-loop-progress-removal';effect='Removing parts.Pop leaves the nonempty while condition unchanged forever.';rationale='Original report records Timeout, with all coverage IDs retained. The removed loop body contains its sole progress step; do not treat the debug is-success line as final status.';decision='detected-timeout-requires-truthful-review';proposal='Retain Timeout14021 and its source/coverage evidence. If further evidence is needed, use one isolated child with a scaled finite deadline and owned cleanup; never change global mutation timeout policy.'
 elif '.Equals' in member or 'operator ' in member:
  cause='value-comparison-contract';effect='Typed/boxed/null/reflexive equality or inequality can change, affecting generic comparer/cache/collection decisions.';rationale='This changes comparison semantics, unlike numeric hash variation. Null/absent/reference-alias and boxed other-type paths need genuine consumer contract coverage.';decision='genuine-comparison-gap-test-proposal';proposal='Private public generic-comparer/array controls cover selected IDs. Add focused cache/collection scenarios and compare repeated/fresh generation only where consumer harm is established.'
 elif '..ctor' in member:
  cause='legacy-constructor-default';effect='Public constructor fallback/default metadata changes; actual parser callers may pass the full value explicitly.';rationale='Do not infer current generator harm without checking overload selection. Public compatibility/default behavior can still differ for direct IR clients.';decision='unresolved-public-default-and-caller-boundary';proposal='Inspect full-vs-legacy constructor call sites and exercise the real renderer/diagnostic consumer using an omitted default only if that API contract is supported.'
 else:
  cause=typename+'-'+member.split('.',1)[1]+'-'+('diagnostic-text' if mutant['mutator']=='String mutation' and any(word in mutant['original'] for word in ('Warning','Error','Info','What:','Fix:','Expected:','Invalid','falling back')) else mutant['mutator'])
  effect=contracts.get(typename,'Public model/property behavior changes if the mutated branch is reachable.')
  rationale='Exact changed expression: '+mutant['original'].replace('\r\n',' ')[:180]+' -> '+mutant['replacement'].replace('\r\n',' ')[:180]+'. Reachability and the relevant observable contract must be checked at this member/caller boundary.'
  decision='source-inspected-gap-or-domain-boundary-proposal'
  proposal=proposals.get(typename,'Exercise the documented public property/model result through its real consumer; keep unused/internal-only behavior and external API assumptions explicit.')
 if ident in controls:
  decision='demonstrated-contract-gap-private-test-candidate';rationale+=' The linked private case passes original e0 and fails this literal variant; original report status is preserved.'
 key=mutant['path']+'#'+member+'#'+cause
 if key not in groups:groups[key]={'id':'C'+str(len(groups)+1).zfill(3),'path':mutant['path'],'member':member,'cause':cause,'contract_effect':effect,'proposal':proposal,'ids':[],'status_counts':collections.Counter()}
 group=groups[key];group['ids'].append(ident);group['status_counts'][raw['status']]+=1
 rows.append({'id':ident,'observed_status':raw['status'],'path':mutant['path'],'source_sha256':hashlib.sha256(data['source'].encode()).hexdigest(),'location':raw['location'],'original':mutant['original'],'replacement':mutant['replacement'],'mutator':mutant['mutator'],'member':member,'member_signature':symbol,'member_lines':[mutant['memberStartLine'],mutant['memberEndLine']],'direct_source_callers':callers,'caller_bound':'All real invocation expressions in SourceTools+Contracts resolve; nameof intrinsics are not calls. Property/operator access, implicit generic comparers and external consumers need the separate bounds described here.','coverage_count':len(raw.get('coveredBy',[])),'covered_case_examples':[cases[test] for test in raw.get('coveredBy',[])[:4]],'cause_group':group['id'],'contract_effect':effect,'evidence_and_rationale':rationale,'proposed_disposition':decision,'proposed_followup':proposal,'bounds_and_unknowns':bounds,'private_controls':controls.get(ident,[]),'owner':None,'owner_approval':False,'accepted_action':None})
assert len(rows)==817 and len({row['id'] for row in rows})==817 and set(original)=={row['id'] for row in rows}
for group in groups.values():group['status_counts']=dict(group['status_counts'])
packet={'scope':__doc__,'source':'e0eef14ddbbcd03823863a4f417a4b07f6f5bafc','report_sha256':hashlib.sha256(report_path.read_bytes()).hexdigest(),'count':len(rows),'observed_status_counts':dict(collections.Counter(row['observed_status'] for row in rows)),'source_index_sha256':hashlib.sha256((BASE/'source-index.json').read_bytes()).hexdigest(),'semantic_index_bounds':'SDK Roslyn5.12 syntax/source analysis, not runtime qualification. Four missing generated STJ-context members are recorded; every non-nameof invocation resolves. Test/external generic dispatch and getters are not disguised as complete global consumer proof.','mutants':rows}
(BASE/'per-id-ledger.json').write_text(json.dumps(packet,indent=2)+'\n')
(BASE/'cause-groups.json').write_text(json.dumps({'source':packet['source'],'scope':'Review candidates only; no owner acceptance, aggregate triage admission or altered report status.','groups':list(groups.values())},indent=2)+'\n')
print('ids',len(rows),'groups',len(groups),'dispositions',dict(collections.Counter(row['proposed_disposition'] for row in rows)))
