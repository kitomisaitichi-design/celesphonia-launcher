(function(){
'use strict';
var fs=require('fs'),path=require('path'),out=__OUTPUT__,checks=[],finished=false;
function check(name,ok){checks.push({name:name,pass:!!ok});if(!ok)throw Error(name);}
function finish(e){if(finished)return;finished=true;fs.writeFileSync(out,JSON.stringify({status:e?'FAIL':'PASS',error:e&&String(e.stack||e),checks:checks,userSavesRead:false,userSavesWritten:false,fullGameplayVerified:false},null,2));nw.App.quit();}
function key(code,opts,win){opts=opts||{};opts.keyCode=code;opts.which=code;opts.bubbles=true;opts.cancelable=true;(win||window).dispatchEvent(new KeyboardEvent('keydown',opts));}
function capture(suffix,next){setTimeout(function(){try{nw.Window.get().capturePage(function(bytes){try{if(typeof bytes==='string')bytes=Buffer.from(bytes.replace(/^data:[^,]*,/,''),'base64');fs.writeFileSync(out+'.'+suffix+'.png',bytes);next();}catch(e){finish(e);}},{format:'png',datatype:'buffer'});}catch(e){finish(e);}},350);}
var began=Date.now(),timer=setInterval(function(){
 if(Date.now()-began>45000){clearInterval(timer);finish(Error('Native title startup timeout'));return;}
 if(!window.SceneManager||!SceneManager._scene||SceneManager._scene.constructor.name!=='Scene_Title')return;
 clearInterval(timer);
 try{
  DataManager.setupNewGame();var C=CelesphoniaControls,a=$gameParty.leader();C.show();
  var panel=document.getElementById('cc-panel'),head=document.getElementById('cc-head'),shortcut=head.querySelector('button');
  check('default shortcut visible',shortcut.textContent==='Ctrl+Shift+G');
  var before=JSON.stringify([a.hp,a.mp,a.mhp,a.mmp]),bars=panel.querySelectorAll('[role=progressbar]');
  check('current HP and MP values',bars.length===2&&bars[0].getAttribute('aria-valuenow')===String(a.hp)&&bars[1].getAttribute('aria-valuenow')===String(a.mp));
  key(119,{key:'F8'});check('F8 no longer toggles',panel.style.display==='flex');
  key(71,{key:'g',ctrlKey:true,shiftKey:true});check('default hides',panel.style.display==='none');key(71,{key:'g',ctrlKey:true,shiftKey:true});check('default opens',panel.style.display==='flex');
  shortcut.click();key(72,{key:'h',ctrlKey:true,altKey:true});check('custom shortcut shown',shortcut.textContent==='Ctrl+Alt+H');
  key(71,{key:'g',ctrlKey:true,shiftKey:true});check('old binding released',panel.style.display==='flex');key(72,{key:'h',ctrlKey:true,altKey:true});check('custom hides',panel.style.display==='none');key(72,{key:'h',ctrlKey:true,altKey:true});
  shortcut.click();key(27,{key:'Escape'});check('recording cancel retains binding',shortcut.textContent==='Ctrl+Alt+H');
  shortcut.click();key(71,{key:'g'});check('plain letter rejected',shortcut.textContent==='Press shortcut…');key(27,{key:'Escape'});
  var saved=JSON.parse(fs.readFileSync(path.join(CelesphoniaControlConfig.home,'panel-settings.json'),'utf8'));check('custom shortcut persisted',saved.shortcut.keyCode===72&&saved.shortcut.ctrl&&saved.shortcut.alt);
  check('reading bars leaves native stats unchanged',before===JSON.stringify([a.hp,a.mp,a.mhp,a.mmp]));
  a.setHp(Math.max(0,a.mhp-17));a.setMp(Math.max(0,a.mmp-9));
  setTimeout(function(){try{
   check('bars follow changed native values',bars[0].getAttribute('aria-valuenow')===String(a.hp)&&bars[1].getAttribute('aria-valuenow')===String(a.mp));
   var leader=$gameParty.leader;$gameParty.leader=function(){return null;};setTimeout(function(){try{
    check('no leader state',document.getElementById('cc-vitals-name').textContent==='No active character');$gameParty.leader=leader;
    document.getElementById('cc-tabs').querySelectorAll('button')[1].click();document.getElementById('cc-search').value='';document.getElementById('cc-search').dispatchEvent(new Event('input'));
    check('bars retained across navigation',panel.querySelectorAll('[role=progressbar]').length===2);
    C.detach().then(function(){try{
     key(72,{key:'h',ctrlKey:true,altKey:true},panel.ownerDocument.defaultView);check('custom shortcut in detached window',panel.style.display==='none');
     key(72,{key:'h',ctrlKey:true,altKey:true});check('game shortcut restores detached panel',panel.style.display==='flex');C.dock();
     // Leave the real panel visible for a bounded visual review. No saves loaded.
     window.__panelChecksComplete=true;window.__finishPanelFixture=finish;
     fs.writeFileSync(out+'.ready',JSON.stringify({status:'READY',checks:checks.length}));
     if(__REVIEW__){window.addEventListener('keydown',function(e){if(e.ctrlKey&&e.altKey&&e.keyCode===81)finish();},true);setTimeout(function(){finish(Error('Visual review timed out'));},180000);}
     else capture('normal',function(){try{
      var cards=panel.querySelectorAll('.cc-vital'),left=cards[0].getBoundingClientRect(),right=cards[1].getBoundingClientRect();
      check('normal resource cards separated',right.left-left.right>=10);check('resource cards have readable height',left.height>=50);
      nw.Window.get().resizeTo(660,800);setTimeout(function(){try{check('narrow layout activates',panel.classList.contains('cc-narrow'));check('resource strip fits narrow panel',document.getElementById('cc-vitals').scrollWidth<=panel.clientWidth);capture('narrow',function(){finish();});}catch(e){finish(e);}},350);
     }catch(e){finish(e);}});
    }catch(e){finish(e);}},finish);
   }catch(e){finish(e);}},200);
  }catch(e){finish(e);}},200);
 }catch(e){finish(e);}
},100);
})();
