using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
	public enum MoveFacingType
	{
		FacingTarget = 0,
		FacingMoveDir = 1,
	}

    [ActionCategory("AI")]
    public class BehaviorMove : BehaviorBase
    {
		//=====================================================================
		[ActionSection("Move Setting")]

		[Tooltip("Select target to compute distance.")]
		public AITargetType targetType = AITargetType.AtkTarget;
		
		[Tooltip("When target type = UserSpecify, need assgin this.")]
		public FsmGameObject target;

		protected GameObject targetObj;
		protected Transform targetTransform;
		protected Vector3 moveTargetPos;

		// about control move all directions motion paramter:
		float 	refreshMotionTime = 0.2f;
		float 	refreshMotionTimer = 0.0f;
		float 	refresMotionDistSquareThreshold = 0.25f;

		#region BehaviorBase Override method
		protected override void ModifyAvoidancePriority() 
		{
			AvoidancePriorityDivider.Instance.SetupAvoidancePriority(navAgent, character, true);
		}
		#endregion

		protected void SetTargetObject()
		{
			switch (targetType)
			{
				case AITargetType.Myself:
					targetObj = Owner;
					break;
					
				case AITargetType.AtkTarget:
					targetObj = (aiBrain) ? aiBrain.AtkTargetObj : null;
					break;
					
				case AITargetType.Player:
					targetObj = LevelData.Instance.GetPlayerObj();
					break;
					
				case AITargetType.UserSpecify:
					targetObj = target.Value;
					break;
			}

			if (targetObj)
			{
				targetTransform = targetObj.transform;
			}
		}

		protected void SetDestination(Vector3 pos)
		{
			if (navAgent != null)
			{
				navAgent.SetDestination(pos);
			}
		}

		protected void SetDestination(GameObject obj)
		{
			if (obj != null)
			{
				SetDestination(obj.transform.position);
			}
		}

		protected bool ArrivalDestination(float distThreshold = 0.5f)
		{
			if (navAgent != null)
			{
				return (navAgent.remainingDistance < distThreshold);
			}
			return false;
		}

		public override void OnEnter()
		{
			base.OnEnter();
			
			// reset parameters :
			refreshMotionTimer = 0.0f;
			coverMotionTimer = 0.0f;
		}

		// Code that runs when exiting the state.
		public override void OnExit()
		{
			base.OnExit();

			StoreMoveMotionWeight(animator.GetFloat(AnimatorParameterHash.FrontBackWeight), animator.GetFloat(AnimatorParameterHash.RightLeftWeight));

			if (Owner.activeInHierarchy && navAgent && navAgent.hasPath)
			{
				navAgent.Stop();
			}
		}

		#region control all directions move motion
		protected void SetComputeMotionConfig()
		{
			SetConverMotionConfig();
			refreshMotionTimer = 0.0f;
		}
		
		protected void RecomputeMotion()
		{
			SetComputeMotionConfig();
			ComputeMotion();
			ConverMotion();
		}

		protected void ComputeMotion()
		{
			Vector3 faceMoveDir = moveTargetPos - ownerTransform.position;
			if (faceMoveDir.sqrMagnitude < refresMotionDistSquareThreshold)
			{
				goalFrontBackWeight = 0.0f;
				goalRightLeftWeight = 0.0f;
				return;	// too close to destination.
			}
			
			Vector3 facingDir = ownerTransform.forward;						// assume current already face target object.
			Vector3 cross = Vector3.Cross(facingDir, faceMoveDir);
			float angle = Vector3.Angle(facingDir, faceMoveDir);			// compute angle from facing to moveDir.			
			if (cross.y < 0.0f)
			{
				// adjust angle value.
				angle = -angle;
			}
			
			Vector3 relativeDir = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.right;
			goalFrontBackWeight = relativeDir.x;
			goalRightLeftWeight = relativeDir.z;
		}

		protected override void UpdateMotion()
		{
			if (moveWithFaceTarget)
			{
				refreshMotionTimer += Time.deltaTime;
				
				if (refreshMotionTimer > refreshMotionTime)
				{
					RecomputeMotion();
				}
				else if (converMotion)
				{
					ConverMotion();
				}
			}
			else
			{
				if (converMotion)
				{
					ConverMotion();
				}
			}
		}
		#endregion
    }
}