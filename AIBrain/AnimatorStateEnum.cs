using UnityEngine;
using System.Collections;

public class AnimatorStateHashName
{
	public static int HubLaunch				= Animator.StringToHash("Base Layer.HubLaunch");
	public static int HubIdleBase			= Animator.StringToHash("Base Layer.HubIdleBase");
	public static int BattleLaunch			= Animator.StringToHash("Base Layer.BattleLaunch");
	public static int Spawn					= Animator.StringToHash("Base Layer.Spawn");
	public static int Idle 					= Animator.StringToHash("Base Layer.Idle");
	public static int Walk 					= Animator.StringToHash("Base Layer.Walk");
	public static int Move 					= Animator.StringToHash("Base Layer.Move");
	public static int Attack				= Animator.StringToHash("Base Layer.Attack");
	public static int Stun 					= Animator.StringToHash("Base Layer.Stun");
	public static int Dash 					= Animator.StringToHash("Base Layer.Dash");
	public static int HitReact 				= Animator.StringToHash("Base Layer.HitReact");
	public static int HitLift 				= Animator.StringToHash("Base Layer.HitLift");
	public static int GetUp 				= Animator.StringToHash("Base Layer.GetUp");
	public static int Death					= Animator.StringToHash("Base Layer.Death");
	public static int Summon 				= Animator.StringToHash("Base Layer.Summon");
	public static int Deactivate 			= Animator.StringToHash("Base Layer.Deactivate");
	public static int Unleash 				= Animator.StringToHash("Base Layer.Unleash");
	public static int SprintToFinalSkill 	= Animator.StringToHash("Base Layer.SprintToFinalSkill");
	public static int FinalSkill1 			= Animator.StringToHash("Base Layer.FinalSkill1");
	public static int Leave		 			= Animator.StringToHash("Base Layer.Leave");
}

public class AnimatorParameterHash
{
	public static int Idle					= Animator.StringToHash("Idle");
	public static int Move					= Animator.StringToHash("Move");
	public static int Attack				= Animator.StringToHash("Attack");
	public static int HitReact				= Animator.StringToHash("HitReact");
	public static int HitPartial 			= Animator.StringToHash("HitPartial");
	public static int MirrorAnim			= Animator.StringToHash("MirrorAnim");
	public static int MirrorHitReact		= Animator.StringToHash("MirrorHitReact");
	public static int MirrorRotate			= Animator.StringToHash("MirrorRotate");
	public static int Death					= Animator.StringToHash("Death");
	public static int FrontBackWeight		= Animator.StringToHash("FrontBackWeight");
    public static int RightLeftWeight       = Animator.StringToHash("RightLeftWeight");
    public static int IdleRotateWeight      = Animator.StringToHash("IdleRotateWeight");
}

public enum AnimatorParameterEnum
{
	[StringValue("Null")] 					Null,
	[StringValue("Idle")] 					Idle,
	[StringValue("Move")] 					Move,
	[StringValue("Attack")] 				Attack,
	[StringValue("HitReact")] 				HitReact,
	[StringValue("HitPartial")] 			HitPartial,
	[StringValue("MirrorAnim")] 			MirrorAnim,
	[StringValue("MirrorHitReact")] 		MirrorHitReact,
	[StringValue("Death")] 					Death
}

