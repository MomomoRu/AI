using UnityEngine;
using System.Collections;

public static class BehaviorFunc
{
	public static bool InDistance(GameObject refObj, GameObject targetObj, float compareValue)
	{
		if (refObj && targetObj)
		{
			float dist = CharacterUtility.GetDistanceWithoutRadius(refObj, targetObj.transform.position);
			return (dist < compareValue);
		}
		return false;
	}
	
	public static void FaceDirection(Transform refTransform, Vector3 faceDir, float rotateSpeed)
	{
		faceDir.y = 0.0f;
		if (faceDir != Vector3.zero)
		{
			Quaternion targetRotation = Quaternion.LookRotation(faceDir);
			refTransform.rotation = Quaternion.RotateTowards(refTransform.rotation, targetRotation, Time.deltaTime * rotateSpeed);
		}
	}
	
	public static void FaceTarget(Transform refTransform, GameObject targetObj, float rotateSpeed)
	{
		if (targetObj)
		{
			Vector3 faceDir = targetObj.transform.position - refTransform.position;
			FaceDirection(refTransform, faceDir, rotateSpeed);
		}
	}
	
	public static void MoveByRootMotion(NavMeshAgent navAgent)
	{
		if (navAgent)
		{	
			// use root motion to control move speed, but speed must large than 0 for rotation.
			navAgent.speed = 0.0005f;
		}
	}
	
	public static void UpdateMove(NavMeshAgent navAgent, ref Vector3 preDesiredVelocity, Transform refTransform, float rotateSpeed)
	{
		// Smooth disired velocity
		Vector3 desiredVelocity = Vector3.Lerp(preDesiredVelocity, navAgent.desiredVelocity, 0.5f);
		preDesiredVelocity = navAgent.desiredVelocity;
		// Facing moving dir
		FaceDirection(refTransform, desiredVelocity, rotateSpeed);
		
		MoveByRootMotion(navAgent);
	}

	//------------------------------------------------------------------------------------------
	// for behavior custom move 
	//------------------------------------------------------------------------------------------
	public static void SetCustomMoveTargetObject(GameObject gameObj, CustomMoveInfo info, AIBrain aiBrain)
	{
		switch (info.targetType)
		{
			case AITargetType.Myself:
				info.targetGameObject = gameObj;
				break;
				
			case AITargetType.AtkTarget:
				info.targetGameObject = aiBrain.AtkTargetObj;
				break;
				
			case AITargetType.Player:
				info.targetGameObject = LevelData.Instance.GetPlayerObj();
				break;
		}		
	}

	public static void ComputeCustomMoveDestination(GameObject gameObj, CustomMoveInfo info, AIBrain aiBrain)
	{
		Vector3 selfPos = gameObj.transform.position;
		Vector3 selfDir = gameObj.transform.forward;	
		Vector3 basePos = selfPos;
		Vector3 baseDir = selfDir;
		info.moveTargetPos = selfPos;
		
		try 
		{
			switch (info.moveBasePosType)
			{
				case AITargetType.Myself:
					basePos = selfPos;
					break;
					
				case AITargetType.Player:
				case AITargetType.AtkTarget:
				case AITargetType.UserSpecify:
					basePos = info.targetGameObject.transform.position;
					break;
					
				case AITargetType.SpawnPosition:
					break;
			}
			
			switch (info.moveBaseDirType)
			{
				case MoveBaseDir.MyselfFacing:
					baseDir = selfDir;
					break;
					
				case MoveBaseDir.MyselfToTarget:
					baseDir = info.targetGameObject.transform.position - selfPos;
					break;
					
				case MoveBaseDir.TargetToMyself:
					baseDir = selfPos - info.targetGameObject.transform.position;
					break;
					
				case MoveBaseDir.Unit:
					baseDir = new Vector3(1.0f, 0.0f, 1.0f);
					break;
			}			
		} 
		catch (System.NullReferenceException e) 
		{
			info.finish = true; 	//Goto next state
		}
		
		int testCount = 0;
		float moveDist;
		float moveAngle;
		Vector3 targetPos = selfPos;
		while (CharacterUtility.GetDistanceWithoutRadius(gameObj, targetPos) < info.minDistToDest && testCount < 100)
		{
			moveAngle = info.moveAngleMax;
			if (info.moveAngleMax != info.moveAngleMin)
			{
				moveAngle = Random.Range(info.moveAngleMin, info.moveAngleMax);
			}
			
			moveDist = info.moveDistMax;
			if (info.moveDistMin != info.moveDistMax)
			{
				moveDist = Random.Range(info.moveDistMin, info.moveDistMax);
			}		
			
			info.moveTimer = info.moveTimeMax;
			if (info.moveTimeMin != info.moveTimeMax)
			{
				info.moveTimer = Random.Range(info.moveTimeMin, info.moveTimeMax);
			}				
			
			Vector3 moveDir = Vector3.Normalize(Quaternion.AngleAxis(moveAngle, Vector3.up) * baseDir);
			Vector3 moveVec = moveDist * moveDir;
			targetPos = basePos + moveVec;
			
			info.facingDir = moveDir;				
			testCount++;
		}
		
		info.moveTargetPos = targetPos;

		if (info.faceMoveDir)
		{
			if (info.facingDir != Vector3.zero)
			{
				gameObj.transform.rotation = Quaternion.LookRotation(info.facingDir);
			}
		}
	}

	public static void SetCustomMoveDestination(CustomMoveInfo info, NavMeshAgent navAgent)
	{
		if (navAgent != null)
		{
			navAgent.SetDestination(info.moveTargetPos);
		}
	}

	public static void UpdateCustomMove(GameObject gameObj, CustomMoveInfo info, NavMeshAgent navAgent, float rotateSpeed, ref Vector3 preDesiredVelocity)
	{
		if (!info.character.isDying && !NavMeshFunc.ReachDest(navAgent))
		{
			if (info.faceMoveDir)
			{
				FaceDirection(gameObj.transform, info.facingDir, rotateSpeed);
				MoveByRootMotion(navAgent);
			}
			else
			{
				UpdateMove(navAgent, ref preDesiredVelocity, gameObj.transform, rotateSpeed);
			}
		}
		else
		{
			navAgent.velocity = Vector3.zero;
			info.finish = true;
		}
	}	
}
