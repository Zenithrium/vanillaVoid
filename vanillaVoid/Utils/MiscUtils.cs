using RoR2;
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace vanillaVoid.Utils
{
    public static class MiscUtils
    {
        //Sourced from source code, couldn't access because it was private, modified a little
        public static Vector3? RaycastToDirection(Vector3 position, float maxDistance, Vector3 direction, int layer)
        {
            if (Physics.Raycast(new Ray(position, direction), out RaycastHit raycastHit, maxDistance, layer, QueryTriggerInteraction.Ignore))
            {
                return raycastHit.point;
            }
            return null;
        }

        /// <summary>
        /// Takes a collection and shuffle sorts it around randomly.
        /// </summary>
        /// <typeparam name="T">The type of the collection</typeparam>
        /// <param name="toShuffle">The collection to shuffle.</param>
        /// <param name="random">The random to shuffle the collection with.</param>
        /// <returns>The shuffled collection.</returns>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> toShuffle, Xoroshiro128Plus random)
        {
            List<T> shuffled = new List<T>();
            foreach (T value in toShuffle)
            {
                shuffled.Insert(random.RangeInt(0, shuffled.Count + 1), value);
            }
            return shuffled;
        }

        /// <summary>
        /// Finds a node on the node graph that is closest to the specified position on the specified HullClassification.
        /// </summary>
        /// <param name="position">Where we want to go.</param>
        /// <param name="hullClassification">What hullclassification we want to use to check the node graph.</param>
        /// <param name="checkAirNodes">Should we use air nodes? If not, we use the ground nodes instead.</param>
        /// <returns>The position of a node closest to our desired destination, else a Vector3(0, 0, 0).</returns>
        public static Vector3 FindClosestNodeToPosition(Vector3 position, HullClassification hullClassification, bool checkAirNodes = false)
        {
            Vector3 ResultPosition;

            NodeGraph nodesToCheck = checkAirNodes ? SceneInfo.instance.airNodes : SceneInfo.instance.groundNodes;

            var closestNode = nodesToCheck.FindClosestNode(position, hullClassification);

            if (closestNode != NodeGraph.NodeIndex.invalid)
            {
                nodesToCheck.GetNodePosition(closestNode, out ResultPosition);
                return ResultPosition;
            }

            vanillaVoidPlugin.ModLogger.LogInfo($"No closest node to be found for XYZ: {position}, returning 0,0,0");
            return Vector3.zero;
        }

        /// <summary>
        /// Teleports a body to another GameObject using the director system.
        /// </summary>
        /// <param name="characterBody">The body to teleport.</param>
        /// <param name="target">The GameObject we should teleport to.</param>
        /// <param name="teleportEffect">The teleportation effect we should use upon arrival.</param>
        /// <param name="hullClassification">The hullclassification we should use for traversing the nodegraph.</param>
        /// <param name="rng">The random that we should use to position the body randomly around our target.</param>
        /// <param name="minDistance">How close to the target can we get?</param>
        /// <param name="maxDistance">How far out from the target can we get?</param>
        /// <param name="teleportAir">Should we use the air nodes?</param>
        /// <returns>A bool representing if the teleportation was successful or not.</returns>
        public static bool TeleportBody(CharacterBody characterBody, GameObject target, GameObject teleportEffect, HullClassification hullClassification, Xoroshiro128Plus rng, float minDistance = 20, float maxDistance = 45, bool teleportAir = false)
        {
            if (!characterBody) { return false; }

            SpawnCard spawnCard = ScriptableObject.CreateInstance<SpawnCard>();
            spawnCard.hullSize = hullClassification;
            spawnCard.nodeGraphType = teleportAir ? MapNodeGroup.GraphType.Air : MapNodeGroup.GraphType.Ground;
            spawnCard.prefab = Resources.Load<GameObject>("SpawnCards/HelperPrefab");
            GameObject gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(spawnCard, new DirectorPlacementRule
            {
                placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                position = target.transform.position,
                minDistance = minDistance,
                maxDistance = maxDistance
            }, rng));
            if (gameObject)
            {
                TeleportHelper.TeleportBody(characterBody, gameObject.transform.position);
                GameObject teleportEffectPrefab = teleportEffect;
                if (teleportEffectPrefab)
                {
                    EffectManager.SimpleEffect(teleportEffectPrefab, gameObject.transform.position, Quaternion.identity, true);
                }
                UnityEngine.Object.Destroy(gameObject);
                UnityEngine.Object.Destroy(spawnCard);
                return true;
            }
            else
            {
                UnityEngine.Object.Destroy(spawnCard);
                return false;
            }
        }

        /// <summary>
        /// Returns a point above a hit enemy at the distance specified. If it collides with the world before that point, it returns that point instead.
        /// </summary>
        /// <param name="damageInfo"></param>
        /// <param name="distanceAboveTarget"></param>
        /// <returns></returns>
        public static Vector3? AboveTargetVectorFromDamageInfo(DamageInfo damageInfo, float distanceAboveTarget)
        {
            if (damageInfo.rejected || !damageInfo.attacker) { return null; }


            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();

            if (attackerBody)
            {
                RoR2.TeamMask enemyTeams = RoR2.TeamMask.GetEnemyTeams(attackerBody.teamComponent.teamIndex);
                HurtBox hurtBox = new RoR2.SphereSearch
                {
                    radius = 1,
                    mask = RoR2.LayerIndex.entityPrecise.mask,
                    origin = damageInfo.position
                }.RefreshCandidates().FilterCandidatesByHurtBoxTeam(enemyTeams).OrderCandidatesByDistance().FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes().FirstOrDefault();

                if (hurtBox)
                {
                    if (hurtBox.healthComponent && hurtBox.healthComponent.body)
                    {
                        var body = hurtBox.healthComponent.body;

                        var closestPointOnBounds = body.mainHurtBox.collider.ClosestPointOnBounds(body.transform.position + new Vector3(0, 10000, 0));

                        var raycastPoint = RaycastToDirection(closestPointOnBounds, distanceAboveTarget, Vector3.up, LayerIndex.world.mask);
                        if (raycastPoint.HasValue)
                        {
                            return raycastPoint.Value;
                        }
                        else
                        {
                            return closestPointOnBounds + (Vector3.up * distanceAboveTarget);
                        }
                    }

                }
            }

            return null;
        }

        /// <summary>
        /// Returns a point above the target body at the distance specified. If it collides with the world before that point, it returns that point instead.
        /// </summary>
        /// <param name="body">The target body.</param>
        /// <param name="distanceAbove">How far above the body should our point be?</param>
        /// <returns></returns>
        public static Vector3? AboveTargetBody(CharacterBody body, float distanceAbove)
        {
            if (!body) { return null; }

//#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            var closestPointOnBounds = body.mainHurtBox.collider.ClosestPointOnBounds(body.transform.position + new Vector3(0, 10000, 0));
//#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            var raycastPoint = RaycastToDirection(closestPointOnBounds, distanceAbove, Vector3.up, LayerIndex.world.mask);
            if (raycastPoint.HasValue)
            {
                return raycastPoint.Value;
            }
            else
            {
                return closestPointOnBounds + (Vector3.up * distanceAbove);
            }
        }

        /// <summary>
        /// Returns a dictionary containing surface normal alignment information from the specified ray. Useful for positioning things on a normal relative to look correctly.
        /// </summary>
        /// <param name="ray">A ray that determines where we start looking from, and what direction.</param>
        /// <param name="layerMask">The index of the layer our ray should collide with.</param>
        /// <param name="distance">How far out we should cast our ray.</param>
        /// <returns>The dictionary containing the alignment information based on the normal the raycast hit, else if it hit nothing it returns null.</returns>
        public static Dictionary<string, Vector3> GetAimSurfaceAlignmentInfo(Ray ray, int layerMask, float distance)
        {
            Dictionary<string, Vector3> SurfaceAlignmentInfo = new Dictionary<string, Vector3>();

            var didHit = Physics.Raycast(ray, out RaycastHit raycastHit, distance, layerMask, QueryTriggerInteraction.Ignore);

            if (!didHit)
            {
                vanillaVoidPlugin.ModLogger.LogInfo($"GetAimSurfaceAlignmentInfo did not hit anything in the aim direction on the specified layer ({layerMask}).");
                return null;
            }

            var point = raycastHit.point;
            var right = Vector3.Cross(ray.direction, Vector3.up);
            var up = Vector3.ProjectOnPlane(raycastHit.normal, right);
            var forward = Vector3.Cross(right, up);

            SurfaceAlignmentInfo.Add("Position", point);
            SurfaceAlignmentInfo.Add("Right", right);
            SurfaceAlignmentInfo.Add("Forward", forward);
            SurfaceAlignmentInfo.Add("Up", up);

            return SurfaceAlignmentInfo;
        }

        /// <summary>
        /// Returns a plural version of the string
        /// </summary>
        /// <param name="str">target string</param>
        /// <returns>pluralized string</returns>
        private static List<string> alreadyPlural = new List<string>() // "rounds" and "-es" does not need to be registered here
        {
            "400 Tickets",
            "Balls",
            "Gummy Vitamins", 
            // "Zoom Lenses", 
            "Booster Boots", 
            // "Jellied Soles", 
            "Life Savings", 
            // "Needles", 
            "Predatory Instincts",
            "Fueling Bellows",
            "Sharpened Chopsticks",
            "Hardlight Shields",
            "Steroids",
            // "Boxing Gloves",
            // "Spafnar's Fries",
            "Charging Nanobots",
            "Experimental Jets",
            "Prototype Jet Boots",
            "Low Quality Speakers",
            // "Purrfect Headphones",
            // "Inoperative Nanomachines",
            "Stargazer's Records",
            // "Prison Shackles",
            "Baby's Toys",
            "Defensive Microbots",
            "Death's Regards",
            "Spare Drone Parts",
            "Brainstalks",
            // "Empathy Cores",
            "Sorcerer's Pills",
            "Shifted Quartz",
            "Clasping Claws",
            "Enhancement Vials",
            "<style=cIsVoid>Corrupts all Shatterspleens.</style>",
            "Empyrean Braces",
            "Orbs of Falsity",
            "Visions of Heresy",
            "Hooks of Heresy",
            "Strides of Heresy",
            "Essence of Heresy",
            "Beads of Fealty",
            "Prescriptions",
            "Snake Eyes",
            "Panic Mines",
        };
        // metarule: "(\\S+) of", "Pluralize($1) of $2s"
        // metarule: "The (.+)", "Pluralize($1)"
        // metarule: "Silence Between Two Strikes", "Silences Between Two Strikes"
        private static Dictionary<string, string> rules = new Dictionary<string, string>() {
            { "rounds$", "rounds" },
            { "tooth$", "teeth" },
            { "fungus$", "fungi" },
            { "(a|e|i|o|u)o$", "$1os" },
            { "o$", "oes" },
            { "(a|e|i|o|u)y$", "$1ys" },
            { "y$", "ies" },
            { "ff$", "s" },
            { "fe?$", "ves" },
            { "es$", "es" },
            { "(ch|sh|x|z|s)$", "$1es" },
            { "s?$", "s" }
        };
        public static string GetPlural(string str)
        {
            if (alreadyPlural.Contains(str)) return str;
            if (str == "Silence Between Two Strikes") return "Silences Between Two Strikes";
            Match metarule = Regex.Match(str, "(.+) of (.+)", RegexOptions.IgnoreCase);
            if (metarule.Success) return GetPlural(metarule.Groups[1].Value) + " of " + metarule.Groups[2].Value;
            metarule = Regex.Match(str, "^the (.+)", RegexOptions.IgnoreCase);
            if (metarule.Success) return GetPlural(metarule.Groups[1].Value);
            foreach (var k in rules.Keys)
            {
                string res = Regex.Replace(str, k, rules[k], RegexOptions.IgnoreCase);
                if (res != str) return res;
            }
            return str;
        }
    }
}
