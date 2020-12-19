using UnityEngine;
using System.Collections;
using HutongGames.PlayMaker;
using System.Collections.Generic;
using Xpec;

[System.Serializable]
public class ForceAttackFromHitReact
{	
    public bool 			attackWhenTimeOut = false;
    public float 			minTimeInHitReact = 1.0f;
    public float 			maxTimeInHitReact = 2.0f;
}

[System.Serializable]
public class CustomMoveInfo
{
	public bool 							enable = false;
	public AITargetType 					targetType = AITargetType.Player;	
	public AITargetType 					moveBasePosType = AITargetType.Myself;
	public MoveBaseDir 						moveBaseDirType = MoveBaseDir.MyselfFacing;
	public float 							moveAngleMin;
	public float 							moveAngleMax;
	public float 							moveDistMin;
	public float 							moveDistMax;
	public float 							moveTimeMin;
	public float 							moveTimeMax;
	public float							minDistToDest;
	public bool 							faceMoveDir = false;

	[HideInInspector] public Character 		character;
	[HideInInspector] public GameObject		targetGameObject;
	[HideInInspector] public Vector3		moveTargetPos;
	[HideInInspector] public float 			moveTimer;
	[HideInInspector] public Vector3		facingDir;
	[HideInInspector] public bool			finish = false;
}

[System.Serializable]
public class StateMachineSetting
{
	// for wait state :
	public float 				minWaitTime = 0.5f;
	public float 				maxWaitTime = 1.0f;

	public BrainBehaviorInfo 	idle;
	public BrainBehaviorInfo 	goToTarget;
	public BrainBehaviorInfo 	attack;
	public BrainBehaviorInfo 	wait;
	public BrainBehaviorInfo	wander;
	public CustomMoveInfo 		wanderSetting;

	[HideInInspector] public BrainBehavior[]	behaviours;
}

public class AIBrain : MonoBehaviour
{
	public enum ManagerType
	{
		AIBrain,
		PlayMaker,
	}
	
	public enum AIState
	{
		NULL,
		SPAWN,
		IDLE,
		GO_TO_TARGET,
		ATTACK,
		WAIT,
		WANDER,
		HIT_REACT,
		HIT_LIFT,
		STUN,
		DEATH,
		AIStateNum,
	}

	public ManagerType 		managerType = ManagerType.PlayMaker;

	// about find AI target :
	public bool				alwaysLookAtPlayer = true;
    public float 			alertDistance = 100.0f;
    public float 			chaseDistance = 4.0f;
    public float 			attackDistance = 1.5f;
    public float 			attackDistance2 = 1.5f;
    public float 			attackDistance3 = 1.5f;
    public float 			attackDistance4 = 1.5f;
    public float            attackDistance5 = 1.5f;
    public float            maxChaseTime = 5.0f;
    public bool             hitReactInAttack = true;
    public ForceAttackFromHitReact forceAttackFromHitReact;
	public StateMachineSetting fsmSetting;

    private PlayMakerFSM 	FSM;
    private EquipmentManager equipment;
	private List<ProjectileLauncher> equipmentProjectileLaunchers = new List<ProjectileLauncher>();
    private TargetSearch 	targetSearch = null;
    private Character		character;
    private Character		atkTargetCharacter;
    private Animator        animator;
    private ImpactPause     impactPause;
	private CharacterLocomotion locomotion;
	private NavMeshAgent	navAgent;

    private int 			animLoopCountAtDeath = 0;
    private bool 			checkAnimAtDeathFinish = false;
	private	float 			refreshAttackTargetCD = 0.0f;
	
	// AI Brain state machine :
	private AIState 		currState = AIState.NULL;
	private AIState 		nextState = AIState.SPAWN;
	private Vector3 		preDesiredVelocity;
	private float			delayFSMUpdateTime = 0.3f;
	private float			randomWaitTime = 0.3f;
	private float			stateTimer = 0.0f;
	private bool			enableBrainFSM = false;
	private bool			enforceReEnterState = false;
	
    // Brain state cd timer control :
    private Dictionary<string, bool> statesCDTimeSwitch = new Dictionary<string, bool>();	// <groupName, bool> record cd timer whether enable.
    private Dictionary<string, float> statesCDTime = new Dictionary<string, float>();		// <groupName, float>
    private Dictionary<string, float> exitStatePassTimes = new Dictionary<string, float>();	// <groupName, float>
    private List<string> exitStatesNames = new List<string>();								// <stateName> record exitStatePassTimes stateName for modify Dictionary value.
    private Dictionary<string, string> statesCDTimeGroupTable = new Dictionary<string, string>();	// <stateName, groupName> record each state corresponding cd timer group name. no cd group will set as state name.

    // FSM event name :
    private string          hitReactEvent = "HitReact";
    private string 			hitLiftEvent = "HitLift";
    private string 			deathEvent = "Death";
    private string 			atkTargetDeathEvent = "AtkTargetDeath";
    private	static Vector3	playerHeight = Vector3.up * 2.0f;

	/// <summary>
	/// The anchor rotation. Keep the information for rotation back function.
	/// </summary>
	[HideInInspector]
	public Vector3 			anchorRotation;

    private bool			AddListenerDone = false;
    private GameObject atkTargetObj;
    public GameObject AtkTargetObj
    {
        get { return atkTargetObj; }
        set
        { 
            atkTargetObj = value;

            if (atkTargetObj)
            {
                atkTargetCharacter = atkTargetObj.GetComponent<Character>();
                SetEquipmentTarget();
            }
        }
    }

    private float timeInHitReact = 0.0f;
    public float TimeInHitReact
    {
        get { return timeInHitReact; }
        set { timeInHitReact = value; }
    }

	private float frontBackWeight = 0.0f;	// animator front ~ back weight.
	public float FrontBackWeight
	{
		get { return frontBackWeight; }
		set { frontBackWeight = value; }
	}

	private float rightLeftWeight = 0.0f;	// animator right ~ left weight.
	public float RightLeftWeight
	{
		get { return rightLeftWeight; }
		set { rightLeftWeight = value; }
	}

    private bool doForceAttackFromHitReact = false;
    public bool DoForceAttackFromHitReact
    {
        get { return doForceAttackFromHitReact; }
        set { doForceAttackFromHitReact = value; }
    }

	private bool forceStopMove = false;
	public bool ForceStopMove
	{
		get { 	return forceStopMove;	}
		set 
		{
			if (forceStopMove != value && !value)
			{
				forceStopMove = value;	// modify forceStopMove flag before do state OnEnter(). 

				// not force stop move any more, do OnEnter to reset enter state information.
				if (currState == AIState.GO_TO_TARGET)
				{
					OnEnterGoToTarget();
				}
				else if (currState == AIState.WANDER)
				{
					OnEnterWander();
				}
			}
			else
			{
				forceStopMove = value;
			}
		}
	}
	
    private void SetEquipmentTarget()
    {
		foreach (ProjectileLauncher launcher in equipmentProjectileLaunchers)
        {
            if (launcher)
            {
                launcher.SetTarget(AtkTargetObj);
            }
        }
    }

	void Awake()
	{
		character = GetComponent<Character>();
		animator = GetComponent<Animator>();
		navAgent = GetComponent<NavMeshAgent>();
		FSM = GetComponent<PlayMakerFSM>();

		InitBrain();
	}

    // Use this for initialization
    void Start()
    {		
        // get self component :
        equipment = GetComponent<EquipmentManager>();        
		if (equipment)
		{
			Equipment[] rangeWeapons = equipment.GetEquipments(EquipmentType.RangeWeapon);
			foreach (Equipment weapon in rangeWeapons)
			{
				ProjectileLauncher launcher = weapon.equipmentObject.GetComponent<ProjectileLauncher>();
				if (launcher)
				{
					equipmentProjectileLaunchers.Add(launcher);
				}
			}
		}
        impactPause = GetComponent<ImpactPause>();

		// catch target info:
        AtkTargetObj = FindBestTarget();

        DoForceAttackFromHitReact = forceAttackFromHitReact.attackWhenTimeOut;
    }

	void InitBrain()
	{
		if (managerType == ManagerType.AIBrain)
		{
			if (locomotion == null)
			{
				locomotion = new CharacterLocomotion(animator);
			}
			
			if (FSM)
			{
				FSM.enabled = false;
			}
			
			if (navAgent)
			{
				navAgent.updateRotation = false;
			}

			InitBrainBehavior();
		}
		else
		{
			if (FSM)
			{
				FSM.enabled = true;
			}
		}

		Operate(true);
	}

	void InitBrainBehavior()
	{
		int aiStateNum = (int)AIState.AIStateNum;
		fsmSetting.behaviours = new BrainBehavior[aiStateNum];

		for (int i=0 ; i < aiStateNum ; i++)
		{
			AIState state = (AIState)i;
			BrainBehavior behavior = null;

			if (state == AIState.IDLE)
			{
				behavior = new BrainBehavior(state, character, animator, locomotion, navAgent, fsmSetting.idle.animatorParameter, fsmSetting.idle.rotateSpeed);
			}
			else if (state == AIState.GO_TO_TARGET)
			{
				behavior = new BrainBehavior(state, character, animator, locomotion, navAgent, fsmSetting.goToTarget.animatorParameter, fsmSetting.goToTarget.rotateSpeed);
			}
			else if (state == AIState.ATTACK)
			{
				behavior = new BrainBehavior(state, character, animator, locomotion, navAgent, fsmSetting.attack.animatorParameter, fsmSetting.attack.rotateSpeed);
			}
			else if (state == AIState.WAIT)
			{
				behavior = new BrainBehavior(state, character, animator, locomotion, navAgent, fsmSetting.wait.animatorParameter, fsmSetting.wait.rotateSpeed);
			}
			else if (state == AIState.WANDER)
			{
				behavior = new BrainBehavior(state, character, animator, locomotion, navAgent, fsmSetting.wander.animatorParameter, fsmSetting.wander.rotateSpeed);
			}
			else
			{
				behavior = new BrainBehavior(state, character, animator, locomotion, navAgent);
			}
			
			fsmSetting.behaviours[i] = behavior;
		}

		if (fsmSetting.wanderSetting.enable)
		{
			fsmSetting.wanderSetting.character = character;
		}
	}
	
	public void SetManagerType(ManagerType type)
	{
		managerType = type;
		InitBrain();
	}

	public void Operate(bool enable)
	{
		if (managerType == ManagerType.AIBrain)
		{
			enableBrainFSM = enable;
		}
		else 
		{
			if (FSM == null)
			{
				FSM = GetComponent<PlayMakerFSM>();
			}

			if (FSM)
			{
				FSM.enabled = enable;
			}
		}
	}
	
	public void ReTargeting(List<string> tags)
	{	
		targetSearch.tags.Clear();

		for (int i = 0 ; i < tags.Count ; i++)
		{
			targetSearch.tags.Add(tags[i]);
		}

		// catch target info:
		AtkTargetObj = FindBestTarget();
	}

    void InitTargetSearch()
    {
        targetSearch = new TargetSearch(); 
		
        // set FanArea :
        FanArea fa = new FanArea();
        fa.angle = 360.0f;
        fa.distance = 100.0f;
        targetSearch.fanAreas.Add(fa);

        if (character == null)
        {
            character = GetComponent<Character>();
        }

		List<string> tags = CharacterUtility.GetTargetSearchTags(gameObject);
		ReTargeting(tags);
    }

    void ResetTimeInHitReact()
    {
        if (forceAttackFromHitReact.attackWhenTimeOut)
        {
            TimeInHitReact = Random.Range(forceAttackFromHitReact.minTimeInHitReact, forceAttackFromHitReact.maxTimeInHitReact);
        }
    }

    void OnSpawned()
    {
        ResetTimeInHitReact();

		if (managerType == ManagerType.AIBrain)
		{
			InitBrain();
			SetState(AIState.SPAWN);
		}
    }

    void OnEnable()
    {
        AtkTargetObj = FindBestTarget();	// reset attack target when OnEnable.

        AddListener();
    }
	
    void OnDisable()
    {
        RemoveListener();
    }

    void OnDestroy()
    {
        RemoveListener();
    }
	
    void AddListener()
    {
        if (!AddListenerDone)
        {
			Messenger.AddGameObjectListener(gameObject, MessageNameEunm.SelfDeath, SelfDeath);
			Messenger.AddGameObjectListener<DamageContent>(gameObject, MessageNameEunm.SelfHitReact, SelfHitReact);
            Messenger.AddGlobalListener<GameObject>(MessageNameEunm.PlayerChanged, PlayerChanged);
            Messenger.AddGlobalListener(MessageNameEunm.PlayerDeath, PlayerDeath);
            Messenger.AddGlobalListener<GameObject>(MessageNameEunm.KillAI, AtkTargetDeath);
            
            AddListenerDone = true;
        }
    }

    void RemoveListener()
    {
        if (AddListenerDone)
        {
			if (Messenger.CheckGameObjectListener(gameObject, MessageNameEunm.SelfDeath, SelfDeath))
			{
				Messenger.RemoveGameObjectListener(gameObject, MessageNameEunm.SelfDeath, SelfDeath);
			}
			if (Messenger.CheckGameObjectListener<DamageContent>(gameObject, MessageNameEunm.SelfHitReact, SelfHitReact))
			{
				Messenger.RemoveGameObjectListener<DamageContent>(gameObject, MessageNameEunm.SelfHitReact, SelfHitReact);
			}
			if (Messenger.CheckGlobalListener<GameObject>(MessageNameEunm.PlayerChanged, PlayerChanged))
			{
				Messenger.RemoveGlobalListener<GameObject>(MessageNameEunm.PlayerChanged, PlayerChanged);
			}
			if (Messenger.CheckGlobalListener(MessageNameEunm.PlayerDeath, PlayerDeath))
			{
				Messenger.RemoveGlobalListener(MessageNameEunm.PlayerDeath, PlayerDeath);
			}
			if (Messenger.CheckGlobalListener<GameObject>(MessageNameEunm.KillAI, AtkTargetDeath))
			{
				Messenger.RemoveGlobalListener<GameObject>(MessageNameEunm.KillAI, AtkTargetDeath);
			} 
            AddListenerDone = false;
        }
    }

    private void SelfDeath()
    {
		if (managerType == ManagerType.AIBrain)
		{
			SetState(AIState.DEATH);
		}
		else if (FSM != null)
        {
            FSM.Fsm.Event(deathEvent);
        }
    }
	
	private void SelfHitReact(DamageContent damageContent)
    {
		ResetTimeInHitReact();
		if (damageContent.lift)
		{
			if (managerType == ManagerType.PlayMaker && FSM)
			{
				FSM.Fsm.Event(hitLiftEvent);
			}
			else
			{
				SetState(AIState.HIT_LIFT);
			}
			character.OnHitReact();
		}
		else if (character.InLift())
		{
			if (impactPause)
			{
				impactPause.Pause(0.3f);
			}
		}
		else
		{
			if (managerType == ManagerType.PlayMaker && FSM)
			{
				FSM.Fsm.Event(hitReactEvent);
			}
			else
			{
				SetState(AIState.HIT_REACT, true);
			}
			character.OnHitReact();
		}
	}	
	
	private void PlayerChanged(GameObject target)
    {
        if (character && !character.IsAIPlayer)
        {
            AtkTargetObj = target;
        }
    }

    private void AtkTargetDeath(GameObject target)
    {
        if (!CompareTag(Tags.Player))
            return;
        
        if (target != AtkTargetObj) 
            return;
		
        CheckAnimAtDeath();
    }
	
    private void PlayerDeath()
    {
		if (!CompareTag(Tags.AI) || !CompareTag(Tags.DestructibleObj))
            return;

        CheckAnimAtDeath();
    }

    private void CheckAnimAtDeath()
    {
        if (!animator)
        {
            return;
        }
        
        // before change state, make sure current animation finish.
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        animLoopCountAtDeath = (int)Mathf.Floor(stateInfo.normalizedTime);
        checkAnimAtDeathFinish = true;
    }

    private bool IsAnimAtDeathFinish()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        int curAnimLoopCount = (int)Mathf.Floor(stateInfo.normalizedTime);
        if (curAnimLoopCount > animLoopCountAtDeath)
        {
            checkAnimAtDeathFinish = false;
            return true;
        }
        return false;
    }

    //-----------------------------------------------------------------------------------
    // About State CD Time :
    //-----------------------------------------------------------------------------------
    public void SetStateCDTimeGroup(string stateName, string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            groupName = stateName;
        }

        if (!statesCDTimeGroupTable.ContainsKey(stateName))
        {
            statesCDTimeGroupTable.Add(stateName, groupName);
        }
        else
        {
            statesCDTimeGroupTable[stateName] = groupName;
        }
    }
	
    public string GetStateCDTimeGroupName(string stateName)
    {
        if (statesCDTimeGroupTable.ContainsKey(stateName))
        {
            return statesCDTimeGroupTable[stateName];
        }
        return "";
    }

    public void SetStateCDTime(string stateName, float time)
    {
        string group = GetStateCDTimeGroupName(stateName);
        if (!statesCDTime.ContainsKey(group))
        {
            statesCDTime.Add(group, time);
        }
        else
        {
            statesCDTime[group] = time;
        }
    }

    public float GetStateCDTime(string stateName)
    {
        string group = GetStateCDTimeGroupName(stateName);
        if (statesCDTime.ContainsKey(group))
        {
            return statesCDTime[group];
        }
        return 0.0f;
    }

    public bool IsInStateCDTime(string stateName)
    {
        return (GetStateCDTime(stateName) >= GetExitStatePassTime(stateName));
    }

    public void SetStateCDTimerSwitch(string stateName, bool bValue)
    {
        string group = GetStateCDTimeGroupName(stateName);
        if (!statesCDTimeSwitch.ContainsKey(group))
        {
            statesCDTimeSwitch.Add(group, bValue);
        }
        else
        {
            statesCDTimeSwitch[group] = bValue;
        }
    }

    public bool IsStateCDTimerEnable(string stateName)
    {
        string group = GetStateCDTimeGroupName(stateName);
        if (statesCDTimeSwitch.ContainsKey(group))
        {
            return statesCDTimeSwitch[group];
        }		
        return false;
    }	
    //-----------------------------------------------------------------------------------
    // Timer to record time since state exit.
    //-----------------------------------------------------------------------------------
    public void ResetExitStatePassTime(string stateName, float resetValue = 0.0f)
    {
        string group = GetStateCDTimeGroupName(stateName);

        if (!exitStatePassTimes.ContainsKey(group))
        {
            exitStatePassTimes.Add(group, resetValue);
        }
        else
        {
            exitStatePassTimes[group] = resetValue;
        }

        if (!exitStatesNames.Contains(stateName))
        {
            exitStatesNames.Add(stateName);
        }
    }

    public float GetExitStatePassTime(string stateName)
    {
        string group = GetStateCDTimeGroupName(stateName);
        if (!exitStatePassTimes.ContainsKey(group))
        {
            // Init pass time a large value.
            exitStatePassTimes[group] = 100000000.0f;
        }

        return exitStatePassTimes[group];
    }

    public void AddExitStatePassTime(string stateName, float addValue)
    {
        string group = GetStateCDTimeGroupName(stateName);
        exitStatePassTimes[group] += addValue;
    }
    //-----------------------------------------------------------------------------------
    // Update Current Info function
    //-----------------------------------------------------------------------------------
    GameObject FindBestTarget()
    {
        if (targetSearch == null)
        {
            InitTargetSearch();
        }

        return targetSearch.FindBest(gameObject.transform);
    }

    public GameObject RefreshAttackTarget(float interval = 0.0f)
    {
		if (AtkTargetObj == null || refreshAttackTargetCD > interval)
		{
	        AtkTargetObj = FindBestTarget();

			refreshAttackTargetCD = 0.0f;
		}

		refreshAttackTargetCD += Time.deltaTime;
    
		return AtkTargetObj;
    }

    //-----------------------------------------------------------------------------------
    // Update status function :
    //-----------------------------------------------------------------------------------
    bool HaveTarget()
    {	
        return (AtkTargetObj != null);
    }
	
    //-----------------------------------------------------------------------------------
    // Update :
    //-----------------------------------------------------------------------------------	
    void UpdateCurrentInfo()
    {
        // without attack target, do refresh :
        if (AtkTargetObj == null || 
            (atkTargetCharacter != null && atkTargetCharacter.isDying) ||
            !AtkTargetObj.gameObject.activeInHierarchy)
        {
            AtkTargetObj = FindBestTarget();
        }
    }

    public void UpdateExitStatePassTimes()
    {
		for (int i = 0; i < exitStatesNames.Count; i++)
        {
			string stateName = exitStatesNames[i];
            if (IsStateCDTimerEnable(stateName))
            {
                AddExitStatePassTime(stateName, Time.deltaTime);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
		if (enableBrainFSM)
		{
			UpdateStateMachine();
		}

        UpdateCurrentInfo();
        UpdateExitStatePassTimes();

		if (alwaysLookAtPlayer && atkTargetObj && animator && character)
        {
			if (character.AIEliteType == EliteType.MiniBoss || character.AIEliteType == EliteType.Boss) 
			{
				animator.SetLookAtPosition(atkTargetObj.transform.position + playerHeight);
				animator.SetLookAtWeight(0.5f, 0.3f);
			}
        }

        if (checkAnimAtDeathFinish && IsAnimAtDeathFinish())
        {
			if (managerType == ManagerType.AIBrain)
			{
				SetState(AIState.IDLE);
			}
			else if (FSM != null)
			{
				FSM.Fsm.Event(atkTargetDeathEvent);
			}    
		}
    }

	//---------------------------------------------------------------------------------------------------------
	// about AI Brain state machine :
	//---------------------------------------------------------------------------------------------------------
	void UpdateStateMachine()
	{
		if (currState != nextState || enforceReEnterState)
		{
			stateTimer = 0.0f;

			OnStateExit(currState);
			OnStateEnter(nextState);

			currState = nextState;
			enforceReEnterState = false;
			delayFSMUpdateTime = 1.0f;

			if (Debug.isDebugBuild) {
				Logger.Log(Logger.LogType.AI, "[" + gameObject.name + "] change to FSM state = " + currState);
			}
		}
		else
		{
			stateTimer += Time.deltaTime;
		}

		// Do it in 5 FPS
		if (delayFSMUpdateTime > 0.2f)
		{
			OnStateUpdate(currState);
			delayFSMUpdateTime = 0.2f * Random.value;
		}
		else
		{
			delayFSMUpdateTime += Time.deltaTime;
		}

		OnStatePostUpdate(currState);
	}

	void OnEnterGoToTarget()
	{
		if (ForceStopMove)
		{
			return;
		}

		BrainBehavior behavior = fsmSetting.behaviours[(int)AIState.GO_TO_TARGET];
		behavior.OnEnter();

		navAgent.SetDestination(AtkTargetObj.transform.position);
		preDesiredVelocity = navAgent.desiredVelocity;
	}

	void OnEnterWander()
	{
		if (ForceStopMove)
		{
			return;
		}

		BrainBehavior behavior = fsmSetting.behaviours[(int)AIState.WANDER];
		behavior.OnEnter();

		fsmSetting.wanderSetting.finish = false;
		
		preDesiredVelocity = navAgent.desiredVelocity;
		BehaviorFunc.SetCustomMoveTargetObject(gameObject, fsmSetting.wanderSetting, this);
		BehaviorFunc.ComputeCustomMoveDestination(gameObject, fsmSetting.wanderSetting, this);
		BehaviorFunc.SetCustomMoveDestination(fsmSetting.wanderSetting, navAgent);
		BehaviorFunc.UpdateCustomMove(gameObject, fsmSetting.wanderSetting, navAgent, behavior.RotateSpeed, ref preDesiredVelocity);
	}

	void OnStateEnter(AIState state)
	{
		BrainBehavior behavior = fsmSetting.behaviours[(int)state];

		if (state != AIState.GO_TO_TARGET && state != AIState.WANDER)
		{
			behavior.OnEnter();
		}

		if (state == AIState.IDLE)
		{
			BehaviorFunc.FaceTarget(transform, AtkTargetObj, behavior.RotateSpeed);
		}
		else if (state == AIState.GO_TO_TARGET)
		{
			OnEnterGoToTarget();
		}
		else if (state == AIState.ATTACK)
		{
			BehaviorFunc.MoveByRootMotion(navAgent);
			BehaviorFunc.FaceTarget(transform, AtkTargetObj, behavior.RotateSpeed);
		}
		else if (state == AIState.WAIT)
		{
			BehaviorFunc.FaceTarget(transform, AtkTargetObj, behavior.RotateSpeed);
			randomWaitTime = Random.Range(fsmSetting.minWaitTime, fsmSetting.maxWaitTime);
		}
		else if (state == AIState.WANDER)
		{
			OnEnterWander();
		}
	}

	void OnStateExit(AIState state)
	{
		BrainBehavior behavior = fsmSetting.behaviours[(int)state];
		behavior.OnExit();

		if (state == AIState.GO_TO_TARGET)
		{
			if (navAgent && navAgent.hasPath)
			{
				navAgent.Stop();
			}
		}
		else if (state == AIState.WANDER)
		{
			if (navAgent)
			{
				navAgent.Stop();
			}
		}
	}

	void OnStateUpdate(AIState state)
	{
		BrainBehavior behavior = fsmSetting.behaviours[(int)state];

		if (state == AIState.SPAWN)
		{
			if (AnimatorFunc.IsDoingAnim(animator, AnimatorStateHashName.Idle))
			{
				SetState(AIState.IDLE);
				return;
			}
		}
		else if (state == AIState.IDLE)
		{
			if (BehaviorFunc.InDistance(gameObject, AtkTargetObj, alertDistance))
			{
				SetState(AIState.GO_TO_TARGET);
				return;
			}
			else if (BehaviorFunc.InDistance(gameObject, AtkTargetObj, attackDistance))
			{
				SetState(AIState.ATTACK);
				return;
			}
		}
		else if (state == AIState.GO_TO_TARGET)
		{
			if (ForceStopMove)
			{
				return;
			}

			RefreshAttackTarget(1.0f);

			if (BehaviorFunc.InDistance(gameObject, AtkTargetObj, attackDistance))
			{
				SetState(AIState.ATTACK);
				return;
			}

			navAgent.SetDestination(AtkTargetObj.transform.position);
		}
		else if (state == AIState.ATTACK)
		{
			if (AnimatorFunc.AnimFinished(animator, AnimatorStateHashName.Attack))
			{
				SetState(AIState.WAIT);
				return;
			}
		}
		else if (state == AIState.WAIT)
		{
			if (stateTimer > randomWaitTime)
			{
				if (fsmSetting.wanderSetting.enable)
				{
					SetState(AIState.WANDER);
				}
				else
				{
					SetState(AIState.GO_TO_TARGET);
				}
			}
		}
		else if (state == AIState.WANDER)
		{
			if (ForceStopMove)
			{
				return;
			}

			if (fsmSetting.wanderSetting.finish ||
			    stateTimer > fsmSetting.wanderSetting.moveTimer)
			{
				SetState(AIState.GO_TO_TARGET);
				BehaviorFunc.ComputeCustomMoveDestination(gameObject, fsmSetting.wanderSetting, this);
				BehaviorFunc.SetCustomMoveDestination(fsmSetting.wanderSetting, navAgent);
				BehaviorFunc.UpdateCustomMove(gameObject, fsmSetting.wanderSetting, navAgent, behavior.RotateSpeed, ref preDesiredVelocity);
				return;
			}

			BehaviorFunc.SetCustomMoveDestination(fsmSetting.wanderSetting, navAgent);
		}
		else if (state == AIState.HIT_REACT)
		{
			if (AnimatorFunc.AnimFinished(animator, AnimatorStateHashName.HitReact))
			{
				SetState(AIState.IDLE);
				return;
			}
		}
		else if (state == AIState.HIT_LIFT)
		{
			if (AnimatorFunc.AnimFinished(animator, AnimatorStateHashName.HitLift))
			{
				SetState(AIState.IDLE);
				return;
			}
		}
		else if (state == AIState.STUN)
		{
			if (!AnimatorFunc.IsDoingAnim(animator, AnimatorStateHashName.Stun))
			{
				SetState(AIState.IDLE);
				return;
			}
		}
	}

	void OnStatePostUpdate(AIState state)
	{
		BrainBehavior behavior = fsmSetting.behaviours[(int)state];
		
		if (state == AIState.IDLE)
		{
			BehaviorFunc.FaceTarget(transform, AtkTargetObj, behavior.RotateSpeed);
		}
		else if (state == AIState.GO_TO_TARGET)
		{
			if (ForceStopMove)
			{
				return;
			}

			BehaviorFunc.UpdateMove(navAgent, ref preDesiredVelocity, transform, behavior.RotateSpeed);
		}
		else if (state == AIState.ATTACK)
		{
			BehaviorFunc.FaceTarget(transform, AtkTargetObj, behavior.RotateSpeed);
		}
		else if (state == AIState.WANDER)
		{
			if (ForceStopMove)
			{
				return;
			}

			BehaviorFunc.UpdateCustomMove(gameObject, fsmSetting.wanderSetting, navAgent, behavior.RotateSpeed, ref preDesiredVelocity);
		}
	}
	//---------------------------------------------------------------------------------------------------------
	// function for AIBrain state machine:
	//---------------------------------------------------------------------------------------------------------
	void SetState(AIState state, bool enforceReEnter = false)
	{
		nextState = state;
		enforceReEnterState = enforceReEnter;
		if (Debug.isDebugBuild) {
			Logger.Log(Logger.LogType.AI, "[" + gameObject.name + "] change to FSM state from " + currState + " -> " + nextState);
		}
	}
	//---------------------------------------------------------------------------------------------------------
	public void ResetBrainState()
	{
		if (managerType == ManagerType.AIBrain)
		{
			SetState(AIState.IDLE);
			locomotion.DoAnimState(AnimatorStateHashName.Idle);
		}
		else
		{
			FSM.Fsm.Event(atkTargetDeathEvent);			// enforce FSM change to idle state.
			animator.Play(AnimatorStateHashName.Idle);
		}
	}
}


