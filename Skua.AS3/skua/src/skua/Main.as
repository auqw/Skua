package skua {
import flash.display.DisplayObject;
import flash.display.Loader;
import flash.display.LoaderInfo;
import flash.display.MovieClip;
import flash.display.Stage;
import flash.display.StageAlign;
import flash.display.StageScaleMode;
import flash.events.Event;
import flash.events.ErrorEvent;
import flash.events.IOErrorEvent;
import flash.events.KeyboardEvent;
import flash.events.MouseEvent;
import flash.events.ProgressEvent;
import flash.events.SecurityErrorEvent;
import flash.events.TimerEvent;
import flash.events.UncaughtErrorEvent;
import flash.net.URLLoader;
import flash.net.URLRequest;
import flash.system.ApplicationDomain;
import flash.system.LoaderContext;
import flash.system.Security;
import flash.text.TextField;
import flash.utils.Timer;
import flash.utils.getQualifiedClassName;

import skua.api.Auras;
import skua.module.ModalMC;
import skua.module.Modules;
import skua.util.SFSEvent;

[SWF(frameRate="30", backgroundColor="#000000", width="958", height="550")]
public class Main extends MovieClip {
    public static var instance:Main;
    private static var _gameClass:Class;
    private static var _handler:*;

    public var game:*;
    public var external:Externalizer;
    private var sURL:String = 'http://game.aq.com/game/';
    private var versionUrl:String = (sURL + 'api/data/gameversion');
    private var loginURL:String = (sURL + 'api/login/now');
    private var sFile:String;
    private var sBG:String = 'hideme.swf';
    private var isEU:Boolean;
    private var urlLoader:URLLoader;
    private var vars:Object;
    private var loader:Loader;
    private var gameLoadRequested:Boolean = false;
    private var sTitle:String = '<font color="#FDAF2D">AURAS!!!</font>';
    private var stg:Stage;
    private var gameDomain:ApplicationDomain;
    private var customBGLoader:Loader;
    private var customBGReady:MovieClip = null;
    public var customBGLagKiller:MovieClip = null;
    private var customBackgroundURL:String;

    public function Main() {
        String.prototype.trim = function():String {
            var s:String = String(this);
            return s.replace(/^\s+|\s+$/g, "");
        };

        Main.instance = this;

        if (stage) this.init();
        else addEventListener(Event.ADDED_TO_STAGE, this.init);
    }

    public static function loadGame():void {
        if (Main.instance.gameLoadRequested) {
            Main.instance.report('loadGame ignored; already requested');
            return;
        }
        Main.instance.gameLoadRequested = true;
        Main.instance.report('loadGame called');
        Main.instance.onAddedToStage();
        Main.instance.external.call('pre-load');
    }

    public static function setBackgroundValues(sBGValue:String, customBackground:String):void {
        if (sBGValue && sBGValue.length > 0) {
            instance.sBG = sBGValue;
            if (instance.game && instance.game.params) {
                instance.game.params.sBG = sBGValue;
            }
        }
        if (customBackground && customBackground.length > 0) {
            instance.customBackgroundURL = customBackground;
            instance.initCustomBackground();
        } else {
            instance.customBackgroundURL = null;
        }
    }

    private function init(e:Event = null):void {
        removeEventListener(Event.ADDED_TO_STAGE, this.init);
        this.external = new Externalizer();
        this.external.init(this);
    }

    private function onAddedToStage():void {
        Security.allowDomain('*');
        this.report('loading version ' + this.versionUrl);
        this.urlLoader = new URLLoader();
        this.urlLoader.addEventListener(Event.COMPLETE, this.onDataComplete);
        this.urlLoader.addEventListener(IOErrorEvent.IO_ERROR, this.onLoadError);
        this.urlLoader.addEventListener(SecurityErrorEvent.SECURITY_ERROR, this.onLoadError);
        this.urlLoader.load(new URLRequest(this.versionUrl));
    }

    private function onDataComplete(event:Event):void {
        this.urlLoader.removeEventListener(Event.COMPLETE, this.onDataComplete);
        this.report('version loaded ' + event.target.data);
        this.vars = JSON.parse(event.target.data);
        this.sFile = ((this.vars.sFile + '?ver=') + this.vars.sVersion);
        this.sTitle = this.vars.sTitle;
        this.sBG = this.vars.sBG;
        this.isEU = String(this.vars.isEU) == "true";
        this.report('loading game file ' + this.sURL + 'gamefiles/' + this.sFile);
        this.loadGame()
    }

    private function loadGame():void {
        this.loader = new Loader();
        this.loader.contentLoaderInfo.addEventListener(Event.INIT, this.onLoaderInit);
        this.loader.contentLoaderInfo.addEventListener(Event.COMPLETE, this.onComplete);
        this.loader.contentLoaderInfo.addEventListener(ProgressEvent.PROGRESS, this.onLoaderProgress);
        this.loader.contentLoaderInfo.addEventListener(IOErrorEvent.IO_ERROR, this.onLoadError);
        this.loader.contentLoaderInfo.addEventListener(SecurityErrorEvent.SECURITY_ERROR, this.onLoadError);
        this.loader.contentLoaderInfo.uncaughtErrorEvents.addEventListener(UncaughtErrorEvent.UNCAUGHT_ERROR, this.onGameUncaught);
        this.loader.load(new URLRequest(this.sURL + 'gamefiles/' + this.sFile));
    }

    private function onLoaderInit(event:Event):void {
        this.report('game loader init bytes=' + this.loader.contentLoaderInfo.bytesLoaded + '/' + this.loader.contentLoaderInfo.bytesTotal);
    }

    private function onLoaderProgress(event:ProgressEvent):void {
        this.report('game loader progress ' + event.bytesLoaded + '/' + event.bytesTotal);
    }

    private function onComplete(event:Event):void {
        this.loader.contentLoaderInfo.removeEventListener(Event.COMPLETE, this.onComplete);
        this.report('game loader complete bytes=' + this.loader.contentLoaderInfo.bytesLoaded + '/' + this.loader.contentLoaderInfo.bytesTotal);

        try {
            this.stg = stage;
            this.report('stage children before remove ' + this.stg.numChildren);
            this.stg.removeChildAt(0);
            this.game = this.stg.addChild(this.loader.content);
            this.report('game content added ' + getQualifiedClassName(this.game));
            this.stg.scaleMode = StageScaleMode.SHOW_ALL;
            this.stg.align = StageAlign.TOP;

            this.report('game params present ' + (this.game.params != null));
            for (var param:String in root.loaderInfo.parameters) {
                this.game.params[param] = root.loaderInfo.parameters[param];
            }

            this.game.params.vars = this.vars;
            this.game.params.sURL = this.sURL;
            this.game.params.sBG = this.sBG;
            this.game.params.sTitle = this.sTitle;
            this.game.params.isEU = this.isEU;
            this.game.params.loginURL = this.loginURL;
            this.game.params.isWeb = false;
            this.game.params.doSignup = false;
            this.game.params.test = false;

            this.game.addEventListener(MouseEvent.CLICK,this.onGameClick);
            this.report('game sfc present ' + (this.game.sfc != null));
            this.game.sfc.addEventListener(SFSEvent.onExtensionResponse, this.onExtensionResponse);
            this.gameDomain = LoaderInfo(event.target).applicationDomain;

            Modules.init();
            this.stg.addEventListener(Event.ENTER_FRAME, Modules.handleFrame);
            this.stg.addEventListener(Event.ENTER_FRAME, this.monitorLoginScreen);

            this.game.stage.addEventListener(KeyboardEvent.KEY_DOWN, this.key_StageGame);

            if (this.customBackgroundURL && this.customBackgroundURL.length > 0) {
                this.initCustomBackground();
            }

            Auras.initialize();
            this.report('game loaded event emitting');
            this.external.call('loaded');
        } catch (error:Error) {
            this.report('onComplete error ' + error.name + ': ' + error.message + '\n' + error.getStackTrace());
            throw error;
        }
    }

    private function onLoadError(event:ErrorEvent):void {
        this.report('load error ' + event.type + ': ' + event.text);
    }

    private function onGameUncaught(event:UncaughtErrorEvent):void {
        this.report('game uncaught ' + event.error);
    }

    private function report(message:String):void {
        try {
            if (this.external) {
                this.external.debug('[skua-main] ' + message);
            }
        } catch (error:Error) {
        }
    }

    public function onExtensionResponse(packet:*):void {
        this.external.call('pext', JSON.stringify(packet));
    }

    private function onGameClick(event:MouseEvent) : void
    {
        if (event == null)
                return;
        var className:String = getQualifiedClassName(event.target.parent);
        switch(event.target.name)
        {
            case "btCharPage":
                this.external.call("openWebsite","https://account.aq.com/CharPage?id=" + event.target.parent.txtUserName.text);
                return;
            case "btnWiki":
                if (event.target.parent.parent.parent.name == "qRewardPrev") {
                    this.external.call("openWebsite", "https://aqwwiki.wikidot.com/" + instance.game.ui.getChildByName("qRewardPrev").cnt.strTitle.text);
                } else if (className.indexOf("LPFFrameItemPreview") > -1) {
                    this.external.call("openWebsite","https://aqwwiki.wikidot.com/" + event.target.parent.tInfo.getLineText(0));
                } else if (className.indexOf("LPFFrameHousePreview") > -1) {
                    this.external.call("openWebsite","https://aqwwiki.wikidot.com/" + instance.game.ui.mcPopup.getChildByName("mcInventory").previewPanel.frames[3].mc.tInfo.getLineText(0));
                } else if (className.indexOf("mcQFrame") > -1) {
                    this.external.call("openWebsite","https://cse.google.com/cse?oe=utf8&ie=utf8&source=uds&safe=active&sort=&cx=015511893259151479029:wctfduricyy&start=0#gsc.tab=0&gsc.q=" + instance.game.getInstanceFromModalStack("QFrameMC").qData.sName);
                }
                return;
            case "hit":
                if (className.indexOf("cProto") > -1 && event.target.parent.ti.text.toLowerCase() == "wiki monster") {
                    this.external.call("openWebsite", "https://aqwwiki.wikidot.com/" + instance.game.world.myAvatar.target.objData.strMonName || "monsters");
                }
                return;
            default:
                return;
        }
    }

    private function monitorLoginScreen(event:Event):void {
        if (!this.customBGReady || !this.game || !this.game.mcLogin) return;
        
        if (this.game.mcLogin.visible && this.game.mcLogin.mcTitle) {
            var hasCustomBG:Boolean = false;
            var numChildren:int = this.game.mcLogin.mcTitle.numChildren;
            
            for (var i:int = 0; i < numChildren; i++) {
                if (this.game.mcLogin.mcTitle.getChildAt(i) == this.customBGReady) {
                    hasCustomBG = true;
                    break;
                }
            }
            
            if (!hasCustomBG) {
                if (this.customBGReady.parent) {
                    this.customBGReady.parent.removeChild(this.customBGReady);
                }
                while (this.game.mcLogin.mcTitle.numChildren > 0) {
                    this.game.mcLogin.mcTitle.removeChildAt(0);
                }
                this.game.mcLogin.mcTitle.addChild(this.customBGReady);
            }
        }
    }

    private function initCustomBackground():void {
        if (!this.customBackgroundURL) {
            return;
        }

        this.customBGLoader = new Loader();
        this.customBGLoader.contentLoaderInfo.addEventListener(Event.COMPLETE, function (e:Event):void {
            customBGReady = MovieClip(customBGLoader.content);

            var checkTimer:Timer = new Timer(100);
            checkTimer.addEventListener(TimerEvent.TIMER, function (timerEvent:TimerEvent):void {
                if (game) {
                    while (game.mcLogin.mcTitle.numChildren > 0) {
                        game.mcLogin.mcTitle.removeChildAt(0);
                    }
                    game.mcLogin.mcTitle.addChild(customBGReady);
                    checkTimer.stop();
                }
            });
            checkTimer.start();
        });
        this.customBGLoader.load(new URLRequest(this.customBackgroundURL));
        
        var lagKillerLoader:Loader = new Loader();
        lagKillerLoader.contentLoaderInfo.addEventListener(Event.COMPLETE, function (e:Event):void {
            customBGLagKiller = MovieClip(lagKillerLoader.content);
            if (game) {
                game.addChildAt(customBGLagKiller, 0);
                customBGLagKiller.visible = false;
            }
        });
        lagKillerLoader.load(new URLRequest(this.customBackgroundURL));
    }

    public function key_StageGame(kbArgs:KeyboardEvent):void {
        if (!(kbArgs.target is TextField || kbArgs.currentTarget is TextField)) {
            if (kbArgs.keyCode == this.game.litePreference.data.keys['Bank']) {
                if (this.game.stage.focus == null || (this.game.stage.focus != null && !('text' in this.game.stage.focus))) {
                    this.game.world.toggleBank();
                }
            }
        }
    }

    public function getGame():* {
        return this.game;
    }

    public function getExternal():Externalizer {
        return this.external;
    }

    public static function getGameObject(path:String):String {
        var obj:* = _getObjectS(instance.game, path);
        return JSON.stringify(obj);
    }


    private static function getProperties(obj:*):String {
        var p:*;
        var res:String = '';
        var val:String;
        var prop:String;
        for (p in obj) {
            prop = String(p);
            if (prop && prop !== '' && prop !== ' ') {
                val = String(obj[p]);
                res += prop + ': ' + val + ', ';
            }
        }
        res = res.substr(0, res.length - 2);
        return res;
    }

    public static function getGameObjectS(path:String):String {
        if (_gameClass == null) {
            _gameClass = instance.gameDomain.getDefinition(getQualifiedClassName(instance.game)) as Class;
        }
        var obj:* = _getObjectS(_gameClass, path);
        return JSON.stringify(obj);
    }

    public static function getGameObjectKey(path:String, key:String):String {
        var obj:* = _getObjectS(instance.game, path);
        var obj2:* = obj[key];
        return (JSON.stringify(obj2));
    }

    public static function setGameObject(path:String, value:*):void {
        var parts:Array = path.split('.');
        var varName:String = parts.pop();
        var obj:* = _getObjectA(instance.game, parts);
        obj[varName] = value;
    }

    public static function setGameObjectKey(path:String, key:String, value:*):void {
        var parts:Array = path.split('.');
        var obj:* = _getObjectA(instance.game, parts);
        obj[key] = value;
    }

    public static function getArrayObject(path:String, index:int):String {
        var obj:* = _getObjectS(instance.game, path);
        return JSON.stringify(obj[index]);
    }

    public static function setArrayObject(path:String, index:int, value:*):void {
        var obj:* = _getObjectS(instance.game, path);
        obj[index] = value;
    }

    public static function callGameFunction(path:String, ...args):String {
        try {
            var parts:Array = path.split('.');
            var funcName:String = parts.pop();
            var obj:* = _getObjectA(instance.game, parts);
            var func:Function = obj[funcName] as Function;
            return JSON.stringify(func.apply(obj, args));
        } catch (error:Error) {
            instance.external.debug('callGameFunction error path=' + path + ' ' + error.name + ': ' + error.message);
            throw error;
        }
        return "null";
    }

    public static function callGameFunction0(path:String):String {
        try {
            var parts:Array = path.split('.');
            var funcName:String = parts.pop();
            var obj:* = _getObjectA(instance.game, parts);
            var func:Function = obj[funcName] as Function;
            return JSON.stringify(func.apply(obj));
        } catch (error:Error) {
            instance.external.debug('callGameFunction0 error path=' + path + ' ' + error.name + ': ' + error.message);
            throw error;
        }
        return "null";
    }

    public static function selectArrayObjects(path:String, selector:String):String {
        var obj:* = _getObjectS(instance.game, path);
        if (!(obj is Array)) {
            instance.external.debug('selectArrayObjects target is not an array');
            return '';
        }
        var array:Array = obj as Array;
        var nArray:Array = [];
        for (var j:int = 0; j < array.length; j++) {
            nArray.push(_getObjectS(array[j], selector));
        }
        return JSON.stringify(nArray);
    }

    public static function _getObjectS(root:*, path:String):* {
        return _getObjectA(root, path.split('.'));
    }

    public static function _getObjectA(root:*, parts:Array):* {
        var obj:* = root;
        for (var i:int = 0; i < parts.length; i++) {
            obj = obj[parts[i]];
        }
        return obj;
    }

    public static function isNull(path:String):String {
        try {
            return (_getObjectS(instance.game, path) == null).toString();
        } catch (ex:Error) {
        }
        return 'true';
    }

    public static function isTrue():String {
        return true.toString();
    }

    public static function injectScript(uri:String):void {
        var ploader:Loader = new Loader();
        ploader.contentLoaderInfo.addEventListener(Event.COMPLETE, onScriptLoaded);
        var context:LoaderContext = new LoaderContext();
        context.allowCodeImport = true;
        ploader.load(new URLRequest(uri), context);
    }

    private static function onScriptLoaded(event:Event):void {
        try {
            var obj:* = LoaderInfo(event.target).loader.content;
            obj.run(instance);
        } catch (ex:Error) {
            instance.external.debug('Error while running injection: ' + ex);
        }
    }

    public static function catchPackets():void {
        instance.game.sfc.addEventListener(SFSEvent.onDebugMessage, packetReceived);
    }

    public static function sendPacket(packet:String):void {
        instance.game.sfc.sendString(packet);
    }

    public static function sendClientPacket(packet:String, type:String):void {
        if (_handler == null) {
            var cls:Class = Class(instance.gameDomain.getDefinition('it.gotoandplay.smartfoxserver.handlers.ExtHandler'));
            _handler = new cls(instance.game.sfc);
        }
        switch (type) {
            case 'xml':
                xmlReceived(packet);
                break;
            case 'json':
                jsonReceived(packet);
                break;
            case 'str':
                strReceived(packet);
                break;
            default:
                instance.external.debug('Invalid packet type.');
        }
    }

    public static function xmlReceived(packet:String):void {
        _handler.handleMessage(new XML(packet), 'xml');
    }

    public static function jsonReceived(packet:String):void {
        _handler.handleMessage(JSON.parse(packet)['b'], 'json');
    }

    public static function strReceived(packet:String):void {
        var array:Array = packet.substr(1, packet.length - 2).split('%');
        _handler.handleMessage(array.splice(1, array.length - 1), 'str');
    }

    public static function packetReceived(packet:*):void {
        if (packet.params.message.indexOf('%xt%zm%') > -1) {
            instance.external.call('packet', packet.params.message.split(':', 2)[1].trim());
        } else {
            instance.external.call("packetFromServer",processPacket(packet.params.message));
        }
    }

    private static function processPacket(param1:String) : String
    {
        var _loc2_:int = 0;
        param1 = param1.replace("[Sending - STR]: ", "");
        param1 = param1.replace("[ RECEIVED ]: ", "");
        param1 = param1.replace("[Sending]: ", "");
        if(param1.indexOf(", (len: ") > -1)
        {
            _loc2_ = param1.indexOf(", (len: ");
            param1 = param1.slice(0,_loc2_);
        }
        return param1;
    }
}
}
