/* Ver1.053 Map005 PC repeat-stream choice rescue.
 * After the PC's help-text/choice closes, MPP_ChoiceEX forceClear() leaves
 * Window_Message.pause=true with no _textState. CE69 has meanwhile queued
 * valid text+choices. The stale pause intercepts native Window_Message.update,
 * so no Window_ChoiceList is opened and a later OK discards the new choices.
 * Reset only that paused/stale state, for that exact queued CE69 prompt.
 */
(function(){
  'use strict';
  var original=Window_Message.prototype.update;
  var repairs=0;
  Window_Message.prototype.update=function(){
    try{
      var m=window.$gameMessage, map=window.$gameMap;
      if(map && map.mapId()===5 && window.$gameSwitches && $gameSwitches.value(291) &&
         m && m.hasText() && m.isChoice() && !this._textState && this.pause &&
         this._choiceWindow && !this._choiceWindow.active &&
         m.choices()[0]==='闲聊的同时收集评论' &&
         m._choiceCallback){
        this.pause=false;
        this._pauseSkip=false;
        repairs++;
        if(window.console)console.info('[Celesphonia stream repeat] resumed CE69 choice window',repairs);
      }
    }catch(e){if(window.console)console.warn('[Celesphonia stream repeat]',e);}
    return original.apply(this,arguments);
  };
  window.CelesphoniaStreamChoiceRescue={count:function(){return repairs}};
})();
