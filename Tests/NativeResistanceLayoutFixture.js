(function () {
    'use strict';
    var fs=require('fs'), out=__OUTPUT__, fixed=__FIXED__, done=false, began=Date.now();
    var report={checks:[],cases:[],errors:[]};
    function check(name,ok,detail){report.checks.push({name:name,pass:!!ok,detail:detail});if(!ok)throw Error(name+': '+JSON.stringify(detail));}
    function finish(e){if(done)return;done=true;report.status=e?'FAIL':'PASS';report.error=e&&String(e.stack||e);fs.writeFileSync(out,JSON.stringify(report,null,2));nw.App.quit();}
    window.addEventListener('error',function(e){report.errors.push(String(e.message));});
    var timer=setInterval(function(){
        if(Date.now()-began>30000){clearInterval(timer);finish(Error('Native startup timeout'));return;}
        if(!window.SceneManager||!SceneManager._scene||SceneManager._scene.constructor.name!=='Scene_Title')return;
        clearInterval(timer);
        try {
            check('English active without optional controls',!!window.CelesphoniaEnglish&&!window.CelesphoniaControls,{});
            DataManager.createGameObjects();$gameParty.setupStartingMembers();
            var database=JSON.stringify([$dataSystem,$dataCommonEvents,$dataActors,$dataArmors,$dataStates]);
            [false,true].forEach(function(negative){
                [false,true].forEach(function(clothing){
                    [1091,1092,1093,1094,1095,1098,1099,1100,1097].forEach(function(id,i){$gameVariables.setValue(id,(negative?-1:1)*(i===1?100:10+i*5));});
                    $gameVariables.setValue(734,85);$gameVariables.setValue(702,1250);$gameVariables.setValue(722,9999);
                    // CE806 chooses the clothing display using its own native condition.
                    $gameSwitches.setValue(131,clothing);
                    var ip=new Game_Interpreter();ip.setup($dataCommonEvents[806].list,0);
                    for(var n=0;ip.isRunning()&&n<100;n++)ip.update();
                    check('native resistance event completes',!ip.isRunning(),{negative:negative,clothing:clothing});
                    var snapshots=[];
                    [86,87].forEach(function(id){
                        var sprite=new Sprite_Picture(id);sprite.update();
                        var info=sprite.dTextInfo, before=JSON.stringify(info), game=JSON.stringify([$gameVariables._data,$gameSwitches._data]);
                        sprite.makeDynamicBitmap();
                        check('drawing preserves picture text and runtime state',JSON.stringify(info)===before&&sprite.dTextInfo===info&&JSON.stringify([$gameVariables._data,$gameSwitches._data])===game,{id:id});
                        var layout=sprite._celesphoniaEnglishResistanceLayout;
                        if(fixed){
                            check('fixed column bounds',!!layout&&sprite.bitmap.width<=143,{id:id,width:sprite.bitmap.width,layout:layout});
                            check('label/value separation',layout.rows.every(function(r){return r.labelRight+6<=r.valueLeft&&r.valueRight<=139;}),layout.rows);
                        }
                        snapshots.push({id:id,width:sprite.bitmap.width,height:sprite.bitmap.height,layout:layout||null,sprite:sprite});
                    });
                    var combined=new Bitmap(308,164);combined.fillAll('#153270');
                    snapshots.forEach(function(x,i){combined.blt(x.sprite.bitmap,0,0,x.width,x.height,8+i*143,8);delete x.sprite;});
                    var png=out.replace(/\.json$/, '')+'-'+(negative?'negative':'positive')+'-'+(clothing?'clothing':'plain')+'.png';
                    fs.writeFileSync(png,new Buffer(combined._canvas.toDataURL('image/png').split(',')[1],'base64'));
                    report.cases.push({negative:negative,clothing:clothing,columns:snapshots,image:png});
                });
            });
            check('native database remains unchanged',database===JSON.stringify([$dataSystem,$dataCommonEvents,$dataActors,$dataArmors,$dataStates]),{});
            if(!fixed)check('baseline reproduces overlapping columns',report.cases.some(function(c){return c.columns.some(function(x){return x.width>143;});}),report.cases[0].columns);
            check('stream repair retained without controls',!!window.CelesphoniaStreamChoiceRescue,{});
            finish();
        }catch(e){finish(e);}
    },100);
})();
