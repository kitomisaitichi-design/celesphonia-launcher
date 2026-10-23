(function(){
 'use strict';
 var fs=require('fs'),out=__OUTPUT__,fixed=__FIXED__,done=false,began=Date.now();
 var report={checks:[],names:[],columns:[],locked:{},errors:[],userSavesLoaded:false,userSavesWritten:false,fullGameplayVerified:false};
 function check(name,ok,detail){report.checks.push({name:name,pass:!!ok,detail:detail});if(!ok)throw Error(name+': '+JSON.stringify(detail));}
 function finish(e){if(done)return;done=true;report.status=e?'FAIL':'PASS';report.error=e&&String(e.stack||e);fs.writeFileSync(out,JSON.stringify(report,null,2));nw.App.quit();}
 window.addEventListener('error',function(e){report.errors.push(String(e.message));});
 function writeBitmap(bitmap,suffix){var filename=out+'.'+suffix+'.png';fs.writeFileSync(filename,new Buffer(bitmap._canvas.toDataURL('image/png').split(',')[1],'base64'));return filename;}
 function drawCapture(action){var calls=[],previous=Bitmap.prototype.drawText;Bitmap.prototype.drawText=function(text){calls.push(String(text));return previous.apply(this,arguments);};try{var result=action();return{result:result,text:calls.join('')};}finally{Bitmap.prototype.drawText=previous;}}
 function sprite(id){var s=new Sprite_Picture(id),move=s._processNewLine;s.rowPositions=[];s._processNewLine=function(state,bitmap){var r=move.apply(this,arguments);this.rowPositions[state.line]=state.y;return r;};s.update();if(s.dTextInfo)s.makeDynamicBitmap();return s;}
 var timer=setInterval(function(){
  if(Date.now()-began>35000){clearInterval(timer);finish(Error('Native startup timeout'));return;}
  if(!window.SceneManager||!SceneManager._scene||SceneManager._scene.constructor.name!=='Scene_Title')return;
  if(!ImageManager.loadPicture('cursor01').isReady())return;
  clearInterval(timer);
  try{
   check('normal English enabled without controls',!!window.CelesphoniaEnglish&&!window.CelesphoniaControls,{});
   DataManager.createGameObjects();$gameParty.setupStartingMembers();
   var nativeData=JSON.stringify([$dataSystem,$dataCommonEvents,$dataSkills,$dataActors]),a=$gameActors.actor(1),w=new Window_Base(0,0,1024,120);
   var keys=Object.keys(CelesphoniaEnglishDictionary),raw=keys.filter(function(k){return /^Deep in .*belly/.test(CelesphoniaEnglishDictionary[k])&&/n\[1\]/i.test(k);})[0];
   if(!raw)raw=keys.filter(function(k){return /n\[1\]/i.test(k)&&/n\[1\]'s/i.test(CelesphoniaEnglishDictionary[k])&&!/[\n<>]/.test(k);})[0];
   check('reported message found',!!raw,{});
   var tickerTemplate='\\i[\\v[119]]\\c[\\v[118]]\\v[\\v[2995]]';
   $gameVariables._data[2944]=raw.replace(/\\/g,'\x1b');$gameVariables._data[2995]=2944;$gameVariables._data[119]=892;$gameVariables._data[118]=27;
   ['天音','苍蓝星','Mira'].forEach(function(n){a.setName(n);var base=w.convertEscapeCharacters(raw),capture=drawCapture(function(){TickerManager.show(tickerTemplate,false);});report.names.push({nativeName:a.name(),expected:CelesphoniaEnglish.translate(a.name()),base:base,ticker:capture.text,source:raw});
    if(n==='天音'){var containers=[SceneManager._scene._tickerContainer,SceneManager._scene._tickerLogContainer];containers.forEach(function(c){if(c&&c.children.length){var t=c.children[c.children.length-1];if(t.contents&&t.contents.contents){var note=new Bitmap(1024,80);note.fillAll('#14101c');note.blt(t.contents.contents,0,0,Math.min(1024,t.contents.contents.width),Math.min(80,t.contents.contents.height),0,0);report.nameImage=writeBitmap(note,'name');}}});}
   });
   a.forgetSkill(813);a.forgetSkill(814);[952,955,911,963].forEach(function(id){a.learnSkill(id);});
   var ip=new Game_Interpreter();ip.setup($dataCommonEvents[814].list,0);for(var count=0;ip.isRunning()&&count<100;count++)ip.update();check('native title list completes',!ip.isRunning(),{});
   var stateBefore=JSON.stringify([$gameVariables._data,$gameSwitches._data,$gameScreen._pictures]),sprites=[];
   [85,86,87,88,83].forEach(function(id){if(!$gameScreen.picture(id))return;var s=sprite(id),info=s.dTextInfo,lines=info.value.split('\n');if(lines[lines.length-1]==='')lines.pop();report.columns.push({id:id,rows:lines.length,size:info.size,width:s.bitmap.width,height:s.bitmap.height,rowPitch:s.rowPositions[1]-s.rowPositions[0],layout:s._celesphoniaEnglishTitleLayout});sprites.push(s);});
   check('drawing leaves native runtime state intact',stateBefore===JSON.stringify([$gameVariables._data,$gameSwitches._data,$gameScreen._pictures]),{});
   function selectSlot(relative){var menu=new Game_Interpreter();menu.setup($dataCommonEvents[823].list,0);menu.update();$gameVariables._data[4585]=Math.floor((relative-1)/22)+1;$gameVariables._data[4586]=(relative-1)%22+1;menu._index=55;menu._waitCount=0;menu.update();report.lastSelection={relative:relative,address:$gameVariables.value(4584),skill:$gameVariables.value($gameVariables.value(4584)),count:$gameVariables.value(4698)};return sprite(84);}
   var slots=[];for(var slot=1;slot<=$gameVariables.value(4698);slot++)slots.push($gameVariables.value(4600+slot));
   var lockedSlot=slots.indexOf(999)+1,lockCapture=drawCapture(function(){return selectSlot(lockedSlot);});report.locked={nativeValue:$gameVariables._data[4587],rendered:lockCapture.text,width:lockCapture.result.bitmap.width};
   report.selections=[];[911,952,955,963].forEach(function(id){var relative=slots.indexOf(id)+1;check('unlocked title present '+id,relative>0,{});selectSlot(relative);check('native description matches selected skill '+id,$gameVariables._data[4587]===$dataSkills[id].description,{});report.selections.push({id:id,column:Math.floor((relative-1)/22)+1,row:(relative-1)%22+1});});
   var tailAddress=4601+slots.length;$gameVariables._data[tailAddress]=911;var tail=selectSlot(slots.length+1);report.emptyTail={description:$gameVariables._data[4587],profileValue:$gameVariables._data[tailAddress],selection:report.lastSelection};check('unrelated profile field preserved',$gameVariables._data[tailAddress]===911,{});
   var tailNote=new Bitmap(1024,100);tailNote.fillAll('#14101c');if(tail.bitmap)tailNote.blt(tail.bitmap,0,0,tail.bitmap.width,tail.bitmap.height,30,30);report.tailImage=writeBitmap(tailNote,'tail');
   report.native={gap:$gameVariables.value(939),pitch:$gameVariables.value(4598),lockedDescription:$dataSkills[999].description};
   var selected=slots.indexOf(952)+1,description=selectSlot(selected);
   var grid=new Bitmap(Graphics.boxWidth,Graphics.boxHeight);grid.fillAll('#14101c');
   sprites.forEach(function(s){var pic=s.picture();grid.blt(s.bitmap,0,0,s.bitmap.width,s.bitmap.height,pic.x(),pic.y());});
   var heading=sprite(81),pointer=sprite(89);[heading,pointer,description].forEach(function(s){if(s.bitmap){var pic=s.picture();grid.blt(s.bitmap,0,0,s.bitmap.width,s.bitmap.height,pic.x(),pic.y());}});report.gridImage=writeBitmap(grid,'grid');
   var note=new Bitmap(1024,100);note.fillAll('#14101c');note.blt(lockCapture.result.bitmap,0,0,lockCapture.result.bitmap.width,lockCapture.result.bitmap.height,30,30);report.lockedImage=writeBitmap(note,'locked');
   check('native database unchanged',nativeData===JSON.stringify([$dataSystem,$dataCommonEvents,$dataSkills,$dataActors]),{});
   if(fixed){check('names retained in ticker',report.names.every(function(n){return n.ticker.indexOf(n.expected)>=0;}),report.names);check('locked description preserves native placeholder',report.locked.nativeValue===report.native.lockedDescription&&(report.locked.nativeValue!==''||!report.locked.rendered.trim()),report.locked);check('all columns preserve native row pitch',report.columns.every(function(c){return Math.abs(c.rowPitch-report.native.pitch)<.1;}),report.columns);check('empty tail has native locked description',report.emptyTail.description===report.native.lockedDescription,report.emptyTail);check('labels fit native column width',report.columns.every(function(c){return c.width<=$gameVariables.value(939)-8;}),report.columns);}
   finish();
  }catch(e){finish(e);}
 },100);
})();
