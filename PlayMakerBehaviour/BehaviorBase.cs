using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("AI")]
    public class BehaviorBase : FsmStateAction
    {
        //=====================================================================
        [ActionSection("State Info")]

        [Tooltip("This state CD time.")]
        public FsmBool CDTimerEnable = true;
        public FsmString CDTimeGroup = "";
        public FsmFloat CDTime = 0.0f;

		//=====================================================================
        [ActionSection("Animator Control")]

        [Tooltip("Set Animator Bool Parameter : enter = true, exit = false.")]
        public FsmString animatorParameter;

        [Tooltip("Send Animator Trigger.")]
        public FsmString animatorTrigger;
	
        //=====================================================================
        [ActionSection("Behavior Done")]
	
        [Tooltip("This Behavior finish when this state animation finish.")]
        public FsmString finishStateName;
	
        [Tooltip("Send event when finishState animation end.")]
        public FsmEvent animFinishEvent;
	
        //=====================================================================
        [ActionSection("Rotate Parameters")]
	
        [Tooltip("Value to control rotate speed.")]
        public FsmFloat rotateSpeed = 2.0f;

		//=====================================================================
        [ActionSection("Partial Rotate Parameters")]

		[Tooltip("Value to control parital rotate speed.")]
		public FsmFloat partialRotateSpeed = 150f;	
	
        //=====================================================================
        // self object info:
        protected AIBrain 		aiBrain;
        protected Animator		animator;
        protected Character		character;
		protected Transform 	ownerTransform;
        protected NavMeshAgent 	navAgent;
        protected int			finishStateNameId = 0;	
        protected int			preStateLoopCount = 0;	
        protected int 			animatorParameterID = 0;
        protected int 			animatorTriggerID = 0;
        protected bool 			enteredFinishState = false;
        protected bool 			useAnimatorPara = false;
        protected bool 			useAnimatorTrigger = false;
        protected bool 			recordExitTime = false;

		// about control move all directions motion paramter:
		protected bool 			moveWithFaceTarget = false;
		protected float 		goalFrontBackWeight = 0.0f;		// weight for from ~ back.
		protected float 		goalRightLeftWeight = 0.0f;		// weight for right ~ left.
		protected float 		orgFrontBackWeight = 0.0f;		// weight for from ~ back.
		protected float 		orgRightLeftWeight = 0.0f;		// weight for right ~ left.
		protected float 		coverMotionTime = 0.2f;
		protected float 		coverMotionTimer = 0.0f;
		protected float 		coverMotionTimeInverse = 0.0f;
		protected bool 			converMotion = false;

		/// <summary>
		/// Value of animator float parameter "MirrorRotate" in range[-90, 90].
		/// if value > 0, animator rotates to left.
		/// if value < 0, animator rotates to right.
		/// </summary>
		protected float			currentAnimatorRotateAngle = 0f;

		protected virtual void ModifyAvoidancePriority() { }
	
        public override void Reset()
        {
            animFinishEvent = null;
            enteredFinishState = false;
        }

        public override void Awake()
        {
            if (Owner == null)
            {
                return;
            }

            aiBrain = Owner.GetComponent<AIBrain>();
            animator = Owner.GetComponent<Animator>();
            character = Owner.GetComponent<Character>();
            navAgent = Owner.GetComponent<NavMeshAgent>();
			ownerTransform = Owner.transform;
			moveWithFaceTarget = false;
			coverMotionTimeInverse = 1.0f / coverMotionTime;

            // Control position & rotation by behavior, not NavMeshAgent.
			if (navAgent)
			{
	            navAgent.updateRotation = false;
			}
		
            if (animator != null)
            {
                finishStateNameId = Animator.StringToHash(finishStateName.Value);

                useAnimatorPara = !string.IsNullOrEmpty(animatorParameter.Value);
                useAnimatorTrigger = !string.IsNullOrEmpty(animatorTrigger.Value);
                if (useAnimatorPara)
                {
                    animatorParameterID = Animator.StringToHash(animatorParameter.Value);
                }
                if (useAnimatorTrigger)
                {
                    animatorTriggerID = Animator.StringToHash(animatorTrigger.Value);
                }
            }

            // init CD time table.
            aiBrain.SetStateCDTimeGroup(State.Name, CDTimeGroup.Value);
            aiBrain.SetStateCDTime(State.Name, CDTime.Value);
            recordExitTime = (CDTime.Value > 0.0f);
            if (recordExitTime)
            {
                // let CD time condition not satisfy when this state awake.
                aiBrain.ResetExitStatePassTime(State.Name, 0.0f);
                aiBrain.SetStateCDTimerSwitch(State.Name, CDTimerEnable.Value);
            }
        }
	
        // Code that runs on entering the state.
        public override void OnEnter()
        {			
            enteredFinishState = false;
		
			currentAnimatorRotateAngle = animator.GetFloat (AnimatorParameterHash.MirrorRotate);

            // control position & rotation by behavior, not NavMeshAgent.
			if (navAgent)
			{	
				navAgent.updateRotation = false;
			}
		
            if (animator != null)
            {
                // set animatorParameter.
                if (useAnimatorPara)
                {
                    animator.SetBool(animatorParameterID, true);
                }
                if (useAnimatorTrigger)
                {
                    animator.SetTrigger(animatorTriggerID);
                }

                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);	
                if (stateInfo.nameHash == finishStateNameId)
                {
                    preStateLoopCount = (int)Mathf.Floor(stateInfo.normalizedTime);	
                    enteredFinishState = true;
                } 
				else
                {
                    preStateLoopCount = 0;
                    enteredFinishState = false;
                }
            }

			ModifyAvoidancePriority();
        }

        // Code that runs every frame.
        public override void OnUpdate()
        {
            if (!FsmEvent.IsNullOrEmpty(animFinishEvent))
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);			
                if (stateInfo.nameHash == finishStateNameId)
                {
                    enteredFinishState = true;
				
                    int loopCount = (int)Mathf.Floor(stateInfo.normalizedTime);			
                    if (loopCount > preStateLoopCount)
                    {
                        Fsm.Event(animFinishEvent);
                        preStateLoopCount = loopCount;
                    }
                } 
				else
                {
                    if (enteredFinishState)
                    {
                        Fsm.Event(animFinishEvent);
                        preStateLoopCount = 0;
                    }
                }
            }
        }

		// Code that runs every frame.
		public override void OnLateUpdate()
		{
			UpdateMotion();
		}
	
        // Code that runs when exiting the state.
        public override void OnExit()
        {
            if (animator != null)
            {
                // reset animatorParameter.
                if (useAnimatorPara)
                {
                    animator.SetBool(animatorParameterID, false);
                }
				if (useAnimatorTrigger)
				{
					animator.ResetTrigger(animatorTriggerID);
				}
            }

            if (recordExitTime)
            {
                aiBrain.ResetExitStatePassTime(State.Name);
            }
        }	

		protected void MoveByRootMotion()
		{
			BehaviorFunc.MoveByRootMotion(navAgent);
		}
		
		protected void FaceDirection(Vector3 faceDir)
        {
			BehaviorFunc.FaceDirection(ownerTransform, faceDir, rotateSpeed.Value);
        }
	
		/// <summary>
		/// Faces the atk target by transform or animator.
		/// </summary>
        protected void FaceAtkTarget(bool useAnimator = false)
        {
			if (useAnimator) 
			{
				AnimatorRotateTowardAtkTarget();
			} 
			else
			{
				BehaviorFunc.FaceTarget(ownerTransform, aiBrain.AtkTargetObj, rotateSpeed.Value);
			}
        }

		#region control all directions move motion
		protected void StoreMoveMotionWeight(float frontBackWeight = 1.0f, float rightLeftWeight = 0.0f)
		{
			// parameter default : original motion is facing Front direction.
			aiBrain.FrontBackWeight = frontBackWeight;
			aiBrain.RightLeftWeight = rightLeftWeight;
		}

		protected void SetConverMotionConfig(bool setGoalParam = true, float goalFrontBackWeight = 1.0f, float goalRightLeftWeight = 0.0f)
		{
			converMotion = true;
			coverMotionTimer = 0.0f;
			orgFrontBackWeight = aiBrain.FrontBackWeight;
			orgRightLeftWeight = aiBrain.RightLeftWeight;

			if (setGoalParam)
			{
				// default : conver farward dir to Front direction.
				this.goalFrontBackWeight = goalFrontBackWeight;
				this.goalRightLeftWeight = goalRightLeftWeight;
			}
		}
		
		protected void ConverMotion()
		{
			if (coverMotionTimer > coverMotionTime)
			{
				aiBrain.FrontBackWeight = goalFrontBackWeight;
				aiBrain.RightLeftWeight = goalRightLeftWeight;
				converMotion = false;
			}
			
			float weight = coverMotionTimer * coverMotionTimeInverse;
			aiBrain.FrontBackWeight = Mathf.Lerp(orgFrontBackWeight, goalFrontBackWeight, weight);
			aiBrain.RightLeftWeight = Mathf.Lerp(orgRightLeftWeight, goalRightLeftWeight, weight);
			
			animator.SetFloat(AnimatorParameterHash.FrontBackWeight, aiBrain.FrontBackWeight);
			animator.SetFloat(AnimatorParameterHash.RightLeftWeight, aiBrain.RightLeftWeight);
			
			coverMotionTimer += Time.deltaTime;
		}

		abstract protected void UpdateMotion()
		{
			if (converMotion)
			{
				ConverMotion();
			}
		}
		#endregion

		/// <summary>
		/// Animators the parital rotate by angle.
		/// </summary>
		/// <param name="angle">Angle.</param>
		protected void PartialRotateTowardAngle(float angle)
		{
			float presentMirrorRotateAngle;
			float angleDiff = angle - currentAnimatorRotateAngle;
			
			if (angleDiff > 0)
				presentMirrorRotateAngle = currentAnimatorRotateAngle + Time.deltaTime * partialRotateSpeed.Value; // rotate to right
			else 
				presentMirrorRotateAngle = currentAnimatorRotateAngle - Time.deltaTime * partialRotateSpeed.Value; // rotate to left
			
			currentAnimatorRotateAngle = Mathf.Clamp(presentMirrorRotateAngle, 
			                                       currentAnimatorRotateAngle - Mathf.Abs(angleDiff), 
			                                       currentAnimatorRotateAngle + Mathf.Abs(angleDiff));
			
			animator.SetFloat(AnimatorParameterHash.MirrorRotate, currentAnimatorRotateAngle);
		}

		/// <summary>
		/// Rotates the toward atk target by animator parameter "MirrorRotate".
		/// </summary>
		protected void AnimatorRotateTowardAtkTarget() 
		{
			PartialRotateTowardAngle(SignedAngleTowardAtkTarget());
		}

		/// <summary>
		/// Calculated the signed angle between owner.forward and owner-to-atkTarget vector.
		/// </summary>
		/// <returns>The angle toward atk target.</returns>
		protected float SignedAngleTowardAtkTarget() {
			return SignedAngleTowardTarget(Owner, aiBrain.AtkTargetObj);
		}

		/// <summary>
		/// Calculated the signed angle between owner.forward and owner-to-target vector.
		/// </summary>
		/// <returns>The angle toward target.</returns>
		protected float SignedAngleTowardTarget(GameObject ownerObj, GameObject targetObj, float defaultValue = 0f) 
		{
			if (targetObj)
			{
				Vector3 faceDir = targetObj.transform.position - ownerObj.transform.position;
				Vector3 forward = ownerObj.transform.forward;
				faceDir.y = 0;
				forward.y = 0;
				return MathFunc.SignedAngle(forward, faceDir, Vector3.up);
			}

			return defaultValue;
		}
    }
}
