using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
	[ActionCategory("AI")]
	public class BehaviorRotateTo : BehaviorBase
	{	
		[Tooltip("The target angle between the value stored by BehaviorRotateAnchor")]
		public float angle;

		private Vector3 targetRotation;

		// Code that runs on entering the state.
		public override void OnEnter()
		{
			base.OnEnter();

			if (Owner == null)
				return;

			targetRotation = GetTargetRotation(angle);
		}

		// Code that runs every frame.
		public override void OnUpdate()
		{
			base.OnUpdate();
			
			FaceDirection(targetRotation);
		}
		
		public override void OnExit()
		{
			base.OnExit();
		}

		public virtual Vector3 GetTargetRotation(float angle)
		{
			Vector3 originRotate = aiBrain.anchorRotation;

			return Quaternion.AngleAxis(angle, Vector3.up) * originRotate;
		}
	}
}