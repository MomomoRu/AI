using UnityEngine;
using Xpec;

namespace HutongGames.PlayMaker.Actions
{
	[ActionCategory("AI")]
	public class BehaviorGoToTarget : BehaviorMove
	{
		private Vector3 preDesiredVelocity;
		private bool preForceStopMove = false;
		private float delayUpdateTime = -1.0f;

		// Code that runs on entering the state.
		public override void OnEnter()
		{
			preForceStopMove = aiBrain.ForceStopMove;
			if (aiBrain.ForceStopMove)
			{
				return;
			}

			base.OnEnter();		

			SetConverMotionConfig();
			SetTargetObject();
			SetDestination(targetObj);
			UpdateMove();
			MoveByRootMotion();

			preDesiredVelocity = navAgent.desiredVelocity;
			delayUpdateTime = 1.0f;
		}
		
		private void UpdateMove()
		{
			if (converMotion)
				return;

			// Smooth disired velocity
			Vector3 desiredVelocity = (preDesiredVelocity + navAgent.desiredVelocity) * 0.5f;
			preDesiredVelocity = navAgent.desiredVelocity;
			// Facing moving dir
			FaceDirection(desiredVelocity);
		}
		
		// Code that runs every frame.
		public override void OnUpdate()
		{
			base.OnUpdate();

			if (aiBrain.ForceStopMove)
			{
				return;
			}
			else if (preForceStopMove)
			{
				// not force stop move any more, do OnEnter to reset enter state information.
				OnEnter();
			}
			preForceStopMove = aiBrain.ForceStopMove;

			// Do it in 5 FPS
			if (delayUpdateTime > 0.2f)
			{
				SetTargetObject();
				SetDestination(targetObj);
				delayUpdateTime = 0.2f * Random.value;
			}
			else
			{
				delayUpdateTime += Time.deltaTime;
			}

			UpdateMove();
        }
	}
}