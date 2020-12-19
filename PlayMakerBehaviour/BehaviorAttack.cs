using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("AI")]
    public class BehaviorAttack : BehaviorBase
    {
		[Tooltip("Use facing target function by parital rotate or not.")]
		public FsmBool useParitalRotate = false;
		[Tooltip("Whether always facing target when attack.")]
        public FsmBool alwaysFacingTarget;
        public FsmFloat facingTargetTime = -1.0f;
        public FsmBool characterPush = true;

        private float remainFacingTargetTime = -1.0f;		
        private ObstacleAvoidanceType originalObstacleAvoidanceType;

        // Code that runs on entering the state.
        public override void OnEnter()
        {
            base.OnEnter();
		
			if (navAgent)
			{
				originalObstacleAvoidanceType = navAgent.obstacleAvoidanceType;
				if (characterPush.Value == false)
				{
					navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
				}
			}

            remainFacingTargetTime = facingTargetTime.Value;
			currentAnimatorRotateAngle = 0f;

			if (alwaysFacingTarget.Value || remainFacingTargetTime > 0.0f)
			{
				FaceAtkTarget(useParitalRotate.Value);
				remainFacingTargetTime -= Time.deltaTime;
			}

			MoveByRootMotion();
			SetConverMotionConfig();
        }
	
        private void UpdateTransform()
        {
            if (alwaysFacingTarget.Value || remainFacingTargetTime > 0.0f)
            {
				FaceAtkTarget(useParitalRotate.Value);				
				remainFacingTargetTime -= Time.deltaTime;
            } 
			else
            {
				ownerTransform.rotation = animator.rootRotation;
            }
        }

        // Code that runs every frame.
        public override void OnUpdate()
        {
            base.OnUpdate();
            UpdateTransform();
        }
	
        public override void OnExit()
        {
            base.OnExit();

			StoreMoveMotionWeight();
			if (navAgent)
			{
    	        navAgent.obstacleAvoidanceType = originalObstacleAvoidanceType;
			}
        }

		protected override void ModifyAvoidancePriority() 
		{
			AvoidancePriorityDivider.Instance.SetupAvoidancePriority(navAgent, character);
		}
    }
}