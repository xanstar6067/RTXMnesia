using RTXMnesia.Dynamics;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Props
{
    /// <summary>
    /// cLuxProp_EmotionStone (LuxProp_EmotionStone.cpp): interaction starts the emotion flash with the instance's
    /// text and sound; the focus text is the parsed description (DescCat / DescEntry).
    /// </summary>
    public sealed class PropEmotionStone : HplProp
    {
        string _descCat = "", _descEntry = "", _textCat = "", _textEntry = "", _sound = "";

        public PropEmotionStone(AmnesiaEntity entity) : base(entity, HplEntityType.Prop) { }

        protected override void LoadInstanceVariables()
        {
            _descCat = InstanceString("DescCat", "");
            _descEntry = InstanceString("DescEntry", "");
            _textCat = InstanceString("TextCat", "");
            _textEntry = InstanceString("TextEntry", "");
            _sound = InstanceString("Sound", "");
        }

        public override bool CanInteract(HplPhysicsBody body) => true;

        public override bool OnInteract(HplPhysicsBody body, Vector3 hplFocusPos)
        {
            Map.effects.EmotionFlash.Start(Map.hud.Translate(_textCat, _textEntry), _sound);
            return true;
        }

        public override FocusCrosshair GetFocusCrosshair(HplPhysicsBody body, Vector3 hplFocusPos) => FocusCrosshair.Grab;

        public override string GetFocusText() =>
            _descCat == "" || _descEntry == "" ? "" : Map.hud.ParseString(Map.hud.Translate(_descCat, _descEntry));
    }
}
