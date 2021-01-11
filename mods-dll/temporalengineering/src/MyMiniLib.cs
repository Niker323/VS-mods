
using Vintagestory.API.Common;

public static class MyMiniLib
{
    public static int GetAttributeInt(Block block, string atrname, int def = 0)
    {
        if (block != null && block.Attributes != null && block.Attributes[atrname] != null)
        {
            return block.Attributes[atrname].AsInt();
        }
        return def;
    }
}