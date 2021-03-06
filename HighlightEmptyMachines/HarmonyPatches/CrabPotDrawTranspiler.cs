using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using AtraBase.Toolkit;
using AtraBase.Toolkit.Reflection;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using HighlightEmptyMachines.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Objects;

namespace HighlightEmptyMachines.HarmonyPatches;

/// <summary>
/// Hold patches against crab pots.
/// </summary>
[HarmonyPatch(typeof(CrabPot))]
internal class CrabPotDrawTranspiler
{
    [MethodImpl(TKConstants.Hot)]
    private static Color CrabPotNeedsInputColor(CrabPot obj)
        => ModEntry.Config.VanillaMachines[VanillaMachinesEnum.CrabPot]
            && obj.bait.Value is null && obj.heldObject.Value is null ? ModEntry.Config.EmptyColor : Color.White;

#pragma warning disable SA1116 // Split parameters should start on line after declaration. Reviewed
    [HarmonyPatch(nameof(CrabPot.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) })]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Call, typeof(Game1).StaticMethodNamed(nameof(Game1.GlobalToLocal), new[] { typeof(xTile.Dimensions.Rectangle), typeof(Vector2) })),
            })
            .FindNext(new CodeInstructionWrapper[]
            {
                new (OpCodes.Call, typeof(Color).StaticPropertyNamed(nameof(Color.White)).GetGetMethod()),
            })
            .GetLabels(out IList<Label> colorLabels, clear: true)
            .ReplaceInstruction(OpCodes.Call, typeof(CrabPotDrawTranspiler).StaticMethodNamed(nameof(CrabPotDrawTranspiler.CrabPotNeedsInputColor)))
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0),
            }, withLabels: colorLabels);
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling Crabpot.draw:\n\n{ex}", LogLevel.Error);
        }
        return null;
    }
#pragma warning restore SA1116 // Split parameters should start on line after declaration
}