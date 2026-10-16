'use strict';
// Invariant: English translations NEVER create a wider closet, redraw tiles,
// or move either of the original bedroom interaction events.
const assert=require('assert'), fs=require('fs'), path=require('path'), vm=require('vm');
const home=path.dirname(__dirname);
const nativeRoot=process.env.CELESPHONIA_PROFILES_DIR ||
    path.join(process.env.USERPROFILE||'', 'Downloads','Celesphonia Mod Profiles');
const versions=['Upgrade-335-104-f0129524','Upgrade-335-1051-3b49d864','Upgrade-335-1052-054e791f'];
const nativeMaps=versions.map(v=>JSON.parse(fs.readFileSync(
    path.join(nativeRoot,v,'www/data/Map005.json'),'utf8')));
const tileAt=(m,x,y,z)=>m.data[(z*m.height+y)*m.width+x]||0;
for(const map of nativeMaps){
    assert.deepStrictEqual([map.width,map.height],[30,36]);
    for(let i=0;i<3;i++){
        assert.strictEqual(tileAt(map,4,15+i,3),[193,201,209][i],
            'Source wardrobe must be exactly one tile wide');
        assert.strictEqual(tileAt(map,5,15+i,3),0,
            'Adjacent floor must not become a second cabinet');
    }
    assert.deepStrictEqual([map.events[19].x,map.events[19].y],[4,16],
        'Original change-clothes point stays at its authored coordinate');
    if(map.events[83])assert.deepStrictEqual([map.events[83].x,map.events[83].y],[4,15],
        'Shamelessness option stays at its authored coordinate');
}
for(const version of ['three-in-one-3-3-5-v1-04',
    'three-in-one-3-3-5-v1-051','three-in-one-3-3-5-v1-052']){
    function Tilemap(){
        this._mapWidth=nativeMaps[2].width;
        this._mapHeight=nativeMaps[2].height;
        this._mapData=nativeMaps[2].data;
    }
    Tilemap.prototype._readMapData=function(x,y,z){
        return this._mapData[(z*this._mapHeight+y)*this._mapWidth+x]||0;
    };
    const original=Tilemap.prototype._readMapData;
    const context={Tilemap,console,$gameMap:{mapId(){return 5;}},
        CelesphoniaEnglishMeta:{version,mode:'display-only'},
        CelesphoniaEnglishDictionary:{},
        CelesphoniaEnglishLabels:{},
        addEventListener(){},setInterval(){}};
    vm.runInNewContext(fs.readFileSync(path.join(home,'EnglishRuntime.js'),'utf8'),context);
    assert.strictEqual(Tilemap.prototype._readMapData,original,
        'English MUST NOT hook the native tile renderer');
    const renderer=new Tilemap();
    for(let i=0;i<3;i++){
        assert.strictEqual(renderer._readMapData(4,15+i,3),[193,201,209][i]);
        assert.strictEqual(renderer._readMapData(5,15+i,3),0);
    }
    assert.strictEqual(context.CelesphoniaEnglish.mode,'display-only');
}
console.log('PASS: v1.04, Ver1.051 and Ver1.052 all retain original 1x3 wardrobe and unchanged interactions; English has no native tilemap interception');
