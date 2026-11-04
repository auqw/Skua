package skua.api {

import flash.events.TimerEvent;
import flash.utils.Timer;
import flash.utils.getTimer;

import skua.Main;

public class Auras {
    private static var _cleanupTimer:Timer = null;
    private static var _initTimer:Timer = null;
    private static var _initialized:Boolean = false;

    public function Auras() {
        super();
    }

    public static function initialize():void {
        if (_initialized) {
            return;
        }
        
        _initTimer = new Timer(500);
        _initTimer.addEventListener(TimerEvent.TIMER, checkGameReady);
        _initTimer.start();
    }

    private static function checkGameReady(event:TimerEvent):void {
        try {
            var world:* = Main.instance.game.world;
            if (world && world.uoTree && world.monTree) {
                _initTimer.stop();
                _initTimer.removeEventListener(TimerEvent.TIMER, checkGameReady);
                _initTimer = null;
                _initialized = true;
                Main.instance.external.debug("World load check passed, initializing Auras API.");
                
                startAuraAutoCleanup(5);
            }
        } catch (e:Error) {
        }
    }

    public static function GetAuraSnapshots(subject:String):String {
        var world:* = Main.instance.game.world;
        var leaf:* = subject == "Self"
            ? world.uoTree[Main.instance.game.sfc.myUserName.toLowerCase()]
            : (world.myAvatar.target != null ? world.myAvatar.target.dataLeaf : null);
        var snapshots:Array = [];
        if (leaf == null || leaf.hudAuras == null) {
            return JSON.stringify(snapshots);
        }
        var now:int = getTimer();
        for (var name:String in leaf.hudAuras) {
            var aura:* = leaf.hudAuras[name];
            if (aura == null) {
                continue;
            }
            snapshots.push({
                nam: name,
                n: int(aura.n),
                dur: Number(aura.dur),
                remaining: Number(aura.end) > 0 ? Math.max(0, (Number(aura.end) - now) / 1000) : 0,
                persist: aura.persist == true,
                icon: aura.icon != null ? String(aura.icon) : "",
                desc: world.auraDescs != null && world.auraDescs[name] != null ? String(world.auraDescs[name]) : "",
                seq: int(aura.seq)
            });
        }
        snapshots.sortOn("seq", Array.NUMERIC);
        return JSON.stringify(snapshots);
    }

    /**
     * The amount Advanced Skills aura rules compare: the HUD stack count or the summed aura values, whichever is
     * higher. Both are read from the same leaf in one call, so they describe the same moment and the same target.
     */
    public static function GetAuraRuleStacks(subject:String, auraName:String):String {
        var world:* = Main.instance.game.world;
        var leaf:* = subject == "Self"
            ? world.uoTree[Main.instance.game.sfc.myUserName.toLowerCase()]
            : (world.myAvatar.target != null ? world.myAvatar.target.dataLeaf : null);
        if (leaf == null || auraName == null) {
            return "0";
        }
        var key:String = auraName.toLowerCase();
        var hud:Number = 0;
        if (leaf.hudAuras != null) {
            for (var hudName:String in leaf.hudAuras) {
                if (hudName.toLowerCase() == key && leaf.hudAuras[hudName] != null) {
                    hud = Math.max(hud, int(leaf.hudAuras[hudName].n));
                }
            }
        }
        // As rebuildAuraArray does: skip expired entries, and count an aura without a value as 1.
        var values:Number = 0;
        if (leaf.auras != null) {
            for each (var aura:* in leaf.auras) {
                if (aura == null || !aura.nam || aura.e == 1 || String(aura.nam).toLowerCase() != key) {
                    continue;
                }
                var value:Number = Number(aura.val);
                values += aura.val == null || isNaN(value) ? 1 : value;
            }
        }
        return Math.max(hud, values).toString();
    }

    public static function getSubjectAuras(subject:String):Array {
        if (subject == 'Self')
        {
            var userObj:* = Main.instance.game.world.uoTree[Main.instance.game.sfc.myUserName.toLowerCase()];
            return rebuildAuraArray(userObj.auras)
        }
        else
        {
            var monID:int = 0;
            if (Main.instance.game.world.myAvatar.target != null) {
                monID = Main.instance.game.world.myAvatar.target.dataLeaf.MonMapID;
            }
            var monObj:* = Main.instance.game.world.monTree[monID];
            return rebuildAuraArray(monObj.auras)
        }
    }

    public static function  rebuildAuraArray(auras:Object):Array {
        var rebuiltAuras:Array = [];
        if (!auras) {
            return rebuiltAuras;
        }
        
        for (var i:int = 0; i < auras.length; i++) {
            var aura:Object = auras[i];
            
            if (!aura) {
                continue;
            }
            
            if (!aura.hasOwnProperty("nam") || !aura.nam) {
                continue;
            }
            
            if (aura.e == 1) {
                continue;
            }
            
            var rebuiltAura:Object = {};
            var hasVal:Boolean = false;
            
            for (var key:String in aura) {
                if (key == "cLeaf") {
                    rebuiltAura[key] = "cycle_";
                } else if (key == "val") {
                    var rawVal:* = aura[key];
                    if (rawVal == null || isNaN(rawVal)) {
                        rebuiltAura[key] = 1;
                    } else {
                        rebuiltAura[key] = rawVal;
                    }
                    hasVal = true;
                } else {
                    rebuiltAura[key] = aura[key];
                }
            }
            if (!hasVal) {
                rebuiltAura.val = 1;
            }
            
            rebuiltAuras.push(rebuiltAura);
        }
        
        return rebuiltAuras;
    }

    public static function rebuilduoTree(playerName:String):Object {
        var plrUser:String = playerName.toLowerCase();
        var userObj:* = Main.instance.game.world.uoTree[plrUser];
        if (!userObj) {
            return {};
        }

        var rebuiltObj:Object = {};
        for (var prop:String in userObj) {
            if (prop == "auras") {
                rebuiltObj[prop] = rebuildAuraArray(userObj.auras);
            } else {
                rebuiltObj[prop] = userObj[prop];
            }
        }

        return rebuiltObj;
    }

    public static function rebuildmonTree(monID:int):Object {
        var monObj:* = Main.instance.game.world.monTree[monID];
        if (!monObj) {
            return {};
        }
        var rebuiltObj:Object = {};
        for (var prop:String in monObj) {
            if (prop == "auras") {
                rebuiltObj[prop] = rebuildAuraArray(monObj.auras);
            } else {
                rebuiltObj[prop] = monObj[prop];
            }
        }
        return rebuiltObj;
    }

    public static function HasAnyActiveAura(subject:String, auraNames:String):String {
        var auraList:Array = auraNames.split(',');
        var auras:Object = null;
        try {
            auras = getSubjectAuras(subject);
        } catch (e:Error) {
            return false.toString();
        }

        var auraCount:int = auras.length;
        var auraListCount:int = auraList.length;
        
        for (var i:int = 0; i < auraCount; i++) {
            var aura:Object = auras[i];
            var auraNameLower:String = aura.nam.toLowerCase();
            for (var j:int = 0; j < auraListCount; j++) {
                if (auraNameLower == auraList[j].toLowerCase().replace(/^\s+|\s+$/g, "")) {
                    return true.toString();
                }
            }
        }
        return false.toString();
    }

    public static function GetAurasValue(subject:String, auraName:String):Number {
        var aura:Object = null;
        var auras:Array = null;
        try {
            auras = getSubjectAuras(subject);
        } catch (e:Error) {
            return 442;
        }

        var lowerAuraName:String = auraName.toLowerCase();
        for (var i:int = 0; i < auras.length; i++) {
            aura = auras[i];
            if (aura.nam.toLowerCase() == lowerAuraName) {
                return aura.val;
            }
        }
        return 444;
    }

    public static function GetPlayerAura(playerName:String):String {
        var plrUser:String = playerName.toLowerCase();
        try {
            var userObj:* = Main.instance.game.world.uoTree[plrUser];
            if (!userObj) {
                return '[]';
            }
            return JSON.stringify(rebuildAuraArray(userObj.auras))
        }
        catch (e:Error) {
        }
        return '[]';
    }

    public static function GetMonsterAuraByName(monsterName:String):String {

        try {
            var monID:int = 0;
            var lowerMonsterName:String = monsterName.toLowerCase();
            for each (var monster:* in Main.instance.game.world.monsters)
            {
                if (monster && monster.objData.strMonName.toLowerCase() == lowerMonsterName) {
                    monID = monster.objData.MonMapID
                }
            }
            var monObj:* = Main.instance.game.world.monTree[monID];
            if (!monObj) {
                return 'Error: Couldn\'t get Monster Object Tree';
            }
            return JSON.stringify(rebuildAuraArray(monObj.auras));
        }
        catch (e:Error) {
        }
        return '[]';
    }

    public static function GetMonsterAuraByID(monID:int):String {
        try {
            var monObj:* = Main.instance.game.world.monTree[monID];
            if (!monObj) {
                return 'Error: Couldn\'t get Monster Object Tree';
            }
            return JSON.stringify(rebuildAuraArray(monObj.auras));
        }
        catch (e:Error) {
        }
        return '[]';
    }

    private static function startAuraAutoCleanup(intervalSeconds:int = 5):void {
        if (_cleanupTimer != null) {
            return;
        }
        
        _cleanupTimer = new Timer(intervalSeconds * 1000);
        _cleanupTimer.addEventListener(TimerEvent.TIMER, onCleanupTimer);
        _cleanupTimer.start();
    }

    public static function stopAuraAutoCleanup():void {
        if (_cleanupTimer == null) {
            return;
        }
        
        _cleanupTimer.stop();
        _cleanupTimer.removeEventListener(TimerEvent.TIMER, onCleanupTimer);
        _cleanupTimer = null;
    }

    private static function onCleanupTimer(event:TimerEvent):void {
        var world:* = Main.instance.game.world;
        if (!world) {
            return;
        }
        
        var cleanedCount:int = 0;
        
        // Clean player auras in uoTree
        for (var playerName:String in world.uoTree) {
            var userObj:* = world.uoTree[playerName];
            if (userObj && userObj.auras is Array) {
                cleanedCount += cleanExpiredAuras(userObj.auras);
            }
        }
        
        // Clean monster auras in monTree
        for (var monID:String in world.monTree) {
            var monObj:* = world.monTree[monID];
            if (monObj && monObj.auras is Array) {
                cleanedCount += cleanExpiredAuras(monObj.auras);
            }
        }
    }

    private static function cleanExpiredAuras(auras:Array):int {
        var removedCount:int = 0;
        for (var i:int = auras.length - 1; i >= 0; i--) {
            var aura:Object = auras[i];
            if (!aura || !aura.hasOwnProperty("nam") || !aura.nam || aura.e == 1) {
                auras.splice(i, 1);
                removedCount++;
            }
        }
        return removedCount;
    }
}
}
