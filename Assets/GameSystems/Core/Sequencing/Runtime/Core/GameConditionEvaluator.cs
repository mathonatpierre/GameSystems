using System;
namespace GameSystems.Actions
{
    public static class GameConditionEvaluator
    {
        public static bool Evaluate(GameCondition[] conditions, GameConditionMode mode, in GameActionContext context)
        {
            conditions ??= Array.Empty<GameCondition>(); if (conditions.Length == 0) return true;
            return mode switch { GameConditionMode.All => All(conditions, context), GameConditionMode.Any => Any(conditions, context), GameConditionMode.None => !Any(conditions, context), GameConditionMode.Not => conditions[0] == null || !conditions[0].Evaluate(context), _ => false };
        }
        static bool All(GameCondition[] items, in GameActionContext context) { for (int i=0;i<items.Length;i++) if (items[i]!=null && !items[i].Evaluate(context)) return false; return true; }
        static bool Any(GameCondition[] items, in GameActionContext context) { for (int i=0;i<items.Length;i++) if (items[i]!=null && items[i].Evaluate(context)) return true; return false; }
    }
}
