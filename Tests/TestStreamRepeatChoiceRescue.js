// Targeted runtime regression: native MPP_ChoiceEX forceClear() leaves a
// paused Window_Message after the Map005 PC choice is confirmed.
const fs=require('fs'),vm=require('vm'),assert=require('assert');
const source=fs.readFileSync(require('path').join(__dirname,'../StreamRepeatChoiceRescue.js'),'utf8');
let lastUpdate;
const message={hasText:()=>true,isChoice:()=>true,choices:()=>['闲聊的同时收集评论','游戏直播','唱歌直播','色情的直播','返回'],_choiceCallback:()=>{}};
const switches={value:()=>true};
const map={mapId:()=>5};
const context={window:{},console};
context.window=context;
context.$gameMessage=message;
context.$gameMap=map;
context.$gameSwitches=switches;
function Window_Message(){}
Window_Message.prototype.update=function(){lastUpdate={paused:this.pause,wasCleared:this._textState===null};};
context.Window_Message=Window_Message;
vm.runInNewContext(source,context,{filename:'StreamRepeatChoiceRescue.js'});
function run({pause=true,loaded=true,first=false,mapId=5,choice=true}={}){
  context.$gameMap={mapId:()=>mapId};
  context.$gameSwitches={value:()=>!first};
  context.$gameMessage=Object.assign({},message,{choices:()=>choice?message.choices():['Other']});
  const w=new Window_Message();
  w.pause=pause;w._textState=loaded?null:{text:'still displaying'};
  w._choiceWindow={active:false};
  w._pauseSkip=true;
  w.update();
  return {pause:w.pause,skip:w._pauseSkip};
}
assert.strictEqual(run().pause,false,'Stale repeat stream pause must be released');
assert.strictEqual(run().skip,false,'Stale stream skip must be released');
assert.strictEqual(run({loaded:false}).pause,true,'Active message must remain paused');
assert.strictEqual(run({first:true}).pause,true,'First stream unaffected');
assert.strictEqual(run({mapId:6}).pause,true,'Other maps unaffected');
assert.strictEqual(run({choice:false}).pause,true,'Unrelated choices unaffected');
assert.strictEqual(run({pause:false}).pause,false,'Unpaused message unchanged');
assert.strictEqual(context.CelesphoniaStreamChoiceRescue.count(),2,'Only the two explicitly repeated valid cases should trigger');
console.log('STREAM_REPEAT_RESCUE: 7 scenarios PASS');
