using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public static class Global
{
    //ENTITY TYPE
    public const int TYPE_MONSTER = 0;

    public const int TYPE_MY_HERO = 1;
    public const int TYPE_OP_HERO = 2;

    //MAP SIZE
    public const int MAP_WIDTH = 17630;

    public const int MAP_HEIGHT = 9000;

    //RANGE
    public const int WIND_RANGE = 1280;

    public const int SHIELD_RANGE = 2200;
    public const int CONTROL_RANGE = 2200;
    public const int VIEW_RANGE = 2200;
    public const int BASE_TARGET_RANGE = 5000;

    //OTHER CONSTANT
    public const int MAX_MANA = 150;

    public const int DEFEND_DIST = 6200;
    public const int ATTACK_DIST = 5000;
    public const int DEFEND_FARM_DIST = 9000;
    public const int ATTACK_FARM_DIST = 7000;
    public const int MANA_DEFEND = 50;

    //Value ACTION
    public const double GUARD = 1e5;
    public const double FARMING_VALUE = 1e6;
    public const double MONSTER_DEFENSE_VALUE = 1e7;
    public const double WIND_SPELL_VALUE = 1e8;
    public const double ATTACK_MOVE_VALUE = 1e9;
    public const double ATTACK_SHIELD_SPELL_VALUE = 4e9;
    public const double ATTACK_WIND_SPELL_VALUE = 3e9;
    public const double ATTACK_CONTROL_SPELL_VALUE = 2e9;
    public const double ATTACK_CONTROL_OP_VALUE = 5e9;
    public const double ATTACK_WIND_OP_VALUE = 6e9;

    // Generates a random number within a range.
    public static int RandomNumber(int min, int max)
    {
        return (new Random()).Next(min, max);
    }

    public static double CalculateDistance(Point p1, Point p2)
    {
        return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
    }

    public static bool IsInRange(Point p1, Point p2, int range)
    {
        return CalculateDistance(p1, p2) <= range;
    }

    public static Point GeneratePoint(Point pos, int range, double angle)
    {
        double x = pos.X + range * Math.Cos(angle);
        double y = pos.Y + range * Math.Sin(angle);
        return new Point((int)x, (int)y);
    }
    public static List<Point> GetPositionGuard(Point tPos, int range, int divider = 12)
    {
        List<Point> result = new List<Point>();
        Point pos = new Point();
        foreach (int i in Enumerable.Range(1, 12))
        {
            double angle = 2 * Math.PI / divider * (0.5 + i);
            pos = Global.GeneratePoint(tPos, range, angle);
            if (pos.X < 0 || pos.Y < 0 || pos.X > Global.MAP_WIDTH || pos.Y > Global.MAP_HEIGHT)
                continue;
            result.Add(pos);
        }
        return result;
    }
}

public class MainBase
{
    public int EntityCount;
    public Point Coord;
    public int HeroesCount;
    public int Health;
    public int Mana;
    public Point OpCoord;
    public bool Position; //0 if top left, 1 if bottom right

    public MainBase(Point basePos, int heroesCount, int health)
    {
        Coord = basePos;
        HeroesCount = heroesCount;
        Health = health;
    }
}

public class Entity
{
    public int Id;
    public int Type;
    public Point Coord;
    public int ShieldLife;
    public int IsControlled;
    public int Health;
    public Point Vector;
    public int NearBase;
    public int ThreatFor;
    public bool IsTarget = false;
    public bool HasAction = false;
    public string Action = "";

    public Entity(int id, int type, Point coord, int shieldLife, int isControlled, int health, Point vector, int nearBase, int threatFor)
    {
        this.Id = id;
        this.Type = type;
        this.Coord = coord;
        this.ShieldLife = shieldLife;
        this.IsControlled = isControlled;
        this.Health = health;
        this.Vector = vector;
        this.NearBase = nearBase;
        this.ThreatFor = threatFor;
    }
}

struct Action
{
    public string? Text;
    public double Value;
    public Entity? Hero;
    public Entity? Monster;

    public void UpdateValue(double value, string? action, Entity? hero = null, Entity? monster = null)
    {
        if (value > Value)
        {
            Text = action;
            Value = value;
            Hero = hero;
            Monster = monster;
        }
    }

    public void SpellWindOp( Entity hero, List<Entity> monsters, List<Entity> oppHeroes, MainBase mainBase)
    {
        foreach (Entity oppHero in oppHeroes)
        {
            if (oppHero.ShieldLife > 0)
                continue;
            if (!Global.IsInRange(oppHero.Coord, mainBase.OpCoord, 5000))
                continue;
            if (!Global.IsInRange(oppHero.Coord, hero.Coord, Global.WIND_RANGE))
                continue;
            int mayAttackMonster = 0;
            foreach (Entity monster in monsters)
            {
                if (Global.IsInRange(monster.Coord, oppHero.Coord, 800) && monster.ShieldLife > 0)
                    mayAttackMonster++;
            }
            if (mayAttackMonster > 0)
                UpdateValue(
                    Global.ATTACK_WIND_OP_VALUE + mayAttackMonster,
                    $"SPELL WIND {Global.MAP_WIDTH / 2} {Global.MAP_HEIGHT / 2}",
                    hero);
        }
    }

    public void SpellOpControl( Entity hero, List<Entity> monsters, List<Entity> oppHeroes, MainBase mainBase)
    {
        foreach (Entity oppHero in oppHeroes)
        {
            if (oppHero.ShieldLife > 0)
                continue;
            if (!Global.IsInRange(oppHero.Coord, mainBase.OpCoord, 5000))
                continue;
            if (!Global.IsInRange(oppHero.Coord, hero.Coord, Global.CONTROL_RANGE))
                continue;
            int mayAttackMonster = 0;
            foreach (Entity monster in monsters)
            {
                if (Global.IsInRange(monster.Coord, oppHero.Coord, 800))
                    mayAttackMonster++;
            }
            if (mayAttackMonster > 0)
                UpdateValue(
                    Global.ATTACK_CONTROL_OP_VALUE + mayAttackMonster,
                    $"SPELL CONTROL {oppHero.Id} {Global.MAP_WIDTH / 2} {Global.MAP_HEIGHT / 2} go see anywhere",
                    hero);
        }
    }

    public void SpellMonsterControl( Entity hero, List<Entity> monsters, MainBase mainBase)
    {
        foreach (Entity monster in monsters)
        {
            if ((monster.NearBase == 1 && monster.ThreatFor == 2) ||
                (monster.NearBase == 0 && monster.ThreatFor == 2))
                continue;
            if (monster.Health < 15)
                continue;
            if (Global.CalculateDistance(monster.Coord, hero.Coord) > Global.CONTROL_RANGE ||
               Global.CalculateDistance(hero.Coord, mainBase.OpCoord) > 7000)
                continue;
            UpdateValue(
                Global.ATTACK_CONTROL_SPELL_VALUE,
                $"SPELL CONTROL {monster.Id} {mainBase.OpCoord.X} {mainBase.OpCoord.Y} go to enemy base",
                hero);
        }
    }

    public void SpellWindAttack( Entity hero, List<Entity> monsters, MainBase mainBase)
    {
        int monsterNearBase = 0;
        foreach (Entity monster in monsters)
        {
            if (Global.IsInRange(monster.Coord, mainBase.OpCoord, (5000 + 2200)) &&
               Global.IsInRange(monster.Coord, hero.Coord, Global.WIND_RANGE) &&
               monster.ShieldLife == 0)
                monsterNearBase++;
            if (monsterNearBase >= 2)
                UpdateValue(
                    Global.ATTACK_WIND_SPELL_VALUE,
                    $"SPELL WIND {mainBase.OpCoord.X} {mainBase.OpCoord.Y} not here",
                    hero);
        }
    }

    public void SpellShield( Entity hero, List<Entity> monsters, MainBase mainBase)
    {
        foreach (Entity monster in monsters)
        {
            if (!(monster.NearBase == 1 && monster.ThreatFor == 2) ||
                (monster.NearBase == 0 && monster.ThreatFor == 2))
                continue;
            if (monster.ShieldLife > 0 || Global.CalculateDistance(monster.Coord, hero.Coord) > Global.SHIELD_RANGE)
                continue;
            if (Global.CalculateDistance(monster.Coord, mainBase.OpCoord) > 5000)
                continue;
            if (monster.Health / 2 < (Global.CalculateDistance(monster.Coord, mainBase.OpCoord) - 300) / 400)
                continue;
            UpdateValue(
                Global.ATTACK_SHIELD_SPELL_VALUE,
                $"SPELL SHIELD {monster.Id} i protect u",
                hero);
        }
    }

    public void Attack( Entity hero, MainBase mainBase)
    {
        int randomRange = Global.RandomNumber(1500, 6000);
        List<Point> EstimatePosition = Global.GetPositionGuard(mainBase.OpCoord, randomRange);
        Point target = EstimatePosition[(new Random()).Next(EstimatePosition.Count)];
        UpdateValue(
                Global.ATTACK_MOVE_VALUE,
                $"MOVE {target.X} {target.Y} ASSAUUUULLTTTT",
                hero);
    }

    public void Defend( Entity hero, List<Entity> monsters, MainBase mainBase)
    {
        foreach (Entity monster in monsters)
        {
            if (monster.IsTarget ||
                !((monster.NearBase == 1 && monster.ThreatFor == 1) ||
                (monster.NearBase == 0 && monster.ThreatFor == 1)))
                continue;
            UpdateValue(
                Global.MONSTER_DEFENSE_VALUE - Global.CalculateDistance(mainBase.Coord, monster.Coord) - Global.CalculateDistance(monster.Coord, hero.Coord),
                $"MOVE {monster.Coord.X + monster.Vector.X} {monster.Coord.Y + monster.Vector.Y} time to get mana",
                hero,
                monster);
        }
    }

    public void Farming( Entity hero, List<Entity> monsters, MainBase mainBase, bool canAttack)
    {
        int distMax = canAttack ? Global.ATTACK_FARM_DIST : Global.DEFEND_FARM_DIST;
        foreach (Entity monster in monsters)
        {
            if (monster.IsTarget || !Global.IsInRange(monster.Coord, mainBase.Coord, distMax))
                continue;
            UpdateValue(
                   Global.FARMING_VALUE - Global.CalculateDistance(monster.Coord, hero.Coord),
                   $"MOVE {monster.Coord.X + monster.Vector.X} {monster.Coord.Y + monster.Vector.Y} time to get mana",
                   hero,
                   monster);
        }
    }

    public void SpellWindDefend( Entity hero, List<Entity> monsters, MainBase mainBase)
    {
        if (mainBase.Mana >= 10)
        {
            bool monsterNearBase = false;
            foreach (Entity monster in monsters)
            {
                if (Global.IsInRange(monster.Coord, hero.Coord, Global.WIND_RANGE) &&
                    monster.ShieldLife == 0 &&
                    monster.Health + 3 > 2 * Global.CalculateDistance(monster.Coord, mainBase.Coord) / 400)
                {
                    monsterNearBase = true;
                    break;
                }
            }
            if (monsterNearBase)
                UpdateValue(
                    Global.WIND_SPELL_VALUE,
                    $"SPELL WIND {mainBase.OpCoord.X} {mainBase.OpCoord.Y}",
                    hero);
        }
    }

    public void Guard( Entity hero, MainBase mainBase, bool canAttack, int a)
    {
        int distMax = canAttack ? Global.ATTACK_DIST : Global.DEFEND_DIST;
        Point guardPosition = Global.GetPositionGuard(mainBase.Coord, distMax)[a];
        UpdateValue(
                Global.GUARD,
                ((hero.Coord != guardPosition) ? $"MOVE {guardPosition.X} {guardPosition.Y} go to guard" : "WAIT"),
                hero);
    }

   
};

internal class Player
{
    private static void Main(string[] args)
    {
        string[] inputs;
        MainBase mainBase;
        inputs = Console.ReadLine().Split(' ');

        // base_x,base_y: The corner of the map representing your base
        int baseX = int.Parse(inputs[0]);
        int baseY = int.Parse(inputs[1]);
        bool canAttack = false;
        // heroesPerPlayer: Always 3
        int heroesPerPlayer = int.Parse(Console.ReadLine());
        mainBase = new MainBase(new Point(baseX, baseY), heroesPerPlayer, 3);
        mainBase.OpCoord = new Point(Global.MAP_WIDTH - baseX, Global.MAP_HEIGHT - baseY);

        // game loop
        while (true)
        {
            inputs = Console.ReadLine().Split(' ');
            mainBase.Health = int.Parse(inputs[0]); // Your base health
            mainBase.Mana = int.Parse(inputs[1]); // Ignore in the first league; Spend ten mana to cast a spell

            //op data
            inputs = Console.ReadLine().Split(' ');
            int oppHealth = int.Parse(inputs[0]);
            int oppMana = int.Parse(inputs[1]);

            mainBase.EntityCount = int.Parse(Console.ReadLine()); // Amount of heros and monsters you can see
            List<Entity> myHeroes = new List<Entity>(mainBase.EntityCount);
            List<Entity> oppHeroes = new List<Entity>(mainBase.EntityCount);
            List<Entity> monsters = new List<Entity>(mainBase.EntityCount);

            for (int i = 0; i < mainBase.EntityCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int id = int.Parse(inputs[0]); // Unique identifier
                int type = int.Parse(inputs[1]); // 0=monster, 1=your hero, 2=opponent hero
                Point coord = new Point(int.Parse(inputs[2]), int.Parse(inputs[3])); //coordonate
                int shieldLife = int.Parse(inputs[4]); // Ignore for this league; Count down until shield spell fades
                int isControlled = int.Parse(inputs[5]); // Ignore for this league; Equals 1 when this entity is under a control spell
                int health = int.Parse(inputs[6]); // Remaining health of this monster
                Point vector = new Point(int.Parse(inputs[7]), int.Parse(inputs[8])); //trajectory
                int nearBase = int.Parse(inputs[9]); // 0=monster with no target yet, 1=monster targeting a base
                int threatFor = int.Parse(inputs[10]); // Given this monster's trajectory, is it a threat to 1=your base, 2=your opponent's base, 0=neither

                Entity entity = new Entity(
                    id, type, coord, shieldLife, isControlled, health, vector, nearBase, threatFor
                );

                switch (type)
                {
                    case Global.TYPE_MONSTER:
                        monsters.Add(entity);
                        break;

                    case Global.TYPE_MY_HERO:
                        myHeroes.Add(entity);
                        break;

                    case Global.TYPE_OP_HERO:
                        oppHeroes.Add(entity);
                        break;
                }
            }
            int heroAttackId = 1;
            if (mainBase.Mana > Global.MAX_MANA)
                canAttack = true;
            while (true)
            {
                Action action = new Action()
                {
                    Text = null,
                    Value = 0,
                    Hero = null,
                    Monster = null,
                };
                int a = -1;
                foreach (Entity hero in myHeroes)
                {
                    a++;
                    if (hero.HasAction)
                        continue;
                    action.Guard( hero, mainBase, canAttack, a);
                    action.SpellWindDefend( hero, monsters, mainBase);
                    action.Farming( hero, monsters, mainBase, canAttack);
                    action.Defend( hero, monsters, mainBase);
                    if (canAttack && a == heroAttackId)
                    {
                        action.Attack( hero, mainBase);
                    }
                    if (mainBase.Mana >= Global.MANA_DEFEND)
                    {
                        action.SpellShield( hero, monsters, mainBase);
                        action.SpellWindAttack( hero, monsters, mainBase);
                        action.SpellMonsterControl( hero, monsters, mainBase);
                        action.SpellOpControl( hero, monsters, oppHeroes, mainBase);
                        action.SpellWindOp( hero, monsters, oppHeroes, mainBase);
                    }
                }
                if (action.Hero == null)
                    break;
                if (action.Monster != null)
                    action.Monster.IsTarget = true;
                if (action.Text.Contains("SPELL"))
                    mainBase.Mana -= 10;
                action.Hero.HasAction = true;
                action.Hero.Action = action.Text;
            }
            foreach (Entity hero in myHeroes)
            {
                Console.WriteLine(hero.Action);
            }
        }
    }

    
}
