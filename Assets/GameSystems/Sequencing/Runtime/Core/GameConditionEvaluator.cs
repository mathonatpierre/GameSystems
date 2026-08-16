using System;
namespace GameSystems.Sequencing
{
    public static class GameConditionEvaluator
    {
        public static bool Evaluate(GameCondition[] conditions, GameConditionMode mode, in GameActionContext context)
        {
            conditions ??= Array.Empty<GameCondition>();
            return mode switch { GameConditionMode.All => All(conditions, context), GameConditionMode.Any => Any(conditions, context), GameConditionMode.None => !Any(conditions, context), GameConditionMode.Not => !TryFirst(conditions, context, out bool result) || !result, _ => false };
        }
        static bool All(GameCondition[] items, in GameActionContext context) { for (int i=0;i<items.Length;i++) if (items[i]?.Enabled == true && !items[i].Evaluate(context)) return false; return true; }
        static bool Any(GameCondition[] items, in GameActionContext context) { for (int i=0;i<items.Length;i++) if (items[i]?.Enabled == true && items[i].Evaluate(context)) return true; return false; }
        static bool TryFirst(GameCondition[] items, in GameActionContext context, out bool result)
        { for (int i=0;i<items.Length;i++) if (items[i]?.Enabled == true) { result = items[i].Evaluate(context); return true; } result = false; return false; }
    }
}
