using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace HutongGames.PlayMaker.Actions
{
	[ActionCategory("AI")]
	public class BehaviorCustomMove : BehaviorMove
	{
		public MoveFacingType 	facingType = MoveFacingType.FacingMoveDir;
		public AITargetType 	moveBasePosType = AITargetType.Myself;
		public MoveBaseDir 		moveBaseDirType = MoveBaseDir.MyselfFacing;
		
		public FsmFloat 		moveAngleMin;
		public FsmFloat 		moveAngleMax;
		public FsmFloat 		moveDistMin;
		public FsmFloat 		moveDistMax;
		public FsmFloat 		moveTimeMin;
		public FsmFloat 		moveTimeMax;
		public bool 			mirrorAngleRange = false;
		public bool 			faceMoveDir = false;
		private bool 			preForceStopMove = false;

		[Tooltip("Min distance from current position to new destination.")]
		public FsmFloat			minDistToDest;
		
		[Tooltip("Send event when arrive current destination.")]
		public FsmEvent			arriveDestEvent;
		
		[Tooltip("Send event when move time is up.")]
		public FsmEvent			moveTimeUpEvent;

		private float 			moveAngle;
		private float 			moveDist;
		private float 			moveTimer;
		private Vector3 		preDesiredVelocity;
		private Vector3			facingDir;

		#region PlayMaker life cycle methods

		public override void Awake()
		{
			base.Awake();
			moveWithFaceTarget = (facingType == MoveFacingType.FacingTarget);
		}
		
		// Code that runs on entering the state.
		public override void OnEnter()
		{
			preForceStopMove = aiBrain.ForceStopMove;
			if (aiBrain.ForceStopMove)
				return;

			base.OnEnter();

			SetTargetObject();
			SetCustomMoveTimer();
			SetCustomMoveDestination();
			SetDestination(moveTargetPos);
			UpdateMove();
			MoveByRootMotion();
			RecomputeMotion();
			
			Messenger.AddGlobalListener<GameObject>(MessageNameEunm.AttackTargetChanged, OnTargetChanged);
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

			moveTimer -= Time.deltaTime;		
			if (moveTimer <= 0)
			{
				Fsm.Event(moveTimeUpEvent);
				
				SetCustomMoveDestination();
				SetDestination(moveTargetPos);
			}
			
			UpdateMove();
		}
		
		// Code that runs when exiting the state.
		public override void OnExit()
		{
			base.OnExit();
			Messenger.RemoveGlobalListener<GameObject>(MessageNameEunm.AttackTargetChanged, OnTargetChanged);
		}
		
		public override void Reset()
		{
			target = null;
			moveAngleMin = -90.0f;
			moveAngleMax = 90.0f;
			moveDistMin = 3.0f;
			moveDistMax = 5.0f;
			moveTimeMin = 2.0f;
			moveTimeMax = 4.0f;
			minDistToDest = 3.0f;
			moveTimer = 0.0f;
		}	
		
		#endregion
		
		private void UpdateMove()
		{
			if (!(character.isDying || ArrivalDestination()))
			{
				if (moveWithFaceTarget)
				{
					BehaviorFunc.FaceTarget(ownerTransform, targetObj, rotateSpeed.Value);
				}
				else if (faceMoveDir)
				{
					FaceDirection(facingDir);
				}
				else
				{
					// Smooth disired velocity
					Vector3 desiredVelocity = (preDesiredVelocity + navAgent.desiredVelocity) * 0.5f;
					preDesiredVelocity = navAgent.desiredVelocity;
					
					// Facing moving dir
					FaceDirection(desiredVelocity);
				}
			}
			else
			{
				if (FsmEvent.IsNullOrEmpty(arriveDestEvent))
				{
					SetCustomMoveDestination();
					SetDestination(moveTargetPos);
				}
				else
				{
					navAgent.velocity = Vector3.zero;
					Fsm.Event(arriveDestEvent);
				}
			}
		}

		private void SetCustomMoveTimer()
		{
			moveTimer = moveTimeMax.Value;
			if (moveTimeMin != moveTimeMax)
			{
				moveTimer = Random.Range(moveTimeMin.Value, moveTimeMax.Value);
			}
		}

		private void SetCustomMoveDestination()
		{
			Vector3 selfPos = ownerTransform.position;
			Vector3 selfDir = ownerTransform.forward;	
			Vector3 basePos = selfPos;
			Vector3 baseDir = selfDir;		
			moveTargetPos = selfPos;
			
			try 
			{
				switch (moveBasePosType)
				{
					case AITargetType.Myself:
						basePos = selfPos;
						break;
						
					case AITargetType.Player:
					case AITargetType.AtkTarget:
					case AITargetType.UserSpecify:
						basePos = targetObj.transform.position;
						break;
						
					case AITargetType.SpawnPosition:
						break;
				}
				
				switch (moveBaseDirType)
				{
					case MoveBaseDir.MyselfFacing:
						baseDir = selfDir;
						break;
						
					case MoveBaseDir.MyselfToTarget:
						baseDir = targetObj.transform.position - selfPos;
						break;
						
					case MoveBaseDir.TargetToMyself:
						baseDir = selfPos - targetObj.transform.position;
						break;
						
					case MoveBaseDir.Unit:
						baseDir = new Vector3(1.0f, 0.0f, 1.0f);
						break;
				}
			} 
			catch (System.NullReferenceException e) 
			{
				Fsm.Event (arriveDestEvent); //Goto next state
			}
			
			int testCount = 0;
			Vector3 targetPos = selfPos;
			while (CharacterUtility.GetDistanceWithoutRadius(Owner, targetPos) < minDistToDest.Value && testCount < 100)
			{
				moveAngle = moveAngleMax.Value;
				if (moveAngleMax.Value != moveAngleMin.Value)
				{
					float rangeMin = moveAngleMin.Value;
					float rangeMax = moveAngleMax.Value;
					if (mirrorAngleRange)
					{
						bool randomSide = (Random.value > 0.5f);
						if (randomSide)
						{
							float temp = rangeMin;
							rangeMin = -rangeMax;
							rangeMax = -temp;
						}
					}
					moveAngle = Random.Range(rangeMin, rangeMax);
				}
				else 
				{
					if (mirrorAngleRange)
					{
						bool randomSide = (Random.value > 0.5f);
						if (randomSide)
						{
							moveAngle = -moveAngle;
						}
					}
				}
				
				moveDist = moveDistMax.Value;
				if (moveDistMin != moveDistMax)
				{
					moveDist = Random.Range(moveDistMin.Value, moveDistMax.Value);
				}
				
				Vector3 moveDir = Vector3.Normalize(Quaternion.AngleAxis(moveAngle, Vector3.up) * baseDir);
				Vector3 moveVec = moveDist * moveDir;
				targetPos = basePos + moveVec;
				facingDir = moveDir;

				testCount++;
			}
			
			moveTargetPos = targetPos;
			
			if (!moveWithFaceTarget && faceMoveDir)
			{
				if (facingDir != Vector3.zero)
				{
					ownerTransform.rotation = Quaternion.LookRotation(facingDir);
				}
			}
		}
		
		/// <summary>
		/// Callback function for MessageNameEnum.AttackTargetChanged Message
		/// </summary>
		/// <param name="targetObj">Target object.</param>
		private void OnTargetChanged(GameObject obj) 
		{
			targetObj = obj;
		}
	}
}