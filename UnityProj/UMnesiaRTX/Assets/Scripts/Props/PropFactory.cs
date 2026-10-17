using RTXMnesia.ScriptApi;
using RTXMnesia.World;

namespace RTXMnesia.Props
{
    /// <summary>
    /// The Lux entity loaders by .ent EntityType (LuxBase.cpp: AddPropLoader / AddEnemyLoader). StaticProp and plain
    /// models are not script entities in HPL2 (null).
    /// </summary>
    public static class PropFactory
    {
        public static HplProp Create(AmnesiaEntity entity)
        {
            if (entity == null) return null;
            switch (entity.entityType)
            {
                case "Object": return new PropObject(entity);
                case "SwingDoor": return new PropSwingDoor(entity);
                case "Lamp": return new PropLamp(entity);
                case "Item": return new PropItem(entity);
                case "LevelDoor": return new PropLevelDoor(entity);
                case "Lever": return new PropLever(entity);
                case "Wheel": return new PropWheel(entity);
                case "MoveObject": return new PropMoveObject(entity);
                case "NPC": return new PropNPC(entity);
                case "OilBarrel": return new PropOilBarrel(entity);
                case "EmotionStone": return new PropEmotionStone(entity);
                case "MultiSlider": return new PropMultiSlider(entity);
                case "Photocell": case "Chest": case "Button":
                    return new PropGeneric(entity, HplEntityType.Prop);
                case "CritterBug": return new PropCritterBug(entity);
                case "CritterSpider": return new PropCritterSpider(entity);
                case "Enemy_ManPig": // the grunt and the water lurker are made by HplMap
                    return new PropGeneric(entity, HplEntityType.Enemy);
                case "CommentaryIcon":
                    return new PropGeneric(entity, HplEntityType.CommentaryIcon);
                default:
                    return null;
            }
        }
    }
}
