"""Compute only existing source seals from exact selected committed objects."""
import copy
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys

ROOT=Path(__file__).resolve().parents[6]
OWNER=ROOT/'references/Hexalith.FrontComposer'
PACKET=Path(__file__).resolve().parent
sys.path.insert(0,str(OWNER/'eng'))
import dependency_graph as dg


def require(condition,message):
    if not condition:raise ValueError(message)


def main():
    candidate='ebd3f76dc0b320d455917c593ff86526b58d0c77'
    require(subprocess.check_output(['git','rev-parse','HEAD'],cwd=OWNER,text=True).strip()==candidate,'Owner moved.')
    policy=dg.load_policy(OWNER/'eng/dependency-graph-policy.json')
    identity='github.com/hexalith/hexalith.platform'
    old=copy.deepcopy(policy['consumer_source_projections'][identity])
    direct=dg._owner_edges(OWNER,candidate,'github.com/hexalith/hexalith.frontcomposer',1,policy,OWNER)
    selected=next(edge['commit'] for edge in direct if edge['repository']==identity)
    platform=ROOT/'references/Hexalith.Platform'
    raw=dg.read_blob(platform,selected,'.gitmodules',1048576,'selected Platform inventory')
    modules=dg.read_gitmodules(platform,selected,1048576)
    links=dg.list_gitlinks(platform,selected,67108864)
    by_path={fields['path']:dg.normalize_identity(fields['url'],'selected Platform inventory') for fields in modules.values()}
    require(set(links)==set(by_path),'Platform inventory does not match its mappings.')
    inventory=[{'owner_repository':identity,'owner_commit':selected,'path':path,'repository':by_path[path],'commit':commit,'depth':2}
               for path,commit in sorted(links.items())]
    canonical={path:hashlib.sha256(dg.read_blob(platform,selected,path,16777216,'canonical Platform source')).hexdigest() for path in old['source_files']}
    require(canonical==old['source_files'],'Canonical source changed; this finite reseal cannot alter source authority.')
    untrusted=sorted({edge['repository'] for edge in inventory}-{row['identity'] for row in policy['trusted_identities']})
    require(untrusted==old['inventory_only_identities'],'Inventory-only identity authority changed.')
    configuration=dg.consumer_configuration(OWNER,candidate,policy)
    row=copy.deepcopy(old)
    row.update({'owner_commit':selected,'gitmodules_sha256':hashlib.sha256(raw).hexdigest(),
                'inventory_sha256':dg.canonical_digest(inventory),
                'builds_commit':next(edge['commit'] for edge in direct if edge['repository']==policy['builds_identity']),
                'root_selectors_sha256':dg.canonical_digest([{key:edge[key] for key in ('path','repository','commit')} for edge in direct]),
                'consumer_configuration_sha256':dg.canonical_digest(configuration)})
    policy['consumer_source_projections'][identity]=row
    dg.assert_policy_well_formed(policy)
    raw_policy=(OWNER/'eng/dependency-graph-policy.json').read_bytes()
    prefix,projection=raw_policy.split(b'  "consumer_source_projections":',1)
    for field,value in row.items():
        if value!=old[field]:
            before=json.dumps(old[field]).encode();after=json.dumps(value).encode()
            require(projection.count(before)==1,'Source field is ambiguous: '+field)
            projection=projection.replace(before,after,1)
    (OWNER/'eng/dependency-graph-policy.json').write_bytes(prefix+b'  "consumer_source_projections":'+projection)
    manifest_path=OWNER/'tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-target-manifest.json'
    before=manifest_path.read_bytes();manifest=json.loads(before)
    entries=[{'path':'src/Hexalith.FrontComposer.SourceTools/Transforms/'+name+'.cs',
              'reason':'Enum declarations with implicit ordinals only; no executable expressions or Stryker mutation sites. Native reports contain no active mutants for this inventory file. Executable uses remain in the unchanged mutation targets.',
              'owner':'Story 10-4 SourceTools quality/release-automation'}
             for name in ('FormFieldTypeCategory','ProjectionRenderStrategy','TypeCategory')]
    for entry in entries:
        require(not any(item['path']==entry['path'] for item in manifest['explicitExclusions']),'Enum accounting already present.')
    marker=b'  "triageActions":'
    position=before.index(marker)
    head=before[:position]
    # Insert only into the existing finite explicitExclusions array, retaining all other bytes.
    close=head.rfind(b'  ],')
    require(close!=-1,'Cannot locate explicit exclusions end.')
    eol=b'\r\n' if b'\r\n' in before else b'\n'
    preceding=head[:close].rstrip(b'\r\n')
    require(preceding.endswith(b'    }'),'Unexpected exclusion shape.')
    addition=json.dumps(entries,indent=2).splitlines()[1:-1]
    rendered=eol.join(('  '+line).encode() for line in addition)
    changed=preceding+b','+eol+rendered+eol+head[close:]+before[position:]
    expected=copy.deepcopy(manifest);expected['explicitExclusions'].extend(entries)
    require(json.loads(changed)==expected,'Manifest edit changed unrelated metadata.')
    manifest_path.write_bytes(changed)
    evidence={'source':candidate,'scope':'Uncommitted finite inventory/source-seal repair, not active-policy or native qualification.',
              'old_projection':old,'selected_projection':row,'changed_fields':[key for key in row if row[key]!=old[key]],
              'root_inventory':direct,'platform_inventory':inventory,'consumer_configuration':configuration,
              'canonical_source_sha256':canonical,'enum_inventory_entries':entries,
              'selected_builds_catalog_sha256':hashlib.sha256(dg.read_blob(ROOT/'references/Hexalith.Builds',row['builds_commit'],'Props/Directory.Packages.props',4194304,'selected root catalog')).hexdigest()}
    with (PACKET/'computed-source-seals.json').open('x') as output:output.write(json.dumps(evidence,indent=2)+'\n')
    print(json.dumps({'changed_fields':evidence['changed_fields'],'selected_platform':selected,'selected_builds':row['builds_commit'],'enum_entries':len(entries)}))


if __name__=='__main__':main()
