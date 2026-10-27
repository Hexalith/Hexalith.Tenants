"""Real Dapr/HTTPS read qualification, including an exact one-call 503 retry guard.

Requires the existing CI Dapr runtime, Docker and openssl. A temporary Sentry executable is
copied from the matching runtime image; credentials, listeners and sidecars are isolated.
--mutate-retries must fail with four backend calls and proves the retry guard is effective.
"""
from pathlib import Path
import os,subprocess,socket,time,ssl,threading,json,urllib.request,urllib.error,hashlib,signal,shutil,tempfile,argparse,re,uuid
from http.server import ThreadingHTTPServer,BaseHTTPRequestHandler
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--output',type=Path,required=True)
parser.add_argument('--daprd',type=Path)
parser.add_argument('--mutate-retries',action='store_true')
args=parser.parse_args()
REPO=Path(__file__).resolve().parents[2]
daprd=args.daprd or Path(shutil.which('daprd') or Path(os.environ.get('HEXALITH_DAPR_HOME',str(Path.home())))/'.dapr/bin/daprd')
version=re.search(r'\d+\.\d+\.\d+',subprocess.check_output([str(daprd),'--version'],text=True)).group()
workspace=tempfile.TemporaryDirectory(prefix='tenants-dapr-retries-')
ROOT=Path(workspace.name)
namespace='tenants-retries-'+uuid.uuid4().hex[:12]
receiver_alias='tenants-api-'+uuid.uuid4().hex[:12]
backend=None
processes=[]; files=[]; results=[]; counts={}
def port():
 with socket.socket() as s:s.bind(('127.0.0.1',0));return s.getsockname()[1]
def start(name,args,env=None):
 f=(ROOT/(name+'.log')).open('w');files.append(f)
 p=subprocess.Popen(args,stdout=f,stderr=subprocess.STDOUT,env=env);processes.append(p);return p
def wait_for(predicate,timeout=35):
 until=time.monotonic()+timeout
 while time.monotonic()<until:
  if predicate():return
  time.sleep(.1)
 raise RuntimeError('readiness deadline')
def request(url,method='GET',headers=None):
 req=urllib.request.Request(url,data=b'',method=method,headers=headers or {})
 try:
  with urllib.request.urlopen(req,timeout=12) as r:return r.status,{k.lower():v for k,v in r.headers.items()},r.read()
 except urllib.error.HTTPError as e:return e.code,{k.lower():v for k,v in e.headers.items()},e.read()
def healthy(p):
 try:return request('http://127.0.0.1:'+str(p)+'/v1.0/healthz/outbound')[0]==204
 except Exception:return False
class Backend(BaseHTTPRequestHandler):
 def do_GET(self):
  key=(self.command,self.path);counts[key]=counts.get(key,0)+1
  if self.path=='/api/tenants/failure': self.send_response(503);self.end_headers();self.wfile.write(b'controlled failure');return
  if self.headers.get('If-None-Match')=='"dw144"':self.send_response(304)
  else:self.send_response(200)
  self.send_header('ETag','"dw144"');self.send_header('X-Hexalith-Projection-Version','17');self.send_header('X-Hexalith-Is-Stale','false');self.send_header('X-Hexalith-Served-At','2026-10-26T17:00:00Z');self.end_headers()
  if self.headers.get('If-None-Match')!='"dw144"':self.wfile.write(json.dumps({'path':self.path,'authorizationForwarded':self.headers.get('Authorization')=='Bearer dw144-sentinel','encoding':self.headers.get('X-Hexalith-Route-Encoding')}).encode())
 def do_POST(self):self.send_response(200);self.end_headers()
 def log_message(self,*args):pass
try:
 # Reuse the runtime/container setup already installed by the blocking CI lane.
 container=subprocess.check_output(['docker','create','daprio/sentry:'+version,'/sentry'],text=True).strip()
 try:
  subprocess.run(['docker','cp',container+':/sentry',str(ROOT/'sentry')],check=True,stdout=subprocess.DEVNULL)
 finally:
  subprocess.run(['docker','rm','-f',container],check=True,stdout=subprocess.DEVNULL)
 (ROOT/'sentry').chmod(0o700)
 certs=ROOT/'certs';certs.mkdir(exist_ok=True)
 cfg=ROOT/'mtls.yaml';cfg.write_text('apiVersion: dapr.io/v1alpha1\nkind: Configuration\nmetadata:\n  name: dw144\nspec:\n  mtls:\n    enabled: true\n  accessControl:\n    defaultAction: deny\n    trustDomain: hexalith.io\n')
 sentryport=port(); baseenv=os.environ.copy();baseenv['NAMESPACE']=namespace
 start('sentry',[str(ROOT/'sentry'),'--issuer-credentials',str(certs),'--trust-domain','hexalith.io','--port',str(sentryport),'--healthz-port',str(port()),'--metrics-port',str(port()),'--config',str(cfg)],baseenv)
 wait_for(lambda:(certs/'issuer.key').exists())
 env=baseenv|{'DAPR_TRUST_ANCHORS':(certs/'ca.crt').read_text(),'DAPR_CERT_CHAIN':(certs/'issuer.crt').read_text(),'DAPR_CERT_KEY':(certs/'issuer.key').read_text(),'DAPR_HOST_IP':'127.0.0.1'}
 original=(REPO/'deploy/dapr/accesscontrol.tenants-api.yaml').read_text()
 receiver=ROOT/'receiver.yaml';receiver.write_text(original.replace('{env:DAPR_TRUST_DOMAIN|hexalith.io}','public').replace('{env:DAPR_NAMESPACE|hexalith}',namespace))
 resources=ROOT/'resources';resources.mkdir(exist_ok=True);resiliency=(REPO/'deploy/dapr/resiliency.yaml').read_text().replace('      tenants-api:', '      '+receiver_alias+':');resiliency=resiliency.replace('maxRetries: 0','maxRetries: 3',1) if args.mutate_retries else resiliency;(resources/'resiliency.yaml').write_text(resiliency)
 # Both new readers scan the AppHost directory; Memories-only capabilities must stay invisible.
 (ROOT/'secrets.json').write_text('{}')
 for component in ['llm.memories.yaml','secretstore.memories.yaml']:
  source=(REPO/'src/Hexalith.Tenants.AppHost/DaprComponents'/component).read_text()
  (resources/component).write_text(source.replace('DaprComponents/secrets.json',str(ROOT/'secrets.json')))
 subprocess.run(['openssl','req','-x509','-newkey','rsa:2048','-keyout',str(ROOT/'app.key'),'-out',str(ROOT/'app.crt'),'-days','1','-nodes','-subj','/CN=localhost','-addext','subjectAltName=IP:127.0.0.1,DNS:localhost'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,check=True)
 backend=ThreadingHTTPServer(('127.0.0.1',0),Backend);sslctx=ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER);sslctx.load_cert_chain(ROOT/'app.crt',ROOT/'app.key');backend.socket=sslctx.wrap_socket(backend.socket,server_side=True);threading.Thread(target=backend.serve_forever,daemon=True).start()
 def sidecar(app,config,ap=None):
  hp=port();args=[str(daprd),'--app-id',app,'--log-level','debug','--dapr-http-port',str(hp),'--dapr-grpc-port',str(port()),'--dapr-internal-grpc-port',str(port()),'--metrics-port',str(port()),'--enable-mtls','--sentry-address','127.0.0.1:'+str(sentryport),'--control-plane-trust-domain','hexalith.io','--control-plane-namespace',namespace,'--config',str(config),'--resources-path',str(resources),'--scheduler-host-address','','--placement-host-address','']
  if ap:args += ['--app-port',str(ap),'--app-protocol','https']
  start(app,args,env);wait_for(lambda:healthy(hp));return hp
 receiverport=sidecar(receiver_alias,receiver,backend.server_port)
 caller_cfg=ROOT/'caller.yaml';caller_cfg.write_text((REPO/'deploy/dapr/accesscontrol.tenants-ui.yaml').read_text().replace('{env:DAPR_TRUST_DOMAIN|hexalith.io}','public'));caller=sidecar('tenants-ui',caller_cfg);denied=sidecar('dw144-denied',caller_cfg)
 time.sleep(2)
 for reader,hp in [('tenants-ui',caller),('tenants-api',receiverport)]:
  code,_,body=request('http://127.0.0.1:'+str(hp)+'/v1.0/metadata')
  components={component['name'] for component in json.loads(body).get('components',[])}
  results.append({'case':reader+' cannot load Memories capabilities','status':code,'expected':200,'pass':code==200 and not components.intersection({'llm','secretstore'})})
 def invoke(hp,path,method='GET',extra=None):
  return request('http://127.0.0.1:'+str(hp)+path,method,{'dapr-app-id':receiver_alias,'Authorization':'Bearer dw144-sentinel',**(extra or {})})
 paths=['/api/tenants','/api/tenants/tenant.alpha','/api/tenants/tenant.alpha/users','/api/tenants/tenant.alpha/audit','/api/users/user.alpha/tenants','/api/global-administrators']
 for path in paths:
  code,headers,body=invoke(caller,path);results.append({'case':'authorized '+path,'status':code,'expected':200,'diagnostic':body[:300].decode(errors='replace') if code!=200 else '', 'pass':code==200 and json.loads(body).get('authorizationForwarded') is True and json.loads(body).get('encoding') is None and headers.get('etag')=='"dw144"' and headers.get('x-hexalith-projection-version')=='17'})
 code,headers,_=invoke(caller,paths[1],extra={'If-None-Match':'"dw144"'});results.append({'case':'conditional304','status':code,'expected':304,'pass':code==304 and headers.get('etag')=='"dw144"' and headers.get('x-hexalith-projection-version')=='17'})
 for hp,method,label in [(denied,'GET','denied caller'),(caller,'POST','denied verb')]:
  code,_,_=invoke(hp,paths[0],method);results.append({'case':label,'status':code,'expected':403,'pass':code==403})
 code,_,_=invoke(caller,'/api/tenants/failure');observed=counts.get(('GET','/api/tenants/failure'),0);results.append({'case':'no retry on 503','status':code,'backendCalls':observed,'expected':503,'pass':code==503 and observed==1})
 code,_,_=invoke(caller,'/private/secret');results.append({'case':'denied unrelated path','status':code,'expected':403,'pass':code==403})
 report={'runtime':version,'mutation':args.mutate_retries,'namespace':namespace,'productionAclSha256':hashlib.sha256(original.encode()).hexdigest(),'results':results,'backendCounts':{str(k):v for k,v in counts.items()}}
 args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2));assert len(results)==13 and all(r['pass'] for r in results), 'Read transport contract failed; see the required 503 backend-call count.'
finally:
 for p in reversed(processes):
  p.terminate()
 for p in reversed(processes):
  try:p.wait(timeout=5)
  except subprocess.TimeoutExpired:p.kill();p.wait()
 for f in files:f.close()
 if backend is not None:backend.shutdown();backend.server_close()
 workspace.cleanup()
