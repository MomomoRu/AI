using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
	[ActionCategory("AI")]
	public class BehaviorRotateBack : BehaviorBase
	{	
		private Vector3 targetRotation;
		// Code that runs on entering the state.
		public override void OnEnter()
		{
			base.OnEnter();

			targetRotation = aiBrain.anchorRotation;
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
	}
}