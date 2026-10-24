/* Celesphonia English overlay: render-time replacements only, ES5 for older NW.js. */
(function (global) {
    'use strict';
    if (global.CelesphoniaEnglish && global.CelesphoniaEnglish._runtimeLoaded) return;
    var dict = global.CelesphoniaEnglishDictionary || {}, labels = global.CelesphoniaEnglishLabels || {}, own = Object.prototype.hasOwnProperty;
    var humanLabels = global.CelesphoniaEnglishHumanLabels || {};
    var enemyPrefixes = global.CelesphoniaEnglishEnemyPrefixes || {}, enemyNames = global.CelesphoniaEnglishEnemyNames || {};
    var enemyPrefixKeys = Object.keys(enemyPrefixes).sort(function (a, b) { return b.length - a.length; });
    var descriptions = global.CelesphoniaEnglishDescriptions || {}, descriptionKeys = Object.keys(descriptions).sort(function (a, b) { return b.length - a.length; });
    var pending = {}, seenCount = 0, installed = false;
    var cache = Object.create(null), cacheKeys = [], cacheCursor = 0, cacheLimit = 2048;
    var templateBuckets = {}, fallbackTemplates = [];
    function normalizeCodes(text) { return text.replace(/\x1b/g, '\\').replace(/\\\\(?=[Nn]\[\d+\])/g, '\\'); }
    // Named plugin placeholders are opaque slots, never JavaScript expressions.
    var templateSlot = /%[1-9]|\\[NnVv]\[\d+\]|\$\{[A-Za-z_$][A-Za-z0-9_$]*\}|\{(?:attacker|target)\}/g;
    var templates = Object.keys(dict).filter(function (key) { return /%[1-9]|\$\{[A-Za-z_$][A-Za-z0-9_$]*\}|\{(?:attacker|target)\}/.test(key); }).map(function (key) {
        var slots = [], parts = normalizeCodes(key).split(/(%[1-9]|\\[NnVv]\[\d+\]|\$\{[A-Za-z_$][A-Za-z0-9_$]*\}|\{(?:attacker|target)\})/g), pattern = '^';
        parts.forEach(function (part) {
            if (/^%[1-9]$/.test(part)) { slots.push(part); pattern += '([^\\r\\n]+?)'; }
            else if (/^\\[NnVv]\[\d+\]$/.test(part)) { slots.push(part.toUpperCase());pattern += /^\\v/i.test(part)?'([+-]?\\d+(?:\\.\\d+)?)':'([^\\r\\n<>]*?)'; }
            else if (/^(?:\$\{[A-Za-z_$][A-Za-z0-9_$]*\}|\{(?:attacker|target)\})$/.test(part)) { slots.push(part);pattern += '([^\\r\\n]+?)'; }
            else pattern += part.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        });
        return {source:key, regex: new RegExp(pattern + '$'), slots: slots, target: normalizeCodes(dict[key]), specificity:normalizeCodes(key).replace(templateSlot,'').length, anchor: /[\u3040-\u30ff\u3400-\u9fff]{2}/.exec(key)};
    });
    templates.forEach(function (item, index) {
        item.id = index;
        var anchor = item.anchor;
        if (anchor) (templateBuckets[anchor[0]] || (templateBuckets[anchor[0]] = [])).push(item);
        else fallbackTemplates.push(item);
    });
    function candidatesFor(text, buckets, fallback) {
        var candidates = fallback ? fallback.slice() : [], checked = {}, runs = text.match(/[\u3040-\u30ff\u3400-\u9fff]+/g) || [];
        runs.forEach(function (run) { for (var i = 0; i < run.length - 1; i++) {
            (buckets[run.substring(i, i + 2)] || []).forEach(function (item) {
                if (!own.call(checked, item.id)) { checked[item.id] = true; candidates.push(item); }
            });
        }});
        return candidates;
    }
    var actorBuckets = {}, actorSerial = 0, unterminatedNameTargets = Object.create(null);
    Object.keys(dict).forEach(function (key) {
        var target = normalizeCodes(dict[key]);key = normalizeCodes(key);
        if (/^\\[Nn]<\\[Nn]\[\d+\]/.test(key) && key.split('\n')[0].indexOf('>') < 0) unterminatedNameTargets[target] = true;
        if (!/\\[Nn]\[\d+\]/.test(key) || /%[1-9]/.test(key)) return;
        var slots = [], anchor = null, pattern = '^';
        key.split(/(\\[Nn]\[\d+\])/g).forEach(function (part) {
            var actor = /^\\[Nn]\[(\d+)\]$/.exec(part);
            if (actor) { slots.push(actor[1]); pattern += '([^\r\n<>]*?)'; }
            else {
                if (!anchor) { var chinese = /[\u3040-\u30ff\u3400-\u9fff]{2}/.exec(part); if (chinese) anchor = chinese[0]; }
                pattern += part.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            }
        });
        if (!anchor) return;
        var item = {id: actorSerial++, regex: new RegExp(pattern + '$'), slots: slots, target: target, size: key.length};
        (actorBuckets[anchor] || (actorBuckets[anchor] = [])).push(item);
    });
    function expandedActors(text) {
        var candidates = candidatesFor(text, actorBuckets);
        candidates.sort(function (a, b) { return b.size - a.size; });
        for (var i = 0; i < candidates.length; i++) {
            var item = candidates[i], match = item.regex.exec(text), values = {}, consistent = true;
            if (!match) continue;
            item.slots.forEach(function (slot, index) { if (own.call(values, slot) && values[slot] !== match[index + 1]) consistent = false; values[slot] = match[index + 1]; });
            if (consistent) return item.target.replace(/\\[Nn]\[(\d+)\]/g, function (code, id) {
                if (!own.call(values, id)) return code;
                var value = values[id], actor = global.$gameActors && $gameActors._data && $gameActors._data[Number(id)];
                // Some native notifications leave a doubled name escape behind.
                // Remove its stray slash only when it precedes that exact actor.
                if (actor && typeof actor.name === 'function' && value === '\\' + actor.name()) value = actor.name();
                return exact(value);
            });
        }
        return text;
    }
    function exact(text) { return own.call(dict, text) ? dict[text] : text; }
    function reviewedLine(text) {
        if (own.call(dict,text)) return dict[text];
        var frame=/^((?:(?:\\|\x1b)>)*)([\s\S]*?)(\s*)$/.exec(text),body=frame?normalizeCodes(frame[2]):normalizeCodes(text);
        var output=structured(body);
        // This exact requirement can arrive after its bracketed label translated.
        // Reuse the reviewed native field; preserve all colors and the threshold.
        if(output===body&&/^\\C\[(?:7|16)\]\[Arousal\] \\C\[(?:8|0)\]\d+ \\C\[(?:7|16)\]\s*以上$/i.test(body)) output=structured(body.replace('[Arousal]','[発情値]'));
        if(output===body)return text;
        if(text.indexOf('\x1b')>=0)output=output.replace(/\\/g,'\x1b');
        return (frame?frame[1]:'')+output+(frame?frame[3]:'');
    }
    // Native requirement/status fields are assembled before reaching a window.
    // Recognize complete cataloged numeric/named-variable grammars; never substitute
    // prose fragments or write the value back into a game variable.
    var variableBuckets = {}, variableSerial = 0;
    Object.keys(dict).forEach(function (source) {
        var key = normalizeCodes(source), target = normalizeCodes(dict[source]);
        if (!/\\(?:[Vv]\[\d+\]|cm\[[^\]\r\n]+\])/i.test(key) || /%[1-9]/.test(key)) return;
        var slots = [], anchor = null, pattern = '^';
        key.split(/(\\[VvNn]\[\d+\]|\\cm\[[^\]\r\n]+\])/gi).forEach(function (part) {
            var slot = /^\\(?:([VvNn])\[(\d+)\]|cm\[([^\]\r\n]+)\])$/i.exec(part);
            if (slot) { slots.push(slot[3] ? 'CM:'+slot[3] : slot[1].toUpperCase() + slot[2]);pattern += slot[1] && /v/i.test(slot[1]) ? '([+-]?\\d+(?:\\.\\d+)?)' : '([^\\r\\n<>]*?)'; }
            else { if (!anchor) { var hit = /[\u3040-\u30ff\u3400-\u9fff]{2}/.exec(part);if (hit) anchor = hit[0]; }pattern += part.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); }
        });
        if (!anchor) return;
        var item = {id:variableSerial++,regex:new RegExp(pattern + '$'),slots:slots,target:target,size:key.length};
        (variableBuckets[anchor] || (variableBuckets[anchor] = [])).push(item);
    });
    function expandedVariables(text) {
        var selected = candidatesFor(text, variableBuckets).sort(function (a,b) { return b.size-a.size; });
        for (var i=0;i<selected.length;i++) {
            var item=selected[i],match=item.regex.exec(text),values={},consistent=true;
            if (!match) continue;
            item.slots.forEach(function(slot,index){if(own.call(values,slot)&&values[slot]!==match[index+1])consistent=false;values[slot]=match[index+1];});
            if(consistent)return item.target.replace(/\\(?:([VvNn])\[(\d+)\]|cm\[([^\]\r\n]+)\])/gi,function(code,kind,id,named){var slot=named?'CM:'+named:kind.toUpperCase()+id;return own.call(values,slot)?exact(values[slot]):code;});
        }
        return text;
    }
    function structured(text) {
        if (own.call(dict, text)) return dict[text];
        // An observed older overlay expanded the isolated counter suffix first.
        // Repair only that complete native streak field, from its reviewed grammar.
        var staleStreak = /^(\d+) times连续绝顶！$/.exec(text);
        if (staleStreak && own.call(dict, '\\v[616]次连续绝顶！')) return dict['\\v[616]次连续绝顶！'].replace(/\\v\[616\]/gi, staleStreak[1]);
        var variableText = expandedVariables(text);
        if (variableText !== text) return variableText;
        var caption = enemyCaption(text);
        if (caption !== text) return caption;
        // Older generic templates could split "prefix + enemy + stat" at the
        // prefix's possessive marker. Repair that observed complete log grammar
        // only when both the reconstructed enemy and stat notice are known.
        var legacy=/^(.*?)'s (.+?)的(.+?) has (increased|decreased)!$/.exec(text);
        if(legacy){
            for(var p=0;p<enemyPrefixKeys.length;p++){
                var prefix=enemyPrefixKeys[p],short=prefix.replace(/的$/,'');
                if(legacy[1]!==short&&legacy[1]!==enemyPrefixes[prefix])continue;
                var name=enemyCaption(prefix+legacy[2]),notice="%9's "+legacy[3]+' has '+legacy[4]+'!';
                if(name!==prefix+legacy[2]&&own.call(dict,notice))return dict[notice].replace(/%9/g,function(){return name;});
            }
        }
        var selected = candidatesFor(text, templateBuckets, fallbackTemplates).sort(function (a, b) { return (b.specificity||0)-(a.specificity||0)||a.id-b.id; });
        for (var i = 0; i < selected.length; i++) {
            var item = selected[i], match = item.regex.exec(text), values = {}, consistent = true;
            if (!match) continue;
            item.slots.forEach(function (slot, index) { if (own.call(values, slot) && values[slot] !== match[index + 1]) consistent = false; values[slot] = match[index + 1]; });
            if (item.source === '%9を入力してください。' || item.source === '%9の値を%1に変更しました。') {
                if (['横幅','高さ','パディング','フォントサイズ','行の高さ','背景の透明度','行数','背景画像のファイル名','フォント名'].indexOf(values['%9']) < 0) consistent = false;
            }
            if (consistent) return item.target.replace(templateSlot, function (slot) { var key=/^\\[NnVv]\[/.test(slot)?slot.toUpperCase():slot;return own.call(values,key)?translate(values[key]):slot; });
        }
        return expandedActors(text);
    }
    function decoratedLabel(text) {
        var counted = /^(.*?)(\(-?\d+\)|\([A-Za-z][A-Za-z0-9 +/.-]*\)|[ⅠⅡⅢⅣⅤⅥⅦⅧⅨⅩ]+|[IVX]{1,4})$/.exec(text);
        if (counted && own.call(dict, counted[1])) return dict[counted[1]] + counted[2];
        var quoted = /^About ["“](.+)["”]$/.exec(text);
        if (quoted && own.call(dict, quoted[1])) return 'About "' + dict[quoted[1]] + '"';
        return text;
    }
    function enemyCaption(text) {
        var rest = text, prefixes = '', count = 0;
        while (count++ < 20) {
            // A real base name can itself begin with a state-prefix word.
            // Prefer the complete known enemy before consuming another prefix.
            if (own.call(enemyNames, rest)) return prefixes + enemyNames[rest];
            // Native makeUniqueNames appends a Latin letter to duplicate enemies.
            for (var letters = 1; letters <= 2; letters++) {
                var duplicate = new RegExp('^(.*?)( ?[A-Z\uFF21-\uFF3A]{' + letters + '})$').exec(rest);
                if (duplicate && own.call(enemyNames, duplicate[1])) return prefixes + enemyNames[duplicate[1]] + duplicate[2];
            }
            var prefix = null;
            for (var i = 0; i < enemyPrefixKeys.length; i++) if (rest.indexOf(enemyPrefixKeys[i]) === 0) { prefix = enemyPrefixKeys[i]; break; }
            if (!prefix) break;
            prefixes += enemyPrefixes[prefix] + ' ';rest = rest.substring(prefix.length);
        }
        return text;
    }
    function segments(text) {
        // The weekday is a saved variable expanded by the native converter.
        // Recognize only the complete calendar display grammar; never rewrite
        // numeral characters or mutate the game's day/time variables.
        var weekdays = {'日':'Sun', '一':'Mon', '二':'Tue', '三':'Wed', '四':'Thu', '五':'Fri', '六':'Sat', '月':'Mon', '火':'Tue', '水':'Wed', '木':'Thu', '金':'Fri', '土':'Sat'};
        // A complete label between icons, numbers or punctuation can be translated.
        // Never replace a known word inside an unknown Chinese sentence, or inside
        // an escape-code payload (name boxes, variables, colors and plugin tags).
        return text.split(/((?:\\|\x1b)(?:[A-Za-z]+(?:\[[^\]\r\n]*\]|<[^>\r\n]*>)?|[.$|!><^{}\\])|<[^>\r\n]*>)/g).map(function (part, index) {
            if (index % 2) {
                var bracketed = /^<\[([^\]<>]+)\]>$/.exec(part);
                if (bracketed && own.call(dict, bracketed[1])) return '\u2039' + dict[bracketed[1]] + '\u203a';
                var human = /^<([^<>\r\n]+)>$/.exec(part);
                if (human && own.call(humanLabels, human[1])) return '\u2039' + humanLabels[human[1]] + '\u203a';
                // Some display tables wrap human labels in angle brackets.
                // Translate only an exact cataloged label beginning with CJK;
                // arbitrary tags and escape payloads remain intact.
                if (/^<[\u3040-\u30ff\u3400-\u9fff]/.test(part) && own.call(dict, part)) return dict[part];
                return part;
            }
            part = part.replace(/(^|[\s\d])星期(?:\(([日一二三四五六月火水木金土])\)|([日一二三四五六月火水木金土]))(?=$|[\s(（])/g, function (_, before, bracketed, plain) { return before + weekdays[bracketed || plain]; });
            // A reviewed calendar template translates before variable expansion.
            // Its saved weekday glyph therefore appears inside the English frame.
            // Match that whole frame only, preserving unrelated date numerals.
            part = part.replace(/(^|\s)Day (\d+) \(weekday ([日一二三四五六月火水木金土])\)(?=$|[\s(（])/g, function (_, before, day, name) { return before + 'Day ' + day + ' (' + weekdays[name] + ')'; });
            var trimmed = part.replace(/^\s+|\s+$/g, '');
            if (trimmed) {
                var translated = structured(normalizeCodes(trimmed));
                if (translated === normalizeCodes(trimmed)) translated = decoratedLabel(trimmed);
                if (translated !== trimmed) return part.substring(0, part.indexOf(trimmed)) + translated + part.substring(part.indexOf(trimmed) + trimmed.length);
            }
            // Location/rank fields use these visible brackets. Match a whole
            // cataloged field, including floor numbers and ASCII place names.
            part = part.replace(/【([^】\r\n]+)】/g, function (whole, caption) {
                return own.call(dict, caption) ? '【' + dict[caption] + '】' : whole;
            });
            // Item pop-ups append a cataloged first-line description after a name
            // and quantity. Replace that complete field only at field boundaries.
            descriptionKeys.forEach(function (source) {
                var at = part.indexOf(source);if (at < 0) return;
                var end = at + source.length;
                if ((at === 0 || /\s/.test(part.charAt(at - 1))) && (end === part.length || /[\s\[\]()]/.test(part.charAt(end)))) part = part.substring(0, at) + descriptions[source] + part.substring(end);
            });
            return part.replace(/(?:HP|MP|SP|TP)?[\u3040-\u30ff\u3400-\u9fff]+/g, function (label) {
                // These one-character source fields are weekday names. Their
                // exact fields translate, but they must not turn a date suffix
                // such as "10月" into "10Mon" through generic label matching.
                var weekday={'日':'Sun','月':'Mon','火':'Tue','木':'Thu','金':'Fri'};
                if (own.call(weekday,label) && labels[label]===weekday[label]) return label;
                return own.call(labels, label) ? labels[label] : label;
            });
        }).join('');
    }
    function translateUncached(text) {
        if (typeof text !== 'string') return text;
        if (own.call(dict, text)) return dict[text];
        if (/^<WordWrap>/i.test(text)) return text.substring(0, 10) + translate(text.substring(10));
        // Some plugins pass already-converted escape characters to the renderer.
        var raw = text.replace(/\x1b/g, '\\');
        if (own.call(dict, raw)) return dict[raw].replace(/\\/g, '\x1b');
        var cosmetics = /^((?:\\[cCiI]\[(?:\\[vV]\[\d+\]|\d+)\]\s*)+)([\s\S]*)$/.exec(raw);
        if (cosmetics) {
            var body = structured(cosmetics[2]);
            if (body !== cosmetics[2]) {
                var decorated = cosmetics[1] + body;
                return text.indexOf('\x1b') >= 0 ? decorated.replace(/\\/g, '\x1b') : decorated;
            }
        }
        var formatted = structured(raw);
        if (formatted !== raw) return text.indexOf('\x1b') >= 0 ? formatted.replace(/\\/g, '\x1b') : formatted;
        var lines = text.split('\n').map(reviewedLine).join('\n');
        if (lines !== text) return segments(lines);
        return segments(text);
    }
    function translate(text) {
        if (typeof text !== 'string') return text;
        if (own.call(dict, text)) return correctedTerms(dict[text]);
        // English text and rendered Latin glyphs need no pattern searches.
        if (!/[\u3040-\u30ff\u3400-\u9fff]/.test(text)) return correctedTerms(text);
        if (own.call(cache, text)) return cache[text];
        var result = correctedTerms(translateUncached(text));
        if (text.length <= 6000) {
            if (cacheKeys.length < cacheLimit) cacheKeys.push(text);
            else { delete cache[cacheKeys[cacheCursor]]; cacheKeys[cacheCursor] = text; cacheCursor = (cacheCursor + 1) % cacheLimit; }
            cache[text] = result;
        }
        return result;
    }
    function correctedTerms(text) {
        // Skill 955 tags 110/95 map to anal cap and anal EXP variables.
        // Correct the two reviewed English labels, not arbitrary anatomy text.
        return text.replace(/Extreme Sensitivity \(Intestines\)/g,'Extreme Sensitivity (Anal)')
            .replace(/\[Intestinal development EXP modifier\]/g,'[Anal development EXP modifier]');
    }
    function observe(text) {
        if(global.CelesphoniaEnglish&&global.CelesphoniaEnglish.captureSeen===false)return text;
        if (typeof text === 'string' && text.length > 1 && /[\u3040-\u30ff\u3400-\u9fff]/.test(text.replace(/(?:\\|\x1b)[A-Za-z]+(?:\[[^\]]*\]|<[^>]*>)?/g, '')) && text.length < 6000 && !own.call(pending, text) && seenCount < 10000) {
            pending[text] = text; seenCount++;
        }
        return text;
    }
    function wrap(proto, method, glyphSafe) {
        if (!proto || typeof proto[method] !== 'function') return;
        var previous = proto[method];
        proto[method] = function (text) {
            // Ticker's private window overrides escape conversion without calling
            // Window_Base. Hook its presentation prototype when it first renders.
            if (method === 'drawTextEx' && typeof this.convertEscapeCharacters === 'function' && !this.convertEscapeCharacters._celesphoniaEnglishConvert) wrapConverter(Object.getPrototypeOf(this));
            var args = Array.prototype.slice.call(arguments);
            // Message renderers draw individual glyphs through Bitmap. Translating a
            // single Chinese character there would corrupt unmatched Chinese sentences.
            var glyph = typeof text === 'string' && text.length === 1 && /[\u3040-\u30ff\u3400-\u9fff]/.test(text);
            args[0] = glyphSafe && glyph ? text : observe(translate(text));
            var name = this.constructor && this.constructor.name, fit = null;
            if(method==='drawText'&&name==='Window_Info'&&args[2]===-5){
                if(args[1]===36)args[3]=106;
                if(args[1]===148)args[3]=54;
                if(args[1]===208)args[3]=Math.max(1,this.contentsWidth()-208);
            }
            if(method==='drawText'&&name==='Window_Info'&&args[2]===1){
                if(args[1]===36)args[3]=106;
                if(args[1]===170){args[1]=148;args[3]=Math.max(1,this.contentsWidth()-148);}
            }
            if(method==='drawTextEx'&&name==='Window_Custom'&&args[1]<this.contentsWidth()) fit=fitSingleLine(this,args[0],this.contentsWidth()-args[1]);
            if(method==='drawTextEx'&&name==='Window_SoloStatus'){
                installSoloFieldWidth(Object.getPrototypeOf(this));
                if(args[1]<this.contentsWidth())fit=fitSingleLine(this,args[0],Math.min(this._celesphoniaEnglishFieldWidth||this.contentsWidth()-args[1],this.contentsWidth()-args[1]));
            }
            // EnemyBook places two note descriptions at fixed row positions.
            // Lay out their translated prose together in the remaining panel.
            // The native enemy, stats and note strings are never rewritten.
            if (method === 'drawTextEx' && name === 'Window_EnemyBookStatus' && this._enemy && this._enemy.meta) {
                var first = this._enemy.meta.desc1, second = this._enemy.meta.desc2;
                var a = typeof first === 'string' ? translate(first) : '', b = typeof second === 'string' ? translate(second) : '';
                if (a !== first || b !== second) {
                    if (text === second && (text !== first || this._celesphoniaEnglishDescriptionY !== undefined && args[2] > this._celesphoniaEnglishDescriptionY)) return 0;
                    if (text === first) { this._celesphoniaEnglishDescriptionY = args[2];args[0] = a + (b ? '\n' + b : ''); }
                    fit = fitPanelText(this, args[0], this.contentsWidth() - args[1], this.contentsHeight() - args[2]);
                }
            }
            if (method === 'drawTextEx' && name === 'Window_Glossary') {
                var glossaryLayout=this._celesphoniaEnglishGlossaryLayout;
                if(glossaryLayout && glossaryLayout.item===this._itemData &&
                   glossaryLayout.page===this._pageIndex &&
                   (text===glossaryLayout.source || args[0]===glossaryLayout.display)) fit=glossaryLayout.fit;
                else if(args[0]!==text) fit=fitPanelText(this,args[0],this.contentsWidth()-args[1],this.contentsHeight()-args[2]);
            }
            if (method === 'drawTextEx' && this._celesphoniaEnglishEquipmentPainting) fit = this._celesphoniaEnglishEquipmentLayout;
            else if (method === 'drawTextEx' && name === 'Window_Help' && args[0] !== text) fit = fitPanelText(this, args[0], this.contentsWidth() - args[1], this.contentsHeight() - args[2]);
            if (method === 'drawTextEx' && global.SceneManager && SceneManager._scene && SceneManager._scene._hzChoiceHelpWindow === this && args[0] !== text) fit = fitPanelText(this, args[0], this.contentsWidth() - args[1], this.contentsHeight() - args[2]);
            var reset = this.resetFontSettings, hadReset = own.call(this, 'resetFontSettings'), font = this.contents && this.contents.fontSize;
            var calc = this.calcTextHeight, hadCalc = own.call(this, 'calcTextHeight');
            var fitCaption = method === 'drawText' && (name === 'Window_Info'||name === 'Window_SoloStatus'||name === 'Window_Help' && args[0] !== text) &&
                this.contents && typeof args[3] === 'number' && args[3] > 0;
            if (fitCaption) while (this.contents.fontSize > 12 && this.textWidth(args[0]) > args[3]) this.contents.fontSize--;
            if (fit) {
                args[0] = fit.text;this._celesphoniaEnglishPanelLayout = fit;
                this.resetFontSettings = function () { reset.call(this);this.contents.fontSize = fit.fontSize; };
                this.calcTextHeight = function (state, all) {
                    var value = calc.call(this, state, all), line = state.text.substring(state.index).split('\n')[0];
                    return /\x1bi\[\d+\]/i.test(line) ? Math.max(32, value) : value;
                };
            }
            var hadFlag = own.call(this, '_celesphoniaEnglishRendering'), prior = this._celesphoniaEnglishRendering;
            if (args[0] !== text) this._celesphoniaEnglishRendering = true;
            try { return previous.apply(this, args); }
            finally {
                if (hadFlag) this._celesphoniaEnglishRendering = prior; else delete this._celesphoniaEnglishRendering;
                if (fit) { if (hadReset) this.resetFontSettings = reset;else delete this.resetFontSettings;if (hadCalc) this.calcTextHeight = calc;else delete this.calcTextHeight;if (this.contents) this.contents.fontSize = font; }
                if (fitCaption) this.contents.fontSize = font;
            }
        };
    }
    function resistancePicture(sprite,info){
        // CE806 owns two five-row D_TEXT pictures, spaced exactly 143px apart.
        // Render their local bitmap as cells; never rewrite picture/variable text.
        if(!info||info.size!==18||!global.SceneManager||typeof SceneManager.getHiddenWindow!=='function')return false;
        var labels=sprite._pictureId===86?['瘴气耐性','拘束耐性','羞耻耐性','催眠耐性','(?:淫咒|诅咒)耐性']:
            sprite._pictureId===87?['快感耐性','发情耐性','拘束回避','陷阱回避','服装耐久']:null;
        if(!labels||typeof info.originalValue!=='string'||typeof info.value!=='string')return false;
        var source=info.originalValue.replace(/\x1b/g,'\\').trim().split(/\r?\n/),lines=info.value.trim().split(/\r?\n/);
        if(source.length!==5||lines.length!==5)return false;
        var names=sprite._pictureId===86?['Miasma Res','Bind Res','Shame Res','Hypno Res','Curse Res']:
            ['Pleasure Res','Arousal Res','Bind Evade','Trap Evade','Clothing HP'];
        var rows=[];
        for(var i=0;i<5;i++){
            if(!new RegExp('^\\\\c\\[16\\]'+labels[i]+'(?:\\s|$)','i').test(source[i]))return false;
            var chars=[],color=0,match,re=/\x1bC\[(\d+)\]|([^\x1b]+)/gi;
            while((match=re.exec(lines[i]))) {
                if(match[1]!==undefined)color=Number(match[1]);
                else for(var j=0;j<match[2].length;j++)chars.push({text:match[2][j],color:color});
            }
            var plain=chars.map(function(c){return c.text;}).join(''),tail=/(-?\d+(?:\.\d+)?\s*%|\d+\s*\/\s*\d+|-\s*\/\s*-)\s*$/.exec(plain);
            if(!tail)return false;
            rows.push({label:names[i],value:tail[1],chars:chars.slice(tail.index,tail.index+tail[1].length)});
        }
        var hidden=SceneManager.getHiddenWindow(),nativeBitmap=sprite.bitmap;
        if(!nativeBitmap)return false;
        var bitmap=new Bitmap(143,nativeBitmap.height),rowHeight=nativeBitmap.height/5;
        bitmap.fontFace=nativeBitmap.fontFace;sprite.bitmap=bitmap;sprite.applyTextDecoration();
        rows.forEach(function(row,index){
            var size=16,valueWidth,labelWidth;
            do {
                bitmap.fontSize=size;
                valueWidth=bitmap.measureTextWidth(row.value);labelWidth=bitmap.measureTextWidth(row.label);
                if(labelWidth+valueWidth+10<=135||size===12)break;
                size--;
            }while(size>=12);
            valueWidth=Math.min(valueWidth,88);var valueLeft=139-valueWidth,labelSpace=valueLeft-8;
            bitmap.textColor=hidden.textColor(16);
            bitmap.drawText(row.label,2,index*rowHeight,labelSpace,rowHeight,'left');
            // Preserve the native per-character colors, including red values.
            var measured=bitmap.measureTextWidth(row.value),scale=measured?valueWidth/measured:1,x=valueLeft;
            row.chars.forEach(function(c){var width=bitmap.measureTextWidth(c.text)*scale;bitmap.textColor=hidden.textColor(c.color);bitmap.drawText(c.text,x,index*rowHeight,width,rowHeight,'left');x+=width;});
            row.fontSize=size;row.labelRight=2+Math.min(labelWidth,labelSpace);row.valueLeft=valueLeft;row.valueRight=139;
            delete row.chars;
        });
        sprite._celesphoniaEnglishResistanceLayout={width:143,rows:rows};
        return true;
    }
    function installTitleDescription(){
        var proto=global.Game_Interpreter&&Game_Interpreter.prototype,native=proto&&proto.command355;
        if(typeof native!=='function'||native._celesphoniaTitleDescription)return;
        var wrapped=function(){
            var list=global.$dataCommonEvents&&$dataCommonEvents[823]&&$dataCommonEvents[823].list;
            var first=this.currentCommand(),second=this._list&&this._list[this._index+1],third=this._list&&this._list[this._index+2];
            if(this._list!==list||!first||!second||second.code!==655||third&&third.code===655||
               first.parameters[0]!=='var SkillID = $gameVariables.value($gameVariables.value(4584))'||
               second.parameters[0]!=='$gameVariables._data[4587] = $dataSkills[SkillID].description')return native.apply(this,arguments);
            var variables=global.$gameVariables,read=variables.value,slot=Number(read.call(variables,4584)),count=Number(read.call(variables,4698));
            if(!isFinite(count)||count<1||count>110||slot>=4601&&slot<=4600+count)return native.apply(this,arguments);
            // CE823 lets the last column's cursor reach unpopulated rows.
            // Those addresses belong to unrelated profile fields. Supply the
            // native blank-description sentinel only during this display read.
            var wasOwn=own.call(variables,'value');variables.value=function(id){return Number(id)===slot?999:read.apply(this,arguments);};
            try{return native.apply(this,arguments);}finally{if(wasOwn)variables.value=read;else delete variables.value;}
        };
        wrapped._celesphoniaTitleDescription=true;proto.command355=wrapped;
    }
    function fitTitleColumn(sprite,info,render,args,nativeResult){
        var gap=global.$gameVariables&&Number($gameVariables.value(939)),limit=gap>64&&gap<512?gap-8:214;
        var pitch=info.size+8,lines=info.value.split('\n'),widths=sprite.textWidths||[],fonts=[],icons=[];
        lines.forEach(function(line,i){
            var origin=(line.match(/\x1bi\[\d+\]/gi)||[]).length*(Window_Base._iconWidth+4);icons[i]=origin;
            fonts[i]=Math.max(12,Math.min(info.size,Math.floor(info.size*(limit-origin)/Math.max(1,(widths[i]||0)-origin))));
        });
        var layout={width:sprite.bitmap.width,height:sprite.bitmap.height,rowPitch:pitch,fontSizes:fonts,limit:limit};
        sprite._celesphoniaEnglishTitleLayout=layout;
        if(sprite.bitmap.width<=limit)return nativeResult;
        var hidden=SceneManager.getHiddenWindow(),calc=hidden.calcTextHeight,hadCalc=own.call(hidden,'calcTextHeight'),font=hidden.contents.fontSize;
        var newline=sprite._processNewLine,hadLine=own.call(sprite,'_processNewLine'),character=sprite._processCharacter,hadChar=own.call(sprite,'_processCharacter');
        try{
            // CE814's cursor uses a fixed native row pitch. Fitting a long
            // label must never add lines or move subsequent entries.
            hidden.calcTextHeight=function(){return pitch;};
            sprite._processNewLine=function(state,bitmap){var result=newline.apply(this,arguments);this.hiddenWindow.contents.fontSize=fonts[state.line]||info.size;state.height=pitch;return result;};
            sprite._processCharacter=function(state,bitmap){
                var origin=icons[state.line]||0,w=(this.textWidths&&this.textWidths[state.line])||0;
                var scale=Math.min(1,(limit-origin)/Math.max(1,w-origin)),ctx=bitmap instanceof Bitmap&&bitmap._context;
                var glyph=state.text[state.index];
                if(!ctx||scale>=1||state.x<origin||glyph==='\x1b'||glyph==='\n')return character.apply(this,arguments);
                // Keep icons and vertical coordinates native. Compress only
                // an overlong text row after reducing its own font to 12px.
                ctx.save();ctx.translate(origin,0);ctx.scale(scale,1);ctx.translate(-origin,0);
                try{return character.apply(this,arguments);}finally{ctx.restore();}
            };
            var result=render.apply(sprite,args),original=sprite.bitmap;
            if(original.width>limit){var bounded=new Bitmap(limit,original.height);bounded.blt(original,0,0,limit,original.height,0,0);sprite.bitmap=bounded;}
            layout.width=sprite.bitmap.width;layout.height=sprite.bitmap.height;
            return result;
        }finally{
            if(hadCalc)hidden.calcTextHeight=calc;else delete hidden.calcTextHeight;
            hidden.contents.fontSize=font;
            if(hadLine)sprite._processNewLine=newline;else delete sprite._processNewLine;
            if(hadChar)sprite._processCharacter=character;else delete sprite._processCharacter;
        }
    }
    function fitSingleLine(win,text,width){
        var prior=win.contents.fontSize,chosen,size=win.standardFontSize(),rendered=win.convertEscapeCharacters(text);
        var icons=(rendered.match(/\x1bi\[\d+\]/gi)||[]).length,plain=rendered.replace(/\x1b[A-Za-z]+(?:\[[^\]]*\]|<[^>]*>)?/g,'').replace(/\x1b[.$|!><^{}\\]/g,'');
        for(;size>=12;size--){win.contents.fontSize=size;var measured=win.textWidth(plain)+icons*(Window_Base._iconWidth+4);chosen={text:text,fontSize:size,width:measured,availableWidth:width,availableHeight:win.lineHeight(),singleLine:true};if(measured<=width-4)break;}
        win.contents.fontSize=prior;return chosen;
    }
    function installSoloFieldWidth(proto){
        var native=proto&&proto.drawSoloParameter;
        if(typeof native!=='function'||native._celesphoniaEnglishFieldWidth)return;
        var wrapped=function(actor,code,parameter){
            var prior=this._celesphoniaEnglishFieldWidth,had=own.call(this,'_celesphoniaEnglishFieldWidth');this._celesphoniaEnglishFieldWidth=Number(parameter.width);
            try{return native.apply(this,arguments);}finally{if(had)this._celesphoniaEnglishFieldWidth=prior;else delete this._celesphoniaEnglishFieldWidth;}
        };
        wrapped._celesphoniaEnglishFieldWidth=true;proto.drawSoloParameter=wrapped;
    }
    function panelLineWidth(win, text) {
        // MV draws \I icons as fixed-size blits, not font glyphs.  Count them
        // before choosing a wrap point, as well as in the final width check.
        var rendered = win.convertEscapeCharacters(text);
        var icons = rendered.match(/\x1bi\[\d+\]/gi) || [];
        var plain = rendered.replace(/\x1b[A-Za-z]+(?:\[[^\]]*\]|<[^>]*>)?/g, '').replace(/\x1b[.$|!><^{}\\]/g, '');
        return win.textWidth(plain) + icons.length * (Window_Base._iconWidth + 4);
    }
    function fitPanelText(win, text, width, height, minimum, initial) {
        width = Math.max(1, width - 8);height = Math.max(1, height);
        var original = win.contents.fontSize, start = initial || win.standardFontSize(), chosen;
        for (var size = start; size >= (minimum || 12); size -= 2) {
            win.contents.fontSize = size;var lines = [], widest = 0;
            text.split('\n').forEach(function (paragraph) {
                var line = '', joiner = '';
                // ItemDescription assembles adjacent [stat][trait] fields
                // without spaces. A soft boundary between those fields gives
                // English help a legal wrap without changing their wording.
                paragraph.replace(/\]\[/g, ']\u0001[').split(/(\s+|\u0001)/).forEach(function (part) {
                    if (!part) return;
                    if (part === '\u0001') { joiner = '';return; }
                    if (/^\s+$/.test(part)) { joiner = ' ';return; }
                    var next = line ? line + joiner + part : part;
                    if (line && panelLineWidth(win, next) > width) { lines.push(line);line = part; }
                    else line = next;
                    joiner = '';
                });
                lines.push(line);
            });
            var totalHeight = 0;
            lines.forEach(function (line) { var rendered = win.convertEscapeCharacters(line), icons = rendered.match(/\x1bi\[\d+\]/gi) || [];widest = Math.max(widest, panelLineWidth(win, line));totalHeight += Math.max(size + 8, icons.length ? Window_Base._iconHeight : 0); });
            chosen = {text:lines.join('\n'),fontSize:size,lines:lines.length,width:widest,height:totalHeight,availableWidth:width,availableHeight:height,fits:widest <= width && totalHeight <= height};
            if (chosen.fits) break;
        }
        win.contents.fontSize = original;return chosen;
    }
    function equipmentParts(text, change) {
        // Escape arguments and plugin tags are opaque, including their labels.
        return text.split(/((?:\\|\x1b)(?:[A-Za-z]+(?:\[[^\]\r\n]*\]|<[^>\r\n]*>)?|[.$|!><^{}\\])|<[^>\r\n]*>)/g).map(function(part,index){return index%2?part:change(part);}).join('');
    }
    function equipmentDisplay(text,item) {
        // The native formatter prefixes the complete database description, then
        // appends stats. Translate that original field before its assembled tail.
        var description=item.description||'',display;
        if(description&&text.indexOf(description)===0)display=translate(description)+translate(text.substring(description.length));
        else display=translate(text);
        return equipmentParts(display,function(part){
            return part.replace(/\[([^\[\]\r\n]+)\]/g,function(whole,field){return '['+translate(field)+']';});
        });
    }
    function compactEquipment(text) {
        return equipmentParts(text,function(part){
            // These are parameter/trait phrases in equipment help, never a
            // global prose replacement or a change to a native stat name.
            return part.replace(/\bPhysical Attack\b/gi,'ATK').replace(/\bPhysical Defense\b/gi,'DEF')
                .replace(/\b(?:Magic|Magical) Attack\b/gi,'MAT').replace(/\b(?:Magic|Magical) Defense\b/gi,'MD').replace(/\bMDF\b/g,'MD')
                .replace(/\b(?:Physical|Phys\.) Evasion\b/gi,'PHYS-EVA').replace(/\b(?:Critical|Crit\.?) Evasion\b/gi,'CRIT-EVA')
                .replace(/\b(?:Magic|Magical) Evasion\b/gi,'MAG-EVA').replace(/\b(?:Magic|Magical) Reflection\b/gi,'MAG-REF')
                .replace(/\b(?:Physical|Phys\.) Hit\b/gi,'PHYS-HIT').replace(/\bCritical (?:Rate|Chance)\b/gi,'CRIT')
                .replace(/\b(HP|MP|SP|TP) Regeneration\b/gi,'$1 Regen')
                .replace(/\[(Attack|Defense|Agility)(?=[+\-\s\d\]])/g,function(whole,label){return '['+({Attack:'ATK',Defense:'DEF',Agility:'AGI'})[label];})
                .replace(/\b(ATK|DEF|MAT|MD|AGI) (?:Reduction|Down) \((Minor|Small|Major|Large)\)/g,function(whole,stat,size){return stat+' Down ('+(/Minor|Small/.test(size)?'S':'L')+')';})
                .replace(/\[Physical\]/g,'[PHYS]').replace(/\bResistance(?=\[)/g,'Res.').replace(/\bWeakness(?=\[)/g,'Weak.')
                .replace(/\bElement(?=\[)/g,'Elem.');
        });
    }
    function equipmentHover(win) {
        return win&&win.visible&&win.openness===255&&win._celesphoniaEnglishEquipmentLayout&&win._celesphoniaEnglishEquipmentLayout.scrollable&&
            TouchInput.x>=win.x&&TouchInput.x<win.x+win.width&&TouchInput.y>=win.y&&TouchInput.y<win.y+win.height;
    }
    function installEquipmentHelp() {
        if(!global.Window_Help||Window_Help.prototype.refresh._celesphoniaEnglishEquipment)return;
        var proto=Window_Help.prototype,setItem=proto.setItem,refresh=proto.refresh,update=proto.update;
        proto.setItem=function(item){
            this._celesphoniaEnglishEquipmentItem=item&&global.DataManager&&(DataManager.isWeapon(item)||DataManager.isArmor(item))?item:null;
            return setItem.apply(this,arguments);
        };
        proto.refresh=function(){
            var item=this._celesphoniaEnglishEquipmentItem,source=this._text;
            if(!item||typeof source!=='string'||typeof this.descriotion!=='function'||source!==this.descriotion(item)){
                this._celesphoniaEnglishEquipmentLayout=null;this.origin.y=0;this.upArrowVisible=false;this.downArrowVisible=false;
                if(this.contents&&this.contents.height!==this.contentsHeight())this.createContents();
                return refresh.apply(this,arguments);
            }
            var display=equipmentDisplay(source,item),width=this.contentsWidth()-this.textPadding(),height=this.contentsHeight();
            var base=Window_Base.prototype.standardFontSize.call(this),layout=fitPanelText(this,display,width,height,base,base),compact=display;
            if(!layout.fits){
                compact=compactEquipment(display);
                layout=fitPanelText(this,compact,width,height,20,base);
                if(!layout.fits){
                    // Keep dense descriptions readable and all effects reachable
                    // in the existing panel instead of shrinking to tiny text.
                    layout=fitPanelText(this,compact,width,Infinity,20,20);
                    layout.scrollable=layout.height>height;layout.viewportHeight=height;
                }
            }
            layout.abbreviated=compact!==display;layout.originalDisplay=display;layout.source=source;
            this._celesphoniaEnglishEquipmentLayout=layout;this.origin.y=0;
            var bitmapHeight=Math.max(height,Math.ceil(layout.height));
            if(this.contents.width!==this.contentsWidth()||this.contents.height!==bitmapHeight)this.contents=new Bitmap(this.contentsWidth(),bitmapHeight);
            this.contents.clear();this._celesphoniaEnglishEquipmentPainting=true;
            try{this.drawTextEx(layout.text,this.textPadding(),0);}
            finally{this._celesphoniaEnglishEquipmentPainting=false;}
            this.upArrowVisible=false;this.downArrowVisible=!!layout.scrollable;
        };
        proto.refresh._celesphoniaEnglishEquipment=true;
        proto.update=function(){
            var result=update.apply(this,arguments),layout=this._celesphoniaEnglishEquipmentLayout;
            if(layout&&layout.scrollable){
                var limit=Math.max(0,this.contents.height-this.contentsHeight());
                if(equipmentHover(this)&&Math.abs(TouchInput.wheelY)>=20)this.origin.y=Math.max(0,Math.min(limit,this.origin.y+(TouchInput.wheelY>0?1:-1)*(layout.fontSize+8)*2));
                this.upArrowVisible=this.origin.y>0;this.downArrowVisible=this.origin.y<limit;
            }
            return result;
        };
        if(global.Window_Selectable&&typeof Window_Selectable.prototype.processWheel==='function'){
            var wheel=Window_Selectable.prototype.processWheel;
            Window_Selectable.prototype.processWheel=function(){
                var scene=global.SceneManager&&SceneManager._scene;
                if(scene&&equipmentHover(scene._helpWindow))return;
                return wheel.apply(this,arguments);
            };
        }
    }
    var liveCommentRendering = false;
    function installLiveComments() {
        if (!global.LCM || typeof LCM.show !== 'function' || LCM.show._celesphoniaEnglishLiveComments) return;
        var native = LCM.show;
        var wrapped = function (text) {
            var args = Array.prototype.slice.call(arguments), display = typeof text === 'string' ? translate(text) : text;
            var container = global.SceneManager && SceneManager._scene && SceneManager._scene._liveCommentContainer;
            var priorRendering = liveCommentRendering;
            if (display !== text && container && typeof SceneManager.getHiddenWindow === 'function') {
                var params = PluginManager.parameters('LiveComment'), font = Number(params['フォントサイズ']), lineHeight = Number(params['1行の高さ']);
                var padding = Number(params['テキストパディング']) || 0, win = SceneManager.getHiddenWindow(), standard = win.standardFontSize, oldFont = win.contents.fontSize;
                try {
                    win.standardFontSize = function () { return font; };
                    var layout = fitPanelText(win, display, container._width - 2 * padding, Infinity);
                    layout.height = layout.lines * lineHeight;layout.availableHeight = layout.height;
                    container._celesphoniaEnglishLiveCommentLayout = layout;display = layout.text;
                } finally { win.standardFontSize = standard;win.contents.fontSize = oldFont; }
            }
            args[0] = display;liveCommentRendering = display !== text;
            try { return native.apply(this, args); }
            finally { liveCommentRendering = priorRendering; }
        };
        wrapped._celesphoniaEnglishLiveComments = true;LCM.show = wrapped;
        // This native window allocates lineHeight pixels per line, whereas the
        // base renderer uses fontSize+8. Use its own geometry for local wrapping.
        var height = Window_Base.prototype.calcTextHeight;
        Window_Base.prototype.calcTextHeight = function () {
            if (liveCommentRendering && this.constructor && this.constructor.name === 'Window_LiveComment') return this.lineHeight();
            return height.apply(this, arguments);
        };
    }
    function installGroperTicker() {
        // Map002 E474 and its siblings use TMng Talk -> variable 2944, then
        // CE97 selects that variable through 2995, and CE881 draws a Ticker.
        // Ticker.js bypasses Window_Base.convertEscapeCharacters entirely.
        // Other TMng modes use the same nested message with different frames.
        // Resolve its display value while native synchronous drawing runs;
        // preserve the native argument, log text and stored variables.
        if (!global.TickerManager || typeof TickerManager.show !== 'function' ||
            TickerManager.show._celesphoniaEnglishGroperTicker) return;
        var native=TickerManager.show;
        var modes={2944:1,2947:1,2950:1,2953:1,2957:1,2961:1,2964:1,2966:1,2968:1,2970:1,2972:1,2974:1,2976:1,2980:1,2981:1,2983:1,2984:1,2986:1,2988:1,2989:1,2990:1,2991:1,2993:1,2994:1};
        var wrapped=function(text){
            var variables=global.$gameVariables;
            if (typeof text!=='string'||!/(?:\\|\x1b)v\[(?:\\|\x1b)v\[2995\]\]/i.test(text)||!variables||typeof variables.value!=='function') return native.apply(this,arguments);
            var messageId=Number(variables.value(2995));
            if(!own.call(modes,messageId))return native.apply(this,arguments);
            var raw=variables.value(messageId);
            if (typeof raw!=='string') return native.apply(this,arguments);
            var source=raw.replace(/\x1b/g,'\\');
            if (!own.call(dict,source)&&! /\\+n\[\d+\]/i.test(source)) return native.apply(this,arguments);
            var target=translate(source);
            var rendered=normalizeCodes(target).replace(/\\/g,'\x1b');
            if(messageId!==2974&&global.$gameActors){
                // Every native TMng mode inserts after Ticker's slash pass.
                // Consume the complete numeric name escape, including doubled
                // escapes, before its translated Latin name can become a code.
                rendered=rendered.replace(/\x1b+N\[(\d+)\]/gi,function(code,id){
                    if(!global.$dataActors||!$dataActors[Number(id)])return code;
                    var actor=$gameActors.actor(Number(id));return actor&&typeof actor.name==='function'?translate(actor.name()):code;
                });
            }
            if(messageId===2974){
                // CE32 stores two escapes before actor 360. Ticker's nested
                // variable expansion leaves one escape before its name; an
                // English name then becomes a control code (e.g. AFTERNOON).
                // Resolve the reviewed time field in this synchronous display
                // argument, before insertion, keeping actor/save values native.
                if(source!=='到了\\\\N[360]。'||!global.$gameActors)return native.apply(this,arguments);
                var actor=$gameActors.actor(360),name=actor&&actor.name();
                if(typeof name!=='string'||!name)return native.apply(this,arguments);
                rendered=normalizeCodes(target).replace(/\\N\[360\]/gi,function(){return translate(name);});
            }
            var originalReader=variables.value, wasOwn=own.call(variables,'value');
            variables.value=function(id){
                return Number(id)===messageId ? rendered : originalReader.apply(this,arguments);
            };
            try{return native.apply(this,arguments);}
            finally{
                if(wasOwn)variables.value=originalReader;
                else delete variables.value;
            }
        };
        wrapped._celesphoniaEnglishGroperTicker=true;
        TickerManager.show=wrapped;
    }
    function fitFixedLines(win, text, width, height, start) {
        var prior = win.contents.fontSize, chosen;
        for (var size = start; size >= 10; size--) {
            win.contents.fontSize = size;var widest = 0, total = 0;
            text.split('\n').forEach(function (line) {
                var rendered = win.convertEscapeCharacters(line), icons = rendered.match(/\x1bi\[\d+\]/gi) || [];
                var plain = rendered.replace(/\x1b[A-Za-z]+(?:\[[^\]]*\]|<[^>]*>)?/g, '').replace(/\x1b[.$|!><^{}\\]/g, '');
                widest = Math.max(widest, win.textWidth(plain) + icons.length * 36);
                total += Math.max(size + 8, icons.length ? 32 : 0);
            });
            chosen = {text:text,fontSize:size,width:widest,height:total,availableWidth:width-8,availableHeight:height};
            if (widest <= width-8 && total <= height) break;
        }
        win.contents.fontSize = prior;return chosen;
    }
    function installSaveTitles() {
        function displayInfo(win, info, width) {
            var caption = translate(info.title), version = global.CelesphoniaEnglishMeta && CelesphoniaEnglishMeta.version;
            var versions = {'gorpg-1-23':'Gorpg 1.23','maple-1-7-2':'Maple 1.7.2','three-in-one-3-3-4':'3-in-1 3.3.4','three-in-one-3-3-5-trial':'3-in-1 3.3.5 Trial','three-in-one-3-3-5-v1-01':'3-in-1 3.3.5 v1.01','three-in-one-3-3-5-v1-02':'3-in-1 3.3.5 v1.02'};
            // Older translated save headers can contain concatenated debug labels.
            // Identify the game's title, then render a stable version caption.
            // Neither global save metadata nor DataManager.makeSavefileInfo changes.
            if (versions[version] && /^(?:Magical\s*Girl\s*Celesphonia|魔法少女(?:苍蓝星|蒼藍星|セレスフォニア)|Celesphonia)/i.test(info.title)) caption = 'Magical Girl Celesphonia | ' + versions[version];
            var copy = {};Object.keys(info).forEach(function (key) { copy[key] = info[key]; });
            copy.title = caption;
                while (win.contents.fontSize > 18 && win.textWidth(copy.title) > width) win.contents.fontSize--;
                if (win.textWidth(copy.title) > width) {
                    while (copy.title.length && win.textWidth(copy.title + '…') > width) copy.title = copy.title.slice(0,-1);
                    copy.title += '…';
                }
                win._celesphoniaEnglishSaveTitle = {caption:copy.title,width:win.textWidth(copy.title),availableWidth:width,fontSize:win.contents.fontSize};
            return copy;
        }
        if (global.Window_SavefileList && typeof Window_SavefileList.prototype.drawGameTitle === 'function') {
            var draw = Window_SavefileList.prototype.drawGameTitle;
            Window_SavefileList.prototype.drawGameTitle = function (info, x, y, width) {
                if (!info || typeof info.title !== 'string') return draw.apply(this, arguments);
                var font = this.contents.fontSize;
                try { return draw.call(this, displayInfo(this, info, width), x, y, width); }
                finally { this.contents.fontSize = font; }
            };
        }
        // Maple's YEP_SaveCore renders the title in a separate information panel.
        // Substitute a local header only for that call; leave save notes, stored
        // metadata, the native return value and subsequent rendering untouched.
        if (global.Window_SaveInfo && typeof Window_SaveInfo.prototype.drawGameTitle === 'function') {
            var panelDraw = Window_SaveInfo.prototype.drawGameTitle;
            Window_SaveInfo.prototype.drawGameTitle = function () {
                var info = this._info, font = this.contents.fontSize;
                if (!info || typeof info.title !== 'string') return panelDraw.apply(this, arguments);
                try {
                    this._info = displayInfo(this, info, this.contents.width);
                    return panelDraw.apply(this, arguments);
                } finally { this._info = info;this.contents.fontSize = font; }
            };
        }
    }
    function installDiagnosticPresentation() {
        // Maple's choice parser reads RegExp.$1 after console.log. Preserve the
        // legacy match state before calling native diagnostics or modal APIs.
        function matchState() {
            var state = {input:RegExp.input,match:RegExp.lastMatch,left:RegExp.leftContext,slots:[]};
            for (var i=1;i<=9;i++) state.slots.push(RegExp['$'+i]);return state;
        }
        function restoreMatch(state) {
            function literal(text) {
                var result='';for(var i=0;i<text.length;i++){var ch=text.charAt(i);result+=('.*+?^${}()|[]\\'.indexOf(ch)>=0?'\\':'')+ch;}return result;
            }
            var pattern='';state.slots.forEach(function(value){pattern+='(?=[\\s\\S]*?('+literal(value)+'))';});
            var restore=new RegExp(pattern+literal(state.match),'g');restore.lastIndex=state.left.length;restore.exec(state.input);
        }
        // Modal/diagnostic text must match a reviewed complete field or grammar.
        // Preserve user input, defaults, objects, arguments and native return values.
        function displayed(value) {
            if (typeof value !== 'string') return value;
            var direct = structured(normalizeCodes(value));if (direct !== value) return direct;
            var trimmed = value.replace(/^\s+|\s+$/g,''), body = structured(normalizeCodes(trimmed));
            if (body !== trimmed) return value.substring(0,value.indexOf(trimmed)) + body + value.substring(value.indexOf(trimmed)+trimmed.length);
            var operation = /^(\d+ : )([\s\S]+)$/.exec(value);
            if (operation) { body = structured(normalizeCodes(operation[2]));if (body !== operation[2]) return operation[1] + body; }
            return value;
        }
        ['prompt','alert'].forEach(function (name) {
            var native = global[name];
            if (typeof native !== 'function' || native._celesphoniaEnglishDiagnostic) return;
            var wrapped = function () {
                var state=matchState(),args = Array.prototype.slice.call(arguments);
                try { if (args.length) args[0] = displayed(args[0]); } finally { restoreMatch(state); }
                return native.apply(this,args);
            };
            wrapped._celesphoniaEnglishDiagnostic = true;global[name] = wrapped;
        });
        var bootTitle = global.Scene_Boot && Scene_Boot.prototype.updateDocumentTitle;
        if (typeof bootTitle === 'function' && !bootTitle._celesphoniaEnglishTitle) {
            var translatedBootTitle = function () {
                var result = bootTitle.apply(this, arguments), state = matchState();
                try { if (global.document) document.title = displayed(document.title); }
                finally { restoreMatch(state); }
                return result;
            };
            translatedBootTitle._celesphoniaEnglishTitle = true;
            Scene_Boot.prototype.updateDocumentTitle = translatedBootTitle;
        }
        if (global.console) ['log','warn','error'].forEach(function (name) {
            var native = global.console[name];
            if (typeof native !== 'function' || native._celesphoniaEnglishDiagnostic) return;
            var wrapped = function () {
                var state=matchState(),args;
                try { args=Array.prototype.map.call(arguments,displayed); } finally { restoreMatch(state); }
                return native.apply(this,args);
            };
            wrapped._celesphoniaEnglishDiagnostic = true;global.console[name] = wrapped;
        });
        var manager = global.SceneManager, update = manager && manager.updateDragInfo;
        if (typeof update === 'function' && !update._celesphoniaEnglishDiagnostic) {
            var wrappedUpdate = function () {
                var result = update.apply(this,arguments);
                if (global.document && document.title.replace(/\s+/g,' ').replace(/^ | $/g,'') === String(this._documentTitle).replace(/\s+/g,' ').replace(/^ | $/g,'') &&
                    this._documentTitle === this._infoHelp + this.infoWindow + this.infoExtend) {
                    var state=matchState();
                    try { document.title = displayed(this._infoHelp) + displayed(this.infoWindow) + displayed(this.infoExtend); }
                    finally { restoreMatch(state); }
                }
                return result;
            };
            wrappedUpdate._celesphoniaEnglishDiagnostic = true;manager.updateDragInfo = wrappedUpdate;
        }
    }
    function wrapConverter(proto) {
        var convert = proto && proto.convertEscapeCharacters;
        if (typeof convert !== 'function' || convert._celesphoniaEnglishConvert) return;
        var wrapped = function (text) {
            // CE97 expands a numeric streak and a nested message variable in one
            // native field. Preserve that precise grammar until expansion so
            // the complete notification wins over the isolated "times" label.
            var nativeForm = typeof text === 'string' ? text.replace(/\x1b/g, '\\').replace(/\\\\/g, '\\') : '';
            var nativeStreak = /^\\i\[\\v\[119\]\]\\c\[\\v\[118\]\]\\v\[616\]次\\v\[\\v\[2995\]\]$/i.test(nativeForm);
            // Keep the weekday glyph in the known native calendar frame until
            // the full display is assembled. Exact standalone labels such as
            // water/earth or Sunday would otherwise destroy its date context.
            var nativeCalendar = /^(?:\\c\[\d+\])?\\v\[202\]星期\(\\v\[203\]\)\s+\\v\[211\](?:\\c\[\d+\])?$/i.test(nativeForm);
            var nativeMenuCalendar=/^\s*(?:\\c\[\d+\])?(?:星期\\v\[4965\]|\\v\[4965\]曜日)\(\\v\[211\]\)$/i.test(nativeForm);
            var input = nativeStreak ? text : translate(text), isEnglish = input !== text || this._celesphoniaEnglishRendering;
            // Quest CE935/1213 preconvert only some escapes in variable 376.
            // Expand a known complete string into this display argument before
            // native escape conversion, so lowercase actor escapes expand too.
            // Never alter the variable, item metadata or canonical picture text.
            if (!nativeStreak && typeof input === 'string' && global.$gameVariables) input = input.replace(/(?:\\|\x1b)V\[(\d+)\]/gi, function (code, id) {
                if (nativeCalendar && Number(id) === 203 || nativeMenuCalendar && Number(id)===4965) return code;
                var value = $gameVariables.value(Number(id));
                if (typeof value !== 'string') return code;
                var canonical = value.replace(/\\\x1b/g, '\x1b').replace(/\x1b/g, '\\');
                if (!own.call(dict, canonical)) return code;
                isEnglish = true;return dict[canonical];
            });
            if (isEnglish) {
                input = normalizeCodes(input);
                // Reviewed native Maple fields omit the name-box closing >.
                // Repair only this translated display copy, after its numeric
                // actor slot; valid compound captions keep their own delimiter.
                if (typeof text === 'string' && (own.call(dict, text) || own.call(unterminatedNameTargets, input)) && input.split('\n')[0].indexOf('>') < 0) {
                    input = input.replace(/^(\\[Nn]<\\[Nn]\[\d+\])/, '$1>');
                }
            }
            if (isEnglish && global.Window_Message && this instanceof Window_Message && typeof this.setWordWrap === 'function' && /^<WordWrap>/i.test(input)) input = input.replace(/\n/g, '<br>');
            var rendered = convert.call(this, input);
            // One native speaker field nests a literal label in a nonnumeric
            // actor escape. Resolve only a reviewed full label in a name box.
            rendered = rendered.replace(/(\x1b[Nn]<)\x1b[Nn]\[([^\]\r\n]+)\](?=>)/g, function (whole, prefix, label) {
                var english = exact(label);return english !== label ? prefix + english : whole;
            });
            var fullField = nativeStreak ? translate(rendered) : rendered;
            isEnglish = isEnglish || fullField !== rendered;
            rendered = fullField;
            // Native item-icon escapes expand names after the whole quest note
            // has translated. Replace only that referenced icon/name field;
            // database records and reward quantities remain original.
            if (typeof input === 'string' && global.$dataItems) input.replace(/(?:\\|\x1b)ii\[(\d+)\]/gi, function (_, id) {
                var item = $dataItems[Number(id)];if (!item || typeof item.name !== 'string') return _;
                var english = exact(item.name);if (english === item.name) return _;
                ['I', 'i'].forEach(function (code) {
                    var prefix = '\x1b' + code + '[' + item.iconIndex + ']';
                    rendered = rendered.split(prefix + item.name).join(prefix + english);
                    rendered = rendered.split(prefix + ' ' + item.name).join(prefix + ' ' + english);
                });
                return _;
            });
            if (typeof input === 'string' && global.$gameVariables) input.replace(/(?:\\|\x1b)V\[(\d+)\]/gi, function (_, id) {
                if (nativeCalendar && Number(id) === 203 || nativeMenuCalendar && Number(id)===4965) return _;
                var value = String($gameVariables.value(Number(id))), english = translate(value);
                if (value && english !== value) rendered = rendered.split(value).join(english);
                return _;
            });
            if(nativeMenuCalendar){var weekdays={'日':'Sun','一':'Mon','二':'Tue','三':'Wed','四':'Thu','五':'Fri','六':'Sat','月':'Mon','火':'Tue','水':'Wed','木':'Thu','金':'Fri','土':'Sat'};rendered=rendered.replace(/([日一二三四五六月火水木金土])(?=\([^\r\n]*\))/,function(day){return weekdays[day];});}
            // Named CM values stay native in storage; translate only reviewed
            // values expanded in this local display copy.
            if(typeof input==='string'&&global.$CM_value&&typeof $CM_value.get==='function')input.replace(/(?:\\|\x1b)CM\[([^\]\r\n]+)\]/gi,function(_,key){
                var id=parseInt(key),value=$CM_value.get(isNaN(id)?key:id);
                if(typeof value==='string'&&value){var english=translate(value);if(english!==value)rendered=rendered.split(value).join(english);}
                return _;
            });
            rendered = translate(rendered);
            if (isEnglish && global.Window_Message && this instanceof Window_Message && typeof this.checkWordWrap === 'function') {
                this._wordWrap = true;rendered = rendered.replace(/\u2008/g, ' ');
            }
            return observe(rendered);
        };
        wrapped._celesphoniaEnglishConvert = true;proto.convertEscapeCharacters = wrapped;
    }
    function installObjectiveLedger() {
        if (!global.Scene_Glossary || !Scene_Glossary.prototype.createHelpWindow) return;
        function objectiveText(scene) {
            if (!global.$gameParty || $gameParty.getSelectedGlossaryType() !== 1 || !global.$gameActors) return null;
            // CallPurpose stores the current story objective in actor 76. It is
            // independent of accepted mission inventory; read it without creating
            // an actor, granting an item, or changing mission progress.
            var actor = $gameActors._data && $gameActors._data[76];
            var source = actor && actor.name();
            if (!source) return null;
            var win = scene._helpWindow, rendered = win.convertEscapeCharacters(source), width = win.contentsWidth();
            var lines = [];
            rendered.split('\n').forEach(function (paragraph) {
                var line = '';
                paragraph.split(/\s+/).forEach(function (word) {
                    var candidate = line ? line + ' ' + word : word;
                    var plain = candidate.replace(/\x1b[A-Za-z]+(?:\[[^\]]*\])?/g, '');
                    if (line && win.textWidth(plain) > width) { lines.push(line); line = word; }
                    else line = candidate;
                });
                lines.push(line);
            });
            return 'Accepted Quests\nCurrent Objective\n' + lines.join('\n');
        }
        var createHelp = Scene_Glossary.prototype.createHelpWindow;
        Scene_Glossary.prototype.createHelpWindow = function () {
            createHelp.apply(this, arguments);
            var text = objectiveText(this);
            if (text === null) return;
            this._helpWindow.height = this._helpWindow.fittingHeight(text.split('\n').length);
            this._helpWindow.visible = true;
            this._helpWindow.createContents();
            this._helpWindow.setText(text);
            this._helpWindow.refresh();
        };
        var updateHelp = Scene_Glossary.prototype.updateHelp;
        Scene_Glossary.prototype.updateHelp = function () {
            updateHelp.apply(this, arguments);
            var text = objectiveText(this);
            if (text !== null) this._helpWindow.setText(text);
        };
    }
    function installReinforcementBatch() {
        if (!global.Game_Interpreter || !global.BattleManager) return;
        var version = global.CelesphoniaEnglishMeta && global.CelesphoniaEnglishMeta.version;
        if (version !== 'three-in-one-3-3-4' && version !== 'three-in-one-3-3-5-trial' && version !== 'three-in-one-3-3-5-v1-01' && version !== 'three-in-one-3-3-5-v1-02') return;
        var command = Game_Interpreter.prototype.pluginCommand;
        if (typeof command !== 'function' || command._celesphoniaEnglishReinforcementOriginal) return;
        // This command appends all battlers synchronously, then recreates the
        // entire sprite list inside its troop loop. Coalesce only that visual
        // work; retain every native command, random roll, state and name update.
        var wrapped = function (name) {
            var sprites = BattleManager._spriteset;
            if (typeof name !== 'string' || name.toLowerCase() !== 'add_enemy_troop' || !global.$gameParty || !$gameParty.inBattle() || !sprites || typeof sprites.removeEnemies !== 'function' || typeof sprites.createEnemies !== 'function') return command.apply(this, arguments);
            var remove = sprites.removeEnemies, create = sprites.createEnemies;
            var ownRemove = own.call(sprites, 'removeEnemies'), ownCreate = own.call(sprites, 'createEnemies'), removed = false, created = false;
            sprites.removeEnemies = function () { removed = true; };
            sprites.createEnemies = function () { created = true; };
            try { return command.apply(this, arguments); }
            finally {
                if (ownRemove) sprites.removeEnemies = remove; else delete sprites.removeEnemies;
                if (ownCreate) sprites.createEnemies = create; else delete sprites.createEnemies;
                if (removed) remove.call(sprites);
                if (created) create.call(sprites);
            }
        };
        wrapped._celesphoniaEnglishReinforcementOriginal = command;
        Game_Interpreter.prototype.pluginCommand = wrapped;
    }
    function installSeparateBedroomToggle() {
        // The original bedroom stacks wardrobe actions at (4,16) and
        // (4,15). Keep native 1x3 furniture and both original menus,
        // but move the second marker above the wardrobe at (4,14).
        var version=global.CelesphoniaEnglishMeta && global.CelesphoniaEnglishMeta.version;
        if(version!=='three-in-one-3-3-5-v1-051' &&
           version!=='three-in-one-3-3-5-v1-052' &&
           version!=='three-in-one-3-3-5-v1-053' &&
           version!=='three-in-one-3-3-5-v1-06' &&
           version!=='three-in-one-3-3-5-v1-061') return;
        if(!global.Game_Map || !global.Game_Event ||
            typeof Game_Map.prototype.update!=='function' ||
            typeof Game_Event.prototype.meetsConditions!=='function') return;
        var nativeUpdate=Game_Map.prototype.update;
        if(nativeUpdate._celesphoniaSeparateWardrobe) return;
        function targetMap(map) {
            if(!map || typeof map.mapId!=='function' || map.mapId()!==5 ||
               !global.$dataMap || !$dataMap.events) return false;
            var outfit=$dataMap.events[19], toggle=$dataMap.events[83];
            return outfit && toggle && outfit.x===4 && outfit.y===16 &&
                toggle.x===4 && toggle.y===15 &&
                typeof toggle.note==='string' &&
                toggle.note.indexOf('<TE:不知廉耻立绘调整>')!==-1;
        }
        Game_Map.prototype.update=function(){
            if(targetMap(this) && typeof this.event==='function'){
                var toggle=this.event(83);
                if(toggle && toggle._eventId===83 &&
                   typeof toggle.locate==='function' &&
                   (toggle.x===4 || toggle.x===5) && toggle.y===15) {
                    toggle.locate(4,14);
                    if(typeof toggle.refresh==='function')toggle.refresh();
                }
            }
            return nativeUpdate.apply(this,arguments);
        };
        Game_Map.prototype.update._celesphoniaSeparateWardrobe=true;
        // Fit the complete two-column source artwork into its existing
        // one-column footprint. These hooks affect rendering only: native
        // map cells, passability and event command lists remain untouched.
        if(global.Tilemap && Tilemap.prototype._readMapData &&
           global.Spriteset_Map && Spriteset_Map.prototype.update && global.Sprite) {
            var readTile=Tilemap.prototype._readMapData;
            Tilemap.prototype._readMapData=function(x,y,z){
                if(targetMap(global.$gameMap) && x===4 && y>=15 && y<=17 && z===3)
                    return 0;
                return readTile.apply(this,arguments);
            };
            var spriteUpdate=Spriteset_Map.prototype.update;
            Spriteset_Map.prototype.update=function(){
                var result=spriteUpdate.apply(this,arguments);
                if(!targetMap(global.$gameMap) || !this._tilemap){
                    if(this._celesphoniaWardrobe)this._celesphoniaWardrobe.visible=false;
                    return result;
                }
                var map=global.$gameMap,tw=map.tileWidth(),th=map.tileHeight();
                if(!this._celesphoniaWardrobe){
                    var sprite=this._celesphoniaWardrobe=new Sprite(new Bitmap(tw,th*3));
                    sprite.anchor.y=1;sprite.z=1;this._tilemap.addChild(sprite);
                    ImageManager.loadTileset('Modern_Living Room').addLoadListener(function(source){
                        sprite.bitmap.blt(source,384,384,96,144,0,0,tw,th*3);
                    });
                }
                this._celesphoniaWardrobe.visible=true;
                this._celesphoniaWardrobe.x=map.adjustX(4)*tw;
                this._celesphoniaWardrobe.y=map.adjustY(18)*th;
                return result;
            };
        }
        var nativeConditions=Game_Event.prototype.meetsConditions;
        Game_Event.prototype.meetsConditions=function(page){
            // Creator recalculation can reset Shamelessness immediately
            // after a slot with value 80 loads. Make only this separated
            // menu accessible; never change gameplay variables, skills,
            // saved events, or other unlock conditions.
            if(this._mapId===5 && this._eventId===83 &&
               targetMap(global.$gameMap) && page &&
               page.conditions && page.conditions.variableValid &&
               page.conditions.variableId===1090 &&
               page.conditions.variableValue===80)
                return true;
            return nativeConditions.apply(this,arguments);
        };
    }
    function installGlossaryPictureLayout() {
        if(!global.Window_Glossary || !Window_Glossary.prototype.drawItemSub ||
           !Window_Glossary.prototype.getPictureScale) return;
        var proto=Window_Glossary.prototype,draw=proto.drawItemSub,scale=proto.getPictureScale;
        if(draw._celesphoniaEnglishPictureLayout) return;
        proto.getPictureScale=function(item,bitmap,text){
            var layout=this._celesphoniaEnglishGlossaryLayout;
            if(layout && layout.item===item && layout.page===this._pageIndex && layout.bitmap===bitmap)
                return layout.scale;
            return scale.apply(this,arguments);
        };
        proto.drawItemSub=function(bitmap){
            this._celesphoniaEnglishGlossaryLayout=null;
            var picture=this.getPictureName(this._pageIndex);
            if(!bitmap || !bitmap.width || !bitmap.height ||
               !/^Glossary_[124]$/.test(picture || '') || this.getPicturePosition()!=='text')
                return draw.apply(this,arguments);
            var source=this.getDescription(this._pageIndex) || '',display=translate(source);
            var width=this.contentsWidth(),height=this.contentsHeight();
            var top=Math.max(0,Math.min(this.getTextPosition(),height)),gap=display.trim()?12:0;
            var nativeScale=scale.call(this,this._itemData,bitmap,source);
            var imageBudget=display.trim()?Math.min(240,(height-top)*0.32):height-top;
            var fit=fitPanelText(this,display,width,Math.max(1,height-top-imageBudget-gap));
            if(!display.trim())fit={text:'',fontSize:this.standardFontSize(),height:0,fits:true};
            var available=Math.max(0,height-top-fit.height-gap);
            var imageScale=Math.max(0,Math.min(nativeScale,1,(width-8)/bitmap.width,
                Math.min(imageBudget,available)/bitmap.height));
            this._celesphoniaEnglishGlossaryLayout={item:this._itemData,page:this._pageIndex,
                bitmap:bitmap,source:source,display:display,fit:fit,scale:imageScale,
                imageHeight:bitmap.height*imageScale};
            this.drawItemText(source,top);
            this.drawPicture(bitmap,source,top+fit.height+gap);
            this.drawPlusPictures();
        };
        proto.drawItemSub._celesphoniaEnglishPictureLayout=true;
    }
    function install() {
        if (installed) return; installed = true;
        installTitleDescription();
        installSeparateBedroomToggle();
        installGlossaryPictureLayout();
        // SceneGlossary loads its authored help pictures through
        // ImageManager.loadPicture. Redirect only the four source-verified
        // picture/animation names to this English overlay's own assets.
        // The native bitmap, animation frames, and game data stay untouched.
        var artwork = global.CelesphoniaEnglishImageOverrides || {};
        if (global.ImageManager && typeof ImageManager.loadBitmap === 'function') {
            [
                {method:'loadPicture',names:artwork.picture},
                {method:'loadAnimation',names:artwork.animation}
            ].forEach(function (entry) {
                if (!entry.names || !Object.keys(entry.names).length ||
                    typeof ImageManager[entry.method] !== 'function') return;
                var original=ImageManager[entry.method], mapping=entry.names;
                if (original._celesphoniaEnglishPictureRedirect) return;
                var redirected=function (name,hue) {
                    if (typeof name==='string' && own.call(mapping,name)) {
                        return this.loadBitmap('celesphonia-english/images/',
                            mapping[name],hue,true);
                    }
                    return original.apply(this,arguments);
                };
                redirected._celesphoniaEnglishPictureRedirect=true;
                redirected._celesphoniaEnglishOriginal=original;
                ImageManager[entry.method]=redirected;
            });
        }
        if (global.$dataEnemies) $dataEnemies.forEach(function (enemy) {
            if (!enemy || typeof enemy.name !== 'string' || !enemy.name) return;
            var english = exact(enemy.name);enemyNames[enemy.name] = english;enemyNames[english] = english;
            // Older overlay logs may contain a partially translated caption.
            // Recover only this exact known database-name alias, never prose.
            var alias=enemy.name.replace(/(?:HP|MP|SP|TP)?[\u3040-\u30ff\u3400-\u9fff]+/g,function(label){return own.call(labels,label)?labels[label]:label;});if(alias!==enemy.name)enemyNames[alias]=english;
        });
        // Some variants already ship English system-message formats. Their
        // native formatter still inserts original enemy captions into the slots.
        // Index those exact native grammars once, without changing TextManager.
        var messages = global.$dataSystem && $dataSystem.terms && $dataSystem.terms.messages;
        if (messages) Object.keys(messages).forEach(function (key) {
            var text = messages[key];
            if (typeof text !== 'string' || own.call(dict, text) || /[\u3040-\u30ff\u3400-\u9fff]/.test(text) || !/%[1-9]/.test(text)) return;
            var slots = [], pattern = '^';
            text.split(/(%[1-9])/).forEach(function (part) {
                if (/^%[1-9]$/.test(part)) { slots.push(part);pattern += '([^\\r\\n]+?)'; }
                else pattern += part.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            });
            var item = {id:templates.length,regex:new RegExp(pattern + '$'),slots:slots,target:text,specificity:text.replace(/%[1-9]/g,'').length};
            templates.push(item);fallbackTemplates.push(item);
        });
        if (global.Bitmap) {
            wrap(Bitmap.prototype, 'drawText', true);
            wrap(Bitmap.prototype, 'measureTextWidth', true);
        }
        if (global.Spriteset_Map && typeof Spriteset_Map.prototype.createUpperLayer === 'function') {
            var upperLayer = Spriteset_Map.prototype.createUpperLayer;
            Spriteset_Map.prototype.createUpperLayer = function () {
                var result = upperLayer.apply(this,arguments), screen = this._hzChoiceScreen;
                var context = screen && screen.bitmap && screen.bitmap.context;
                if (context && !context._celesphoniaEnglishMapCaptions) {
                    var fill = context.fillText;
                    context.fillText = function (text,x,y) {
                        // HzChoiceEvent bypasses Bitmap.drawText. Restrict the
                        // hook to its own canvas and fit the native 90/120px pill.
                        var display=translate(text),args=Array.prototype.slice.call(arguments),font=this.font;
                        args[0]=display;
                        try {
                            var size=/^(\d+)px/.exec(font),limit=size&&Number(size[1])===16?112:82;
                            if(size&&typeof this.measureText==='function')for(var n=Number(size[1]);n>9&&this.measureText(display).width>limit;n--)this.font=font.replace(/^\d+px/,(n-1)+'px');
                            args[3]=limit;
                            return fill.apply(this,args);
                        } finally { this.font=font; }
                    };
                    context._celesphoniaEnglishMapCaptions=true;
                }
                return result;
            };
        }
        if (global.Window_Base) {
            wrap(Window_Base.prototype, 'drawText');
            wrap(Window_Base.prototype, 'drawTextEx');
            var nativeWindowInitialize=Window_Base.prototype.initialize;
            Window_Base.prototype.initialize=function(){
                if(this.constructor&&this.constructor.name==='Window_SoloStatus')installSoloFieldWidth(Object.getPrototypeOf(this));
                return nativeWindowInitialize.apply(this,arguments);
            };
            wrapConverter(Window_Base.prototype);
            ['weaponDescriotion', 'ArmorDescriotion'].forEach(function (method) {
                var original = Window_Base.prototype[method];if (typeof original !== 'function') return;
                Window_Base.prototype[method] = function (item) {
                    var text = original.apply(this, arguments), option = item && item.meta && item.meta.option;
                    // ItemDescription appends this exact note field as a suffix.
                    // Translate the complete option before the assembled stat line.
                    if (typeof option === 'string' && text.slice(-option.length) === option) text = text.slice(0, -option.length) + translate(option);
                    return translate(text);
                };
            });
            // Names and variable values are translated only during escape-code rendering.
            ['actorName', 'partyMemberName'].forEach(function (method) {
                if (typeof Window_Base.prototype[method] !== 'function') return;
                var previous = Window_Base.prototype[method];
                Window_Base.prototype[method] = function () { return translate(previous.apply(this, arguments)); };
            });
        }
        if (global.Sprite_Picture && typeof Sprite_Picture.prototype.makeDynamicBitmap === 'function') {
            var makePicture = Sprite_Picture.prototype.makeDynamicBitmap;
            Sprite_Picture.prototype.makeDynamicBitmap = function () {
                var info = this.dTextInfo, raw = info && info.originalValue;
                if(this._pictureId===84&&info&&info.size===20&&typeof raw==='string'&&/^\s*(?:\\|\x1b)v\[4587\]\s*$/i.test(raw)&&global.$gameVariables&&$gameVariables._data[4587]===''){
                    // CE823 writes an empty description for locked title 999.
                    // Game_Variables.value turns that empty string into zero;
                    // preserve its blank presentation without changing storage.
                    var blank={};Object.keys(info).forEach(function(key){blank[key]=info[key];});blank.value='';this.dTextInfo=blank;
                    try{return makePicture.apply(this,arguments);}finally{this.dTextInfo=info;}
                }
                if((this._pictureId===86||this._pictureId===87)&&typeof raw==='string'&&
                   /^(?:\\|\x1b)c\[16\](?:瘴气耐性|快感耐性)\s/i.test(raw)){
                    var nativeResult=makePicture.apply(this,arguments);
                    resistancePicture(this,info);
                    return nativeResult;
                }
                // Common Events 463/468 draw the single/group groper HUD with
                // D_TEXT. Translate only the three verified caption lines.
                // The native turn and excitement/crowd variable values,
                // originalValue, picture ID and game logic stay untouched.
                if (typeof raw === 'string' && info && typeof info.value === 'string') {
                    var hudNative=raw.replace(/\x1b/g,'\\').replace(/\r/g,'').split('\n');
                    var hudDisplay=info.value.replace(/\r/g,'').split('\n');
                    if (hudNative.length === 4 && hudDisplay.length === 4 &&
                        /^Turn \\v\[1506\] \/ 10$/i.test(hudNative[0]) &&
                        /^(痴汉兴奋度|痴汉聚集度) \\v\[1507\]$/i.test(hudNative[1]) &&
                        /^痴汉LV (熟练|普通|生疏)$/.test(hudNative[2]) &&
                        hudNative[3] === '' && hudDisplay[3] === '' &&
                        /^Turn /.test(hudDisplay[0])) {
                        var groperMetric=/^痴汉兴奋度/.test(hudNative[1])?'痴汉兴奋度':'痴汉聚集度';
                        var metricName=groperMetric==='痴汉兴奋度'?'Groper Excitement':'Groper Crowd';
                        var shownMetric=hudDisplay[1].replace(
                            new RegExp('^'+groperMetric+'(?=\\s|$)'),metricName);
                        var ranks={'熟练':'Groper Level: Technician',
                                   '普通':'Groper Level: Normal',
                                   '生疏':'Groper Level: Clumsy'};
                        var oldRank=hudNative[2].substring('痴汉LV '.length);
                        if ((shownMetric!==hudDisplay[1]||
                             hudDisplay[1].indexOf(metricName)===0) &&
                            (hudDisplay[2]===hudNative[2]||
                             hudDisplay[2]===ranks[oldRank])) {
                            var localized={};
                            Object.keys(info).forEach(function(key){localized[key]=info[key];});
                            localized.value=[hudDisplay[0],shownMetric,
                                ranks[oldRank],''].join('\n');
                            this.dTextInfo=localized;
                            try{return makePicture.apply(this,arguments);}
                            finally{this.dTextInfo=info;}
                        }
                    }
                }
                // CommonEvent 814 owns this title-column grammar. Preserve its
                // fixed row pitch while fitting each English label separately.
                var title = info && (info.size === 16 || info.size === 21) && typeof raw === 'string' && /(?:\\|\x1b)is\[(?:\\|\x1b)v\[4581\]\]/i.test(raw);
                var profile = info && typeof raw === 'string' && /(?:\\|\x1b)v\[4691\]/i.test(raw);
                var quest = info && info.size === 22 && typeof raw === 'string' && /^\s*(?:\\|\x1b)v\[376\]\s*$/i.test(raw) && this._pictureId === 92;
                var development = info && this._pictureId === 82 && typeof raw === 'string' && /(?:\\|\x1b)v\[1102\]/i.test(raw) && /LV/.test(raw);
                var experience = info && this._pictureId >= 85 && this._pictureId <= 88 && typeof raw === 'string' && /(?:\\|\x1b)v\[(?:\\|\x1b)v\[4700\]\]/i.test(raw);
                var heading = info && this._pictureId === 84 && typeof raw === 'string' && /(?:\\|\x1b)v\[1021\]/i.test(raw) && /(?:\\|\x1b)v\[1022\]/i.test(raw);
                if ((development || experience || heading) && global.SceneManager && typeof SceneManager.getHiddenWindow === 'function') {
                    var nativePicture = this.picture && this.picture(), originX = nativePicture && nativePicture.x ? nativePicture.x() : development ? 472 : heading ? 40 : 80;
                    var originY = nativePicture && nativePicture.y ? nativePicture.y() : development ? 280 : heading ? 120 : 260;
                    var panel = SceneManager.getHiddenWindow(), localInfo = {}, oldFont = panel.contents.fontSize;
                    Object.keys(info).forEach(function (key) { localInfo[key] = info[key]; });
                    var display = info.value;
                    if (experience) display = display.replace(/Consecutive Orgasm Record/g,'Orgasm Streak').replace(/Highest Pleasure in One Session/g,'Peak Session Pleasure').replace(/Sexual Harassment Exp\./g,'Harassment Exp.').replace(/Aphrodisiac Exposure Exp\./g,'Aphrodisiac Exp.');
                    if (development) {
                        var compact = [], empty = false;
                        display.split('\n').forEach(function (line) { var blank = !line.trim();if (!blank || !empty) compact.push(line);empty = blank; });
                        display = compact.join('\n');
                    }
                    var panelWidth = experience ? 184 : heading ? 460-originX-16 : Graphics.boxWidth-originX-20;
                    var panelHeight = heading ? 260-originY-12 : Graphics.boxHeight-originY-16;
                    try {
                        var fixedLayout = fitFixedLines(panel,display,panelWidth,panelHeight,info.size||16);
                        localInfo.value = fixedLayout.text;localInfo.size = fixedLayout.fontSize;this.dTextInfo = localInfo;
                        this._celesphoniaEnglishDevelopmentLayout = fixedLayout;
                        return makePicture.apply(this,arguments);
                    } finally { this.dTextInfo = info;panel.contents.fontSize = oldFont; }
                }
                if ((profile || quest) && global.SceneManager && typeof SceneManager.getHiddenWindow === 'function') {
                    var picture = this.picture && this.picture(), x = picture && picture.x ? picture.x() : 460, y = picture && picture.y ? picture.y() : 80;
                    var hidden = SceneManager.getHiddenWindow(), standard = hidden.standardFontSize, font = hidden.contents.fontSize;
                    var local = {};Object.keys(info).forEach(function (key) { local[key] = info[key]; });
                    try {
                        hidden.standardFontSize = function () { return info.size || standard.call(this); };
                        var width = quest ? Graphics.boxWidth - 64 : Graphics.boxWidth - x - 20;
                        var height = quest ? 2 * Math.min(y - 142, Graphics.boxHeight - y - 150) : Graphics.boxHeight - y - 32;
                        var layout = fitPanelText(hidden, info.value, width, height);
                        local.value = layout.text;local.size = layout.fontSize;this.dTextInfo = local;
                        if (quest) this._celesphoniaEnglishQuestLayout = layout;
                        else this._celesphoniaEnglishProfileLayout = layout;
                        return makePicture.apply(this, arguments);
                    } finally { this.dTextInfo = info;hidden.standardFontSize = standard;hidden.contents.fontSize = font; }
                }
                var result = makePicture.apply(this, arguments);
                return title&&this.bitmap ? fitTitleColumn(this,info,makePicture,arguments,result) : result;
            };
        }
        installObjectiveLedger();
        installReinforcementBatch();
        installSaveTitles();
        installDiagnosticPresentation();
        installLiveComments();
        installEquipmentHelp();
        installGroperTicker();
    }
    function flushSeen() {
        try {
            if (!global.require || !global.CelesphoniaEnglishMeta || !seenCount) return;
            var fs = require('fs'), path = require('path');
            var web = path.dirname(decodeURIComponent(global.location.pathname).replace(/^\/(?:([A-Za-z]:))/, '$1'));
            // NW.js can expose a chrome-extension URL rather than a file URL.
            var appRoot = global.nw && nw.App && nw.App.startPath ? nw.App.startPath : typeof process !== 'undefined' ? process.cwd() : null;
            var folder = appRoot && typeof global.CelesphoniaEnglishMeta.webPath === 'string' ? path.resolve(appRoot, global.CelesphoniaEnglishMeta.webPath, 'celesphonia-english') : path.join(web, 'celesphonia-english');
            var file = path.join(folder, 'seen-untranslated.json'), combined = {};
            if (fs.existsSync(file)) combined = JSON.parse(fs.readFileSync(file, 'utf8'));
            Object.keys(pending).forEach(function (key) { combined[key] = key; });
            fs.writeFileSync(file, JSON.stringify(combined), 'utf8');
            pending = {}; seenCount = 0;
        } catch (error) { if (global.console) console.warn('English overlay: seen-text log could not be saved.', error.message); }
    }
    global.CelesphoniaEnglish = {translate: translate, install: install, installDiagnosticPresentation: installDiagnosticPresentation, flushSeen: flushSeen, captureSeen:true, mode: 'display-only', _runtimeLoaded: true};
    // Plugins are loaded asynchronously by main.js. Install after them, before title rendering.
    if (global.Scene_Boot && Scene_Boot.prototype.start) {
        var start = Scene_Boot.prototype.start;
        Scene_Boot.prototype.start = function () { install(); return start.apply(this, arguments); };
    } else install();
    if (global.addEventListener) global.addEventListener('beforeunload', flushSeen);
    if (global.setInterval) global.setInterval(flushSeen, 30000);
}(typeof window !== 'undefined' ? window : this));
