using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace WhiteDragon
{
    public class ValidatorTests
    {
        readonly List<Object> cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in cleanup)
                if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
        }

        T Make<T>(string name) where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            o.name = name;
            cleanup.Add(o);
            return o;
        }

        /// <summary>A valid item that triggers no item-level rule.</summary>
        ItemDefinition Item(string id, params string[] tags)
        {
            var i = Make<ItemDefinition>(id);
            i.id = id;
            i.displayName = id;
            i.description = "desc";
            i.tags = tags;
            return i;
        }

        static List<ContentIssue> Run(ContentSet set) => ContentRules.Validate(set);

        static int Count(List<ContentIssue> issues, string code, IssueSeverity? severity = null) =>
            issues.Count(i => i.Code == code && (severity == null || i.Severity == severity));

        // ---------- Ids ----------

        [Test]
        public void EmptyId_IsError()
        {
            var set = new ContentSet();
            set.Items.Add(Item(""));
            var status = Make<StatusEffectDefinition>("s");
            set.Statuses.Add(status);
            Assert.AreEqual(2, Count(Run(set), "id.empty", IssueSeverity.Error));
        }

        [Test]
        public void DuplicateId_IsErrorPerType_IgnoringCase()
        {
            var set = new ContentSet();
            set.Items.Add(Item("flint"));
            set.Items.Add(Item("FLINT"));
            var syn = Make<SynergyDefinition>("flint_syn");
            syn.id = "flint";
            syn.tag = "x";
            set.Synergies.Add(syn);
            var issues = Run(set);
            Assert.AreEqual(2, Count(issues, "id.duplicate", IssueSeverity.Error), "both items flagged; synergy is a different type");
        }

        // ---------- Location ----------

        [Test]
        public void ResourcesLoadedTypeOutsideItsFolder_IsError()
        {
            var set = new ContentSet();
            var lost = Item("lost");
            var ok = Item("ok");
            var otherRoot = Item("other_root");
            var status = Make<StatusEffectDefinition>("burn");
            status.id = "burn";
            var effect = Make<HomingEffect>("homing");
            set.Items.AddRange(new[] { lost, ok, otherRoot });
            set.Statuses.Add(status);
            set.Effects.Add(effect);
            set.Paths[lost] = "Assets/WhiteDragon/Data/Lost.asset";
            set.Paths[ok] = "Assets/WhiteDragon/Data/Resources/Items/Ok.asset";
            set.Paths[otherRoot] = "Assets/Elsewhere/Resources/Items/Other.asset";
            set.Paths[status] = "Assets/WhiteDragon/Data/Resources/Items/Burn.asset";
            set.Paths[effect] = "Assets/Anywhere/Homing.asset";

            var flagged = Run(set).Where(i => i.Code == "location").Select(i => i.Asset).ToList();
            CollectionAssert.AreEquivalent(new Object[] { lost, status }, flagged);
        }

        // ---------- Effects ----------

        [Test]
        public void NullEffectSlots_AndApplyStatusWithoutStatus_AreErrors()
        {
            var set = new ContentSet();
            var item = Item("a");
            item.effects.Add(null);
            var syn = Make<SynergyDefinition>("syn");
            syn.id = "syn";
            syn.tag = "a";
            syn.requiredCount = 1;
            syn.effects.Add(null);
            var apply = Make<ApplyStatusEffect>("apply");
            set.Items.Add(item);
            set.Synergies.Add(syn);
            set.Effects.Add(apply);

            var issues = Run(set);
            Assert.AreEqual(2, Count(issues, "effects.null", IssueSeverity.Error));
            Assert.AreEqual(1, Count(issues, "applystatus.nostatus", IssueSeverity.Error));

            apply.status = Make<StatusEffectDefinition>("st");
            Assert.AreEqual(0, Count(Run(set), "applystatus.nostatus"));
        }

        // ---------- Items ----------

        [Test]
        public void ItemWarnings_NoPools_Weight_Name_Description()
        {
            var set = new ContentSet();
            var bad = Item("bad");
            bad.poolIds = new string[0];
            bad.weightMultiplier = 0f;
            bad.displayName = "";
            bad.description = " ";
            set.Items.Add(bad);
            set.Items.Add(Item("good"));

            var issues = Run(set);
            Assert.AreEqual(1, Count(issues, "item.nopools", IssueSeverity.Warning));
            Assert.AreEqual(1, Count(issues, "item.weight", IssueSeverity.Warning));
            Assert.AreEqual(1, Count(issues, "item.noname", IssueSeverity.Warning));
            Assert.AreEqual(1, Count(issues, "item.nodescription", IssueSeverity.Warning));
            Assert.IsTrue(issues.Where(i => i.Code.StartsWith("item.")).All(i => i.Asset == bad));
        }

        // ---------- Synergies and tags ----------

        [Test]
        public void SynergyThatCanNeverActivate_IsWarning()
        {
            var set = new ContentSet();
            set.Items.Add(Item("a", "fire"));
            set.Items.Add(Item("b", "Fire"));
            var reachable = Make<SynergyDefinition>("ok");
            reachable.id = "ok";
            reachable.tag = "fire";
            reachable.requiredCount = 2;
            var unreachable = Make<SynergyDefinition>("never");
            unreachable.id = "never";
            unreachable.tag = "fire";
            unreachable.requiredCount = 3;
            var noTag = Make<SynergyDefinition>("notag");
            noTag.id = "notag";
            set.Synergies.AddRange(new[] { reachable, unreachable, noTag });

            var flagged = Run(set).Where(i => i.Code == "synergy.unreachable").Select(i => i.Asset).ToList();
            CollectionAssert.AreEquivalent(new Object[] { unreachable, noTag }, flagged);
        }

        [Test]
        public void TagOnOneItemAndNoSynergy_IsWarning()
        {
            var set = new ContentSet();
            set.Items.Add(Item("a", "lonely", "shared", "synergized"));
            set.Items.Add(Item("b", "shared"));
            var syn = Make<SynergyDefinition>("syn");
            syn.id = "syn";
            syn.tag = "synergized";
            syn.requiredCount = 1;
            set.Synergies.Add(syn);

            var lonely = Run(set).Where(i => i.Code == "tag.single").ToList();
            Assert.AreEqual(1, lonely.Count);
            StringAssert.Contains("'lonely'", lonely[0].Message);
            Assert.AreEqual(IssueSeverity.Warning, lonely[0].Severity);
        }

        [Test]
        public void TagsAndPools_MustBeLowercaseWithoutSpaces()
        {
            var set = new ContentSet();
            var item = Item("a", "Fire", "two words", "fine");
            item.poolIds = new[] { "Boss", "normal" };
            set.Items.Add(item);
            set.Items.Add(Item("b", "fire", "fine"));
            Assert.AreEqual(3, Count(Run(set), "format", IssueSeverity.Warning), "'Fire', 'two words', 'Boss'");
        }

        // ---------- Pools ----------

        [Test]
        public void PedestalPoolWithNoEligibleItem_IsWarning()
        {
            var set = new ContentSet();
            var locked = Item("locked");
            locked.poolIds = new[] { "boss" };
            locked.requiredUnlockId = "secret";
            var zero = Item("zero");
            zero.poolIds = new[] { "treasure" };
            zero.weightMultiplier = 0f;
            set.Items.AddRange(new[] { Item("n"), locked, zero });
            var marker = Make<ItemDefinition>("pedestal_marker");
            set.PedestalPools.Add(("normal", marker));
            set.PedestalPools.Add(("boss", marker));
            set.PedestalPools.Add(("treasure", marker));
            set.PedestalPools.Add(("treasure", marker));

            var pools = Run(set).Where(i => i.Code == "pool.empty").Select(i => i.Message).ToList();
            Assert.AreEqual(2, pools.Count);
            Assert.IsTrue(pools.Any(m => m.Contains("'boss'")));
            Assert.IsTrue(pools.Any(m => m.Contains("'treasure'") && m.Contains("2 pedestal")));
        }

        // ---------- Statuses ----------

        [Test]
        public void StatusTiming_Warnings()
        {
            var set = new ContentSet();
            var noDuration = Make<StatusEffectDefinition>("a");
            noDuration.id = "a";
            noDuration.duration = 0f;
            var badTick = Make<StatusEffectDefinition>("b");
            badTick.id = "b";
            badTick.tickInterval = 0f;
            badTick.damagePerSecond = 2f;
            var slowOnly = Make<StatusEffectDefinition>("c");
            slowOnly.id = "c";
            slowOnly.tickInterval = 0f;
            slowOnly.damagePerSecond = 0f;
            set.Statuses.AddRange(new[] { noDuration, badTick, slowOnly });

            var issues = Run(set);
            Assert.AreEqual(1, Count(issues, "status.duration", IssueSeverity.Warning));
            Assert.AreEqual(1, Count(issues, "status.tick", IssueSeverity.Warning));
            Assert.IsTrue(issues.Where(i => i.Code == "status.tick").All(i => i.Asset == badTick));
        }

        // ---------- Safety ----------

        [Test]
        public void CleanContent_HasNoIssues()
        {
            var set = new ContentSet();
            set.Items.Add(Item("a", "rock"));
            set.Items.Add(Item("b", "rock"));
            var syn = Make<SynergyDefinition>("syn");
            syn.id = "syn";
            syn.tag = "rock";
            syn.requiredCount = 2;
            set.Synergies.Add(syn);
            set.PedestalPools.Add(("normal", syn));
            CollectionAssert.IsEmpty(Run(set));
        }

        [Test]
        public void Validate_NeverModifiesAssets()
        {
            var set = new ContentSet();
            var item = Item("Bad Id", "Fire");
            item.effects.Add(null);
            item.weightMultiplier = -1f;
            set.Items.Add(item);
            set.Items.Add(Item("bad id"));
            string before = JsonUtility.ToJson(item);
            Run(set);
            Assert.AreEqual(before, JsonUtility.ToJson(item));
        }
    }
}
