package skua.module
{
import flash.display.Sprite;
import flash.events.MouseEvent;
import flash.text.TextField;
import skua.Main;

public class QuestRequirementWiki extends Module
{
    public function QuestRequirementWiki()
    {
        super("QuestRequirementWiki");
        enabled = true;
    }

    override public function onFrame(game:*):void
    {
        var modalStack:* = game.ui.ModalStack;
        if (!modalStack || modalStack.numChildren == 0) return;

        var frame:* = modalStack.getChildAt(0);
        if (!frame || !frame.cnt || !frame.cnt.core) return;

        var req:TextField = frame.cnt.core.strReq;
        if (!req || req.text.length == 0) return;

        if (req.parent.getChildByName("skuaWikiMulti")) return;

        var container:Sprite = new Sprite();
        container.name = "skuaWikiMulti";
        req.parent.addChild(container);

        var lineHeight:Number = 18;

        for (var i:int = 0; i < req.numLines; i++)
        {
            var raw:String = req.getLineText(i);
            if (!raw) continue;
            raw = raw.replace(/\s+/g, " ");

            var wikiPart:String = raw.replace(/\d+\/\d+/g, "");
            wikiPart = wikiPart.replace(/,$/, "");
            wikiPart = wikiPart.replace(/\s+/g, " ");
            wikiPart = wikiPart.replace(/^\s+|\s+$/g, "");

            if (wikiPart.length < 2) continue;

            var textField:TextField = new TextField();
            textField.text = wikiPart;
            var hit:Sprite = new Sprite();

            hit.graphics.beginFill(0x000000, 0);

            hit.graphics.drawRect(
                    req.x,
                    req.y + (i * lineHeight),
                    req.width,
                    lineHeight
            );

            hit.graphics.endFill();

            hit.buttonMode = true;
            hit.mouseEnabled = true;

            (function(url:String):void
            {
                hit.addEventListener(MouseEvent.CLICK, function(e:MouseEvent):void
                {
                    Main.instance.external.call("openWebsite", url);
                });
            })("https://aqwwiki.wikidot.com/" + textField.text);

            container.addChild(hit);
        }
    }
}
}