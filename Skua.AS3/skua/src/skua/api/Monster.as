package skua.api {

import skua.Main;

public class Monster {

    public function Monster() {
        super();
    }

    public static function attackMonsterByID(id:int):String {
        var bestTarget:* = getBestMonsterTargetByID(id);
        return attackTarget(bestTarget);
    }

    public static function attackMonsterByName(name:String):String {
        var bestTarget:* = getBestMonsterTarget(name);
        return attackTarget(bestTarget);
    }

    public static function selectMonsterByID(id:int):String {
        var bestTarget:* = getBestMonsterTargetByID(id);
        return selectTarget(bestTarget);
    }

    public static function selectMonsterByName(name:String):String {
        var bestTarget:* = getBestMonsterTarget(name);
        return selectTarget(bestTarget);
    }

    private static function sortMonstersByHP(a:*, b:*):Number {
        var aHP:int = (a.dataLeaf && a.dataLeaf.intHP) ? a.dataLeaf.intHP : 0;
        var bHP:int = (b.dataLeaf && b.dataLeaf.intHP) ? b.dataLeaf.intHP : 0;

        var aAlive:Boolean = aHP > 0;
        var bAlive:Boolean = bHP > 0;

        if (aAlive != bAlive) {
            return aAlive ? -1 : 1;
        }

        if (aHP != bHP) {
            return aHP - bHP;
        }

        var aMapID:int = a.objData ? a.objData.MonMapID : 0;
        var bMapID:int = b.objData ? b.objData.MonMapID : 0;
        return aMapID - bMapID;
    }

    public static function getBestMonsterTarget(name:String):* {
        var targetCandidates:Array = [];
        var world:* = Main.instance.game.world;
        var lowerName:String = name.toLowerCase();
        var isWildcard:Boolean = name == '*';

        for each (var monster:* in world.getMonstersByCell(world.strFrame)) {
            if (monster.pMC != null) {
                var monName:String = monster.objData.strMonName.toLowerCase();
                if (isWildcard || monName.indexOf(lowerName) > -1) {
                    targetCandidates.push(monster);
                }
            }
        }

        if (targetCandidates.length == 0)
            return null;

        targetCandidates.sort(sortMonstersByHP);
        return targetCandidates[0];
    }

    public static function getBestMonsterTargetByID(id:int):* {
        var targetCandidates:Array = [];
        var world:* = Main.instance.game.world;

        for each (var monster:* in world.getMonstersByCell(world.strFrame)) {
            if (monster.pMC != null && monster.objData && (monster.objData.MonMapID == id || monster.objData.MonID == id)) {
                targetCandidates.push(monster);
            }
        }

        if (targetCandidates.length == 0)
            return null;

        targetCandidates.sort(sortMonstersByHP);
        return targetCandidates[0];
    }

    public static function availableMonstersInCell():String {
        var retMonsters:Array = [];
        var world:* = Main.instance.game.world;
        
        for each (var monster:* in world.getMonstersByCell(world.strFrame)) {
            if (monster.pMC != null) {
                retMonsters.push(getMonData(monster));
            }
        }
        return JSON.stringify(retMonsters);
    }

    public static function getTargetMonster():String {
        var world:* = Main.instance.game.world;
        var monster:* = world.myAvatar.target
        if (!monster || (monster.dataLeaf && monster.dataLeaf.intHP <= 0)) {
            world.cancelTarget();
            return JSON.stringify({});
        }
        return JSON.stringify(getMonData(monster));
    }

    public static function getMonsters():String {
        var retMonsters:Array = [];
        for each (var monster:* in Main.instance.game.world.monsters) {
            retMonsters.push(getMonData(monster));
        }
        return JSON.stringify(retMonsters);
    }

    public static function getMonData(mon:Object):Object
    {
        var monsterData:Object = {};
        for (var prop:String in mon.objData) {
            monsterData[prop] = mon.objData[prop];
        }
        if (mon.dataLeaf) {
            monsterData.intHP = mon.dataLeaf.intHP;
            monsterData.intHPMax = mon.dataLeaf.intHPMax;
            monsterData.intState = mon.dataLeaf.intState;
        }
        return monsterData;
    }

    private static function attackTarget(target:*):String {
        if (target != null && target.pMC != null) {
            Main.instance.game.world.setTarget(target);
            Main.instance.game.world.approachTarget();
            return true.toString();
        }
        return false.toString();
    }

    private static function selectTarget(target:*):String {
        if (target != null && target.pMC != null) {
            Main.instance.game.world.setTarget(target);
            return true.toString();
        }
        return false.toString();
    }
}
}
