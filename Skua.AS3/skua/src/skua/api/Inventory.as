package skua.api {

import skua.Main;

public class Inventory {

    private static function categories():* {
        var domain:* = Main.instance.game.loaderInfo.applicationDomain;
        return domain.hasDefinition("InvCat") ? domain.getDefinition("InvCat") : null;
    }

    public static function hasInventoryCategories():String {
        return (categories() != null).toString();
    }

    public static function inventoryBagUsedSlots():int {
        var cat:* = categories();
        var items:Array = Main.instance.game.world.myAvatar.items;
        return cat == null ? items.length : cat.countBag(items);
    }

    public static function inventoryMiscSlots():int {
        var cat:* = categories();
        if (cat == null) return 0;
        return "MISC_SLOTS" in cat
            ? int(cat.MISC_SLOTS)
            : int(Main.instance.game.world.myAvatar.objData.iBagSlots);
    }

    public static function inventoryMiscUsedSlots():int {
        var cat:* = categories();
        return cat == null ? 0 : cat.countMisc(Main.instance.game.world.myAvatar.items);
    }

    public static function isFavoriteItem(itemID:int):String {
        var domain:* = Main.instance.game.loaderInfo.applicationDomain;
        if (domain == null || !domain.hasDefinition("liteAssets.draw.FavStore")) {
            return false.toString();
        }
        var store:* = domain.getDefinition("liteAssets.draw.FavStore");
        return Boolean(store.has(itemID)).toString();
    }

    public static function inventoryPool(itemJson:String):String {
        var cat:* = categories();
        return cat == null ? "bag" : cat.poolOf(JSON.parse(itemJson));
    }

    // Quantity is the amount purchased, rather than the desired final stack.
    public static function inventoryHasSpaceFor(itemJson:String, quantity:int = 1, purchase:Boolean = false, outgoingJson:String = null):String {
        var world:* = Main.instance.game.world;
        var item:Object = JSON.parse(itemJson);
        var cat:* = categories();
        var owned:*;
        if (cat == null) {
            for each (owned in world.myAvatar.items) {
                if (int(owned.ItemID) == int(item.ItemID) && int(owned.iQty) < int(owned.iStk)) return "true";
            }
            return (outgoingJson != null || world.myAvatar.items.length < int(world.myAvatar.objData.iBagSlots)).toString();
        }
        if (cat.isHouse(item)) {
            return (world.myAvatar.houseitems.length < int(world.myAvatar.objData.iHouseSlots)).toString();
        }
        var freed:int = purchase ? cat.freedBy(world.myAvatar.items, item, quantity) : 0;
        if (outgoingJson != null && cat.poolOf(JSON.parse(outgoingJson)) == cat.poolOf(item)) freed++;
        return (!cat.isFullFor(world.myAvatar.items, item, int(world.myAvatar.objData.iBagSlots), freed)).toString();
    }
}
}
