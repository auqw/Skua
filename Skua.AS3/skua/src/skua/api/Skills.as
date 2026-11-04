package skua.api {

import skua.Main;

public class Skills {

    public function Skills() {
        super();
    }

    /**
     * Milliseconds until the game's own time check (World.actionTimeCheck) lets the skill through: the global
     * cooldown and the skill's own cooldown with haste, whichever ends later. Reading it changes nothing.
     */
    private static function timeLeft(skill:*):Number {
        var world:* = Main.instance.game.world;
        var currentTime:Number = new Date().getTime();
        var hasteMultiplier:Number = 1 - Math.min(Math.max(world.myAvatar.dataLeaf.sta.$tha, -1), 0.5);
        var cooldown:Number = skill.OldCD != null ? skill.OldCD : skill.cd;
        var own:Number = Math.round(cooldown * hasteMultiplier) - (currentTime - skill.ts);
        var global:Number = world.GCD - (currentTime - world.GCDTS);
        var left:Number = Math.max(0, own, global);
        return isNaN(left) ? 0 : left;
    }

    private static function actionTimeCheck(skill:*):Boolean {
        if (timeLeft(skill) > 0) {
            return false;
        }
        delete skill.OldCD;
        return true;
    }

    /** What else World.testAction refuses before it uses a skill: the target, mana, the potion slot and stuns. */
    private static function canAct(world:*, skill:*):Boolean {
        var leaf:* = world.myAvatar.dataLeaf;
        // Self ("s") and friendly ("f") skills need no enemy target.
        if (skill.tgt != "s" && skill.tgt != "f") {
            var target:* = world.myAvatar.target;
            if (target == null || target.dataLeaf == null || target.dataLeaf.intHP <= 0) {
                return false;
            }
        }
        if (skill.mp != null && Math.round(skill.mp * leaf.sta["$cmc"]) > leaf.intMP) {
            return false;
        }
        if (skill.ref == "i1" && (skill.sArg1 == "" || (world.lockdownPots && new Date().getTime() - world.lockdownPotsTS <= 5000))) {
            return false;
        }
        for each (var aura:* in leaf.auras) {
            var category:String = aura != null && aura.cat != null ? String(aura.cat).replace(/\s+/g, "").toLowerCase() : "";
            if (category == "stun" || category == "stone" || category == "paralyze" || category == "disable" || category == "disabled") {
                return false;
            }
        }
        return true;
    }

    public static function canUseSkill(index:int):String {
        var world:* = Main.instance.game.world;
        var skill:* = world.actions.active[index];
        return (skill != null && skill.isOK && !skill.skillLock && !skill.lock && timeLeft(skill) == 0 && canAct(world, skill)).toString();
    }

    /** Milliseconds until the skill's cooldown (its own or the global one) has ended; 0 when it has. */
    public static function skillCooldownRemaining(index:int):String {
        var skill:* = Main.instance.game.world.actions.active[index];
        return (skill != null ? timeLeft(skill) : 0).toString();
    }

    /** Milliseconds until the global cooldown that follows every skill used has ended; 0 when it has. */
    public static function globalCooldownRemaining():String {
        var world:* = Main.instance.game.world;
        var left:Number = Math.max(0, world.GCD - (new Date().getTime() - world.GCDTS));
        return (isNaN(left) ? 0 : left).toString();
    }

    public static function useSkill(index:int):String {
        var skill:* = Main.instance.game.world.actions.active[index];
        if (skill != null && actionTimeCheck(skill)) {
            Main.instance.game.world.testAction(skill);
            return true.toString();
        }

        return false.toString();
    }
}
}
