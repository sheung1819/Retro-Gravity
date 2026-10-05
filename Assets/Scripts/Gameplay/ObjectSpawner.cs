using System.Collections.Generic;
using UnityEngine;

namespace Pulse
{
    /// <summary>
    /// Spawns good/bad objects from the edge opposite the floor, moves them along
    /// the current gravity, and resolves catches, hits and misses against the catcher.
    /// </summary>
    public class ObjectSpawner : MonoBehaviour
    {
        private PulseConfig config;
        private CatcherController catcher;
        private GameManager game;

        private readonly List<FallingObject> active = new List<FallingObject>();
        private readonly Stack<FallingObject> goodPool = new Stack<FallingObject>();
        private readonly Stack<FallingObject> badPool = new Stack<FallingObject>();

        private float spawnTimer;
        private bool spawning;
        private bool iterating;
        private bool clearPending;

        // Current difficulty values, pushed by GameManager.
        private float spawnInterval = 1f;
        private float travelTime = 3f;
        private float badChance = 0.25f;

        public void Init(PulseConfig cfg, CatcherController c, GameManager gm)
        {
            config = cfg;
            catcher = c;
            game = gm;
        }

        public void SetDifficulty(float interval, float travel, float bad)
        {
            spawnInterval = interval;
            travelTime = travel;
            badChance = bad;
        }

        public void SetSpawning(bool on)
        {
            spawning = on;
            if (on) spawnTimer = 0.6f; // brief breather at the start of a run
        }

        public void ClearAll()
        {
            // A catch/hit callback (e.g. the final hit ending the run) can ask for a
            // clear while we're mid-loop; defer it until the loop is done.
            if (iterating)
            {
                clearPending = true;
                return;
            }
            for (int i = active.Count - 1; i >= 0; i--) Recycle(i);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (spawning)
            {
                spawnTimer -= dt;
                if (spawnTimer <= 0f)
                {
                    Spawn();
                    spawnTimer += spawnInterval * Random.Range(0.8f, 1.2f);
                }
            }

            MoveAndResolve(dt);
        }

        // ---------- Spawning ----------

        private float FallSpeed(FloorEdge floor)
        {
            // Every floor gets the same crossing time, so short (sideways) and long
            // (vertical) axes feel equally fair on a tall phone screen.
            float distance = Mathf.Abs(Playfield.ViewEdgeCoordinate(floor.Opposite()) - catcher.FloorLine);
            return distance / Mathf.Max(0.3f, travelTime);
        }

        private void Spawn()
        {
            FloorEdge floor = catcher.CurrentFloor;
            bool good = Random.value >= badChance;
            float radius = good ? config.goodRadius : config.badRadius;

            // Spawn anywhere the catcher can reach, just outside the opposite edge of the screen.
            Vector2 range = Playfield.AlongRange(floor);
            float half = catcher.Length * 0.5f;
            float along = Random.Range(range.x + half, range.y - half);
            FloorEdge spawnEdge = floor.Opposite();
            float outside = Playfield.ViewEdgeCoordinate(spawnEdge) + Playfield.AxisSign(spawnEdge) * radius * 1.5f;
            Vector2 pos = floor.IsHorizontal() ? new Vector2(along, outside) : new Vector2(outside, along);

            FallingObject obj = Get(good);
            obj.Launch(pos, floor.FallDirection() * FallSpeed(floor));
            active.Add(obj);
        }

        private FallingObject Get(bool good)
        {
            var pool = good ? goodPool : badPool;
            if (pool.Count > 0) return pool.Pop();
            return FallingObject.Create(transform, good, good ? config.goodRadius : config.badRadius);
        }

        private void Recycle(int index)
        {
            FallingObject obj = active[index];
            active.RemoveAt(index);
            obj.gameObject.SetActive(false);
            (obj.IsGood ? goodPool : badPool).Push(obj);
        }

        // ---------- Movement + collision ----------

        private void MoveAndResolve(float dt)
        {
            FloorEdge floor = catcher.CurrentFloor;
            Vector2 fallDir = floor.FallDirection();
            Vector2 targetVel = fallDir * FallSpeed(floor);
            Rect catchRect = catcher.GetCatchRect();
            float floorLine = catcher.FloorLine;
            float sign = Playfield.AxisSign(floor);
            Rect bounds = Playfield.ViewRect;
            bounds.xMin -= 3f; bounds.yMin -= 3f; bounds.xMax += 3f; bounds.yMax += 3f;

            iterating = true;
            for (int i = active.Count - 1; i >= 0 && !clearPending; i--)
            {
                FallingObject obj = active[i];

                // Gravity may have flipped: swing in-flight objects onto the new heading.
                obj.Velocity = Vector2.MoveTowards(obj.Velocity, targetVel, config.gravityTurnRate * dt);
                obj.Tick(dt);

                Vector2 p = obj.transform.position;

                if (Overlaps(catchRect, p, obj.Radius))
                {
                    if (obj.IsGood) game.OnGoodCaught(p);
                    else if (!game.OnBadTouched(p)) continue; // invulnerable: hazard passes through
                    Recycle(i);
                    continue;
                }

                // Past the catcher's line on the fall axis → reached the floor.
                float depth = (floor.IsHorizontal() ? p.y : p.x) - floorLine;
                if (depth * sign > catcher.Thickness + obj.Radius)
                {
                    // Objects that end up "behind" the floor because of a flip don't count as misses.
                    if (obj.IsGood && !catcher.IsFlipping) game.OnGoodMissed(p);
                    Recycle(i);
                    continue;
                }

                if (!bounds.Contains(p)) Recycle(i);
            }
            iterating = false;

            if (clearPending)
            {
                clearPending = false;
                ClearAll();
            }
        }

        private static bool Overlaps(Rect r, Vector2 c, float radius)
        {
            float cx = Mathf.Clamp(c.x, r.xMin, r.xMax);
            float cy = Mathf.Clamp(c.y, r.yMin, r.yMax);
            float dx = c.x - cx, dy = c.y - cy;
            return dx * dx + dy * dy <= radius * radius;
        }
    }
}
