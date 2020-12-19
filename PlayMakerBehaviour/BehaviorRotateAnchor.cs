using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
	[ActionCategory("AI")]
	public class BehaviorRotateAnchor : FsmStateAction
	{	
		private AIBrain aiBrain;

		[Tooltip("Send event when rotate anchor stored.")]
		public FsmEvent finished;

		public override void Awake()
		{
			if (Owner == null)
				return;
			
			aiBrain = Owner.GetComponent<AIBrain>();
		}
		
		// Code that runs every frame.
		public override void OnUpdate()
		{
			aiBrain.anchorRotation = Owner.transform.forward;

			if (aiBrain.anchorRotation != null) 
			{
				Fsm.Event(finished);
			}
		}
	}
}