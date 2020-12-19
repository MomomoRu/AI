using UnityEngine;
using System.Collections;

[System.Serializable]
public class BrainBehaviorInfo
{
	public AnimatorParameterEnum				animatorParameter = AnimatorParameterEnum.Null;
	public float 								rotateSpeed = 0.0f;
}

public class BrainBehavior
{
	private AIBrain.AIState						state;
	private Character							character;
	private Animator							animator;
	private CharacterLocomotion 				locomotion;
	private NavMeshAgent						navAgent;
	private AnimatorParameterEnum				animatorParameter = AnimatorParameterEnum.Null;

	private float rotateSpeed = 0.0f;
	public float RotateSpeed
	{
		get { return rotateSpeed; }
		set { rotateSpeed = value; }
	}

	private int	animStateHash = 0;
	public int AnimStateHash
	{
		get
		{
			if (animStateHash == 0)
			{
				switch(state)
				{
					case AIBrain.AIState.SPAWN:
						animStateHash = AnimatorStateHashName.Spawn;
						break;

					case AIBrain.AIState.IDLE:
					case AIBrain.AIState.WAIT:
						animStateHash = AnimatorStateHashName.Idle;
						break;

					case AIBrain.AIState.GO_TO_TARGET:
					case AIBrain.AIState.WANDER:
						animStateHash = AnimatorStateHashName.Walk;
						break;

					case AIBrain.AIState.ATTACK:
						animStateHash = AnimatorStateHashName.Attack;
						break;

					case AIBrain.AIState.HIT_REACT:
						animStateHash = AnimatorStateHashName.HitReact;
						break;

					case AIBrain.AIState.HIT_LIFT:
						animStateHash = AnimatorStateHashName.HitLift;
						break;

					case AIBrain.AIState.STUN:
						animStateHash = AnimatorStateHashName.Stun;
						break;

					case AIBrain.AIState.DEATH:
						animStateHash = AnimatorStateHashName.Death;
						break;

					default:
						animStateHash = AnimatorStateHashName.Idle;
						break;
				}
			}
			return animStateHash;
		}
	}

	private int	animatorParameterID = 0;
	public int AnimatorParameterID
	{
		get	
		{
			if (animatorParameterID == 0 && animatorParameter!= AnimatorParameterEnum.Null)
			{
				string animatorParameterString = CommonFunc.GetStringValue(animatorParameter);
				animatorParameterID = Animator.StringToHash(animatorParameterString);
			}
			return animatorParameterID;
		}
	}

	bool UseParameterForStateTransition()
	{
		return (animatorParameter != AnimatorParameterEnum.Null);
	}

	public BrainBehavior(AIBrain.AIState state, Character character, Animator anim, CharacterLocomotion locomotion, NavMeshAgent agent,
	                     AnimatorParameterEnum animPara = AnimatorParameterEnum.Null, 
	                     float rotateSpeed = 0.0f)
	{
		this.state = state;
		this.character = character;
		this.animator = anim;
		this.locomotion = locomotion;
		this.navAgent = agent;
		this.animatorParameter = animPara;
		this.rotateSpeed = rotateSpeed;
	}
	
	public void OnEnter()
	{
		if (animator != null)
		{
			if (UseParameterForStateTransition())
			{
				animator.SetBool(AnimatorParameterID, true);
			}
			else
			{
				locomotion.DoAnimState(AnimStateHash);
			}
		}

		ModifyAvoidancePriority();
	}

	public void OnExit()
	{
		if (animator != null)
		{
			if (UseParameterForStateTransition())
			{
				animator.SetBool(AnimatorParameterID, false);
			}
		}
	}

	void ModifyAvoidancePriority() 
	{
		bool separatePriority = (state == AIBrain.AIState.GO_TO_TARGET || state == AIBrain.AIState.WANDER);
		AvoidancePriorityDivider.Instance.SetupAvoidancePriority(navAgent, character, separatePriority);
	}
}

