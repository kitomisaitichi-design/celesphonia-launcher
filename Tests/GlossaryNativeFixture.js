(function(){
var fs=require('fs'),out=__OUTPUT__,checks=[],pages=[],phase=0,began=Date.now(),bitmaps={},done=false;
function check(name,ok,detail){checks.push({name:name,pass:!!ok,detail:detail});if(!ok)throw Error(name+': '+JSON.stringify(detail));}
function finish(e){if(done)return;done=true;clearInterval(timer);fs.writeFileSync(out,JSON.stringify({status:e?'FAIL':'PASS',error:e&&String(e.stack||e),checks:checks,pages:pages,userSavesRead:false,userSavesWritten:false,fullGameplayVerified:false},null,2));nw.App.quit();}
var timer=setInterval(function(){try{
 if(Date.now()-began>55000)throw Error('Glossary fixture timeout '+phase);
 if(phase===0){if(!window.SceneManager||!SceneManager._scene||SceneManager._scene.constructor.name!=='Scene_Title')return;
  DataManager.setupNewGame();[566,568,572].forEach(function(id){var w=new Window_Glossary(240,72);w._itemData=$dataItems[id];bitmaps[id]=ImageManager.loadPicture(w.getPictureName(0));});phase=1;return;}
 if(![566,568,572].every(function(id){return bitmaps[id].isReady();}))return;
 var before=JSON.stringify([$dataItems,$gameVariables._data,$gameSwitches._data]);
 [[Graphics.boxWidth,Graphics.boxHeight],[1024,768],[1280,720]].forEach(function(size){
  var old=[Graphics.boxWidth,Graphics.boxHeight];Graphics.boxWidth=size[0];Graphics.boxHeight=size[1];
  [566,568,572].forEach(function(id){
   var w=new Window_Glossary(240,72);w._itemData=$dataItems[id];w._pageIndex=0;
   var blits=[],glyphs=[],blt=w.contents.blt,draw=w.contents.drawText;
   w.contents.blt=function(bitmap,sx,sy,sw,sh,dx,dy,dw,dh){if(bitmap===bitmaps[id])blits.push({x:dx,y:dy,width:dw||sw,height:dh||sh});return blt.apply(this,arguments);};
   w.contents.drawText=function(text,x,y,width,height){if(text)glyphs.push({x:x,y:y,width:this.measureTextWidth(text),height:height});return draw.apply(this,arguments);};
   w.drawItemSub(bitmaps[id]);var picture=blits[0],layout=w._celesphoniaEnglishGlossaryLayout;
   var row={item:id,viewport:size,content:[w.contentsWidth(),w.contentsHeight()],picture:picture,textBottom:glyphs.reduce(function(m,p){return Math.max(m,p.y+p.height);},0),textRight:glyphs.reduce(function(m,p){return Math.max(m,p.x+p.width);},0),layout:layout&&{imageHeight:layout.imageHeight,textHeight:layout.fit.height,fontSize:layout.fit.fontSize,scale:layout.scale}};
   pages.push(row);
   if(__EXPECT_FIXED__ && id===572 && size[0]===1024 && !fs.existsSync(out+'.png')){
    w.x=0;w.y=0;var stage=new PIXI.Container();stage.addChild(w);
    var snapshot=Bitmap.snap(stage);
    fs.writeFileSync(out+'.png',Buffer.from(snapshot._canvas.toDataURL('image/png').split(',')[1],'base64'));
   }
   if(__EXPECT_FIXED__){
    check('picture stays within glossary '+id+' '+size,picture&&picture.x>=0&&picture.y>=0&&picture.x+picture.width<=w.contentsWidth()+1&&picture.y+picture.height<=w.contentsHeight()+1,row);
    check('English text stays above picture '+id+' '+size,row.textBottom<=picture.y+1,row);
    check('English text stays inside width '+id+' '+size,row.textRight<=w.contentsWidth()+1,row);
    check('image keeps aspect ratio '+id+' '+size,Math.abs(picture.width/picture.height-bitmaps[id].width/bitmaps[id].height)<0.001,row);
    check('text pages reserve most room for reading '+id+' '+size,!w.getDescription(0).trim()||picture.height<=w.contentsHeight()*0.34+1,row);
   }
   w._pageIndex=1;w.drawItemSub(null);if(__EXPECT_FIXED__)check('next text-only page clears picture layout '+id, !w._celesphoniaEnglishGlossaryLayout,{});
  });Graphics.boxWidth=old[0];Graphics.boxHeight=old[1];
 });
 check('native glossary records and game state unchanged',JSON.stringify([$dataItems,$gameVariables._data,$gameSwitches._data])===before,{});
 finish(null);
}catch(e){finish(e);}},100);
})();
