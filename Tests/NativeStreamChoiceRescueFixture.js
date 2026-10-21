(function () {
    'use strict';
    var fs = require('fs'), output = __OUTPUT__, input = __SAVE__;
    var expectedRepair = __REPAIR__, expectedControls = __CONTROLS__;
    var report = {checks: [], errors: []}, began = Date.now(), done = false;
    function check(name, ok, detail) {
        report.checks.push({name: name, pass: !!ok, detail: detail});
        if (!ok) throw new Error(name + ': ' + JSON.stringify(detail));
    }
    function finish(error) {
        if (done) return;
        done = true; report.status = error ? 'FAIL' : 'PASS';
        report.error = error && String(error.stack || error);
        fs.writeFileSync(output, JSON.stringify(report, null, 2));
        nw.App.quit();
    }
    window.addEventListener('error', function (e) { report.errors.push(String(e.message)); });
    var timer = setInterval(function () {
        if (Date.now() - began > 30000) { clearInterval(timer); finish(new Error('Native startup timeout')); return; }
        if (!window.SceneManager || !SceneManager._scene || SceneManager._scene.constructor.name !== 'Scene_Title') return;
        clearInterval(timer);
        try {
            check('launch mode matches runtime-controls setting', !!window.CelesphoniaControls === expectedControls, !!window.CelesphoniaControls);
            check('repair loads independently of controls', !!window.CelesphoniaStreamChoiceRescue === expectedRepair, !!window.CelesphoniaStreamChoiceRescue);
            DataManager.createGameObjects();
            DataManager.extractSaveContents(JsonEx.parse(LZString.decompressFromBase64(fs.readFileSync(input, 'utf8'))));
            report.sourceMap = $gameMap.mapId();
            // Position this in-memory copy at the PC; never rewrite its save.
            var mapText = fs.readFileSync(__WEB__ + '/data/Map005.json', 'utf8');
            try { $dataMap = JSON.parse(mapText); }
            catch (e) {
                var bytes = new Buffer(mapText, 'base64'), key = 'maple';
                for (var b = 0; b < bytes.length; b++) bytes[b] ^= key.charCodeAt(b % 5);
                $dataMap = JSON.parse(bytes.toString('utf8'));
            }
            DataManager.onLoad($dataMap);
            $gamePlayer.reserveTransfer(5, 8, 12, 8, 0);
            $gamePlayer.performTransfer();
            report.prerequisites = {map: $gameMap.mapId(), firstComplete: $gameSwitches.value(291), unlocked: $gameSwitches.value(287), site: $gameSwitches.value(286), webcam: $gameParty.numItems($dataItems[435]), hour: $gameVariables.value(204)};
            check('repeat-stream save has native prerequisites', report.prerequisites.map === 5 && report.prerequisites.firstComplete && report.prerequisites.unlocked && report.prerequisites.site && report.prerequisites.webcam > 0, report.prerequisites);
            $gameMessage.clear();
            var win = new Window_Message();
            SceneManager._scene.addWindow(win);
            win.subWindows().forEach(function (w) { SceneManager._scene.addWindow(w); });
            win.pause = true;
            win._textState = {text: 'Previous PC help'};
            win.forceClear();
            check('native forceClear reproduces the stale pause', win.pause && !win._textState, {pause: win.pause, textState: win._textState});
            var interpreter = new Game_Interpreter();
            interpreter.setup($dataCommonEvents[69].list, 15);
            for (var i = 0; i < 120 && !($gameMessage.hasText() && $gameMessage.isChoice()); i++) interpreter.update();
            check('native CE69 queues the repeat-stream prompt', $gameMessage.hasText() && $gameMessage.isChoice() && $gameMessage.choices()[0] === '闲聊的同时收集评论', $gameMessage.choices());
            var choices = $gameMessage.choices().slice();
            for (var frame = 0; frame < 240 && !win._choiceWindow.active; frame++) {
                Graphics.frameCount++;
                win.update();
                win.subWindows().forEach(function (w) { w.update(); });
            }
            report.menu = {active: win._choiceWindow.active, pause: win.pause, choices: choices, repairs: expectedRepair ? CelesphoniaStreamChoiceRescue.count() : 0};
            check('choice window opens only when repair is loaded', !!win._choiceWindow.active === expectedRepair, report.menu);
            if (expectedRepair) {
                check('repair ran once despite overlay plus session fallback', CelesphoniaStreamChoiceRescue.count() === 1, report.menu);
                win._choiceWindow.select(0);
                win._choiceWindow.processOk();
                for (var step = 0; step < 120 && interpreter.isRunning(); step++) interpreter.update();
                check('native choice callback starts the selected stream branch', $gameVariables.value(4755) === 1, $gameVariables.value(4755));
            } else {
                check('baseline preserves the reported stuck state', win.pause && $gameMessage.isChoice(), report.menu);
            }
            finish();
        } catch (e) { finish(e); }
    }, 100);
})();
