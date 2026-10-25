(function () {
    'use strict';
    var fs=require('fs'),out=__OUTPUT__,fixed=__FIXED__,done=false,start=Date.now();
    var report={checks:[],cases:[],userSavesLoaded:false,userSavesWritten:false,fullGameplayVerified:false};
    function check(name,ok,detail){report.checks.push({name:name,pass:!!ok,detail:detail});if(!ok)throw Error(name+': '+JSON.stringify(detail));}
    function finish(e){if(done)return;done=true;report.status=e?'FAIL':'PASS';report.error=e&&String(e.stack||e);fs.writeFileSync(out,JSON.stringify(report,null,2));nw.App.quit();}
    function capture(help,row,suffix){
        help.update();
        var stage=new PIXI.Container(),layer=new WindowLayer();layer.move(0,0,Graphics.boxWidth,Graphics.boxHeight);stage.addChild(layer);layer.addChild(help);
        var frame=Bitmap.snap(stage),image=new Bitmap(help.width,help.height+42);image.fillAll('#142961');image.fontFace=help.contents.fontFace;image.fontSize=22;image.textColor='#e8efff';image.drawText(row.name+'  |  '+CelesphoniaEnglishMeta.version,16,4,image.width-32,32);image.blt(frame,0,0,help.width,help.height,0,42);
        var file=out.replace(/\.json$/,'')+'-'+row.kind+'-'+row.id+(suffix||'')+'.png';fs.writeFileSync(file,new Buffer(image._canvas.toDataURL('image/png').split(',')[1],'base64'));layer.removeChild(help);return file;
    }
    var timer=setInterval(function(){
        if(Date.now()-start>40000){clearInterval(timer);finish(Error('Native equipment startup timeout'));return;}
        if(!window.SceneManager||!SceneManager._scene||SceneManager._scene.constructor.name!=='Scene_Title')return;
        clearInterval(timer);
        try{
            check('English active without optional controls',!!window.CelesphoniaEnglish&&!window.CelesphoniaControls,{});
            DataManager.createGameObjects();$gameParty.setupStartingMembers();
            var original=JSON.stringify([$dataSystem,$dataWeapons,$dataArmors,$dataStates,$gameVariables._data,$gameSwitches._data]);
            var help=new Window_Help(2),draw=Bitmap.prototype.drawText,calls=[];
            Bitmap.prototype.drawText=function(text,x,y,maxWidth,lineHeight,align){
                if(this===help.contents){calls.push({text:String(text),x:x,y:y,right:x+Math.min(this.measureTextWidth(String(text)),maxWidth||Infinity),bottom:y+lineHeight,font:this.fontSize});}
                return draw.apply(this,arguments);
            };
            var picks=[['armor',153],['weapon',10],['weapon',45],['weapon',191],['armor',187],['armor',185],['armor',426],['armor',408],['armor',349]];
            picks.forEach(function(p){
                var item=(p[0]==='armor'?$dataArmors:$dataWeapons)[p[1]];if(!item||!item.name)return;
                calls=[];help.setItem(null);help.setItem(item);
                var layout=help._celesphoniaEnglishEquipmentLayout||null;
                if(!fixed)layout=help._celesphoniaEnglishPanelLayout||null;
                var row={kind:p[0],id:item.id,name:CelesphoniaEnglish.translate(item.name),nativeText:help._text,width:help.contentsWidth(),height:help.contentsHeight(),layout:layout,calls:calls.length,minFont:calls.reduce(function(n,c){return Math.min(n,c.font);},100),bottom:calls.reduce(function(n,c){return Math.max(n,c.bottom);},0),right:calls.reduce(function(n,c){return Math.max(n,c.right);},0),rendered:calls.map(function(c){return c.text;}).join('')};
                if(fixed){
                    check('equipment complete readable layout '+p.join('-'),!!layout&&layout.fits&&layout.fontSize>=20,row);
                    check('equipment has no missing AGI labels '+p.join('-'),!/敏捷下降/.test(row.rendered),{});
                    if(layout.abbreviated)check('canonical MDF abbreviation '+p.join('-'),!/\bMD\b/.test(layout.text),{});
                    var numbers=function(t){return t.match(/[+\-]?\d+(?:\.\d+)?%?/g)||[];},codes=function(t){return t.match(/(?:\\|\x1b)[A-Za-z]+(?:\[[^\]\r\n]*\]|<[^>\r\n]*>)?/g)||[];};
                    check('numbers and display codes preserved '+p.join('-'),JSON.stringify(numbers(layout.text))===JSON.stringify(numbers(layout.originalDisplay))&&JSON.stringify(codes(layout.text))===JSON.stringify(codes(layout.originalDisplay)),{});
                    if(item===$dataWeapons[10])check('short description retains full wording and native font',!layout.abbreviated&&layout.fontSize===Window_Base.prototype.standardFontSize.call(help),layout);
                }
                row.image=capture(help,row,'');
                if(fixed&&layout.scrollable){help.origin.y=help.contents.height-help.contentsHeight();row.bottomImage=capture(help,row,'-bottom');help.origin.y=0;}
                report.cases.push(row);
            });
            Bitmap.prototype.drawText=draw;
            if(!fixed)check('baseline reproduces missing composed AGI states',report.cases.some(function(c){return c.id===153&&/敏捷下降/.test(c.rendered);}),{});
            if(!fixed)check('baseline reproduces clipping or tiny text',report.cases.some(function(c){return c.bottom>c.height||c.right>c.width||c.minFont<20||c.layout&&!c.layout.fits;}),{});
            if(fixed){
                var dense=report.cases.filter(function(c){return c.layout.scrollable;})[0];
                check('native dense equipment available for wheel check',!!dense,{});
                var item=(dense.kind==='armor'?$dataArmors:$dataWeapons)[dense.id];help.setItem(item);
                var savedScene=SceneManager._scene;SceneManager._scene={_helpWindow:help};TouchInput._x=help.x+30;TouchInput._y=help.y+30;TouchInput._wheelY=120;
                help.update();check('hover wheel reaches lower help text',help.origin.y>0&&help.upArrowVisible,{});
                var list=new Window_Selectable(0,help.height,Graphics.boxWidth,200),scrolled=0;list.activate();list.scrollDown=function(){scrolled++;};list.processWheel();check('help wheel does not move item list',scrolled===0,{});
                TouchInput._y=help.height+40;list.processWheel();check('list wheel still works outside help',scrolled===1,{});
                TouchInput._y=help.y+30;TouchInput._wheelY=-120;help.update();check('help scroll returns to top',help.origin.y===0,{});TouchInput.clear();SceneManager._scene=savedScene;
                var firstText=help._text;help.setItem(item);help.setItem(item);check('repeated selection preserves native text',help._text===firstText,{});help.refresh();check('refresh preserves complete help',help._celesphoniaEnglishEquipmentLayout.fits,{});
                var failures=[],count=0,scrollable=0;
                [$dataWeapons,$dataArmors].forEach(function(data){data.forEach(function(record){if(!record||!record.name)return;help.setItem(record);var fit=help._celesphoniaEnglishEquipmentLayout;count++;if(!fit||!fit.fits||fit.fontSize<20)failures.push({id:record.id,name:record.name,layout:fit});if(fit&&fit.scrollable)scrollable++;});});
                check('all native weapon and armor help remains readable and reachable',failures.length===0,{records:count,scrollable:scrollable,failures:failures});report.inventory={records:count,scrollable:scrollable};
            }
            help.setItem(null);check('empty selection clears help',help._text===''&&help.origin.y===0&&(!fixed||help.contents.height===help.contentsHeight()&&!help.downArrowVisible&&!help.upArrowVisible),{});
            check('native database and game state remain unchanged',original===JSON.stringify([$dataSystem,$dataWeapons,$dataArmors,$dataStates,$gameVariables._data,$gameSwitches._data]),{});
            report.version=CelesphoniaEnglishMeta.version;finish();
        }catch(e){finish(e);}
    },100);
})();
