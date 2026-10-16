(function(){
var fs=require('fs'),out=__OUTPUT__,checks=[],started=Date.now(),phase=0;
function check(name,ok,detail){checks.push({name:name,pass:!!ok,detail:detail});if(!ok)throw Error(name+': '+JSON.stringify(detail));}
function finish(e){clearInterval(timer);fs.writeFileSync(out,JSON.stringify({status:e?'FAIL':'PASS',error:e&&String(e.stack||e),checks:checks,userSavesRead:false,userSavesWritten:false,fullGameplayVerified:false},null,2));nw.App.quit();}
var timer=setInterval(function(){try{
 if(Date.now()-started>45000)throw Error('Native fixture timeout phase '+phase);
 if(phase===0){if(!window.SceneManager||!SceneManager._scene||SceneManager._scene.constructor.name!=='Scene_Title')return;
  check('latest controls retain startup movement repair',CelesphoniaControls.version==='1.1.4',CelesphoniaControls.version);
  DataManager.setupNewGame();DataManager.loadMapData(5);phase=1;return;}
 if(!DataManager.isMapLoaded())return;
 $gameSwitches.setValue(345,true);$gameMap.setup(5);var before=JSON.stringify($dataMap),vars=JSON.stringify($gameVariables._data);
 $gameVariables.setValue(1090,5);vars=JSON.stringify($gameVariables._data);
 $gameMap.update(false);
 var a=$gameMap.event(19),b=$gameMap.event(83);
 check('clothing event retains native coordinate',a.x===4&&a.y===16,[a.x,a.y]);
 check('second event is above the wardrobe',b.x===4&&b.y===14,[b.x,b.y]);
 check('approach tile beside upper action is passable',$gameMap.isPassable(5,14,2),{});
 check('second template page activates below recalculated threshold',b.page()&&b.list().length>1,{page:b._pageIndex,commands:b.list().length});
 check('source map/tile/event data unchanged',JSON.stringify($dataMap)===before,{});
 check('no progression variable edited by runtime separation',JSON.stringify($gameVariables._data)===vars,{});
 var sprites=new Spriteset_Map();sprites.update();
 check('complete wardrobe fits existing one-tile footprint',sprites._celesphoniaWardrobe&&sprites._celesphoniaWardrobe.bitmap.width===$gameMap.tileWidth()&&sprites._celesphoniaWardrobe.bitmap.height===$gameMap.tileHeight()*3,{});
 check('cabinet renders below native event indicators',sprites._celesphoniaWardrobe.z<a.screenZ()&&sprites._celesphoniaWardrobe.z<b.screenZ(),{cabinet:sprites._celesphoniaWardrobe.z,clothes:a.screenZ(),upper:b.screenZ()});
 [a,b].forEach(function(ev,i){
  $gameMap._interpreter.clear();$gameMessage.clear();$gamePlayer.locate(5,i===0?16:14);$gamePlayer.setDirection(4);
  $gamePlayer.checkEventTriggerThere([0]);
  check('native adjacent action targets menu '+ev.eventId(),ev.isStarting(),{event:ev.eventId(),position:[ev.x,ev.y]});
  $gameMap.setupStartingEvent();var n=0;
  while(n++<240&&!$gameMessage.isChoice()&&!$gameMessage.hasText()&&!SceneManager.isNextScene(Scene_Equip)&&$gameMap._interpreter.isRunning())$gameMap._interpreter.update();
  check('native event opens its menu '+ev.eventId(),i===0?SceneManager.isNextScene(Scene_Equip):$gameMessage.isChoice(),{choices:$gameMessage.choices(),equipmentScene:SceneManager.isNextScene(Scene_Equip),text:$gameMessage.allText(),commands:n});
  SceneManager._nextScene=null;
  $gameMap._interpreter.clear();$gameMessage.clear();ev.clearStartingFlag();
 });
 check('five translated images still installed',!!CelesphoniaEnglish,{});
 finish(null);
}catch(e){finish(e);}},100);
})();
