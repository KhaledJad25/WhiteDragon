using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace WhiteDragon
{
    public class EnemyValidatorTests
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

        static List<ContentIssue> Run(ContentSet set) => ContentRules.Validate(set);

        static int Count(List<ContentIssue> issues, string code, IssueSeverity severity) =>
            issues.Count(i => i.Code == code && i.Severity == severity);

        static BrainState State(string name, params EnemyBehavior[] behaviors) =>
            new BrainState { name = name, behaviors = new List<EnemyBehavior>(behaviors) };

        static BrainState Go(BrainState s, string target)
        {
            s.transitions.Add(new BrainTransition { condition = TransitionCondition.Always, target = target });
            return s;
        }

        StateMachineBrain Brain(string name, params BrainState[] states)
        {
            var b = Make<StateMachineBrain>(name);
            b.states = new List<BrainState>(states);
            return b;
        }

        EnemyDefinition Enemy(string id, EnemyBrainDefinition brain, MovementMode movement = MovementMode.Ground)
        {
            var e = Make<EnemyDefinition>(id);
            e.id = id;
            e.brain = brain;
            e.movement = movement;
            return e;
        }

        [Test]
        public void EnemyWithoutBrain_IsError_AndSceneEnemyOnBuiltInChase_IsWarning()
        {
            var set = new ContentSet();
            var noBrain = Enemy("lost", null);
            set.Enemies.Add(noBrain);
            var go = new GameObject("LooseEnemy");
            cleanup.Add(go);
            go.AddComponent<CharacterController>();
            var sceneEnemy = go.AddComponent<WhiteDragon.Enemy>();
            sceneEnemy.definition = noBrain;
            set.SceneEnemies.Add(sceneEnemy);

            var issues = Run(set);
            Assert.AreEqual(1, Count(issues, "enemy.nobrain", IssueSeverity.Error));
            Assert.AreEqual(1, Count(issues, "enemy.builtinchase", IssueSeverity.Warning));
            Assert.AreEqual(1, Count(issues, "enemy.novisual", IssueSeverity.Warning), "no visual prefab and no enemy prefab");
        }

        [Test]
        public void BrainStructure_Errors()
        {
            var wait = Make<WaitBehavior>("wait");
            var set = new ContentSet();
            set.Brains.Add(Brain("empty"));
            set.Brains.Add(Brain("broken",
                Go(State("A", wait, null), "Missing"),
                State("Stuck", wait)));
            var terminal = State("End", wait);
            terminal.terminal = true;
            set.Brains.Add(Brain("fine", Go(State("A", wait), "End"), terminal));
            set.Brains.Add(Brain("single", State("Only", wait)));

            var issues = Run(set);
            Assert.AreEqual(1, Count(issues, "brain.nostates", IssueSeverity.Error));
            Assert.AreEqual(1, Count(issues, "brain.nullbehavior", IssueSeverity.Error));
            Assert.AreEqual(1, Count(issues, "brain.badtarget", IssueSeverity.Error));
            Assert.AreEqual(1, Count(issues, "brain.noexit", IssueSeverity.Error), "only 'Stuck'; terminal and single-state brains are fine");
        }

        [Test]
        public void AttackWithoutTelegraph_IsError_TelegraphBeforeIt_IsFine()
        {
            var dash = Make<DashBehavior>("dash");
            var tele = Make<TelegraphBehavior>("tele");
            var wait = Make<WaitBehavior>("wait");
            var set = new ContentSet();
            set.Brains.Add(Brain("bare", Go(State("Wait", wait), "Dash"), Go(State("Dash", dash), "Wait")));
            set.Brains.Add(Brain("start", Go(State("Dash", dash), "Wait"), Go(State("Wait", wait), "Tele"), Go(State("Tele", tele), "Dash")));
            set.Brains.Add(Brain("good", Go(State("Wait", wait), "Tele"), Go(State("Tele", tele), "Dash"), Go(State("Dash", dash), "Wait")));
            set.Brains.Add(Brain("through", Go(State("Wait", wait), "Tele"), Go(State("Tele", tele), "Check"),
                Go(State("Check"), "Dash"), Go(State("Dash", dash), "Wait")));

            var flagged = Run(set).Where(i => i.Code == "brain.notelegraph").Select(i => i.Asset.name).ToList();
            CollectionAssert.AreEquivalent(new[] { "bare", "start" }, flagged,
                "no telegraph before it, or it is the start state; a telegraph directly or through an empty decision state is fine");
        }

        [Test]
        public void GroundEnemyUsingFlyingOnlyBehavior_IsError()
        {
            var hover = Make<HoverBehavior>("hover");
            var brain = Brain("b", State("Hover", hover));
            var set = new ContentSet();
            set.Brains.Add(brain);
            set.Enemies.Add(Enemy("walker", brain, MovementMode.Ground));
            set.Enemies.Add(Enemy("flyer", brain, MovementMode.Flying));
            var issues = Run(set).Where(i => i.Code == "enemy.movement").ToList();
            Assert.AreEqual(1, issues.Count);
            StringAssert.Contains("walker", issues[0].Message);
        }

        [Test]
        public void Behaviors_NoDescription_Unused_AndSharedByMoreThanThreeBrains()
        {
            var undescribed = Make<UndescribedBehavior>("undescribed");
            var unused = Make<WaitBehavior>("unused");
            var common = Make<WaitBehavior>("common");
            var set = new ContentSet();
            set.Behaviors.AddRange(new EnemyBehavior[] { undescribed, unused, common });
            for (int i = 0; i < 4; i++)
            {
                var brain = Brain("brain" + i, State("S", common, i == 0 ? undescribed : null));
                brain.states[0].behaviors.RemoveAll(b => b == null);
                set.Brains.Add(brain);
                set.Enemies.Add(Enemy("enemy" + i, brain));
            }

            var issues = Run(set);
            Assert.AreEqual(1, Count(issues, "behavior.nodescription", IssueSeverity.Warning));
            Assert.AreEqual(1, Count(issues, "behavior.unused", IssueSeverity.Warning));
            var shared = issues.Single(i => i.Code == "behavior.shared");
            Assert.AreEqual(IssueSeverity.Warning, shared.Severity);
            Assert.AreSame(common, shared.Asset);
            foreach (var n in new[] { "enemy0", "enemy1", "enemy2", "enemy3" }) StringAssert.Contains(n, shared.Message);
        }

        [Test]
        public void ThreeBrainsSharingABehavior_IsNotReported()
        {
            var common = Make<WaitBehavior>("common");
            var set = new ContentSet();
            set.Behaviors.Add(common);
            for (int i = 0; i < ContentRules.SharedBehaviorLimit; i++) set.Brains.Add(Brain("b" + i, State("S", common)));
            Assert.AreEqual(0, Run(set).Count(i => i.Code == "behavior.shared"));
        }

        [Test]
        public void DuplicateVariantIds_AreErrors()
        {
            var set = new ContentSet();
            var a = Make<EnemyVariant>("a");
            a.id = "fast";
            var b = Make<EnemyVariant>("b");
            b.id = "FAST";
            set.Variants.Add(a);
            set.Variants.Add(b);
            Assert.AreEqual(2, Count(Run(set), "id.duplicate", IssueSeverity.Error));
        }

        [Test]
        public void ShippedEnemies_PassEveryEnemyRule()
        {
            var problems = ContentValidator.Collect()
                .Where(i => i.Code.StartsWith("enemy.") || i.Code.StartsWith("brain.") || i.Code.StartsWith("behavior."))
                .Select(i => i.ToString()).ToList();
            CollectionAssert.IsEmpty(problems);
        }
    }
}
