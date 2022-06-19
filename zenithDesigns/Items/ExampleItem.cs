using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;
using static vanillaVoid.Main;

namespace vanillaVoid.Items
{
    public class ExampleItem : ItemBase<ExampleItem>
    {
        public ConfigEntry<float> DamageCoeff;
        public override string ItemName => "Abyss-Touched Adze";

        public override string ItemLangTokenName => "ADZE_ITEM";

        public override string ItemPickupDesc => "Deal more damage to weaker enemies.";

        public override string ItemFullDescription => $"Deal up to <style=cIsDamage>{DamageCoeff.Value}</style> additional damage <style=cStack>(+{DamageCoeff.Value} damage per stack)</style> to enemies with base health lower than 400 <style=cStack>(+50 health per stack)</style>.";

        public override string ItemLore => "So you're saying you destroyed- \n Traded! " +
            "\n ...traded, our only crowbar. For that. \n Don't be so sour, come on! It's much better than a crowbar. " +
            "\n I don't even know what it is. \n It's an adze. It's like an...old time-y crowbar. More or less." +
            "\n Ohh, so you decided our modern tools were too important, too useful for you? \n Oh quit the whining. This thing's a relic, it'd be worth way more than a crowbar. And it's probably way more useful, too." +
            "\n It'd better be.";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override GameObject ItemModel => MainAssets.LoadAsset<GameObject>("ExampleItemPrefab.prefab");

        public override Sprite ItemIcon => MainAssets.LoadAsset<Sprite>("ExampleItemIcon.png");

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            Hooks();
        }

        public override void CreateConfig(ConfigFile config)
        {

        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict();
        }

        public override void Hooks()
        {

        }

    }
}
