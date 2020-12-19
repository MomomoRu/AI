using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("AI")]
    public class BehaviorIdle : BehaviorBase
    {
		[Tooltip("Use facing target function by partial rotate or not.")]
		public FsmBool useParitalRotate = false;
		
		[Tooltip("Whether always facing target when attack.")]
		public FsmBool alwaysFacingTarget = true;

        private int rotateID = Animator.StringToHash("Rotate");
        private float rotateRatio = 0f;
        private float rotateBlendingRate = 5f; //  (1/rotateBlendingRate) == Blending Time in Sec.

        // Code that runs on entering the state.
        public override void OnEnter()
        {
            base.OnEnter();
		
			if (alwaysFacingTarget.Value)
				FaceAtkTarget(useParitalRotate.Value);
			
			rotateRatio = 0f;
            animator.SetFloat(rotateID, 0.0f);

			SetConverMotionConfig();
        }
	
        // Code that runs every frame.
        public override void OnUpdate()
        {
            base.OnUpdate();
			RotateBySpeed();
        }

		public override void OnExit()
		{
			base.OnExit();			
			StoreMoveMotionWeight();
		}

		private void RotateBySpeed()
		{
			// adjust rotate parameter, but rarely used.
			Vector3 oriForward = ownerTransform.forward;
			if (alwaysFacingTarget.Value)
				FaceAtkTarget(useParitalRotate.Value);
			
			Vector3 newForward = ownerTransform.forward;
			float diffAngle = Mathf.Abs(Vector3.Angle(oriForward, newForward));
			float diffSpeed = diffAngle / Time.deltaTime;
			if (diffSpeed > 30f)
			{
				rotateRatio += rotateBlendingRate * Time.deltaTime;
				rotateRatio = Mathf.Min(1.0f, rotateRatio);
			} 
			else if (diffSpeed < 10f)
			{
				rotateRatio -= rotateBlendingRate * Time.deltaTime;
				rotateRatio = Mathf.Max(0.0f, rotateRatio);
			}
			animator.SetFloat(rotateID, rotateRatio);
		}
		
		protected override void ModifyAvoidancePriority() 
		{
			AvoidancePriorityDivider.Instance.SetupAvoidancePriority(navAgent, character);
		}
    }
}