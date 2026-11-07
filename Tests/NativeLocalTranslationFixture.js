(function(){
 'use strict';var fs=require('fs'),out=__OUTPUT__,done=false,start=Date.now(),report={checks:[],userSavesLoaded:false,userSavesWritten:false,fullGameplayVerified:false};
 function check(name,ok,detail){report.checks.push({name:name,pass:!!ok,detail:detail});if(!ok)throw Error(name+': '+JSON.stringify(detail));}
 function finish(error){if(done)return;done=true;report.status=error?'FAIL':'PASS';report.error=error&&String(error.stack||error);fs.writeFileSync(out,JSON.stringify(report,null,2));nw.App.quit();}
 var timer=setInterval(function(){
  if(Date.now()-start>35000){clearInterval(timer);finish(Error('Native local adapter startup timeout'));return;}
  if(!window.SceneManager||!SceneManager._scene||SceneManager._scene.constructor.name!=='Scene_Title'||!window.CelesphoniaLocalTranslation||CelesphoniaLocalTranslation.status().entries<2)return;clearInterval(timer);
  try{
   check('local mode runs without runtime controls',CelesphoniaLocalMode&&!window.CelesphoniaControls);
   check('curated English renderer stays separate',!window.CelesphoniaEnglish);
   check('partial paused French run loaded',CelesphoniaLocalTranslation.status().language==='fr');
   DataManager.createGameObjects();$gameParty.setupStartingMembers();var source=JSON.stringify([$dataItems,$dataActors,$dataCommonEvents,$dataSystem]),item=$dataItems[15],name=CelesphoniaLocalTranslation.translate(item.name,'Names','Items/15'),desc=CelesphoniaLocalTranslation.translate(item.description,'Descriptions','Items/15');
   check('native item name uses generated local cache',name!==item.name&&!/[\u3400-\u9fff]/.test(name),name);
   check('native description uses generated local cache',desc!==item.description&&!/[\u3400-\u9fff]/.test(desc),desc);
   check('uncached text stays original',CelesphoniaLocalTranslation.translate('まだキャッシュされていない文です。','Dialogue','Private')==='まだキャッシュされていない文です。');
    var help=new Window_Help(2);help.setItem(item);help.update();var layout=help._localHelpLayout;
    check('translated help wraps within native viewport',layout&&layout.width<=layout.availableWidth+1,layout);report.helpLayout=layout;
    var dense=new Window_Help(2);dense.width=280;dense.createContents();dense.setItem(item);dense.update();var narrow=dense._localHelpLayout;
    check('dense translated description remains reachable',narrow&&narrow.scrollable&&narrow.width<=narrow.availableWidth+1,narrow);
    TouchInput._x=dense.x+20;TouchInput._y=dense.y+20;dense.openness=255;TouchInput._wheelY=1200;for(var i=0;i<50;i++)dense.update();TouchInput._wheelY=0;
    check('help can scroll to its final line',dense.origin.y===dense.contents.height-dense.contentsHeight(),dense.origin.y);
    var stage=new PIXI.Container(),layer=new WindowLayer();layer.move(0,0,Graphics.boxWidth,Graphics.boxHeight);stage.addChild(layer);layer.addChild(help);var frame=Bitmap.snap(stage);report.image=out+'.png';fs.writeFileSync(report.image,new Buffer(frame._canvas.toDataURL('image/png').split(',')[1],'base64'));
   check('native databases unchanged by local display',source===JSON.stringify([$dataItems,$dataActors,$dataCommonEvents,$dataSystem]));report.name=name;report.description=desc;report.localStatus=CelesphoniaLocalTranslation.status();finish();
  }catch(e){finish(e);}
 },200);
})();
