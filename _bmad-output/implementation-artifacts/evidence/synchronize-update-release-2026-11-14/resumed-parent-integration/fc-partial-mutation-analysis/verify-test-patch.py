"""Focused proposed test cases against original e0 and literal mutant variants; no hosted mutation qualification claim."""
import hashlib,io,json,os,pathlib,shutil,subprocess,tarfile
EVIDENCE=pathlib.Path(__file__).resolve().parent
ROOT=EVIDENCE.parents[5]
OWNER=ROOT/'references/Hexalith.FrontComposer'
WORK=pathlib.Path(os.environ['TMPDIR'])/'FrontComposer'
SOURCE='e0eef14ddbbcd03823863a4f417a4b07f6f5bafc'
BUILDS='73e60af8a1fcdf3f06726d5d1b6e1199cce778ee'
records={'scope':__doc__,'source':SOURCE,'builds':BUILDS,'commands':[],'workspace':str(WORK)}
if (EVIDENCE/'test-patch-verification.json').exists(): raise SystemExit('Refusing to overwrite reproduction evidence')
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
def run(name,argv,expected=0):
    log=EVIDENCE/(name+'.log')
    with log.open('x') as output: result=subprocess.run(argv,cwd=WORK,stdout=output,stderr=subprocess.STDOUT)
    records['commands'].append({'name':name,'command':argv,'exit_code':result.returncode,'expected_exit_code':expected,'log':log.name,'sha256':hashlib.sha256(log.read_bytes()).hexdigest()})
    (EVIDENCE/'test-patch-verification.json').write_text(json.dumps(records,indent=2)+'\n')
    print(name,result.returncode,flush=True)
    if result.returncode!=expected: raise SystemExit(result.returncode or 1)
def scoped(text,method,needle,replacement):
    start=text.index(method);tail=text[start:]
    if needle not in tail: raise ValueError(needle)
    return text[:start]+tail.replace(needle,replacement,1)

patch=EVIDENCE/'ProjectionParserSurvivorRegressionTests.cs'
shutil.copy2(patch,WORK/'tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation'/patch.name)
records['proposed_test_source_sha256']=hashlib.sha256(patch.read_bytes()).hexdigest()
project=WORK/'tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj'
variants={
'baseline':lambda text:text,
'12455':lambda text:text.replace('Location location = field.Locations.FirstOrDefault() ?? Location.None;','Location location = Location.None;'),
'12457':lambda text:text.replace('string fieldFilePath = location.SourceTree?.FilePath ?? filePath;','string fieldFilePath = filePath;'),
'11924':lambda text:text.replace('outerTds.Modifiers.Any','outerTds.Modifiers.All'),
'12009':lambda text:text.replace('statusProperty ??= property;','statusProperty = property;'),
'12036':lambda text:text.replace('return string.Join(", ", sorted);','return string.Join("", sorted);'),
'12376':lambda text:scoped(text,'private static string? ParseDescriptionAttribute','attr.ConstructorArguments.Length > 0','attr.ConstructorArguments.Length >= 0'),
'12234':lambda text:scoped(text,'private static FieldDisplayFormat ParseDisplayFormat','DescribeUnsupportedType(propertyType, propertyType.TypeKind == TypeKind.Enum)','DescribeUnsupportedType(propertyType, propertyType.TypeKind != TypeKind.Enum)'),
'12251':lambda text:scoped(text,'if (IsCurrencyCompatible(resolvedTypeName))','DescribeUnsupportedType(propertyType, propertyType.TypeKind == TypeKind.Enum)','DescribeUnsupportedType(propertyType, propertyType.TypeKind != TypeKind.Enum)'),
'12460':lambda text:text.replace('"Invalid argument for attribute \'ProjectionBadge\' on enum member \'{0}\': slot must be a defined BadgeSlot value"','""'),
'12461':lambda text:scoped(text,'private static EquatableArray<BadgeMappingEntry> ParseBadgeMappings','"Warning"','""'),
'12303':lambda text:scoped(text,'private static string? ParseProjectionEmptyStateCta','attr.ConstructorArguments.Length > 0','attr.ConstructorArguments.Length >= 0'),
'12280':lambda text:scoped(text,'private static (string? GroupName, string? Description) ParseProjectionFieldGroup','attr.ConstructorArguments.Length > 0','attr.ConstructorArguments.Length >= 0'),
'12334':lambda text:scoped(text,'private static int? ParseColumnPriority','attr.ConstructorArguments.Length > 0','attr.ConstructorArguments.Length >= 0'),
'12192':lambda text:scoped(text,'private static FieldDisplayFormat ParseDisplayFormat','attr.ConstructorArguments.Length > 0','attr.ConstructorArguments.Length >= 0'),
'12207':lambda text:scoped(text,'private static FieldDisplayFormat ParseDisplayFormat','ctorArg.Value is null\n                            ? "null"','false\n                            ? "null"'),
}
for mutant,apply in variants.items():
    changed=apply(original)
    if mutant!='baseline': assert changed!=original
    parser.write_text(changed)
    records.setdefault('private_variants',{})[mutant]=hashlib.sha256(parser.read_bytes()).hexdigest()
    run(mutant+'-test-patch-build',['dotnet','build',str(project),'-c','Release','-p:UseHexalithProjectReferences=false','-p:UseMicrosoftTestingPlatformRunner=true','-p:GeneratePackageOnBuild=false','-p:RunAnalyzers=false','-p:UseSharedCompilation=false','-m:1','-nodeReuse:false'])
    report=EVIDENCE/(mutant+'-test-patch-trx')
    run(mutant+'-test-patch-run',['dotnet','test','--project',str(project),'--no-build','--configuration','Release','--filter-class','Hexalith.FrontComposer.SourceTools.Tests.Mutation.ProjectionParserSurvivorRegressionTests','--minimum-expected-tests','13','--fail-skips','on','--results-directory',str(report),'--report-xunit-trx','--report-xunit-trx-filename','tests.trx','-p:UseHexalithProjectReferences=false','-p:UseMicrosoftTestingPlatformRunner=true'],expected=0 if mutant=='baseline' else 2)
print('Private proposed tests complete',flush=True)
