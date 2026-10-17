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
                case "Photocell": case "Chest": case "Button": case "OilBarrel": case "EmotionStone":
                case "MultiSlider": case "CritterBug": case "CritterSpider":
                    return new PropGeneric(entity, HplEntityType.Prop);
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
