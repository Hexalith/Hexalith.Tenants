"""Bounded literal-mutation parser observations; not a Stryker score or qualification run."""
import hashlib,io,json,os,pathlib,shutil,subprocess,tarfile
EVIDENCE=pathlib.Path(__file__).resolve().parent
ROOT=EVIDENCE.parents[5]
OWNER=ROOT/'references/Hexalith.FrontComposer'
WORK=pathlib.Path(os.environ['TMPDIR'])/'FrontComposer'
SOURCE='e0eef14ddbbcd03823863a4f417a4b07f6f5bafc'
BUILDS='73e60af8a1fcdf3f06726d5d1b6e1199cce778ee'
records={'scope':__doc__,'source':SOURCE,'builds':BUILDS,'commands':[],'workspace':str(WORK)}
if (EVIDENCE/'reproduction.json').exists(): raise SystemExit('Refusing to overwrite reproduction evidence')
def archive(repository,revision,target):
    target.mkdir(parents=True,exist_ok=True)
    with tarfile.open(fileobj=io.BytesIO(subprocess.check_output(['git','archive',revision],cwd=repository))) as source:
        source.extractall(target,members=[item for item in source.getmembers() if not item.name.startswith(('references/','.agents/skills/','.codex/skills/'))],filter='data')
archive(OWNER,SOURCE,WORK)
archive(ROOT/'references/Hexalith.Builds',BUILDS,WORK/'references/Hexalith.Builds')
probe=WORK/'analysis/Probe';probe.mkdir(parents=True)
shutil.copy2(EVIDENCE/'Probe.cs',probe/'Program.cs')
(probe/'Probe.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework><OutputType>Exe</OutputType><IsPackable>false</IsPackable></PropertyGroup><ItemGroup><ProjectReference Include="../../src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj"/><PackageReference Include="Microsoft.CodeAnalysis.CSharp"/></ItemGroup></Project>''')
parser=WORK/'src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs'
original=parser.read_text();records['parser_git_bytes_sha256']=hashlib.sha256(parser.read_bytes()).hexdigest()
records['probe_sha256']=hashlib.sha256((EVIDENCE/'Probe.cs').read_bytes()).hexdigest()
records['runner_sha256']=hashlib.sha256(pathlib.Path(__file__).read_bytes()).hexdigest()
os.environ.update(NUGET_PACKAGES=str(pathlib.Path(os.environ['TMPDIR'])/'packages'),NUGET_HTTP_CACHE_PATH=str(pathlib.Path(os.environ['TMPDIR'])/'http-cache'),DOTNET_CLI_HOME=str(pathlib.Path(os.environ['TMPDIR'])/'dotnet-home'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0')
def run(name,argv):
    log=EVIDENCE/(name+'.log')
    with log.open('x') as output: result=subprocess.run(argv,cwd=WORK,stdout=output,stderr=subprocess.STDOUT)
    records['commands'].append({'name':name,'command':argv,'exit_code':result.returncode,'log':log.name,'sha256':hashlib.sha256(log.read_bytes()).hexdigest()})
    (EVIDENCE/'reproduction.json').write_text(json.dumps(records,indent=2)+'\n')
    print(name,result.returncode,flush=True)
    if result.returncode: raise SystemExit(result.returncode)
def scoped(text,method,needle,replacement):
    start=text.index(method);tail=text[start:]
    if needle not in tail: raise ValueError(needle)
    return text[:start]+tail.replace(needle,replacement,1)
variants={
'baseline':lambda text:text,
'12455':lambda text:text.replace('Location location = field.Locations.FirstOrDefault() ?? Location.None;','Location location = Location.None;'),
'12457':lambda text:text.replace('string fieldFilePath = location.SourceTree?.FilePath ?? filePath;','string fieldFilePath = filePath;'),
'12456':lambda text:text.replace('Location location = field.Locations.FirstOrDefault() ?? Location.None;','Location location = field.Locations.First() ?? Location.None;'),
'11924':lambda text:text.replace('outerTds.Modifiers.Any','outerTds.Modifiers.All'),
'11921':lambda text:text.replace('containingType.DeclaringSyntaxReferences.FirstOrDefault()','containingType.DeclaringSyntaxReferences.First()'),
'12009':lambda text:text.replace('statusProperty ??= property;','statusProperty = property;'),
'12376':lambda text:scoped(text,'private static string? ParseDescriptionAttribute','attr.ConstructorArguments.Length > 0','attr.ConstructorArguments.Length >= 0'),
'12133':lambda text:scoped(text,'private static bool HasErrorObsoleteAttribute','return false;','return true;'),
'12234':lambda text:scoped(text,'private static FieldDisplayFormat ParseDisplayFormat','DescribeUnsupportedType(propertyType, propertyType.TypeKind == TypeKind.Enum)','DescribeUnsupportedType(propertyType, propertyType.TypeKind != TypeKind.Enum)'),
'12460':lambda text:text.replace('"Invalid argument for attribute \'ProjectionBadge\' on enum member \'{0}\': slot must be a defined BadgeSlot value"','""'),
'12461':lambda text:scoped(text,'private static EquatableArray<BadgeMappingEntry> ParseBadgeMappings','"Warning"','""'),
'12303':lambda text:scoped(text,'private static string? ParseProjectionEmptyStateCta','attr.ConstructorArguments.Length > 0','attr.ConstructorArguments.Length >= 0'),
}
for mutant,apply in variants.items():
    changed=apply(original)
    if mutant!='baseline': assert changed!=original
    parser.write_text(changed)
    records.setdefault('private_variants',{})[mutant]=hashlib.sha256(parser.read_bytes()).hexdigest()
    run(mutant+'-build',['dotnet','build',str(probe/'Probe.csproj'),'-c','Release','-p:UseHexalithProjectReferences=false','-p:GeneratePackageOnBuild=false','-p:RunAnalyzers=false','-p:UseSharedCompilation=false','-m:1','-nodeReuse:false'])
    run(mutant+'-observations',['dotnet',str(probe/'bin/Release/net11.0/Probe.dll')])
print('Private bounded observations complete',flush=True)
