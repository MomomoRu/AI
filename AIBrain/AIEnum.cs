using UnityEngine;
using System.Collections;

public enum CharacterStatusType
{
    IsDying,
    IsAlive
}

public enum AITargetType
{
    Myself,
    Player,
    Foe,
    SpawnPosition,
    UserSpecify,
	AtkTarget,
	TouchPoint,
}

public enum AIDistanceType
{
    AlertDist,	
    ChaseDist,
    AttackDist,
    AttackDist2,
    AttackDist3,
    AttackDist4,
    AttackDist5,
	None,
	UserSpecify,
}

public enum MoveBaseDir
{
    MyselfFacing,
    MyselfToTarget,
    TargetToMyself,
    Unit
}

public enum ValueCompareType
{
	GreaterThan,
	LessThan,
	Egual,
}

public enum EliteType
{
	Normal = 0,
	Elite = 1,
	MiniBoss = 2,
	Boss = 3,
}
