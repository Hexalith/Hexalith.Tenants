async page => {
 const cases=[];
 for(const culture of ['en','fr'])for(const width of [1440,390])for(const forcedColors of ['none','active']){
  await page.setViewportSize({width,height:1000});await page.emulateMedia({forcedColors});
  await page.goto('http://127.0.0.1:18790/annotated.html?culture='+culture);await page.waitForFunction(()=>window.ready===true);
  const result=await page.evaluate(()=>({status:document.body.dataset.validationStatus,report:document.querySelector('#report').textContent,expected:window.expected}));
  if(result.status!=='passed')throw Error(JSON.stringify(result));
  const key=culture+'-'+width+'-'+forcedColors;
  const client=await page.context().newCDPSession(page);await client.send('DOM.enable');await client.send('CSS.enable');await client.send('Accessibility.enable');
  const doc=await client.send('DOM.getDocument');const query=await client.send('DOM.querySelectorAll',{nodeId:doc.root.nodeId,selector:'[data-testid="tenants-correction-target-user"]'});
  const identities=[];
  for(let i=0;i<query.nodeIds.length;i++){
   const nodeId=query.nodeIds[i];const ax=await client.send('Accessibility.getPartialAXTree',{nodeId,fetchRelatives:true});
   if(!ax.nodes.some(n=>n.name?.value===result.expected[i]))throw Error('Accessible scalar missing '+key+' '+i);
   const font=await client.send('CSS.getPlatformFontsForNode',{nodeId});
   const target=page.locator('[data-testid="tenants-correction-target-user"]').nth(i);
   identities.push({display:await target.textContent(),fonts:font.fonts,accessibleNames:ax.nodes.map(n=>n.name?.value).filter(Boolean)});
   await target.screenshot({path:'/tmp/tenants-dw390-browser/annotated-'+key+'-'+i+'.png',animations:'disabled'});
  }
  await page.screenshot({path:'/tmp/tenants-dw390-browser/annotated-'+key+'.png',fullPage:true,animations:'disabled'});
  cases.push({culture,width,forcedColors,...result,identities});await client.detach();
 }
 await page.goto('http://127.0.0.1:18790/annotated.html?culture=en&mutation=literal');await page.waitForFunction(()=>window.ready===true);
 for(let i=0;i<5;i++)await page.locator('[data-testid="tenants-correction-target-user"]').nth(i).screenshot({path:'/tmp/tenants-dw390-browser/literal-mutation-'+i+'.png',animations:'disabled'});
 const mutation=await page.evaluate(()=>({status:document.body.dataset.validationStatus,report:document.querySelector('#report').textContent}));
 if(mutation.status!=='failed')throw Error('Literal-whitespace mutation escaped');
 return {userAgent:await page.evaluate(()=>navigator.userAgent),cases,mutation};
}
