const fs=require('fs'),vm=require('vm'),assert=require('assert');
const dictionary={'%1的%2 上升到了%3！':'Le %2 de %1 est monté à %3 !'};
const context={console,window:null,CelesphoniaEnglishDictionary:dictionary,Bitmap:function(){},setTimeout:()=>{}};context.window=context;context.Bitmap.prototype.drawText=function(v){return v};context.Bitmap.prototype.measureTextWidth=function(v){return v.length};vm.createContext(context);vm.runInContext(fs.readFileSync(process.argv[2],'utf8'),context);
assert.strictEqual(context.CelesphoniaEnglish.translate('Amane的ATK 上升到了5！'),'Le ATK de Amane est monté à 5 !');console.log('PASS: existing shared renderer substitutes reordered format slots by ID');
