'use strict';
const fs = require('fs'), vm = require('vm'), path = require('path'), assert = require('assert');
const home = path.dirname(__dirname), calls = [], written = {};
const data = {actors: {1: {name: '天音'}}, variables: {4: '秘密记忆', 8: 3202, 9: 4}, choices: ['是', '否'], messages: ['原文一', '原文二'], tickerLog: ['\\i[\\V[8]]\\c[\\V[9]] 任务完成！']};
data.items = {301: {name: '怪魔的魂片(极小)', iconIndex: 2245}, 303: {name: '怪魔的魂片(中)', iconIndex: 2245}};
data.actors[101]={name:'Amane'};data.actors[360]={name:'下午'};
Object.assign(data.variables,{119:892,118:27,616:2,2995:2980,2980:'连续绝顶！'});
data.variables[376]='交给\\n[1]。\n\\\x1bc[6]任务条件\\\x1bc[0]';
const before = JSON.stringify(data);
function Bitmap() {}
Bitmap.prototype.drawText = function (text) { calls.push(text); return text; };
Bitmap.prototype.measureTextWidth = function (text) { return text.length; };
function Window_Base() { this.contents = new Bitmap(); }
Window_Base.prototype.drawText = function (text) { return this.contents.drawText(text); };
Window_Base.prototype.drawTextEx = function (text) { return this.convertEscapeCharacters(text); };
Window_Base.prototype.actorName = function (id) { return data.actors[id].name; };
Window_Base.prototype.partyMemberName = Window_Base.prototype.actorName;
Window_Base.prototype.convertEscapeCharacters = function (text) {
    this._wordWrap = false;
    return text.replace(/\\/g, '\x1b').replace(/\x1bN\[(\d+)\]/gi, (_, id) => this.actorName(id))
        .replace(/\x1bV\[(\d+)\]/gi, (_, id) => data.variables[id])
        .replace(/\x1bII\[(\d+)\]/gi, (_, id) => '\x1bI[' + data.items[id].iconIndex + ']' + data.items[id].name);
};
function Window_Message() { Window_Base.call(this); }
Window_Message.prototype = Object.create(Window_Base.prototype);
Window_Message.prototype.checkWordWrap = function () {};
function Scene_Boot() {}
function Window_SavefileList() { Window_Base.call(this);this.contents.fontSize=28; }
Window_SavefileList.prototype=Object.create(Window_Base.prototype);
Window_SavefileList.prototype.textWidth=function(text){return text.length*this.contents.fontSize/2;};
Window_SavefileList.prototype.drawGameTitle=function(info,x,y,width){this.drawnInfo=info;return this.drawText(info.title,x,y,width);};
function Window_SaveInfo(){Window_SavefileList.call(this);this.contents.width=700;}
Window_SaveInfo.prototype=Object.create(Window_SavefileList.prototype);
Window_SaveInfo.prototype.drawGameTitle=function(y){if(this.raise)throw Error('native draw failure');this.drawText(this._info.title,0,y,this.contents.width);return y+54;};
const promptCalls=[];
function Spriteset_Map() {}
function Sprite_Picture() {}
Sprite_Picture.prototype.makeDynamicBitmap=function(){return this.dTextInfo.value;};
const mapCalls=[];
Spriteset_Map.prototype.createUpperLayer=function(){this._hzChoiceScreen={bitmap:{context:{font:'16px GameFont',measureText(text){return {width:text.length*10};},fillText(text,x,y,width){mapCalls.push({text,font:this.font,width});}}}};};
Scene_Boot.prototype.start = function () { calls.push('boot'); };
const realV104Titles=JSON.parse(fs.readFileSync(path.join(home,'English','three-in-one-3-3-5-v1-04','dictionary.json'),'utf8'));
const realV1051=JSON.parse(fs.readFileSync(path.join(home,'English','three-in-one-3-3-5-v1-051','dictionary.json'),'utf8'));
const realV1051Catalog=JSON.parse(fs.readFileSync(path.join(home,'English','three-in-one-3-3-5-v1-051','catalog.json'),'utf8'));
const realV1051Manifest=JSON.parse(fs.readFileSync(path.join(home,'English','three-in-one-3-3-5-v1-051','manifest.json'),'utf8'));
const realV1051Aliases=JSON.parse(fs.readFileSync(path.join(home,'English','v1051-rendered-choice-aliases.json'),'utf8')).derived_aliases;
const realV1051Engrams=JSON.parse(fs.readFileSync(path.join(home,'English','v1051-engram-overrides.json'),'utf8'));
const realV1052=JSON.parse(fs.readFileSync(path.join(home,'English','three-in-one-3-3-5-v1-052','dictionary.json'),'utf8'));
const realV1052Catalog=JSON.parse(fs.readFileSync(path.join(home,'English','three-in-one-3-3-5-v1-052','catalog.json'),'utf8'));
const realV1052Manifest=JSON.parse(fs.readFileSync(path.join(home,'English','three-in-one-3-3-5-v1-052','manifest.json'),'utf8'));
assert.strictEqual(realV1052Manifest.VersionId,'three-in-one-3-3-5-v1-052');
assert.strictEqual(Object.keys(realV1052).length,realV1052Manifest.TranslatedTexts);
assert.strictEqual(Object.keys(realV1052Catalog).length,realV1052Manifest.TotalTexts);
assert.strictEqual(Object.keys(realV1052).length,Object.keys(realV1051).length,'v1.052 must preserve the complete v1.051 English pack');
for(const [key,value] of Object.entries(realV1051))
    assert.strictEqual(realV1052[key],value,'Ver1.052 regressed existing translation: '+key);
assert.notStrictEqual(realV1051Manifest.Fingerprints['data/Enemies.json'],realV1052Manifest.Fingerprints['data/Enemies.json'],
    'v1.052 source-specific enemy marker must distinguish previous version');
assert.strictEqual(Object.keys(realV1051).length,realV1051Manifest.TranslatedTexts);
assert.strictEqual(Object.keys(realV1051Catalog).length,realV1051Manifest.TotalTexts);
assert.strictEqual(Object.keys(realV1051Aliases).length,6);
assert.strictEqual(Object.keys(realV1051Engrams).length,9);
for(const [key,value] of Object.entries(realV1051Aliases)){
    assert.strictEqual(realV1051[key],value,'the version 1.051 post-guard choice caption must translate');
    assert.strictEqual(realV1051Catalog[key].kinds[0],'native-rendered-choice-alias');
}
const v103Dictionary=JSON.parse(fs.readFileSync(path.join(home,'English','three-in-one-3-3-5-v1-03','dictionary.json'),'utf8'));
// Actual v1.04 native plugin labels and historical dynamic displays must
// survive when the v1.04 catalog is rebuilt from game data.
const restoredNativeLabels=[
    '\u6575\u5168\u4f53', // FTKR: All Enemies
    '\u88c5\u5099\u4e0d\u53ef', // FTKR: Cannot Equip
    '\u5bf9h\u7684\u770b\u6cd5', // HypState: View of Sex
    '\u53e3\u8154\u4fb5\u8680\u5ea6', // HypState: Oral Corruption
    '\u5473\u65b9\u5168\u4f53', // FTKR: All Allies
];
restoredNativeLabels.forEach(key=>{
    assert.ok(Object.prototype.hasOwnProperty.call(v103Dictionary,key),'v1.03 legacy source must be known: '+key);
    assert.strictEqual(realV104Titles[key],v103Dictionary[key],'v1.04 lost a verified legacy display: '+key);
});
const nativeV104Title=Object.keys(realV104Titles).find(k=>k.startsWith('魔法少女苍蓝星★三合一改 C变身/'));
assert.ok(nativeV104Title,'real v1.04 source title must be inventoried and translated');
Scene_Boot.prototype.updateDocumentTitle=function(){context.document.title=nativeV104Title;};
const imageRequests=[];
const imageLoader={
    loadBitmap(folder,name,hue,smooth){
        const result={folder,name,hue,smooth};imageRequests.push(result);return result;
    },
    loadPicture(name,hue){return this.loadBitmap('img/pictures/',name,hue,true);},
    loadAnimation(name,hue){return this.loadBitmap('img/animations/',name,hue,true);}
};
const context = {Bitmap, Window_Base, Window_Message, Window_SavefileList, Window_SaveInfo, Scene_Boot, Spriteset_Map, Sprite_Picture, console, document:{title:''}, location: {pathname: '/C:/fixture/www/index.html'},
    TickerManager:{show:function(text){this.lastText=text;return context.$gameVariables.value(2944);}},
    ImageManager:imageLoader,
    CelesphoniaEnglishImageOverrides:{picture:{Glossary_1:'Glossary_1',Glossary_2:'Glossary_2',Glossary_4:'Glossary_4',hair_win_UI:'hair_win_UI'},animation:{'催眠用语':'HypnosisWords'}},
    Yanfly:{Param:{SaveNotePrompt:'请为该记忆添加专属刻印描述'}},prompt:function(){promptCalls.push(Array.from(arguments));return this.promptResult;},promptResult:null,
    $gameVariables: {value: id => data.variables[id]},
    $dataItems: data.items,
    CelesphoniaEnglishDictionary: {'天音': 'Amane', '是': 'Yes', '否': 'No', '雷': 'Lightning', '秘密记忆': 'Secret Memories',
        '\\C[7][発情値] \\C[8]\\V[2407] \\C[7]以上':'\\C[7][Arousal] \\C[8]\\V[2407] \\C[7]or above',
        '进度\\V[4]/\\V[4]':'Progress \\V[4]/\\V[4]', '私立二宫市学校':'Private Ninomiya Academy',
        '怪魔的魂片(极小)': 'Monstrum Soul Fragment (Tiny)', '怪魔的魂片(中)': 'Monstrum Soul Fragment (Medium)',
        '任务完成！': 'Quest completed!', '原文一\n原文二': 'First line\nSecond line', '\\c[5]%1受到了%2点伤害！': '\\c[5]%1 took %2 damage!',
        '\\c[17]获得了 : %1 %2 x %3 %4': '\\c[17]Obtained: %1 %2 x %3 %4',
        '发光的徽章。说不定有人在收集这个。': 'A glowing medal. Someone might collect these.',
        '\\n[1]抵抗着触手，\n但是触手力量强大。\n\\n[1]无法挣脱。': '\\n[1] struggles against the tentacle,\nbut it is too strong.\n\\n[1] cannot break free.',
        '%9倒下了！': '%9 collapsed!', '%9 : 最近真是无聊啊。': '%9 : Things have been so boring lately.', '\\n[1]的声': "\\n[1]'s Voice", '<魂(Sl)>': 'Soul (SL)', '学校1F走廊': 'School 1F Hallway',
        '\\n<\\n[1]>\\n[1]前辈才要注意啊？\n因为平常太过正经了所以有些迟钝呢……。': '\\n<\\n[1]>You are the one who needs to be careful, \\n[1]-senpai!\nYou are always so serious that you can be a little clueless...'},
    CelesphoniaEnglishLabels: {'天音': 'Amane', '淫欲': 'Lust', '支配度': 'Domination', '物理回避率': 'Phys. Evasion',
        '闪闪发光的徽章': 'Shiny Medal', '和怪魔的接触': 'Contact with a Monstrum'},
    CelesphoniaEnglishDescriptions: {'发光的徽章。说不定有人在收集这个。': 'A glowing medal. Someone might collect these.'},
    CelesphoniaEnglishHumanLabels: {'A.性爱经验': 'Anal Sex Exp.', '口开发度': 'Oral Dev.'},
    CelesphoniaEnglishMeta: {version: 'three-in-one-3-3-4'}, addEventListener() {}, setInterval() {},
    require: name => name === 'path' ? path.win32 : {existsSync: () => false, writeFileSync: (file, text) => { written[file] = text; }}
};
Object.assign(context.CelesphoniaEnglishDictionary, {
 '\x1bN[101]凭借魔导书·娅露斯的力量变身了！':'\x1bN[101] transformed with the power of Alrus!',
 '\\\\N[360]到了。':'It is now \\\\N[360].',
 '\\\\n[1]喝进了触手的体液……。':'\\\\n[1] drank the tentacle fluid....',
 '发情(微)':'Arousal (Mild)', '媚药侵染(Hypersensitivity)':'Aphrodisiac Corruption (Hypersensitivity)',
 '近战':'Melee', '虚脱':'Exhaustion', '无垢天衣':'Innocent Gear', '下午':'Afternoon',
 '\\v[616]次连续绝顶！':'\\v[616] consecutive orgasms!', '连续绝顶！':'Consecutive Orgasms!',
 '今天的课表是：\n上午：性教育课\n下午：\\cm[课程名]':"Today's schedule:\nMorning: Sex Education\nAfternoon: \\cm[课程名]", '数学':'Mathematics'
});
Object.keys(realV104Titles).filter(k=>k.startsWith('魔法少女苍蓝星★三合一改')).forEach(
    k=>context.CelesphoniaEnglishDictionary[k]=realV104Titles[k]
);
restoredNativeLabels.forEach(k=>context.CelesphoniaEnglishDictionary[k]=realV104Titles[k]);
for(const key of [...Object.keys(realV1051Aliases),'污浊宝珠','邪宝珠','不知廉耻','技能/状态/装备详细信息']){
    assert.ok(realV1051[key],'missing creator Ver1.051 English label: '+key);
    context.CelesphoniaEnglishDictionary[key]=realV1051[key];
}
for(const [key,value] of Object.entries(realV1051Engrams)){
    assert.strictEqual(realV1051[key],value,'reviewed Skill Engram source must have exact English display');
    context.CelesphoniaEnglishDictionary[key]=value;
}
Object.assign(context.CelesphoniaEnglishDictionary,{'交给\\n[1]。\n\\c[6]任务条件\\c[0]':'Give it to \\n[1].\n\\c[6]Quest requirements\\c[0]','变节者成员':'Renegade Member'});
Object.assign(context.CelesphoniaEnglishDictionary,{
 '\\n<\\n[1]事情结束了。':'\\n<\\n[1]It is over.',
 '\\n<\\n[1]的声音>到了':"\\n<\\n[1]'s Voice>Here."
});
context.CelesphoniaEnglishLabels['次']='times';
context.CelesphoniaEnglishDictionary['月']='Mon';context.CelesphoniaEnglishLabels['月']='Mon';
Object.assign(context.CelesphoniaEnglishDictionary, {
 '日':'Sun', '水':'Water', '土':'Earth',
 '第一阶段':'Stage One', '第二阶段':'Stage Two', '第三阶段':'Stage Three',
 '\\c[4]\\v[202]星期(\\v[203]) \\v[211]\\c[0]':'\\c[4]Day \\v[202] (weekday \\v[203]) \\v[211]\\c[0]'
});
Object.assign(context.CelesphoniaEnglishDictionary,{
 '变节者:纵火者':'Renegade: Arsonist',
 '%1的%2上升了！':"%1's %2 has increased!",
 '%9的攻击力大幅上升了！':"%9's Attack has greatly increased!",
 "%9's 攻击力大幅 has increased!":"%9's Attack has greatly increased!",
 '%9 : （\\\\n[6]会来救我的……想办法扛过去吧……）':'%9 : (\\\\n[6] will come to save me... I have to hold out...)'
});
context.CelesphoniaEnglishLabels['变节者']='Renegade';
context.$dataEnemies=[null,{name:'变节者:纵火者'}];
context.$gameActors={_data:{1:{name:()=>data.actors[1].name},101:{name:()=>data.actors[101].name},360:{name:()=>data.actors[360].name}}};
const reinforcementCalls=[];
function Game_Interpreter() {}
Game_Interpreter.prototype.pluginCommand=function(name,args){
 if(name!=='add_enemy_troop')return 'delegated';
 for(const id of args){reinforcementCalls.push(id);context.BattleManager._spriteset.removeEnemies();context.BattleManager._spriteset.createEnemies();}
 if(args.includes('throw'))throw new Error('native command failure');
 return 'native-result';
};
const spriteCalls=[];
const spritePrototype={removeEnemies(){spriteCalls.push('remove');},createEnemies(){spriteCalls.push('create');}};
context.Game_Interpreter=Game_Interpreter;context.BattleManager={_spriteset:Object.create(spritePrototype)};context.$gameParty={inBattle:()=>true};
context.CelesphoniaEnglishEnemyPrefixes={'锋利的':'Sharp','迅捷的':'Swift'};
Object.assign(context.CelesphoniaEnglishEnemyPrefixes,{'先攻的':'Preemptive','灵巧的':'Nimble'});
context.CelesphoniaEnglishEnemyNames={'粘液体:Slime':'Slime: Slime','Slime: Slime':'Slime: Slime'};
context.window = context; vm.createContext(context);
context.CelesphoniaEnglishDictionary['请为该记忆添加专属刻印描述']='Enter a note for this save slot.';
vm.runInContext(fs.readFileSync(path.join(home, 'EnglishRuntime.js'), 'utf8'), context);
// Simulate a later plugin alias of Scene_Boot.start; it must still install the overlay.
const boot = Scene_Boot.prototype.start;
Scene_Boot.prototype.start = function () { calls.push('plugin'); return boot.call(this); };
new Scene_Boot().start();
const tickerOriginal='\\i[\\v[119]]\\c[\\v[118]]\\v[\\v[2995]]';
const tickerSource=Object.keys(realV1052).find(k=>k.includes('舔得这么津津有味'));
assert(tickerSource && realV1052[tickerSource]);
const originalVariablesReader=context.$gameVariables.value;
const originalSavedVariable=data.variables[2944];
const originalSelector=data.variables[2995];
context.CelesphoniaEnglishDictionary[tickerSource]=realV1052[tickerSource];
// The runtime keeps a reference to the English dictionary; this is an
// isolated test fixture, not an update to the installed game dictionary.
assert.strictEqual(context.TickerManager.show('Unrelated ticker'),originalSavedVariable,
    'Unrelated tickers must retain the native result');
data.variables[2944]=tickerSource.replace(/\\/g,'\x1b');
data.variables[2995]=2944;
assert.strictEqual(context.$gameVariables.value,originalVariablesReader);
const savedGroperSource=data.variables[2944];
const localizedGroper=context.TickerManager.show(tickerOriginal);
assert(localizedGroper.includes('Licking it so eagerly'),
    'The source-matched groper ticker must render the reviewed English sentence');
assert.strictEqual(context.TickerManager.lastText,tickerOriginal,
    'Native ticker command arguments must remain unchanged');
assert.strictEqual(data.variables[2944],savedGroperSource);
assert.strictEqual(context.$gameVariables.value,originalVariablesReader);
if(originalSavedVariable===undefined)delete data.variables[2944];
else data.variables[2944]=originalSavedVariable;
if(originalSelector===undefined)delete data.variables[2995];
else data.variables[2995]=originalSelector;
for(const metric of ['痴汉兴奋度','痴汉聚集度']){
    for(const rank of ['熟练','普通','生疏']){
        const pic=new Sprite_Picture();
        const original='Turn \\v[1506] / 10\n'+metric+' \\v[1507]\n痴汉LV '+rank+'\n';
        const rendered='Turn 5 / 10\n'+metric+' 23\n痴汉LV '+rank+'\n';
        pic.dTextInfo={originalValue:original,value:rendered,realTime:true,size:20};
        const liveInfo=pic.dTextInfo;
        const english=pic.makeDynamicBitmap();
        assert(english.startsWith('Turn 5 / 10\nGroper '));
        assert(english.includes(' 23\nGroper Level: '));
        assert.strictEqual(pic.dTextInfo,liveInfo,'Groper HUD must never modify native source info');
        assert.strictEqual(pic.dTextInfo.value,rendered,'Groper HUD must never mutate game numeric variables');
    }
}
const unknownSprite=new Sprite_Picture();
unknownSprite.dTextInfo={originalValue:'Turn \\v[1506] / 10\nUnknown \\v[1507]\n痴汉LV 熟练\n',
                         value:'Turn 5 / 10\nUnknown 23\n痴汉LV 熟练\n'};
assert.strictEqual(unknownSprite.makeDynamicBitmap(),unknownSprite.dTextInfo.value,
    'Unknown native HUDs must not receive the groper translation');
for(const name of ['Glossary_1','Glossary_2','Glossary_4','hair_win_UI']){
    const bmp=imageLoader.loadPicture(name,0);
    assert.strictEqual(bmp.folder,'celesphonia-english/images/');
    assert.strictEqual(bmp.name,name);
}
const animated=imageLoader.loadAnimation('催眠用语',0);
assert.strictEqual(animated.folder,'celesphonia-english/images/');
assert.strictEqual(animated.name,'HypnosisWords');
assert.strictEqual(imageLoader.loadPicture('OtherPicture').folder,'img/pictures/',
    'An unrelated native picture must not be redirected');
assert.strictEqual(imageLoader.loadAnimation('OtherAnimation').folder,'img/animations/',
    'An unrelated native animation must not be redirected');
new Scene_Boot().updateDocumentTitle();
assert.strictEqual(context.document.title,realV104Titles[nativeV104Title],
    'v1.04 boot window title and C/A/TAB instructions must be rendered in English');
const window = new Window_Base();
for(const [key,value] of Object.entries(realV1051Aliases)){
    assert.strictEqual(window.drawText(key),value,'MPP_ChoiceEX displayed choice was not translated');
    assert.strictEqual(context.CelesphoniaEnglish.translate(key),value,'original non-guarded menu choice did not translate');
}
for(const [key,value] of Object.entries(realV1051Engrams)){
    assert.strictEqual(window.drawText(key),value,'Engram native description line was not translated');
    assert.strictEqual(context.CelesphoniaEnglish.translate(key),value,'Engram exact string was not translated');
}
assert.strictEqual(window.drawText('污浊宝珠'),'Tainted Orb');
assert.strictEqual(window.drawText('邪宝珠'),'Wicked Orb');
assert.strictEqual(window.drawText('不知廉耻'),'Shamelessness');
assert.strictEqual(window.drawText('技能/状态/装备详细信息'),'Skill/Status/Equip. Info');
restoredNativeLabels.forEach(key=>{
    assert.strictEqual(context.CelesphoniaEnglish.translate(key),v103Dictionary[key],
        'legacy native/runtime display must render in English: '+key);
    assert.strictEqual(window.drawText(key),v103Dictionary[key],
        'native window must display the restored label in English: '+key);
});
['第一阶段','第二阶段','第三阶段'].forEach((label,i)=>{
 assert.equal(window.convertEscapeCharacters('\\n<\\n['+label+']>Status'),'\x1bn<Stage '+['One','Two','Three'][i]+'>Status','literal stage titles inside native nonnumeric actor name boxes render from their separately cataloged labels');
});
const saveWindow=new Window_SavefileList(),saveInfo={title:'Magical Girl Celesphonia☆三合一改 C[Transformation]解除变身 角色详细信息',playtime:'01:18:32',characters:[['Actor1',0]]},savedMetadata=JSON.stringify(saveInfo);
assert.equal(saveWindow.drawGameTitle(saveInfo,192,0,700),'Magical Girl Celesphonia | 3-in-1 3.3.4');
assert.equal(JSON.stringify(saveInfo),savedMetadata,'save-slot rendering never rewrites stored title, characters or playtime');
saveWindow.drawGameTitle(saveInfo,192,0,120);
assert.ok(saveWindow._celesphoniaEnglishSaveTitle.width<=120,'narrow save headers fit without horizontal glyph squeezing');
assert.equal(saveWindow.contents.fontSize,28,'save-slot title sizing restores the normal font');
assert.equal(saveWindow.drawGameTitle({title:'My custom title'},0,0,700),'My custom title','unrelated titles remain intact');
const panel=new Window_SaveInfo();panel._info=saveInfo;
assert.equal(panel.drawGameTitle(12),66,'Maple custom panel preserves its native vertical advance');
assert.strictEqual(panel._info,saveInfo);assert.equal(panel.contents.fontSize,28);
assert.equal(panel._celesphoniaEnglishSaveTitle.caption,'Magical Girl Celesphonia | 3-in-1 3.3.4');
panel.raise=true;assert.throws(()=>panel.drawGameTitle(0),/native draw failure/);
assert.strictEqual(panel._info,saveInfo,'restore the original header even when native rendering fails');
assert.equal(JSON.stringify(saveInfo),savedMetadata);assert.equal(panel.contents.fontSize,28);
assert.strictEqual(context.prompt(context.Yanfly.Param.SaveNotePrompt,'用户的备注'),null,'preserve cancellation');
assert.deepEqual(promptCalls.pop(),['Enter a note for this save slot.','用户的备注']);
context.promptResult='unchanged user input';assert.equal(context.prompt('unrelated prompt','native default'),'unchanged user input');
assert.deepEqual(promptCalls.pop(),['unrelated prompt','native default']);
assert.equal(context.CelesphoniaEnglish.translate('灵巧的变节者:纵火者Ａ'),'Nimble Renegade: ArsonistＡ','native full-width unique-enemy letters are recognized');
assert.equal(context.CelesphoniaEnglish.translate('Renegade:纵火者Ｂ'),'Renegade: ArsonistＢ','recover a complete known caption from an older partial rendering');
assert.equal(context.CelesphoniaEnglish.translate('先攻的变节者:纵火者Ｄ的攻击力大幅上升了！'),"Preemptive Renegade: ArsonistＤ's Attack has greatly increased!",'specific state grammar must not split a decorated enemy name at its first possessive marker');
assert.equal(context.CelesphoniaEnglish.translate("灵巧的变节者:纵火者Ａ's 攻击力大幅 has increased!"),"Nimble Renegade: ArsonistＡ's Attack has greatly increased!");
assert.equal(context.CelesphoniaEnglish.translate("先攻's Renegade:纵火者Ｄ的攻击力大幅 has increased!"),"Preemptive Renegade: ArsonistＤ's Attack has greatly increased!",'an older split log is repaired only from known enemy-prefix and stat records');
assert.equal(context.CelesphoniaEnglish.translate('Amane : （Memoria会来救我的……想办法扛过去吧……）'),'Amane : (Memoria will come to save me... I have to hold out...)','mixed native speaker/actor interpolation translates the whole ticker sentence');
assert.equal(context.CelesphoniaEnglish.translate('Amane : （会来救我的……想办法扛过去吧……）'),'Amane : ( will come to save me... I have to hold out...)','an empty native actor name does not prevent matching the complete known sentence');
assert.equal(context.CelesphoniaEnglish.translate('2 星期(二)'), '2 Tue');
assert.equal(context.CelesphoniaEnglish.translate('星期三(Afternoon)'), 'Wed(Afternoon)');
assert.equal(context.CelesphoniaEnglish.translate('这是星期二发生的未知完整句子。'), '这是星期二发生的未知完整句子。');
const calendarSnapshot=JSON.stringify(data);
Object.entries({'日':'Sun','一':'Mon','二':'Tue','三':'Wed','四':'Thu','五':'Fri','六':'Sat','月':'Mon','火':'Tue','水':'Wed','木':'Thu','金':'Fri','土':'Sat'}).forEach(([glyph,day])=>{
 assert.equal(context.CelesphoniaEnglish.translate('Day 12 (weekday '+glyph+') Afternoon'),'Day 12 ('+day+') Afternoon');
 assert.equal(context.CelesphoniaEnglish.translate('12 星期('+glyph+')'),'12 '+day);
});
assert.equal(JSON.stringify(data),calendarSnapshot,'calendar rendering must preserve saved variables');
Object.entries({'日':'Sun','一':'Mon','二':'Tue','三':'Wed','四':'Thu','五':'Fri','六':'Sat','月':'Mon','火':'Tue','水':'Wed','木':'Thu','金':'Fri','土':'Sat'}).forEach(([glyph,day])=>{
 Object.assign(data.variables,{202:12,203:glyph,211:'下午'});
 const stored=JSON.stringify(data);
 assert.equal(window.convertEscapeCharacters('\\c[4]\\v[202]星期(\\v[203]) \\v[211]\\c[0]'),'\x1bc[4]Day 12 ('+day+') Afternoon\x1bc[0]','reviewed calendar templates keep weekday context through native variable expansion');
 assert.equal(window.convertEscapeCharacters('\\c[6]\\v[202]星期(\\v[203]) \\v[211]\\c[0]'),'\x1bc[6]12'+day+' Afternoon\x1bc[0]','older calendar frames resolve before standalone glyph translations');
 assert.equal(JSON.stringify(data),stored);
});
delete data.variables[202];delete data.variables[203];delete data.variables[211];
assert.equal(JSON.stringify(data),calendarSnapshot,'calendar fixture restores original stored variables');
assert.equal(context.CelesphoniaEnglish.translate('12月'),'12月','calendar support must not alter a numeric month');
assert.equal(context.CelesphoniaEnglish.translate('锋利的迅捷的粘液体:Slime A'), 'Sharp Swift Slime: Slime A');
assert.equal(context.CelesphoniaEnglish.translate('锋利的未知完整句子。'), '锋利的未知完整句子。');
const command=new Game_Interpreter();assert.equal(command.pluginCommand('add_enemy_troop',[11,22,33]),'native-result');
assert.deepEqual(reinforcementCalls,[11,22,33]);assert.deepEqual(spriteCalls,['remove','create']);
assert.equal(Object.hasOwn(context.BattleManager._spriteset,'createEnemies'),false);
assert.throws(()=>command.pluginCommand('add_enemy_troop',['throw']),/native command failure/);
assert.equal(Object.hasOwn(context.BattleManager._spriteset,'removeEnemies'),false);
assert.equal(command.pluginCommand('unrelated',[]),'delegated');

assert.equal(window.drawTextEx('\\ii[301] * 3\n\\ii[303] * 7'), '\x1bI[2245]Monstrum Soul Fragment (Tiny) * 3\n\x1bI[2245]Monstrum Soul Fragment (Medium) * 7', 'quest reward icon references translate complete parenthesized item names and retain quantities');
assert.equal(window.drawText('是'), 'Yes');
assert.equal(window.drawText('雷'), 'Lightning', 'whole menu labels translate');
assert.equal(new Bitmap().drawText('雷'), '雷', 'individual message glyphs do not translate');
assert.equal(new Bitmap().measureTextWidth('雷'), 1, 'individual glyph widths stay unchanged');
assert.equal(window.drawTextEx('原文一\n原文二'), 'First line\nSecond line');
assert.equal(window.drawTextEx('\\N[1]: \\V[4]'), 'Amane: Secret Memories');
assert.equal(window.drawTextEx('\\c[5]天音受到了12点伤害！'), '\x1bc[5]Amane took 12 damage!');
assert.equal(context.CelesphoniaEnglish.translate('\x1bc[5]Amane受到了12点伤害！'), '\x1bc[5]Amane took 12 damage!', 'templates also match already-converted display codes');
assert.equal(new Bitmap().measureTextWidth('秘密记忆'), 'Secret Memories'.length);
const message = new Window_Message();
assert.equal(message.drawTextEx('原文一\n原文二'), 'First line\nSecond line');
assert.equal(message._wordWrap, true, 'English dialogue wraps in the window');
message.drawTextEx('未翻译原文');
assert.equal(message._wordWrap, false, 'original dialogue keeps its original wrap setting');
assert.equal(window.drawText('未知菜单'), '未知菜单');
assert.equal(window.drawTextEx('\\i[3202] \\c[27]淫欲\\c[0] 2 / 200'), '\x1bi[3202] \x1bc[27]Lust\x1bc[0] 2 / 200');
assert.equal(window.drawTextEx('\\c[4]物理回避率\\c[0]: 35%'), '\x1bc[4]Phys. Evasion\x1bc[0]: 35%');
assert.equal(window.drawTextEx('\\c[6] 任务完成！'), '\x1bc[6] Quest completed!', 'complete notifications translate within display controls');
assert.equal(window.drawText('Welcome, 天音-senpai.'), 'Welcome, Amane-senpai.', 'names in mixed-language display strings translate');
assert.equal(window.drawText('天音心中的淫欲仍然未知。'), '天音心中的淫欲仍然未知。', 'no substring replacement inside an unknown Chinese sentence');
assert.equal(context.CelesphoniaEnglish.translate('\\n<天音>未翻译原文'), '\\n<天音>未翻译原文', 'name-box control payload stays intact');
assert.equal(window.drawTextEx('\\c[4]<魂(Sl)>\\c[0] +20'), '\x1bc[4]Soul (SL)\x1bc[0] +20', 'exact human labels in angle brackets translate within controls');
assert.equal(context.CelesphoniaEnglish.translate('<未知标签> +20'), '<未知标签> +20', 'unknown angle tags remain intact');
assert.equal(context.CelesphoniaEnglish.translate('<config name="天音">'), '<config name="天音">', 'plugin tag attributes remain intact');
assert.equal(window.drawTextEx('\\c[27]<A.性爱经验>\\c[0] 12'), '\x1bc[27]‹Anal Sex Exp.›\x1bc[0] 12', 'picture captions with ASCII prefixes remain visible human text');
assert.equal(window.drawTextEx('\\f[i]\\c[5]<口开发度> LV 0 / 3'), '\x1bf[i]\x1bc[5]‹Oral Dev.› LV 0 / 3', 'profile development caption translates independently of dynamic values');
assert.equal(context.CelesphoniaEnglish.translate('\\n<A.性爱经验>未知正文'), '\\n<A.性爱经验>未知正文', 'a known human caption inside a name-box control remains opaque');
assert.equal(window.drawTextEx('Location: 【学校1F走廊】'), 'Location: 【School 1F Hallway】', 'decorated locations match as complete fields despite floor numbers');
assert.equal(context.CelesphoniaEnglish.translate('【未知地点1F】'), '【未知地点1F】', 'unknown decorated locations remain intact');
assert.equal(window.drawTextEx('\\i[906]Female Student : 最近真是无聊啊。'), '\x1bi[906]Female Student : Things have been so boring lately.', 'native NPC ticker composition translates the whole cataloged sentence');
assert.equal(window.drawTextEx('\\c[17]获得了 : \\I[2692] 闪闪发光的徽章 x 1 发光的徽章。说不定有人在收集这个。'),
    '\x1bc[17]Obtained: \x1bI[2692] Shiny Medal x 1 A glowing medal. Someone might collect these.', 'formatted item pickup translates its item name and complete description');
assert.equal(window.drawText('★和怪魔的接触'), '★Contact with a Monstrum', 'title decorators stay visible');
assert.equal(window.drawTextEx('Amane抵抗着触手，\n但是触手力量强大。\nAmane无法挣脱。'), 'Amane struggles against the tentacle,\nbut it is too strong.\nAmane cannot break free.', 'name-expanded messages match as complete blocks before partial lines');
assert.equal(window.drawText('Amane倒下了！'), 'Amane collapsed!', 'state suffix templates preserve actor names');
assert.equal(window.drawText('未知角色倒下了！'), '未知角色 collapsed!', 'an unknown actor name is preserved rather than guessed');
assert.equal(window.drawTextEx('\\N[1]的声'), "Amane's Voice", 'speaker-label actor substitutions translate only at display time');
assert.equal(window.drawTextEx('\\n<\\n[1]>\\n[1]前辈才要注意啊？\n因为平常太过正经了所以有些迟钝呢……。'), '\x1bn<Amane>You are the one who needs to be careful, Amane-senpai!\nYou are always so serious that you can be a little clueless...', 'canonical dialogue controls retain actor slots through conversion');
function PrivateTicker() { Window_Base.call(this); this.addPlus = false; }
PrivateTicker.prototype = Object.create(Window_Base.prototype);
PrivateTicker.prototype.convertEscapeCharacters = function (text) {
    text = text.replace(/\\/g, '\x1b');
    for (let i = 0; i < 2; i++) text = text.replace(/\x1bV\[(\d+)\]/gi, (_, id) => (data.variables[id] > 0 && this.addPlus ? '+' : '') + data.variables[id]);
    return text.replace(/\x1bN\[(\d+)\]/gi, (_, id) => this.actorName(id));
};
const ticker = new PrivateTicker();
assert.equal(ticker.drawTextEx(data.tickerLog[0]), '\x1bi[3202]\x1bc[4] Quest completed!', 'private ticker converter translates after nested icon/color variable expansion');
assert.equal(ticker.convertEscapeCharacters('\\i[\\v[119]]\\c[\\v[118]]\\v[616]次\\v[\\v[2995]]'),'\x1bi[892]\x1bc[27]2 consecutive orgasms!','native Heart streak expands as one complete message');
assert.equal(context.CelesphoniaEnglish.translate('999次连续绝顶！'),'999 consecutive orgasms!','streak count is unrestricted within the complete numeric grammar');
assert.equal(context.CelesphoniaEnglish.translate('2 times连续绝顶！'),'2 consecutive orgasms!','observed partial counter matches only the complete known streak grammar');
assert.equal(context.CelesphoniaEnglish.translate('未知故事2 times连续绝顶！还有文字'),'未知故事2 times连续绝顶！还有文字','legacy streak repair does not replace prose fragments');
assert.equal(window.drawTextEx('\\v[376]'),'Give it to Amane.\n\x1bc[6]Quest requirements\x1bc[0]','mixed native quest escapes translate before actor expansion');
assert.equal(window.drawTextEx('\\n<\\n[变节者成员]>unknown'),'\x1bn<Renegade Member>unknown','malformed native nonnumeric actor caption resolves only a reviewed name-box label');
assert.equal(window.drawTextEx('\\n<\\n[未知标签]>unknown'),'\x1bn<\x1bn[未知标签]>unknown','unknown malformed captions remain original');
assert.equal(window.drawTextEx('\\n<\\n[1]事情结束了。'),'\x1bn<Amane>It is over.','reviewed unterminated numeric actor name box is repaired on the display copy');
assert.equal(window.drawTextEx('\\n<\\n[1]Unreviewed text'),'\x1bn<AmaneUnreviewed text','unreviewed unterminated captions retain native behavior');
assert.equal(window.drawTextEx('\\n<\\n[1]的声音>到了'),"\x1bn<Amane's Voice>Here.",'valid compound caption is not closed early');
assert.equal(context.CelesphoniaEnglish.translate('未知故事2次连续绝顶！还有文字'),'未知故事2次连续绝顶！还有文字','streak grammar does not replace fragments of unknown prose');
ticker.addPlus = true;
assert.equal(ticker.convertEscapeCharacters('\\V[8]'), '+3202', 'custom ticker numeric formatting stays intact');
assert.equal(ticker.convertEscapeCharacters('\\N[1]倒下了！'), 'Amane collapsed!', 'private ticker translates native state concatenations');
assert.equal(ticker.drawTextEx('\\I[2692] 闪闪发光的徽章 x 1 发光的徽章。说不定有人在收集这个。'), '\x1bI[2692] Shiny Medal x 1 A glowing medal. Someone might collect these.', 'item first-line description renders in the private ticker');
assert.equal(window.drawText('未知句子包含发光的徽章。说不定有人在收集这个。还有内容'), '未知句子包含发光的徽章。说不定有人在收集这个。还有内容', 'description fields never replace fragments inside unknown Chinese sentences');
assert.equal(context.CelesphoniaEnglish.translate('\x1bi[1650]\x1bc[4]Amane凭借魔导书·娅露斯的力量变身了！'), '\x1bi[1650]\x1bc[4]Amane transformed with the power of Alrus!', 'ESC actor templates match complete wrapped transformation notices');
assert.equal(window.drawTextEx('\\i[\\V[8]]\\c[\\V[9]]\\N[101]凭借魔导书·娅露斯的力量变身了！'), '\x1bi[3202]\x1bc[4]Amane transformed with the power of Alrus!', 'nested icon/color variables retain their native values');
assert.equal(context.CelesphoniaEnglish.translate('\x1bi[0]\x1bc[0]\x1b天音喝进了触手的体液……。'), '\x1bi[0]\x1bc[0]Amane drank the tentacle fluid....', 'stray doubled name escape is stripped only for the actual actor');
assert.equal(window.drawTextEx('\\i[0]\\\\N[360]到了。'), '\x1bi[0]It is now Afternoon.', 'doubled actor escapes are normalized only in translated presentation text');
assert.equal(window.drawTextEx('\\i[892]发情(微)(26)'), '\x1bi[892]Arousal (Mild)(26)', 'state counters preserve the number');
assert.equal(window.drawTextEx('\\I[2518]\\c[27]媚药侵染(Hypersensitivity)'), '\x1bI[2518]\x1bc[27]Aphrodisiac Corruption (Hypersensitivity)');
assert.equal(window.drawText('近战Ⅳ'), 'MeleeⅣ');
assert.equal(window.drawText('About "无垢天衣"'), 'About "Innocent Gear"');
assert.equal(window.drawText('<[虚脱]>'), '‹Exhaustion›');
assert.equal(context.CelesphoniaEnglish.translate('发情(微)仍然是未知完整句子。'), '发情(微)仍然是未知完整句子。');
assert.equal(JSON.stringify(data), before, 'stored actor names, variables, messages and choices must stay unchanged');
context.CelesphoniaEnglish.install();
assert.equal(window.drawText('否'), 'No', 'reinstall must not duplicate hooks');
context.CelesphoniaEnglish.flushSeen();
assert.equal(Object.keys(written).length, 1);
assert('未知菜单' in JSON.parse(Object.values(written)[0]));
const reports = [];
assert.equal(context.CelesphoniaEnglish.translate('\x1bC[7][発情値] \x1bC[8]100 \x1bC[7]以上'),'\x1bC[7][Arousal] \x1bC[8]100 \x1bC[7]or above','native numeric requirement keeps color and threshold');
assert.equal(context.CelesphoniaEnglish.translate('进度23/23'),'Progress 23/23','complete numeric progress grammar');
assert.equal(context.CelesphoniaEnglish.translate('进度23/24'),'进度23/24','inconsistent repeated slots cannot match');
assert.equal(context.CelesphoniaEnglish.translate('未知故事进度23/23还有文字'),'未知故事进度23/23还有文字','numeric templates cannot match fragments of unknown prose');
assert.equal(context.CelesphoniaEnglish.translate('月'),'Mon','the complete native weekday field translates');
assert.equal(context.CelesphoniaEnglish.translate('10月'),'10月','a weekday label cannot corrupt an unrelated numeric date suffix');
assert.equal(context.CelesphoniaEnglish.translate('今天的课表是：\n上午：性教育课\n下午：0'),"Today's schedule:\nMorning: Sex Education\nAfternoon: 0",'native named-variable expansion preserves an empty course value');
assert.equal(context.CelesphoniaEnglish.translate('今天的课表是：\n上午：性教育课\n下午：数学'),"Today's schedule:\nMorning: Sex Education\nAfternoon: Mathematics",'native named-variable expansion translates a known complete course value');
assert.equal(context.CelesphoniaEnglish.translate('别的对话今天的课表是：\n上午：性教育课\n下午：数学').includes("Today's schedule"),false,'named-variable grammar cannot consume a fragment of unrelated dialogue');
const mapSprites=new Spriteset_Map();mapSprites.createUpperLayer();const mapContext=mapSprites._hzChoiceScreen.bitmap.context,originalFont=mapContext.font;
mapContext.fillText('私立二宫市学校',100,100);
assert.equal(mapCalls[0].text,'Private Ninomiya Academy');assert.equal(mapCalls[0].width,112);assert.equal(mapContext.font,originalFont,'map font is restored after drawing');
for (const id of ['gorpg-1-23', 'maple-1-7-2', 'three-in-one-3-3-4', 'three-in-one-3-3-5-trial','three-in-one-3-3-5-v1-01']) {
    const directory = path.join(home, 'English', id), dict = JSON.parse(fs.readFileSync(path.join(directory, 'dictionary.json'), 'utf8'));
    const meta = JSON.parse(fs.readFileSync(path.join(directory, 'manifest.json'), 'utf8'));
    const catalog = JSON.parse(fs.readFileSync(path.join(directory, 'catalog.json'), 'utf8'));
    assert.equal(Object.keys(dict).length, meta.TranslatedTexts);
    const controls = /(?:\\|\x1b)(?:[A-Za-z]+(?:\[[^\]\r\n]*\]|<[^>\r\n]*>)?|[.$|!><^{}\\])|%[1-9]|<(?:\/?[A-Za-z][^>\r\n]*)>|(?:if|en|cmpd|cmxs)\([^\r\n]*\)/g;
    for (const [source, target] of Object.entries(dict)) {
        assert(source in catalog);
        assert.deepStrictEqual(source.match(controls) || [], target.match(controls) || [], source);
        assert.equal(source.split('\n').length, target.split('\n').length);
    }
    assert.equal(dict['普通'], 'Normal', 'difficulty must not reuse an unrelated reputation label');
    assert.equal(dict['天音'], 'Amane');
    reports.push({version: id, validated: Object.keys(dict).length});
}
fs.writeFileSync(path.join(home, 'english-runtime-test-results.json'), JSON.stringify({status: 'PASS', hooks: 'Render, names, speaker labels, variables, widths, multiline and actor-expanded dialogue, private ticker converters, formatted battle/state/item templates, complete description fields, icon and color labels, complete notifications, title decorators, glyph safety, plugin order, seen-text log', storedDataUnchanged: true, packs: reports}, null, 2));
console.log('PASS: display hooks, stored-data immutability, native Heart streak, and all five dictionaries.');
