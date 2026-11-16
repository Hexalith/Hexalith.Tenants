"""Observe exact selected catalog defaults with existing read-only MSBuild evaluation."""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[6]
OWNER=ROOT/'references/Hexalith.FrontComposer'
PACKET=Path(__file__).resolve().parent/'sdk-selection-analysis'
BUILDS=ROOT/'references/Hexalith.Builds'
SELECTED='65fd22f37b9fad64b595841923cdbe050563ab99'


def require(condition,message):
    if not condition:raise ValueError(message)


def blob(path):return subprocess.check_output(['git','show',SELECTED+':'+path],cwd=BUILDS)


def main():
    PACKET.mkdir(exist_ok=False)
    scratch=Path(os.environ['TMPDIR'])/'catalog';(scratch/'Props').mkdir(parents=True)
    sources={}
    for path in ['Props/Directory.Packages.props','Props/Logging.Build.targets','Tools/evaluated_catalog.py']:
        raw=blob(path);target=scratch/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw)
        sources[path]=hashlib.sha256(raw).hexdigest()
    (scratch/'global.json').write_bytes((OWNER/'global.json').read_bytes())
    os.environ.update({'DOTNET_CLI_HOME':str(Path(os.environ['TMPDIR'])/'dotnet-home'),'DOTNET_CLI_TELEMETRY_OPTOUT':'1','DOTNET_SKIP_FIRST_TIME_EXPERIENCE':'1','MSBUILDDISABLENODEREUSE':'1','DOTNET_CLI_USE_MSBUILD_SERVER':'0','NUGET_PACKAGES':str(Path(os.environ['TMPDIR'])/'packages')})
    spec=importlib.util.spec_from_file_location('selected_evaluation',scratch/'Tools/evaluated_catalog.py');evaluation=importlib.util.module_from_spec(spec);spec.loader.exec_module(evaluation)
    spec=importlib.util.spec_from_file_location('unchanged_graph',OWNER/'eng/dependency_graph.py');graph=importlib.util.module_from_spec(spec);sys.path.insert(0,str(OWNER/'eng'));spec.loader.exec_module(graph)
    root=ET.fromstring((scratch/'Props/Directory.Packages.props').read_bytes())
    names=['HexalithEventStoreVersion','HexalithMemoriesVersion','HexalithTenantsVersion','HexalithMcpCliVersion','HexalithOpenApiVersion','HexalithAspNetCoreOpenApiVersion']
    rows={name:[{'value':element.text,'attributes':element.attrib} for element in root.iter(name)] for name in names}
    selectors={}
    for name,expected in graph._SDK42_CURRENT_PROJECTS.items():
        clauses=re.split(r'\s+or\s+',list(root.iter(name))[1].attrib['Condition'].strip())
        projects=[re.fullmatch(r"\$\(MSBuildProjectName\.Equals\('([A-Za-z0-9._-]+)'\)\)",clause).group(1) for clause in clauses]
        require(set(projects)==expected and len(projects)==len(expected),'Selected SDK4.2 subject inventory differs.')
        selectors[name]={'count':len(projects),'subjects':sorted(projects),'matches_existing_frozen_subjects':True}
    original_run=evaluation.subprocess.run;commands=[]
    def observed_run(command,*args,**kwargs):
        result=original_run(command,*args,**kwargs);index=len(commands)+1
        log=PACKET/f'msbuild-{index:02}.log';log.write_text(result.stdout+'\n'+result.stderr)
        commands.append({'command':command,'exit_code':result.returncode,'log':log.name,'log_sha256':hashlib.sha256(log.read_bytes()).hexdigest()})
        (PACKET/'commands.json').write_text(json.dumps(commands,indent=2)+'\n')
        return result
    evaluation.subprocess.run=observed_run
    cases=[]
    for project in ['Hexalith.FrontComposer.Shell','Hexalith.Platform.Protection','Hexalith.Parties','Hexalith.Builds.Module.AppHost']:
        properties,versions=evaluation.evaluate_catalog(scratch/'Props/Directory.Packages.props',configuration='Release',consumer_project_name=project)
        cases.append({'project':project,'mode':'package','selected':{name:properties.get(name) for name in names}})
    # Use the existing evaluator's supported argv precedence for source-mode observations;
    # these are evaluation-only -getProperty calls, never restore/build/target execution.
    for project,extra in [('Hexalith.Platform.Protection',[]),('Hexalith.Platform.Protection',['-p:HexalithEventStoreFromSource=false','-p:HexalithMemoriesFromSource=false','-p:HexalithTenantsFromSource=false','-p:HexalithMcpCliFromSource=false'])]:
        wrapper=scratch/(project+'.proj');wrapper.write_text('<Project><Import Project="'+str(scratch/'Props/Directory.Packages.props')+'" /></Project>')
        properties=json.loads(evaluation._run(wrapper,['-p:UseHexalithProjectReferences=true',*extra,'-getProperty:'+','.join(names)],neutral=True,configuration='Release',working_directory=scratch))['Properties']
        cases.append({'project':project,'mode':'source' if not extra else 'source-with-explicit-package-optouts','selected':properties})
    require(cases[0]['selected']['HexalithEventStoreVersion']=='5.1.1','FrontComposer package default differs.')
    require(all(case['selected']['HexalithEventStoreVersion']=='4.2.0' for case in cases[1:4]),'A retained package role changed.')
    require(cases[4]['selected']['HexalithEventStoreVersion']=='5.1.1' and cases[5]['selected']['HexalithEventStoreVersion']=='4.2.0','Source/package guards differ.')
    refusals=[]
    for name in ['HexalithEventStoreVersion','HexalithMemoriesVersion','HexalithTenantsVersion']:
        try:graph.assert_selected_catalog_property_shape(root,name,'exact selected65fd',allow_eventstore_qualification=True,catalog_source=(BUILDS,SELECTED,4194304))
        except graph.GraphError as error:refusals.append({'property':name,'refusal':str(error)})
        else:raise ValueError('Unexpected existing profile admission.')
    # Configuration cannot bypass the closed proof: strict literal-property validation
    # also refuses the two declarations, even if5.1.1 is written as its expected value.
    try:graph.assert_selected_catalog_property(root,'HexalithEventStoreVersion','5.1.1','existing literal-policy path')
    except graph.GraphError as error:refusals.append({'property':'literal-policy-configuration','refusal':str(error)})
    else:raise ValueError('Unexpected policy-only conditional admission.')
    result={'scope':'Read-only proposal evidence, no owner code/profile/catalog/selector edits or runtime qualification.',
            'selected_builds':SELECTED,'source_sha256':sources,'graph_sha256':hashlib.sha256((OWNER/'eng/dependency_graph.py').read_bytes()).hexdigest(),
            'rows':rows,'selectors':selectors,'observed_selections':cases,'existing_refusals':refusals,
            'existing_profile_keys':sorted(graph._PROFILE_KEYS),'existing_owner_check_keys':sorted(graph._OWNER_CHECK_KEYS)}
    (PACKET/'observations.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({'evaluated_cases':len(cases),'native_msbuild_commands':len(commands),'all_exits_zero':all(command['exit_code']==0 for command in commands),'existing_refusals':len(refusals)}))


if __name__=='__main__':main()
