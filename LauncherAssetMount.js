(function () {
    'use strict';
    var fs = require('fs'), path = require('path');
    var root = path.dirname(process.execPath);
    var configFile = path.join(root, 'launcher-base.json');
    if (!fs.existsSync(configFile)) return;
    var config = JSON.parse(fs.readFileSync(configFile, 'utf8'));
    var localWeb = path.join(root, 'www'), baseWeb = path.join(config.BaseRoot, 'www');
    function fileUrl(p) {
        return 'file:///' + p.replace(/\\/g, '/').split('/').map(function (s, i) {
            return i === 0 && /^[a-z]:$/i.test(s) ? s : encodeURIComponent(s);
        }).join('/');
    }
    function existsAsset(folder, relative) {
        if (fs.existsSync(path.join(folder, relative))) return true;
        var encrypted = relative.replace(/\.png$/i, '.rpgmvp').replace(/\.ogg$/i, '.rpgmvo').replace(/\.m4a$/i, '.rpgmvm');
        return encrypted !== relative && fs.existsSync(path.join(folder, encrypted));
    }
    function resolve(url) {
        if (typeof url !== 'string' || /^(data:|blob:|https?:)/i.test(url)) return url;
        var value;
        try { value = decodeURIComponent(url.split('?')[0].split('#')[0]); } catch (_) { return url; }
        value = value.replace(/\\/g, '/');
        var local = localWeb.replace(/\\/g, '/');
        if (/^file:\/\/\//i.test(value)) value = value.substring(8);
        if (value.toLowerCase().indexOf(local.toLowerCase() + '/') === 0) value = value.substring(local.length + 1);
        if (!/^(img|audio|movies)\//i.test(value) || value.split('/').indexOf('..') >= 0) return url;
        if (existsAsset(localWeb, value)) return url;
        if (existsAsset(baseWeb, value)) return fileUrl(path.join(baseWeb, value));
        return url;
    }
    window.LauncherAssetResolver = resolve;
    var originalOpen = XMLHttpRequest.prototype.open;
    XMLHttpRequest.prototype.open = function () {
        var args = Array.prototype.slice.call(arguments); args[1] = resolve(args[1]);
        return originalOpen.apply(this, args);
    };
    function hookSource(proto) {
        var descriptor = Object.getOwnPropertyDescriptor(proto, 'src');
        if (!descriptor || !descriptor.set || !descriptor.configurable) return;
        Object.defineProperty(proto, 'src', { configurable: descriptor.configurable, enumerable: descriptor.enumerable,
            get: descriptor.get, set: function (url) { descriptor.set.call(this, resolve(url)); } });
    }
    hookSource(HTMLImageElement.prototype); hookSource(HTMLMediaElement.prototype);
    var requestImage = Bitmap.prototype._requestImage;
    Bitmap.prototype._requestImage = function (url) { return requestImage.call(this, resolve(url)); };
})();
