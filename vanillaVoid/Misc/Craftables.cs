using RoR2;
using UnityEngine;
using vanillaVoid.Items;

namespace vanillaVoid.Misc
{
    public class Craftables : MonoBehaviour
    {
        //boots + something s hould make seers
        //duplicator should do something with brokens
        //collectors compulsion + something = exhaust?



        public void CreateCraftables()
        {
            var seersIndex = DLC1Content.Items.CritGlassesVoid.itemIndex;
            var encrustedKeyIndex = DLC1Content.Items.TreasureCacheVoid.itemIndex;
            var needletickIndex = DLC1Content.Items.BleedOnHitVoid.itemIndex;
            var weepingIndex = DLC1Content.Items.MushroomVoid.itemIndex;
            var spacesIndex = DLC1Content.Items.BearVoid.itemIndex;

            var shrimpIndex = DLC1Content.Items.MissileVoid.itemIndex;
            var polyluteIndex = DLC1Content.Items.ChainLightningVoid.itemIndex;
            var singuloIndex = DLC1Content.Items.ElementalRingVoid.itemIndex;
            var tentaIndex = DLC1Content.Items.SlowOnHitVoid.itemIndex;
            var voidsentIndex = DLC1Content.Items.ExplodeOnDeathVoid.itemIndex;
            var lysateIndex = DLC1Content.Items.EquipmentMagazineVoid.itemIndex;

            var pluriIndex = DLC1Content.Items.ExtraLifeVoid.itemIndex;
            var pluriConsumedIndex = DLC1Content.Items.ExtraLifeVoidConsumed.itemIndex;
            var benthicIndex = DLC1Content.Items.CloverVoid.itemIndex;

            var zoeaIndex = DLC1Content.Items.VoidMegaCrabItem.itemIndex;

            var adzeIndex = AbyssalAdze.instance.ItemDef.itemIndex;
            var exhaustIndex = ExtraterrestrialExhaust.instance.ItemDef.itemIndex;
            var coolantIndex = CryoCanister.instance.ItemDef.itemIndex;
            var vialsIndex = EnhancementVials.instance.ItemDef.itemIndex;
            var clockworkIndex = ClockworkMechanism.instance.ItemDef.itemIndex;

            var emptyVialsIndex = EmptyVials.instance.ItemDef.itemIndex;
            var consumedClockworkIndex = ConsumedClockworkMechanism.instance.ItemDef.itemIndex;

            var lotusIndex = CrystalLotus.instance.ItemDef.itemIndex;
            var bladeIndex = ExeBlade.instance.ItemDef.itemIndex;
            var clutchIndex = VoidFin.instance.ItemDef.itemIndex;
            var cornuIndex = VoidShell.instance.ItemDef.itemIndex;
            var quillIndex = DashQuill.instance.ItemDef.itemIndex;
            var corrosiveIndex = CorrosiveCore.instance.ItemDef.itemIndex;

            var orreryIndex = LensOrrery.instance.ItemDef.itemIndex;

            var cloverPickupIndex = PickupCatalog.FindPickupIndex(RoR2Content.Items.Clover.itemIndex);
            var crowbarPickupIndex = PickupCatalog.FindPickupIndex(RoR2Content.Items.Crowbar.itemIndex);
            var scopePickupIndex = PickupCatalog.FindPickupIndex(DLC1Content.Items.CritDamage.itemIndex);

            var vialsPickupIndex = PickupCatalog.FindPickupIndex(vialsIndex);
            var encrustedKeyPickupIndex = PickupCatalog.FindPickupIndex(encrustedKeyIndex);
            var benthicPickupIndex = PickupCatalog.FindPickupIndex(benthicIndex);
            var lysatePickupIndex = PickupCatalog.FindPickupIndex(lysateIndex);
            var emptyVialsPickupIndex = PickupCatalog.FindPickupIndex(emptyVialsIndex);


            var adzeCraftableDef = ScriptableObject.CreateInstance<CraftableDef>();
            adzeCraftableDef.itemIndex = adzeIndex;
            adzeCraftableDef.recipes = new Recipe[2];

            adzeCraftableDef.recipes[0] = CreateRecipe(crowbarPickupIndex, vialsPickupIndex); //crowbarVials
            adzeCraftableDef.recipes[1] = CreateRecipe(crowbarPickupIndex, encrustedKeyIndex); //crowbarKey

            var clockworkCraftableDef = ScriptableObject.CreateInstance<CraftableDef>();
            clockworkCraftableDef.itemIndex = clockworkIndex;
            clockworkCraftableDef.recipes = new Recipe[3];

            clockworkCraftableDef.recipes[0] = CreateRecipe(vialsPickupIndex, consumedClockworkIndex);
            clockworkCraftableDef.recipes[1] = CreateRecipe(vialsPickupIndex, DLC1Content.Items.FragileDamageBonus.itemIndex);
            clockworkCraftableDef.recipes[2] = CreateRecipe(emptyVialsPickupIndex, RoR2Content.Equipment.Recycle.equipmentIndex);

            var vialsCraftableDef = ScriptableObject.CreateInstance<CraftableDef>();
            vialsCraftableDef.itemIndex = vialsIndex;
            vialsCraftableDef.recipes = new Recipe[7];

            vialsCraftableDef.recipes[0] = CreateRecipe(encrustedKeyPickupIndex, cloverPickupIndex, 3);
            vialsCraftableDef.recipes[1] = CreateRecipe(benthicPickupIndex, RoR2Content.Items.TreasureCache.itemIndex, 3);
            vialsCraftableDef.recipes[2] = CreateRecipe(encrustedKeyPickupIndex, benthicPickupIndex, 5);
            vialsCraftableDef.recipes[3] = CreateRecipe(emptyVialsPickupIndex, DLC1Content.Items.HealingPotion.itemIndex);
            vialsCraftableDef.recipes[4] = CreateRecipe(emptyVialsPickupIndex, lysatePickupIndex);
            vialsCraftableDef.recipes[5] = CreateRecipe(consumedClockworkIndex, RoR2Content.Equipment.Recycle.equipmentIndex);
            vialsCraftableDef.recipes[6] = CreateRecipe(vialsPickupIndex, DLC1Content.Equipment.GummyClone.equipmentIndex, 3);

            var needletickCraftableDef = ScriptableObject.CreateInstance<CraftableDef>();
            needletickCraftableDef.itemIndex = needletickIndex;
            needletickCraftableDef.recipes = new Recipe[2];

            needletickCraftableDef.recipes[0] = CreateRecipe(vialsPickupIndex, RoR2Content.Items.BleedOnHit.itemIndex);
            needletickCraftableDef.recipes[1] = CreateRecipe(vialsPickupIndex, RoR2Content.Items.BleedOnHitAndExplode.itemIndex, 3);


            var shrimpCraftableDef = ScriptableObject.CreateInstance<CraftableDef>();
            shrimpCraftableDef.itemIndex = shrimpIndex;
            shrimpCraftableDef.recipes = new Recipe[4];

            shrimpCraftableDef.recipes[0] = CreateRecipe(RoR2Content.Items.PersonalShield.itemIndex, exhaustIndex);
            shrimpCraftableDef.recipes[1] = CreateRecipe(DLC3Content.Items.ShieldBooster.itemIndex, exhaustIndex);
            shrimpCraftableDef.recipes[2] = CreateRecipe(vialsPickupIndex, RoR2Content.Items.Missile.itemIndex);
            shrimpCraftableDef.recipes[3] = CreateRecipe(vialsPickupIndex, exhaustIndex);

            var polyluteCraftableDef = ScriptableObject.CreateInstance<CraftableDef>();
            polyluteCraftableDef.itemIndex = polyluteIndex;
            polyluteCraftableDef.recipes = new Recipe[3];

            polyluteCraftableDef.recipes[0] = CreateRecipe(vialsPickupIndex, RoR2Content.Items.ChainLightning.itemIndex);
            polyluteCraftableDef.recipes[1] = CreateRecipe(seersIndex, RoR2Content.Items.ChainLightning.itemIndex);
            polyluteCraftableDef.recipes[2] = CreateRecipe(needletickIndex, RoR2Content.Items.ChainLightning.itemIndex);


            var bandCraftableDef = ScriptableObject.CreateInstance<CraftableDef>();
            bandCraftableDef.itemIndex = singuloIndex;
            bandCraftableDef.recipes = new Recipe[3];

            bandCraftableDef.recipes[0] = CreateRecipe(vialsPickupIndex, RoR2Content.Items.IceRing.itemIndex);
            bandCraftableDef.recipes[1] = CreateRecipe(vialsPickupIndex, RoR2Content.Items.FireRing.itemIndex);
            bandCraftableDef.recipes[2] = CreateRecipe(vialsPickupIndex, RoR2Content.Equipment.Blackhole.equipmentIndex);


            var clutchCraftableDef = ScriptableObject.CreateInstance<CraftableDef>();
            clutchCraftableDef.itemIndex = clutchIndex;
            clutchCraftableDef.recipes = new Recipe[4];

            clutchCraftableDef.recipes[0] = CreateRecipe(quillIndex, DLC2Content.Items.KnockBackHitEnemies.itemIndex);
            clutchCraftableDef.recipes[1] = CreateRecipe(vialsPickupIndex, DLC2Content.Items.KnockBackHitEnemies.itemIndex);
            clutchCraftableDef.recipes[2] = CreateRecipe(weepingIndex, DLC3Content.Items.JumpDamageStrike.itemIndex);
            clutchCraftableDef.recipes[3] = CreateRecipe(quillIndex, DLC3Content.Items.JumpDamageStrike.itemIndex);


            var burdenCraftableDef = ScriptableObject.CreateInstance<CraftableDef>();
            burdenCraftableDef.itemIndex = bladeIndex;
            burdenCraftableDef.recipes = new Recipe[2];

            burdenCraftableDef.recipes[0] = CreateRecipe(RoR2Content.Items.ExecuteLowHealthElite.itemIndex, voidsentIndex);
            burdenCraftableDef.recipes[1] = CreateRecipe(vialsPickupIndex, RoR2Content.Items.ExecuteLowHealthElite.itemIndex);


            var quillCraftableDef = ScriptableObject.CreateInstance<CraftableDef>();
            quillCraftableDef.itemIndex = quillIndex;
            quillCraftableDef.recipes = new Recipe[5];

            quillCraftableDef.recipes[0] = CreateRecipe(vialsPickupIndex, RoR2Content.Items.Feather.itemIndex);
            quillCraftableDef.recipes[1] = CreateRecipe(quillIndex, RoR2Content.Items.JumpBoost.itemIndex, 2);
            quillCraftableDef.recipes[2] = CreateRecipe(weepingIndex, RoR2Content.Items.JumpBoost.itemIndex);
            quillCraftableDef.recipes[3] = CreateRecipe(RoR2Content.Items.Feather.itemIndex, clutchIndex);
            quillCraftableDef.recipes[4] = CreateRecipe(RoR2Content.Items.JumpBoost.itemIndex, clutchIndex);


            var corrosiveCraftableDef = ScriptableObject.CreateInstance<CraftableDef>();
            corrosiveCraftableDef.itemIndex = corrosiveIndex;
            corrosiveCraftableDef.recipes = new Recipe[7];

            var tankIndex = DLC1Content.Items.StrengthenBurn.itemIndex;

            corrosiveCraftableDef.recipes[0] = CreateRecipe(vialsPickupIndex, tankIndex);
            corrosiveCraftableDef.recipes[1] = CreateRecipe(tankIndex, coolantIndex);
            corrosiveCraftableDef.recipes[2] = CreateRecipe(lysatePickupIndex, coolantIndex);
            corrosiveCraftableDef.recipes[3] = CreateRecipe(needletickIndex, tentaIndex);
            corrosiveCraftableDef.recipes[4] = CreateRecipe(RoR2Content.Items.BleedOnHit.itemIndex, tentaIndex);
            corrosiveCraftableDef.recipes[5] = CreateRecipe(lotusIndex, RoR2Content.Items.IgniteOnKill.itemIndex);
            corrosiveCraftableDef.recipes[6] = CreateRecipe(vialsPickupIndex, coolantIndex);

            var benthicCraftableDef = ScriptableObject.CreateInstance<CraftableDef>();
            benthicCraftableDef.itemIndex = benthicIndex;
            benthicCraftableDef.recipes = new Recipe[3];

            benthicCraftableDef.recipes[0] = CreateRecipe(cloverPickupIndex, clockworkIndex);
            benthicCraftableDef.recipes[1] = CreateRecipe(cloverPickupIndex, vialsPickupIndex);
            benthicCraftableDef.recipes[2] = CreateRecipe(DLC1Content.Items.RandomEquipmentTrigger.itemIndex, clockworkIndex);

            var orreryCraftableDef = ScriptableObject.CreateInstance<CraftableDef>();
            orreryCraftableDef.itemIndex = orreryIndex;
            orreryCraftableDef.recipes = new Recipe[3];

            orreryCraftableDef.recipes[0] = CreateRecipe(scopePickupIndex, vialsPickupIndex);
            orreryCraftableDef.recipes[1] = CreateRecipe(scopePickupIndex, seersIndex);
            orreryCraftableDef.recipes[2] = CreateRecipe(scopePickupIndex, shrimpIndex);

            var zoeaCraftableDef = ScriptableObject.CreateInstance<CraftableDef>();
            zoeaCraftableDef.itemIndex = zoeaIndex;
            zoeaCraftableDef.recipes = new Recipe[3];

            var zoeaYellows = new Recipe();
            zoeaYellows.amountToDrop = 1;
            zoeaYellows.ingredients = new RecipeIngredient[2];
            zoeaYellows.ingredients[0].itemTier = ItemTier.Boss;
            zoeaYellows.ingredients[1].pickupIndex = vialsPickupIndex;
            zoeaYellows.ingredients[0].forbiddenTags = new ItemTag[1] { ItemTag.PowerShape };

            zoeaCraftableDef.recipes[0] = CreateRecipe(needletickIndex, pluriIndex);
            zoeaCraftableDef.recipes[1] = CreateRecipe(vialsPickupIndex, pluriIndex);
            zoeaCraftableDef.recipes[2] = zoeaYellows;
        }

        Recipe CreateRecipe(ItemIndex first, ItemIndex second, int count = 1)
        {
            Recipe recipe = new Recipe();
            recipe.amountToDrop = count;
            recipe.ingredients = SetIngredients(first, second);
            return recipe;
        }

        Recipe CreateRecipe(PickupIndex first, ItemIndex second, int count = 1)
        {
            Recipe recipe = new Recipe();
            recipe.amountToDrop = count;
            recipe.ingredients = SetIngredients(first, second);
            return recipe;
        }

        Recipe CreateRecipe(PickupIndex first, PickupIndex second, int count = 1)
        {
            Recipe recipe = new Recipe();
            recipe.amountToDrop = count;
            recipe.ingredients = SetIngredients(first, second);
            return recipe;
        }

        Recipe CreateRecipe(PickupIndex first, EquipmentIndex second, int count = 1)
        {
            Recipe recipe = new Recipe();
            recipe.amountToDrop = count;
            recipe.ingredients = SetIngredients(first, second);
            return recipe;
        }

        Recipe CreateRecipe(ItemIndex first, EquipmentIndex second, int count = 1)
        {
            return CreateRecipe(PickupCatalog.FindPickupIndex(first), second, count);
        }

        RecipeIngredient[] SetIngredients(ItemIndex first, ItemIndex second)
        {
            var ingredients = new RecipeIngredient[2];
            ingredients[0].pickupIndex = PickupCatalog.FindPickupIndex(first);
            ingredients[1].pickupIndex = PickupCatalog.FindPickupIndex(second);
            return ingredients;
        }

        RecipeIngredient[] SetIngredients(PickupIndex first, ItemIndex second)
        {
            var ingredients = new RecipeIngredient[2];
            ingredients[0].pickupIndex = first;
            ingredients[1].pickupIndex = PickupCatalog.FindPickupIndex(second);
            return ingredients;
        }

        RecipeIngredient[] SetIngredients(PickupIndex first, PickupIndex second)
        {
            var ingredients = new RecipeIngredient[2];
            ingredients[0].pickupIndex = first;
            ingredients[1].pickupIndex = second;
            return ingredients;
        }

        RecipeIngredient[] SetIngredients(PickupIndex first, EquipmentIndex second)
        {
            var ingredients = new RecipeIngredient[2];
            ingredients[0].pickupIndex = PickupCatalog.FindPickupIndex(first);
            ingredients[1].pickupIndex = PickupCatalog.FindPickupIndex(second);
            return ingredients;
        }
    }
}